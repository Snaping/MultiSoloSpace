
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuickScrcpy.Models;
using QuickScrcpy.Services;

namespace QuickScrcpy
{
    public partial class MainWindow : Window
    {
        private readonly NetworkScanner _scanner;
        private readonly ScrcpyManager _scrcpyManager;
        private readonly DependencyInstaller _dependencyInstaller;
        private readonly ObservableCollection<DeviceInfo> _devices;

        public MainWindow()
        {
            InitializeComponent();
            
            _scanner = new NetworkScanner();
            _scrcpyManager = new ScrcpyManager();
            _dependencyInstaller = new DependencyInstaller();
            _devices = new ObservableCollection<DeviceInfo>();

            DeviceDataGrid.ItemsSource = _devices;
            
            _scanner.DeviceFound += Scanner_DeviceFound;
            _scanner.ScanProgressChanged += Scanner_ScanProgressChanged;
            _scanner.ScanCompleted += Scanner_ScanCompleted;
            _scanner.LogMessage += Scanner_LogMessage;
            
            _scrcpyManager.LogMessage += ScrcpyManager_LogMessage;
            _scrcpyManager.ConnectionStateChanged += ScrcpyManager_ConnectionStateChanged;
            
            _dependencyInstaller.StatusChanged += DependencyInstaller_StatusChanged;
            _dependencyInstaller.ProgressChanged += DependencyInstaller_ProgressChanged;
            _dependencyInstaller.InstallationCompleted += DependencyInstaller_InstallationCompleted;
            
            Log("QuickScrcpy 初始化完成");
            Log(_dependencyInstaller.GetDependencyStatus());
            
            if (!_dependencyInstaller.IsAdbInstalled || !_dependencyInstaller.IsScrcpyInstalled)
            {
                Log("\n提示: 检测到缺少必要依赖，请点击「安装依赖」按钮安装");
            }
        }

        private void DependencyInstaller_StatusChanged(object? sender, string e)
        {
            Dispatcher.Invoke(() =>
            {
                Log(e);
                StatusTextBlock.Text = e;
            });
        }

        private void DependencyInstaller_ProgressChanged(object? sender, int e)
        {
            Dispatcher.Invoke(() =>
            {
                InstallProgressBar.Value = e;
            });
        }

        private void DependencyInstaller_InstallationCompleted(object? sender, bool e)
        {
            Dispatcher.Invoke(() =>
            {
                InstallProgressBar.Visibility = Visibility.Collapsed;
                InstallDependenciesButton.IsEnabled = true;
                
                if (e)
                {
                    MessageBox.Show("所有依赖安装完成！", "安装成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    Log(_dependencyInstaller.GetDependencyStatus());
                }
                else
                {
                    MessageBox.Show("依赖安装过程中出现错误，请查看日志获取详细信息", "安装失败", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
        }

        private async void InstallDependenciesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_dependencyInstaller.IsInstalling)
            {
                Log("安装正在进行中，请等待完成...");
                return;
            }

            if (_dependencyInstaller.IsAdbInstalled && _dependencyInstaller.IsScrcpyInstalled)
            {
                var result = MessageBox.Show(
                    "所有依赖已安装。是否要重新安装？", 
                    "依赖已存在", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes)
                    return;
            }

            InstallDependenciesButton.IsEnabled = false;
            InstallProgressBar.Visibility = Visibility.Visible;
            InstallProgressBar.Value = 0;
            
            Log("开始安装依赖...");
            Log("注意: 下载可能需要几分钟时间，请耐心等待");
            
            await _dependencyInstaller.InstallAllDependenciesAsync();
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_dependencyInstaller.IsAdbInstalled)
            {
                MessageBox.Show(
                    "检测到未安装 Android Platform Tools (adb)。\n请先点击「安装依赖」按钮安装必要组件。",
                    "缺少依赖",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (_scanner.IsScanning)
            {
                _scanner.CancelScan();
                Log("正在取消扫描...");
                return;
            }

            ScanButton.Content = "⏹ 取消扫描";
            ScanProgressBar.Visibility = Visibility.Visible;
            StatusTextBlock.Text = "正在扫描网络设备...";
            Log("开始扫描网络设备...");
            Log("正在获取 ARP 缓存并检测常见服务端口...");
            
            _devices.Clear();
            UpdateEmptyState();

            var devices = await _scanner.ScanNetworkAsync();
            
            Log($"扫描完成，共发现 {devices.Count} 个设备");
            StatusTextBlock.Text = $"扫描完成，发现 {devices.Count} 个设备";
            DeviceCountTextBlock.Text = $"已发现: {devices.Count} 台设备";
            
            ScanButton.Content = "🔍 扫描网络设备";
            ScanProgressBar.Visibility = Visibility.Collapsed;
        }

        private void Scanner_DeviceFound(object? sender, DeviceInfo e)
        {
            Dispatcher.Invoke(() =>
            {
                _devices.Add(e);
                UpdateEmptyState();
                
                var deviceInfo = $"发现设备: {e.IPAddress}";
                if (!string.IsNullOrEmpty(e.MACAddress))
                {
                    deviceInfo += $" ({e.MACAddress})";
                }
                if (!string.IsNullOrEmpty(e.Manufacturer) && e.Manufacturer != "未知设备")
                {
                    deviceInfo += $" [{e.Manufacturer}]";
                }
                deviceInfo += $" - {e.DeviceName}";
                Log(deviceInfo);
            });
        }

        private void Scanner_ScanProgressChanged(object? sender, int e)
        {
            Dispatcher.Invoke(() =>
            {
                ScanProgressBar.Value = e;
                StatusTextBlock.Text = $"扫描进度: {e}%";
            });
        }

        private void Scanner_ScanCompleted(object? sender, System.EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ScanProgressBar.Value = 0;
            });
        }

        private void Scanner_LogMessage(object? sender, string e)
        {
            Dispatcher.Invoke(() => Log($"[扫描器] {e}"));
        }

        private void ManualConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ManualConnectDialog();
            if (dialog.ShowDialog() == true)
            {
                var device = new DeviceInfo
                {
                    IPAddress = dialog.IPAddress,
                    Port = dialog.Port,
                    DeviceName = $"手动添加 ({dialog.IPAddress})",
                    IsAndroid = true,
                    Status = "手动添加"
                };
                
                _devices.Add(device);
                UpdateEmptyState();
                Log($"手动添加设备: {device.IPAddress}:{device.Port}");
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDevice = DeviceDataGrid.SelectedItem as DeviceInfo;
            if (selectedDevice == null)
            {
                MessageBox.Show("请先选择一个设备", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_dependencyInstaller.IsAdbInstalled)
            {
                MessageBox.Show(
                    "检测到未安装 Android Platform Tools (adb)。\n请先点击「安装依赖」按钮安装必要组件。",
                    "缺少依赖",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ConnectButton.IsEnabled = false;
            Log($"正在连接设备: {selectedDevice.IPAddress}:{selectedDevice.Port}");
            StatusTextBlock.Text = $"正在连接 {selectedDevice.IPAddress}...";

            var result = await _scrcpyManager.ConnectDeviceAsync(selectedDevice);
            
            if (result)
            {
                MessageBox.Show($"成功连接到设备: {selectedDevice.IPAddress}", "连接成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                StatusTextBlock.Text = $"已连接到 {selectedDevice.IPAddress}";
                UpdateButtonStates();
                DeviceDataGrid.Items.Refresh();
            }
            else
            {
                MessageBox.Show($"连接失败，请检查设备是否开启ADB网络调试", "连接失败", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusTextBlock.Text = "连接失败";
            }

            ConnectButton.IsEnabled = true;
        }

        private async void StartMirrorButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDevice = DeviceDataGrid.SelectedItem as DeviceInfo;
            if (selectedDevice == null || !selectedDevice.IsConnected)
            {
                MessageBox.Show("请先连接一个设备", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_dependencyInstaller.IsScrcpyInstalled)
            {
                MessageBox.Show(
                    "检测到未安装 scrcpy。\n请先点击「安装依赖」按钮安装必要组件。",
                    "缺少依赖",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Log("正在启动投屏...");
            StatusTextBlock.Text = "正在启动投屏...";
            
            await _scrcpyManager.StartScrcpyAsync(selectedDevice);
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDevice = DeviceDataGrid.SelectedItem as DeviceInfo;
            if (selectedDevice == null)
                return;

            var result = MessageBox.Show($"确定要断开设备 {selectedDevice.IPAddress} 吗？", 
                "确认断开", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                await _scrcpyManager.DisconnectAsync(selectedDevice);
                UpdateButtonStates();
                DeviceDataGrid.Items.Refresh();
                StatusTextBlock.Text = $"已断开 {selectedDevice.IPAddress}";
            }
        }

        private void DeviceDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var selectedDevice = DeviceDataGrid.SelectedItem as DeviceInfo;
            
            ConnectButton.IsEnabled = selectedDevice != null && !selectedDevice.IsConnected;
            StartMirrorButton.IsEnabled = selectedDevice != null && selectedDevice.IsConnected;
            DisconnectButton.IsEnabled = selectedDevice != null && selectedDevice.IsConnected;
        }

        private void UpdateEmptyState()
        {
            EmptyStateGrid.Visibility = _devices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ScrcpyManager_LogMessage(object? sender, string e)
        {
            Dispatcher.Invoke(() => Log(e));
        }

        private void ScrcpyManager_ConnectionStateChanged(object? sender, bool e)
        {
            Dispatcher.Invoke(UpdateButtonStates);
        }

        private void Log(string message)
        {
            LogTextBox.AppendText(message + "\n");
            LogTextBox.ScrollToEnd();
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
            Log("日志已清空");
            Log(_dependencyInstaller.GetDependencyStatus());
        }
    }
}
