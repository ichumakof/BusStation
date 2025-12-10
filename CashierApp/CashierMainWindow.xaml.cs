using BLL.Interfaces;
using BLL.Models;
using BLL.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CashierApp
{
    public partial class CashierMainWindow : Window
    {
        private readonly ILookupService _lookupService;
        private List<CityDTO> _cities = new List<CityDTO>();
        private int? _selectedCityId = null;
        private readonly DispatcherTimer _typingTimer;

        public CashierMainWindow(ILookupService lookupService = null)
        {
            InitializeComponent();

            // если не передали сервис, попытаемся создать стандартную реализацию (без DI)
            _lookupService = lookupService ?? new BLL.Services.LookupService();

            // Приветствие (берём App.CurrentUser, если есть)
            try
            {
                var userProp = App.Current.GetType().GetProperty("CurrentUser");
                var user = userProp?.GetValue(App.Current, null);
                if (user != null)
                {
                    var nameProp = user.GetType().GetProperty("FullName") ?? user.GetType().GetProperty("Name") ?? user.GetType().GetProperty("Login");
                    tbCashierName.Text = nameProp?.GetValue(user)?.ToString() ?? "";
                }
            }
            catch { tbCashierName.Text = ""; }

            dpDate.SelectedDate = DateTime.Today;
            dpDate.SelectedDateChanged += DpDate_SelectedDateChanged;

            _typingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _typingTimer.Tick += TypingTimer_Tick;

            _ = LoadCitiesAsync();

            this.PreviewMouseDown += (s, e) =>
            {
                if (!IsMouseOverElement(lbSuggestions) && !IsMouseOverElement(tbCitySearch))
                    HideSuggestions();
            };
        }

        private bool IsMouseOverElement(FrameworkElement el)
        {
            if (el == null) return false;
            var pos = Mouse.GetPosition(el);
            return pos.X >= 0 && pos.Y >= 0 && pos.X <= el.ActualWidth && pos.Y <= el.ActualHeight;
        }

        private async Task LoadCitiesAsync()
        {
            try
            {
                var list = await _lookupService.GetCitiesAsync();
                _cities = list?.ToList() ?? new List<CityDTO>();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке городов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void tbCitySearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _typingTimer.Stop();
            _typingTimer.Start();
        }

        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            _typingTimer.Stop();
            ShowSuggestionsFor(tbCitySearch.Text);
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

            lbSuggestions.ItemsSource = matches;
            lbSuggestions.DisplayMemberPath = "CityName";
            borderSuggestions.Visibility = Visibility.Visible;
        }

        private void HideSuggestions()
        {
            borderSuggestions.Visibility = Visibility.Collapsed;
            lbSuggestions.ItemsSource = null;
        }

        private void lbSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbSuggestions.SelectedItem is CityDTO si)
                SelectCity(si.CityID, si.CityName);
        }

        private void lbSuggestions_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (lbSuggestions.SelectedItem is CityDTO si)
                SelectCity(si.CityID, si.CityName);
        }

        private void SelectCity(int cityId, string cityName)
        {
            _selectedCityId = cityId;
            tbCitySearch.Text = cityName;
            HideSuggestions();
            _ = LoadTripsForSelectedAsync();
        }

        private void tbCitySearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (borderSuggestions.Visibility == Visibility.Visible && lbSuggestions.Items.Count > 0)
                {
                    lbSuggestions.Focus();
                    lbSuggestions.SelectedIndex = 0;
                    var item = (ListBoxItem)lbSuggestions.ItemContainerGenerator.ContainerFromIndex(0);
                    item?.Focus();
                }
            }
            else if (e.Key == Key.Enter)
            {
                var txt = tbCitySearch.Text?.Trim();
                if (!string.IsNullOrEmpty(txt))
                {
                    var exact = _cities.FirstOrDefault(c => string.Equals(c.CityName, txt, StringComparison.CurrentCultureIgnoreCase));
                    if (exact != null)
                    {
                        SelectCity(exact.CityID, exact.CityName);
                        return;
                    }
                }
                if (lbSuggestions.Items.Count > 0 && lbSuggestions.SelectedItem is CityDTO si)
                    SelectCity(si.CityID, si.CityName);
            }
        }

        private void DpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedCityId.HasValue)
                _ = LoadTripsForSelectedAsync();
        }

        private async Task LoadTripsForSelectedAsync()
        {
            if (!_selectedCityId.HasValue) return;
            var cityId = _selectedCityId.Value;
            var date = dpDate.SelectedDate ?? DateTime.Today;

            try
            {
                var tripsDto = await _lookupService.GetTripsByArrivalCityAndDateAsync(cityId, date);

                var items = tripsDto.Select(x => new TripItem
                {
                    TripID = x.TripID,
                    RouteTitle = $"Иваново - {x.ArrivalName}",
                    ExtraInfo = $"Отправление - {x.FormattedDeparture}",
                    Price = x.Price,
                    PriceText = $"{(x.Price):F2} руб.",
                    DepartureTime = x.DepartureDateTime.ToString("HH:mm")
                }).ToList();

                icTrips.ItemsSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке рейсов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class TripItem
        {
            public int TripID { get; set; }
            public string RouteTitle { get; set; }
            public string ExtraInfo { get; set; }
            public double Price { get; set; }
            public string PriceText { get; set; }
            public string DepartureTime { get; set; }
        }
    }
}