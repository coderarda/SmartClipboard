using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SmartClipboard {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window {
        private readonly IntPtr _hwnd;
        private readonly NotifyIconManager _tray;
        private readonly AppWindow _appWindow;

        private int windowWidth = 300;
        private int windowHeight = 600;

        // Windows message constants
        private const int WM_USER = 0x0400;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONUP = 0x0205;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private const int GWL_WNDPROC = -4;
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        private IntPtr _oldWndProc;
        private WndProcDelegate _newWndProc;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public MainWindow() {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            
            Title = "SmartClipboard";

            // Set window size using AppWindow
            _hwnd = WindowNative.GetWindowHandle(this);

            // Hook into window messages
            _newWndProc = new WndProcDelegate(WndProc);
            _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));

            // Create tray icon
            _tray = new NotifyIconManager(_hwnd);
            _tray.CreateTrayIcon("Smart Clipboard", "SMARTCLIPBOARD.ico");

            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            _appWindow.Resize(new SizeInt32(windowWidth, windowHeight));

            Clipboard.ContentChanged += new EventHandler<object>(OnClipboardContentChanged);

            // Position window near taskbar on startup
            PositionWindowNearTaskbar();

            // Handle window closing to hide to tray instead
            _appWindow.Closing += OnWindowClosing;

            // Customize the title bar to look native
        }

        private async void OnClipboardContentChanged(object? sender, object e) {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if(dataPackageView.Contains(StandardDataFormats.Text)) {
                var text = await dataPackageView.GetTextAsync();
                if(!ClipboardListView.Items.Any(item => (item as ClipboardContentView)?.ClipboardContent == text)) {
                    ClipboardListView.Items.Add(new ClipboardContentView(text));
                }
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
            // Handle tray icon messages
            if(msg == WM_USER) {
                int notificationMsg = (int)lParam & 0xFFFF;

                if(notificationMsg == WM_LBUTTONUP) {
                    // Toggle window visibility on left-click
                    if(_appWindow.IsVisible) {
                        _appWindow.Hide();
                    }
                    else {
                        PositionWindowNearTaskbar();
                        _appWindow.Show();
                        BringToFront();
                    }
                }
                else if(notificationMsg == WM_RBUTTONUP) {
                    // Show context menu on right-click
                    uint cmd = _tray.ShowContextMenu();

                    if(cmd == NotifyIconManager.MENU_SHOW) {
                        if(!_appWindow.IsVisible) {
                            PositionWindowNearTaskbar();
                            _appWindow.Show();
                            BringToFront();
                        }
                    }
                    else if(cmd == NotifyIconManager.MENU_EXIT) {
                        ExitApplication();
                    }
                }
            }

            // Call original window procedure
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void ExitApplication() {
            // Remove tray icon
            _tray.RemoveTrayIcon();

            // Actually close the application
            Application.Current.Exit();
        }

        private void OnWindowClosing(AppWindow sender, AppWindowClosingEventArgs args) {
            // Prevent actual closing and hide to tray instead
            args.Cancel = true;
            _appWindow.Hide();
        }

        private void PositionWindowNearTaskbar() {
            // Get taskbar handle and position
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", "SmartClipboard");

            // Get monitor info for the window
            IntPtr monitor = MonitorFromWindow(_hwnd, MONITOR_DEFAULTTONEAREST);
            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            GetMonitorInfo(monitor, ref monitorInfo);

            RECT workArea = monitorInfo.rcWork;
            RECT monitorRect = monitorInfo.rcMonitor;


            int margin = 10;

            int x, y;

            if(taskbarHandle != IntPtr.Zero) {
                GetWindowRect(taskbarHandle, out RECT taskbarRect);

                // Determine taskbar position
                int taskbarWidth = taskbarRect.Right - taskbarRect.Left;
                int taskbarHeight = taskbarRect.Bottom - taskbarRect.Top;

                // Bottom taskbar (most common)
                if(taskbarRect.Bottom == monitorRect.Bottom && taskbarRect.Top > monitorRect.Top) {
                    x = workArea.Right - windowWidth - margin;
                    y = workArea.Bottom - windowHeight - margin;
                }
                // Top taskbar
                else if(taskbarRect.Top == monitorRect.Top && taskbarRect.Bottom < monitorRect.Bottom) {
                    x = workArea.Right - windowWidth - margin;
                    y = workArea.Top + margin;
                }
                // Left taskbar
                else if(taskbarRect.Left == monitorRect.Left && taskbarRect.Right < monitorRect.Right) {
                    x = workArea.Left + margin;
                    y = workArea.Bottom - windowHeight - margin;
                }
                // Right taskbar
                else {
                    x = workArea.Right - windowWidth - margin;
                    y = workArea.Bottom - windowHeight - margin;
                }
            }
            else {
                // Fallback: bottom-right corner
                x = workArea.Right - windowWidth - margin;
                y = workArea.Bottom - windowHeight - margin;
            }

            // Move the window to the calculated position
            _appWindow.Move(new PointInt32(x, y));
        }

        private void BringToFront() {
            // Bring window to front and activate it
            const uint SWP_SHOWWINDOW = 0x0040;
            const uint SWP_NOMOVE = 0x0002;
            const uint SWP_NOSIZE = 0x0001;

            SetWindowPos(_hwnd, new IntPtr(-1), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            SetWindowPos(_hwnd, new IntPtr(-2), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e) {
            // Open settings window
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Activate();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            var tb = sender as TextBox;
            if(tb != null) {
                var text = tb.Text.ToLower();

                if(string.IsNullOrWhiteSpace(text)) {
                    // Show all items when search is empty
                    foreach(var item in ClipboardListView.Items) {
                        if(item is ClipboardContentView view) {
                            view.Visibility = Visibility.Visible;
                        }
                    }
                }
                else {
                    // Filter items based on search text
                    foreach(var item in ClipboardListView.Items) {
                        if(item is ClipboardContentView view) {
                            if(view.ClipboardContent.ToLower().Contains(text)) {
                                view.Visibility = Visibility.Visible;
                            }
                            else {
                                view.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }
            }
        }
    }
}
