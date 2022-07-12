using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using Pipoga;

namespace Pipoga
{
    public class Circle : IRasterizable, IArea
    {
        public float _radius;
        public Vector2 _center;

        public Circle(float radius, Vector2 center)
        {
            _radius = radius;
            _center = center;
        }

        public bool Contains(Vector2 p)
        {
            return (p - _center).Length() <= _radius;
        }

        /// <summary>
        /// Algorithm from:
        /// https://en.wikipedia.org/wiki/Midpoint_circle_algorithm#Variant_with_integer-based_arithmetic
        /// </summary>
        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            // TODO Scaling only based on X-component does not work with
            // non-square pixels.
            int pixelRadius = (int)(inversePixelSize.X * _radius);
            Point translation = (_center * inversePixelSize).ToPoint();

            // Generate one octant (1/8) of the circle.
            // Move counter-clockwise starting from the
            // coordinate (radius, 0).
            var origOctant = new List<(int, int)>(0xff);
            (int xi, int yi) = (pixelRadius, 0);
            // Precompute square radius.
            int rr = pixelRadius * pixelRadius;
            while (xi >= yi)
            {
                origOctant.Add((xi, yi));

                yi++;
                int a =
                    (((xi * xi + yi * yi - rr) + ((yi << 1) + 1)) << 1)
                    + (1 - (xi << 1));
                if (a > 0)
                {
                    xi--;
                }
            }

            // The other 7 octants are generated by reflecting and rotating the
            // original.
            Func<(int, int), (int, int), (int, int)> refl =
                (v, l) =>
                {
                    (int vx, int vy) = v;
                    (int lx, int ly) = l;
                    int a = ((vx * lx + vy * ly) << 1) / (lx * lx + ly * ly);
                    return (a * lx - vx, a * ly - vy);
                };

            foreach (var (octant, color) in new[]
                {
                    (origOctant,
                        Color.Red),
                    (origOctant.Select(p => refl(p, (0, 1))),
                        Color.Green),
                    (origOctant.Select(p => refl(p, (1, 0))),
                        Color.Blue),
                    (origOctant.Select(p => refl(refl(p, (0, 1)), (1, 0))),
                        Color.Purple)
                }
            )
            {
                foreach (var (x, y) in octant)
                {
                    yield return new Vertex(
                        x + translation.X,
                        y + translation.Y,
                        color);
                    // Return 90-degree-rotated as well.
                    yield return new Vertex(
                        -y + translation.X,
                        x + translation.Y,
                        Color.Lerp(color, Color.Black, 0.5f));
                }
            }
        }
    }
}
