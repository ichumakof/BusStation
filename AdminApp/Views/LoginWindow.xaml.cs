using System;
using System.Windows;
using BLL.Interfaces;
using AdminApp.ViewModels;

namespace AdminApp.Views
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
            // Устанавливаем DialogResult; окно закроется автоматически
            this.DialogResult = dialogResult;
            this.Close();
        }

        public BLL.Models.UserDTO LoggedUser => _vm?.LoggedUser;
    }
}