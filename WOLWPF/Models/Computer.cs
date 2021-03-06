﻿using System;
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
        private string _hostname;
        private string _ip;
        private string _mac;
        public string IP { get { return _ip; } set { _ip = value; OnPropertyChanged("IP"); } }
        public string Hostname { get { return _hostname; } set { _hostname = value; OnPropertyChanged("Hostname"); } }
        public string MAC { get { return _mac; } set { _mac = value; OnPropertyChanged("MAC"); } }



        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
