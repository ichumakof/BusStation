using System;
using System.Windows;
using BLL.Interfaces;    // IAuthService
using BLL.Models;       // UserDTO
using CashierApp.ViewModels;

namespace CashierApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _vm;

        public LoginWindow(IAuthService authService)
        {
            InitializeComponent();

            _vm = new LoginViewModel(authService ?? throw new ArgumentNullException(nameof(authService)));
            _vm.RequestClose += Vm_RequestClose;

            DataContext = _vm;
        }

        private void Vm_RequestClose(object sender, bool dialogResult)
        {
            this.DialogResult = dialogResult;
            this.Close();
        }

        // Внешний код (App.OnStartup и т.д.) может получить результат
        public UserDTO LoggedUser => _vm?.LoggedUser;
    }
}
