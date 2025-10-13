using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SmartClipboard {
    public sealed partial class ClipboardContentView : UserControl, INotifyPropertyChanged {
        private string _clipboardContent;
        public string ClipboardContent {
            get => _clipboardContent;
            set {
                if (_clipboardContent != value) {
                    _clipboardContent = value;
                    OnPropertyChanged(nameof(ClipboardContent));
                }
            }
        }

        private string _timestamp;
        public string Timestamp {
            get => _timestamp;
            set {
                if (_timestamp != value) {
                    _timestamp = value;
                    OnPropertyChanged(nameof(Timestamp));
                }
            }
        }

        public ClipboardContentView(string content) {
            this.InitializeComponent();
            _clipboardContent = content;
            _timestamp = DateTime.Now.ToString("HH:mm");
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e) {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(ClipboardContent);
            Clipboard.SetContent(dataPackage);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}