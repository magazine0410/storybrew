using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using SkiaSharp;

namespace StorybrewCommon.Subtitles
{
    public class FontGradient : FontEffect
    {
        public Vector2 Offset = new Vector2(0, 0);
        public Vector2 Size = new Vector2(0, 24);
        public Color4 Color = new Color4(255, 0, 0, 0);
        public WrapMode WrapMode = WrapMode.TileFlipXY;

        public bool Overlay => true;
        public Vector2 Measure() => Vector2.Zero;

        public void Draw(SKBitmap bitmap, SKCanvas canvas, FontText text, float x, float y)
        {
            var transparentColor = Color.WithOpacity(0);
            using (var shader = SKShader.CreateLinearGradient(
                new SKPoint(x + Offset.X, y + Offset.Y),
                new SKPoint(x + Offset.X + Size.X, y + Offset.Y + Size.Y),
                new[] { Color.ToSKColor(), transparentColor.ToSKColor() },
                getTileMode(WrapMode)))
            using (var paint = new SKPaint() { Shader = shader, IsAntialias = true })
                text.Draw(canvas, paint, x, y);
        }

        private static SKShaderTileMode getTileMode(WrapMode wrapMode)
        {
            switch (wrapMode)
            {
                case WrapMode.Tile: return SKShaderTileMode.Repeat;
                case WrapMode.Clamp: return SKShaderTileMode.Clamp;
                default: return SKShaderTileMode.Mirror;
            }
        }
    }
}