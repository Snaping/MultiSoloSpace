using System;
using System.Runtime.InteropServices;
using System.Threading;
using RemoteDeskClient.Models;

namespace RemoteDeskClient.Services;

public class RemoteController
{
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private int _remoteScreenWidth = 1920;
    private int _remoteScreenHeight = 1080;

    public RemoteController()
    {
        _screenWidth = GetSystemMetrics(SM_CXSCREEN);
        _screenHeight = GetSystemMetrics(SM_CYSCREEN);
    }

    public void SetRemoteScreenSize(int width, int height)
    {
        _remoteScreenWidth = width;
        _remoteScreenHeight = height;
    }

    public void HandleControlMessage(ControlMessage message)
    {
        switch (message.Type.ToLower())
        {
            case "mousemove":
                MouseMove(message.X, message.Y);
                break;
            case "mousedown":
                MouseDown(message.Button);
                break;
            case "mouseup":
                MouseUp(message.Button);
                break;
            case "mouseclick":
                MouseClick(message.X, message.Y, message.Button);
                break;
            case "mousedoubleclick":
                MouseDoubleClick(message.X, message.Y, message.Button);
                break;
            case "mousescroll":
                MouseScroll(message.Y);
                break;
            case "keydown":
                KeyDown(message.KeyCode);
                break;
            case "keyup":
                KeyUp(message.KeyCode);
                break;
            case "keypress":
                KeyPress(message.Text);
                break;
            case "textinput":
                TypeText(message.Text);
                break;
        }
    }

    private void MouseMove(int x, int y)
    {
        var screenX = (int)((double)x / _remoteScreenWidth * _screenWidth);
        var screenY = (int)((double)y / _remoteScreenHeight * _screenHeight);
        
        SetCursorPos(screenX, screenY);
    }

    private void MouseDown(int button)
    {
        uint mouseEvent = button switch
        {
            0 => MOUSEEVENTF_LEFTDOWN,
            1 => MOUSEEVENTF_RIGHTDOWN,
            2 => MOUSEEVENTF_MIDDLEDOWN,
            _ => MOUSEEVENTF_LEFTDOWN
        };
        
        mouse_event(mouseEvent, 0, 0, 0, UIntPtr.Zero);
    }

    private void MouseUp(int button)
    {
        uint mouseEvent = button switch
        {
            0 => MOUSEEVENTF_LEFTUP,
            1 => MOUSEEVENTF_RIGHTUP,
            2 => MOUSEEVENTF_MIDDLEUP,
            _ => MOUSEEVENTF_LEFTUP
        };
        
        mouse_event(mouseEvent, 0, 0, 0, UIntPtr.Zero);
    }

    private void MouseClick(int x, int y, int button)
    {
        MouseMove(x, y);
        Thread.Sleep(50);
        MouseDown(button);
        Thread.Sleep(50);
        MouseUp(button);
    }

    private void MouseDoubleClick(int x, int y, int button)
    {
        MouseClick(x, y, button);
        Thread.Sleep(100);
        MouseClick(x, y, button);
    }

    private void MouseScroll(int delta)
    {
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)(delta * WHEEL_DELTA), UIntPtr.Zero);
    }

    private void KeyDown(int keyCode)
    {
        keybd_event((byte)keyCode, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
    }

    private void KeyUp(int keyCode)
    {
        keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private void KeyPress(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        foreach (var c in text)
        {
            var keyCode = VkKeyScan(c);
            if (keyCode != -1)
            {
                var isShift = (keyCode & 0x100) != 0;
                var virtualKey = (byte)(keyCode & 0xFF);
                
                if (isShift)
                {
                    KeyDown(VK_SHIFT);
                }
                
                KeyDown(virtualKey);
                KeyUp(virtualKey);
                
                if (isShift)
                {
                    KeyUp(VK_SHIFT);
                }
            }
        }
    }

    public void TypeText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        foreach (var c in text)
        {
            var inputs = new INPUT[2];
            
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].ki.wScan = (ushort)c;
            inputs[0].ki.dwFlags = KEYEVENTF_UNICODE;
            
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].ki.wScan = (ushort)c;
            inputs[1].ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
            
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
            
            Thread.Sleep(10);
        }
    }

    public void SendKeyCombination(params int[] keys)
    {
        foreach (var key in keys)
        {
            KeyDown(key);
        }
        
        Thread.Sleep(50);
        
        for (var i = keys.Length - 1; i >= 0; i--)
        {
            KeyUp(keys[i]);
        }
    }

    public void SendCtrlC() => SendKeyCombination(VK_CONTROL, 'C');
    public void SendCtrlV() => SendKeyCombination(VK_CONTROL, 'V');
    public void SendCtrlA() => SendKeyCombination(VK_CONTROL, 'A');
    public void SendCtrlX() => SendKeyCombination(VK_CONTROL, 'X');
    public void SendEnter() => KeyDown(VK_RETURN);

    #region P/Invoke Constants and Methods

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const int WHEEL_DELTA = 120;
    
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_RETURN = 0x0D;

    private const int INPUT_KEYBOARD = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT
    {
        [FieldOffset(0)]
        public int type;
        [FieldOffset(4)]
        public MOUSEINPUT mi;
        [FieldOffset(4)]
        public KEYBDINPUT ki;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint cButtons, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

    #endregion
}
