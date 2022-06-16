using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pipoga
{
    public enum MouseEvent
    {
        None,
        HoverEnter,
        HoverExit,
    }
    /// <summary>
    /// A cursor with appearance changing based on events.
    /// </summary>
    public class Cursor : IRasterizable
    {
        public Point Position { get; private set; }

        public MouseEvent State
        {
            get => _state;
            set
            {
                switch (value)
                {
                case MouseEvent.HoverEnter:
                    Current = Hover;
                    break;
                case MouseEvent.HoverExit:
                    Current = Default;
                    break;
                default:
                    Current = Default;
                    break;
                }
                _state = value;
            }
        }
        public Texture2D Current { get; set; }
        public Texture2D Default
        {
            private get => _default;
            set
            {
                // Initialize current at the same time to the default.
                Current = value;
                _default = value;
            }
        }
        public Texture2D Hover { private get; set; }

        private MouseEvent _state = MouseEvent.None;
        private Texture2D _default;

        public void Update(MouseState state)
        {
            Position = state.position;
        }

        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            var sprite = new Color[Current.Width * Current.Height];
            Current.GetData(sprite);

            for (int i = 0; i < Current.Height; i++)
            {
                for (int j = 0; j < Current.Width; j++)
                {
                    // Set screen point of cursor to the color on its sprite.
                    var col = sprite[i * Current.Width + j];
                    var pos =
                        (Position.ToVector2() * inversePixelSize).ToPoint()
                        + new Point(i, j);
                    yield return new Vertex(pos.X, pos.Y, col);
                }
            }
        }
    }
}
