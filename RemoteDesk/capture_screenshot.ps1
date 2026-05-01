Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$processName = "RemoteDesk"
$processes = Get-Process -Name $processName -ErrorAction SilentlyContinue

if ($processes.Count -eq 0) {
    Write-Host "RemoteDesk process not found."
    exit 1
}

$process = $processes[0]
$handle = $process.MainWindowHandle

Write-Host "Found RemoteDesk window: $($process.MainWindowTitle)"

Add-Type @"
    using System;
    using System.Runtime.InteropServices;
    using System.Drawing;
    using System.Drawing.Imaging;
    
    public class WindowCapture {
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, out RECT rect);
        
        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        public static Bitmap CaptureWindow(IntPtr hWnd) {
            RECT rect;
            GetWindowRect(hWnd, out rect);
            
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            
            using (Graphics g = Graphics.FromImage(bitmap)) {
                IntPtr hdc = g.GetHdc();
                PrintWindow(hWnd, hdc, 0);
                g.ReleaseHdc(hdc);
            }
            
            return bitmap;
        }
    }
"@ -ReferencedAssemblies "System.Drawing.dll", "System.Windows.Forms.dll"

$bitmap = [WindowCapture]::CaptureWindow($handle)
$outputPath = Join-Path $PWD "RemoteDesk_Screenshot.png"
$bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()

Write-Host "Screenshot saved to: $outputPath"
