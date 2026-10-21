using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace BeYourself
{
    internal static class MottoRenderer
    {
        internal static Font CreateFont(Settings settings)
        {
            var style = settings.Bold ? FontStyle.Bold : FontStyle.Regular;
            try { return new Font(settings.FontFamily, settings.FontSize, style, GraphicsUnit.Point); }
            catch (ArgumentException) { return new Font(FontFamily.GenericSansSerif, settings.FontSize, FontStyle.Regular); }
        }

        internal static Bitmap Render(Settings settings, Size available, float dpi)
        {
            int width = Math.Max(1, available.Width), height = Math.Max(1, available.Height);
            using (var measuring = new Bitmap(1, 1))
            using (var graphics = Graphics.FromImage(measuring))
            using (var font = CreateFont(settings))
            using (var format = new StringFormat())
            {
                format.Alignment = (StringAlignment)(settings.Position % 3);
                // Position columns use left / center / right, matching StringAlignment values.
                format.FormatFlags = StringFormatFlags.LineLimit;
                float em = font.SizeInPoints * dpi / 72f;
                const int padding = 5;
                int usableWidth = Math.Max(1, width - padding * 2);
                int usableHeight = Math.Max(1, height - padding * 2);
                // Keep all ordinary multiline content visible in small previews and on smaller monitors.
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    using (var pixelFont = new Font(font.FontFamily, em, font.Style, GraphicsUnit.Pixel))
                    {
                        SizeF measured = graphics.MeasureString(settings.Text, pixelFont, new SizeF(usableWidth, 100000), format);
                        float contentHeight = measured.Height + pixelFont.GetHeight(graphics) * 0.15f;
                        if (attempt < 7 && em > 1 && (contentHeight > usableHeight || measured.Width > usableWidth))
                        {
                            em = Math.Max(1, em * Math.Min(usableHeight / contentHeight, usableWidth / measured.Width) * 0.94f);
                            continue;
                        }
                        int bitmapWidth = Math.Min(width, Math.Max(1, (int)Math.Ceiling(measured.Width) + padding * 2));
                        int bitmapHeight = Math.Min(height, Math.Max(1, (int)Math.Ceiling(contentHeight) + padding * 2));
                        var bitmap = new Bitmap(bitmapWidth, bitmapHeight, PixelFormat.Format32bppArgb);
                        using (var canvas = Graphics.FromImage(bitmap))
                        using (var path = new GraphicsPath())
                        using (var brush = new SolidBrush(Color.FromArgb(settings.ColorArgb)))
                        using (var edge = new Pen(Color.FromArgb(150, 0, 0, 0), Math.Max(1.5f, em / 24)))
                        {
                            canvas.SmoothingMode = SmoothingMode.AntiAlias;
                            canvas.CompositingQuality = CompositingQuality.HighQuality;
                            edge.LineJoin = LineJoin.Round;
                            path.AddString(settings.Text, pixelFont.FontFamily, (int)pixelFont.Style, em,
                                new RectangleF(padding, padding, Math.Max(1, bitmapWidth - padding * 2), Math.Max(1, bitmapHeight - padding * 2)), format);
                            canvas.DrawPath(edge, path);
                            canvas.FillPath(brush, path);
                        }
                        return bitmap;
                    }
                }
                throw new InvalidOperationException("Text layout failed.");
            }
        }

        internal static Rectangle Place(Rectangle workingArea, Size textSize, int position, int requestedMargin)
        {
            int marginX = Math.Min(requestedMargin, Math.Max(0, (workingArea.Width - textSize.Width) / 2));
            int marginY = Math.Min(requestedMargin, Math.Max(0, (workingArea.Height - textSize.Height) / 2));
            int column = position % 3, row = position / 3;
            int x = column == 0 ? workingArea.Left + marginX : column == 1 ? workingArea.Left + (workingArea.Width - textSize.Width) / 2 : workingArea.Right - textSize.Width - marginX;
            int y = row == 0 ? workingArea.Top + marginY : row == 1 ? workingArea.Top + (workingArea.Height - textSize.Height) / 2 : workingArea.Bottom - textSize.Height - marginY;
            return new Rectangle(x, y, textSize.Width, textSize.Height);
        }
    }
}
