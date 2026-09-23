using OpenTK;
using SkiaSharp;

namespace StorybrewCommon.Subtitles
{
    public interface FontEffect
    {
        bool Overlay { get; }

        Vector2 Measure();

        /// <summary>
        /// Draws the effect on the bitmap, for text horizontally centered on x with its top at y.
        /// </summary>
        void Draw(SKBitmap bitmap, SKCanvas canvas, FontText text, float x, float y);
    }
}
