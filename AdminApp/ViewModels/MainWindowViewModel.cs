using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace AdminApp.ViewModels
{
    public class MessageRequestEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string Caption { get; set; } = "";
        public MessageBoxButton Button { get; set; } = MessageBoxButton.OK;
        public MessageBoxImage Icon { get; set; } = MessageBoxImage.None;
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler RequestOpenScheduleGenerator;
        public event EventHandler<MessageRequestEventArgs> RequestShowMessage;
        public event EventHandler RequestOpenUsers;
        public event EventHandler RequestOpenReports;

        public MainWindowViewModel()
        {
            OpenScheduleGeneratorCommand = new RelayCommand(() => RequestOpenScheduleGenerator?.Invoke(this, EventArgs.Empty));
            OpenReportsCommand = new RelayCommand(() => RequestOpenReports?.Invoke(this, EventArgs.Empty));
            OpenTripsCommand = new RelayCommand(OpenTrips);
            OpenUsersCommand = new RelayCommand(() => RequestOpenUsers?.Invoke(this, EventArgs.Empty));

            AdminName = GetAdminName();
        }

        private string GetAdminName()
        {
            try
            {
                var user = App.Current.GetType().GetProperty("CurrentUser")?.GetValue(App.Current, null);
                if (user != null)
                {
                    var nameProp = user.GetType().GetProperty("FullName") ?? user.GetType().GetProperty("Name") ?? user.GetType().GetProperty("Login");
                    return nameProp?.GetValue(user)?.ToString() ?? "";
                }
            }
            catch { }
            return "";
        }

        private string _adminName;
        public string AdminName { get => _adminName; set { if (_adminName != value) { _adminName = value; OnPropertyChanged(nameof(AdminName)); } } }

        public ICommand OpenScheduleGeneratorCommand { get; }
        public ICommand OpenReportsCommand { get; }
        public ICommand OpenTripsCommand { get; }
        public ICommand OpenUsersCommand { get; }

        private void OpenReports()
        {
            RequestOpenReports?.Invoke(this, EventArgs.Empty);
        }

        private void OpenTrips()
        {
            RequestShowMessage?.Invoke(this, new MessageRequestEventArgs { Message = "Модуль управления рейсами в разработке", Caption = "Информация", Icon = MessageBoxImage.Information });
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Простая реализация RelayCommand
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
    }
}