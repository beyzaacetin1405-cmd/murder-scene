namespace DedektifOyunu.Domain
{
    // ============================================================
    // DOMAIN KATMANI - TEMİZ MİMARİ'NİN (CLEAN ARCHITECTURE) KALBİ
    // ============================================================
    //
    // NEDEN BU KATMAN AYRILDI?
    // ─────────────────────────────────────────────────────────────
    // Geleneksel yaklaşımda tüm oyun kuralları ViewModel'e gömülür.
    // Bu "God Object" anti-pattern'dir: Scarlett == Killer? -> WPF
    // dependency olur; test edilemez; değiştirilemez; ölçeklenmez.
    //
    // Clean Architecture (Robert C. Martin) şunu diyor:
    //   "İş kuralları, UI veya DB frameworklerine bağımlı olmamalıdır."
    //
    // Bu katman; WPF'i, SQLite'ı, DispatcherTimer'ı TANIMAZ.
    // Sadece saf C# nesneleri ve iş kuralları vardır.
    // Bu sayede oyun mantığı Unit Test ile saniyede test edilebilir.
    // ============================================================

    // ──────────────────────────────────────────────────────
    // ENUM: Oyun Durumu Makinesi (State Machine Pattern)
    // ──────────────────────────────────────────────────────
    // NEDEN STATE MACHINE?
    // Naive yaklaşım: bool _isIntroVisible, bool _isFocusActive...
    // 5 bool = 32 olası durum → Mantıksız ve bug dolu.
    // State Machine: Her an SADECE 1 geçerli durum vardır.
    // Bu; AAA oyunların (Unreal Engine, Unity) core pattern'idir.
    public enum GameState
    {
        Splash,        // Açılış ekranı
        CaseBriefing,  // Vaka dosyası / Senaryo okuma
        Investigating, // Serbest keşif: mekan gezme, delil arama
        Interrogating, // Karakter sorgulama (Focus Mode)
        Verdict,       // Son karar: Katili suçlama
        CaseSolved,    // Oyun kazanıldı
        GameOver       // Oyun kaybedildi
    }

    // ──────────────────────────────────────────────────────
    // ENUM: Sorgu Sonucu (Interrogation Result)
    // ──────────────────────────────────────────────────────
    // NEDEN string döndürmüyoruz?
    // String tabanlı sonuçlar (return "success") stringly-typed
    // kod üretir: Typo'ya açık, refactor edilemez, IntelliSense yok.
    // Enum: Compiler-safe, documentation-as-code, hız için ideal.
    public enum InterrogationResult
    {
        NeutralResponse,
        SuspectPanicked,
        EvidenceMatchFound,    // Doğru delil sunuldu
        EvidenceMismatch,      // Yanlış delil sunuldu
        SuspectBroke,          // Şüpheli çöktü, itiraf ediyor
        InnocentAccused,       // Masum kişi suçlandı
        KillerAccusedWithProof // Katil yeterli delil ile suçlandı
    }

    // ──────────────────────────────────────────────────────
    // VALUE OBJECT: Sorgu Sonucu (Immutable Result Wrapper)
    // ──────────────────────────────────────────────────────
    // NEDEN VALUE OBJECT?
    // DDD (Domain-Driven Design) prensibi: Sonuçlar immutable
    // (değiştirilemez) olmalıdır. Fonksiyon çağrıldıktan sonra
    // sonuç verisi asla dışarıdan kirletilemez.
    // Eric Evans "Domain-Driven Design" kitabının temel taşı budur.
    public sealed record InterrogationOutcome(
        InterrogationResult Result,
        string DialogueLine,
        int ReputationChange,
        int StressChange,
        bool IsGameOver
    );

    // ──────────────────────────────────────────────────────
    // ENTITY: Karakter (Domain Entity with Business Rules)
    // ──────────────────────────────────────────────────────
    // NEDEN ENTITY?
    // DDD'de Entity = Kimliği (ID) olan ve zamanla değişebilen nesne.
    // Karakterin stress seviyesi değişir → Entity olmalı.
    // Ama UI'a bağımlı olmamalı → INotifyPropertyChanged YOK.
    // UI adaptasyonu ayrı katmanda (ViewModel) yapılacak.
    public sealed class CharacterEntity
    {
        // Tekil Kimlik: ViewModel'deki "Name == Scarlett" 
        // string karşılaştırmasını sonlandırır. Stringly-typed bug'ı önler.
        public Guid Id { get; } = Guid.NewGuid();

        public string Name { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string ImagePath { get; init; } = string.Empty;
        public bool IsKiller { get; set; } = false;
        public int StressLevel { get; private set; } = 0;
        public bool HasConfessed { get; private set; } = false;

        // ──────────────────────────────────────────────────
        // Delil-Yanıt Haritası (Evidence-Reaction Map)
        // ──────────────────────────────────────────────────
        // NEDEN DICTIONARY?
        // Önceki kodda PresentEvidence() içinde 7 satır if-else zinciri vardı.
        // "Open/Closed Principle" (SOLID): Yeni bir delil-tepki eklemek için
        // method'u DEĞİŞTİRMEK zorunda kalıyordun. Bu kapalı değil.
        // Dictionary ile: Yeni delil → sadece veri ekle, kod değişmez.
        // Bu yaklaşım Unity'deki ScriptableObject pattern'ine benzer.
        public Dictionary<Guid, (string Dialogue, int StressImpact, bool IsCritical)> EvidenceReactions { get; init; }
            = new Dictionary<Guid, (string, int, bool)>();

        // Spesifik İtiraf Delili (Cinayet Silahı Zorunluluğu)
        public Guid? RequiredConfessionEvidenceId { get; set; }

        // İfade havuzu: Basit konuşmalar burada, sırlar kilit arkasında
        public List<string> BaseDialogues { get; init; } = new List<string>();
        public List<string> PressedDialogues { get; init; } = new List<string>();
        public string ConfessionDialogue { get; set; } = string.Empty;

        // ──────────────────────────────────────────────────
        // Domain Metodu: ApplyStress
        // ──────────────────────────────────────────────────
        // NEDEN DOMAIN METODU?
        // Önceki kodda: SelectedSuspect.StressLevel += 10 (ViewModel'de)
        // Bu "Anemic Domain Model" anti-pattern'idir. Nesne kendi
        // kurallarını bilmez, dışarısı onu manipüle eder.
        // Rich Domain Model: Nesne kendi kurallarını uygular.
        // StressLevel asla 0'ın altına veya 100'ün üstüne çıkamaz.
        public void ApplyStress(int amount)
        {
            StressLevel = Math.Clamp(StressLevel + amount, 0, 100);
        }

        // Domain Metodu: Confess (itiraf et)
        public string Confess()
        {
            HasConfessed = true;
            StressLevel = 100;
            return ConfessionDialogue;
        }
    }

    // ──────────────────────────────────────────────────────
    // ENTITY: Delil
    // ──────────────────────────────────────────────────────
    public sealed class EvidenceEntity
    {
        // ID ile karşılaştırma: "Nadir Zehir Şişesi" == "Nadir Zehir Şişesi"
        // string karşılaştırması yerine Guid → hata imkansız hale gelir
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; init; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCollected { get; private set; } = false;
        public bool IsLocked { get; private set; } = false;

        public void Collect() => IsCollected = true;
        public void Lock() => IsLocked = true;
        public void Unlock() => IsLocked = false;
    }

    // ──────────────────────────────────────────────────────
    // AGGREGATE ROOT: Mekan (Location Aggregate)
    // ──────────────────────────────────────────────────────
    // NEDEN AGGREGATE ROOT?
    // DDD: "Bir grup objenin tutarlılığını sağlayan üst nesne."
    // Mekan → kendi karakterlerine ve delillerine sahiptir.
    // Dışarıdan doğrudan karakter listesi değiştirilemez.
    // Sadece mekan'ın metotları üzerinden erişilir.
    public sealed class LocationAggregate
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; init; } = string.Empty;
        public string BackgroundImagePath { get; init; } = string.Empty;
        public string AtmosphereDescription { get; init; } = string.Empty;

        // Encapsulated: Private backing field, read-only IReadOnlyList
        private readonly List<CharacterEntity> _suspects = new();
        private readonly List<EvidenceEntity> _evidence = new();

        // IReadOnlyList → Dışarıdan Add/Remove yapılamaz. Encapsulation sağlandı.
        public IReadOnlyList<CharacterEntity> Suspects => _suspects.AsReadOnly();
        public IReadOnlyList<EvidenceEntity> Evidence => _evidence.AsReadOnly();

        public void AddSuspect(CharacterEntity character) => _suspects.Add(character);
        public void AddEvidence(EvidenceEntity evidence) => _evidence.Add(evidence);

        // Domain Metodu: Koleksiyon dışa açık değil, mekan kendisi veriyor
        public EvidenceEntity? CollectNextEvidence()
        {
            var uncollected = _evidence.FirstOrDefault(e => !e.IsCollected && !e.IsLocked);
            uncollected?.Collect();
            return uncollected;
        }

        // Domain Metodu: Kilitli delili açığa çıkarma
        public void UnlockEvidenceByName(string name)
        {
            var ev = _evidence.FirstOrDefault(e => e.Name == name);
            ev?.Unlock();
        }
    }

    // ──────────────────────────────────────────────────────
    // AGGREGATE ROOT: Oyun Oturumu (GameSession)
    // ──────────────────────────────────────────────────────
    // NEDEN OYUN OTURUMU?
    // Oyunun tüm durumu (State) tek yerde: Save/Load için mükemmel.
    // Gelecekte JSON'a serileştirip kayıt dosyası yapılabilir.
    public sealed class GameSession
    {
        public Guid SessionId { get; } = Guid.NewGuid();
        public DateTime StartedAt { get; } = DateTime.UtcNow;
        public GameState CurrentState { get; private set; } = GameState.Splash;
        public int PlayerReputation { get; private set; } = 100;
        public List<EvidenceEntity> Inventory { get; } = new List<EvidenceEntity>();
        public List<string> AuditLog { get; } = new List<string>();

        // ──────────────────────────────────────────────────
        // Domain Metodu: Geçiş (Transition)
        // ──────────────────────────────────────────────────
        // NEDEN GEÇİŞ METODU?
        // Direkt property setter = herkes state'i değiştirebilir.
        // State Machine mantığında geçişler kontrol edilmelidir.
        // Örn: CaseSolved'dan Investigating'e geçmek anlamsız.
        public bool TransitionTo(GameState newState)
        {
            // Geçerli geçişleri tanımla (State Transition Table)
            var validTransitions = new Dictionary<GameState, HashSet<GameState>>
            {
                { GameState.Splash,        new() { GameState.CaseBriefing } },
                { GameState.CaseBriefing,  new() { GameState.Investigating } },
                { GameState.Investigating, new() { GameState.Interrogating, GameState.Verdict } },
                { GameState.Interrogating, new() { GameState.Interrogating, GameState.Investigating, GameState.CaseSolved, GameState.GameOver } },
                { GameState.Verdict,       new() { GameState.CaseSolved, GameState.GameOver, GameState.Investigating } },
                { GameState.CaseSolved,    new() { GameState.Splash } },
                { GameState.GameOver,      new() { GameState.Splash } },
            };

            if (!validTransitions[CurrentState].Contains(newState))
                return false; // Geçersiz geçiş denemesi: sessizce reddet

            AuditLog.Add($"[{DateTime.UtcNow:HH:mm:ss}] {CurrentState} → {newState}");
            CurrentState = newState;
            return true;
        }

        public void ChangeReputation(int delta)
        {
            PlayerReputation = Math.Clamp(PlayerReputation + delta, 0, 100);
            AuditLog.Add($"[{DateTime.UtcNow:HH:mm:ss}] Reputation: {PlayerReputation} (Δ{delta:+#;-#;0})");
        }

        public void AddToInventory(EvidenceEntity evidence)
        {
            Inventory.Add(evidence);
            AuditLog.Add($"[{DateTime.UtcNow:HH:mm:ss}] Evidence collected: {evidence.Name}");
        }

        // Oyunu sıfırla: Tüm state temizlenir, baştaki Splash'e döner
        public void Reset()
        {
            CurrentState = GameState.Splash;
            PlayerReputation = 100;
            Inventory.Clear();
            AuditLog.Clear();
        }
    }
}
