using DedektifOyunu.Domain;

namespace DedektifOyunu.Services
{
    public interface IGameService
    {
        GameSession Session { get; }
        IReadOnlyList<LocationAggregate> Locations { get; }
        LocationAggregate CurrentLocation { get; }

        bool StartGame();
        bool TravelTo(LocationAggregate location);
        EvidenceEntity? SearchScene();

        InterrogationOutcome Interrogate(CharacterEntity suspect, InterrogationAction action, EvidenceEntity? evidence = null);
        InterrogationOutcome Accuse(CharacterEntity suspect);
        void RestartGame();
    }

    public enum InterrogationAction { Press, Doubt, PresentEvidence }

    public sealed class GameService : IGameService
    {
        public GameSession Session { get; } = new GameSession();
        private readonly Random _random = new Random();

        private readonly List<LocationAggregate> _locations = new();
        public IReadOnlyList<LocationAggregate> Locations => _locations.AsReadOnly();

        private LocationAggregate _currentLocation = null!;
        public LocationAggregate CurrentLocation => _currentLocation;

        public GameService()
        {
            BuildWorld();
            _currentLocation = _locations[0];
        }

        private void BuildWorld()
        {
            _locations.Clear();

            string baseDir = System.AppContext.BaseDirectory;
            
            // Arkaplanlar
            string bgMalikane   = System.IO.Path.Combine(baseDir, "Images", "bg_malikane.png");
            string bgOtel       = System.IO.Path.Combine(baseDir, "Images", "bg_otel.png");
            string bgGeceKulubu = System.IO.Path.Combine(baseDir, "Images", "bg_gecekuclub.png");
            string bgLiman      = System.IO.Path.Combine(baseDir, "Images", "bg_liman.png");

            // Karakter Görselleri
            string imgCansu = System.IO.Path.Combine(baseDir, "Images", "1.png"); // Yeşil
            string imgBeyza = System.IO.Path.Combine(baseDir, "Images", "2.png"); // Beyaz
            string imgKerem = System.IO.Path.Combine(baseDir, "Images", "3.png"); // Siyah Deri
            string imgEmir  = System.IO.Path.Combine(baseDir, "Images", "4.png"); // BMW
            string imgBurak = System.IO.Path.Combine(baseDir, "Images", "5.png"); // Takım

            // Ana Deliller
            var bıcak = new EvidenceEntity { Name = "Kanlı Bıçak", Description = "Bıçağın kabzasında katilin kıyafetinden bir parça kalmış." };
            var iz    = new EvidenceEntity { Name = "Çamurlu Ayak İzi", Description = "Olay yerinden dışarı giden taze bir iz." };
            var kayıt = new EvidenceEntity { Name = "Güvenlik Kaydı", Description = "Karanlık bir silüet hızla uzaklaşıyor." };
            var not   = new EvidenceEntity { Name = "Tehdit Mektubu", Description = "Mirasın bölünmesini istemeyen birinden gelen bir not." };

            // 1. SAHNE: VİLLA
            var villa = new LocationAggregate { Name = "LÜKS VİLLA", BackgroundImagePath = bgMalikane, AtmosphereDescription = "Cesedin bulunduğu ana mekan." };
            villa.AddSuspect(new CharacterEntity { Name = "Beyza", Title = "Evin Hanımı", ImagePath = imgBeyza, BaseDialogues = new() {"Kocamın ölümüyle yıkıldım."} });
            villa.AddSuspect(new CharacterEntity { Name = "Cansu", Title = "Hizmetçi", ImagePath = imgCansu, BaseDialogues = new() {"Vazoları siliyordum."} });
            villa.AddSuspect(new CharacterEntity { Name = "Kerem", Title = "Kardeş", ImagePath = imgKerem, BaseDialogues = new() {"Abimle miras kavgamız vardı."} });
            villa.AddSuspect(new CharacterEntity { Name = "Emir", Title = "Şoför", ImagePath = imgEmir, BaseDialogues = new() {"Araba garajdaydı."} });
            villa.AddSuspect(new CharacterEntity { Name = "Burak", Title = "Avukat", ImagePath = imgBurak, BaseDialogues = new() {"Vasiyet henüz imzalanmadı."} });
            villa.AddEvidence(bıcak);

            // 2. SAHNE: CAFE
            var cafe = new LocationAggregate { Name = "NEON CAFE", BackgroundImagePath = bgGeceKulubu, AtmosphereDescription = "Görgü tanıklarının mekanı." };
            cafe.AddSuspect(new CharacterEntity { Name = "Cansu", Title = "Garson", ImagePath = imgCansu, BaseDialogues = new() {"Hızlıca çıkan birini gördüm."} });
            cafe.AddSuspect(new CharacterEntity { Name = "Beyza", Title = "Müşteri", ImagePath = imgBeyza, BaseDialogues = new() {"Sadece kahve içtim."} });
            cafe.AddSuspect(new CharacterEntity { Name = "Kerem", Title = "Müzisyen", ImagePath = imgKerem, BaseDialogues = new() {"Sahnede kendi dünyamdaydım."} });
            cafe.AddSuspect(new CharacterEntity { Name = "Emir", Title = "Barista", ImagePath = imgEmir, BaseDialogues = new() {"Makinelerle uğraşıyordum."} });
            cafe.AddSuspect(new CharacterEntity { Name = "Burak", Title = "Düzenli Müşteri", ImagePath = imgBurak, BaseDialogues = new() {"Her zamanki köşemdeydim."} });
            cafe.AddEvidence(iz);

            // 3. SAHNE: OFİS
            var ofis = new LocationAggregate { Name = "AR-GE OFİSİ", BackgroundImagePath = bgOtel, AtmosphereDescription = "Arthur'un iş sırları." };
            ofis.AddSuspect(new CharacterEntity { Name = "Beyza", Title = "Yazılımcı", ImagePath = imgBeyza, BaseDialogues = new() {"Sistemde sızıntı yok."} });
            ofis.AddSuspect(new CharacterEntity { Name = "Kerem", Title = "Güvenlik", ImagePath = imgKerem, BaseDialogues = new() {"Kameralar o gece arızalıydı."} });
            ofis.AddSuspect(new CharacterEntity { Name = "Emir", Title = "CEO Yardımcısı", ImagePath = imgEmir, BaseDialogues = new() {"Arthur'un yerinde gözüm yoktu."} });
            ofis.AddSuspect(new CharacterEntity { Name = "Cansu", Title = "Asistan", ImagePath = imgCansu, BaseDialogues = new() {"Evrakları düzenliyordum."} });
            ofis.AddSuspect(new CharacterEntity { Name = "Burak", Title = "Analist", ImagePath = imgBurak, BaseDialogues = new() {"Şirket zordaydı."} });
            ofis.AddEvidence(kayıt);

            // 4. SAHNE: LİMAN
            var liman = new LocationAggregate { Name = "OTOPARK / LİMAN", BackgroundImagePath = bgLiman, AtmosphereDescription = "Kaçışın son noktası." };
            liman.AddSuspect(new CharacterEntity { Name = "Emir", Title = "Vale", ImagePath = imgEmir, BaseDialogues = new() {"Siyah BMW'yi kim aldı görmedim."} });
            liman.AddSuspect(new CharacterEntity { Name = "Beyza", Title = "Yolcu", ImagePath = imgBeyza, BaseDialogues = new() {"Gemi saatini bekliyordum."} });
            liman.AddSuspect(new CharacterEntity { Name = "Cansu", Title = "Temizlikçi", ImagePath = imgCansu, BaseDialogues = new() {"Vardiyam bitmişti."} });
            liman.AddSuspect(new CharacterEntity { Name = "Kerem", Title = "Tamirci", ImagePath = imgKerem, BaseDialogues = new() {"Lastik değiştiriyordum."} });
            liman.AddSuspect(new CharacterEntity { Name = "Burak", Title = "Bekçi", ImagePath = imgBurak, BaseDialogues = new() {"Fenerim bozulmuştu."} });
            liman.AddEvidence(not);

            _locations.Add(villa); _locations.Add(cafe); _locations.Add(ofis); _locations.Add(liman);

            // Katil Seçimi (VİLLA'dan)
            var villaSuspects = villa.Suspects.ToList();
            var killer = villaSuspects[_random.Next(0, 5)];
            killer.IsKiller = true;
            killer.RequiredConfessionEvidenceId = bıcak.Id;
            killer.ConfessionDialogue = "EVET BEN YAPTIM! Miras bana kalmalıydı!";

            string eskal = killer.Name switch {
                "Cansu" => "YEŞİL CEKETLİ",
                "Beyza" => "BEYAZ KIYAFETLİ",
                "Kerem" => "SİYAH DERİ CEKETLİ",
                "Emir" => "BMW LOGOLU KAPÜŞONLU",
                _ => "SİYAH TAKIM ELBİSELİ"
            };

            bıcak.Description += $" Üzerindeki kumaş lifleri {eskal} birine ait.";
            kayıt.Description += $" Görüntülerde {eskal} birinin kaçtığı görülüyor.";

            foreach (var loc in _locations) {
                foreach (var s in loc.Suspects) {
                    if (!s.IsKiller) {
                        s.BaseDialogues.Add($"O gece {eskal} birinin hızla uzaklaştığını gördüm.");
                        s.EvidenceReactions[bıcak.Id] = ($"Bu bıçak... Bu lifler {eskal} birine ait gibi!", 10, false);
                        s.EvidenceReactions[kayıt.Id] = ("Bu görüntüdeki silüeti tanıyor gibiyim.", 5, false);
                    } else {
                        s.EvidenceReactions[bıcak.Id] = ("O bıçak! Ben... Onu elime bile almadım!", 40, true);
                        s.EvidenceReactions[kayıt.Id] = ("Kamera kaydı mı? Kimse beni... yani kimseyi göremez!", 35, true);
                    }
                }
            }
        }

        public void RestartGame() { BuildWorld(); Session.Reset(); _currentLocation = _locations[0]; }
        public bool StartGame() { Session.TransitionTo(GameState.CaseBriefing); return Session.TransitionTo(GameState.Investigating); }
        public bool TravelTo(LocationAggregate loc) { _currentLocation = loc; return true; }
        public EvidenceEntity? SearchScene() { var f = _currentLocation.CollectNextEvidence(); if(f != null) Session.AddToInventory(f); return f; }
        
        public InterrogationOutcome Interrogate(CharacterEntity s, InterrogationAction a, EvidenceEntity? e = null) {
            if (a == InterrogationAction.Press) { s.ApplyStress(20); return new InterrogationOutcome(InterrogationResult.NeutralResponse, s.BaseDialogues[_random.Next(s.BaseDialogues.Count)], 0, 20, false); }
            if (a == InterrogationAction.Doubt) { s.ApplyStress(15); return new InterrogationOutcome(InterrogationResult.NeutralResponse, "Neden benden şüpheleniyorsun?", 0, 15, false); }
            if (e == null) return new InterrogationOutcome(InterrogationResult.NeutralResponse, "Henüz bir delil seçmediniz.", 0, 0, false);
            if (s.EvidenceReactions.TryGetValue(e.Id, out var r)) {
                s.ApplyStress(r.StressImpact);
                if (s.IsKiller && r.IsCritical && s.StressLevel >= 40) return new InterrogationOutcome(InterrogationResult.SuspectBroke, r.Dialogue, 20, r.StressImpact, false);
                return new InterrogationOutcome(InterrogationResult.EvidenceMatchFound, r.Dialogue, 10, r.StressImpact, false);
            }
            return new InterrogationOutcome(InterrogationResult.EvidenceMismatch, "Bunun benimle ne ilgisi var?", 0, 0, false);
        }
        
        public InterrogationOutcome Accuse(CharacterEntity s) {
            if (s.IsKiller && s.StressLevel >= 40) { Session.TransitionTo(GameState.CaseSolved); return new InterrogationOutcome(InterrogationResult.KillerAccusedWithProof, s.ConfessionDialogue, 100, 0, true); }
            Session.ChangeReputation(-30); if(Session.PlayerReputation <= 0) Session.TransitionTo(GameState.GameOver);
            return new InterrogationOutcome(InterrogationResult.InnocentAccused, "Masum birini suçladınız! İtibarınız sarsıldı.", -30, 0, false);
        }
    }
}
