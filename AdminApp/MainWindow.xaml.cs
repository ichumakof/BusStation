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
    }
}