using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Pipoga
{
    public static class Collisions
    {
        public struct Collision
        {
            public float depth;
            public Vector2 normal;
            public Collision(float depth, Vector2 normal)
            {
                this.depth = depth;
                this.normal = normal;
            }
        }

        /// <summary>
        /// Forms a Collision out of two AABBs if they are colliding
        /// Algorithm from: https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-the-basics-and-impulse-resolution--gamedev-6331
        /// </summary>
        /// <param name="colliderPos">The position of the object that will
        /// react to the collision </param>
        /// <param name="colliderSize">The size of it</param>
        /// <param name="collideePos">The position of the object that the
        /// resulting collision will be based on</param>
        /// <param name="collideeSize">The size of it</param>
        /// <returns>null if there was no collision</returns>
        public static Collision? AABBtoAABB(
            Vector2 colliderPos, Vector2 colliderSize,
            Vector2 collideePos, Vector2 collideeSize)
        {
            Vector2 collideeHalf = 0.5f * collideeSize;
            Vector2 colliderHalf = 0.5f * colliderSize;
            Vector2 separat =
                (colliderPos + colliderHalf) - (collideePos + collideeHalf);

            float xOverlap =
                collideeHalf.X + colliderHalf.X - Math.Abs(separat.X);
            float yOverlap =
                collideeHalf.Y + colliderHalf.Y - Math.Abs(separat.Y);

            if (xOverlap > 0 && yOverlap > 0)
            {
                if (xOverlap < yOverlap)
                {
                    return new Collision(
                        xOverlap,
                        separat.X > 0 ? Vector2.UnitX : -Vector2.UnitX
                    );
                }
                else
                {
                    return new Collision(
                        yOverlap,
                        separat.Y > 0 ? Vector2.UnitY : -Vector2.UnitY
                    );
                }
            }
            else
            {
                return null;
            }
        }

        public static Collision? LineToAABB(
            Line collider,
            Vector2 collideePos, Vector2 collideeSize)
        {
            var rectangleNormals = new Vector2[4] {
                -Vector2.UnitY,
                -Vector2.UnitX,
                Vector2.UnitX,
                Vector2.UnitY,
            };

            var rectLines = new Line[4] {
                // topleft->topright ( TOP )
                new Line(collideePos,
                         collideePos + Vector2.UnitX * collideeSize),
                // topleft->bottomleft ( LEFT )
                new Line(collideePos,
                         collideePos + Vector2.UnitY * collideeSize),
                // topright->bottomright ( RIGHT )
                new Line(collideePos + collideeSize * Vector2.UnitX,
                         collideePos + collideeSize),
                // bottomleft->bottomright ( BOTTOM )
                new Line(collideePos + collideeSize * Vector2.UnitY,
                         collideePos + collideeSize),
            };

            // Find the closest collision point
            float t = Single.MaxValue;
            int k = -1;
            for (int i = 0; i < rectLines.Length; i++)
            {
                (float tt, _) = collider.IntersectWith(rectLines[i])
                    ?? (Single.MaxValue, 0);
                if(tt < t)
                {
                    t = tt;
                    k = i;
                }
            }

            if (k >= 0)
            {
                return new Collision(t, rectangleNormals[k]);
            }

            return null;
        }
    }
}
