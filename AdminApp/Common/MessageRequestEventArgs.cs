using System;
using System.Windows;

namespace AdminApp.Common
{
    public class MessageRequestEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string Caption { get; set; } = "";
        public MessageBoxButton Button { get; set; } = MessageBoxButton.OK;
        public MessageBoxImage Icon { get; set; } = MessageBoxImage.None;
    }
}