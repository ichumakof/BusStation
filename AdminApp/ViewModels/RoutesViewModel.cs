using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using BLL.Interfaces;

namespace AdminApp.ViewModels
{
    public class RoutesViewModel : INotifyPropertyChanged
    {
        private readonly IRouteManagerService _routeManager;

        public RoutesViewModel(IRouteManagerService routeManager)
        {
            _routeManager = routeManager ?? throw new ArgumentNullException(nameof(routeManager));
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
            CancelCommand = new RelayCommand(Cancel);
        }

        // Публичные свойства для биндинга
        private string _cityName;
        public string CityName { get => _cityName; set { _cityName = value; OnPropertyChanged(); } }

        private int _hours;
        public int Hours { get => _hours; set { _hours = value; OnPropertyChanged(); } }

        private int _minutes;
        public int Minutes { get => _minutes; set { _minutes = value; OnPropertyChanged(); } }

        private int _distance;
        public int Distance { get => _distance; set { _distance = value; OnPropertyChanged(); } }

        private string _info;
        public string Info { get => _info; set { _info = value; OnPropertyChanged(); } }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged(); } }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Событие, чтобы View мог закрыть окно: аргумент — nullable bool (DialogResult)
        public event EventHandler<bool?> RequestClose;

        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }

        private async Task SaveAsync()
        {
            Info = string.Empty;

            if (string.IsNullOrWhiteSpace(CityName))
            {
                Info = "Введите название города.";
                return;
            }

            if (Hours < 0 || Minutes < 0 || Minutes >= 60)
            {
                Info = "Укажите корректное время (часы >=0, минуты 0..59).";
                return;
            }

            if (Distance <= 0)
            {
                Info = "Укажите расстояние в километрах (>0).";
                return;
            }

            IsBusy = true;
            try
            {
                var duration = Hours * 60 + Minutes;
                var result = await _routeManager.CreateRouteWithCityAsync(CityName.Trim(), Distance, duration);
                if (!result.Success)
                {
                    Info = result.ErrorMessage ?? "Не удалось создать маршрут.";
                    return;
                }

                Info = "Маршрут создан.";
                // Закрываем форму с результатом true
                RequestClose?.Invoke(this, true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        #endregion
    }

    // Простая реализация RelayCommand и AsyncRelayCommand (если в проекте уже есть — используйте свои)
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
        public event EventHandler CanExecuteChanged;
        public void Execute(object parameter) => _execute();
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;
        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public event EventHandler CanExecuteChanged;

        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter)) return;
            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}