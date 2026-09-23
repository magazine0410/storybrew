using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using SkiaSharp;

namespace StorybrewCommon.Subtitles
{
    public class FontBackground : FontEffect
    {
        public Color4 Color = new Color4(0, 0, 0, 255);

        public bool Overlay => false;
        public Vector2 Measure() => Vector2.Zero;

        public void Draw(SKBitmap bitmap, SKCanvas canvas, FontText text, float x, float y)
        {
            canvas.Clear(Color.ToSKColor());
        }
    }
}