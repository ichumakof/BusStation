using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BLL.Interfaces;
using BLL.Models;

namespace AdminApp.Views
{
    public partial class ScheduleGeneratorView : Window
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleGeneratorView(IScheduleService scheduleService)
        {
            InitializeComponent();
            _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
            Loaded += ScheduleGeneratorView_Loaded;
        }

        private async void ScheduleGeneratorView_Loaded(object sender, RoutedEventArgs e)
        {
            dpStartDate.SelectedDate = DateTime.Today;
            dpEndDate.SelectedDate = DateTime.Today.AddDays(30);
            InitTimePickers();

            await LoadRoutesAsync();
            await LoadDriversAsync();
            await LoadBusesAsync();
        }

        private void InitTimePickers()
        {
            if (cbHour != null && cbHour.Items.Count == 0)
            {
                for (int h = 0; h <= 23; h++) cbHour.Items.Add(h);
                cbHour.SelectedItem = 8;
            }
            if (cbMinute != null && cbMinute.Items.Count == 0)
            {
                for (int m = 0; m < 60; m += 5) cbMinute.Items.Add(m);
                cbMinute.SelectedItem = 0;
            }
        }

        private TimeSpan GetSelectedTime()
        {
            int hour = (cbHour != null && cbHour.SelectedItem != null) ? Convert.ToInt32(cbHour.SelectedItem) : 8;
            int minute = (cbMinute != null && cbMinute.SelectedItem != null) ? Convert.ToInt32(cbMinute.SelectedItem) : 0;
            return new TimeSpan(hour, minute, 0);
        }

        private async Task LoadRoutesAsync()
        {
            try
            {
                var routes = await _scheduleService.GetRoutesAsync();
                cbRoutes.ItemsSource = routes;
                cbRoutes.DisplayMemberPath = "RouteInfo";
                cbRoutes.SelectedValuePath = "RouteID";
                if (routes != null && routes.Count > 0)
                    cbRoutes.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки маршрутов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDriversAsync()
        {
            try
            {
                var drivers = await _scheduleService.GetDriversAsync();
                cbDrivers.ItemsSource = drivers;
                cbDrivers.DisplayMemberPath = "Title";
                cbDrivers.SelectedValuePath = "Id";
                if (drivers != null && drivers.Count > 0)
                    cbDrivers.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки водителей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadBusesAsync()
        {
            try
            {
                var buses = await _scheduleService.GetBusesAsync();
                cbBuses.ItemsSource = buses;
                cbBuses.DisplayMemberPath = "Title";
                cbBuses.SelectedValuePath = "Id";
                if (buses != null && buses.Count > 0)
                    cbBuses.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки автобусов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            var selectedRoute = cbRoutes.SelectedItem as RouteDTO;
            var startDate = dpStartDate.SelectedDate.HasValue ? dpStartDate.SelectedDate.Value : DateTime.Today;
            var endDate = dpEndDate.SelectedDate.HasValue ? dpEndDate.SelectedDate.Value : DateTime.Today;
            var selectedDays = GetSelectedDays();
            var depTime = GetSelectedTime();

            pbProgress.Visibility = Visibility.Visible;
            btnGenerate.IsEnabled = false;
            tbInfo.Text = "Генерация расписания...";

            var priceValue = 0.0;
            double parsedPrice;
            if (!string.IsNullOrWhiteSpace(tbPrice.Text) && double.TryParse(tbPrice.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsedPrice))
                priceValue = parsedPrice;

            try
            {
                var request = new ScheduleGenerationRequest
                {
                    RouteID = selectedRoute != null ? selectedRoute.RouteID : 0,
                    StartDate = startDate,
                    EndDate = endDate,
                    DaysOfWeek = selectedDays,
                    DepartureTimeOfDay = depTime,
                    SkipExisting = true,
                    DriverID = cbDrivers.SelectedValue is int dv ? dv : (int?)null,
                    BusID = cbBuses.SelectedValue is int bv ? bv : (int?)null,
                    Price = priceValue
                };

                var created = await _scheduleService.GenerateScheduleAsync(request);

                MessageBox.Show(
                    $"Расписание сгенерировано.\nСоздано рейсов: {created}\nПериод: {startDate:dd.MM.yyyy} — {endDate:dd.MM.yyyy}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                pbProgress.Visibility = Visibility.Collapsed;
                btnGenerate.IsEnabled = true;
                tbInfo.Text = "Готово";
            }
        }

        private bool ValidateInput()
        {
            if (cbRoutes.SelectedItem == null)
            {
                MessageBox.Show("Выберите маршрут.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!dpStartDate.SelectedDate.HasValue || !dpEndDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите период дат.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (dpStartDate.SelectedDate.Value.Date > dpEndDate.SelectedDate.Value.Date)
            {
                MessageBox.Show("Дата начала больше даты окончания.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (GetSelectedDays().Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один день недели.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cbDrivers.SelectedItem == null)
            {
                MessageBox.Show("Выберите водителя.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cbBuses.SelectedItem == null)
            {
                MessageBox.Show("Выберите автобус.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // проверка tbPrice
            double tmpPrice;
            if (string.IsNullOrWhiteSpace(tbPrice.Text) || !double.TryParse(tbPrice.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out tmpPrice) || tmpPrice < 0)
            {
                MessageBox.Show("Укажите корректную цену (>=0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private IList<DayOfWeek> GetSelectedDays()
        {
            var days = new List<DayOfWeek>();
            foreach (var cb in FindLogicalChildren<CheckBox>(this))
            {
                if (cb.IsChecked != true) continue;
                var key = (cb.Tag != null ? cb.Tag.ToString() : null)
                          ?? (cb.Content != null ? cb.Content.ToString() : null)
                          ?? cb.Name;
                var mapped = MapDayOfWeek(key);
                if (mapped.HasValue && !days.Contains(mapped.Value))
                    days.Add(mapped.Value);
            }
            return days;
        }

        private static DayOfWeek? MapDayOfWeek(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            var s = key.Trim().ToLowerInvariant();

            if (s.Contains("mon")) return DayOfWeek.Monday;
            if (s.Contains("tue")) return DayOfWeek.Tuesday;
            if (s.Contains("wed")) return DayOfWeek.Wednesday;
            if (s.Contains("thu")) return DayOfWeek.Thursday;
            if (s.Contains("fri")) return DayOfWeek.Friday;
            if (s.Contains("sat")) return DayOfWeek.Saturday;
            if (s.Contains("sun")) return DayOfWeek.Sunday;

            if (s.Contains("пон") || s == "пн") return DayOfWeek.Monday;
            if (s.Contains("втор") || s == "вт") return DayOfWeek.Tuesday;
            if (s.Contains("сред") || s == "ср") return DayOfWeek.Wednesday;
            if (s.Contains("чет") || s == "чт") return DayOfWeek.Thursday;
            if (s.Contains("пят") || s == "пт") return DayOfWeek.Friday;
            if (s.Contains("суб") || s == "сб") return DayOfWeek.Saturday;
            if (s.Contains("воск") || s == "вс") return DayOfWeek.Sunday;

            int n;
            if (int.TryParse(s, out n) && n >= 0 && n <= 6) return (DayOfWeek)n;
            return null;
        }

        private static IEnumerable<T> FindLogicalChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            foreach (var rawChild in LogicalTreeHelper.GetChildren(depObj))
            {
                var depChild = rawChild as DependencyObject;
                if (depChild == null) continue;
                var t = depChild as T;
                if (t != null) yield return t;
                foreach (var childOfChild in FindLogicalChildren<T>(depChild))
                    yield return childOfChild;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (btnGenerate != null) btnGenerate.IsEnabled = true;
                if (pbProgress != null) pbProgress.Visibility = Visibility.Collapsed;
                if (tbInfo != null) tbInfo.Text = "Отменено";
            }
            finally
            {
                this.Close();
            }
        }
    }
}
