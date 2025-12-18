using System.Windows;
using BLL.Interfaces;
using AdminApp.ViewModels;

namespace AdminApp.Views
{
    public partial class UsersWindow : Window
    {
        public UsersWindow(IUserService userService, int currentAdminId)
        {
            InitializeComponent();
            DataContext = new UsersViewModel(userService, currentAdminId, onlyCashiers: true);
        }
    }
}