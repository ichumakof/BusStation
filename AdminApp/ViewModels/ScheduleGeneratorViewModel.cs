using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BLL.Interfaces;
using BLL.Models;

namespace AdminApp.ViewModels
{
    public class ScheduleGeneratorViewModel : INotifyPropertyChanged
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleGeneratorViewModel(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));

            Routes = new ObservableCollection<RouteDTO>();
            Drivers = new ObservableCollection<object>();
            Buses = new ObservableCollection<object>();

            HourList = new ObservableCollection<int>(Enumerable.Range(0, 24));
            MinuteList = new ObservableCollection<int>(Enumerable.Range(0, 60).Where(m => m % 5 == 0));

            SelectedHour = 8;
            SelectedMinute = 0;
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(30);
            PriceText = "0,00";
            InfoText = "Выберите параметры для генерации";

            GenerateCommand = new AsyncRelayCommand(GenerateAsync, () => !IsBusy);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
        }

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<MessageRequestEventArgs> RequestShowMessage;
        public event EventHandler<bool> RequestClose; // true = success/ok, false = cancelled
        #endregion

        #region Collections / Bindable props

        public ObservableCollection<RouteDTO> Routes { get; }
        public ObservableCollection<object> Drivers { get; }
        public ObservableCollection<object> Buses { get; }

        public ObservableCollection<int> HourList { get; }
        public ObservableCollection<int> MinuteList { get; }

        private RouteDTO _selectedRoute;
        public RouteDTO SelectedRoute { get => _selectedRoute; set { if (_selectedRoute != value) { _selectedRoute = value; OnProp(nameof(SelectedRoute)); } } }

        private int? _selectedDriverId;
        public int? SelectedDriverId { get => _selectedDriverId; set { if (_selectedDriverId != value) { _selectedDriverId = value; OnProp(nameof(SelectedDriverId)); } } }

        private int? _selectedBusId;
        public int? SelectedBusId { get => _selectedBusId; set { if (_selectedBusId != value) { _selectedBusId = value; OnProp(nameof(SelectedBusId)); } } }

        private int _selectedHour;
        public int SelectedHour { get => _selectedHour; set { if (_selectedHour != value) { _selectedHour = value; OnProp(nameof(SelectedHour)); } } }

        private int _selectedMinute;
        public int SelectedMinute { get => _selectedMinute; set { if (_selectedMinute != value) { _selectedMinute = value; OnProp(nameof(SelectedMinute)); } } }

        private DateTime _startDate;
        public DateTime StartDate { get => _startDate; set { if (_startDate != value) { _startDate = value; OnProp(nameof(StartDate)); } } }

        private DateTime _endDate;
        public DateTime EndDate { get => _endDate; set { if (_endDate != value) { _endDate = value; OnProp(nameof(EndDate)); } } }

        // Days of week bound from UI (your XAML already had checkboxes for these)
        private bool _monday = true; public bool Monday { get => _monday; set { if (_monday != value) { _monday = value; OnProp(nameof(Monday)); } } }
        private bool _tuesday = true; public bool Tuesday { get => _tuesday; set { if (_tuesday != value) { _tuesday = value; OnProp(nameof(Tuesday)); } } }
        private bool _wednesday = true; public bool Wednesday { get => _wednesday; set { if (_wednesday != value) { _wednesday = value; OnProp(nameof(Wednesday)); } } }
        private bool _thursday = true; public bool Thursday { get => _thursday; set { if (_thursday != value) { _thursday = value; OnProp(nameof(Thursday)); } } }
        private bool _friday = true; public bool Friday { get => _friday; set { if (_friday != value) { _friday = value; OnProp(nameof(Friday)); } } }
        private bool _saturday = false; public bool Saturday { get => _saturday; set { if (_saturday != value) { _saturday = value; OnProp(nameof(Saturday)); } } }
        private bool _sunday = false; public bool Sunday { get => _sunday; set { if (_sunday != value) { _sunday = value; OnProp(nameof(Sunday)); } } }

        private string _priceText;
        public string PriceText { get => _priceText; set { if (_priceText != value) { _priceText = value; OnProp(nameof(PriceText)); } } }

        private string _infoText;
        public string InfoText { get => _infoText; set { if (_infoText != value) { _infoText = value; OnProp(nameof(InfoText)); } } }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; private set { if (_isBusy != value) { _isBusy = value; OnProp(nameof(IsBusy)); } } }

        #endregion

        #region Commands
        public ICommand GenerateCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        #region Init / Load

        // Загружает справочники в коллекции (вызывается из View)
        public async Task InitializeAsync()
        {
            await LoadRoutesAsync();
            await LoadDriversAsync();
            await LoadBusesAsync();
        }

        private async Task LoadRoutesAsync()
        {
            try
            {
                var list = await _scheduleService.GetRoutesAsync();
                App.Current.Dispatcher.Invoke(() =>
                {
                    Routes.Clear();
                    if (list != null)
                        foreach (var r in list) Routes.Add(r);
                    if (Routes.Count > 0 && SelectedRoute == null) SelectedRoute = Routes[0];
                });
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs { Message = "Ошибка загрузки маршрутов: " + ex.Message, Caption = "Ошибка", Icon = MessageBoxImage.Error });
            }
        }

        private async Task LoadDriversAsync()
        {
            try
            {
                var list = await _scheduleService.GetDriversAsync();
                App.Current.Dispatcher.Invoke(() =>
                {
                    Drivers.Clear();
                    if (list != null)
                        foreach (var d in list) Drivers.Add(d);
                    // Попытаться заполнить SelectedDriverId если не задано
                    if (Drivers.Count > 0 && !SelectedDriverId.HasValue)
                    {
                        var id = TryGetIdFromObject(Drivers[0]);
                        SelectedDriverId = id;
                    }
                });
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs { Message = "Ошибка загрузки водителей: " + ex.Message, Caption = "Ошибка", Icon = MessageBoxImage.Error });
            }
        }

        private async Task LoadBusesAsync()
        {
            try
            {
                var list = await _scheduleService.GetBusesAsync();
                App.Current.Dispatcher.Invoke(() =>
                {
                    Buses.Clear();
                    if (list != null)
                        foreach (var b in list) Buses.Add(b);
                    if (Buses.Count > 0 && !SelectedBusId.HasValue)
                    {
                        var id = TryGetIdFromObject(Buses[0]);
                        SelectedBusId = id;
                    }
                });
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs { Message = "Ошибка загрузки автобусов: " + ex.Message, Caption = "Ошибка", Icon = MessageBoxImage.Error });
            }
        }

        // пытается взять публичное свойство Id/ID или возвращает null
        private int? TryGetIdFromObject(object o)
        {
            if (o == null) return null;
            var t = o.GetType();
            var p = t.GetProperty("Id") ?? t.GetProperty("ID") ?? t.GetProperty("Id", System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (p == null) return null;
            try
            {
                var v = p.GetValue(o);
                if (v == null) return null;
                return Convert.ToInt32(v);
            }
            catch { return null; }
        }

        #endregion

        #region Generate / Validate

        private string ValidateRequest(ScheduleGenerationRequest request)
        {
            if (request == null) return "Неверные параметры запроса.";

            if (request.RouteID <= 0)
                return "Выберите маршрут.";

            if (request.StartDate == default || request.EndDate == default)
                return "Укажите период дат.";

            if (request.StartDate.Date > request.EndDate.Date)
                return "Дата начала больше даты окончания.";

            if (request.DaysOfWeek == null || request.DaysOfWeek.Count == 0)
                return "Выберите хотя бы один день недели.";

            if (!request.DriverID.HasValue)
                return "Выберите водителя.";

            if (!request.BusID.HasValue)
                return "Выберите автобус.";

            if (request.Price < 0)
                return "Укажите корректную цену (>=0).";

            return null;
        }

        private async Task GenerateAsync()
        {
            // собираем request из текущих свойств VM
            var request = new ScheduleGenerationRequest
            {
                RouteID = SelectedRoute?.RouteID ?? 0,
                StartDate = StartDate,
                EndDate = EndDate,
                DaysOfWeek = GetSelectedDays().ToList(),
                DepartureTimeOfDay = new TimeSpan(SelectedHour, SelectedMinute, 0),
                SkipExisting = true,
                DriverID = SelectedDriverId,
                BusID = SelectedBusId,
                Price = ParsePrice(PriceText)
            };

            var validationError = ValidateRequest(request);
            if (validationError != null)
            {
                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs { Message = validationError, Caption = "Ошибка", Icon = MessageBoxImage.Warning });
                return;
            }

            IsBusy = true;
            InfoText = "Генерация расписания...";

            try
            {
                var created = await _scheduleService.GenerateScheduleAsync(request);

                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs
                {
                    Message = $"Расписание сгенерировано.\nСоздано рейсов: {created}\nПериод: {StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}",
                    Caption = "Успех",
                    Icon = MessageBoxImage.Information
                });

                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs { Message = "Ошибка генерации: " + ex.Message, Caption = "Ошибка", Icon = MessageBoxImage.Error });
            }
            finally
            {
                IsBusy = false;
                InfoText = "Готово";
            }
        }

        private double ParsePrice(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0.0;
            if (double.TryParse(s.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                return v;
            return -1;
        }

        private System.Collections.Generic.IEnumerable<DayOfWeek> GetSelectedDays()
        {
            if (Monday) yield return DayOfWeek.Monday;
            if (Tuesday) yield return DayOfWeek.Tuesday;
            if (Wednesday) yield return DayOfWeek.Wednesday;
            if (Thursday) yield return DayOfWeek.Thursday;
            if (Friday) yield return DayOfWeek.Friday;
            if (Saturday) yield return DayOfWeek.Saturday;
            if (Sunday) yield return DayOfWeek.Sunday;
        }

        #endregion

        #region Helpers / Commands impl

        protected void OnProp(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;
            public RelayCommand(Action execute, Func<bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }
            public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
            public void Execute(object parameter) => _execute();
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }

        public class AsyncRelayCommand : ICommand
        {
            private readonly Func<Task> _execute;
            private readonly Func<bool> _canExecute;
            private bool _isRunning;
            public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }
            public bool CanExecute(object parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);
            public async void Execute(object parameter)
            {
                _isRunning = true;
                CommandManager.InvalidateRequerySuggested();
                try { await _execute(); }
                finally { _isRunning = false; CommandManager.InvalidateRequerySuggested(); }
            }
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }

        #endregion
    }
}