using BrewLib.Graphics.Text;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using SkiaSharp;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Util;
using System.Diagnostics;
using System.Drawing;
using Tiny;

namespace StorybrewCommon.Subtitles
{
    public class FontTexture
    {
        public string Path { get; }
        public bool IsEmpty => Path == null;
        public float OffsetX { get; }
        public float OffsetY { get; }
        public int BaseWidth { get; }
        public int BaseHeight { get; }
        public int Width { get; }
        public int Height { get; }

        public FontTexture(string path, float offsetX, float offsetY, int baseWidth, int baseHeight, int width, int height)
        {
            Path = path;
            OffsetX = offsetX;
            OffsetY = offsetY;
            BaseWidth = baseWidth;
            BaseHeight = baseHeight;
            Width = width;
            Height = height;
        }

        public Vector2 OffsetFor(OsbOrigin origin)
        {
            switch (origin)
            {
                default:
                case OsbOrigin.TopLeft: return new Vector2(OffsetX, OffsetY);
                case OsbOrigin.TopCentre: return new Vector2(OffsetX + Width * 0.5f, OffsetY);
                case OsbOrigin.TopRight: return new Vector2(OffsetX + Width, OffsetY);
                case OsbOrigin.CentreLeft: return new Vector2(OffsetX, OffsetY + Height * 0.5f);
                case OsbOrigin.Centre: return new Vector2(OffsetX + Width * 0.5f, OffsetY + Height * 0.5f);
                case OsbOrigin.CentreRight: return new Vector2(OffsetX + Width, OffsetY + Height * 0.5f);
                case OsbOrigin.BottomLeft: return new Vector2(OffsetX, OffsetY + Height);
                case OsbOrigin.BottomCentre: return new Vector2(OffsetX + Width * 0.5f, OffsetY + Height);
                case OsbOrigin.BottomRight: return new Vector2(OffsetX + Width, OffsetY + Height);
            }
        }
    }

    public class FontDescription
    {
        public string FontPath;
        public int FontSize = 76;
        public Color4 Color = new Color4(0, 0, 0, 100);
        public Vector2 Padding = Vector2.Zero;
        public FontStyle FontStyle = FontStyle.Regular;
        public bool TrimTransparency;
        public bool EffectsOnly;
        public bool Debug;
    }

    public class FontGenerator
    {
        public string Directory { get; }
        private readonly FontDescription description;
        private readonly FontEffect[] effects;
        private readonly string projectDirectory;
        private readonly string assetDirectory;

        private readonly Dictionary<string, FontTexture> textureCache = new Dictionary<string, FontTexture>();

        /// <summary>
        /// Textures cached by another renderer (System.Drawing before this) are generated again.
        /// </summary>
        private const string renderer = "SkiaSharp";

        internal FontGenerator(string directory, FontDescription description, FontEffect[] effects, string projectDirectory, string assetDirectory)
        {
            Directory = directory;
            this.description = description;
            this.effects = effects;
            this.projectDirectory = projectDirectory;
            this.assetDirectory = assetDirectory;
        }

        public FontTexture GetTexture(string text)
        {
            if (!textureCache.TryGetValue(text, out FontTexture texture))
                textureCache.Add(text, texture = generateTexture(text));
            return texture;
        }

        private FontTexture generateTexture(string text)
        {
            var filename = text.Length == 1 ? $"{(int)text[0]:x4}.png" : $"_{textureCache.Count(l => l.Key.Length > 1):x3}.png";
            var bitmapPath = Path.Combine(assetDirectory, Directory, filename);

            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(bitmapPath));

            var fontPath = Path.Combine(projectDirectory, description.FontPath);
            if (!File.Exists(fontPath)) fontPath = description.FontPath;

            float offsetX = 0, offsetY = 0;
            int baseWidth, baseHeight, width, height;
            using (var typeface = loadTypeface(fontPath, description.FontStyle))
            using (var font = SkiaText.CreateFont(typeface, description.FontSize))
            {
                // Like GDI+, simulate the styles the font doesn't have
                if (description.FontStyle.HasFlag(FontStyle.Bold) && typeface.FontStyle.Weight < (int)SKFontStyleWeight.SemiBold)
                    font.Embolden = true;
                if (description.FontStyle.HasFlag(FontStyle.Italic) && typeface.FontStyle.Slant == SKFontStyleSlant.Upright)
                    font.SkewX = -0.25f;

                var fontText = new FontText(text, font, description.FontStyle);
                baseWidth = (int)Math.Ceiling(fontText.Width);
                baseHeight = (int)Math.Ceiling(fontText.Height);

                var effectsWidth = 0f;
                var effectsHeight = 0f;
                foreach (var effect in effects)
                {
                    var effectSize = effect.Measure();
                    effectsWidth = Math.Max(effectsWidth, effectSize.X);
                    effectsHeight = Math.Max(effectsHeight, effectSize.Y);
                }
                width = (int)Math.Ceiling(baseWidth + effectsWidth + description.Padding.X * 2);
                height = (int)Math.Ceiling(baseHeight + effectsHeight + description.Padding.Y * 2);

                var paddingX = description.Padding.X + effectsWidth * 0.5f;
                var paddingY = description.Padding.Y + effectsHeight * 0.5f;
                var textX = paddingX + fontText.Width * 0.5f;
                var textY = paddingY;

                offsetX = -paddingX;
                offsetY = -paddingY;

                if (text.Length == 1 && char.IsWhiteSpace(text[0]) || width == 0 || height == 0)
                    return new FontTexture(null, offsetX, offsetY, baseWidth, baseHeight, width, height);

                using (var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul)))
                {
                    using (var canvas = new SKCanvas(bitmap))
                    using (var textPaint = new SKPaint() { Color = description.Color.ToSKColor(), IsAntialias = true })
                    {
                        if (description.Debug)
                        {
                            var r = new Random(textureCache.Count);
                            canvas.Clear(new SKColor((byte)r.Next(100, 255), (byte)r.Next(100, 255), (byte)r.Next(100, 255)));
                        }
                        else canvas.Clear(SKColors.Transparent);

                        foreach (var effect in effects)
                            if (!effect.Overlay)
                                effect.Draw(bitmap, canvas, fontText, textX, textY);
                        if (!description.EffectsOnly)
                            fontText.Draw(canvas, textPaint, textX, textY);
                        foreach (var effect in effects)
                            if (effect.Overlay)
                                effect.Draw(bitmap, canvas, fontText, textX, textY);

                        if (description.Debug)
                            using (var pen = new SKPaint() { Color = new SKColor(255, 0, 0), Style = SKPaintStyle.Stroke, StrokeWidth = 1 })
                            {
                                canvas.DrawLine(textX, textY, textX, textY + baseHeight, pen);
                                canvas.DrawLine(textX - baseWidth * 0.5f, textY, textX + baseWidth * 0.5f, textY, pen);
                            }
                    }

                    var bounds = description.TrimTransparency ? BitmapHelper.FindTransparencyBounds(bitmap) : null;
                    if (bounds != null && bounds != new Rectangle(0, 0, bitmap.Width, bitmap.Height))
                    {
                        var trimBounds = bounds.Value;
                        using (var trimmedBitmap = new SKBitmap())
                        {
                            if (!bitmap.ExtractSubset(trimmedBitmap, SKRectI.Create(trimBounds.Left, trimBounds.Top, trimBounds.Width, trimBounds.Height)))
                                throw new InvalidOperationException($"Failed to trim {bitmapPath} to {trimBounds}");

                            offsetX += trimBounds.Left;
                            offsetY += trimBounds.Top;
                            width = trimmedBitmap.Width;
                            height = trimmedBitmap.Height;
                            BrewLib.Util.Misc.WithRetries(() => savePng(trimmedBitmap, bitmapPath));
                        }
                    }
                    else BrewLib.Util.Misc.WithRetries(() => savePng(bitmap, bitmapPath));
                }
            }
            return new FontTexture(Path.Combine(Directory, filename), offsetX, offsetY, baseWidth, baseHeight, width, height);
        }

        private static SKTypeface loadTypeface(string fontPath, FontStyle fontStyle)
        {
            if (File.Exists(fontPath))
            {
                var typeface = SKTypeface.FromFile(fontPath);
                if (typeface != null)
                    return typeface;
                Trace.WriteLine($"Failed to load font {fontPath}, using it as a font name");
            }

            // Not a font file, look for an installed font with that name
            var style = new SKFontStyle(
                fontStyle.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                fontStyle.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
            return SKTypeface.FromFamilyName(fontPath, style);
        }

        private static void savePng(SKBitmap bitmap, string path)
        {
            using (var data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.Create(path))
                data.SaveTo(stream);
        }

        internal void HandleCache(TinyToken cachedFontRoot)
        {
            if (!matches(cachedFontRoot))
                return;

            foreach (var cacheEntry in cachedFontRoot.Values<TinyObject>("Cache"))
            {
                var path = cacheEntry.Value<string>("Path");
                var hash = cacheEntry.Value<string>("Hash");

                var fullPath = Path.Combine(assetDirectory, path);
                if (!File.Exists(fullPath) || HashHelper.GetFileMd5(fullPath) != hash)
                    continue;

                var text = cacheEntry.Value<string>("Text");
                if (text.Contains('\ufffd'))
                {
                    Trace.WriteLine($"Ignoring invalid font texture \"{text}\" ({path})");
                    continue;
                }
                if (textureCache.ContainsKey(text))
                    throw new InvalidDataException($"The font texture for \"{text}\" ({path}) has been cached multiple times");

                textureCache.Add(text, new FontTexture(
                    path,
                    cacheEntry.Value<float>("OffsetX"),
                    cacheEntry.Value<float>("OffsetY"),
                    cacheEntry.Value<int>("BaseWidth"),
                    cacheEntry.Value<int>("BaseHeight"),
                    cacheEntry.Value<int>("Width"),
                    cacheEntry.Value<int>("Height")
                ));
            }
        }

        private bool matches(TinyToken cachedFontRoot)
        {
            if (cachedFontRoot.Value<string>("Renderer") == renderer &&
                cachedFontRoot.Value<string>("FontPath") == description.FontPath &&
                cachedFontRoot.Value<int>("FontSize") == description.FontSize &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorR"), description.Color.R, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorG"), description.Color.G, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorB"), description.Color.B, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("ColorA"), description.Color.A, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("PaddingX"), description.Padding.X, 0.00001f) &&
                MathUtil.FloatEquals(cachedFontRoot.Value<float>("PaddingY"), description.Padding.Y, 0.00001f) &&
                cachedFontRoot.Value<FontStyle>("FontStyle") == description.FontStyle &&
                cachedFontRoot.Value<bool>("TrimTransparency") == description.TrimTransparency &&
                cachedFontRoot.Value<bool>("EffectsOnly") == description.EffectsOnly &&
                cachedFontRoot.Value<bool>("Debug") == description.Debug)
            {
                var effectsRoot = cachedFontRoot.Value<TinyArray>("Effects");
                if (effectsRoot.Count != effects.Length)
                    return false;

                for (var i = 0; i < effects.Length; i++)
                    if (!matches(effects[i], effectsRoot[i].Value<TinyToken>()))
                        return false;

                return true;
            }
            return false;
        }

        private bool matches(FontEffect fontEffect, TinyToken cache)
        {
            var effectType = fontEffect.GetType();
            if (cache.Value<string>("Type") != effectType.FullName)
                return false;

            foreach (var field in effectType.GetFields())
            {
                var fieldType = field.FieldType;
                if (fieldType == typeof(Color4))
                {
                    var color = (Color4)field.GetValue(fontEffect);
                    if (!MathUtil.FloatEquals(cache.Value<float>($"{field.Name}R"), color.R, 0.00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}G"), color.G, 0.00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}B"), color.B, 0.00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}A"), color.A, 0.00001f))
                        return false;
                }
                else if (fieldType == typeof(Vector3))
                {
                    var vector = (Vector3)field.GetValue(fontEffect);
                    if (!MathUtil.FloatEquals(cache.Value<float>($"{field.Name}X"), vector.X, 0.00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}Y"), vector.Y, 0.00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}Z"), vector.Z, 0.00001f))
                        return false;
                }
                else if (fieldType == typeof(Vector2))
                {
                    var vector = (Vector2)field.GetValue(fontEffect);
                    if (!MathUtil.FloatEquals(cache.Value<float>($"{field.Name}X"), vector.X, 0.00001f) ||
                        !MathUtil.FloatEquals(cache.Value<float>($"{field.Name}Y"), vector.Y, 0.00001f))
                        return false;
                }
                else if (fieldType == typeof(double))
                {
                    if (!MathUtil.DoubleEquals(cache.Value<double>(field.Name), (double)field.GetValue(fontEffect), 0.00001))
                        return false;
                }
                else if (fieldType == typeof(float))
                {
                    if (!MathUtil.FloatEquals(cache.Value<float>(field.Name), (float)field.GetValue(fontEffect), 0.00001f))
                        return false;
                }
                else if (fieldType == typeof(int) || fieldType.IsEnum)
                {
                    if (cache.Value<int>(field.Name) != (int)field.GetValue(fontEffect))
                        return false;
                }
                else if (fieldType == typeof(string))
                {
                    if (cache.Value<string>(field.Name) != (string)field.GetValue(fontEffect))
                        return false;
                }
                else throw new InvalidDataException($"Unexpected field type {fieldType} for {field.Name} in {effectType.FullName}");
            }
            return true;
        }

        internal TinyObject ToTinyObject() => new TinyObject
        {
            { "Renderer", renderer },
            { "FontPath", PathHelper.WithStandardSeparators(description.FontPath) },
            { "FontSize", description.FontSize },
            { "ColorR", description.Color.R },
            { "ColorG", description.Color.G },
            { "ColorB", description.Color.B },
            { "ColorA", description.Color.A },
            { "PaddingX", description.Padding.X },
            { "PaddingY", description.Padding.Y },
            { "FontStyle", description.FontStyle },
            { "TrimTransparency", description.TrimTransparency },
            { "EffectsOnly", description.EffectsOnly },
            { "Debug", description.Debug },
            { "Effects", effects.Select(e => fontEffectToTinyObject(e))},
            { "Cache", textureCache.Where(l => !l.Value.IsEmpty).Select(l => letterToTinyObject(l))},
        };

        private TinyObject letterToTinyObject(KeyValuePair<string, FontTexture> letterEntry) => new TinyObject
        {
            { "Text", letterEntry.Key },
            { "Path", PathHelper.WithStandardSeparators(letterEntry.Value.Path) },
            { "Hash", HashHelper.GetFileMd5(Path.Combine(assetDirectory, letterEntry.Value.Path)) },
            { "OffsetX", letterEntry.Value.OffsetX },
            { "OffsetY", letterEntry.Value.OffsetY },
            { "BaseWidth", letterEntry.Value.BaseWidth },
            { "BaseHeight", letterEntry.Value.BaseHeight },
            { "Width", letterEntry.Value.Width },
            { "Height", letterEntry.Value.Height },
        };

        private TinyObject fontEffectToTinyObject(FontEffect fontEffect)
        {
            var effectType = fontEffect.GetType();
            var cache = new TinyObject
            {
                ["Type"] = effectType.FullName,
            };

            foreach (var field in effectType.GetFields())
            {
                var fieldType = field.FieldType;
                if (fieldType == typeof(Color4))
                {
                    var color = (Color4)field.GetValue(fontEffect);
                    cache[$"{field.Name}R"] = color.R;
                    cache[$"{field.Name}G"] = color.G;
                    cache[$"{field.Name}B"] = color.B;
                    cache[$"{field.Name}A"] = color.A;
                }
                else if (fieldType == typeof(Vector3))
                {
                    var vector = (Vector3)field.GetValue(fontEffect);
                    cache[$"{field.Name}X"] = vector.X;
                    cache[$"{field.Name}Y"] = vector.Y;
                    cache[$"{field.Name}Z"] = vector.Z;
                }
                else if (fieldType == typeof(Vector2))
                {
                    var vector = (Vector2)field.GetValue(fontEffect);
                    cache[$"{field.Name}X"] = vector.X;
                    cache[$"{field.Name}Y"] = vector.Y;
                }
                else if (fieldType == typeof(double))
                    cache[field.Name] = (double)field.GetValue(fontEffect);
                else if (fieldType == typeof(float))
                    cache[field.Name] = (float)field.GetValue(fontEffect);
                else if (fieldType == typeof(int) || fieldType.IsEnum)
                    cache[field.Name] = (int)field.GetValue(fontEffect);
                else if (fieldType == typeof(string))
                    cache[field.Name] = (string)field.GetValue(fontEffect);
                else throw new InvalidDataException($"Unexpected field type {fieldType} for {field.Name} in {effectType.FullName}");
            }

            return cache;
        }
    }
}