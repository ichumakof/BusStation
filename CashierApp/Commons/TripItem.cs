using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashierApp.Commons
{
    public class TripItem : INotifyPropertyChanged
    {
        public int TripID { get; set; }
        public string RouteTitle { get; set; }
        public string ExtraInfo { get; set; }
        public double Price { get; set; }
        public string PriceText { get; set; }
        public string DepartureTime { get; set; }

        private int _availableSeats = -1;
        public int AvailableSeats
        {
            get => _availableSeats;
            set
            {
                if (_availableSeats != value)
                {
                    _availableSeats = value;
                    OnPropertyChanged(nameof(AvailableSeats));
                    OnPropertyChanged(nameof(AvailableSeatsText));
                }
            }
        }
        public string AvailableSeatsText => AvailableSeats < 0 ? "..." : AvailableSeats.ToString();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
