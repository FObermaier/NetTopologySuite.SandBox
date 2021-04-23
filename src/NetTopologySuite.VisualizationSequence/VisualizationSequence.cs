using System;
using System.Collections.Generic;

namespace NetTopologySuite.Geometries.Implementation
{
    [Serializable]
    public sealed class VisualizationSequence : CoordinateSequence
    {
        [NonSerialized]
        private WeakReference<Coordinate[]> _coordinateArrayRef;

        private readonly PointF[] _points;
        private readonly double[] _z;
        private readonly double[] _m;

        public VisualizationSequence(int count, int dimension = 2, int measures = 0)
            : base(count, dimension, measures)
        {
            _points = new PointF[count];
            switch (dimension)
            {
                case 3:
                {
                    if (measures == 0)
                        _z = new double[count];
                    else
                        _m = new double[count];
                    return;
                }
                case 4:
                    _z = new double[count];
                    _m = new double[count];
                    break;
            }
        }

        /// <summary>
        /// Creates an instance of this class using the provided points
        /// </summary>
        /// <param name="points">The visualization points</param>
        /// <param name="z">The z-ordinate values (may be <c>null</c>)</param>
        /// <param name="m">The m-ordinate values (may be <c>null</c>)</param>
        public VisualizationSequence(PointF[] points, double[] z = null, double[] m = null) :
            base(points?.Length ?? 0, 2 + (z != null ? 1 : 0) + (m != null ? 1 : 0), (m != null ? 1 : 0))
        {
            _points = points ?? Array.Empty<PointF>();
            _z = z;
            _m = m;
        }

        /// <inheritdoc cref="CoordinateSequence.GetOrdinate(int, int)"/>
        public override double GetOrdinate(int index, int ordinateIndex)
        {
            if (ordinateIndex == 0)
                return _points[index].X;
            if (ordinateIndex == 1)
                return _points[index].Y;
            if (ordinateIndex == 2)
            {
                if (_z != null)
                    return _z[index];
                if (_m != null)
                    return _m[index];
            }
            if (ordinateIndex == 3 && _m != null)
                    return _m[index];

            return double.NaN;
        }

        /// <inheritdoc cref="CoordinateSequence.SetOrdinate(int, int, double)"/>
        public override void SetOrdinate(int index, int ordinateIndex, double value)
        {
            if (ordinateIndex < 2)
            {
                var point = _points[index];
                _points[index] = ordinateIndex == 0
                    ? new PointF(value, point.Y)
                    : new PointF(point.X, value);
                ReleaseCoordinateArray();
            }

            if (ordinateIndex == 3)
            {
                if (_z != null)
                    _z[index] = value;
                else if (_m != null)
                    _m[index] = value;
            }

            if (ordinateIndex == 4)
            {
                if (_m != null) _m[index] = value;
            }
        }

        /// <inheritdoc cref="CoordinateSequence.Copy"/>
        public override CoordinateSequence Copy()
        {
            var points = new PointF[Count];
            Buffer.BlockCopy(_points, 0, points, 0, Count * 8);

            return new VisualizationSequence(points, _z, _m);
        }

        /// <summary>
        /// Gets a value indicating the stored visualization points
        /// </summary>
        public IReadOnlyList<PointF> XY => _points;

        /// <summary>
        /// Gets a value indicating the stored z-ordinate values
        /// </summary>
        public IReadOnlyList<double> Z => _z;

        /// <summary>
        /// Gets a value indicating the stored m-ordinate values
        /// </summary>
        public IReadOnlyList<double> M => _m;

        /// <inheritdoc cref="CoordinateSequence.ToCoordinateArray"/>>
        public override Coordinate[] ToCoordinateArray()
        {
            var ret = GetCachedCoords();
            if (ret != null)
            {
                return ret;
            }

            ret = new Coordinate[Count];
            if (_z != null)
            {
                if (_m != null)
                {
                    for (int i = 0; i < ret.Length; i++)
                        ret[i] = new CoordinateZM(_points[i].X, _points[i].Y, _z[i], _m[i]);
                }
                else
                {
                    for (int i = 0; i < ret.Length; i++)
                        ret[i] = new CoordinateZ(_points[i].X, _points[i].Y, _z[i]);
                }
            }
            else
            {
                if (_m != null)
                {
                    for (int i = 0; i < ret.Length; i++)
                        ret[i] = new CoordinateM(_points[i].X, _points[i].Y, _m[i]);
                }
                else
                {
                    for (int i = 0; i < ret.Length; i++)
                        ret[i] = new Coordinate(_points[i].X, _points[i].Y);
                }
            }

            _coordinateArrayRef = new WeakReference<Coordinate[]>(ret);
            return ret;

        }
        /// <summary>
        /// Releases the weak reference to the weak referenced coordinate array
        /// </summary>
        /// <remarks>This is necessary if you modify the values of the <see cref="XY"/>, <see cref="Z"/>, <see cref="M"/> arrays externally.</remarks>
        public void ReleaseCoordinateArray()
        {
            _coordinateArrayRef = null;
        }

        private Coordinate[] GetCachedCoords()
        {
            Coordinate[] array = null;
            _coordinateArrayRef?.TryGetTarget(out array);
            return array;
        }
    }
}
