
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using QuickScrcpy.Models;
using QuickScrcpy.Utils;

namespace QuickScrcpy.Services
{
    public class NetworkScanner
    {
        public event EventHandler<DeviceInfo>? DeviceFound;
        public event EventHandler<int>? ScanProgressChanged;
        public event EventHandler? ScanCompleted;
        public event EventHandler<string>? LogMessage;

        private CancellationTokenSource? _cancellationTokenSource;
        private int _totalDevices;
        private int _scannedDevices;
        private Dictionary<string, string>? _arpCache;
        private readonly SemaphoreSlim _semaphore;

        public bool IsScanning => _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;

        private static readonly int[] KeyPorts = new[] { 5555 };

        private static readonly Dictionary<int, string> PortServices = new()
        {
            { 5555, "ADB" },
            { 7612, "AirDroid" },
            { 22, "SSH" },
            { 80, "HTTP" },
            { 443, "HTTPS" },
            { 5900, "VNC" }
        };

        public NetworkScanner()
        {
            _arpCache = new Dictionary<string, string>();
            _semaphore = new SemaphoreSlim(50, 50);
        }

        private void OnLog(string message)
        {
            LogMessage?.Invoke(this, message);
        }

        public async Task<List<DeviceInfo>> ScanNetworkAsync(int port = 5555, int timeout = 800)
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
                    OnLog("错误: 无法获取本地IP地址");
                    throw new Exception("无法获取本地IP地址");
                }

                OnLog($"本地IP: {localIP}");

                LoadArpCache();
                OnLog($"ARP缓存中有 {_arpCache?.Count ?? 0} 条记录");

                var subnet = GetSubnet(localIP);
                var ipParts = subnet.Split('.');
                if (ipParts.Length < 3)
                {
                    throw new Exception("无效的IP地址格式");
                }

                var baseIP = $"{ipParts[0]}.{ipParts[1]}.{ipParts[2]}.";
                _totalDevices = 254;
                _scannedDevices = 0;

                OnLog($"开始扫描子网: {baseIP}1 - {baseIP}254");

                var tasks = new List<Task>();
                for (int i = 1; i <= 254; i++)
                {
                    if (token.IsCancellationRequested)
                        break;

                    var currentIP = i;
                    var ip = $"{baseIP}{currentIP}";
                    
                    tasks.Add(ScanDeviceSafeAsync(ip, port, timeout, token, devices, lockObj));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                OnLog($"扫描错误: {ex.Message}");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                ScanCompleted?.Invoke(this, EventArgs.Empty);
            }

            OnLog($"扫描完成，共发现 {devices.Count} 个设备");
            return devices;
        }

        private async Task ScanDeviceSafeAsync(string ip, int defaultPort, int timeout, 
            CancellationToken token, List<DeviceInfo> devices, object lockObj)
        {
            try
            {
                await _semaphore.WaitAsync(token);
                
                var device = await ScanDeviceAsync(ip, defaultPort, timeout, token);
                
                if (device != null)
                {
                    lock (lockObj)
                    {
                        devices.Add(device);
                    }
                    DeviceFound?.Invoke(this, device);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                OnLog($"扫描 {ip} 时出错: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
                Interlocked.Increment(ref _scannedDevices);
                var progress = _scannedDevices * 100 / _totalDevices;
                ScanProgressChanged?.Invoke(this, progress);
            }
        }

        private async Task<DeviceInfo?> ScanDeviceAsync(string ip, int defaultPort, int timeout, CancellationToken token)
        {
            var isOnline = false;
            var macAddress = GetMacFromArpCache(ip);
            var manufacturer = "未知设备";
            var hasAdbOpen = false;

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, timeout);
                isOnline = reply.Status == IPStatus.Success;
            }
            catch
            {
            }

            if (!string.IsNullOrEmpty(macAddress))
            {
                isOnline = true;
                manufacturer = MacVendorLookup.LookupVendor(macAddress);
            }

            if (!isOnline)
            {
                return null;
            }

            hasAdbOpen = await CheckPortQuickAsync(ip, 5555, 500);

            var isAndroid = false;
            if (hasAdbOpen)
            {
                isAndroid = true;
            }
            else if (MacVendorLookup.IsLikelyMobileDevice(manufacturer))
            {
                isAndroid = manufacturer != "Apple";
            }

            var device = new DeviceInfo
            {
                IPAddress = ip,
                MACAddress = macAddress ?? "",
                Port = hasAdbOpen ? 5555 : defaultPort,
                Manufacturer = manufacturer,
                IsAndroid = isAndroid,
                LastSeen = DateTime.Now
            };

            if (hasAdbOpen)
            {
                device.DeviceName = $"Android设备 ({ip})";
                device.Status = "ADB可用";
            }
            else if (isAndroid && !string.IsNullOrEmpty(macAddress))
            {
                device.DeviceName = $"{manufacturer}设备 ({ip})";
                device.Status = "潜在Android设备";
            }
            else if (manufacturer == "Apple")
            {
                device.DeviceName = $"Apple设备 ({ip})";
                device.Status = "Apple设备";
            }
            else
            {
                device.DeviceName = $"网络设备 ({ip})";
                device.Status = "在线设备";
            }

            return device;
        }

        private void LoadArpCache()
        {
            _arpCache = new Dictionary<string, string>();
            
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "arp",
                        Arguments = "-a",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("接口") || 
                        trimmedLine.StartsWith("Internet") ||
                        trimmedLine.StartsWith("---") ||
                        trimmedLine.StartsWith("Interface:") ||
                        string.IsNullOrEmpty(trimmedLine))
                    {
                        continue;
                    }

                    var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var ip = parts[0];
                        var mac = parts[1];
                        
                        if (IPAddress.TryParse(ip, out _) && 
                            mac.Length >= 11 && 
                            mac != "00-00-00-00-00-00" &&
                            mac != "ff-ff-ff-ff-ff-ff")
                        {
                            var normalizedMac = mac.Replace('-', ':').ToUpper();
                            _arpCache[ip] = normalizedMac;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnLog($"加载ARP缓存出错: {ex.Message}");
            }
        }

        private string? GetMacFromArpCache(string ip)
        {
            if (_arpCache == null)
                return null;

            _arpCache.TryGetValue(ip, out var mac);
            return mac;
        }

        public void CancelScan()
        {
            _cancellationTokenSource?.Cancel();
        }

        private IPAddress? GetLocalIPAddress()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    var ipProps = ni.GetIPProperties();
                    
                    foreach (var ip in ipProps.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (ip.Address.ToString().StartsWith("127."))
                                continue;
                            
                            if (ip.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred)
                            {
                                return ip.Address;
                            }
                        }
                    }
                }
            }
            
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            try
            {
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address;
            }
            catch
            {
                return null;
            }
        }

        private string GetSubnet(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.0";
        }

        private async Task<bool> CheckPortQuickAsync(string ip, int port, int timeout)
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

        private async Task<bool> CheckPortAsync(string ip, int port, int timeout)
        {
            return await CheckPortQuickAsync(ip, port, timeout);
        }

        public async Task<Dictionary<int, string>> ScanPortsAsync(string ip, int[]? ports = null, int timeout = 1000)
        {
            var result = new Dictionary<int, string>();
            var targetPorts = ports ?? KeyPorts;

            foreach (var port in targetPorts)
            {
                var isOpen = await CheckPortAsync(ip, port, timeout);
                if (isOpen)
                {
                    PortServices.TryGetValue(port, out var service);
                    result[port] = service ?? "未知服务";
                }
            }

            return result;
        }

        public Dictionary<string, string> GetArpTable()
        {
            LoadArpCache();
            return _arpCache ?? new Dictionary<string, string>();
        }
    }
}
