
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using QuickScrcpy.Models;

namespace QuickScrcpy.Services
{
    public class ScrcpyManager
    {
        public event EventHandler<string>? LogMessage;
        public event EventHandler<bool>? ConnectionStateChanged;

        private Process? _scrcpyProcess;
        private string? _adbPath;
        private string? _scrcpyPath;

        public bool IsConnected { get; private set; }
        public DeviceInfo? ConnectedDevice { get; private set; }

        public ScrcpyManager()
        {
            InitializePaths();
        }

        private void InitializePaths()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            _adbPath = Path.Combine(baseDir, "adb", "adb.exe");
            if (!File.Exists(_adbPath))
            {
                _adbPath = Path.Combine(baseDir, "platform-tools", "adb.exe");
            }
            
            _scrcpyPath = Path.Combine(baseDir, "scrcpy", "scrcpy.exe");
            if (!File.Exists(_scrcpyPath))
            {
                _scrcpyPath = "scrcpy.exe";
            }
        }

        public async Task<bool> ConnectDeviceAsync(DeviceInfo device)
        {
            if (device == null || string.IsNullOrEmpty(device.IPAddress))
            {
                Log("设备信息无效");
                return false;
            }

            Log($"正在连接设备: {device.IPAddress}:{device.Port}");

            try
            {
                var adbResult = await RunAdbCommandAsync($"connect {device.IPAddress}:{device.Port}");
                Log($"ADB连接结果: {adbResult}");

                if (adbResult.Contains("connected") || adbResult.Contains("already connected"))
                {
                    ConnectedDevice = device;
                    IsConnected = true;
                    device.IsConnected = true;
                    ConnectionStateChanged?.Invoke(this, true);
                    Log("设备连接成功");
                    return true;
                }
                else
                {
                    Log("设备连接失败，请检查设备是否开启ADB调试");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"连接错误: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StartScrcpyAsync(DeviceInfo device, bool fullScreen = false, int maxSize = 1920, int bitRate = 8000000)
        {
            if (!IsConnected || ConnectedDevice == null)
            {
                Log("请先连接设备");
                return false;
            }

            Log("正在启动投屏...");

            try
            {
                if (_scrcpyProcess != null && !_scrcpyProcess.HasExited)
                {
                    _scrcpyProcess.Kill();
                    _scrcpyProcess.Dispose();
                }

                var arguments = $"-s {device.IPAddress}:{device.Port} --max-size={maxSize} --bit-rate={bitRate}";
                if (fullScreen)
                {
                    arguments += " --fullscreen";
                }

                _scrcpyProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _scrcpyPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                _scrcpyProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Log($"scrcpy: {e.Data}");
                };

                _scrcpyProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Log($"scrcpy错误: {e.Data}");
                };

                _scrcpyProcess.Exited += (s, e) =>
                {
                    Log("投屏已关闭");
                };

                _scrcpyProcess.Start();
                _scrcpyProcess.BeginOutputReadLine();
                _scrcpyProcess.BeginErrorReadLine();

                Log("投屏已启动，您可以通过鼠标和键盘控制设备");
                return true;
            }
            catch (Exception ex)
            {
                Log($"启动投屏失败: {ex.Message}");
                Log("请确保scrcpy已安装并添加到系统PATH，或放置在程序目录下");
                return false;
            }
        }

        public async Task DisconnectAsync(DeviceInfo device)
        {
            if (device == null)
                return;

            try
            {
                if (_scrcpyProcess != null && !_scrcpyProcess.HasExited)
                {
                    _scrcpyProcess.Kill();
                    _scrcpyProcess.Dispose();
                    _scrcpyProcess = null;
                }

                await RunAdbCommandAsync($"disconnect {device.IPAddress}:{device.Port}");
                
                if (ConnectedDevice?.IPAddress == device.IPAddress)
                {
                    ConnectedDevice = null;
                    IsConnected = false;
                }
                device.IsConnected = false;
                ConnectionStateChanged?.Invoke(this, false);
                Log($"设备 {device.IPAddress} 已断开连接");
            }
            catch (Exception ex)
            {
                Log($"断开连接时出错: {ex.Message}");
            }
        }

        private async Task<string> RunAdbCommandAsync(string arguments)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _adbPath ?? "adb.exe",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                return string.IsNullOrEmpty(output) ? error : output;
            }
            catch (Exception ex)
            {
                return $"错误: {ex.Message}";
            }
        }

        private void Log(string message)
        {
            LogMessage?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
