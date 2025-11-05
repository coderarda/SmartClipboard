using System;
using System.IO;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class NotifyIconManager {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATA {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    public static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    public static extern bool DestroyMenu(IntPtr hMenu);

    public const uint NIM_ADD = 0x00000000;
    public const uint NIM_DELETE = 0x00000002;
    public const uint NIF_MESSAGE = 0x00000001;
    public const uint NIF_ICON = 0x00000002;
    public const uint NIF_TIP = 0x00000004;

    public const uint MF_STRING = 0x00000000;
    public const uint MF_SEPARATOR = 0x00000800;
    public const uint TPM_RETURNCMD = 0x0100;
    public const uint TPM_RIGHTBUTTON = 0x0002;

    public const uint MENU_SHOW = 1000;
    public const uint MENU_EXIT = 1001;

    private IntPtr _hwnd;
    private NOTIFYICONDATA _nid;

    public NotifyIconManager(IntPtr hwnd) {
        _hwnd = hwnd;
    }

    public void CreateTrayIcon(string tooltip, string iconPath) {
        _nid = new NOTIFYICONDATA();
        _nid.cbSize = Marshal.SizeOf(_nid);
        _nid.hWnd = _hwnd;
        _nid.uID = 1;
        _nid.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
        _nid.uCallbackMessage = 0x400; // custom message ID
        
        // Get the full path relative to the application directory
        string fullIconPath = Path.Combine(AppContext.BaseDirectory, iconPath);
        _nid.hIcon = new System.Drawing.Icon(fullIconPath).Handle;
        _nid.szTip = tooltip;

        Shell_NotifyIcon(NIM_ADD, ref _nid);
    }

    public void RemoveTrayIcon() {
        Shell_NotifyIcon(NIM_DELETE, ref _nid);
    }

    public uint ShowContextMenu() {
        IntPtr hMenu = CreatePopupMenu();
        
        AppendMenu(hMenu, MF_STRING, MENU_SHOW, "Show");
        AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
        AppendMenu(hMenu, MF_STRING, MENU_EXIT, "Exit");
        GetCursorPos(out POINT pt);
        SetForegroundWindow(_hwnd);

        uint cmd = (uint)TrackPopupMenu(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);
        
        DestroyMenu(hMenu);
        
        return cmd;
    }
}