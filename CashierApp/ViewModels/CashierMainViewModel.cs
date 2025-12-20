using BLL.Interfaces;
using BLL.Models;
using BLL.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace CashierApp.ViewModels
{
    public class MessageRequestEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string Caption { get; set; } = "";
        public MessageBoxButton Button { get; set; } = MessageBoxButton.OK;
        public MessageBoxImage Icon { get; set; } = MessageBoxImage.None;
    }

    public class CashierMainViewModel : INotifyPropertyChanged
    {
        private readonly ILookupService _lookupService;
        private readonly ITicketService _ticketService;
        private readonly DispatcherTimer _typingTimer;
        private List<CityDTO> _cities = new List<CityDTO>();
        private int? _selectedCityId = null;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<MessageRequestEventArgs> RequestShowMessage;
        public event EventHandler<TripItem> RequestOpenSellDialog;

        public CashierMainViewModel(ILookupService lookupService = null, ITicketService ticketService = null)
        {
            _lookupService = lookupService ?? new LookupService();
            _ticketService = ticketService ?? new TicketService();
            CashierName = GetCurrentUserName();
            SelectedDate = DateTime.Today;

            Suggestions = new ObservableCollection<CityDTO>();
            Trips = new ObservableCollection<TripItem>();

            _typingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _typingTimer.Tick += (s, e) =>
            {
                _typingTimer.Stop();
                ShowSuggestionsFor(SearchText);
            };

            KeyDownDownCommand = new RelayCommand(OnKeyDown_Down);
            KeyDownEnterCommand = new RelayCommand(OnKeyDown_Enter);

            OpenSellDialogCommand = new RelayCommand<TripItem>(OnOpenSellDialog, t => t != null && (t.AvailableSeats < 0 || t.AvailableSeats > 0));
        }

        #region Проперти
        private string _cashierName;
        public string CashierName
        {
            get => _cashierName;
            set { if (_cashierName != value) { _cashierName = value; OnPropertyChanged(nameof(CashierName)); } }
        }

        private DateTime? _selectedDate;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set { if (_selectedDate != value) { _selectedDate = value; OnPropertyChanged(nameof(SelectedDate)); DateChanged(); } }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));

                    // debounce
                    _typingTimer.Stop();
                    _typingTimer.Start();
                }
            }
        }

        public ObservableCollection<CityDTO> Suggestions { get; }
        private bool _areSuggestionsVisible = false;
        public bool AreSuggestionsVisible
        {
            get => _areSuggestionsVisible;
            private set { if (_areSuggestionsVisible != value) { _areSuggestionsVisible = value; OnPropertyChanged(nameof(AreSuggestionsVisible)); } }
        }

        private CityDTO _selectedSuggestion;
        public CityDTO SelectedSuggestion
        {
            get => _selectedSuggestion;
            set
            {
                if (_selectedSuggestion != value)
                {
                    _selectedSuggestion = value;
                    OnPropertyChanged(nameof(SelectedSuggestion));
                    if (_selectedSuggestion != null)
                    {
                        // Запускаем безопасную асинхронную последовательность выбора города
                        StartSelectCityFlow(_selectedSuggestion.CityID, _selectedSuggestion.CityName);
                    }
                }
            }
        }

        public ObservableCollection<TripItem> Trips { get; }
        #endregion

        #region Команды
        public ICommand KeyDownDownCommand { get; }
        public ICommand KeyDownEnterCommand { get; }
        public ICommand OpenSellDialogCommand { get; }

        private void OnKeyDown_Down() { /* noop */ }

        private void OnKeyDown_Enter()
        {
            var txt = SearchText?.Trim();
            if (!string.IsNullOrEmpty(txt))
            {
                var exact = _cities.FirstOrDefault(c => string.Equals(c.CityName, txt, StringComparison.CurrentCultureIgnoreCase));
                if (exact != null)
                {
                    StartSelectCityFlow(exact.CityID, exact.CityName);
                    return;
                }
            }

            if (Suggestions.Count > 0)
            {
                var first = Suggestions.FirstOrDefault();
                if (first != null)
                    StartSelectCityFlow(first.CityID, first.CityName);
            }
        }

        private void OnOpenSellDialog(TripItem trip)
        {
            if (trip == null) return;
            RequestOpenSellDialog?.Invoke(this, trip);
        }
        #endregion

        #region Города / рейсы
        public async Task LoadCitiesAsync()
        {
            try
            {
                var list = await _lookupService.GetCitiesAsync();
                _cities = list?.ToList() ?? new List<CityDTO>();
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs
                {
                    Message = "Ошибка при загрузке городов: " + ex.Message,
                    Caption = "Ошибка",
                    Button = MessageBoxButton.OK,
                    Icon = MessageBoxImage.Error
                });
            }
        }

        private void ShowSuggestionsFor(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                HideSuggestions();
                return;
            }

            var q = text.Trim().ToLowerInvariant();
            var matches = _cities
                .Where(c => !string.IsNullOrEmpty(c.CityName) && c.CityName.ToLower().Contains(q))
                .Take(50)
                .ToList();

            if (!matches.Any())
            {
                HideSuggestions();
                return;
            }

            Suggestions.Clear();
            foreach (var m in matches) Suggestions.Add(m);

            AreSuggestionsVisible = true;
        }

        public void HideSuggestions()
        {
            AreSuggestionsVisible = false;
            Suggestions.Clear();
            SelectedSuggestion = null;
        }

        private void StartSelectCityFlow(int cityId, string cityName)
        {
            try
            {
                _typingTimer.Stop();

                _selectedCityId = cityId;
                // Устанавливаем текст поиска сразу (UI)
                SearchText = cityName;

                // скрываем подсказки (это безопасно — SelectedSuggestion станет null но не вызовет повторного SelectCity)
                HideSuggestions();

                // Асинхронно загрузим рейсы в "безопасной" обёртке, чтобы любые исключения были обработаны
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await LoadTripsForSelectedAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Пропускаем обратно в UI поток показ сообщения
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            RequestShowMessage?.Invoke(this, new MessageRequestEventArgs
                            {
                                Message = "Ошибка при загрузке рейсов: " + ex.Message,
                                Caption = "Ошибка",
                                Button = MessageBoxButton.OK,
                                Icon = MessageBoxImage.Error
                            });
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs
                {
                    Message = "Ошибка выбора города: " + ex.Message,
                    Caption = "Ошибка",
                    Button = MessageBoxButton.OK,
                    Icon = MessageBoxImage.Error
                });
            }
        }

        private void DateChanged()
        {
            if (_selectedCityId.HasValue)
                StartSelectCityFlow(_selectedCityId.Value, SearchText);
        }

        private async Task LoadTripsForSelectedAsync()
        {
            if (!_selectedCityId.HasValue) return;
            var cityId = _selectedCityId.Value;
            var date = SelectedDate ?? DateTime.Today;

            try
            {
                var tripsDto = await _lookupService.GetTripsByArrivalCityAndDateAsync(cityId, date).ConfigureAwait(false);

                // Обновляем коллекцию в UI-потоке
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Trips.Clear();
                    var items = tripsDto.Select(x => new TripItem
                    {
                        TripID = x.TripID,
                        RouteTitle = $"Иваново - {x.ArrivalName}",
                        ExtraInfo = $"Отправление - {x.FormattedDeparture}",
                        Price = x.Price,
                        PriceText = $"{(x.Price):F2} руб.",
                        DepartureTime = x.DepartureDateTime.ToString("HH:mm"),
                        AvailableSeats = -1 // пока не загружено
                    });

                    foreach (var it in items)
                    {
                        Trips.Add(it);
                        // асинхронно подтянем свободные места, но безопасно (исключения ловим внутри)
                        _ = LoadAvailableSeatsAsync(it);
                    }
                });
            }
            catch (Exception ex)
            {
                // сюда попадает исключение только если не перехвачено выше
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RequestShowMessage?.Invoke(this, new MessageRequestEventArgs
                    {
                        Message = "Ошибка при загрузке рейсов: " + ex.Message,
                        Caption = "Ошибка",
                        Button = MessageBoxButton.OK,
                        Icon = MessageBoxImage.Error
                    });
                });
            }
        }

        private async Task LoadAvailableSeatsAsync(TripItem trip)
        {
            try
            {
                var count = await _ticketService.GetAvailableSeatsAsync(trip.TripID).ConfigureAwait(false);
                Application.Current.Dispatcher.Invoke(() => trip.AvailableSeats = Math.Max(0, count));
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(() => trip.AvailableSeats = -1);
            }
        }
        #endregion

        private string GetCurrentUserName()
        {
            try
            {
                var user = App.Current.GetType().GetProperty("CurrentUser")?.GetValue(App.Current, null);
                if (user != null)
                {
                    var nameProp = user.GetType().GetProperty("FullName") ?? user.GetType().GetProperty("Name") ?? user.GetType().GetProperty("Login");
                    return nameProp?.GetValue(user)?.ToString() ?? "";
                }
            }
            catch { }
            return "";
        }

        #region Вспомогательные классы и команды
        public class TripItem : INotifyPropertyChanged
        {
            public int TripID { get; set; }
            public string RouteTitle { get; set; }
            public string ExtraInfo { get; set; }
            public double Price { get; set; }
            public string PriceText { get; set; }
            public string DepartureTime { get; set; }

            private int _availableSeats = -1;
            public int AvailableSeats
            {
                get => _availableSeats;
                set
                {
                    if (_availableSeats != value)
                    {
                        _availableSeats = value;
                        OnPropertyChanged(nameof(AvailableSeats));
                        OnPropertyChanged(nameof(AvailableSeatsText));
                    }
                }
            }
            public string AvailableSeatsText => AvailableSeats < 0 ? "..." : AvailableSeats.ToString();

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;
            public RelayCommand(Action execute, Func<bool> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }
            public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
            public void Execute(object parameter) => _execute();
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }

        public class RelayCommand<T> : ICommand
        {
            private readonly Action<T> _execute;
            private readonly Func<T, bool> _canExecute;
            public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }
            public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;
            public void Execute(object parameter) => _execute((T)parameter);
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}