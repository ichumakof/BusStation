using BLL.Interfaces;
using BLL.Services;
using CashierApp.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

namespace CashierApp
{
    public partial class CashierMainWindow : Window
    {
        private readonly CashierMainViewModel _vm;
        private readonly ITicketService _ticketService = new TicketService();

        public CashierMainWindow(ILookupService lookupService = null)
        {
            InitializeComponent();

            _vm = new CashierMainViewModel(lookupService, _ticketService);
            DataContext = _vm;

            _vm.RequestShowMessage += Vm_RequestShowMessage;
            _vm.RequestOpenSellDialog += Vm_RequestOpenSellDialog;

            // Обработчик клика вне подсказок — если у вас имена контролов другие, замените lbSuggestions/tbCitySearch на реальные
            this.PreviewMouseDown += (s, e) =>
            {
                try
                {
                    var lb = this.FindName("lbSuggestions") as FrameworkElement;
                    var tb = this.FindName("tbCitySearch") as FrameworkElement;
                    if (!(IsMouseOverElement(lb) || IsMouseOverElement(tb)))
                        _vm.HideSuggestions();
                }
                catch { /* без фатальной ошибки */ }
            };

            _ = _vm.LoadCitiesAsync();
        }

        private void Vm_RequestOpenSellDialog(object sender, CashierMainViewModel.TripItem trip)
        {
            try
            {
                if (trip == null) return;

                var dlg = new Views.SellTicketDialog(_ticketService, GetCurrentUserId(), trip)
                {
                    Owner = this
                };

                if (dlg.ShowDialog() == true)
                {
                    if (trip.AvailableSeats >= 0)
                        trip.AvailableSeats = Math.Max(0, trip.AvailableSeats - dlg.SoldCount);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии диалога продажи: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Vm_RequestShowMessage(object sender, MessageRequestEventArgs e)
        {
            MessageBox.Show(e.Message, e.Caption, e.Button, e.Icon);
        }

        private bool IsMouseOverElement(System.Windows.FrameworkElement el)
        {
            if (el == null) return false;
            var pos = Mouse.GetPosition(el);
            return pos.X >= 0 && pos.Y >= 0 && pos.X <= el.ActualWidth && pos.Y <= el.ActualHeight;
        }

        private int GetCurrentUserId()
        {
            try
            {
                var user = App.Current.GetType().GetProperty("CurrentUser")?.GetValue(App.Current, null);
                if (user != null)
                {
                    var idProp = user.GetType().GetProperty("UserID") ?? user.GetType().GetProperty("Id") ?? user.GetType().GetProperty("ID");
                    var val = idProp?.GetValue(user);
                    if (val is int i) return i;
                    if (val != null && int.TryParse(val.ToString(), out int j)) return j;
                }
            }
            catch { }
            return 0;
        }
    }
}