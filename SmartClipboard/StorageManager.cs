using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartClipboard {
    public static class StorageManager {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartClipboard"
        );

        private static readonly string ClipboardDataFile = Path.Combine(AppDataPath, "clipboard_items.json");

        static StorageManager() {
            if(!Directory.Exists(AppDataPath)) {
                Directory.CreateDirectory(AppDataPath);
            }
        }

        public static async Task SaveClipboardItemsAsync(List<ClipboardItem> items) {
            try {
                var options = new JsonSerializerOptions {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(items, options);
                await File.WriteAllTextAsync(ClipboardDataFile, json);
                System.Diagnostics.Debug.WriteLine($"Saved {items.Count} clipboard items to {ClipboardDataFile}");
            }
            catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error saving clipboard items: {ex.Message}");
            }
        }

        public static async Task<List<ClipboardItem>> LoadClipboardItemsAsync() {
            try {
                if(!File.Exists(ClipboardDataFile)) {
                    System.Diagnostics.Debug.WriteLine("No clipboard data file found, returning empty list");
                    return new List<ClipboardItem>();
                }

                var json = await File.ReadAllTextAsync(ClipboardDataFile);
                var items = JsonSerializer.Deserialize<List<ClipboardItem>>(json) ?? new List<ClipboardItem>();
                System.Diagnostics.Debug.WriteLine($"Loaded {items.Count} clipboard items from {ClipboardDataFile}");
                return items;
            }
            catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error loading clipboard items: {ex.Message}");
                return new List<ClipboardItem>();
            }
        }
    }
}
