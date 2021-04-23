using System;
using System.Runtime.InteropServices;
using NetTopologySuite.Geometries.Implementation;
using NUnit.Framework;
using SkiaSharp;
using SDPoint = System.Drawing.PointF;


namespace NetTopologySuite.VisualizationSequence.Test
{
    public class PointFTests
    {
        [Test]
        public void TestConstructor()
        {
            var pt1 = new PointF(1f, 2f);
            var pt2 = new PointF(1f, 2d);
            var pt3 = new PointF(1d, 2f);

            Assert.That(pt1.X, Is.EqualTo(1f));
            Assert.That(pt1.Y, Is.EqualTo(2f));

            Assert.That(pt1, Is.EqualTo(pt2).Using<PointF>((u, v) => u.X == v.X && u.Y == v.Y));
            Assert.That(pt1, Is.EqualTo(pt2).Using<PointF>((u, v) => u.X == v.X && u.Y == v.Y));
            Assert.That(pt2, Is.EqualTo(pt3).Using<PointF>((u, v) => u.X == v.X && u.Y == v.Y));
        }

        [Test]
        public void TestWithSkiaSharp()
        {
            var skPt = new Span<SKPoint>(new[] { new SKPoint(1, 2) });
            var pt = MemoryMarshal.Cast<SKPoint, PointF>(skPt);
            Assert.That(pt[0].X, Is.EqualTo(1f));
            Assert.That(pt[0].Y, Is.EqualTo(2f));
            Assert.That(pt[0], Is.EqualTo(skPt[0]).Using<PointF, SKPoint>((u, v) => u.X == v.X && u.Y == v.Y));

            pt[0] = new PointF(3, 4);
            skPt = MemoryMarshal.Cast<PointF, SKPoint>(pt);

            Assert.That(pt[0].X, Is.EqualTo(3f));
            Assert.That(pt[0].Y, Is.EqualTo(4f));
            Assert.That(pt[0], Is.EqualTo(skPt[0]).Using<PointF, SKPoint>((u, v) => u.X == v.X && u.Y == v.Y));
        }

        [Test]
        public void TestWithSystemDrawing()
        {
            var skPt = new Span<SDPoint>(new[] { new SDPoint(1, 2) });
            var pt = MemoryMarshal.Cast<SDPoint, PointF>(skPt);

            Assert.That(pt[0].X, Is.EqualTo(1f));
            Assert.That(pt[0].Y, Is.EqualTo(2f));
            Assert.That(pt[0], Is.EqualTo(skPt[0]).Using<PointF, SDPoint>((u, v) => u.X == v.X && u.Y == v.Y));

            pt[0] = new PointF(3, 4);
            skPt = MemoryMarshal.Cast<PointF, SDPoint>(pt);

            Assert.That(pt[0].X, Is.EqualTo(3f));
            Assert.That(pt[0].Y, Is.EqualTo(4f));
            Assert.That(pt[0], Is.EqualTo(skPt[0]).Using<PointF, SDPoint>((u, v) => u.X == v.X && u.Y == v.Y));
        }
    }
}
