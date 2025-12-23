using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using AdminApp.ViewModels;
using BLL.Interfaces;
using BLL.Models;

namespace AdminApp.Views
{
    public partial class ScheduleGeneratorView : Window
    {
        private readonly IScheduleService _scheduleService;
        private readonly ScheduleGeneratorViewModel _vm;

        public ScheduleGeneratorView(IScheduleService scheduleService)
        {
            InitializeComponent();

            _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
            _vm = new ScheduleGeneratorViewModel(_scheduleService);

            DataContext = _vm;

            Loaded += ScheduleGeneratorView_Loaded;

            // подписываемся на события VM для показа сообщений и закрытия окна
            _vm.RequestShowMessage += Vm_RequestShowMessage;
            _vm.RequestClose += Vm_RequestClose;
        }

        private void Vm_RequestClose(object sender, bool ok)
        {
            try
            {
                if (ok) this.DialogResult = true;
                else this.DialogResult = false;
            }
            catch { /* DialogResult может быть недоступен если окно non-modal */ }
            this.Close();
        }

        private void Vm_RequestShowMessage(object sender, Commons.MessageRequestEventArgs e)
        {
            MessageBox.Show(e.Message, e.Caption, e.Button, e.Icon);
        }

        private async void ScheduleGeneratorView_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем начальные UI значения (как в вашем старом коде)
            dpStartDate.SelectedDate = DateTime.Today;
            dpEndDate.SelectedDate = DateTime.Today.AddDays(30);

            // Привязываем коллекции VM к контролам (без изменения XAML)
            cbRoutes.ItemsSource = _vm.Routes;
            cbRoutes.DisplayMemberPath = "RouteInfo";
            cbRoutes.SelectedValuePath = "RouteID";

            cbDrivers.ItemsSource = _vm.Drivers;
            cbDrivers.DisplayMemberPath = "Title";
            cbDrivers.SelectedValuePath = "Id";

            cbBuses.ItemsSource = _vm.Buses;
            cbBuses.DisplayMemberPath = "Title";
            cbBuses.SelectedValuePath = "Id";

            cbHour.ItemsSource = _vm.HourList;
            cbMinute.ItemsSource = _vm.MinuteList;

            // Устанавливаем начальные значения hour/minute
            cbHour.SelectedItem = _vm.SelectedHour;
            cbMinute.SelectedItem = _vm.SelectedMinute;

            // Связываем кнопки с командами VM, чтобы не менять XAML
            if (btnGenerate != null) btnGenerate.Command = _vm.GenerateCommand;
            if (btnCancel != null) btnCancel.Command = _vm.CancelCommand;

            // Подписываем обработчики, чтобы UI обновлял свойства VM (т.к. мы не меняем XAML)
            cbRoutes.SelectionChanged += (s, ev) =>
            {
                _vm.SelectedRoute = cbRoutes.SelectedItem as RouteDTO;
            };

            cbDrivers.SelectionChanged += (s, ev) =>
            {
                _vm.SelectedDriverId = TryGetSelectedId(cbDrivers.SelectedItem);
            };

            cbBuses.SelectionChanged += (s, ev) =>
            {
                _vm.SelectedBusId = TryGetSelectedId(cbBuses.SelectedItem);
            };

            dpStartDate.SelectedDateChanged += (s, ev) =>
            {
                _vm.StartDate = dpStartDate.SelectedDate ?? DateTime.Today;
            };

            dpEndDate.SelectedDateChanged += (s, ev) =>
            {
                _vm.EndDate = dpEndDate.SelectedDate ?? DateTime.Today;
            };

            cbHour.SelectionChanged += (s, ev) =>
            {
                if (cbHour.SelectedItem != null) _vm.SelectedHour = Convert.ToInt32(cbHour.SelectedItem);
            };

            cbMinute.SelectionChanged += (s, ev) =>
            {
                if (cbMinute.SelectedItem != null) _vm.SelectedMinute = Convert.ToInt32(cbMinute.SelectedItem);
            };

            // CheckBoxes (без использования неинициализированных локальных переменных)
            var chkMon = FindName("chkMonday") as CheckBox;
            if (chkMon != null)
            {
                chkMon.Checked += (s, e2) => _vm.Monday = true;
                chkMon.Unchecked += (s, e2) => _vm.Monday = false;
            }

            var chkTue = FindName("chkTuesday") as CheckBox;
            if (chkTue != null)
            {
                chkTue.Checked += (s, e2) => _vm.Tuesday = true;
                chkTue.Unchecked += (s, e2) => _vm.Tuesday = false;
            }

            var chkWed = FindName("chkWednesday") as CheckBox;
            if (chkWed != null)
            {
                chkWed.Checked += (s, e2) => _vm.Wednesday = true;
                chkWed.Unchecked += (s, e2) => _vm.Wednesday = false;
            }

            var chkThu = FindName("chkThursday") as CheckBox;
            if (chkThu != null)
            {
                chkThu.Checked += (s, e2) => _vm.Thursday = true;
                chkThu.Unchecked += (s, e2) => _vm.Thursday = false;
            }

            var chkFri = FindName("chkFriday") as CheckBox;
            if (chkFri != null)
            {
                chkFri.Checked += (s, e2) => _vm.Friday = true;
                chkFri.Unchecked += (s, e2) => _vm.Friday = false;
            }

            var chkSat = FindName("chkSaturday") as CheckBox;
            if (chkSat != null)
            {
                chkSat.Checked += (s, e2) => _vm.Saturday = true;
                chkSat.Unchecked += (s, e2) => _vm.Saturday = false;
            }

            var chkSun = FindName("chkSunday") as CheckBox;
            if (chkSun != null)
            {
                chkSun.Checked += (s, e2) => _vm.Sunday = true;
                chkSun.Unchecked += (s, e2) => _vm.Sunday = false;
            }

            // Price textbox updates VM
            if (tbPrice != null) tbPrice.TextChanged += (s, ev) => _vm.PriceText = tbPrice.Text;

            // Подписываемся на изменения свойств IsBusy/InfoText, чтобы отображать в UI (если у вас есть tbInfo/pbProgress)
            _vm.PropertyChanged += (s, ev) =>
            {
                if (ev.PropertyName == nameof(_vm.IsBusy))
                {
                    if (btnGenerate != null) btnGenerate.IsEnabled = !_vm.IsBusy;
                }
            };

            // Запускаем загрузку данных
            await SafeInitializeAsync();
        }

        private async Task SafeInitializeAsync()
        {
            try
            {
                await _vm.InitializeAsync();

                // Установим SelectedIndex на UI, если коллекции заполнены (повторяет старое поведение)
                if (cbRoutes.Items.Count > 0 && cbRoutes.SelectedIndex < 0) cbRoutes.SelectedIndex = 0;
                if (cbDrivers.Items.Count > 0 && cbDrivers.SelectedIndex < 0) cbDrivers.SelectedIndex = 0;
                if (cbBuses.Items.Count > 0 && cbBuses.SelectedIndex < 0) cbBuses.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int? TryGetSelectedId(object selected)
        {
            if (selected == null) return null;
            var t = selected.GetType();
            var p = t.GetProperty("Id") ?? t.GetProperty("ID") ?? t.GetProperty("Id", System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (p == null) return null;
            try
            {
                var v = p.GetValue(selected);
                if (v == null) return null;
                return Convert.ToInt32(v);
            }
            catch { return null; }
        }
    }
}