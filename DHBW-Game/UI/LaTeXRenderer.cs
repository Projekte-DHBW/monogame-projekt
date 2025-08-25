using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;
using CSharpMath.SkiaSharp;
using Typography.OpenFont;

public static class LaTeXRenderer
{
    /// <summary>
    /// Renders the given LaTeX string to a Texture2D, supporting mixed normal text and inline math formulas.
    /// Normal text uses the provided font (defaults to Press Start 2P).
    /// Formulas are rendered using the provided math OTF font (defaults to Concrete-Math.otf).
    /// Assumes inline math delimited by \( and \).
    /// Use verbatim strings for LaTeX input, e.g., @"Hello, \( E = mc^2 \>".
    /// </summary>
    /// <param name="graphicsDevice">The MonoGame GraphicsDevice used to create the texture.</param>
    /// <param name="latex">The LaTeX string to render, potentially containing text and inline math.</param>
    /// <param name="textFontPath">Path to the font file for normal text (TTF/OTF). Defaults to "Content/fonts/PressStart2P-Regular.ttf".</param>
    /// <param name="mathFontPath">Path to the math OTF font file. Defaults to "Content/fonts/Concrete-Math.otf".</param>
    /// <param name="textFontSize">Font size in points for normal text.</param>
    /// <param name="mathFontSize">Font size in points for math text.</param>
    /// <param name="color">Color for the text (both normal and math parts). Defaults to Color.Black if not specified.</param>
    /// <param name="maxWidth">Maximum width in pixels before wrapping to a new line. Defaults to float.PositiveInfinity for no wrapping.</param>
    /// <returns>A Texture2D containing the rendered content, or a 1x1 fallback texture on error.</returns>
    public static Texture2D Render(GraphicsDevice graphicsDevice, string latex, string textFontPath = "Content/fonts/PressStart2P-Regular.ttf", string mathFontPath = "Content/fonts/Concrete-Math.otf", float textFontSize = 20f, float mathFontSize = 20f, Color color = default, float maxWidth = float.PositiveInfinity)
    {
        if (color == default) color = Color.Black; // Default to black if no color is provided

        try
        {
            var bitmap = RenderToBitmap(latex, textFontPath, mathFontPath, textFontSize, mathFontSize, color, maxWidth);

            if (bitmap == null) return new Texture2D(graphicsDevice, 1, 1);

            // Convert SKBitmap to Texture2D by copying pixel data (assuming RGBA order)
            var texture = new Texture2D(graphicsDevice, bitmap.Width, bitmap.Height);
            var pixelData = new Color[bitmap.Width * bitmap.Height];
            var span = bitmap.GetPixelSpan();

            for (int i = 0; i < pixelData.Length; i++)
            {
                int offset = i * 4;
                byte r = span[offset + 0];
                byte g = span[offset + 1];
                byte b = span[offset + 2];
                byte a = span[offset + 3];
                pixelData[i] = new Color(r, g, b, a);
            }

            texture.SetData(pixelData);

            return texture;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering to texture: {ex.Message}");
            return new Texture2D(graphicsDevice, 1, 1); // Return a fallback 1x1 texture on failure
        }
    }

    /// <summary>
    /// Renders the given LaTeX string to a PNG file for testing or debugging purposes.
    /// </summary>
    /// <param name="latex">The LaTeX string to render, potentially containing text and inline math.</param>
    /// <param name="textFontPath">Path to the font file for normal text (TTF/OTF). Defaults to "Content/fonts/PressStart2P-Regular.ttf".</param>
    /// <param name="mathFontPath">Path to the math OTF font file. Defaults to "Content/fonts/Concrete-Math.otf".</param>
    /// <param name="textFontSize">Font size in points for normal text.</param>
    /// <param name="mathFontSize">Font size in points for math text.</param>
    /// <param name="color">Color for the text (both normal and math parts). Defaults to Color.Black if not specified.</param>
    /// <param name="outputPath">Path to save the PNG file. Defaults to "output.png".</param>
    /// <param name="maxWidth">Maximum width in pixels before wrapping to a new line. Defaults to float.PositiveInfinity for no wrapping.</param>
    public static void RenderToFile(string latex, string textFontPath = "Content/fonts/PressStart2P-Regular.ttf", string mathFontPath = "Content/fonts/Concrete-Math.otf", float textFontSize = 20f, float mathFontSize = 20f, Color color = default, string outputPath = "output.png", float maxWidth = float.PositiveInfinity)
    {
        if (color == default) color = Color.Black; // Default to black if no color is provided

        try
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var bitmap = RenderToBitmap(latex, textFontPath, mathFontPath, textFontSize, mathFontSize, color, maxWidth);

            if (bitmap == null) return;

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(outputPath);
            data.SaveTo(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering to file '{outputPath}': {ex.Message}");
        }
    }

    /// <summary>
    /// Internal method to render the LaTeX string to an SKBitmap, handling parsing, tokenization, line wrapping, and drawing.
    /// </summary>
    /// <param name="latex">The LaTeX string to render.</param>
    /// <param name="textFontPath">Path to the text font file.</param>
    /// <param name="mathFontPath">Path to the math font file.</param>
    /// <param name="textFontSize">Font size for text.</param>
    /// <param name="mathFontSize">Font size for math.</param>
    /// <param name="color">Rendering color.</param>
    /// <param name="maxWidth">Maximum line width in pixels.</param>
    /// <returns>An SKBitmap with the rendered content, or null if no content is renderable.</returns>
    private static SKBitmap RenderToBitmap(string latex, string textFontPath, string mathFontPath, float textFontSize, float mathFontSize, Color color, float maxWidth)
    {
        SKTypeface typeface = null;
        try
        {
            typeface = SKTypeface.FromFile(textFontPath);
            if (typeface == null) throw new Exception("Typeface is null.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load normal text font from '{textFontPath}': {ex.Message}. Falling back to default.");
        }

        using var font = new SKFont(typeface, textFontSize);
        using var paint = new SKPaint();
        paint.Color = new SKColor(color.R, color.G, color.B, color.A);
        paint.IsAntialias = true; // Disable if a sharper, more pixelated rendering is preferred

        Typeface mathTypeface = null;
        if (!string.IsNullOrEmpty(mathFontPath))
        {
            try
            {
                using var mathStream = File.OpenRead(mathFontPath);
                var reader = new OpenFontReader();
                mathTypeface = reader.Read(mathStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load math font from '{mathFontPath}': {ex.Message}. Falling back to default.");
            }
        }

        // Parse the LaTeX string into alternating text and math parts
        var parts = ParseLaTeX(latex);

        float textAscent = -font.Metrics.Ascent;
        float textDescent = font.Metrics.Descent;

        // Tokenize parts into words, spaces, and math tokens for line wrapping
        var tokens = new List<Token>();
        foreach (var part in parts)
        {
            if (part is MathPart mp)
            {
                var painter = new MathPainter
                {
                    LaTeX = mp.Math,
                    FontSize = mathFontSize,
                    TextColor = new SKColor(color.R, color.G, color.B, color.A)
                };
                if (mathTypeface != null)
                {
                    painter.LocalTypefaces = new Typeface[] { mathTypeface };
                }
                var measure = painter.Measure(float.PositiveInfinity);
                tokens.Add(new Token
                {
                    Content = mp.Math,
                    IsMath = true,
                    Width = measure.Width,
                    Ascent = -measure.Top,
                    Descent = measure.Bottom
                });
            }
            else if (part is TextPart tp)
            {
                string text = tp.Text;
                int i = 0;
                while (i < text.Length)
                {
                    if (text[i] == ' ')
                    {
                        int start = i;
                        i++;
                        while (i < text.Length && text[i] == ' ') i++;
                        string spaces = text.Substring(start, i - start);
                        tokens.Add(new Token
                        {
                            Content = spaces,
                            IsSpace = true,
                            Width = font.MeasureText(spaces),
                            Ascent = textAscent,
                            Descent = textDescent
                        });
                    }
                    else
                    {
                        int start = i;
                        i++;
                        while (i < text.Length && text[i] != ' ') i++;
                        string word = text.Substring(start, i - start);
                        tokens.Add(new Token
                        {
                            Content = word,
                            IsSpace = false,
                            Width = font.MeasureText(word),
                            Ascent = textAscent,
                            Descent = textDescent
                        });
                    }
                }
            }
        }

        // Layout tokens into lines, applying word wrapping based on maxWidth
        var lines = new List<Line>();
        var currentLine = new Line();
        float currentLineWidth = 0f;
        for (int tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
        {
            var token = tokens[tokenIndex];
            float w = token.Width;
            if (token.IsSpace)
            {
                if (currentLine.Parts.Count == 0) continue; // Skip leading spaces

                if (currentLineWidth + w > maxWidth)
                {
                    currentLine.Width = currentLineWidth;
                    lines.Add(currentLine);
                    currentLine = new Line();
                    currentLineWidth = 0f;
                    continue; // Skip space at start of new line
                }

                currentLine.Parts.Add(new TextPart { Text = token.Content });
                currentLineWidth += w;
                currentLine.MaxAscent = Math.Max(currentLine.MaxAscent, token.Ascent);
                currentLine.MaxDescent = Math.Max(currentLine.MaxDescent, token.Descent);
            }
            else // Word or math token
            {
                if (currentLineWidth + w > maxWidth && currentLine.Parts.Count > 0)
                {
                    currentLine.Width = currentLineWidth;
                    lines.Add(currentLine);
                    currentLine = new Line();
                    currentLineWidth = 0f;
                }

                Part newPart = token.IsMath ? (Part)new MathPart { Math = token.Content } : new TextPart { Text = token.Content };
                currentLine.Parts.Add(newPart);
                currentLineWidth += w;
                currentLine.MaxAscent = Math.Max(currentLine.MaxAscent, token.Ascent);
                currentLine.MaxDescent = Math.Max(currentLine.MaxDescent, token.Descent);
            }
        }
        if (currentLine.Parts.Count > 0)
        {
            currentLine.Width = currentLineWidth;
            lines.Add(currentLine);
        }

        // Calculate overall dimensions for the bitmap
        float padding = 0f;
        float maxLineWidth = 0f;
        float totalHeight = 0f;
        foreach (var line in lines)
        {
            maxLineWidth = Math.Max(maxLineWidth, line.Width);
            totalHeight += line.MaxAscent + line.MaxDescent;
        }

        if (lines.Count == 0 || totalHeight == 0 || maxLineWidth == 0) return null;

        // Create the bitmap and canvas for rendering
        var info = new SKImageInfo((int)MathF.Ceiling(maxLineWidth), (int)MathF.Ceiling(totalHeight), SKColorType.Rgba8888);
        var bitmap = new SKBitmap(info);
        using var canvas = new SKCanvas(bitmap);

        float currentY = padding;
        foreach (var line in lines)
        {
            float baselineY = currentY + line.MaxAscent;
            float currentX = 0f;
            foreach (var part in line.Parts)
            {
                if (part is TextPart textPart)
                {
                    canvas.DrawText(textPart.Text, currentX, baselineY, SKTextAlign.Left, font, paint);
                    currentX += font.MeasureText(textPart.Text);
                }
                else if (part is MathPart mathPart)
                {
                    var painter = new MathPainter
                    {
                        LaTeX = mathPart.Math,
                        FontSize = mathFontSize,
                        TextColor = new SKColor(color.R, color.G, color.B, color.A)
                    };
                    if (mathTypeface != null)
                    {
                        painter.LocalTypefaces = new Typeface[] { mathTypeface };
                    }
                    painter.Draw(canvas, currentX, baselineY);
                    currentX += painter.Measure(float.PositiveInfinity).Width;
                }
            }
            currentY += line.MaxAscent + line.MaxDescent;
        }

        return bitmap;
    }

    /// <summary>
    /// Base class for parsed parts of the LaTeX string (text or math).
    /// </summary>
    private abstract class Part { }

    /// <summary>
    /// Represents a text segment in the parsed LaTeX.
    /// </summary>
    private class TextPart : Part { public string Text { get; set; } }

    /// <summary>
    /// Represents a math formula segment in the parsed LaTeX.
    /// </summary>
    private class MathPart : Part { public string Math { get; set; } }

    /// <summary>
    /// Represents a tokenized element (word, space, or math) for layout and wrapping.
    /// </summary>
    private class Token
    {
        public string Content { get; set; }
        public bool IsSpace { get; set; }
        public bool IsMath { get; set; }
        public float Width { get; set; }
        public float Ascent { get; set; }
        public float Descent { get; set; }
    }

    /// <summary>
    /// Represents a single line in the laid-out text, containing parts and metrics.
    /// </summary>
    private class Line
    {
        public List<Part> Parts { get; set; } = new List<Part>();
        public float MaxAscent { get; set; }
        public float MaxDescent { get; set; }
        public float Width { get; set; }
    }

    /// <summary>
    /// Parses the LaTeX string into a list of text and math parts, identifying inline math delimited by \( and \).
    /// Handles unbalanced delimiters by treating remaining content as text.
    /// </summary>
    /// <param name="latex">The input LaTeX string.</param>
    /// <returns>A list of parsed Part objects (TextPart or MathPart).</returns>
    private static List<Part> ParseLaTeX(string latex)
    {
        var parts = new List<Part>();
        int index = 0;
        while (index < latex.Length)
        {
            int startMath = latex.IndexOf("\\(", index);
            if (startMath == -1)
            {
                parts.Add(new TextPart { Text = latex.Substring(index) });
                break;
            }

            if (startMath > index)
            {
                parts.Add(new TextPart { Text = latex.Substring(index, startMath - index) });
            }

            int endMath = latex.IndexOf("\\)", startMath + 2);
            if (endMath == -1)
            {
                // Treat rest as text if no closing delimiter
                parts.Add(new TextPart { Text = latex.Substring(startMath) });
                break;
            }

            string math = latex.Substring(startMath + 2, endMath - startMath - 2).Trim();
            parts.Add(new MathPart { Math = math });

            index = endMath + 2;
        }
        return parts;
    }
}