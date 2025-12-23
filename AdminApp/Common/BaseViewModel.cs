using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdminApp.Common
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnProp([CallerMemberName] string propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        protected bool SetProp<T>(ref T field, T value, [CallerMemberName] string propName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnProp(propName);
            return true;
        }
    }
}