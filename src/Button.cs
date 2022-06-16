using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pipoga
{
    public class Button
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

        public void ProcessMouseEvents(MouseState mouse)
        {
            if (Body.ToRectangle().Contains(mouse.position))
            {
                OnHoverEnter();
                // Mouse clicks are reacted to immediately and only once.
                if (mouse.m1WasDown)
                {
                    OnClick();
                }
            }
            else
            {
                OnHoverExit();
            }
        }

        public void OnHoverEnter()
        {
            if (!entered)
            {
                entered = true;
                var tmp = BackgroundColor;
                BackgroundColor = ForegroundColor;
                ForegroundColor = tmp;
            }
        }

        public void OnHoverExit()
        {
            // Always reset to correct colors.
            entered = false;
            BackgroundColor = originalBackground;
            ForegroundColor = originalForeground;
        }

        public void OnClick()
        {
            callback();
        }
    }
}

