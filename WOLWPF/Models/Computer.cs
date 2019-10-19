using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WOLWPF.Models
{
    class Computer : INotifyPropertyChanged
    {
        public string IP { get { return this.IP; } set { this.IP = value; OnPropertyChanged("IP"); } }
        public string Hostname { get { return this.Hostname; } set { this.Hostname = value; OnPropertyChanged("Hostname"); } }
        public string MAC { get { return this.MAC; } set { this.MAC = value; OnPropertyChanged("MAC"); } }



        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
