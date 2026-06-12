using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DedektifOyunu.Models
{
    // AAA Kalitesinde Model Yapısı (L.A. Noire Esintili)
    public class CharacterModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private string _title = string.Empty;
        // Örn: "Malikane Uşağı" veya "Gizemli Yabancı"
        public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

        private string _imagePath = string.Empty;
        public string ImagePath { get => _imagePath; set { _imagePath = value; OnPropertyChanged(); } }

        private int _stressLevel = 0;
        // L.A. Noire tarzı Stres / Nabız göstergesi (0-100 arası). Soru sordukça artabilir.
        public int StressLevel { get => _stressLevel; set { _stressLevel = value; OnPropertyChanged(); } }

        private bool _isKiller;
        public bool IsKiller { get => _isKiller; set { _isKiller = value; OnPropertyChanged(); } }

        private string _currentDialogue = string.Empty;
        // Şu an ekranda okunan replik
        public string CurrentDialogue { get => _currentDialogue; set { _currentDialogue = value; OnPropertyChanged(); } }

        // Karakterin tüm ifadelerinin listesi
        public ObservableCollection<string> Statements { get; set; } = new ObservableCollection<string>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
