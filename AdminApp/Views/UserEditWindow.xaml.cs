using System.Windows;
using AdminApp.ViewModels;

namespace AdminApp.Views
{
    public partial class UserEditWindow : Window
    {
        public UserEditWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as UserEditViewModel;
            if (vm == null)
            {
                DialogResult = false;
                return;
            }

            // Копируем пароль из PasswordBox в VM
            vm.Password = pwdBox.Password ?? string.Empty;

            var validation = vm.Validate();
            if (!string.IsNullOrEmpty(validation))
            {
                MessageBox.Show(this, validation, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (vm.SaveCommand.CanExecute(null))
                vm.SaveCommand.Execute(null);
        }
    }
}