using System;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using NetTopologySuite.DataStructures;
using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;

namespace NetTopologySuite.Geometries
{
    /**
     * This class adds curvature to a plain {@link LineSegment}.
     * <i>
     * Bulges are something that women have (mostly to please the opposite sex it seems)
     * and something that guys try to get by placing socks in strategic places. At least
     * until they get older. Which is the time they tend to develop bulges in not so
     * strategic places. In other words: bulges are all about curvature.
     * </i>
     *
     * The bulge is the tangent of 1/4 of the included angle for the arc between the
     * selected vertex and the next vertex in the polyline's vertex list. A negative
     * bulge value indicates that the arc goes clockwise from the selected vertex to the
     * next vertex. A bulge of 0 indicates a straight segment, and a bulge of 1 is a
     * semicircle.
     *
     * {@see https://www.afralisp.net/archive/lisp/Bulges1.htm}
     * {@see https://www.afralisp.net/archive/lisp/Bulges2.htm}
     *
     *
     */
    public class BulgeSegment : IComparable<BulgeSegment>
    {
        /// <summary>
        /// A constant with the value of <see cref="Math.PI"/> * 2.0d
        /// </summary>
        private const double PIx2 = Math.PI * 2;

        /// <summary>
        /// A constant defining the default scale value for the precision model
        /// </summary>
        private const double DEFAULT_SCALE = 1e8;

        /// <summary>
        /// The precision model to use when computing new coordinates
        /// </summary>
        private static PrecisionModel _precisionModel;

        /// <summary>
        /// Gets or sets the precision model to use when working with <see cref="BulgeSegment"/>s
        /// </summary>
        public static PrecisionModel PrecisionModel
        {
            get => _precisionModel ?? (_precisionModel = new PrecisionModel(DEFAULT_SCALE));
            set => _precisionModel = value;
        }


        private Coordinate p, p3;
        private Coordinate p1, p2;
        private double bulge;

        /**
     * Creates an instance of this class
     *
     * @param p1    the starting point of the segment
     * @param p2    the end-point of the segment
     * @param bulge a value describing the bulge
     */
        public BulgeSegment(Coordinate p1, Coordinate p2, double bulge)
        {
            CheckBulge(bulge);
            this.p1 = p1;
            this.p2 = p2;
        }

        /**
     * Creates an instance of this class
     *
     * @param ls    A line segment describing starting- and end-point
     * @param bulge a value describing the bulge
     */
        public BulgeSegment(LineSegment ls, double bulge)
            : this(ls.P0, ls.P1, bulge)
        {
        }

        /**
     * Creates an instance of this class
     *
     * @param x1    the x-ordinate of the starting point of the segment
     * @param y1    the y-ordinate of the starting point of the segment
     * @param x2    the x-ordinate of the end-point of the segment
     * @param y2    the y-ordinate of the end-point of the segment
     * @param bulge a value describing the bulge
     */
        public BulgeSegment(double x1, double y1, double x2, double y2, double bulge)
            : this(new Coordinate(x1, y1), new Coordinate(x2, y2), bulge)
        {
        }

        /**
     * Creates an instance of this class based on three points defining an arc
     * @param p1 the starting point of the arc
     * @param p2 an (arbitrary) point on the arc
     * @param p3 the end point of the arc
     */
        public BulgeSegment(Coordinate p1, Coordinate p2, Coordinate p3)
        {

            this.p1 = p1;
            this.p2 = p3;

            if (new LineSegment(p1, p3).DistancePerpendicular(p2) > 1e-10)
            {
                p = CircleCenter(p1, p2, p3);

                double angle = AngleUtility.InteriorAngle(p1, p, p3);
                if (Orientation.Index(p, p1, p3) != OrientationIndex.Clockwise)
                {
                    angle -= Math.PI;
                    angle *= -1;
                }

                /*
                double phiS = Angle.angle(this.p, p1);
                double phiA = Angle.angle(this.p, p2) - phiS;
                double phiE = Angle.angle(this.p, p3) - phiS;
                if (Math.abs(phiE) > Math.PI ||
                    (phiA < 0 && phiE > 0) || (phiA > 0 && phiE < 0))
                  throw new IllegalArgumentException("Input points define an arc longer than a semi-circle");
                double bulge = Math.tan(-phiE/4d);
                */
                double bulge = Math.Tan(angle / 4d);
                CheckBulge(bulge);
                this.bulge = bulge;
            }
            else
                this.bulge = 0d;
        }


        /**
     * Gets a coordinate defining the shape of the bulge
     *
     * @param index the index of the coordinate.
     *              Valid values and their meaning are:
     *              <ul>
     *                <li>0 ... {@linkplain BulgeSegment#getCentre()}</li>
     *                <li>1 ... {@linkplain BulgeSegment#getP1()}</li>
     *                <li>2 ... {@linkplain BulgeSegment#getP2()}</li>
     *                <li>3 ... {@linkplain BulgeSegment#getP3()}</li>
     *              </ul>
     * @return A coordinate
     */
        public Coordinate this[int index]
        {

            get
            {
                if (index == 0) return Centre;
                if (index == 1) return p1;
                if (index == 2) return p2;
                if (index == 3) return P3;
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /**
     * Gets the centre coordinate of the circle this {@link BulgeSegment} lies on
     * @return the centre coordinate
     */
        public Coordinate Centre
        {
            get
            {
                if (p != null)
                {
                    var res = p.Copy();
                    _precisionModel.MakePrecise(res);
                    return res;
                }

                if (bulge == 0d)
                    return null;

                double c = p1.Distance(p2);
                double rwos = (Sagitta() - Radius()) * Math.Sign(bulge);
                double dx = -rwos * (p2.Y - p1.Y) / c;
                double dy = rwos * (p2.X - p1.X) / c;

                p = new Coordinate(
                    0.5d * (p1.X + p2.X) + dx,
                    0.5d * (p1.Y + p2.Y) + dy);

                return Centre;

            }

        }

        /**
     * Gets the centre coordinate of the circle this {@link BulgeSegment} lies on
     * @return the centre coordinate
     */
        public Coordinate P
        {
            get => Centre;
        }

        /**
     * Gets the coordinate of the starting point of this {@link BulgeSegment}.
     * @return the starting point coordinate
     */
        public Coordinate P1
        {
            get => this.p1.Copy();
        }

        /**
     * Gets the coordinate of the end-point of this {@link BulgeSegment}.
     * @return the end-point coordinate
     */
        public Coordinate P2
        {
            get => p2.Copy();
        }

        /**
     * Gets the coordinate of the mid-point of this {@link BulgeSegment}.
     * @return the centre coordinate
     */
        public Coordinate P3
        {
            get
            {
                if (p3 != null)
                {
                    var res = p3.Copy();
                    _precisionModel.MakePrecise(res);
                    return p3;
                }

                if (bulge == 0d)
                {
                    p3 = new Coordinate(0.5 * (p1.X + p2.X), 0.5 * (p1.Y + p2.Y));
                    return P3;
                }

                double c = p1.Distance(p2);
                double s = Sagitta() * Math.Sign(bulge);
                double dx = -s * (p2.Y - p1.Y) / c;
                double dy = s * (p2.X - p1.X) / c;

                p3 = new Coordinate(
                    0.5d * (p1.X + p2.X) + dx,
                    0.5d * (p1.Y + p2.Y) + dy);

                return P3;
            }
        }

        /**
     * Gets a value describing the curvature of a segment from {@link #p1} to {@link #p2}.
     * <p>
     * The bulge is the tangent of 1/4 of the included angle for the arc between {@link #p1}
     * and {@link #p2}.A negative bulge value indicates that the arc goes clockwise from
     * {@link #p1} to {@link #p2}. A bulge of 0 indicates a straight segment, and a bulge of
     * 1 is a semicircle.
     * </p>
     * <p>
     *   A positive bulge value indicates that
     *   <ul>
     *     <li>{@linkplain #getCentre()} will return a point on the right of the segment</li>
     *     <li>the bulge is on the left of the segment</li>
     *   </ul>
     *   For a negative bulge value it is the other way around.
     * </p>
     * @return the bulge
     */
        public double Bulge
        {
            get => this.bulge;
        }

        /**
     * Computes the length of the bulge
     *
     * @return
     */
        public double Length
        {
            get => this.bulge != 0d ? Math.Abs(Phi() * Radius()) : p1.Distance(p2);
        }

        /**
     * Computes the area of the bulge
     *
     * @return the area
     */
        public double Area()
        {
            if (this.bulge == 0)
                return 0d;

            double r = Radius();
            return Math.Abs(Phi()) * 0.5 * r * r - Triangle.Area(Centre, p1, p2);
        }

        /**
     * Computes a value that describes the height of the bulge
     *
     * @return the height of the bulge
     */
        double Sagitta()
        {
            return 0.5 * p1.Distance(p2) * Math.Abs(this.bulge);
        }

        /**
     * Computes a value that describes the min. distance of the chord from
     * {@linkplain #getP1()} to {@linkplain #getP2()} from {@linkplain #getCentre()}
     *
     * @return the apothem
     */
        double Apothem()
        {
            return Radius() - Sagitta();
        }


        /**
     * Gets the chord as a {@linkplain LineSegment}
     *
     * @return a chord from {@linkplain #getP1()} to {@linkplain #getP2()}
     */
        public LineSegment Chord {

        
            get => new LineSegment(this.p1, this.p2);
        
        }

    /**
     * Computes the radius of the circle this {@link BulgeSegment} lies on.
     *
     * @return a radius
     */
    double Radius()
    {
        if (this.bulge == 0d)
            return double.PositiveInfinity;

        double halfC = 0.5 * p1.Distance(p2);
        double s = Sagitta();
        return 0.5 * (halfC * halfC + s * s) / s;
    }

    /**
     * Method to reverse the bulge segment definition
     */
    public void Reverse()
    {
        Coordinate tmp = this.p1;
        this.p1 = p2;
        this.p2 = tmp;
        this.bulge *= -1;
    }

    /**
     * Creates a copy of this bulge segment
     * @return a bulge segment
     */
    public BulgeSegment Reversed()
    {
        return new BulgeSegment(this.p2.Copy(), this.p1.Copy(), -this.bulge) { p = p?.Copy() };
    }

    /**
     * Computes the envelope of this bulge segment
     * @return the envelope
     */
    public Envelope Envelope
    {
        get
        {
            var res = new Envelope(p1, p2);
            if (bulge == 0d) return res;

            var p = Centre;
            int q1 = GetQuadrant(p, this.bulge > 0 ? p2 : p1);
            int q2 = GetQuadrant(p, this.bulge > 0 ? p1 : p2);
            if (q1 == q2) return res;

            if (q2 < q1) q2 += 4;

            double r = _precisionModel.MakePrecise(Radius());
            switch (q1)
            {
                case 0:
                    if (q2 > 0)
                    {
                        res.ExpandToInclude(p.X, p.Y + r);
                        if (q2 > 1) res.ExpandToInclude(p.X - r, p.Y);
                    }

                    break;
                case 1:
                    if (q2 > 1)
                    {
                        res.ExpandToInclude(p.X - r, p.Y);
                        if (q2 > 2) res.ExpandToInclude(p.X, p.Y - r);
                    }

                    break;
                case 2:
                    if (q2 > 2)
                    {
                        res.ExpandToInclude(p.X, p.Y - r);
                        if (q2 > 3) res.ExpandToInclude(p.X + r, p.Y);
                    }

                    break;
                case 3:
                    if (q2 > 3)
                    {
                        res.ExpandToInclude(p.X + r, p.Y);
                        if (q2 > 4) res.ExpandToInclude(p.X, p.Y + r);
                    }

                    break;
            }

            return res;
        }
    }

    /**
     * Computes the opening angle between {@linkplain #getCentre()},  {@linkplain #p1} and  {@linkplain #p2}
     *
     * @return an angle in radians
     */
    double Phi()
    {
        return 4 * Math.Atan(this.bulge);
    }

    /**
         * Predicate to compute i
         * @param c
         * @return
         */
    public bool IntersectsArea(Coordinate c)
    {

        if (this.bulge == 0d)
            return false;

        var phiInt = AngleInterval;
        double phiC = AngleUtility.Angle(p, c);
        if (phiC < 0) phiC += PIx2;

        if (phiInt.contains(phiC))
        {
            if (p.Distance(c) <= Radius())
                return !new Triangle(p, p1, p2).Contains(c);
        }

        return false;
    }

    /**
             * Predicate to compute i
             * @param c
             * @return
             */
            public bool Intersects(Coordinate c)
    {

        double phiS, phiE;
        Coordinate centre = Centre;

        if (bulge > 0)
        {
            phiS = AngleUtility.Angle(centre, this.p2);
            phiE = AngleUtility.Angle(centre, this.p1);
        }
        else
        {
            phiS = AngleUtility.Angle(centre, this.p1);
            phiE = AngleUtility.Angle(centre, this.p2);
        }
        if (phiS < 0) phiS += PIx2;
        if (phiE < 0) phiE += PIx2;

        double phiC = AngleUtility.Angle(centre, c);
        if (phiC < 0) phiC += PIx2;

        if (phiS <= phiC && phiC <= phiE)
        {
            if (centre.Distance(c) <= Radius())
                return !new Triangle(centre, p1, p2).Contains(c);
        }
        return false;
    }

    public bool Intersects(BulgeSegment bs)
    {
        if (bs.Bulge == 0d)
            return Intersects(bs.Chord);

        if (Centre.Distance(bs.Centre) - (Radius() + bs.Radius()) > maxDelta)
            return false;

        var u = new CircleCircleIntersectionUtility(Centre, Radius(), bs.Centre, bs.Radius());

        int numIntersections = u.getNumIntersections();
        if (numIntersections == 0)
            return false;

        if (numIntersections < 2)
        {
            for (int i = 0; i < numIntersections; i++)
            {
                Coordinate c = u.getIntersection(i);
                if (Intersects(c) && bs.Intersects(c))
                    return true;
            }
            return false;
        }

        // circles coincide, check if there is an overlap in the angle intervals
        var thisAi = AngleInterval;
        var otherAi = bs.AngleInterval;
        return thisAi.Overlaps(otherAi);
    }

    private Interval AngleInterval
    {

        get
        {
            var centre = Centre;
            double thisPhiS = AngleUtility.Angle(centre, p1);
            if (thisPhiS < 0d) thisPhiS = thisPhiS += PIx2;
            double thisPhiE = thisPhiS - Phi();

            return Interval.Create(thisPhiS, thisPhiE);
        }
    }

    public IList<Coordinate> GetSequence(double distance, PrecisionModel pm)
    {

        if (pm == null)
            pm = _precisionModel;

        if (distance <= 0d)
            throw new ArgumentException("distance must be positive", nameof(distance));

        var res = new List<Coordinate>();
        res.Add(this.p1);
        if (Length > 0 && this.bulge != 0)
        {
            var tmp = Centre;
            var centre = p;
            double r = Radius();
            double phi = -Phi();
            double phiStep = phi / (Length / distance);
            int numSteps = (int)(phi / phiStep) - 1;

            phi = AngleUtility.Angle(centre, p1) + phiStep;
            while (numSteps-- > 0)
            {
                double x = r * Math.Cos(phi);
                double y = r * Math.Sin(phi);
                res.Add(new Coordinate(
                        pm.MakePrecise(centre.X + x),
                        pm.MakePrecise(centre.Y + y)));
                phi += phiStep;
            }
        }

        res.Add(p2);
        return res;
    }

    private static int GetQuadrant(Coordinate center, Coordinate other)
    {
        double vX = other.X - center.X;
        double vY = other.Y - center.Y;

        if (vX >= 0)
        {
            if (vY >= 0) return 0;
            return 3;
        }
        if (vY >= 0) return 1;
        return 2;
    }

    public int CompareTo(BulgeSegment other)
    {
        if (other == null) return 1;
        return Envelope.CompareTo(other.Envelope);
    }

    private static void CheckBulge(double bulge)
    {
        if (bulge < -1d || bulge > 1)
            throw new ArgumentException("bulge must be in the range [-1, 1], bulge=" + bulge, nameof(bulge));
    }

    private static Coordinate CircleCenter(Coordinate A, Coordinate B, Coordinate C)
    {

        // deltas
        double dy_a = B.Y - A.Y;
        double dx_a = B.X - A.X;
        double dy_b = C.Y - B.Y;
        double dx_b = C.X - B.X;

        // slopes
        double m_a = dy_a / dx_a;
        double m_b = dy_b / dx_b;

        var res = new Coordinate();
        res.X = (m_a * m_b * (A.Y - C.Y) + m_b * (A.X + B.X) - m_a * (B.X + C.X)) / (2 * (m_b - m_a));
        res.Y = -1 * (res.X - (A.X + B.X) / 2) / m_a + (A.Y + B.Y) / 2;
        _precisionModel.MakePrecise(res);

        return res;

    }

        private class CircleCircleIntersectionUtility
        {
            private readonly Coordinate center1; //, center2;
    private readonly DD radius1, radius2;
    private readonly DD dx, dy, dist;

    private Coordinate[] intPt = null;

            public CircleCircleIntersectionUtility(
                    Coordinate c1, double r1, Coordinate c2, double r2)
            {

                this.center1 = c1;
                this.radius1 = new DD(r1);
                //this.center2 = c2;
                this.radius2 = new DD(r2);

                this.dx = new DD(c2.X) - new DD(c1.X);
                this.dy = new DD(c2.X) - new DD(c1.Y);
                this.dist = (dx.Sqr() + dy.Sqr()).Sqrt();
            }

            public int getNumIntersections()
            {
                if (dist.c > radius1 + radius2)
                    // no intersections, circles are too far apart
                    return 0;
                if (dist.LessThan(radius1 - radius2))
                    // no intersections, one circle contains the other
                    return 0;
                if (dist == DD.Zero) && radius1 == radius2)
                    // infinite intersections Circles coincide
                    return 3;
                if (dist.subtract(radius1).selfSubtract(radius2).abs().doubleValue() <= maxDelta)
                    return 1;

                return 2;
            }

            private Coordinate getIntersection(int index)
            {

                if (intPt != null)
                    return intPt[index];

                DD a = radius1.sqr().selfSubtract(radius2.sqr()).selfAdd(dist.sqr()).selfDivide(Two.multiply(dist));
                double h = radius1.sqr().selfSubtract(a.sqr()).sqrt().doubleValue();

                double cx2 = new DD(center1.x).selfAdd(a.multiply(dx).selfDivide(dist)).doubleValue();
                double cy2 = new DD(center1.y).selfAdd(a.multiply(dy).selfDivide(dist)).doubleValue();

                intPt = new Coordinate[2];
                double dx = this.dx.doubleValue();
                double dy = this.dy.doubleValue();
                double dist = this.dist.doubleValue();

                intPt[0] = new CoordinateXY(cx2 + h * dy / dist,
                                            cy2 - h * dx / dist);
                intPt[1] = new CoordinateXY(cx2 - h * dy / dist,
                                            cy2 + h * dx / dist);

                if (index == 0 || index == 2)
                    return intPt[index];

                return null;
            }
        }
    }
}
