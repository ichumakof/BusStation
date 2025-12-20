using System.Windows;
using AdminApp.ViewModels;
using BLL.Interfaces;
using BLL.Services;

namespace AdminApp.Views
{
    public partial class ReportsWindow : Window
    {
        private readonly ReportsViewModel _vm;

        public ReportsWindow()
        {
            InitializeComponent();

            // Создаём сервисы через фабрику (FactoryService) и передаём в конструктор VM
            ITicketService ticketService = FactoryService.CreateTicketService();
            IReportService reportService = FactoryService.CreateReportService();

            _vm = new ReportsViewModel(ticketService, reportService);

            // Подписываемся на события VM вместо использования несуществующего CloseAction
            _vm.RequestClose += Vm_RequestClose;
            _vm.RequestShowMessage += Vm_RequestShowMessage;

            DataContext = _vm;

            // Асинхронная инициализация VM (не ждём синхронно)
            _ = _vm.InitializeAsync();
        }

        private void Vm_RequestShowMessage(object sender, MessageRequestEventArgs e)
        {
            MessageBox.Show(this, e.Message, e.Caption ?? string.Empty, e.Button, e.Icon);
        }

        private void Vm_RequestClose(object sender, bool success)
        {
            // Если окно открыто как диалог, можно задать DialogResult
            if (this.IsVisible && this.Owner != null)
            {
                this.DialogResult = success;
            }
            this.Close();
        }
    }
}