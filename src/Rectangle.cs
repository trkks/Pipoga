using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pipoga
{
    public class Rectangle : IRasterizable, IArea
    {
        public Vector2 Size { get; set; }
        public Vector2 Position { get; set; }

        public float X => Position.X;
        public float Y => Position.Y;
        public float W => Size.X;
        public float H => Size.Y;

        // Top.
        public Vector2 TLeft  => Position;
        public Vector2 TRight => Position + Vector2.UnitX * W;
        // Bottom.
        public Vector2 BRight => Position + Size;
        public Vector2 BLeft  => Position + Vector2.UnitY * H;

        public Rectangle(Vector2 position, Vector2 size)
        {
            Size = size;
            Position = position;
        }

        public Rectangle(float x, float y, float width, float height)
        {
            Size = new Vector2(width, height);
            Position = new Vector2(x, y);
        }

        public Rectangle(Microsoft.Xna.Framework.Rectangle rect)
        {
            Size = rect.Size.ToVector2();
            Position = rect.Location.ToVector2();
        }

        public bool Colliding(Rectangle target)
        {
            return Position.X + Size.X >= target.Position.X &&
                   Position.Y + Size.Y >= target.Position.Y &&
                   Position.X <= target.Position.X + target.Size.X &&
                   Position.Y <= target.Position.Y + target.Size.Y;
        }

        public Vector2 Center()
        {
            return Position + Size * 0.5f;
        }

        public override string ToString()
        {
            return "Size: " + Size + " pos: " + Position;
        }

        public Microsoft.Xna.Framework.Rectangle ToXnaRectangle()
        {
            return new Microsoft.Xna.Framework.Rectangle(
                Position.ToPoint(), Size.ToPoint()
            );
        }

        public bool Contains(Vector2 p)
        {
            return X <= p.X && Y <= p.Y && p.X <= X + W && p.Y <= Y + H;
        }

        public IEnumerable<Vertex> GetVertices(Vector2 inversePixelSize)
        {
            var pos = (Position * inversePixelSize).ToPoint();
            var size = (Size * inversePixelSize).ToPoint();

            for (int i = 0; i < size.Y; i++)
            {
                int y = pos.Y + i;
                for (int j = 0; j < size.X; j++)
                {
                    int x = pos.X + j;
                    yield return new Vertex(x, y, Color.White);
                }
            }
        }
    }
}
