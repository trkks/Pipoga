using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Pipoga;

/// <summary>
/// Smaller utilities that do not deserve their own file.
/// </summary>
namespace Pipoga.Utilities
{
    public class TextureSelector
    {
        public readonly int texture;
        public int segment;
        private TextureSelector() { }
        public TextureSelector(int t)
        {
            texture = t;
        }

        public TextureSelector(int t, int s)
        {
            texture = t;
            segment = s;
        }
        public override string ToString()
        {
            return $"Texture index: {texture} Segment: {segment}";
        }
    }

    public class Route
    {
        public Line line;
        public float accum;
        public float step;
        private Route() { }
        public Route(Vector2 start, Vector2 end)
        {
            line = new Line(start, end);
            step = 0.01f / line.len;
            accum = 0;
        }
    }

    public class Curve
    {
        Vector2[] points;

        public Curve(Vector2[] ps)
        {
            points = new Vector2[4] { ps[0], ps[1], ps[2], ps[3] };
        }

        public Vector2 TangentAt(float t)
        {
            // Cubic Bézier curve:
            // https://en.wikipedia.org/wiki/Bézier_curve
            float t2 = t * t;
            return 3f * (1f - 2f * t + t2) * (points[1] - points[0])
                + 6f * (t - t2) * (points[2] - points[1])
                + 3f * t2 * (points[3] - points[2]);
        }
    }
}
