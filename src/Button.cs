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
        public RectangleBody Body { get; private set; }
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }

        readonly Color originalBackground;
        readonly Color originalForeground;
        Action callback;

        bool entered;

        public Button(
            Point position,
            Point size,
            Action onClick = null,
            Color? backgroundColor = null,
            Color? foregroundColor = null)
        {
            Body = new RectangleBody(position.ToVector2(), size.ToVector2());
            this.callback = onClick ?? (() => {});
            BackgroundColor = backgroundColor ?? Color.Black;
            ForegroundColor = foregroundColor ?? Color.White;
            originalBackground = BackgroundColor;
            originalForeground = ForegroundColor;

            entered = false;
        }

        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            // First the rectangle inside.
            // The buttons are rectangles (at least for now).
            var r = new Rectangle(
                (Body.position * inversePixelSize).ToPoint(),
                (Body.size * inversePixelSize).ToPoint()
            );
            for (int i = 0; i < r.Height; i++)
            {
                int y = r.Y + i;
                for (int j = 0; j < r.Width; j++)
                {
                    int x = r.X + j;
                    yield return new Vertex(x, y, BackgroundColor);
                }
            }

            // TODO Using this for the properties feels stupid...
            var rb = new RectangleBody(r);

            // Then the borders.
            foreach (var line in Enumerable.Select(
                new[] {
                    (rb.TopLeft,     rb.TopRight),
                    (rb.TopRight,    rb.BottomRight),
                    (rb.BottomRight, rb.BottomLeft),
                    (rb.BottomLeft,  rb.TopLeft),
                },
                t => new Line(t.Item1, t.Item2)
            ))
            {
                // NOTE This pattern of unwrapping the iterator is dumb and C#
                // is dumb >:(
                foreach (var x in line.GetVertices(inversePixelSize).Select(
                    // TODO Implement style for Line, which contains coloring.
                    v => new Vertex(v.X, v.Y, ForegroundColor)
                ))
                {
                    yield return x;
                }
            }
        }

        public ButtonEvent Update(MouseState mouse)
        {
            if (Body.ToRectangle().Contains(mouse.position))
            {
                OnHoverEnter();
                // Mouse clicks are reacted to immediately and only once.
                if (mouse.m1WasDown)
                {
                    OnClick();
                    return ButtonEvent.Click;
                }
                else
                {
                    return ButtonEvent.HoverEnter;
                }
            }
            else
            {
                OnHoverExit();
                return ButtonEvent.HoverExit;
            }
            // Dead code
            return ButtonEvent.None;
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
            callback();
        }
    }
}
