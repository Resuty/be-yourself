using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BeYourself
{
    internal sealed class DesktopOverlay : Form
    {
        internal IntPtr DesktopHost { get; private set; }
        internal Rectangle LastBounds { get; private set; }
        private Settings settings;
        internal bool Attached { get { return IsHandleCreated && NativeMethods.IsWindow(DesktopHost) && NativeMethods.GetParent(Handle) == DesktopHost; } }

        public DesktopOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            Text = "Be yourself · Desktop";
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                var value = base.CreateParams;
                value.ExStyle |= NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT |
                    NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
                return value;
            }
        }

        internal bool Display(Settings value)
        {
            settings = value;
            if (!settings.Visible) { Hide(); return true; }
            IntPtr host = NativeMethods.FindDesktopHost();
            if (host == IntPtr.Zero) { Hide(); return false; }
            IntPtr window = Handle;
            int style = NativeMethods.GetWindowLong(window, NativeMethods.GWL_STYLE);
            NativeMethods.SetWindowLong(window, NativeMethods.GWL_STYLE, (style & ~NativeMethods.WS_POPUP) | NativeMethods.WS_CHILD);
            if (NativeMethods.GetParent(window) != host)
            {
                NativeMethods.SetParent(window, host);
                if (NativeMethods.GetParent(window) != host) throw new Win32Exception("Cannot attach to the Windows desktop.");
            }
            DesktopHost = host;
            Show();
            Render();
            return true;
        }

        private void Render()
        {
            Screen screen = Screen.PrimaryScreen;
            foreach (var item in Screen.AllScreens) if (item.DeviceName == settings.ScreenName) screen = item;
            Rectangle area = screen.WorkingArea;
            float dpi;
            using (var graphics = Graphics.FromHwnd(DesktopHost)) dpi = graphics.DpiX;
            int margin = (int)Math.Round(settings.Margin * dpi / 96f);
            var available = new Size(Math.Max(1, area.Width - Math.Min(margin * 2, area.Width / 2)),
                Math.Max(1, area.Height - Math.Min(margin * 2, area.Height / 2)));
            using (var bitmap = MottoRenderer.Render(settings, available, dpi))
            {
                LastBounds = MottoRenderer.Place(area, bitmap.Size, settings.Position, margin);
                Present(bitmap, LastBounds, settings.Opacity);
            }
        }

        private void Present(Bitmap bitmap, Rectangle bounds, int opacity)
        {
            IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
            IntPtr memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
            IntPtr image = IntPtr.Zero, previous = IntPtr.Zero;
            try
            {
                image = bitmap.GetHbitmap(Color.FromArgb(0));
                previous = NativeMethods.SelectObject(memoryDc, image);
                var destination = new NativeMethods.Point(bounds.X, bounds.Y);
                var source = new NativeMethods.Point(0, 0);
                var size = new NativeMethods.Size(bitmap.Width, bitmap.Height);
                var blend = new NativeMethods.Blend { Operation = 0, Flags = 0, Alpha = (byte)(opacity * 255 / 100), Format = 1 };
                if (!NativeMethods.UpdateLayeredWindow(Handle, screenDc, ref destination, ref size, memoryDc, ref source, 0, ref blend, 2))
                    throw new Win32Exception();
                NativeMethods.MapWindowPoints(IntPtr.Zero, DesktopHost, ref destination, 1);
                if (!NativeMethods.SetWindowPos(Handle, IntPtr.Zero, destination.X, destination.Y, bounds.Width, bounds.Height,
                    NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW)) throw new Win32Exception();
            }
            finally
            {
                if (previous != IntPtr.Zero) NativeMethods.SelectObject(memoryDc, previous);
                if (image != IntPtr.Zero) NativeMethods.DeleteObject(image);
                if (memoryDc != IntPtr.Zero) NativeMethods.DeleteDC(memoryDc);
                NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x84) { message.Result = new IntPtr(-1); return; } // HTTRANSPARENT
            if (message.Msg == 0x21) { message.Result = new IntPtr(3); return; } // MA_NOACTIVATE
            base.WndProc(ref message);
        }
    }
}
