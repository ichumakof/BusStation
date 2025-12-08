using System;
using System.Windows;
using BLL.Interfaces; // ваш IAuthService
using BLL.Services;   // реализация AuthService

namespace AdminApp
{
    public partial class App : Application
    {
        public static BLL.Models.UserDTO CurrentUser { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Предотвращаем автоматическое завершение при закрытии временного окна логина
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            IAuthService authService = new AuthService();
            var login = new Views.LoginWindow(authService);

            bool? res;
            try
            {
                res = login.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при показе окна входа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            if (res != true)
            {
                // Отмена входа или крестик — корректно закрываем
                Shutdown();
                return;
            }

            var user = login.LoggedUser;

            
            if (user == null)
            {
                Shutdown();
                return;
            }

            App.CurrentUser = user;
            // Разрешаем только администраторам
            if (!string.Equals(App.CurrentUser?.RoleName?.Trim(), "Administrator", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("У вашей учетной записи нет доступа к административному приложению.", "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }
            // Защита: если начало завершения уже запущено — прекращаем
            if (Application.Current.Dispatcher.HasShutdownStarted || Application.Current.Dispatcher.HasShutdownFinished)
            {
                MessageBox.Show("Приложение уже завершает работу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var main = new MainWindow();

                // Назначаем главное окно приложения до показа
                Application.Current.MainWindow = main;

                // Возвращаем нормальное поведение завершения приложения (по желанию)
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;

                main.Show();
            }
            catch (InvalidOperationException invEx)
            {
                MessageBox.Show("Не удалось открыть главное окно (приложение в процессе завершения): " + invEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании главного окна: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}