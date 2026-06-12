using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DedektifOyunu.Models
{
    public class EvidenceModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private string _description = string.Empty;
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }

        private bool _isFound = false;
        public bool IsFound { get => _isFound; set { _isFound = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
