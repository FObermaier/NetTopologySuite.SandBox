using System;
using System.Globalization;

namespace NetTopologySuite.Geometries.Implementation
{
    /// <summary>
    /// Simple point structure for 2D points.
    /// Ordinate values are stored in single precision.
    /// </summary>
    [Serializable]
    public readonly struct PointF
    {
        /// <summary>
        /// Creates a new PointF
        /// </summary>
        /// <param name="x">The x-ordinate</param>
        /// <param name="y">The y-ordinate</param>
        public PointF(double x, float y) : this((float)x, y)
        { }

        /// <summary>
        /// Creates a new PointF
        /// </summary>
        /// <param name="x">The x-ordinate</param>
        /// <param name="y">The y-ordinate</param>
        public PointF(float x, double y) : this(x, (float)y)
        { }

        /// <summary>
        /// Creates a new PointF
        /// </summary>
        /// <param name="x">The x-ordinate</param>
        /// <param name="y">The y-ordinate</param>
        public PointF(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets a value indicating the x-ordinate value
        /// </summary>
        public float X { get; }

        /// <summary>
        /// Gets a value indicating the y-ordinate value
        /// </summary>
        public float Y { get; }

        public override int GetHashCode()
        {
            return 17 ^ X.GetHashCode() ^ Y.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PointF other))
                return false;

            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override string ToString()
        {
            return string.Format(NumberFormatInfo.InvariantInfo, "PointF({0}, {1})", X, Y);
        }
    }
}
