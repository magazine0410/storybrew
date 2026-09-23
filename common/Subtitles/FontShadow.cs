using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using SkiaSharp;

namespace StorybrewCommon.Subtitles
{
    public class FontShadow : FontEffect
    {
        public int Thickness = 1;
        public Color4 Color = new Color4(0, 0, 0, 100);

        public bool Overlay => false;
        public Vector2 Measure() => new Vector2(Thickness * 2);

        public void Draw(SKBitmap bitmap, SKCanvas canvas, FontText text, float x, float y)
        {
            if (Thickness < 1)
                return;

            using (var paint = new SKPaint() { Color = Color.ToSKColor(), IsAntialias = true })
                for (var i = 1; i <= Thickness; i++)
                    text.Draw(canvas, paint, x + i, y + i);
        }
    }
}