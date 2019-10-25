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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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
                    { Thread _thread = new Thread(() => Scan()); _thread.Start(); }));
            }
        }

        private RelayCommand _wakeUpCommand;
        public RelayCommand WakeUpCommand
        {
            get
            {
                return _wakeUpCommand ??
                    (_wakeUpCommand = new RelayCommand(obj => 
                    WakePC(SelectedComputer.MAC, SelectedComputer.IP)));
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

        public void Scan()
        {
            string myCompname = Dns.GetHostName();
            IPHostEntry iPHostEntry = Dns.GetHostEntry(myCompname);
            IPAddress myIp = Dns.GetHostEntry(myCompname).AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
            string[] ipArray = myIp.ToString().Split('.');

            Parallel.For(1, 254, j =>
            {
                ScanIP(string.Concat(ipArray[0] + ".", ipArray[1] + ".", ipArray[2] + ".", j));
            });
        }
        
        public void ScanIP(string ip)
        {
            IPAddress dstIP = IPAddress.Parse(ip);
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
                App.Current.Dispatcher.Invoke(new Action(() =>
                {
                    Computers.Add(new Computer() { IP = dstIP.ToString(), Hostname = host.HostName, MAC = macAddress });
                }));

            }
            catch { }
        }

        public void WakePC(string _Mac, string ip)
        {
            IPAddress IP = IPAddress.Parse(ip);
            UdpClient UDP = new UdpClient();
            string Mac = _Mac.Replace(":", "");
            try
            {
                UDP.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                int offset = 0;
                byte[] buffer = new byte[512];

                for (int i = 0; i < 6; i++)
                {
                    buffer[offset++] = 0xFF;
                }

                for (int i = 0; i < 16; i++)
                {
                    int j = 0;
                    for (int k = 0; k < 6; k++)
                    {
                        buffer[offset++] = byte.Parse(Mac.Substring(j, 2), System.Globalization.NumberStyles.HexNumber);
                        j += 2;
                    }
                }
                UDP.EnableBroadcast = true;
                UDP.Send(buffer, 512, new IPEndPoint(IP, 0x1));
            }
            catch { }
            finally
            {
                UDP.Close();
            }
        }
    }
}
