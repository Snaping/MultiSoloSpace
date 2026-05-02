
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace QuickScrcpy.Services
{
    public class DependencyInstaller
    {
        public event EventHandler<string>? StatusChanged;
        public event EventHandler<int>? ProgressChanged;
        public event EventHandler<bool>? InstallationCompleted;

        private readonly HttpClient _httpClient;
        private readonly string _toolsDir;
        private readonly string _scrcpyDir;
        private readonly string _platformToolsDir;

        public string? AdbPath { get; private set; }
        public string? ScrcpyPath { get; private set; }
        public bool IsAdbInstalled { get; private set; }
        public bool IsScrcpyInstalled { get; private set; }
        public bool IsInstalling { get; private set; }

        private const string ScrcpyLatestReleaseUrl = "https://api.github.com/repos/Genymobile/scrcpy/releases/latest";
        private const string PlatformToolsUrl = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip";

        public DependencyInstaller()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "QuickScrcpy/1.0");
            
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _toolsDir = Path.Combine(baseDir, "tools");
            _scrcpyDir = Path.Combine(_toolsDir, "scrcpy");
            _platformToolsDir = Path.Combine(_toolsDir, "platform-tools");
            
            CheckDependencies();
        }

        public void CheckDependencies()
        {
            IsAdbInstalled = false;
            IsScrcpyInstalled = false;
            AdbPath = null;
            ScrcpyPath = null;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            var possibleAdbPaths = new List<string>
            {
                Path.Combine(_platformToolsDir, "adb.exe"),
                Path.Combine(baseDir, "platform-tools", "adb.exe"),
                Path.Combine(baseDir, "adb", "adb.exe"),
                "adb.exe"
            };

            foreach (var path in possibleAdbPaths)
            {
                try
                {
                    if (File.Exists(path) || IsCommandInPath(path))
                    {
                        AdbPath = path;
                        IsAdbInstalled = true;
                        break;
                    }
                }
                catch
                {
                }
            }

            var possibleScrcpyPaths = new List<string>
            {
                Path.Combine(_scrcpyDir, "scrcpy.exe"),
                Path.Combine(baseDir, "scrcpy", "scrcpy.exe"),
                "scrcpy.exe"
            };

            foreach (var path in possibleScrcpyPaths)
            {
                try
                {
                    if (File.Exists(path) || IsCommandInPath(path))
                    {
                        ScrcpyPath = path;
                        IsScrcpyInstalled = true;
                        break;
                    }
                }
                catch
                {
                }
            }
        }

        private bool IsCommandInPath(string command)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = command,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0 && !string.IsNullOrEmpty(output.Trim());
            }
            catch
            {
                return false;
            }
        }

        public async Task InstallAllDependenciesAsync()
        {
            if (IsInstalling)
                return;

            IsInstalling = true;
            OnStatusChanged("开始检查依赖...");

            try
            {
                if (!IsAdbInstalled)
                {
                    OnStatusChanged("正在安装 Android Platform Tools (adb)...");
                    await InstallPlatformToolsAsync();
                }
                else
                {
                    OnStatusChanged("adb 已安装");
                }

                OnProgressChanged(50);

                if (!IsScrcpyInstalled)
                {
                    OnStatusChanged("正在安装 scrcpy...");
                    await InstallScrcpyAsync();
                }
                else
                {
                    OnStatusChanged("scrcpy 已安装");
                }

                OnProgressChanged(100);
                OnStatusChanged("所有依赖安装完成！");
                OnInstallationCompleted(true);
            }
            catch (Exception ex)
            {
                OnStatusChanged($"安装失败: {ex.Message}");
                OnInstallationCompleted(false);
            }
            finally
            {
                IsInstalling = false;
                CheckDependencies();
            }
        }

        private async Task InstallPlatformToolsAsync()
        {
            OnStatusChanged("正在下载 Android Platform Tools...");
            
            var zipPath = Path.Combine(Path.GetTempPath(), "platform-tools-latest.zip");
            
            await DownloadFileAsync(PlatformToolsUrl, zipPath, 0, 50);
            
            OnStatusChanged("正在解压 Platform Tools...");
            
            EnsureDirectoryExists(_toolsDir);
            ExtractZipToDirectory(zipPath, _toolsDir);
            
            try
            {
                File.Delete(zipPath);
            }
            catch
            {
            }
            
            AdbPath = Path.Combine(_platformToolsDir, "adb.exe");
            IsAdbInstalled = true;
            OnStatusChanged("Android Platform Tools 安装完成");
        }

        private async Task InstallScrcpyAsync()
        {
            OnStatusChanged("正在获取最新版 scrcpy 信息...");
            
            var downloadUrl = await GetLatestScrcpyDownloadUrlAsync();
            
            if (string.IsNullOrEmpty(downloadUrl))
            {
                throw new Exception("无法获取 scrcpy 下载链接");
            }

            OnStatusChanged($"正在下载 scrcpy...");
            
            var zipPath = Path.Combine(Path.GetTempPath(), "scrcpy-latest.zip");
            
            await DownloadFileAsync(downloadUrl, zipPath, 50, 80);
            
            OnStatusChanged("正在解压 scrcpy...");
            
            EnsureDirectoryExists(_toolsDir);
            
            var tempExtractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            ExtractZipToDirectory(zipPath, tempExtractDir);
            
            var dirs = Directory.GetDirectories(tempExtractDir);
            if (dirs.Length > 0)
            {
                var scrcpySourceDir = dirs[0];
                CopyDirectory(scrcpySourceDir, _scrcpyDir, true);
            }
            
            try
            {
                File.Delete(zipPath);
                Directory.Delete(tempExtractDir, true);
            }
            catch
            {
            }
            
            ScrcpyPath = Path.Combine(_scrcpyDir, "scrcpy.exe");
            IsScrcpyInstalled = true;
            OnStatusChanged("scrcpy 安装完成");
        }

        private async Task<string?> GetLatestScrcpyDownloadUrlAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(ScrcpyLatestReleaseUrl);
                
                var win64Pattern = "scrcpy-win64";
                var lines = response.Split('\n');
                
                foreach (var line in lines)
                {
                    if (line.Contains(win64Pattern) && line.Contains("browser_download_url"))
                    {
                        var startIndex = line.IndexOf("https://");
                        if (startIndex >= 0)
                        {
                            var endIndex = line.IndexOf('"', startIndex);
                            if (endIndex > startIndex)
                            {
                                return line.Substring(startIndex, endIndex - startIndex);
                            }
                        }
                    }
                }
                
                return "https://github.com/Genymobile/scrcpy/releases/download/v2.4/scrcpy-win64-v2.4.zip";
            }
            catch
            {
                return "https://github.com/Genymobile/scrcpy/releases/download/v2.4/scrcpy-win64-v2.4.zip";
            }
        }

        private async Task DownloadFileAsync(string url, string destinationPath, int startProgress, int endProgress)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var canReportProgress = totalBytes != -1;
                
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                
                var buffer = new byte[8192];
                long totalBytesRead = 0;
                int bytesRead;
                
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    
                    if (canReportProgress)
                    {
                        var progress = (int)((double)totalBytesRead / totalBytes * (endProgress - startProgress)) + startProgress;
                        OnProgressChanged(progress);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"下载失败: {ex.Message}");
            }
        }

        private void ExtractZipToDirectory(string zipPath, string extractDir)
        {
            using var zipFile = new ZipFile(zipPath);
            foreach (ZipEntry entry in zipFile)
            {
                if (!entry.IsFile)
                    continue;

                var entryPath = Path.Combine(extractDir, entry.Name);
                var entryDir = Path.GetDirectoryName(entryPath);
                
                if (entryDir != null)
                {
                    EnsureDirectoryExists(entryDir);
                }

                using var entryStream = zipFile.GetInputStream(entry);
                using var fileStream = new FileStream(entryPath, FileMode.Create, FileAccess.Write);
                
                var buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
                return;

            EnsureDirectoryExists(destDir);

            foreach (var file in dir.GetFiles())
            {
                var targetPath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetPath, true);
            }

            if (recursive)
            {
                foreach (var subDir in dir.GetDirectories())
                {
                    var targetSubDir = Path.Combine(destDir, subDir.Name);
                    CopyDirectory(subDir.FullName, targetSubDir, recursive);
                }
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public string GetDependencyStatus()
        {
            CheckDependencies();
            var status = "依赖状态:\n";
            status += $"  adb: {(IsAdbInstalled ? "✓ 已安装" : "✗ 未安装")}\n";
            status += $"  scrcpy: {(IsScrcpyInstalled ? "✓ 已安装" : "✗ 未安装")}";
            return status;
        }

        protected virtual void OnStatusChanged(string message)
        {
            StatusChanged?.Invoke(this, message);
        }

        protected virtual void OnProgressChanged(int progress)
        {
            ProgressChanged?.Invoke(this, progress);
        }

        protected virtual void OnInstallationCompleted(bool success)
        {
            InstallationCompleted?.Invoke(this, success);
        }
    }
}
