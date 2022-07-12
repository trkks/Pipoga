using Microsoft.Xna.Framework;

namespace Pipoga
{
    /// <summary>
    /// An interface for world-shapes that can be interacted with through a
    /// screen.
    /// </summary>
    public interface IArea
    {
        /// <summary>
        /// Check if the point is contained inside this shape's bounds.
        /// </summary>
        /// <param name="p">The point to check for.</param>
        /// <returns>True, if point is contained in the shape's area.</returns>
        public bool Contains(Vector2 p);
        // TODO Somehow figure out a generic (-enough) way to implement SAT...
    }
}
