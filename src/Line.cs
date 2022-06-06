using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Pipoga
{
    public class Line
    {
        public readonly Vector2 start;
        public readonly Vector2 end;
        public readonly Vector2 dir;
        public readonly Vector2 diff;
        public readonly float len;

        public Line(Vector2 start, Vector2 end)
        {
            this.start = start;
            this.end = end;
            diff = end - start;
            len = diff.Length();
            dir = Vector2.Normalize(diff);
        }

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
    }
}
