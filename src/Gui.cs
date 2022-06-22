using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pipoga
{
    /// <summary>
    /// Holds a collection of interactive buttons (which basically a GUI is)
    /// and a bunch of other graphical stuff.
    /// </summary>
    public class Gui : IObserver<Input>, IRasterizable
    {
        List<Button> _buttons;
        Cursor _cursor;

        public Gui(Cursor cursor, Input input)
        {
            input.Subscribe(this);
            _cursor = cursor;
            _buttons = new List<Button>(0xff);
        }

        /// <summary>
        /// Add a new button to the interface that listens to input like the
        /// mouse.
        /// </summary>
        /// <param name="button">The new button to add to the GUI.</param>
        public void Add(Button button)
        {
            _buttons.Add(button);
        }

        public void AddRange(IEnumerable<Button> buttons)
        {
            _buttons.AddRange(buttons);
        }

        /// <summary>
        /// Check that a point collides with the GUI.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if the point is over any GUI-element.</returns>
        public bool IsOver(Point point)
        {
            // TODO This feels misleading, as the Cursor is also part of GUI...
            foreach (var button in _buttons)
            {
                if (button.Body.ToRectangle().Contains(point))
                {
                    return true;
                }
            }
            return false;
        }

        // NOTE These 3 methods are `virtual` because MSDN shows so:
        // https://docs.microsoft.com/en-us/dotnet/standard/events/how-to-implement-an-observer#example
        public virtual void OnCompleted() { }

        public virtual void OnError(Exception error) { }

        public virtual void OnNext(Input input)
        {
            bool isMousePointing = false;
            foreach (var button in _buttons)
            {
                isMousePointing |= button.Update(input.Mouse);
            }
            _cursor.Update(input.Mouse, isMousePointing);
        }

        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            foreach (var button in _buttons)
            {
                foreach (var vertex in button.GetVertices(inversePixelSize))
                {
                    yield return vertex;
                }
            }

            foreach (var vertex in _cursor.GetVertices(inversePixelSize))
            {
                yield return vertex;
            }
        }
    }
}
