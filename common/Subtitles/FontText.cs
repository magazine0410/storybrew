using BrewLib.Graphics.Text;
using SkiaSharp;

namespace StorybrewCommon.Subtitles
{
    /// <summary>
    /// Text being rendered by a font generator, for it and its effects to draw.
    /// </summary>
    public class FontText
    {
        public string Text { get; }
        public SKFont Font { get; }
        public FontStyle Style { get; }

        /// <summary>
        /// Width of the widest line.
        /// </summary>
        public float Width { get; }
        public float Height { get; }

        private readonly string[] lines;
        private readonly float[] lineWidths;

        public FontText(string text, SKFont font, FontStyle style)
        {
            Text = text;
            Font = font;
            Style = style;

            lines = text.Split('\n');
            lineWidths = lines.Select(line => SkiaText.Measure(font, line)).ToArray();

            Width = lineWidths.Max();
            Height = font.Spacing * lines.Length;
        }

        /// <summary>
        /// Draws the text horizontally centered on x, with its top at y.
        /// </summary>
        public void Draw(SKCanvas canvas, SKPaint paint, float x, float y)
        {
            var metrics = Font.Metrics;
            var baseline = y - metrics.Ascent;
            for (var i = 0; i < lines.Length; i++)
            {
                var lineX = x - lineWidths[i] * 0.5f;
                SkiaText.Draw(canvas, Font, lines[i], lineX, baseline, paint);

                var thickness = metrics.UnderlineThickness ?? Font.Size / 14;
                if (Style.HasFlag(FontStyle.Underline))
                    canvas.DrawRect(lineX, baseline + (metrics.UnderlinePosition ?? metrics.Descent * 0.5f), lineWidths[i], thickness, paint);
                if (Style.HasFlag(FontStyle.Strikeout))
                    canvas.DrawRect(lineX, baseline + (metrics.StrikeoutPosition ?? metrics.Ascent * 0.3f), lineWidths[i], metrics.StrikeoutThickness ?? thickness, paint);

                baseline += Font.Spacing;
            }
        }
    }
}
