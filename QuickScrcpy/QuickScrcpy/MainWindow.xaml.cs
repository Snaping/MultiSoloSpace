
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
        private readonly ObservableCollection<DeviceInfo> _devices;

        public MainWindow()
        {
            InitializeComponent();
            
            _scanner = new NetworkScanner();
            _scrcpyManager = new ScrcpyManager();
            _devices = new ObservableCollection<DeviceInfo>();

            DeviceDataGrid.ItemsSource = _devices;
            
            _scanner.DeviceFound += Scanner_DeviceFound;
            _scanner.ScanProgressChanged += Scanner_ScanProgressChanged;
            _scanner.ScanCompleted += Scanner_ScanCompleted;
            
            _scrcpyManager.LogMessage += ScrcpyManager_LogMessage;
            _scrcpyManager.ConnectionStateChanged += ScrcpyManager_ConnectionStateChanged;
            
            Log("QuickScrcpy 初始化完成，准备就绪");
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
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
                Log($"发现设备: {e.IPAddress} - {e.DeviceName}");
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
        }
    }
}
