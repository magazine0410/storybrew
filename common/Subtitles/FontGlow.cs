using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using SkiaSharp;
using StorybrewCommon.Util;

namespace StorybrewCommon.Subtitles
{
    public class FontGlow : FontEffect
    {
        private double[,] kernel;

        private int radius = 6;
        public int Radius
        {
            get { return radius; }
            set
            {
                if (radius == value) return;
                radius = value;
                kernel = null;
            }
        }

        private double power = 0;
        public double Power
        {
            get { return power; }
            set
            {
                if (power == value) return;
                power = value;
                kernel = null;
            }
        }

        public Color4 Color = new Color4(255, 255, 255, 100);

        public bool Overlay => false;
        public Vector2 Measure() => new Vector2(Radius * 2);

        public void Draw(SKBitmap bitmap, SKCanvas canvas, FontText text, float x, float y)
        {
            if (Radius < 1)
                return;

            using (var blurSource = new SKBitmap(new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul)))
            {
                using (var blurCanvas = new SKCanvas(blurSource))
                using (var paint = new SKPaint() { Color = SKColors.White, IsAntialias = true })
                {
                    blurCanvas.Clear(SKColors.Transparent);
                    text.Draw(blurCanvas, paint, x, y);
                }

                if (kernel == null)
                {
                    var radius = Math.Min(Radius, 24);
                    var power = Power >= 1 ? Power : Radius * 0.5;
                    kernel = BitmapHelper.CalculateGaussianKernel(radius, power);
                }

                using (var blurredBitmap = BitmapHelper.ConvoluteAlpha(blurSource, kernel, System.Drawing.Color.FromArgb(Color.ToArgb())))
                    canvas.DrawBitmap(blurredBitmap.Bitmap, 0, 0, SKSamplingOptions.Default, null);
            }
        }
    }
}