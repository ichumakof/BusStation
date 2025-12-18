using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BLL.Interfaces;
using BLL.Models;
using BLL.Services;

namespace CashierApp.ViewModels
{
    public class SellTicketDialogViewModel : INotifyPropertyChanged
    {
        private readonly ITicketService _ticketService;
        private readonly int _currentUserId;
        private readonly TicketPdfPrinter _printer;

        // Папка, куда сохранять билеты
        private readonly string _ticketsFolder = @"C:\Users\ichum\Desktop\игэ(у)\5 семестр\Конструирование ПО\БИЛЕТЫ";

        public SellTicketDialogViewModel(ITicketService ticketService, int currentUserId, CashierMainViewModel.TripItem trip)
        {
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
            _currentUserId = currentUserId;
            Trip = trip ?? throw new ArgumentNullException(nameof(trip));

            Quantity = 1;
            SelectedPaymentType = PaymentTypes[0];

            ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, () => !IsBusy);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));

            _printer = new TicketPdfPrinter();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnProp(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public event EventHandler<MessageBoxEventArgs> RequestShowMessage;
        public event EventHandler<bool> RequestClose;

        public CashierMainViewModel.TripItem Trip { get; }

        private int _availableSeats;
        public int AvailableSeats { get => _availableSeats; set { _availableSeats = value; OnProp(nameof(AvailableSeats)); OnProp(nameof(TotalPrice)); } }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                var v = Math.Max(1, value);
                if (AvailableSeats > 0) v = Math.Min(v, AvailableSeats);
                if (_quantity != v) { _quantity = v; OnProp(nameof(Quantity)); OnProp(nameof(TotalPrice)); }
            }
        }

        public string[] PaymentTypes { get; } = new[] { "Наличные", "Карта" };
        private string _selectedPaymentType;
        public string SelectedPaymentType { get => _selectedPaymentType; set { _selectedPaymentType = value; OnProp(nameof(SelectedPaymentType)); } }

        public double PricePerTicket => Trip.Price;
        public double TotalPrice => Math.Round(PricePerTicket * Quantity, 2);

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnProp(nameof(IsBusy)); } }

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                AvailableSeats = await _ticketService.GetAvailableSeatsAsync(Trip.TripID);
                if (AvailableSeats <= 0)
                {
                    RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Нет свободных мест.", "Информация", MessageBoxImage.Information));
                    RequestClose?.Invoke(this, false);
                    return;
                }
                if (Quantity > AvailableSeats) Quantity = Math.Max(1, AvailableSeats);
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Ошибка загрузки мест: " + ex.Message, "Ошибка", MessageBoxImage.Error));
                RequestClose?.Invoke(this, false);
            }
            finally { IsBusy = false; }
        }

        private async Task ConfirmAsync()
        {
            if (Quantity < 1) { RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Укажите количество билетов.", "Ошибка", MessageBoxImage.Warning)); return; }
            if (AvailableSeats > 0 && Quantity > AvailableSeats) { RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Недостаточно мест.", "Ошибка", MessageBoxImage.Warning)); return; }
            if (string.IsNullOrWhiteSpace(SelectedPaymentType)) { RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Выберите способ оплаты.", "Ошибка", MessageBoxImage.Warning)); return; }

            try
            {
                IsBusy = true;

                // 1) Продажа — теперь ожидаем List<int> с id созданных билетов
                List<int> createdIds = await _ticketService.SellTicketsAsync(new SellTicketsRequest
                {
                    TripID = Trip.TripID,
                    Quantity = Quantity,
                    SoldByUserID = _currentUserId,
                    TypeOfPayment = SelectedPaymentType
                });

                if (createdIds == null || createdIds.Count == 0)
                {
                    RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Продажа не выполнена.", "Внимание", MessageBoxImage.Warning));
                    return;
                }

                // Обновляем AvailableSeats как число проданных мест
                AvailableSeats -= createdIds.Count;

                // 2) Подготовка данных для печати
                List<TicketPrintDTO> ticketsForPrint = null;
                try
                {
                    ticketsForPrint = await _ticketService.GetTicketsForPrintingAsync(createdIds);
                }
                catch (Exception exPrintData)
                {
                    // Если не удалось получить DTO для печати — всё равно закрываем диалог, но сообщаем об ошибке
                    RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Билеты проданы, но не удалось получить данные для печати: " + exPrintData.Message, "Ошибка", MessageBoxImage.Warning));
                    RequestClose?.Invoke(this, true);
                    return;
                }

                // 3) Сохранение PDF и открытие папки
                try
                {
                    var pdfPath = _printer.SaveTicketsToPdf(ticketsForPrint, _ticketsFolder);
                    RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Билеты созданы и сохранены:\n" + pdfPath, "Успех", MessageBoxImage.Information));
                }
                catch (Exception exPrint)
                {
                    RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Билеты проданы, но ошибка при создании PDF: " + exPrint.Message, "Ошибка", MessageBoxImage.Warning));
                }

                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageBoxEventArgs("Ошибка продажи: " + ex.Message, "Ошибка", MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public class MessageBoxEventArgs : EventArgs
        {
            public MessageBoxEventArgs(string message, string caption, MessageBoxImage icon = MessageBoxImage.None)
            {
                Message = message; Caption = caption; Icon = icon;
            }
            public string Message { get; }
            public string Caption { get; }
            public MessageBoxImage Icon { get; }
        }

        // Команды
        public class RelayCommand : ICommand
        {
            private readonly Action _act;
            private readonly Func<bool> _can;
            public RelayCommand(Action act, Func<bool> can = null) { _act = act; _can = can; }
            public bool CanExecute(object p) => _can?.Invoke() ?? true;
            public void Execute(object p) => _act();
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }
        public class AsyncRelayCommand : ICommand
        {
            private readonly Func<Task> _act;
            private readonly Func<bool> _can;
            private bool _run;
            public AsyncRelayCommand(Func<Task> act, Func<bool> can = null) { _act = act; _can = can; }
            public bool CanExecute(object p) => !_run && (_can?.Invoke() ?? true);
            public async void Execute(object p)
            {
                _run = true; CommandManager.InvalidateRequerySuggested();
                try { await _act(); }
                finally { _run = false; CommandManager.InvalidateRequerySuggested(); }
            }
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }
    }
}