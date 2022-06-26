using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Pipoga;

namespace Pipoga
{
    public class Circle : IRasterizable
    {
        public float radius;
        public Vector2 center;
        
        public Circle(float radius, Vector2 center)
        {
            this.radius = radius;
            this.center = center;
        }

        /// <summary>
        /// Algorithm from:
        /// https://en.wikipedia.org/wiki/Midpoint_circle_algorithm#Variant_with_integer-based_arithmetic
        /// </summary>
        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            // NOTE Non-square pixels not allowed (TODO)
            if (inversePixelSize.X != inversePixelSize.Y)
            {
                throw new Exception(
                    "Cannot rasterize circle with non-square pixels"
                );
            }
            int pixelRadius = (int)((float)radius * inversePixelSize.X);
            Point translation = (center * inversePixelSize).ToPoint();
            Func<int, int, int> radiusError = (x, y) =>
                {
                    int re = x * x + y * y - pixelRadius * pixelRadius;
                    return re < 0 ? -re : re;
                };

            // Generate one octant (1/8) of the circle.
            // Move counter-clockwise starting from the
            // coordinate (radius, 0).
            var octant = new List<Point>(0xff);
            (int xi, int yi) = (pixelRadius, 0);
            while (xi > yi)
            {
                octant.Add(new Point(xi, yi));

                yi++;
                if (radiusError(xi - 1, yi) < radiusError(xi, yi))
                {
                    xi--;
                }
            }

            // Return the original octant first.
            foreach (var point in octant)
            {
                Point p = point;
                yield return new Vertex(p.X + translation.X, p.Y + translation.Y, Color.Blue);
                // Return 90-degree-rotated as well.
                yield return new Vertex(-p.Y + translation.X, p.X + translation.Y, Color.Blue);
            }

            // Generate all the other 7 octants by reflections.
            Func<Point, Point, Point> reflect = (v, l) =>
                new Point(2)
                * new Point((v.X * l.X + v.Y * l.Y) / (l.X * l.X + l.Y * l.Y))
                * l
                - v;

            foreach (var across in new[] {
                new Point(0, 1),
                new Point(1, 0),
            })
            {
                foreach (var point in octant)
                {
                    Point p = reflect(point, across);

                    yield return new Vertex(p.X + translation.X, p.Y + translation.Y, Color.Blue);
                    // Return 90-degree-rotated as well.
                    yield return new Vertex(-p.Y + translation.X, p.X + translation.Y, Color.Blue);
                }
            }

            // Generate the reflection across X and then Y.
            foreach (var point in octant)
            {
                Point p = reflect(reflect(point, new Point(0, 1)), new Point(1,0));

                yield return new Vertex(p.X + translation.X, p.Y + translation.Y, Color.Blue);
                // Return 90-degree-rotated as well.
                yield return new Vertex(-p.Y + translation.X, p.X + translation.Y, Color.Blue);
            }
        }
    }
}
