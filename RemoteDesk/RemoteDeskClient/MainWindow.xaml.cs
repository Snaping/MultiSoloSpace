using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using RemoteDeskClient.Models;
using RemoteDeskClient.Services;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Clipboard = System.Windows.Clipboard;

namespace RemoteDeskClient;

public partial class MainWindow : System.Windows.Window
{
    private ConnectionManager? _connectionManager;
    private ScreenCapturer? _screenCapturer;
    private RemoteController? _remoteController;
    
    private ClientMode _currentMode = ClientMode.None;
    private bool _isMouseDown = false;
    private int _lastMouseX = 0;
    private int _lastMouseY = 0;

    public MainWindow()
    {
        InitializeComponent();
        
        Log("RemoteDesk Client initialized.");
        Log("Select mode and click Connect to start.");
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        Dispatcher.Invoke(() =>
        {
            txtLog.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            txtLog.ScrollToEnd();
        });
    }

    private async void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        var signalingServer = txtSignalingServer.Text.Trim();
        var roomId = txtRoomId.Text.Trim();
        
        if (string.IsNullOrEmpty(signalingServer))
        {
            Log("Please enter signaling server URL.");
            return;
        }
        
        if (string.IsNullOrEmpty(roomId))
        {
            Log("Please enter Room ID.");
            return;
        }

        _currentMode = rbController.IsChecked == true ? ClientMode.Controller : ClientMode.Controlled;
        
        Log($"Starting in {_currentMode} mode...");
        Log($"Signaling Server: {signalingServer}");
        Log($"Room ID: {roomId}");

        try
        {
            _connectionManager = new ConnectionManager(signalingServer);
            
            _connectionManager.LogMessage += (s, msg) => Log(msg);
            _connectionManager.ConnectionStateChanged += ConnectionManager_ConnectionStateChanged;
            _connectionManager.VideoFrameReceived += ConnectionManager_VideoFrameReceived;
            _connectionManager.ControlMessageReceived += ConnectionManager_ControlMessageReceived;

            var connected = await _connectionManager.ConnectToSignalingServerAsync();
            if (!connected)
            {
                Log("Failed to connect to signaling server.");
                return;
            }

            if (_currentMode == ClientMode.Controlled)
            {
                var fps = GetSelectedFps();
                var quality = (int)sldQuality.Value;
                
                _screenCapturer = new ScreenCapturer(0, fps, quality);
                _screenCapturer.LogMessage += (s, msg) => Log(msg);
                _screenCapturer.FrameCaptured += ScreenCapturer_FrameCaptured;
                _screenCapturer.Start();
                
                _remoteController = new RemoteController();
                txtRemoteScreen.Text = "Screen Sharing - Local Screen";
            }
            else
            {
                txtRemoteScreen.Text = "Remote Screen - Waiting for connection...";
            }

            await _connectionManager.JoinRoomAsync(roomId, _currentMode);

            txtModeDisplay.Text = $"Mode: {_currentMode}";
            btnConnect.IsEnabled = false;
            btnDisconnect.IsEnabled = true;
            txtSignalingServer.IsEnabled = false;
            txtRoomId.IsEnabled = false;
            rbController.IsEnabled = false;
            rbControlled.IsEnabled = false;
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnDisconnect_Click(object sender, RoutedEventArgs e)
    {
        Log("Disconnecting...");
        
        try
        {
            _screenCapturer?.Stop();
            _screenCapturer?.Dispose();
            _screenCapturer = null;
            
            if (_connectionManager != null)
            {
                await _connectionManager.DisconnectAsync();
                _connectionManager = null;
            }

            _remoteController = null;
            
            txtStatus.Text = "Disconnected";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            txtModeDisplay.Text = "Mode: None";
            
            btnConnect.IsEnabled = true;
            btnDisconnect.IsEnabled = false;
            txtSignalingServer.IsEnabled = true;
            txtRoomId.IsEnabled = true;
            rbController.IsEnabled = true;
            rbControlled.IsEnabled = true;
            
            imgRemoteScreen.Source = null;
            txtRemoteScreen.Text = "Remote Screen";
        }
        catch (Exception ex)
        {
            Log($"Error during disconnect: {ex.Message}");
        }
    }

    private void ConnectionManager_ConnectionStateChanged(object? sender, ConnectionState state)
    {
        Dispatcher.Invoke(() =>
        {
            txtStatus.Text = state.ToString();
            
            switch (state)
            {
                case ConnectionState.Connected:
                    txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    if (_currentMode == ClientMode.Controller)
                    {
                        txtRemoteScreen.Text = "Remote Screen - Connected";
                    }
                    else
                    {
                        txtRemoteScreen.Text = "Screen Sharing - Connected";
                    }
                    break;
                case ConnectionState.Connecting:
                    txtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    break;
                case ConnectionState.Disconnected:
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                    break;
            }
        });
    }

    private void ConnectionManager_VideoFrameReceived(object? sender, byte[] frameData)
    {
        if (_currentMode != ClientMode.Controller) return;

        try
        {
            using var ms = new MemoryStream(frameData);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();

            Dispatcher.Invoke(() =>
            {
                imgRemoteScreen.Source = bitmap;
            });
        }
        catch (Exception ex)
        {
            Log($"Error displaying frame: {ex.Message}");
        }
    }

    private void ConnectionManager_ControlMessageReceived(object? sender, ControlMessage message)
    {
        if (_currentMode != ClientMode.Controlled) return;
        
        _remoteController?.HandleControlMessage(message);
    }

    private async void ScreenCapturer_FrameCaptured(object? sender, byte[] frameData)
    {
        if (_currentMode != ClientMode.Controlled || _connectionManager == null) return;
        
        await _connectionManager.SendVideoFrameAsync(frameData);
    }

    private async void RemoteScreen_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_currentMode != ClientMode.Controller || _connectionManager == null) return;
        
        var pos = e.GetPosition(imgRemoteScreen);
        var (x, y) = GetRemoteCoordinates(pos);
        
        _isMouseDown = true;
        _lastMouseX = x;
        _lastMouseY = y;
        
        var button = e.ChangedButton switch
        {
            MouseButton.Left => 0,
            MouseButton.Right => 1,
            MouseButton.Middle => 2,
            _ => 0
        };
        
        await _connectionManager.SendControlMessageAsync(new ControlMessage
        {
            Type = "mousedown",
            X = x,
            Y = y,
            Button = button
        });
        
        imgRemoteScreen.Focus();
    }

    private async void RemoteScreen_MouseMove(object sender, MouseEventArgs e)
    {
        if (_currentMode != ClientMode.Controller || _connectionManager == null) return;
        
        var pos = e.GetPosition(imgRemoteScreen);
        var (x, y) = GetRemoteCoordinates(pos);
        
        if (_isMouseDown || x != _lastMouseX || y != _lastMouseY)
        {
            _lastMouseX = x;
            _lastMouseY = y;
            
            await _connectionManager.SendControlMessageAsync(new ControlMessage
            {
                Type = "mousemove",
                X = x,
                Y = y
            });
        }
    }

    private async void RemoteScreen_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentMode != ClientMode.Controller || _connectionManager == null) return;
        
        _isMouseDown = false;
        
        var pos = e.GetPosition(imgRemoteScreen);
        var (x, y) = GetRemoteCoordinates(pos);
        
        var button = e.ChangedButton switch
        {
            MouseButton.Left => 0,
            MouseButton.Right => 1,
            MouseButton.Middle => 2,
            _ => 0
        };
        
        await _connectionManager.SendControlMessageAsync(new ControlMessage
        {
            Type = "mouseup",
            X = x,
            Y = y,
            Button = button
        });
    }

    private async void RemoteScreen_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_currentMode != ClientMode.Controller || _connectionManager == null) return;
        
        await _connectionManager.SendControlMessageAsync(new ControlMessage
        {
            Type = "mousescroll",
            Y = e.Delta / 120
        });
    }

    private async void RemoteScreen_KeyDown(object sender, KeyEventArgs e)
    {
        if (_currentMode != ClientMode.Controller || _connectionManager == null) return;
        
        var keyCode = KeyInterop.VirtualKeyFromKey(e.Key);
        
        await _connectionManager.SendControlMessageAsync(new ControlMessage
        {
            Type = "keydown",
            KeyCode = keyCode
        });
    }

    private async void RemoteScreen_KeyUp(object sender, KeyEventArgs e)
    {
        if (_currentMode != ClientMode.Controller || _connectionManager == null) return;
        
        var keyCode = KeyInterop.VirtualKeyFromKey(e.Key);
        
        await _connectionManager.SendControlMessageAsync(new ControlMessage
        {
            Type = "keyup",
            KeyCode = keyCode
        });
    }

    private (int X, int Y) GetRemoteCoordinates(System.Windows.Point localPos)
    {
        var bitmap = imgRemoteScreen.Source as BitmapSource;
        if (bitmap == null) return ((int)localPos.X, (int)localPos.Y);
        
        var actualWidth = imgRemoteScreen.ActualWidth;
        var actualHeight = imgRemoteScreen.ActualHeight;
        
        if (actualWidth <= 0 || actualHeight <= 0) return ((int)localPos.X, (int)localPos.Y);
        
        var scaleX = bitmap.PixelWidth / actualWidth;
        var scaleY = bitmap.PixelHeight / actualHeight;
        
        var remoteX = (int)(localPos.X * scaleX);
        var remoteY = (int)(localPos.Y * scaleY);
        
        return (remoteX, remoteY);
    }

    private int GetSelectedFps()
    {
        return cmbFps.SelectedIndex switch
        {
            0 => 5,
            1 => 10,
            2 => 15,
            3 => 30,
            _ => 15
        };
    }

    private async void BtnSendClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (_connectionManager == null)
        {
            Log("Not connected.");
            return;
        }
        
        try
        {
            var text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                await _connectionManager.SendControlMessageAsync(new ControlMessage
                {
                    Type = "textinput",
                    Text = text
                });
                Log($"Sent clipboard text ({text.Length} chars)");
            }
            else
            {
                Log("Clipboard is empty.");
            }
        }
        catch (Exception ex)
        {
            Log($"Error sending clipboard: {ex.Message}");
        }
    }

    private void BtnReceiveClipboard_Click(object sender, RoutedEventArgs e)
    {
        Log("Clipboard receive handled by remote control messages.");
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_connectionManager != null)
        {
            await _connectionManager.DisconnectAsync();
        }
        
        _screenCapturer?.Stop();
        _screenCapturer?.Dispose();
    }
}
