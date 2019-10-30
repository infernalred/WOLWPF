using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            IPAddress myIp = Dns.GetHostEntry(myCompname).AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
            string[] ipArray = myIp.ToString().Split('.');
            List<Task<Computer>> tasks = new List<Task<Computer>>();


            Parallel.For(1, 254, j =>
            {
                tasks.Add(ScanIPAsync(string.Concat(ipArray[0] + ".", ipArray[1] + ".", ipArray[2] + ".", j)));
            });
            try
            {
                await Task.WhenAll(tasks);
            }
            catch { }
            foreach (var task in tasks)
            {
                App.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (task != null && !task.IsFaulted)
                        Computers.Add(task.Result);
                }));

            }
        }
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
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                UDP.Close();
            }
        }

        public Task<Computer> ScanIPAsync(string ip)
        {
            TaskCompletionSource<Computer> tcs = new TaskCompletionSource<Computer>();
            try
            {
                IPAddress dstIP = IPAddress.Parse(ip);
                IPHostEntry IPHost = null;
                byte[] macAddr = new byte[6];
                uint macAddrLen = (uint)macAddr.Length;
                IPHost = Dns.GetHostEntry(ip);
                if (SendARP(BitConverter.ToInt32(dstIP.GetAddressBytes(), 0), 0, macAddr, ref macAddrLen) != 0)
                    tcs.SetException(new InvalidOperationException("Send ARP failed"));
                string[] str = new string[(int)macAddrLen];
                for (int j = 0; j < macAddrLen; j++)
                    str[j] = macAddr[j].ToString("x2");
                string macAddress = string.Join(":", str);
                Computer comp = new Computer() { IP = dstIP.ToString(), Hostname = IPHost.HostName, MAC = macAddress };
                tcs.SetResult(comp);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
            return tcs.Task;
        }
    }
}
