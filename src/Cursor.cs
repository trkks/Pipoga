using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pipoga
{
    /// <summary>
    /// Contains the image of the cursor and the offset on it to match with
    /// actual mouse position on screen.
    /// </summary>
    public struct CursorIcon
    {
        public Texture2D image;
        public Point offset;

        public int Width => image.Width;
        public int Height => image.Height;
        /// <summary>
        /// Construct a new CursorIcon.
        /// </summary>
        /// <param name="image">Image of the icon.</param>
        /// <param name="point">The point on image that is pointed with.</param>
        /// <exception>
        /// Generic exception (TODO) if the point is outside of image.
        /// </exception>
        public CursorIcon(Texture2D image, Point point)
        {
            this.image = image;
            if (!image.Bounds.Contains(point))
            {
                throw new Exception(
                    $"Given point ({point}) is outside the bounds "
                    + $"({image.Bounds}) of the cursor image."
                );
            }
            this.offset = point;
        }
    }

    /// <summary>
    /// A cursor with appearance changing based on events.
    /// </summary>
    public class Cursor : IRasterizable
    {
        CursorIcon _defaultIcon;
        CursorIcon _pointer;
        CursorIcon _pressed;
        CursorIcon _current;
        Point _position;

        /// <summary>
        /// Create a new cursor with icons for its states.
        ///
        /// The icon names are same as in CSS:
        /// https://developer.mozilla.org/en-US/docs/Web/CSS/cursor#values
        /// </summary>
        public Cursor(CursorIcon defaultIcon, CursorIcon pointer)
        {
            _defaultIcon = defaultIcon;
            _pointer = pointer;
            {
                // The pressed-icon will be same as pointer but scaled smaller.
                float scale = 0.8f;
                var size = new Point(
                    (int)((float)_pointer.Width * scale),
                    (int)((float)_pointer.Height * scale)
                );
                var location = new Point(
                    (int)((float)_pointer.Width - size.X),
                    (int)((float)_pointer.Height - size.Y)
                );
                var pressedRect = new Rectangle(location, size);
                int length = size.X * size.Y;
                Color[] pressedData = new Color[length];
                _pointer.image.GetData(
                    level: 0,
                    rect: pressedRect,
                    data: pressedData,
                    startIndex: 0,
                    elementCount: length
                );
                _pressed.image = new Texture2D(
                    _pointer.image.GraphicsDevice,
                    pressedRect.Width,
                    pressedRect.Height
                );
                _pressed.image.SetData(
                    level: 0,
                    rect: null,
                    data: pressedData,
                    startIndex: 0,
                    elementCount: length
                );
            }
            _current = defaultIcon;
        }

        public void Update(MouseState mouse, bool pointing=false)
        {
            // Change between icons if over a pointee (TODO need more options?).
            _current =
                mouse.m1IsDown 
                ? _pressed
                : pointing
                ? _pointer
                : _defaultIcon;
            _position = mouse.position;
        }

        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            int size = _current.image.Width * _current.image.Height;
            var sprite = new Color[size];
            _current.image.GetData(sprite);

            for (int y = 0; y < _current.image.Height; y++)
            {
                for (int x = 0; x < _current.image.Width; x++)
                {
                    // Set screen point of cursor to the color on its sprite.
                    var col = sprite[y * _current.image.Width + x];
                    var pos =
                        (_position.ToVector2() * inversePixelSize).ToPoint()
                        - _current.offset
                        + new Point(x, y);
                    yield return new Vertex(pos.X, pos.Y, col);
                }
            }
        }
    }
}
