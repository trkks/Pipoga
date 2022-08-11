using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Pipoga
{
    /// <summary>
    /// Struct representing a single screen point, a pixel with a color.
    /// </summary>
    public struct Vertex
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public Color Color { get; set; }

        public Vertex(int x, int y, Color color)
        {
            X = x;
            Y = y;
            Color = color;
        }

        public Vector2 Position => new Vector2(X, Y);

        public override string ToString()
        {
            return $"X:{X} Y:{Y}";
        }
    }

    /// <summary>
    /// Interface for getting the vertices of an object on screen based on
    /// pixel size. The vertices can then be used to draw the implementing
    /// object onto a two dimensional screen.
    /// </summary>
    public interface IRasterizable
    {
         /// <summary>
         /// Return the vertices one after another transformed based on pixel
         /// size.
         /// </summary>
         /// <param name="inversePixelSize">_Inverse_ (1/N) of the pixel size
         /// to reduce the amount of divisions.</param>
         /// <returns>Enumerator of scaled object vertices.</returns>
         IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize);
    }
}
