Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

public class WindowCapture {
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
    
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    
    const int SW_RESTORE = 9;
    
    public static void CaptureWindow(string windowTitle, string outputPath) {
        IntPtr hWnd = FindWindow(null, windowTitle);
        if (hWnd == IntPtr.Zero) {
            Console.WriteLine("Window not found: " + windowTitle);
            return;
        }
        
        ShowWindow(hWnd, SW_RESTORE);
        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(500);
        
        RECT rect;
        GetWindowRect(hWnd, out rect);
        
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        
        Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        Graphics g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        
        bitmap.Save(outputPath, ImageFormat.Png);
        Console.WriteLine("Screenshot saved to: " + outputPath);
        
        g.Dispose();
        bitmap.Dispose();
    }
}
"@ -ReferencedAssemblies "System.Drawing", "System.Windows.Forms"

Start-Sleep -Seconds 3
[WindowCapture]::CaptureWindow("RemoteDesk Client", "d:\WorkSpace\Agent\MultiSoloSpace\MultiSoloSpace\RemoteDesk\WebRTC_RemoteDesk_Screenshot.png")
