using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteDeskClient.Services;

public class ScreenCapturer : IDisposable
{
    private readonly int _screenIndex;
    private readonly int _fps;
    private readonly int _quality;
    private CancellationTokenSource? _cts;
    private Task? _captureTask;
    private bool _isRunning;
    
    public event EventHandler<byte[]>? FrameCaptured;
    public event EventHandler<string>? LogMessage;
    
    public int FrameWidth { get; private set; }
    public int FrameHeight { get; private set; }
    public bool IsRunning => _isRunning;

    public ScreenCapturer(int screenIndex = 0, int fps = 15, int quality = 80)
    {
        _screenIndex = screenIndex;
        _fps = fps;
        _quality = quality;
        
        var screen = System.Windows.Forms.Screen.AllScreens[screenIndex % System.Windows.Forms.Screen.AllScreens.Length];
        FrameWidth = screen.Bounds.Width;
        FrameHeight = screen.Bounds.Height;
    }

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _cts = new CancellationTokenSource();
        
        Log($"Starting screen capture: {FrameWidth}x{FrameHeight} at {_fps} FPS");
        
        _captureTask = CaptureLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _cts?.Cancel();
        
        Log("Stopping screen capture...");
    }

    private async Task CaptureLoopAsync(CancellationToken cancellationToken)
    {
        var frameInterval = 1000 / _fps;
        var encoder = GetJpegEncoder();
        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, _quality);

        while (!cancellationToken.IsCancellationRequested && _isRunning)
        {
            try
            {
                using var bitmap = CaptureScreen();
                using var ms = new MemoryStream();
                
                bitmap.Save(ms, encoder, encoderParams);
                var frameData = ms.ToArray();
                
                FrameCaptured?.Invoke(this, frameData);
            }
            catch (Exception ex)
            {
                Log($"Capture error: {ex.Message}");
            }

            await Task.Delay(frameInterval, cancellationToken);
        }

        Log("Screen capture stopped.");
    }

    private Bitmap CaptureScreen()
    {
        var screen = System.Windows.Forms.Screen.AllScreens[_screenIndex % System.Windows.Forms.Screen.AllScreens.Length];
        var bounds = screen.Bounds;
        
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            
            g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }
        
        return bitmap;
    }

    private ImageCodecInfo GetJpegEncoder()
    {
        var codecs = ImageCodecInfo.GetImageDecoders();
        foreach (var codec in codecs)
        {
            if (codec.FormatID == ImageFormat.Jpeg.Guid)
            {
                return codec;
            }
        }
        return codecs[0];
    }

    public byte[] CaptureSingleFrame()
    {
        using var bitmap = CaptureScreen();
        using var ms = new MemoryStream();
        
        var encoder = GetJpegEncoder();
        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, _quality);
        
        bitmap.Save(ms, encoder, encoderParams);
        return ms.ToArray();
    }

    public static int GetScreenCount()
    {
        return System.Windows.Forms.Screen.AllScreens.Length;
    }

    public static Rectangle GetScreenBounds(int screenIndex)
    {
        var screen = System.Windows.Forms.Screen.AllScreens[screenIndex % System.Windows.Forms.Screen.AllScreens.Length];
        return screen.Bounds;
    }

    private void Log(string message)
    {
        LogMessage?.Invoke(this, message);
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
