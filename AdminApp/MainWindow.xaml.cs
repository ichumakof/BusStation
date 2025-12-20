using AdminApp.Views;
using AdminApp.ViewModels;
using BLL.Interfaces;
using BLL.Services;
using System;
using System.Windows;

namespace AdminApp
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _vm;
        private readonly IScheduleService _scheduleService = new ScheduleService();

        public MainWindow()
        {
            InitializeComponent();

            _vm = new MainWindowViewModel();
            DataContext = _vm;

            // Подписка на событие открытия генератора — View откроет модальное окно
            _vm.RequestOpenScheduleGenerator += Vm_RequestOpenScheduleGenerator;
            _vm.RequestShowMessage += Vm_RequestShowMessage;
            _vm.RequestOpenUsers += Vm_RequestOpenUsers;
            _vm.RequestOpenReports += Vm_RequestOpenReports;
        }

        private void Vm_RequestShowMessage(object sender, MessageRequestEventArgs e)
        {
            MessageBox.Show(e.Message, e.Caption, e.Button, e.Icon);
        }

        private void Vm_RequestOpenScheduleGenerator(object sender, EventArgs e)
        {
            var scheduleWindow = new ScheduleGeneratorView(_scheduleService);
            scheduleWindow.Owner = this;
            scheduleWindow.ShowDialog();
        }
        private void Vm_RequestOpenUsers(object sender, EventArgs e)
        {
            // Пытаемся получить id текущего администратора
            int currentAdminId = 0;
            try
            {
                var user = App.Current.GetType().GetProperty("CurrentUser")?.GetValue(App.Current, null);
                if (user != null)
                {
                    var idProp = user.GetType().GetProperty("UserID") ?? user.GetType().GetProperty("Id") ?? user.GetType().GetProperty("ID");
                    if (idProp != null) currentAdminId = Convert.ToInt32(idProp.GetValue(user));
                }
            }
            catch { }

            IUserService userService = FactoryService.CreateUserService();

            var wnd = new UsersWindow(userService, currentAdminId) { Owner = this };
            wnd.ShowDialog();
        }
        private void Vm_RequestOpenReports(object sender, EventArgs e)
        {
            var wnd = new ReportsWindow { Owner = this };
            wnd.ShowDialog();
        }
    }
}