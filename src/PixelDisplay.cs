using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Pipoga
{
    /// <summary>
    /// A grid of pixels to set with colors. Could be used in some emulation?
    /// </summary>
    class PixelDisplay
    {
        static Color DEFAULT_COLOR = Color.Black;

        // This has to be set before first running `Draw` method.
        public Texture2D PixelTexture { get; set; }
        public Point PixelSize { get; private set; }

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
            set { buffer[y * GridSize.X + x] = value ?? DEFAULT_COLOR; }
        }

        Color[] buffer;

        public PixelDisplay(Point pixelSize, Point gridSize)
        {
            PixelSize = pixelSize;
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

        public Point ToGridCoords(Point pos)
        {
            return pos / PixelSize;
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

        /// <summary>
        /// Plot a line onto the pixel-grid using Bresenham's algorithm:
        /// https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm#Method
        /// </summary>
        /// <param name="line">The line to rasterize.</param>
        public void PlotLine(Line line, Color color)
        {
            int x0, y0, x1, y1;
            {
                var start = line.start.ToPoint();
                var end = line.end.ToPoint();
                x0 = start.X; y0 = start.Y;
                x1 = end.X;   y1 = end.Y;
            }

            if (Math.Abs(y1 - y0) < Math.Abs(x1 - x0))
            {
                if (x0 > x1)
                {
                    PlotLineLow(x1, y1, x0, y0, color);
                }
                else
                {
                    PlotLineLow(x0, y0, x1, y1, color);
                }
            }
            else
            {
                if (y0 > y1)
                {
                    PlotLineHigh(x1, y1, x0, y0, color);
                }
                else
                {
                    PlotLineHigh(x0, y0, x1, y1, color);
                }
            }
        }

        /// <summary>
        /// Plot a rectangle onto the pixel-grid.
        /// </summary>
        public void PlotRect(Rectangle rect, Color color)
        {
            for (int i = 0; i < rect.Height; i++)
            {
                for (int j = 0; j < rect.Width; j++)
                {
                    var (x, y) = (rect.X + j, rect.Y + i);
                    if (Contains(x, y))
                    {
                        this[x, y] = color;
                    }
                }
            }
        }

        /// <summary>
        /// Helper for Bresenham's algorithm.
        /// </summary>
        void PlotLineLow(int x0, int y0, int x1, int y1, Color color)
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

            for (int x = x0; x < x1; x++)
            {
                if (Contains(x, y))
                {
                    this[x, y] = color;
                }

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
        void PlotLineHigh(int x0, int y0, int x1, int y1, Color color)
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

            for (int y = y0; y < y1; y++)
            {
                if (Contains(x, y))
                {
                    this[x, y] = color;
                }

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
