using System;
using System.Windows;
using BLL.Interfaces;    // IAuthService
using BLL.Models;       // UserDTO (пример)

namespace CashierApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthService _authService;

        // Публичное свойство, которое проверяет App.OnStartup
        public UserDTO LoggedUser { get; private set; }

        public LoginWindow(IAuthService authService)
        {
            InitializeComponent();
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;

            var username = UsernameTextBox.Text?.Trim();
            var password = PasswordBox.Password;

            try
            {
                var user = _authService.Authenticate(username, password); // синхронный вариант
                if (user == null)
                {
                    ShowError("Неверный логин или пароль");
                    return;
                }

                LoggedUser = user;

                // ВАЖНО: выставляем DialogResult = true — ShowDialog вернёт true и окно закроется
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при аутентификации");
                // при необходимости логируйте ex
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
