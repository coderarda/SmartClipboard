using System;

namespace SmartClipboard {
    public class ClipboardItem {
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }

        public ClipboardItem() {
            Timestamp = DateTime.Now;
            Content = string.Empty;
            Type = "Text";
        }

        public ClipboardItem(string content) {
            Content = content;
            Timestamp = DateTime.Now;
            Type = "Text";
        }
    }
}
