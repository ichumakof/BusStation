using System;
using System.Windows;
using BLL.Interfaces;
using BLL.Models;

namespace AdminApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthService _authService;
        public UserDTO LoggedUser { get; private set; }

        public LoginWindow(IAuthService authService)
        {
            InitializeComponent();
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Text = string.Empty;

            var username = UsernameTextBox.Text?.Trim();
            var password = PasswordBox.Password;

            try
            {
                // Если Authenticate возвращает null для неверных данных — мы корректно обрабатываем
                var user = _authService.Authenticate(username, password);
                if (user == null)
                {
                    ShowError("Неверный логин или пароль");
                    return;
                }

                LoggedUser = user;

                // ВАЖНО: нужно выставить DialogResult = true, это автоматически закроет окно и ShowDialog вернёт true
                this.DialogResult = true;
            }
            catch (ArgumentException argEx)
            {
                ShowError(argEx.Message);
            }
            catch (Exception ex)
            {
                // Логирование по желанию
                ShowError("Ошибка при аутентификации");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Явно возвращаем false — ShowDialog вернёт false
            this.DialogResult = false;
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
