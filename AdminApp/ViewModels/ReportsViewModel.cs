using BLL.Interfaces;
using BLL.Models;
using BLL.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AdminApp.Commons;
using System.Windows.Input;

namespace AdminApp.ViewModels
{
    public class CitySelectionItem
    {
        public int CityID { get; set; }
        public string CityName { get; set; }
        public bool IsSelected { get; set; }
    }

    public class ReportsViewModel : INotifyPropertyChanged
    {
        private const string OutputFolder = @"C:\Users\ichum\Desktop\игэ(у)\5 семестр\Конструирование ПО\ОТЧЕТЫ";

        private readonly ITicketService _ticketService;
        private readonly IReportService _reportService;

        public ReportsViewModel(ITicketService ticketService, IReportService reportService)
        {
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));

            Cities = new ObservableCollection<CitySelectionItem>();

            FromDate = DateTime.Today.AddDays(-7);
            ToDate = DateTime.Today;

            GenerateReportCommand = new AsyncCommand(GenerateReportAsync, () => !IsBusy);
            CancelCommand = new SimpleCommand(_ => RequestClose?.Invoke(this, false), _ => true);
        }

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<MessageRequestEventArgs> RequestShowMessage;
        public event EventHandler<bool> RequestClose; // true = success/ok, false = cancelled
        #endregion

        #region Bindable props
        public ObservableCollection<CitySelectionItem> Cities { get; }

        private DateTime _fromDate;
        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                if (_fromDate != value)
                {
                    _fromDate = value;
                    OnProp(nameof(FromDate));
                }
            }
        }

        private DateTime _toDate;
        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                if (_toDate != value)
                {
                    _toDate = value;
                    OnProp(nameof(ToDate));
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnProp(nameof(IsBusy));
                    (GenerateReportCommand as AsyncCommand)?.RaiseCanExecuteChanged();
                    (CancelCommand as SimpleCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        #endregion

        #region Commands
        public ICommand GenerateReportCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        // Инициализация (вызывать после создания VM) — вызывайте из UI-потока
        public async Task InitializeAsync()
        {
            await LoadCitiesAsync();
        }

        private async Task LoadCitiesAsync()
        {
            try
            {
                var list = await _ticketService.GetAllCitiesAsync();

                // предполагаем, что вызов InitializeAsync был сделан из UI-потока,
                // поэтому здесь безопасно обновлять коллекцию напрямую.
                Cities.Clear();
                if (list != null)
                {
                    foreach (var c in list)
                    {
                        // исключаем город отправления "Иваново"
                        if (string.Equals(c.CityName, "Иваново", StringComparison.OrdinalIgnoreCase))
                            continue;

                        Cities.Add(new CitySelectionItem { CityID = c.CityID, CityName = c.CityName, IsSelected = false });
                    }
                }
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs
                {
                    Message = "Ошибка при загрузке городов: " + ex.Message,
                    Caption = "Ошибка",
                    Icon = System.Windows.MessageBoxImage.Error
                });
            }
        }

        private async Task GenerateReportAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Обход ObservableCollection делаем на UI-потоке (это выполняется синхронно до первого await)
                var selectedCityIds = Cities.Where(c => c.IsSelected).Select(c => c.CityID).ToList();
                var selectedCityNames = Cities.Where(c => c.IsSelected).Select(c => c.CityName).ToList();
                IList<int> cityFilter = selectedCityIds.Any() ? selectedCityIds : null;

                var report = await _ticketService.GetTicketsReportAsync(FromDate, ToDate, cityFilter);

                // Если отчёт пустой — уведомляем и выходим
                if (report == null)
                {
                    RequestShowMessage?.Invoke(this, new MessageRequestEventArgs
                    {
                        Message = "Нет данных для отчёта.",
                        Caption = "Информация",
                        Icon = System.Windows.MessageBoxImage.Information
                    });
                    return;
                }

                // Если пользователь выбрал конкретные города, то добавляем в report.Items записи с нулевыми значениями
                if (selectedCityNames != null && selectedCityNames.Count > 0)
                {
                    if (report.Items == null) report.Items = new List<TicketReportItem>();

                    foreach (var cityName in selectedCityNames)
                    {
                        bool exists = report.Items.Any(it =>
                        {
                            if (it == null) return false;
                            if (!string.IsNullOrEmpty(it.RouteTitle))
                                return it.RouteTitle.IndexOf(cityName, StringComparison.OrdinalIgnoreCase) >= 0;
                            return false;
                        });

                        if (!exists)
                        {
                            var zeroItem = new TicketReportItem
                            {
                                RouteTitle = cityName,
                                SoldCount = 0,
                                ReturnedCount = 0,
                                EarnedSum = 0.0
                            };
                            report.Items.Add(zeroItem);
                        }
                    }

                    // Пересчитаем суммарные значения (если в модели есть Total... поля)
                    try
                    {
                        report.TotalSold = report.Items.Sum(i => i.SoldCount);
                        report.TotalReturned = report.Items.Sum(i => i.ReturnedCount);
                        report.TotalEarned = report.Items.Sum(i => i.EarnedSum);
                    }
                    catch
                    {
                        // не критично — если в модели нет таких полей или отличаются имена, пропускаем
                    }
                }

                if (!System.IO.Directory.Exists(OutputFolder))
                    System.IO.Directory.CreateDirectory(OutputFolder);

                var filename = System.IO.Path.Combine(OutputFolder, $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                // Генерация PDF выполняется в BLL (может быть медленной) — вызываем и ждём.
                // Мы не используем ConfigureAwait(false), чтобы продолжение выполнялось в UI-контексте.
                await _reportService.GeneratePdfReportAsync(report, filename);

                // Показываем уведомление (мы всё ещё в UI-контексте)
                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs
                {
                    Message = $"Отчёт успешно сохранён:\n{filename}",
                    Caption = "Готово",
                    Icon = System.Windows.MessageBoxImage.Information
                });

                // Закрываем окно — обработчик работает с WPF-элементами
                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                RequestShowMessage?.Invoke(this, new MessageRequestEventArgs
                {
                    Message = "Ошибка при формировании отчёта: " + ex.Message,
                    Caption = "Ошибка",
                    Icon = System.Windows.MessageBoxImage.Error
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region Helpers

        private void OnProp(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region Local command implementations (простые и самодостаточные)

        // Простой ICommand для синхронных команд
        private class SimpleCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Func<object, bool> _canExecute;

            public SimpleCommand(Action<object> execute, Func<object, bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object parameter) => _execute(parameter);

            public event EventHandler CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        // Простой Async ICommand (для параметров без значения)
        private class AsyncCommand : ICommand
        {
            private readonly Func<Task> _executeAsync;
            private readonly Func<bool> _canExecute;
            private bool _isRunning;

            public AsyncCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
            {
                _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

            public async void Execute(object parameter)
            {
                await ExecuteAsync().ConfigureAwait(false);
            }

            public async Task ExecuteAsync()
            {
                if (!CanExecute(null)) return;
                try
                {
                    _isRunning = true;
                    RaiseCanExecuteChanged();
                    await _executeAsync();
                }
                finally
                {
                    _isRunning = false;
                    RaiseCanExecuteChanged();
                }
            }

            public event EventHandler CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}