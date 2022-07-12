using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Pipoga
{
    using XnaRect = Microsoft.Xna.Framework.Rectangle;

    public class Animations
    {
        // Maps an integer to the frame of an animation (a rectangle to a
        // texture) and another to this frame's duration (number of repeats).
        public struct FrameDuration
        {
            public int frame;
            public int duration;
            public FrameDuration(int f, int d) { frame = f; duration = d; }
        }

        public class Animation
        {
            public int handle;
            public int index;
            public int elapsedFrames;
            // Disable default constructor
            private Animation() { }

            public Animation(int h)
            {
                handle = h;
            }

            public void Reset()
            {
                elapsedFrames = 0;
                index = 0;
            }
        }

        /// <summary>
        /// Slice the animation atlas frames into rectangles.
        /// </summary>
        /// <param name="spriteSize">Size of a single frame</param>
        /// <param name="atlasSize">Size of the source atlas</param>
        /// <returns>
        /// The animation frames located on source atlas ordered from left to
        /// right, top to bottom.
        /// </returns>
        public static XnaRect[] FramesFrom(Point spriteSize, Point atlasSize)
        {
            var atlasMask = new XnaRect(Point.Zero, spriteSize);
            int frameCount = atlasSize.X / spriteSize.X
                             * atlasSize.Y / spriteSize.Y;
            var result = new XnaRect[frameCount];

            // Add first frame
            result[0] = new XnaRect(atlasMask.Location, atlasMask.Size);

            // Sample through the atlas by sprite size:
            for (int i = 1; i < frameCount; i++)
            {
                // Decide from where on atlas to pick the rectangle
                int newFrameX = atlasMask.X + atlasMask.Width;

                if (newFrameX + spriteSize.X > atlasSize.X)
                {
                    // Reset column and move by one row
                    int newFrameY = atlasMask.Y + atlasMask.Height;
                    if (newFrameY + spriteSize.Y > atlasSize.Y)
                    {
                        System.Console.WriteLine(
                            "Sampling frame from outside of atlas's Y-axis: "+
                            $"{newFrameY + spriteSize.Y} > {atlasSize.Y}"
                        );
                        break;
                    }
                    else
                    {
                        atlasMask.Y = newFrameY;
                    }
                    atlasMask.X = 0;
                }
                else
                {
                    // Move by one column
                    atlasMask.X = newFrameX;
                }

                // Add the frame
                result[i] = new XnaRect(atlasMask.Location, atlasMask.Size);
            }
            return result;
        }
    }
}
