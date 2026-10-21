using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.Win32;

namespace BeYourself
{
    [DataContract]
    internal sealed class Settings
    {
        [DataMember] public string Language = Strings.DetectLanguage(CultureInfo.CurrentUICulture.Name);
        [DataMember] public string Text = "今天的自律，是明天的自由。";
        [DataMember] public string FontFamily = "Microsoft YaHei UI";
        [DataMember] public float FontSize = 36;
        [DataMember] public bool Bold = true;
        [DataMember] public int ColorArgb = Color.White.ToArgb();
        [DataMember] public int Opacity = 90;
        [DataMember] public int Position = 4;
        [DataMember] public int Margin = 48;
        [DataMember] public string ScreenName = "";
        [DataMember] public bool Visible = true;

        public static Settings Defaults()
        {
            var value = new Settings();
            value.Text = Strings.Get(value.Language, "sample");
            return value;
        }

        // DataContractJsonSerializer skips field initializers when reading older files.
        [OnDeserializing]
        private void BeforeRead(StreamingContext context)
        {
            var defaults = Defaults();
            Language = defaults.Language; Text = defaults.Text; FontFamily = defaults.FontFamily;
            FontSize = defaults.FontSize; Bold = defaults.Bold; ColorArgb = defaults.ColorArgb;
            Opacity = defaults.Opacity; Position = defaults.Position; Margin = defaults.Margin;
            ScreenName = ""; Visible = true;
        }

        public void Normalize()
        {
            Language = Strings.DetectLanguage(Language);
            if (String.IsNullOrWhiteSpace(Text)) Text = Strings.Get(Language, "sample");
            Text = Text.Replace("\r\n", "\n").Replace('\r', '\n');
            if (Text.Length > 2000) Text = Text.Substring(0, Char.IsHighSurrogate(Text[1999]) ? 1999 : 2000);
            if (String.IsNullOrWhiteSpace(FontFamily)) FontFamily = "Microsoft YaHei UI";
            if (Single.IsNaN(FontSize) || Single.IsInfinity(FontSize)) FontSize = 36;
            FontSize = Math.Max(12, Math.Min(144, FontSize));
            Opacity = Math.Max(10, Math.Min(100, Opacity));
            Position = Math.Max(0, Math.Min(8, Position));
            Margin = Math.Max(0, Math.Min(300, Margin));
            ScreenName = ScreenName ?? "";
            ColorArgb = Color.FromArgb(255, Color.FromArgb(ColorArgb)).ToArgb();
        }

        public Settings Copy() { return (Settings)MemberwiseClone(); }
    }

    internal sealed class SettingsStore
    {
        public readonly string Path;
        public SettingsStore(string path) { Path = path; }
        public static string DefaultPath
        {
            get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeYourself", "settings.json"); }
        }

        public Settings Load()
        {
            if (!File.Exists(Path)) return Settings.Defaults();
            using (var stream = File.OpenRead(Path))
            {
                // Accept UTF-8 files saved with a BOM by Windows text editors.
                if (stream.ReadByte() != 0xEF || stream.ReadByte() != 0xBB || stream.ReadByte() != 0xBF) stream.Position = 0;
                var settings = (Settings)new DataContractJsonSerializer(typeof(Settings)).ReadObject(stream);
                if (settings == null) throw new SerializationException("Invalid settings.json");
                settings.Normalize();
                return settings;
            }
        }

        public void Save(Settings settings)
        {
            settings.Normalize();
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path)));
            string temporary = Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    new DataContractJsonSerializer(typeof(Settings)).WriteObject(stream, settings);
                    stream.Flush(true);
                }
                if (File.Exists(Path)) File.Replace(temporary, Path, Path + ".bak");
                else File.Move(temporary, Path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }

    internal static class StartupRegistration
    {
        internal const string KeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        internal const string ValueName = "Be yourself";
        public static bool Enabled
        {
            get { return Entry != null; }
        }
        internal static string Entry
        {
            get { using (var key = Registry.CurrentUser.OpenSubKey(KeyPath)) return key == null ? null : key.GetValue(ValueName) as string; }
        }
        internal static string Command(string executable) { return "\"" + executable + "\""; }
        internal static void SetEntry(string command)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
            {
                if (command != null) key.SetValue(ValueName, command, RegistryValueKind.String);
                else key.DeleteValue(ValueName, false);
            }
        }
    }
}
