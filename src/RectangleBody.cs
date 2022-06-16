using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pipoga
{
    public class RectangleBody
    {
        public Vector2 size;
        public Vector2 position;

        public float X => position.X;
        public float Y => position.Y;
        public float W => size.X;
        public float H => size.Y;
        public Vector2 TopLeft     => position;
        public Vector2 TopRight    => position + new Vector2(W - 1, 0);
        public Vector2 BottomRight => position + size - Vector2.One;
        public Vector2 BottomLeft  => position + new Vector2(0, H - 1);

        public RectangleBody(Vector2 position, Vector2 size)
        {
            this.size = size;
            this.position = position;
        }

        public RectangleBody(float x, float y, float width, float height)
        {
            this.size = new Vector2(width, height);
            this.position = new Vector2(x, y);
        }

        public RectangleBody(Rectangle rect)
        {
            size = rect.Size.ToVector2();
            position = rect.Location.ToVector2();
        }

        public bool Colliding(RectangleBody target)
        {
            return position.X + size.X >= target.position.X &&
                   position.Y + size.Y >= target.position.Y &&
                   position.X <= target.position.X + target.size.X &&
                   position.Y <= target.position.Y + target.size.Y;
        }

        public Vector2 Center()
        {
            return position + size * 0.5f;
        }

        public override string ToString()
        {
            return "size: " + size + " pos: " + position;
        }

        public Rectangle ToRectangle()
        {
            return new Rectangle(position.ToPoint(), size.ToPoint());
        }
    }
}
