using System;
using System.Windows;
using CashierApp.ViewModels;
using BLL.Interfaces;
using CashierApp.Commons;

namespace CashierApp.Views
{
    public partial class SellTicketDialog : Window
    {
        private readonly SellTicketDialogViewModel _vm;

        public int SoldCount { get; private set; } = 0;

        public SellTicketDialog(ITicketService ticketService, int currentUserId, Commons.TripItem trip)
        {
            InitializeComponent();

            _vm = new SellTicketDialogViewModel(ticketService, currentUserId, trip);
            DataContext = _vm;

            Loaded += async (s, e) => await _vm.InitializeAsync();

            _vm.RequestShowMessage += (s, e) => MessageBox.Show(this, e.Message, e.Caption, MessageBoxButton.OK, e.Icon);
            _vm.RequestClose += (s, ok) =>
            {
                if (ok)
                {
                    SoldCount = _vm.Quantity;
                    DialogResult = true;
                }
                else DialogResult = false;
                Close();
            };
        }

        private void Increment_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.AvailableSeats <= 0) return;
            _vm.Quantity = Math.Min(_vm.AvailableSeats, _vm.Quantity + 1);
        }

        private void Decrement_Click(object sender, RoutedEventArgs e)
        {
            _vm.Quantity = Math.Max(1, _vm.Quantity - 1);
        }
    }
}