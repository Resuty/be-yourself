using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace BeYourself
{
    internal static class NativeMethods
    {
        internal const int GWL_STYLE = -16, GWL_EXSTYLE = -20;
        internal const int WS_CHILD = 0x40000000, WS_POPUP = unchecked((int)0x80000000);
        internal const int WS_EX_LAYERED = 0x80000, WS_EX_TRANSPARENT = 0x20;
        internal const int WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x8000000;
        internal const uint SWP_NOACTIVATE = 0x10, SWP_SHOWWINDOW = 0x40;
        internal delegate bool EnumWindowProc(IntPtr handle, IntPtr parameter);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point { public int X, Y; public Point(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)]
        internal struct Size { public int Width, Height; public Size(int width, int height) { Width = width; Height = height; } }
        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Blend { public byte Operation, Flags, Alpha, Format; }

        [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindowProc callback, IntPtr parameter);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassName(IntPtr handle, StringBuilder name, int capacity);
        [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr handle);
        [DllImport("user32.dll")] internal static extern IntPtr GetParent(IntPtr handle);
        [DllImport("user32.dll", SetLastError = true)] internal static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] internal static extern int GetWindowLong(IntPtr handle, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] internal static extern int SetWindowLong(IntPtr handle, int index, int value);
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool SetWindowPos(IntPtr handle, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] internal static extern int MapWindowPoints(IntPtr from, IntPtr to, ref Point point, uint count);
        [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr handle, out Rect rect);
        [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr handle);
        [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr handle, IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
        [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr value);
        [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr dc);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destinationDc, ref Point destination,
            ref Size size, IntPtr sourceDc, ref Point source, uint colorKey, ref Blend blend, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern uint RegisterWindowMessage(string message);
        [DllImport("user32.dll")] internal static extern bool DestroyIcon(IntPtr icon);
        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

        // Explorer's icon host is above ordinary wallpaper hosts and below application windows.
        // shortcut: Explorer's private window classes may change; stay hidden and retry if absent.
        internal static IntPtr FindDesktopHost()
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows(delegate(IntPtr handle, IntPtr parameter)
            {
                var name = new StringBuilder(64);
                GetClassName(handle, name, name.Capacity);
                if ((name.ToString() == "Progman" || name.ToString() == "WorkerW") &&
                    FindWindowEx(handle, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                { found = handle; return false; }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        internal static Rectangle WindowRectangle(IntPtr handle)
        {
            Rect rect;
            GetWindowRect(handle, out rect);
            return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
    }
}
