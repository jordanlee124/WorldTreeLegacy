using System;

namespace Worldtree.Domain.Camera
{
    /// <summary>Keeps an orthographic view rectangle inside level bounds.</summary>
    public static class ViewBounds
    {
        /// <summary>
        /// Clamps one axis of the view centre so [centre - half, centre + half] stays within [min, max].
        /// When the view is larger than the bounds on this axis it is centred on them instead.
        /// </summary>
        public static float ClampAxis(float centre, float half, float min, float max)
        {
            if (max < min) throw new ArgumentException("max must be >= min");
            if (half * 2f >= max - min) return (min + max) * 0.5f;
            return Math.Max(min + half, Math.Min(max - half, centre));
        }
    }
}
