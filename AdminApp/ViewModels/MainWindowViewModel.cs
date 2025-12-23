using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AdminApp.Commons;

namespace AdminApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler RequestOpenScheduleGenerator;
        public event EventHandler<MessageRequestEventArgs> RequestShowMessage;
        public event EventHandler RequestOpenUsers;
        public event EventHandler RequestOpenReports;
        public event EventHandler RequestOpenRoutes;

        public MainWindowViewModel()
        {
            OpenScheduleGeneratorCommand = new RelayCommand(() => RequestOpenScheduleGenerator?.Invoke(this, EventArgs.Empty));
            OpenReportsCommand = new RelayCommand(() => RequestOpenReports?.Invoke(this, EventArgs.Empty));
            OpenTripsCommand = new RelayCommand(() => RequestOpenRoutes?.Invoke(this, EventArgs.Empty));
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

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}