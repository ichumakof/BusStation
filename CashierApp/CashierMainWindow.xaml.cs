using BLL.Interfaces;
using BLL.Services;
using System;
using System.Windows;
using System.Windows.Input;

namespace CashierApp
{
    public partial class CashierMainWindow : Window
    {
        private readonly ViewModels.CashierMainViewModel _vm;

        public CashierMainWindow(ILookupService lookupService = null)
        {
            InitializeComponent();

            // Создаём VM (если lookupService == null, VM сама создаст стандартную реализацию)
            _vm = new ViewModels.CashierMainViewModel(lookupService);
            DataContext = _vm;

            // Подписываемся на запросы VM: показать MessageBox
            _vm.RequestShowMessage += Vm_RequestShowMessage;

            // Подписка на загрузку данных/ошибки выполняется внутри VM; только требуется
            // глобальная обработка кликов вне подсказок — делегируем в VM
            this.PreviewMouseDown += (s, e) =>
            {
                if (!IsMouseOverElement(lbSuggestions) && !IsMouseOverElement(tbCitySearch))
                    _vm.HideSuggestions();
            };

            // Запускаем асинхронную загрузку городов
            _ = _vm.LoadCitiesAsync();
        }

        private void Vm_RequestShowMessage(object sender, ViewModels.MessageRequestEventArgs e)
        {
            MessageBox.Show(e.Message, e.Caption, e.Button, e.Icon);
        }

        private bool IsMouseOverElement(System.Windows.FrameworkElement el)
        {
            if (el == null) return false;
            var pos = Mouse.GetPosition(el);
            return pos.X >= 0 && pos.Y >= 0 && pos.X <= el.ActualWidth && pos.Y <= el.ActualHeight;
        }
    }
}