using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Pipoga
{
    using XnaRect = Microsoft.Xna.Framework.Rectangle;

    /// <summary>
    /// A grid of pixels to set with colors. Could be used in some emulation?
    /// </summary>
    public class PixelDisplay
    {
        static Color DEFAULT_COLOR = Color.Black;

        // This has to be set before first running `Draw` method.
        public Texture2D PixelTexture { get; set; }
        public Point PixelSize { get; private set; }
        public Vector2 InversePixelSize { get; private set; }

        /// <summary>
        /// The most fitting screen size for given pixel and grid dimensions.
        /// </summary>
        public Point ScreenSize
        {
            get => new Point(
                PixelSize.X * GridSize.X,
                PixelSize.Y * GridSize.Y
            );
        }

        /// <summary>
        /// The size for the grid of pixels.
        /// </summary>
        public Point GridSize { get; private set; }

        // TODO Does this order of indexing cause cache-thrashing?
        public Color? this[int x, int y]
        {
            get { return buffer[y * GridSize.X + x]; }
            set
            {
                var c = value ?? DEFAULT_COLOR;
                // Only set the color if not totally transparent.
                if (c.A > 0)
                {
                    buffer[y * GridSize.X + x] = c;
                }
            }
        }

        public Color? this[Point p]
        {
            get { return buffer[p.Y * GridSize.X + p.X]; }
            set { buffer[p.Y * GridSize.X + p.X] = value ?? DEFAULT_COLOR; }
        }

        Color[] buffer;

        public PixelDisplay(Point pixelSize, Point gridSize)
        {
            PixelSize = pixelSize;
            // Compute this here once to reduce divisions.
            InversePixelSize =
                new Vector2(1f / (float)pixelSize.X, 1f / (float)pixelSize.Y);
            GridSize = gridSize;
            this.buffer = new Color[GridSize.X * GridSize.Y];
        }

        /// <summary>
        /// Render the pixels.
        /// Throws exception if PixelTexture is not set beforehand.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                Color color = buffer[i];
                var location = new Vector2(
                    i % GridSize.X * PixelSize.X,
                    i / GridSize.X * PixelSize.Y
                );

                // TODO "Stretch" the pixel along the axises according to size

                spriteBatch.Draw(
                    PixelTexture,
                    location,
                    null,
                    color,
                    0,
                    Vector2.Zero,
                    PixelSize.ToVector2(),
                    SpriteEffects.None,
                    0
                );
            }
        }

        public bool Contains(Point point)
        {
            return Contains(point.X, point.Y);
        }

        public bool Contains(int x, int y)
        {
            return 0 <= x && x < GridSize.X
                && 0 <= y && y < GridSize.Y;
        }

        public void Plot<T>(T obj) where T : IRasterizable
        {
            foreach (var vertex in obj.GetVertices(InversePixelSize))
            {
                if (Contains(vertex.X, vertex.Y))
                {
                    this[vertex.X, vertex.Y] = vertex.Color;
                }
            }
        }

        /// <summary>
        /// Center the position on platform-screen to the pixel on virtual
        /// screen.
        /// </summary>
        /// <param name="pos">The position on platform-screen.</param>
        /// <returns>The matching position on virtual screen.</returns>
        public Point ToScreenPos(Point pos)
        {
            return (pos.ToVector2() * InversePixelSize).ToPoint();
        }

        public XnaRect ToScreenCoords(Rectangle rect)
        {
            return new XnaRect(
                ToScreenPos(rect.Position.ToPoint()),
                ToScreenPos(rect.Size.ToPoint())
            );
        }

        public void Clear(Color? color=null)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                for (int x = 0; x < GridSize.X; x++)
                {
                    this[x, y] = color;
                }
            }
        }
    }
}
