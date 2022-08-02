using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Pipoga
{
    public class Line : IRasterizable
    {
        public readonly Vector2 start;
        public readonly Vector2 end;
        public readonly Vector2 dir;
        public readonly Vector2 diff;
        public readonly float len;

        public Color Color = Color.White;

        public Line(Vector2 start, Vector2 end)
        {
            this.start = start;
            this.end = end;
            diff = end - start;
            len = diff.Length();
            dir = Vector2.Normalize(diff);
        }

        public Line(int x0, int y0, int x1, int y1) : this(
            new Vector2(x0, y0),
            new Vector2(x1, y1)
        ) { }

        public Vector2 At(float t)
        {
            return start + dir * t * len;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection
        /// </summary>
        public (float, float)? IntersectWith(Line other)
        {
            float denom = (start.X - end.X)
                * (other.start.Y - other.end.Y)
                - (start.Y - end.Y)
                * (other.start.X - other.end.X);

            // First only numerators
            float tNum = (start.X - other.start.X)
                * (other.start.Y - other.end.Y)
                - (start.Y - other.start.Y)
                * (other.start.X - other.end.X);

            float uNum = (start.X - other.start.X)
                * (start.Y - end.Y)
                - (start.Y - other.start.Y)
                * (start.X - end.X);

            // TODO Check signs of numerators in order to divide only when
            // needed
            if (denom < -Single.Epsilon || Single.Epsilon < denom)
            {
                float t = tNum / denom;
                float u = uNum / denom;

                if (0f <= t && t <= 1f
                &&  0f <= u && u <= 1f)
                {
                    // Return the intersection positions along the Lines
                    // t for this and u for other
                    return (t, u);
                }
            }

            return null;
        }

        /// <summary>
        /// Plot a line onto a pixel-grid using Bresenham's algorithm:
        /// https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm#Method
        /// </summary>
        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            int x0, y0, x1, y1;
            {
                var start = this.start.ToPoint();
                var end = this.end.ToPoint();
                x0 = start.X; y0 = start.Y;
                x1 = end.X;   y1 = end.Y;
            }

            IEnumerable<(int, int)> xys;
            if (Math.Abs(y1 - y0) < Math.Abs(x1 - x0))
            {
                if (x0 > x1)
                {
                    xys = PlotLineLow(x1, y1, x0, y0);
                }
                else
                {
                    xys = PlotLineLow(x0, y0, x1, y1);
                }
            }
            else
            {
                if (y0 > y1)
                {
                    xys = PlotLineHigh(x1, y1, x0, y0);
                }
                else
                {
                    xys = PlotLineHigh(x0, y0, x1, y1);
                }
            }
            return Enumerable.Select(
                xys,
                (t) => { var (x, y) = t; return new Vertex(x, y, Color); }
            );
        }

        /// <summary>
        /// Helper for Bresenham's algorithm.
        /// </summary>
        IEnumerable<(int, int)> PlotLineLow(int x0, int y0, int x1, int y1)
        {
            int xd = x1 - x0;
            int yd = y1 - y0;
            int yi = 1;
            if (yd < 0)
            {
                yi = -1;
                yd = -yd;
            }
            int d = (2 * yd) - xd;
            int y = y0;

            for (int x = x0; x <= x1; x++)
            {
                yield return (x, y);

                if (d > 0)
                {
                    y += yi;
                    d += 2 * (yd - xd);
                }
                else
                {
                    d += 2 * yd;
                }
            }
        }

        /// <summary>
        /// Helper for Bresenham's algorithm.
        /// </summary>
        IEnumerable<(int, int)> PlotLineHigh(int x0, int y0, int x1, int y1)
        {
            int xd = x1 - x0;
            int yd = y1 - y0;
            int xi = 1;
            if (xd < 0)
            {
                xi = -1;
                xd = -xd;
            }
            int d = (2 * xd) - yd;
            int x = x0;

            for (int y = y0; y <= y1; y++)
            {
                yield return (x, y);

                if (d > 0)
                {
                    x += xi;
                    d += 2 * (xd - yd);
                }
                else
                {
                    d += 2 * xd;
                }
            }
        }
    }
}
