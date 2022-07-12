using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pipoga
{
    public enum ButtonEvent
    {
        None,
        HoverEnter,
        HoverExit,
        Click,
    }

    public class Button : IRasterizable
    {
        /// <summary>
        /// Private setter as not to change at will. Separate methods are
        /// better for that if ever needed.
        /// </summary>
        public Rectangle Body { get; private set; }
        public Point Size { get => Body.Size.ToPoint(); } // TODO Blehh..
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }

        readonly Color originalBackground;
        readonly Color originalForeground;
        Action<Button> callback;

        bool entered;

        public Button(
            Point position,
            Point size,
            Action<Button> onClick = null,
            Color? backgroundColor = null,
            Color? foregroundColor = null)
        {
            Body = new Rectangle(position.ToVector2(), size.ToVector2());
            callback = onClick ?? (_ => { });
            BackgroundColor = backgroundColor ?? Color.Black;
            ForegroundColor = foregroundColor ?? Color.White;
            originalBackground = BackgroundColor;
            originalForeground = ForegroundColor;

            entered = false;
        }

        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            // Buttons are rectangles (at least for now TODO).

            // First the rectangle inside.
            foreach (var v in Body.GetVertices(inversePixelSize))
            {
                yield return new Vertex(v.X, v.Y, BackgroundColor);
            }

            // Then the borders.
            foreach ((Vector2 s, Vector2 e) in new[]
            {
                // Right and bottom borders need to be translated to the inside
                // of the contained rectangle.
                // To the right,
                (Body.TLeft,                  Body.TRight - Vector2.UnitX),
                // down,
                (Body.TRight - Vector2.UnitX, Body.BRight - Vector2.One),
                // left,
                (Body.BRight - Vector2.One,   Body.BLeft - Vector2.UnitY),
                // and back up. FIXME XXX Bug in drawing the borders... XXX
                (Body.BLeft - Vector2.UnitY,  Body.TLeft),
            })
            {
                // NOTE This pattern of unwrapping the iterator is dumb and C#
                // is dumb >:(
                foreach (var v in new Line(s * inversePixelSize, e * inversePixelSize).GetVertices(inversePixelSize))
                {
                    // TODO Implement style for Line, which contains coloring?
                    yield return new Vertex(v.X, v.Y, ForegroundColor);
                }
            }
        }

        /// <summary>
        /// Update the button according to mouse state. A button changes for
        /// example if it is active and a mouse hovers over it or is clicked.
        /// </summary>
        /// <param name="mouse">State of the mouse.</param>
        /// <returns>True if the mouse is hovering over the button.</returns>
        public bool Update(MouseState mouse)
        {
            if (Body.Contains(mouse.position.ToVector2()))
            {
                OnHoverEnter();
                // Mouse clicks are reacted to immediately and only once.
                if (mouse.m1WasDown)
                {
                    OnClick();
                }
                return true;
            }
            else
            {
                OnHoverExit();
            }
            return false;
        }

        void OnHoverEnter()
        {
            if (!entered)
            {
                entered = true;
                var tmp = BackgroundColor;
                BackgroundColor = ForegroundColor;
                ForegroundColor = tmp;
            }
        }

        void OnHoverExit()
        {
            // Always reset to correct colors.
            entered = false;
            BackgroundColor = originalBackground;
            ForegroundColor = originalForeground;
        }

        void OnClick()
        {
            callback(this);
        }

        public void SetPosition(Point t)
        {
            Body.Position = t.ToVector2();
        }
    }
}
