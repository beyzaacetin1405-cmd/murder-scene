using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DedektifOyunu.Domain;
using DedektifOyunu.Services;
using System;
using System.Linq;
using Avalonia.Threading;

namespace DedektifOyunu.ViewModels
{
    // ============================================================
    // PRESENTATION KATMANI - PRODUCTION-READY VIEWMODEL
    // ============================================================
    //
    // NEDEN BU KADAR İNCE?
    // ─────────────────────────────────────────────────────────────
    // "Thin ViewModel, Fat Service" prensibi.
    // ViewModel'in TEK SORUMLULUĞU: Domain verisini UI'a adapte etmek.
    // İş kuralı YOKTUR burada. "Scarlett itiraf etti mi?" sorusunu
    // ViewModel bilmez. GameService'e sorar, cevabı gösterir.
    //
    // Fark neden önemli?
    // Önceki kodda ViewModel'de 200+ satır oyun mantığı vardı.
    // Şimdi ViewModel yalnızca ~80 satır: Komutları bağla, sonuçları göster.
    // ─────────────────────────────────────────────────────────────

    public class GameViewModel : INotifyPropertyChanged
    {
        // ──────────────────────────────────────────────────────
        // Bağımlılık Enjeksiyonu (Dependency Injection)
        // ──────────────────────────────────────────────────────
        // NEDEN INTERFACE (IGameService)?
        // 'new GameService()' yazsaydık ViewModel somut tipe bağlanırdı.
        // Interface ile: Mock servis geçirilebilir (testability).
        // Gelecekte: SaveGameService, NetworkGameService ayrımı mümkün.
        private readonly IGameService _gameService;

        // ──────────────────────────────────────────────────────
        // UI'a açık property'ler (ObservableCollection)
        // ──────────────────────────────────────────────────────
        // Domain'daki IReadOnlyList → UI'da ObservableCollection olarak wrap.
        // ViewModel burada Domain ↔ UI arasında köprü kurar (Adapter Pattern).
        public ObservableCollection<LocationAggregate> Locations { get; }
        public ObservableCollection<CharacterEntity> CurrentSuspects { get; }
        public ObservableCollection<EvidenceEntity> Inventory { get; }

        private CharacterEntity? _selectedSuspect;
        public CharacterEntity? SelectedSuspect
        {
            get => _selectedSuspect;
            set { _selectedSuspect = value; OnPropertyChanged(); }
        }

        private EvidenceEntity? _selectedEvidence;
        public EvidenceEntity? SelectedEvidence
        {
            get => _selectedEvidence;
            set { _selectedEvidence = value; OnPropertyChanged(); }
        }

        private LocationAggregate? _currentLocation;
        public LocationAggregate? CurrentLocation
        {
            get => _currentLocation;
            private set { _currentLocation = value; OnPropertyChanged(); RefreshSuspects(); }
        }

        // Oyun State'ini Domain'den oku, kopyalama.
        // Single Source of Truth: Tek gerçek kaynak GameSession.
        public GameState CurrentState => _gameService.Session.CurrentState;

        // UI visibility'leri computed property — bool field tutmaya gerek yok.
        // NEDEN COMPUTED?
        // Önceki kodda: _isIntroVisible = true; _isInvestigationVisible = false;
        // Manuel state sync = bug kaynağı. Şimdi otomatik: State değişti → UI güncellendi.
        public bool IsIntroVisible       => CurrentState == GameState.Splash || CurrentState == GameState.CaseBriefing;
        public bool IsInvestigationVisible => CurrentState == GameState.Investigating || CurrentState == GameState.Interrogating;
        public bool IsFocusModeActive    => CurrentState == GameState.Interrogating;
        public bool IsGameOverVisible    => CurrentState == GameState.GameOver;
        public bool IsCaseSolvedVisible  => CurrentState == GameState.CaseSolved;

        public int PlayerReputation => _gameService.Session.PlayerReputation;

        private string _gameMessage = "Soruşturma başlamak üzere...";
        public string GameMessage
        {
            get => _gameMessage;
            set { _gameMessage = value; OnPropertyChanged(); }
        }

        // Daktilo efekti için gösterilen anlık metin
        private string _displayedDialogue = string.Empty;
        public string DisplayedDialogue
        {
            get => _displayedDialogue;
            set { _displayedDialogue = value; OnPropertyChanged(); }
        }

        // Komutlar (ICommand)
        public ICommand StartGameCommand { get; }
        public ICommand TravelCommand { get; }
        public ICommand InspectCharacterCommand { get; }
        public ICommand CloseFocusModeCommand { get; }
        public ICommand SearchSceneCommand { get; }
        public ICommand PressCommand { get; }
        public ICommand DoubtCommand { get; }
        public ICommand PresentEvidenceCommand { get; }
        public ICommand AccuseCommand { get; }
        public ICommand RestartGameCommand { get; }

        // Daktilo animasyonu için DispatcherTimer (WPF UI thread'e özgü)
        private readonly DispatcherTimer _typewriterTimer;
        private string _typewriterTarget = "";
        private int _typewriterIndex;

        public GameViewModel(IGameService gameService)
        {
            // Constructor Injection: Bağımlılık dışarıdan gelir.
            // NEDEN?
            // GameViewModel() içinde 'new GameService()' yazmak:
            //   - Unit test imkansız
            //   - Tight coupling (sıkı bağlantı)
            // Dışarıdan inject edilen servis: Mock edilebilir, test edilebilir.
            _gameService = gameService;

            Locations        = new ObservableCollection<LocationAggregate>(_gameService.Locations);
            CurrentSuspects  = new ObservableCollection<CharacterEntity>();
            Inventory        = new ObservableCollection<EvidenceEntity>();

            CurrentLocation = _gameService.Locations.FirstOrDefault();

            // Komut bağlamaları
            StartGameCommand       = new RelayCommand(_ => ExecuteStartGame());
            TravelCommand          = new RelayCommand(loc => ExecuteTravel(loc as LocationAggregate));
            InspectCharacterCommand= new RelayCommand(ch  => ExecuteInspect(ch as CharacterEntity));
            CloseFocusModeCommand  = new RelayCommand(_ => ExecuteCloseMode());
            SearchSceneCommand     = new RelayCommand(_ => ExecuteSearch());
            PressCommand           = new RelayCommand(_ => ExecuteInterrogate(InterrogationAction.Press));
            DoubtCommand           = new RelayCommand(_ => ExecuteInterrogate(InterrogationAction.Doubt));
            PresentEvidenceCommand = new RelayCommand(_ => ExecuteInterrogate(InterrogationAction.PresentEvidence, SelectedEvidence));
            AccuseCommand          = new RelayCommand(_ => ExecuteAccuse());
            RestartGameCommand     = new RelayCommand(_ => ExecuteRestartGame());

            _typewriterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(18) };
            _typewriterTimer.Tick += OnTypewriterTick;
        }

        private void ExecuteStartGame()
        {
            _gameService.StartGame();
            CurrentLocation = _gameService.CurrentLocation;
            GameMessage = "4 Bölge, 10 Şüpheli, 1 Katil. Arthur Pendelton Davası başlıyor.";
            RefreshStateUI();
        }

        private void ExecuteTravel(LocationAggregate? location)
        {
            if (location == null) return;
            _gameService.TravelTo(location);
            CurrentLocation = location;
            GameMessage = $"{location.Name} — {location.AtmosphereDescription}";
        }

        private void ExecuteSearch()
        {
            var found = _gameService.SearchScene();
            if (found != null)
            {
                Inventory.Add(found);
                GameMessage = $"🔍 DELİL BULUNDU: {found.Name}";
            }
            else
            {
                GameMessage = "Bu mekanda bulunacak her şey bulundu.";
            }
        }

        private void ExecuteInspect(CharacterEntity? character)
        {
            if (character == null) return;
            
            // Sadece Investigating modundayken Interrogating'e geçiş yap.
            // Splash/CaseBriefing durumunda bu geçiş geçersiz ve crash'e yol açar.
            if (_gameService.Session.CurrentState == GameState.Investigating)
                _gameService.Session.TransitionTo(GameState.Interrogating);
            
            SelectedSuspect = character;
            RefreshStateUI();
            StartTypewriter(character.BaseDialogues.FirstOrDefault() ?? "...");
        }

        private void ExecuteCloseMode()
        {
            _typewriterTimer.Stop();
            SelectedSuspect = null;
            _gameService.Session.TransitionTo(GameState.Investigating);
            RefreshStateUI();
        }

        // ──────────────────────────────────────────────────────
        // Merkezi Sorgu Yöneticisi
        // ──────────────────────────────────────────────────────
        // NEDEN TEK METOT?
        // Press, Doubt, PresentEvidence hepsi aynı Service metodunu çağırır.
        // Sadece 'action' parametresi farklıdır.
        // Tekrar eden kod (DRY - Don't Repeat Yourself) önlendi.
        private void ExecuteInterrogate(InterrogationAction action, EvidenceEntity? evidence = null)
        {
            if (SelectedSuspect == null) return;

            var outcome = _gameService.Interrogate(SelectedSuspect, action, evidence);
            ApplyOutcome(outcome);
        }

        private void ExecuteAccuse()
        {
            if (SelectedSuspect == null) return;
            var outcome = _gameService.Accuse(SelectedSuspect);
            GameMessage = outcome.DialogueLine;
            RefreshStateUI();
        }

        private void ExecuteRestartGame()
        {
            _typewriterTimer.Stop();
            _gameService.RestartGame();
            Locations.Clear();
            foreach (var loc in _gameService.Locations)
                Locations.Add(loc);
            Inventory.Clear();
            SelectedSuspect = null;
            SelectedEvidence = null;
            CurrentLocation = _gameService.CurrentLocation;
            GameMessage = "Yeni bir dava başlıyor... Katil değişti!";
            RefreshStateUI();
        }

        // ──────────────────────────────────────────────────────
        // Sonuç Uygulayıcı (Outcome Consumer)
        // ──────────────────────────────────────────────────────
        // Domain'den gelen immutable InterrogationOutcome → UI'ya yansıt.
        // NEDEN AYRI BİR METOT?
        // Tüm sorgu tipleri (Press, Doubt, Evidence) sonucu aynı şekilde
        // uyguluyor. DRY prensibi: Tek yer, tek sorumluluk.
        private void ApplyOutcome(InterrogationOutcome outcome)
        {
            StartTypewriter(outcome.DialogueLine);
            GameMessage = outcome.Result switch
            {
                InterrogationResult.EvidenceMatchFound    => "Delilin bir etkisi oldu!",
                InterrogationResult.SuspectBroke          => "💥 KIRILMA NOKTASI! Şüpheli çöktü!",
                InterrogationResult.InnocentAccused       => "❌ YANLIŞ HAMLE! İtibar düştü!",
                InterrogationResult.EvidenceMismatch      => "Alakasız delil. Dikkatli ol.",
                InterrogationResult.KillerAccusedWithProof=> "🏆 VAKA ÇÖZÜLDÜ!",
                _                                         => GameMessage
            };
            OnPropertyChanged(nameof(PlayerReputation)); // İtibar değişmiş olabilir, güncelle
            RefreshStateUI();
        }

        // Daktilo Efekti
        private void StartTypewriter(string text)
        {
            _typewriterTimer.Stop();
            _typewriterTarget = text;
            _typewriterIndex = 0;
            DisplayedDialogue = "";
            _typewriterTimer.Start();
        }

        private void OnTypewriterTick(object? sender, EventArgs e)
        {
            if (_typewriterIndex < _typewriterTarget.Length)
            {
                DisplayedDialogue += _typewriterTarget[_typewriterIndex++];
            }
            else
            {
                _typewriterTimer.Stop();
            }
        }

        // ObservableCollection güncelle: Yeni mekana gidince şüpheliler değişir
        private void RefreshSuspects()
        {
            CurrentSuspects.Clear();
            if (_currentLocation == null) return;
            foreach (var s in _currentLocation.Suspects)
                CurrentSuspects.Add(s);
        }

        // Tüm computed UI property'leri tek seferde UI'a bildir
        private void RefreshStateUI()
        {
            OnPropertyChanged(nameof(CurrentState));
            OnPropertyChanged(nameof(IsIntroVisible));
            OnPropertyChanged(nameof(IsInvestigationVisible));
            OnPropertyChanged(nameof(IsFocusModeActive));
            OnPropertyChanged(nameof(IsGameOverVisible));
            OnPropertyChanged(nameof(IsCaseSolvedVisible));
            OnPropertyChanged(nameof(PlayerReputation));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
