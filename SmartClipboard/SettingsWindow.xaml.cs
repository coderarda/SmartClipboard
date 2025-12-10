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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.ApplicationModel;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SmartClipboard {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsWindow : Window {
        private const string StartupTaskId = "SmartClipboardStartupTask";

        private bool savingEnabled = false;
        private bool minimizeToTray = true;
        
        public SettingsWindow() {
            InitializeComponent();

            // Enable custom title bar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // Set window size to 400x400
            AppWindow.Resize(new Windows.Graphics.SizeInt32(400, 400));

            // Load current startup status
            LoadStartupStatus();
            
            // Load saving preference
            LoadSavingPreference();
            
            // Load minimize to tray preference
            LoadMinimizeToTrayPreference();
        }

        private async void LoadMinimizeToTrayPreference() {
            try {
                var settings = await LoadSettingsAsync();
                minimizeToTray = settings.MinimizeToTray;
                CheckBox3.IsChecked = minimizeToTray;
                
                if(App.MainWindowInstance != null) {
                    App.MainWindowInstance.SetMinimizeToTray(minimizeToTray);
                }
            }
            catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error loading minimize to tray preference: {ex.Message}");
            }
        }

        private async void LoadSavingPreference() {
            try {
                var settings = await LoadSettingsAsync();
                savingEnabled = settings.SavingEnabled;
                CheckBox2.IsChecked = savingEnabled;
                
                if(App.MainWindowInstance != null) {
                    App.MainWindowInstance.SetSavingEnabled(savingEnabled);
                }
            }
            catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error loading saving preference: {ex.Message}");
            }
        }

        private async void LoadStartupStatus() {
            try {
                var startupTask = await StartupTask.GetAsync(StartupTaskId);
                CheckBox1.IsChecked = startupTask.State == StartupTaskState.Enabled;
                System.Diagnostics.Debug.WriteLine($"Startup task state: {startupTask.State}");
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error loading startup status: {ex.Message}");
                CheckBox1.IsChecked = false;
            }
        }

        private async void CheckBox_Checked(object sender, RoutedEventArgs e) {
            try {
                var startupTask = await StartupTask.GetAsync(StartupTaskId);
                System.Diagnostics.Debug.WriteLine($"Current startup state: {startupTask.State}");

                if (startupTask.State == StartupTaskState.Disabled) {
                    var newState = await startupTask.RequestEnableAsync();
                    System.Diagnostics.Debug.WriteLine($"Requested enable, new state: {newState}");

                    switch (newState) {
                        case StartupTaskState.DisabledByUser:
                            await ShowErrorDialog("Startup was disabled by you in Task Manager. Please enable it there.");
                            CheckBox1.IsChecked = false;
                            break;
                        case StartupTaskState.DisabledByPolicy:
                            await ShowErrorDialog("Startup is disabled by group policy.");
                            CheckBox1.IsChecked = false;
                            break;
                        case StartupTaskState.Enabled:
                            System.Diagnostics.Debug.WriteLine("Startup enabled successfully");
                            break;
                    }
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error enabling startup: {ex.Message}");
                await ShowErrorDialog($"Failed to enable startup: {ex.Message}");
                CheckBox1.IsChecked = false;
            }
        }

        private async void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            try {
                var startupTask = await StartupTask.GetAsync(StartupTaskId);
                if (startupTask.State == StartupTaskState.Enabled) {
                    startupTask.Disable();
                    System.Diagnostics.Debug.WriteLine("Startup disabled successfully");
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error disabling startup: {ex.Message}");
                await ShowErrorDialog($"Failed to disable startup: {ex.Message}");
                CheckBox1.IsChecked = true;
            }
        }

        private async void CheckBox2_Checked(object sender, RoutedEventArgs e) {
            savingEnabled = true;
            await SaveSettingsAsync().ConfigureAwait(false);
            
            if(App.MainWindowInstance != null) {
                App.MainWindowInstance.SetSavingEnabled(true);
            }
        }

        private async void CheckBox3_Checked(object sender, RoutedEventArgs e) {
            minimizeToTray = true;
            await SaveSettingsAsync().ConfigureAwait(false);
            
            if(App.MainWindowInstance != null) {
                App.MainWindowInstance.SetMinimizeToTray(true);
            }
        }

        private void CheckBox2_Unchecked(object sender, RoutedEventArgs e) {
            savingEnabled = false;
            SaveSettingsAsync().ConfigureAwait(false);
            
            if(App.MainWindowInstance != null) {
                App.MainWindowInstance.SetSavingEnabled(false);
            }
        }

        private async void CheckBox3_Unchecked(object sender, RoutedEventArgs e) {
            minimizeToTray = false;
            await SaveSettingsAsync().ConfigureAwait(false);
            
            if(App.MainWindowInstance != null) {
                App.MainWindowInstance.SetMinimizeToTray(false);
            }
        }

        private async Task ShowErrorDialog(string message) {
            var dialog = new ContentDialog {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task<AppSettings> LoadSettingsAsync() {
            try {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SmartClipboard",
                    "settings.json"
                );

                if(!File.Exists(settingsPath)) {
                    return new AppSettings();
                }

                var json = await File.ReadAllTextAsync(settingsPath);
                return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                return new AppSettings();
            }
        }

        private async Task SaveSettingsAsync() {
            try {
                var settings = new AppSettings {
                    SavingEnabled = savingEnabled,
                    MinimizeToTray = minimizeToTray
                };

                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SmartClipboard",
                    "settings.json"
                );

                var directory = Path.GetDirectoryName(settingsPath);
                if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }

                var options = new System.Text.Json.JsonSerializerOptions {
                    WriteIndented = true
                };

                var json = System.Text.Json.JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(settingsPath, json);
            }
            catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}
