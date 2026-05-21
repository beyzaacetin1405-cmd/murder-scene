using System.Windows;
using DedektifOyunu.Services;
using DedektifOyunu.ViewModels;

namespace DedektifOyunu
{
    // ============================================================
    // COMPOSITION ROOT - Bağımlılık Ağacının Tek Kurulum Noktası
    // ============================================================
    //
    // NEDEN COMPOSITION ROOT?
    // ─────────────────────────────────────────────────────────────
    // Tüm 'new' çağrıları (bağımlılık yaratma) uygulamanın tek
    // bir noktasında yapılmalıdır.
    // Bu noktaya "Composition Root" (Mark Seemann) denir.
    //
    // Alternatif (Yanlış): ViewModel'de new GameService()
    //   - Her katman birbirini yaratır → Spaghetti DI
    //   - Değiştirmek için her sınıfı açmak gerekir
    //
    // Doğru (Bu yaklaşım): MainWindow, tüm bağımlılıkları kurar,
    //   inject eder ve ardından çekilir. Sonrası onun işi değil.
    //
    // Gerçek Dünya: ASP.NET Core'daki Program.cs (builder.Services...)
    //               ve Unity'deki Bootstrap/Installer pattern'leri
    //               tam bu prensibi uygular.
    // ─────────────────────────────────────────────────────────────
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // ── Composition Root ──
            // 1) Servisi oluştur (Concrete Implementation)
            IGameService gameService = new GameService();

            // 2) ViewModel'i servisi inject ederek oluştur
            var viewModel = new GameViewModel(gameService);

            // 3) DataContext'e ata: XAML artık ViewModel'i görür
            this.DataContext = viewModel;
            // Bundan sonra MainWindow.xaml.cs'de tek satır kod yok.
            // Tüm UI mantığı XAML Binding ve ViewModel'de.
        }
    }
}