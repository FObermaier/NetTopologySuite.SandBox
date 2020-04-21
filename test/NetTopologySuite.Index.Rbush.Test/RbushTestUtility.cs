using System;
using System.Collections;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;

namespace NetTopologySuite.Index.Rbush.Test
{
    public static class ItemValueGenerators
    {
        public static int GetSomeValueInt32(Random rnd)
        {
            return rnd.Next(30, 90);
        }
        public static double GetSomeValueDouble(Random rnd)
        {
            return 30d + rnd.NextDouble() * 60d;
        }
        public static Guid GetSomeValueGuid(Random rnd)
        {
            return Guid.NewGuid();
        }

    }

    public class RbushTestUtility<T>
    {
        private static readonly double[][] Data =
        {
            new double[] {0, 0, 0, 0}, new double[] {10, 10, 10, 10}, new double[] {20, 20, 20, 20}, new double[] {25, 0, 25, 0},
            new double[] {35, 10, 35, 10}, new double[] {45, 20, 45, 20}, new double[] {0, 25, 0, 25}, new double[] {10, 35, 10, 35},
            new double[] {20, 45, 20, 45}, new double[] {25, 25, 25, 25}, new double[] {35, 35, 35, 35}, new double[] {45, 45, 45, 45},
            new double[] {50, 0, 50, 0}, new double[] {60, 10, 60, 10}, new double[] {70, 20, 70, 20}, new double[] {75, 0, 75, 0},
            new double[] {85, 10, 85, 10}, new double[] {95, 20, 95, 20}, new double[] {50, 25, 50, 25}, new double[] {60, 35, 60, 35},
            new double[] {70, 45, 70, 45}, new double[] {75, 25, 75, 25}, new double[] {85, 35, 85, 35}, new double[] {95, 45, 95, 45},
            new double[] {0, 50, 0, 50}, new double[] {10, 60, 10, 60}, new double[] {20, 70, 20, 70}, new double[] {25, 50, 25, 50},
            new double[] {35, 60, 35, 60}, new double[] {45, 70, 45, 70}, new double[] {0, 75, 0, 75}, new double[] {10, 85, 10, 85},
            new double[] {20, 95, 20, 95}, new double[] {25, 75, 25, 75}, new double[] {35, 85, 35, 85}, new double[] {45, 95, 45, 95},
            new double[] {50, 50, 50, 50}, new double[] {60, 60, 60, 60}, new double[] {70, 70, 70, 70}, new double[] {75, 50, 75, 50},
            new double[] {85, 60, 85, 60}, new double[] {95, 70, 95, 70}, new double[] {50, 75, 50, 75}, new double[] {60, 85, 60, 85},
            new double[] {70, 95, 70, 95}, new double[] {75, 75, 75, 75}, new double[] {85, 85, 85, 85}, new double[] {95, 95, 95, 95}
        };
        private readonly Random RND = new Random(17);

        private readonly Func<Random, T> _someValueGenerator;

        public RbushTestUtility(Func<Random, T> someValueGenerator)
        {
            _someValueGenerator = someValueGenerator;
        }

        private T GetSomeValue()
        {
            return _someValueGenerator(RND);
        }

        public IList<ItemBoundable<Envelope, T>> SomeData(int n)
        {
            if (n > Data.Length)
                throw new ArgumentOutOfRangeException(nameof(n));

            var res = new ItemBoundable<Envelope, T>[n];
            for (int i = 0; i < n; i++)
            {
                double[] bbox = Data[i];
                res[i] = new ItemBoundable<Envelope, T>(new Envelope(bbox[0], bbox[2], bbox[1], bbox[3]), GetSomeValue());
            }

            return res;
        }
    }
}
