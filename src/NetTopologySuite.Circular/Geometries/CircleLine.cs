using System;
using System.Text;
using NetTopologySuite.Mathematics;

namespace NetTopologySuite.Geometries
{
    class CircleLineSegmentIntersectionUtilityDD
    {


        private static readonly DD Zero = new DD(0);
        private static readonly DD Two = new DD(2);
        private static readonly DD minusOne = new DD(-1);
        private static readonly DD Four = new DD(4);

        private double maxDelta = 1 / 1E8;

        private readonly Coordinate center;
  private readonly double radius;
        private readonly LineSegment ls;
  private readonly DD dx, dy, A;
  private DD B, C, det;

        private Coordinate[] intPt;

        public CircleLineSegmentIntersectionUtilityDD(Coordinate center, double radius, LineSegment ls)
        {

            this.center = center;
            this.radius = radius;
            this.ls = ls;

            this.dx = new DD(ls.P1.X) - new DD(ls.P0.X);
            this.dy = new DD(ls.P1.Y) - new DD(ls.P0.Y);

            this.A = dx * dx + dy * dy;
        }

        public int getNumIntersections()
        {

            // Line segment denotes to a point.
            if (A.LessThan(new DD(maxDelta)))
            {
                // Is point on circle?
                if (center.distance(ls.p0) - radius < maxDelta)
                {
                    return 1;
                }
                return 0;
            }

            if (det == null)
                compute();

            if (this.det.lt(Zero))
            {
                if (Math.abs(ls.distancePerpendicular(this.center) - radius) < maxDelta)
                    return 1;
                return 0;
            }

            if (det.equals(Zero))
                return 1;

            return 2;
        }

        public Coordinate getIntersecion(int index)
        {

            if (intPt != null)
                return intPt[index];

            // intialize array
            intPt = new Coordinate[2];

            // Line segment denotes to a point.
            if (A.lt(new DD(maxDelta)))
            {
                // Is point on circle?
                if (center.distance(ls.p0) - radius < maxDelta)
                {
                    return intPt[0] = ls.p0.copy();
                }
                return null;
            }

            if (this.det == null)
                compute();

            if (this.det.lt(Zero))
            {
                if (Math.abs(ls.distancePerpendicular(this.center) - radius) < maxDelta)
                    return intPt[0] = ls.project(center);
                return null;
            }

            if (det.equals(Zero))
            {
                DD t = minusOne.selfMultiply(B).divide(Two.multiply(A));
                intPt[0] = new CoordinateXY(ls.p0.x + t.multiply(dx).doubleValue(),
                        ls.p0.y + t.multiply(dy).doubleValue());
                return index == 0 ? intPt[0] : null;
            }

            DD t = new DD(minusOne.multiply(B).add(det.sqrt())).divide(Two.multiply(A));
            intPt[0] = new CoordinateXY(ls.p0.x + t.multiply(dx).doubleValue(),
                    ls.p0.y + t.multiply(dy).doubleValue());
            t = new DD(minusOne.multiply(B).subtract(det.sqrt())).divide(Two.multiply(A));
            intPt[1] = new CoordinateXY(ls.p0.x + t.multiply(dx).doubleValue(),
                    ls.p0.y + t.multiply(dy).doubleValue());

            if (0 <= index && index <= 1)
                return intPt[index];

            return null;
        }

        private void compute()
        {
            DD cx1 = new DD(ls.p0.x).selfSubtract(center.x);
            DD cy1 = new DD(ls.p0.y).selfSubtract(center.y);

            this.B = Two.multiply(dx.multiply(cx1).add(dy.multiply(cy1)));
            this.C = cx1.sqr().selfAdd(cy1.sqr()).selfSubtract(new DD(radius).sqr());

            this.det = B.sqr().selfSubtract(Four.multiply(A).selfMultiply(C));
        }

        public void print()
        {
            DD t;
            int numInt = getNumIntersections();
            if (numInt > 0) getIntersecion(0);

            StringBuilder sb = new StringBuilder("A:=" + A + ";B:=" + B + ";C:=" + C);
            sb.append("\ndet:=" + this.det);
            if (numInt == 0)
            {
                System.out.println(sb.toString());
                return;
            }
            if (numInt == 1)
            {
                t = minusOne.multiply(this.B).selfDivide(Two.multiply(this.A));
                sb.append("\nt:=" + t);
                sb.append("\n" + getIntersecion(0));
                System.out.println(sb.toString());
                return;
            }
            t = new DD(minusOne.multiply(this.B).add(this.det.sqrt())).divide(Two.multiply(this.A));
            sb.append("\nt:=" + t);
            sb.append("\n" + getIntersecion(0));
            t = new DD(minusOne.multiply(this.B).subtract(this.det.sqrt())).divide(Two.multiply(this.A));
            sb.append("\nt:=" + t);
            sb.append("\n" + getIntersecion(1));

            System.out.println(sb.toString());
            return;
        }
        /*
        public String toString() {
          DD t;
          int numInt = getNumIntersections();
          StringBuilder sb = new StringBuilder("A:=" + A + ";B:=" + B + ";C:=" + C);
          sb.append("\ndet:=" + det);
          if (numInt == 0) return sb.toString();
          if (numInt == 1) {
            t = minusOne.multiply(B).selfDivide(Two.multiply(A));
            sb.append("\nt:=" + t);
            sb.append("\n" + getIntersecion(0));
            return sb.toString();
          }
          t = new DD(minusOne.multiply(B).add(det.sqrt())).divide(Two.multiply(A));
          sb.append("\nt:=" + t);
          sb.append("\n" + getIntersecion(0));
          t = new DD(minusOne.multiply(B).subtract(det.sqrt())).divide(Two.multiply(A));
          sb.append("\nt:=" + t);
          sb.append("\n" + getIntersecion(1));
          return sb.toString();
        }
        */
    }
}
