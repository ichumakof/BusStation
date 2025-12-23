using AdminApp.ViewModels;
using BLL.Services;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace AdminApp.Views
{
    public partial class RoutesWindow : Window
    {
        public RoutesWindow()
        {
            InitializeComponent();

            var routeManager = FactoryService.CreateRouteManagerService();

            var vm = new RoutesViewModel(routeManager);
            vm.RequestClose += Vm_RequestClose;

            DataContext = vm;
        }

        // Обработчик для закрытия окна по событию из ViewModel
        private void Vm_RequestClose(object sender, bool? dialogResult)
        {
            if (dialogResult.HasValue)
                this.DialogResult = dialogResult.Value;
            this.Close();
        }

        // Разрешаем ввод только цифр
        private static readonly Regex _digitsRegex = new Regex("^[0-9]+$");
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_digitsRegex.IsMatch(e.Text);
        }
    }
}