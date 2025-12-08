using BLL.Interfaces;
using BLL.Models;
using BLL.Services;
using System;
using System.Windows;

namespace CashierApp
{
    public partial class App : Application
    {
        public static UserDTO CurrentUser { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Защищаем приложение от автоматического завершения при закрытии временного окна логина
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            IAuthService authService = new AuthService();
            ILookupService lookupService = new LookupService();

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

            // Если пользователь отменил или логин не прошёл — корректно завершаем
            if (res != true || login.LoggedUser == null)
            {
                Shutdown();
                return;
            }

            // Сохраняем текущего пользователя ДО создания главного окна
            App.CurrentUser = login.LoggedUser;

            var role = (App.CurrentUser.RoleName ?? "").Trim();
            if (role.Equals("Cashier", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
            {
                // Если процесс завершения уже начат — прекращаем
                if (Application.Current.Dispatcher.HasShutdownStarted || Application.Current.Dispatcher.HasShutdownFinished)
                {
                    MessageBox.Show("Приложение уже завершает работу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    var main = new CashierMainWindow(lookupService);

                    // Назначаем главное окно приложения до показа — это предотвращает ранний Shutdown
                    Application.Current.MainWindow = main;

                    // Восстанавливаем нормальное поведение завершения (по желанию)
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
            else
            {
                MessageBox.Show("У вашей учетной записи нет доступа к кассовому приложению.", "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
            }
        }
    }
}
