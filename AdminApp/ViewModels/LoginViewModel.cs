using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using BLL.Interfaces;
using BLL.Models;

namespace AdminApp.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _authService;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<bool> RequestClose;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            LoginCommand = new AsyncRelayCommand(LoginAsync, () => !IsBusy);
            CancelCommand = new RelayCommand(Cancel, () => !IsBusy);
        }

        private string _username = string.Empty;
        public string Username { get => _username; set { if (_username != value) { _username = value; OnPropertyChanged(nameof(Username)); } } }

        private string _password = string.Empty;
        // Для простоты — string. При необходимости замените на SecureString.
        public string Password { get => _password; set { if (_password != value) { _password = value; OnPropertyChanged(nameof(Password)); } } }

        private string _errorMessage;
        public string ErrorMessage { get => _errorMessage; private set { if (_errorMessage != value) { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); OnPropertyChanged(nameof(HasError)); } } }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; private set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); } } }

        public UserDTO LoggedUser { get; private set; }

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }

        private async Task LoginAsync()
        {
            ErrorMessage = null;

            var username = Username?.Trim();
            var password = Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                ErrorMessage = "Введите логин";
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Введите пароль";
                return;
            }

            try
            {
                IsBusy = true;
                // Выполним синхронный Authenticate в фоновом потоке, чтобы UI не блокировался
                var user = await Task.Run(() => _authService.Authenticate(username, password));
                if (user == null)
                {
                    ErrorMessage = "Неверный логин или пароль";
                    return;
                }

                LoggedUser = user;
                RequestClose?.Invoke(this, true);
            }
            catch (ArgumentException argEx)
            {
                ErrorMessage = argEx.Message;
            }
            catch (Exception)
            {
                ErrorMessage = "Ошибка при аутентификации";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #region RelayCommand / AsyncRelayCommand

        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;
            public RelayCommand(Action execute, Func<bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }
            public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
            public void Execute(object parameter) => _execute();
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }

        public class AsyncRelayCommand : ICommand
        {
            private readonly Func<Task> _execute;
            private readonly Func<bool> _canExecute;
            private bool _isRunning;
            public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }
            public bool CanExecute(object parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);
            public async void Execute(object parameter)
            {
                _isRunning = true;
                CommandManager.InvalidateRequerySuggested();
                try { await _execute(); }
                finally { _isRunning = false; CommandManager.InvalidateRequerySuggested(); }
            }
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }

        #endregion
    }
}