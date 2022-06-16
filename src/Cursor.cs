using System;

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
    public class Cursor
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
    }
}
