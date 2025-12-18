using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BLL.Interfaces;
using BLL.Models;

namespace AdminApp.ViewModels
{
    public class UserEditViewModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private readonly int _currentAdminId;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnProp(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event EventHandler<MessageBoxEventArgs> RequestShowMessage;
        public event EventHandler<bool> RequestClose;

        public UserDTO User { get; }

        private string _password = string.Empty;
        public string Password { get => _password; set { _password = value; OnProp(nameof(Password)); } }

        public bool IsNew { get; }
        public string Title { get; }
        public bool CanEditRole { get; set; } = false;

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; private set { _isBusy = value; OnProp(nameof(IsBusy)); } }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public UserEditViewModel(IUserService userService, UserDTO userDto, int currentAdminId = 0, bool isNew = false)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _currentAdminId = currentAdminId;
            User = userDto ?? new UserDTO();
            IsNew = isNew;
            Title = IsNew ? "Новый пользователь" : "Редактирование пользователя";

            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
        }

        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(User.Login))
                return "Логин не может быть пустым.";
            if (IsNew && string.IsNullOrEmpty(Password))
                return "При создании пользователя укажите пароль.";
            if (IsNew && !string.IsNullOrEmpty(User.RoleName) && User.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                return "Создание администратора запрещено.";
            return string.Empty;
        }

        private async Task SaveAsync()
        {
            var v = Validate();
            if (!string.IsNullOrEmpty(v))
            {
                RequestShowMessage?.Invoke(this, new MessageBoxEventArgs(v, "Ошибка", MessageBoxImage.Warning));
                return;
            }

            try
            {
                IsBusy = true;

                if (IsNew)
                {
                    int newId = await Task.Run(() => _userService.Create(User, Password));
                    if (newId <= 0)
                    {
                        RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Не удалось создать пользователя.", "Ошибка", MessageBoxImage.Error));
                        return;
                    }
                }
                else
                {
                    string passToSet = string.IsNullOrEmpty(Password) ? null : Password;
                    await Task.Run(() => _userService.Update(User, passToSet));
                }

                RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Операция выполнена.", "Успех", MessageBoxImage.Information));
                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Ошибка: " + ex.Message, "Ошибка", MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public class MessageBoxEventArgs : EventArgs
        {
            public MessageBoxEventArgs(string message, string caption, MessageBoxImage icon = MessageBoxImage.None)
            {
                Message = message; Caption = caption; Icon = icon;
            }
            public string Message { get; }
            public string Caption { get; }
            public MessageBoxImage Icon { get; }
        }

        // Встроенные команды
        public class RelayCommand : ICommand
        {
            private readonly Action _act;
            private readonly Func<bool> _can;
            public RelayCommand(Action act, Func<bool> can = null) { _act = act; _can = can; }
            public bool CanExecute(object p) => _can?.Invoke() ?? true;
            public void Execute(object p) => _act();
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }

        public class AsyncRelayCommand : ICommand
        {
            private readonly Func<Task> _act;
            private readonly Func<bool> _can;
            private bool _run;
            public AsyncRelayCommand(Func<Task> act, Func<bool> can = null) { _act = act; _can = can; }
            public bool CanExecute(object p) => !_run && (_can?.Invoke() ?? true);
            public async void Execute(object p)
            {
                _run = true; CommandManager.InvalidateRequerySuggested();
                try { await _act(); }
                finally { _run = false; CommandManager.InvalidateRequerySuggested(); }
            }
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }
    }
}