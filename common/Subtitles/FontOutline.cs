using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using SkiaSharp;

namespace StorybrewCommon.Subtitles
{
    public class FontOutline : FontEffect
    {
        private const float diagonal = 1.41421356237f;

        public int Thickness = 1;
        public Color4 Color = new Color4(0, 0, 0, 100);

        public bool Overlay => false;
        public Vector2 Measure() => new Vector2(Thickness * diagonal * 2);

        public void Draw(SKBitmap bitmap, SKCanvas canvas, FontText text, float x, float y)
        {
            if (Thickness < 1)
                return;

            using (var paint = new SKPaint() { Color = Color.ToSKColor(), IsAntialias = true })
                for (var i = 1; i <= Thickness; i++)
                    if (i % 2 == 0)
                    {
                        text.Draw(canvas, paint, x - i * diagonal, y);
                        text.Draw(canvas, paint, x, y - i * diagonal);
                        text.Draw(canvas, paint, x + i * diagonal, y);
                        text.Draw(canvas, paint, x, y + i * diagonal);
                    }
                    else
                    {
                        text.Draw(canvas, paint, x - i, y - i);
                        text.Draw(canvas, paint, x - i, y + i);
                        text.Draw(canvas, paint, x + i, y + i);
                        text.Draw(canvas, paint, x + i, y - i);
                    }
        }
    }
}