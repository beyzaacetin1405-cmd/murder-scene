using System;
using System.Windows.Input;

namespace DedektifOyunu.ViewModels
{
    // RelayCommand, ICommand arayüzünü (interface) uygular. WPF'te buton tıklamalarını ViewModel'e bağlamak için kullanılır.
    // Hoca Neden Kullandın Derse: "Click eventleri (olayları) MainWindow.xaml.cs arkasına yazmak Spaghetti koda yol açar. MVVM mimarisinde buton tıklamalarını koddan ayırmak için ICommand (RelayCommand) kullandım."
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute; // Tıklanınca çalışacak metot
        private readonly Predicate<object?>? _canExecute; // Butonun tıklanabilir olup olmadığını belirleyen metot

        // Constructor (Yapıcı Metot)
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Buton tıklanabilir durumda mı?
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        // Buton tıklanabilirlik durumu değiştiğinde UI'ı haberdar eden event
        public event EventHandler? CanExecuteChanged;
        
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        // Tıklandığında asıl çalışan kısım
        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}
