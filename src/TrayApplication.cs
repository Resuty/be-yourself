using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BeYourself
{
    internal sealed class TrayApplication : ApplicationContext
    {
        private readonly SettingsStore store;
        private readonly NotifyIcon tray;
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private readonly Icon icon;
        private readonly ShellMessages messages;
        private readonly Timer recovery = new Timer { Interval = 3000 };
        private Settings settings;
        private DesktopOverlay overlay;
        private SettingsForm dialog;
        private bool exiting;
        private string lastDesktopError;

        internal TrayApplication(SettingsStore settingsStore = null)
        {
            store = settingsStore ?? new SettingsStore(SettingsStore.DefaultPath);
            bool firstRun = !File.Exists(store.Path);
            string warning = null;
            try { settings = store.Load(); }
            catch (Exception error)
            {
                settings = Settings.Defaults();
                warning = Strings.Get(settings.Language, "loadError") + "\n" + error.Message;
            }
            icon = CreateIcon();
            tray = new NotifyIcon { Icon = icon, Text = "Be yourself", ContextMenuStrip = menu, Visible = true };
            RebuildMenu();
            messages = new ShellMessages(RefreshDesktop);
            recovery.Tick += delegate
            {
                // A single cheap host check recovers after Explorer or a wallpaper app rebuilds the desktop.
                if (settings.Visible && (overlay == null || overlay.IsDisposed || !overlay.Attached)) RefreshDesktop();
            };
            RefreshDesktop();
            recovery.Start();
            if (warning != null) tray.ShowBalloonTip(8000, "Be yourself", warning, ToolTipIcon.Warning);
            else if (firstRun) tray.ShowBalloonTip(7000, "Be yourself", Strings.Get(settings.Language, "welcome"), ToolTipIcon.Info);
        }

        private void RebuildMenu()
        {
            while (menu.Items.Count > 0) menu.Items[0].Dispose();
            menu.Items.Add(Strings.Get(settings.Language, "settings") + "…", null, delegate { OpenSettings(); });
            menu.Items.Add(Strings.Get(settings.Language, settings.Visible ? "hide" : "show"), null, delegate
            {
                var replacement = settings.Copy(); replacement.Visible = !replacement.Visible;
                try { store.Save(replacement); settings = replacement; RefreshDesktop(); RebuildMenu(); }
                catch (Exception error) { ShowError(error); }
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.Get(settings.Language, "exit"), null, delegate { ExitThread(); });
        }

        private void OpenSettings()
        {
            if (dialog != null && !dialog.IsDisposed) { dialog.Activate(); return; }
            try
            {
                dialog = new SettingsForm(settings, StartupRegistration.Enabled, SaveSettings);
                dialog.ShowDialog();
                dialog.Dispose(); dialog = null;
            }
            catch (Exception error) { ShowError(error); }
        }

        private void SaveSettings(Settings replacement, bool startup)
        {
            string previousStartup = StartupRegistration.Entry;
            string desiredStartup = startup ? StartupRegistration.Command(Application.ExecutablePath) : null;
            // Write startup first; if disk persistence fails, restore its previous state.
            if (previousStartup != desiredStartup) StartupRegistration.SetEntry(desiredStartup);
            try { store.Save(replacement); }
            catch
            {
                if (previousStartup != desiredStartup) StartupRegistration.SetEntry(previousStartup);
                throw;
            }
            settings = replacement;
            RefreshDesktop();
            // The menu that opened the modal dialog must not be disposed inside its click event.
            messages.DeferMenuRefresh(RebuildMenu);
        }

        private void RefreshDesktop()
        {
            if (exiting) return;
            try
            {
                if (overlay == null || overlay.IsDisposed || (overlay.IsHandleCreated && !overlay.Attached))
                {
                    if (overlay != null) overlay.Dispose();
                    overlay = new DesktopOverlay();
                }
                if (overlay.Display(settings)) lastDesktopError = null;
            }
            catch (Exception error)
            {
                if (overlay != null) { overlay.Dispose(); overlay = null; }
                if (lastDesktopError != error.Message)
                    tray.ShowBalloonTip(8000, "Be yourself", error.Message, ToolTipIcon.Warning);
                lastDesktopError = error.Message;
            }
        }

        private void ShowError(Exception error)
        {
            MessageBox.Show(Strings.Get(settings.Language, "error") + "\n\n" + error.Message, "Be yourself", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static Icon CreateIcon()
        {
            using (var bitmap = new Bitmap(32, 32))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var background = new SolidBrush(Color.FromArgb(24, 77, 72)))
            using (var accent = new SolidBrush(Color.FromArgb(141, 229, 182)))
            using (var font = new Font("Segoe UI", 21, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.FillEllipse(background, 1, 1, 30, 30);
                graphics.DrawString("B", font, Brushes.White, 6, 2);
                graphics.FillEllipse(accent, 23, 22, 7, 7);
                IntPtr handle = bitmap.GetHicon();
                try { return (Icon)Icon.FromHandle(handle).Clone(); }
                finally { NativeMethods.DestroyIcon(handle); }
            }
        }

        protected override void ExitThreadCore()
        {
            exiting = true;
            recovery.Stop(); recovery.Dispose();
            if (dialog != null) dialog.Close();
            if (overlay != null) overlay.Dispose();
            messages.Dispose();
            tray.Visible = false; tray.Dispose(); menu.Dispose(); icon.Dispose();
            base.ExitThreadCore();
        }
    }

    internal sealed class ShellMessages : NativeWindow, IDisposable
    {
        private readonly uint taskbarCreated = NativeMethods.RegisterWindowMessage("TaskbarCreated");
        private readonly Action refreshDesktop;
        private Action refreshMenu;
        internal ShellMessages(Action refresh)
        {
            refreshDesktop = refresh;
            CreateHandle(new CreateParams { Caption = "Be yourself · Messages", ExStyle = NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE });
        }
        internal void DeferMenuRefresh(Action action)
        {
            refreshMenu = action;
            PostMessage(Handle, 0x8001, IntPtr.Zero, IntPtr.Zero);
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == taskbarCreated || message.Msg == 0x7E || message.Msg == 0x1A) refreshDesktop();
            else if (message.Msg == 0x8001 && refreshMenu != null) { var action = refreshMenu; refreshMenu = null; action(); }
            base.WndProc(ref message);
        }
        public void Dispose() { DestroyHandle(); }
    }
}
