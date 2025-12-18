// ViewModels/UsersViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BLL.Interfaces;
using BLL.Models;
using AdminApp.Views;

namespace AdminApp.ViewModels
{
    public class UsersViewModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private readonly bool _onlyCashiers;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnProp(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<UserDTO> Users { get; } = new ObservableCollection<UserDTO>();

        private UserDTO _selectedUser;
        public UserDTO SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value; OnProp(nameof(SelectedUser)); CommandManager.InvalidateRequerySuggested(); }
        }

        public int CurrentAdminId { get; }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand EditSelfCommand { get; }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; private set { _isBusy = value; OnProp(nameof(IsBusy)); } }

        public UsersViewModel(IUserService userService, int currentAdminId, bool onlyCashiers = true)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            CurrentAdminId = currentAdminId;
            _onlyCashiers = onlyCashiers;

            AddCommand = new RelayCommand(OnAdd);
            EditCommand = new RelayCommand(OnEdit, CanEditSelected);
            DeleteCommand = new RelayCommand(OnDelete, CanDeleteSelected);
            RefreshCommand = new AsyncRelayCommand(LoadAsync);
            EditSelfCommand = new RelayCommand(OnEditSelf);

            _ = LoadAsync();
        }

        private bool IsCashier(UserDTO u) =>
            !string.IsNullOrWhiteSpace(u?.RoleName) &&
            (u.RoleName.Equals("Cashier", StringComparison.OrdinalIgnoreCase) ||
             u.RoleName.Equals("Кассир", StringComparison.OrdinalIgnoreCase));

        private bool IsAdministrator(UserDTO u) =>
            !string.IsNullOrWhiteSpace(u?.RoleName) &&
            (u.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
             u.RoleName.Equals("Администратор", StringComparison.OrdinalIgnoreCase));

        private async Task LoadAsync()
        {
            try
            {
                IsBusy = true;
                var list = await Task.Run(() => _userService.GetAll() ?? Enumerable.Empty<UserDTO>());
                if (_onlyCashiers) list = list.Where(IsCashier);

                Users.Clear();
                foreach (var u in list) Users.Add(u);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователей: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void OnAdd()
        {
            // Создаём кассира — роль фиксирована
            int cashierRoleId = Users.FirstOrDefault()?.RoleID ?? 2;
            string cashierRoleName = Users.FirstOrDefault()?.RoleName ?? "Cashier";

            var dto = new UserDTO { RoleID = cashierRoleId, RoleName = cashierRoleName };

            var vm = new UserEditViewModel(_userService, dto, CurrentAdminId, isNew: true) { CanEditRole = false };
            var wnd = new UserEditWindow { DataContext = vm, Owner = Application.Current.MainWindow };

            vm.RequestShowMessage += (s, e) => MessageBox.Show(wnd, e.Message, e.Caption, MessageBoxButton.OK, e.Icon);
            vm.RequestClose += (s, ok) =>
            {
                if (ok) _ = LoadAsync();
                wnd.DialogResult = ok;
                wnd.Close();
            };

            wnd.ShowDialog();
        }

        private bool CanEditSelected() => SelectedUser != null;

        private void OnEdit()
        {
            if (SelectedUser == null) return;

            var copy = new UserDTO
            {
                UserID = SelectedUser.UserID,
                Login = SelectedUser.Login,
                FullName = SelectedUser.FullName,
                RoleID = SelectedUser.RoleID,
                RoleName = SelectedUser.RoleName
            };

            var vm = new UserEditViewModel(_userService, copy, CurrentAdminId, isNew: false) { CanEditRole = false };
            var wnd = new UserEditWindow { DataContext = vm, Owner = Application.Current.MainWindow };

            vm.RequestShowMessage += (s, e) => MessageBox.Show(wnd, e.Message, e.Caption, MessageBoxButton.OK, e.Icon);
            vm.RequestClose += (s, ok) =>
            {
                if (ok) _ = LoadAsync();
                wnd.DialogResult = ok;
                wnd.Close();
            };

            wnd.ShowDialog();
        }

        private bool CanDeleteSelected()
        {
            if (SelectedUser == null) return false;
            if (SelectedUser.UserID == CurrentAdminId) return false; // нельзя удалить себя
            if (!IsCashier(SelectedUser)) return false; // удаляем только кассиров
            return true;
        }

        private void OnDelete()
        {
            if (SelectedUser == null) return;

            if (MessageBox.Show($"Удалить пользователя \"{SelectedUser.Login}\"?", "Подтвердите", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                _userService.Delete(SelectedUser.UserID, CurrentAdminId);
                _ = LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Редактирование данных текущего администратора
        private void OnEditSelf()
        {
            try
            {
                // Предпочтительно — запросить у сервиса
                var me = _userService.GetAll()?.FirstOrDefault(u => u.UserID == CurrentAdminId);
                // Если GetAll не возвращает админа, можно попытаться вытащить из App.Current.CurrentUser
                if (me == null)
                {
                    var appUser = App.Current.GetType().GetProperty("CurrentUser")?.GetValue(App.Current, null);
                    if (appUser != null)
                    {
                        me = new UserDTO
                        {
                            UserID = (int)(appUser.GetType().GetProperty("UserID")?.GetValue(appUser) ?? 0),
                            Login = appUser.GetType().GetProperty("Login")?.GetValue(appUser)?.ToString(),
                            FullName = appUser.GetType().GetProperty("FullName")?.GetValue(appUser)?.ToString(),
                            RoleID = (int)(appUser.GetType().GetProperty("RoleID")?.GetValue(appUser) ?? 0),
                            RoleName = appUser.GetType().GetProperty("RoleName")?.GetValue(appUser)?.ToString()
                        };
                    }
                }

                if (me == null)
                {
                    MessageBox.Show("Не удалось получить данные текущего пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var vm = new UserEditViewModel(_userService, me, CurrentAdminId, isNew: false) { CanEditRole = false };
                var wnd = new UserEditWindow { DataContext = vm, Owner = Application.Current.MainWindow };

                vm.RequestShowMessage += (s, e) => MessageBox.Show(wnd, e.Message, e.Caption, MessageBoxButton.OK, e.Icon);
                vm.RequestClose += (s, ok) =>
                {
                    // Обновление списка кассиров не обязательно, но можно
                    if (ok) _ = LoadAsync();
                    wnd.DialogResult = ok;
                    wnd.Close();
                };

                wnd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка открытия формы редактирования: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Встроенные команды
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