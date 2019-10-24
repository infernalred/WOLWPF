using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WOLWPF.Models;

namespace WOLWPF.ViewModels
{
    class ComputerViewModel : INotifyPropertyChanged
    {
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int destIp, int srcIP, byte[] macAddr, ref uint physicalAddrLen);
        private Computer selectedComputer;
        public ObservableCollection<Computer> Computers { get; set; }

        private RelayCommand _scanCommand;
        public RelayCommand ScanCommand
        {
            get
            {
                return _scanCommand ??
                    (_scanCommand = new RelayCommand(obj =>
                    { ScanIP(); }));
            }
        }


        public Computer SelectedComputer
        {
            get { return selectedComputer; }
            set
            {
                selectedComputer = value;
                OnPropertyChanged("SelectedComputer");
            }
        }
        public ComputerViewModel()
        {
            Computers = new ObservableCollection<Computer>();
        }

        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void ScanIP()
        {
            string myCompname = Dns.GetHostName();
            IPHostEntry iPHostEntry = Dns.GetHostEntry(myCompname);
            IPAddress myIp = Dns.GetHostEntry(myCompname).AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
            string[] ipArray = myIp.ToString().Split('.');
            for (int i = 0; i < 254; i++)
            {
                IPAddress dstIP = IPAddress.Parse(string.Concat(ipArray[0] + ".", ipArray[1] + ".", ipArray[2] + ".", i));
                try
                {
                    IPHostEntry host = Dns.GetHostEntry(dstIP);
                    byte[] macAddr = new byte[6];
                    uint macAddrLen = (uint)macAddr.Length;

                    if (SendARP(BitConverter.ToInt32(dstIP.GetAddressBytes(), 0), 0, macAddr, ref macAddrLen) != 0)
                        throw new InvalidOperationException("Send ARP failed");

                    string[] str = new string[(int)macAddrLen];
                    for (int j = 0; j < macAddrLen; j++)
                        str[j] = macAddr[j].ToString("x2");
                    string macAddress = string.Join(":", str);
                    string _hostname = host.HostName;
                    string _ip = dstIP.ToString();
                    Computers.Add(new Computer() { IP = dstIP.ToString(), Hostname = host.HostName, MAC = macAddress });
                }
                catch { }
            }
        }

    }
}
