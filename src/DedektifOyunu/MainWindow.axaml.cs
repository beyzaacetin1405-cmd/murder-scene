using Avalonia.Controls;
using DedektifOyunu.Services;
using DedektifOyunu.ViewModels;

namespace DedektifOyunu
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            IGameService gameService = new GameService();
            var viewModel = new GameViewModel(gameService);
            this.DataContext = viewModel;
        }
    }
}