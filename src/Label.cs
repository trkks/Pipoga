using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pipoga
{
    public class Label : IRasterizable
    {
        string _text;
        Point _position;
        Color _color;
        public SpriteFont Font { get; set; }
        public string Text
        { 
            get => _text;
            set { _text = value; }
        }

        public Label(string text, Point position, Color color)
        {
            _text = text;
            _position = position;
            _color = color;
        }

        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            // Get font's pixels as array of colors.
            Texture2D fontTexture = Font.Texture;
            var sprite = new Color[fontTexture.Width * fontTexture.Height];
            fontTexture.GetData(sprite);

            // Get a mapping of characters to the font's glyphs.
            var fontGlyphs = Font.GetGlyphs();

            // The characters need to be separated by each others dimensions.
            Point offset = new Point(0);

            foreach (var c in _text)
            {
                // Get the next character's glyph from font. NOTE Uses the
                // default character if given character is not supported.
                var glyph = fontGlyphs[c];
                System.Console.WriteLine(c);
                System.Console.WriteLine(glyph);

                // Get the character's sprite from the font texture.
                Rectangle bounds = glyph.BoundsInTexture;

                // Form vertices from the pixels of the sprite.
                for (int y = 0; y < bounds.Height; y++)
                {
                    for (int x = 0; x < bounds.Width; x++)
                    {
                        int index = (y + bounds.Y)
                            * fontTexture.Width
                            + (x + bounds.X);
                        var col = sprite[index];
                        var pos = 
                            (_position.ToVector2() * inversePixelSize).ToPoint()
                            + offset
                            + new Point(x, y);
                        yield return new Vertex(pos.X, pos.Y, col);
                    }
                }

                // Change the offset according to this glyph.
                // TODO Account for vertical offset and kerning as well.
                offset.X += bounds.Width;
            }
        }
    }
}
