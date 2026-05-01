
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using QuickScrcpy.Models;

namespace QuickScrcpy.Services
{
    public class NetworkScanner
    {
        public event EventHandler<DeviceInfo>? DeviceFound;
        public event EventHandler<int>? ScanProgressChanged;
        public event EventHandler? ScanCompleted;

        private CancellationTokenSource? _cancellationTokenSource;
        private int _totalDevices;
        private int _scannedDevices;

        public bool IsScanning => _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;

        public async Task<List<DeviceInfo>> ScanNetworkAsync(int port = 5555, int timeout = 1000)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            var devices = new List<DeviceInfo>();
            var lockObj = new object();

            try
            {
                var localIP = GetLocalIPAddress();
                if (localIP == null)
                {
                    throw new Exception("无法获取本地IP地址");
                }

                var subnet = GetSubnet(localIP);
                var ipParts = subnet.Split('.');
                if (ipParts.Length < 3)
                {
                    throw new Exception("无效的IP地址格式");
                }

                var baseIP = $"{ipParts[0]}.{ipParts[1]}.{ipParts[2]}.";
                _totalDevices = 254;
                _scannedDevices = 0;

                var tasks = new List<Task>();
                for (int i = 1; i <= 254; i++)
                {
                    if (token.IsCancellationRequested)
                        break;

                    var currentIP = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        if (token.IsCancellationRequested)
                            return;

                        var ip = $"{baseIP}{currentIP}";
                        
                        if (ip == localIP.ToString())
                        {
                            Interlocked.Increment(ref _scannedDevices);
                            ScanProgressChanged?.Invoke(this, _scannedDevices * 100 / _totalDevices);
                            return;
                        }

                        try
                        {
                            using var ping = new Ping();
                            var reply = await ping.SendPingAsync(ip, timeout);
                            
                            if (reply.Status == IPStatus.Success)
                            {
                                var isPortOpen = await CheckPortAsync(ip, port, timeout);
                                
                                var device = new DeviceInfo
                                {
                                    IPAddress = ip,
                                    Port = port,
                                    IsAndroid = isPortOpen,
                                    Status = isPortOpen ? "设备可用" : "在线设备",
                                    LastSeen = DateTime.Now
                                };

                                if (isPortOpen)
                                {
                                    device.DeviceName = $"Android设备 ({ip})";
                                    device.Manufacturer = "Android";
                                }
                                else
                                {
                                    device.DeviceName = $"网络设备 ({ip})";
                                }

                                lock (lockObj)
                                {
                                    devices.Add(device);
                                }
                                DeviceFound?.Invoke(this, device);
                            }
                        }
                        catch
                        {
                            // 忽略错误
                        }
                        finally
                        {
                            Interlocked.Increment(ref _scannedDevices);
                            ScanProgressChanged?.Invoke(this, _scannedDevices * 100 / _totalDevices);
                        }
                    }, token));
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                ScanCompleted?.Invoke(this, EventArgs.Empty);
            }

            return devices;
        }

        public void CancelScan()
        {
            _cancellationTokenSource?.Cancel();
        }

        private IPAddress? GetLocalIPAddress()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                     ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                {
                    foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address;
                        }
                    }
                }
            }
            return null;
        }

        private string GetSubnet(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.0";
        }

        private async Task<bool> CheckPortAsync(string ip, int port, int timeout)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ip, port);
                var timeoutTask = Task.Delay(timeout);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                return completedTask == connectTask && client.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}
