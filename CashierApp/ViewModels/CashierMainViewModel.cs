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
    // Аргументы для показа MessageBox из VM (View подпишется и отобразит)
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
        private readonly DispatcherTimer _typingTimer;
        private List<CityDTO> _cities = new List<CityDTO>();
        private int? _selectedCityId = null;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<MessageRequestEventArgs> RequestShowMessage;

        public CashierMainViewModel(ILookupService lookupService = null)
        {
            _lookupService = lookupService ?? new LookupService();

            CashierName = GetCurrentUserName();

            SelectedDate = DateTime.Today;

            Suggestions = new ObservableCollection<CityDTO>();
            Trips = new ObservableCollection<TripItem>();

            // Таймер для "debounce" ввода
            _typingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _typingTimer.Tick += (s, e) =>
            {
                _typingTimer.Stop();
                ShowSuggestionsFor(SearchText);
            };

            KeyDownDownCommand = new RelayCommand(OnKeyDown_Down);
            KeyDownEnterCommand = new RelayCommand(OnKeyDown_Enter);
        }

        #region Проперти для биндинга

        private string _cashierName;
        public string CashierName { get => _cashierName; set { if (_cashierName != value) { _cashierName = value; OnPropertyChanged(nameof(CashierName)); } } }

        private DateTime? _selectedDate;
        public DateTime? SelectedDate { get => _selectedDate; set { if (_selectedDate != value) { _selectedDate = value; OnPropertyChanged(nameof(SelectedDate)); DateChanged(); } } }

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

                    // (debounce) перезапускаем таймер
                    _typingTimer.Stop();
                    _typingTimer.Start();
                }
            }
        }

        public ObservableCollection<CityDTO> Suggestions { get; }
        private bool _areSuggestionsVisible = false;
        public bool AreSuggestionsVisible { get => _areSuggestionsVisible; private set { if (_areSuggestionsVisible != value) { _areSuggestionsVisible = value; OnPropertyChanged(nameof(AreSuggestionsVisible)); } } }

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
                        SelectCity(_selectedSuggestion.CityID, _selectedSuggestion.CityName);
                    }
                }
            }
        }

        public ObservableCollection<TripItem> Trips { get; }

        #endregion

        #region Команды для клавиш
        public ICommand KeyDownDownCommand { get; }
        public ICommand KeyDownEnterCommand { get; }

        private void OnKeyDown_Down()
        {
            // Сдвиг фокуса на список подсказок — View может это обработать (в нашем случае View не делает дополнительного фокуса),
            // но можем открыть подсказки, если они есть.
            if (AreSuggestionsVisible && Suggestions.Count > 0)
            {
                // В MVVM нельзя явно фокусировать контролы; View может сама перевести фокус при необходимости.
                // Здесь просто оставляем подсказки видимыми.
            }
        }

        private void OnKeyDown_Enter()
        {
            // Попытка выбрать точное совпадение по тексту
            var txt = SearchText?.Trim();
            if (!string.IsNullOrEmpty(txt))
            {
                var exact = _cities.FirstOrDefault(c => string.Equals(c.CityName, txt, StringComparison.CurrentCultureIgnoreCase));
                if (exact != null)
                {
                    SelectCity(exact.CityID, exact.CityName);
                    return;
                }
            }

            if (Suggestions.Count > 0)
            {
                var first = Suggestions.FirstOrDefault();
                if (first != null)
                    SelectCity(first.CityID, first.CityName);
            }
        }
        #endregion

        #region Загрузка городов / показ подсказок / выбор города / загрузка рейсов

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

        private void SelectCity(int cityId, string cityName)
        {
            _selectedCityId = cityId;
            SearchText = cityName;
            HideSuggestions();
            _ = LoadTripsForSelectedAsync();
        }

        private void DateChanged()
        {
            if (_selectedCityId.HasValue)
                _ = LoadTripsForSelectedAsync();
        }

        private async Task LoadTripsForSelectedAsync()
        {
            if (!_selectedCityId.HasValue) return;
            var cityId = _selectedCityId.Value;
            var date = SelectedDate ?? DateTime.Today;

            try
            {
                var tripsDto = await _lookupService.GetTripsByArrivalCityAndDateAsync(cityId, date);
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
                        DepartureTime = x.DepartureDateTime.ToString("HH:mm")
                    });

                    foreach (var it in items) Trips.Add(it);
                });
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs
                {
                    Message = "Ошибка при загрузке рейсов: " + ex.Message,
                    Caption = "Ошибка",
                    Button = MessageBoxButton.OK,
                    Icon = MessageBoxImage.Error
                });
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

        public class TripItem
        {
            public int TripID { get; set; }
            public string RouteTitle { get; set; }
            public string ExtraInfo { get; set; }
            public double Price { get; set; }
            public string PriceText { get; set; }
            public string DepartureTime { get; set; }
        }

        // Простейшая реализация RelayCommand
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

        #endregion

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}