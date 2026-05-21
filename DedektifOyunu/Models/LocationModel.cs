using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DedektifOyunu.Models
{
    public class LocationModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private string _backgroundImage = string.Empty;
        public string BackgroundImage { get => _backgroundImage; set { _backgroundImage = value; OnPropertyChanged(); } }

        public ObservableCollection<CharacterModel> SuspectsHere { get; set; } = new ObservableCollection<CharacterModel>();
        public ObservableCollection<EvidenceModel> EvidenceToFind { get; set; } = new ObservableCollection<EvidenceModel>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
