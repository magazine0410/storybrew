using SkiaSharp;
using System.Drawing;

namespace StorybrewCommon.Scripting
{
    /// <summary>
    /// An image, with the members scripts used from System.Drawing.Bitmap.
    /// </summary>
    public class Bitmap : IDisposable
    {
        /// <summary>
        /// The pixels, as BGRA that isn't premultiplied.
        /// </summary>
        public SKBitmap SKBitmap { get; }

        public int Width => SKBitmap.Width;
        public int Height => SKBitmap.Height;
        public Size Size => new Size(Width, Height);

        public Bitmap(SKBitmap bitmap)
        {
            SKBitmap = bitmap;
        }

        public Color GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= Height) throw new ArgumentOutOfRangeException(nameof(y));

            var color = SKBitmap.GetPixel(x, y);
            return Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
        }

        public static implicit operator SKBitmap(Bitmap bitmap)
            => bitmap.SKBitmap;

        public void Dispose()
            => SKBitmap.Dispose();
    }
}
