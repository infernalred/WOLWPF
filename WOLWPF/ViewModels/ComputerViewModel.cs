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
using System.Text.RegularExpressions;
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
                    {
                        Task.Run(() => Scan());
                    }));
            }
        }

        private RelayCommand _wakeUpCommand;
        public RelayCommand WakeUpCommand
        {
            get
            {
                return _wakeUpCommand ??
                    (_wakeUpCommand = new RelayCommand(obj =>
                    {
                        WakePC(SelectedComputer.MAC, SelectedComputer.IP);
                    }, (obj) => SelectedComputer != null));
                       
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

        public async void Scan()
        {
            string myCompname = Dns.GetHostName();
            //IPHostEntry iPHostEntry = await Dns.GetHostEntryAsync(myCompname);
            IPAddress myIp = Dns.GetHostEntry(myCompname).AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
            string[] ipArray = myIp.ToString().Split('.');

            List<Task<IPHostEntry>> tasks = new List<Task<IPHostEntry>>();
            for (int i = 1; i < 254; i++)
            {
                tasks.Add(Dns.GetHostEntryAsync(string.Concat(ipArray[0] + ".", ipArray[1] + ".", ipArray[2] + ".", i)));
            }
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
            }
            foreach (var task in tasks)
            {
                if (!task.IsFaulted)
                {
                    Computer comp = await ScanIPAsync(task.Result);
                    App.Current.Dispatcher.Invoke(new Action(() =>
                {
                    Computers.Add(comp);
                }));
                }


            }


            //Parallel.For(1, 254, async j =>
            //{
            //    //Computers.Add(async ScanIPAsync(string.Concat(ipArray[0] + ".", ipArray[1] + ".", ipArray[2] + ".", j)));
            //    Computer tmp = await ScanIPAsync(string.Concat(ipArray[0] + ".", ipArray[1] + ".", ipArray[2] + ".", j));
            //    App.Current.Dispatcher.Invoke(new Action(() =>
            //    {
            //        Computers.Add(tmp);
            //    }));
            //});
        }
        public Task<Computer> ScanIPAsync(IPHostEntry iPHostEntry)
        {
            IPAddress dstIP = iPHostEntry.AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
            byte[] macAddr = new byte[6];
            uint macAddrLen = (uint)macAddr.Length;

            if (SendARP(BitConverter.ToInt32(dstIP.GetAddressBytes(), 0), 0, macAddr, ref macAddrLen) != 0)
                throw new InvalidOperationException("Send ARP failed {iPHostEntry.AddressList[0]}");

            string[] str = new string[(int)macAddrLen];
            for (int j = 0; j < macAddrLen; j++)
                str[j] = macAddr[j].ToString("x2");
            string macAddress = string.Join(":", str);

            //return new Computer() { IP = iPHostEntry.AddressList[0].ToString(), Hostname = iPHostEntry.HostName, MAC = macAddress };
            return Task.Run(() =>
            {
                Computer comp = new Computer() { IP = dstIP.ToString(), Hostname = iPHostEntry.HostName, MAC = macAddress };
                return comp;
            });

        }
//public Task<Computer> ScanIPAsync(IPHostEntry iPHostEntry)
//{
//    IPAddress dstIP = IPAddress.Parse(ip);
//    string HostName = string.Empty;
//    string macAddress = string.Empty;
//    try
//    {
//        IPHostEntry host = Dns.GetHostEntry(dstIP);
//        byte[] macAddr = new byte[6];
//        uint macAddrLen = (uint)macAddr.Length;

//        if (SendARP(BitConverter.ToInt32(dstIP.GetAddressBytes(), 0), 0, macAddr, ref macAddrLen) != 0)
//            throw new InvalidOperationException("Send ARP failed");

//        string[] str = new string[(int)macAddrLen];
//        for (int j = 0; j < macAddrLen; j++)
//            str[j] = macAddr[j].ToString("x2");
//        macAddress = string.Join(":", str);
//        HostName = host.HostName;

//    }
//    catch { }
//    //return new Computer() { IP = dstIP.ToString(), Hostname = HostName, MAC = macAddress };
//    return Task.Run( () => {
//        Computer comp = new Computer() { IP = dstIP.ToString(), Hostname = HostName, MAC = macAddress };
//        return comp;
//    }); 

//}

public async void ScanIP(string ip)
        {
            IPAddress dstIP = IPAddress.Parse(ip);
            try
            {
                IPHostEntry host = await Dns.GetHostEntryAsync(dstIP);
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

        //public void ScanIP(string ip)
        //{
        //    IPAddress dstIP = IPAddress.Parse(ip);
        //    try
        //    {
        //        IPHostEntry host = Dns.GetHostEntry(dstIP);
        //        byte[] macAddr = new byte[6];
        //        uint macAddrLen = (uint)macAddr.Length;

        //        if (SendARP(BitConverter.ToInt32(dstIP.GetAddressBytes(), 0), 0, macAddr, ref macAddrLen) != 0)
        //            throw new InvalidOperationException("Send ARP failed");

        //        string[] str = new string[(int)macAddrLen];
        //        for (int j = 0; j < macAddrLen; j++)
        //            str[j] = macAddr[j].ToString("x2");
        //        string macAddress = string.Join(":", str);
        //        App.Current.Dispatcher.Invoke(new Action(() =>
        //        {
        //            Computers.Add(new Computer() { IP = dstIP.ToString(), Hostname = host.HostName, MAC = macAddress });
        //        }));

        //    }
        //    catch { }
        //}

        public void WakePC(string mac, string ip)
        {
            IPAddress IP = IPAddress.Parse(ip);
            UdpClient UDP = new UdpClient();
            string Mac = Regex.Replace(mac, "-|:", "");
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
