using AdminApp.Views;
using BLL.Interfaces;
using BLL.Services;
using System.Windows;

namespace AdminApp
{
    public partial class MainWindow : Window
    {
        private readonly IScheduleService _scheduleService = new ScheduleService();
        public MainWindow()
        {
            InitializeComponent();

            // Устанавливаем имя администратора
            if (App.CurrentUser != null)
            {
                tbAdminName.Text = App.CurrentUser.FullName;
                this.Title = $"Автовокзал - Администратор: {App.CurrentUser.FullName}";
            }
        }

        // 1. Генератор расписания
        private void btnScheduleGenerator_Click(object sender, RoutedEventArgs e)
        {
            var scheduleWindow = new ScheduleGeneratorView(_scheduleService);
            scheduleWindow.Owner = this;
            scheduleWindow.ShowDialog();
        }

        // 2. Отчеты
        private void btnReports_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Модуль отчетов в разработке", "Информация");
        }

        // 3. Управление рейсами
        private void btnTrips_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Модуль управления рейсами в разработке", "Информация");
        }
    }
}