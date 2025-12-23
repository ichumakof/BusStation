using System;
using System.Windows;

namespace CashierApp.Commons
{
    public class MessageRequestEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string Caption { get; set; } = "";
        public MessageBoxButton Button { get; set; } = MessageBoxButton.OK;
        public MessageBoxImage Icon { get; set; } = MessageBoxImage.None;
    }
}
