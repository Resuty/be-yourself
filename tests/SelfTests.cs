using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using BeYourself;

internal static class SelfTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("FAIL: " + message);
        Console.WriteLine("PASS: " + message);
    }

    [STAThread]
    private static int Main(string[] arguments)
    {
        string output = arguments.Length > 0 ? arguments[0] : Path.GetTempPath();
        string temporary = Path.Combine(output, "test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            string originalStartup = StartupRegistration.Entry;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            TestSettings(temporary);
            TestLanguages();
            TestLayout();
            TestRendering(output);
            Check(StartupRegistration.Command("C:\\自律提醒\\Be yourself\\BeYourself.exe") == "\"C:\\自律提醒\\Be yourself\\BeYourself.exe\"", "startup command quotes spaces and Unicode");
            if (Array.IndexOf(arguments, "--desktop") >= 0)
            {
                TestDesktop();
                TestSettingsForms(output);
                TestTrayApplication(temporary);
            }
            Check(StartupRegistration.Entry == originalStartup, "tests leave actual startup registration unchanged");
            Console.WriteLine("All " + checks + " checks passed.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally
        {
            // Delete only the unique test directory created above, never user configuration.
            Directory.Delete(temporary, true);
        }
    }

    private static void TestSettings(string directory)
    {
        string path = Path.Combine(directory, "settings.json");
        var store = new SettingsStore(path);
        Check(store.Load().Opacity == 90, "missing file uses defaults");
        var value = Settings.Defaults();
        value.Text = "做你自己。\r\nBe yourself.\n自分らしく。😀 \"quotes\" \\";
        value.Language = "ja"; value.ScreenName = "\\\\.\\DISPLAY2";
        store.Save(value);
        var loaded = store.Load();
        Check(loaded.Text == value.Text && loaded.Language == "ja" && loaded.ScreenName == value.ScreenName, "Unicode / multiline / emoji / quotes round-trip");
        value.Text = "second version"; store.Save(value);
        Check(store.Load().Text == "second version" && File.ReadAllText(path + ".bak", Encoding.UTF8).Contains("Be yourself"), "atomic replacement retains previous version");
        File.WriteAllText(path, "{}", Encoding.UTF8);
        Check(store.Load().FontSize == 36 && store.Load().Visible && store.Load().Opacity == 90, "old or partial configuration retains defaults");
        File.WriteAllText(path, "not JSON", Encoding.UTF8);
        bool rejected = false;
        try { store.Load(); } catch (System.Runtime.Serialization.SerializationException) { rejected = true; }
        Check(rejected && File.ReadAllText(path, Encoding.UTF8) == "not JSON", "corrupt configuration is rejected and preserved");
        value.FontSize = Single.NaN; value.Position = 99; value.Opacity = -2; value.Margin = -3; value.Language = "xx";
        value.Normalize();
        Check(value.FontSize == 36 && value.Position == 8 && value.Opacity == 10 && value.Margin == 0 && value.Language == "en", "invalid settings are bounded");
        value.Text = new string('a', 1999) + "😀"; value.Normalize();
        Check(value.Text.Length == 1999, "length limit does not split a surrogate pair");
        var copy = value.Copy(); copy.Text = "changed";
        Check(value.Text != copy.Text, "cancel draft cannot mutate active settings");
    }
    private static void TestLanguages()
    {
        foreach (var language in Strings.Languages)
            foreach (var key in Strings.Keys)
                Check(!String.IsNullOrWhiteSpace(Strings.Get(language, key)), language + ": " + key);
        Check(Strings.DetectLanguage("zh-TW") == "zh-CN" && Strings.DetectLanguage("ja-JP") == "ja" && Strings.DetectLanguage("de-DE") == "en", "language detection and fallback");
    }
    private static void TestLayout()
    {
        var area = new Rectangle(-1920, -300, 1920, 1040);
        var size = new Size(420, 100);
        for (int i = 0; i < 9; i++)
            Check(area.Contains(MottoRenderer.Place(area, size, i, 48)), "position " + i + " stays inside negative-coordinate monitor");
        Check(MottoRenderer.Place(area, size, 8, 48).Bottom == area.Bottom - 48, "bottom anchor respects work area and margin");
        Check(MottoRenderer.Place(area, size, 4, 48).X == area.X + (area.Width - size.Width) / 2, "center anchor uses monitor origin");
        Check(area.Contains(MottoRenderer.Place(area, size, 0, 5000)), "large margins remain visible");
    }
    private static void TestRendering(string output)
    {
        foreach (var language in Strings.Languages)
        {
            var settings = Settings.Defaults(); settings.Text = Strings.Get(language, "sample");
            using (var bitmap = MottoRenderer.Render(settings, new Size(1400, 800), 144))
            {
                int transparent = 0, visible = 0, partial = 0;
                for (int x = 0; x < bitmap.Width; x++)
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        byte alpha = bitmap.GetPixel(x, y).A;
                        if (alpha == 0) transparent++;
                        else { visible++; if (alpha < 255) partial++; }
                    }
                Check(transparent > 0 && visible > 0 && partial > 0, language + " renders real text with transparent background and smooth edges");
                bitmap.Save(Path.Combine(output, "motto-" + language + ".png"), ImageFormat.Png);
            }
        }
        var missingFont = Settings.Defaults(); missingFont.FontFamily = "font-that-does-not-exist";
        using (var bitmap = MottoRenderer.Render(missingFont, new Size(300, 200), 96))
            Check(bitmap.Width <= 300 && bitmap.Height <= 200, "font fallback and render bounds");
        var multiline = Settings.Defaults(); multiline.Text = "做你自己。\nBe yourself.\n自分らしく。";
        using (var bitmap = MottoRenderer.Render(multiline, new Size(480, 96), 96))
        {
            int rows = 0; bool previous = false;
            for (int y = 0; y < bitmap.Height; y++)
            {
                bool ink = false;
                for (int x = 0; x < bitmap.Width; x++) if (bitmap.GetPixel(x, y).A > 180) { ink = true; break; }
                if (ink && !previous) rows++;
                previous = ink;
            }
            Check(rows >= 3, "small preview preserves all three text lines");
        }
    }
    private static void TestDesktop()
    {
        IntPtr host = NativeMethods.FindDesktopHost();
        Check(host != IntPtr.Zero, "Explorer desktop host exists");
        using (var overlay = new DesktopOverlay())
        {
            var settings = Settings.Defaults();
            settings.Text = "Be yourself · 桌面层测试 · テスト";
            Check(overlay.Display(settings), "desktop overlay displays");
            Application.DoEvents();
            Check(overlay.Attached && NativeMethods.GetParent(overlay.Handle) == host, "actual HWND parent is Explorer desktop, behind application windows");
            int styles = NativeMethods.GetWindowLong(overlay.Handle, NativeMethods.GWL_STYLE);
            int extended = NativeMethods.GetWindowLong(overlay.Handle, NativeMethods.GWL_EXSTYLE);
            Check((styles & NativeMethods.WS_CHILD) != 0 && (styles & NativeMethods.WS_POPUP) == 0, "overlay is a desktop child, not a popup");
            int required = NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW;
            Check((extended & required) == required && (extended & 0x8) == 0 && !overlay.ShowInTaskbar, "per-pixel transparency, click-through, no activation, no taskbar, no topmost");
            Check(NativeMethods.SendMessage(overlay.Handle, 0x84, IntPtr.Zero, IntPtr.Zero).ToInt32() == -1, "hit test is transparent");
            Check(NativeMethods.SendMessage(overlay.Handle, 0x21, IntPtr.Zero, IntPtr.Zero).ToInt32() == 3, "mouse does not activate overlay");
            var actual = NativeMethods.WindowRectangle(overlay.Handle);
            Check(actual == overlay.LastBounds, "actual desktop coordinates match target position: " + actual + " / " + overlay.LastBounds);
            settings.Visible = false; overlay.Display(settings);
            Check(!overlay.Visible, "hide from tray removes desktop text");
            settings.Visible = true; overlay.Display(settings);
            Check(overlay.Visible && overlay.Attached, "show from tray restores desktop attachment");
        }
    }
    private static void TestSettingsForms(string output)
    {
        foreach (var language in Strings.Languages)
        {
            var settings = Settings.Defaults(); settings.Language = language; settings.Text = "做你自己。\nBe yourself.\n自分らしく。";
            using (var form = new SettingsForm(settings, false, delegate { throw new Exception("Unexpected save"); }))
            {
                form.Show(); Application.DoEvents();
                using (var bitmap = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(Path.Combine(output, "settings-" + language + ".png"), ImageFormat.Png);
                }
                Check(form.Controls.Find("SaveButton", true).Length == 1 && form.Controls.Find("MottoText", true).Length == 1, language + " settings form builds");
                form.Close();
            }
            Check(settings.Text.Contains("Be yourself."), language + " cancel preserves original text");
        }
        bool saved = false;
        var original = Settings.Defaults();
        using (var form = new SettingsForm(original, false, delegate(Settings value, bool startup)
        {
            Check(value.Text == "edited 中文 日本語" && !startup, "save passes edited Unicode text and startup choice"); saved = true;
        }))
        {
            form.Show(); Application.DoEvents();
            ((TextBox)form.Controls.Find("MottoText", true)[0]).Text = "edited 中文 日本語";
            ((Button)form.Controls.Find("SaveButton", true)[0]).PerformClick();
            Application.DoEvents();
            Check(saved && form.DialogResult == DialogResult.OK && original.Text != "edited 中文 日本語", "save commits a draft only through callback");
        }
    }

    private static void TestTrayApplication(string directory)
    {
        var store = new SettingsStore(Path.Combine(directory, "tray.json"));
        store.Save(Settings.Defaults());
        Exception failure = null;
        using (var context = new TrayApplication(store))
        using (var timer = new Timer { Interval = 100 })
        {
            timer.Tick += delegate
            {
                timer.Stop();
                try
                {
                    var field = typeof(TrayApplication).GetField("menu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var menu = (ContextMenuStrip)field.GetValue(context);
                    Check(menu.Items.Count == 4, "tray exposes settings, visibility, separator and exit");
                    menu.Items[1].PerformClick();
                    Check(!store.Load().Visible, "actual tray hide action persists visibility");
                    menu.Items[1].PerformClick();
                    Check(store.Load().Visible, "actual tray show action persists visibility");
                    menu.Items[3].PerformClick();
                }
                catch (Exception error) { failure = error; context.ExitThread(); }
            };
            timer.Start();
            Application.Run(context);
            if (failure != null) throw failure;
            Check(true, "tray exit disposes desktop window and message loop");
        }
    }
}
