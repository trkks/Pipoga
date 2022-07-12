using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Pipoga;

namespace Pipoga
{
    public class Slider : IRasterizable
    {
        public int Value { get; set; }
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }

        int _max;
        int _min;
        int _step;
        // TODO Is this field even needed?
        bool _grabbed;
        Button _slide;

        Rectangle Body { get; set; }

        // TODO Allow the value to be generic ie. float, int, string (array)?
        public Slider(
            int min,
            int max,
            Rectangle body,
            int step=1,
            Color? backgroundColor=null,
            Color? foregroundColor=null)
        {
            if (max < min)
            {
                throw new Exception(
                    $"The maximum value {max} of slider is not greater than "
                    + $"the minimum value {min}"
                );
            }
            _max = max;
            _min = min;
            _step = step;
            _grabbed = false;
            Body = body;

            // TODO Calculate the width based on body size as well.
            var slideSize = new Point(20, (int)(Body.H * 0.8));
            // Position in the middle of the body's rectangle.
            var slidePos = new Point(
                (int)(Body.X + Body.W * 0.5f - slideSize.X * 0.5f),
                (int)(Body.Y + Body.H - slideSize.Y * 0.5f)
            );
            _slide = new Button(
                slidePos,
                slideSize,
                (b) => { this._grabbed = true; }
            );

            // Set value to the middlepoint.
            Value = min + (min - max) / 2;
            BackgroundColor = backgroundColor ?? Color.Black;
            ForegroundColor = foregroundColor ?? Color.White;
        }

        /// <summary>
        /// Update the slider according to mouse state. A slider changes when
        /// it is active and a mouse clicks and drags over it and changes its
        /// value.
        /// </summary>
        /// <param name="mouse">State of the mouse.</param>
        public bool Update(MouseState mouse)
        {
            if (Body.Contains(mouse.position.ToVector2()))
            {
                if (_grabbed && mouse.m1IsDown)
                {
                    // Set and constrain the value by mouse position along this
                    // slider's axis. TODO Support vertical axis.
                    Value = Math.Clamp(
                        (Body.Center().ToPoint() - mouse.position).X,
                        _min,
                        _max
                    );
                    // Move the slide according to the current value relative
                    // to the slider's middlepoint.
                    _slide.SetPosition(new Point(
                        (int)(Body.X + Body.W * 0.5f - _slide.Size.X * 0.5f)
                            - Value,
                        (int)(Body.Y + Body.H - _slide.Size.Y * 0.5f)
                    ));
                }
                else
                {
                    _grabbed = false;
                }
            }
            return _slide.Update(mouse);
        }

        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            foreach (var vertex in Body.GetVertices(inversePixelSize))
            {
                yield return vertex;
            }

            foreach (var vertex in _slide.GetVertices(inversePixelSize))
            {
                yield return new Vertex(
                    vertex.X,
                    vertex.Y,
                    vertex.Color
                );
            }
        }
    }
}
