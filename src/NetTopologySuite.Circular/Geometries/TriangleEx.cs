using NetTopologySuite.Geometries;

namespace NetTopologySuite.Geometries
{
    /// <summary>
    /// Extension methods for <see cref="Triangle"/>s.
    /// </summary>
    public static class TriangleEx
    {
        /// <summary>
        /// Predicate function to test if a <paramref name="pt"/> is inside <<paramref name="self"/>.
        /// </summary>
        /// <param name="self">The triangle</param>
        /// <param name="pt">The point to test</param>
        /// <returns>A value indicating if <c>pt</c> is inside this triangle. </returns>
        public static bool Contains(this Triangle self, Coordinate pt)
        {
            bool hasNegative, hasPositive;

            double d1 = Sign(pt, self.P0, self.P1);
            double d2 = Sign(pt, self.P1, self.P2);
            double d3 = Sign(pt, self.P2, self.P0);

            hasNegative = (d1 < 0) || (d2 < 0) || (d3 < 0);
            hasPositive = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNegative && hasPositive);
        }

        private static double Sign(Coordinate p0, Coordinate p1, Coordinate p2)
        {
            return (p0.X - p2.X) * (p1.Y - p2.Y) - (p1.X - p2.X) * (p0.Y - p2.Y);
        }
    }
}
