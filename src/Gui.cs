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
        // TODO Separate the elements into IInputListener | IRasterizable |
        // IOtherEnum or smth.
        List<IRasterizable> _elements;
        Cursor _cursor;

        public Gui(Cursor cursor, Input input)
        {
            input.Subscribe(this);
            _cursor = cursor;
            _elements = new List<IRasterizable>(0xff);
        }

        /// <summary>
        /// Add a new element to the interface to visualize.
        /// </summary>
        /// <param name="button">The new element to add to the GUI.</param>
        public void Add(IRasterizable elem)
        {
            _elements.Add(elem);
        }

        public void AddRange(IEnumerable<IRasterizable> elems)
        {
            _elements.AddRange(elems);
        }

        /// <summary>
        /// Check that a point collides with the GUI.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if the point is over any GUI-element.</returns>
        public bool IsOver(Point point)
        {
            // TODO This feels misleading, as the Cursor is also part of GUI...
            foreach (var elem in _elements)
            {
                if (elem is Button
                    && ((Button)elem).Body.Contains(point.ToVector2()))
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
            foreach (var elem in _elements)
            {
                // TODO Make some UI-elements IFocusable or smth.
                isMousePointing |=
                    elem is Button
                    ? ((Button)elem).Update(input.Mouse)
                    : elem is Slider
                    ? ((Slider)elem).Update(input.Mouse)
                    : false;
            }
            _cursor.Update(input.Mouse, isMousePointing);
        }

        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            // TODO Use a possibly layered/prioritized collection of
            //  IRasterizables to iterate over in a single loop.
            //  For example labels render on top of buttons (because buttons
            //  could contain labels?)
            foreach (var elem in _elements)
            {
                foreach (var vertex in elem.GetVertices(inversePixelSize))
                {
                    yield return vertex;
                }
            }

            // Cursor renders on top of everything so that user can see it at
            // all times.
            foreach (var vertex in _cursor.GetVertices(inversePixelSize))
            {
                yield return vertex;
            }
        }
    }
}
