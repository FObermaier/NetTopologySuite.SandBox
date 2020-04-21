using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index;
//using NetTopologySuite.Index.HPRtree;
using NetTopologySuite.Index.Quadtree;
using NetTopologySuite.Index.Rbush;
using NetTopologySuite.Index.Rbush.Test;
using NetTopologySuite.Index.Strtree;
using NUnit.Framework;

namespace NetTopologySuite.Tests.NUnit.Performance.Index
{
    public class TreeTimeTest
    {
        public const int NUM_ITEMS = 100_000;

        [Test]
        public void TestWithObject()
        {
            int n = NUM_ITEMS;
            //var items = IndexTester<object>.CreateGridItems(n, t => new object());
            var items = IndexTester<object>.CreateRandomItems(n, x => new object()).ToArray();
            var queries = IndexTester<object>.CreateRandomBoxes(n).ToArray();

            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("Dummy run to ensure classes are loaded before real run");
            Console.WriteLine("------------------------------------------------------");
            Run(items, queries);
            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("Real run");
            Console.WriteLine("------------------------------------------------------");
            Run(items, queries);
        }

        private IList<IndexResult> Run<T>(IList<Tuple<Envelope, T>> items, IList<Envelope> queries)
        {
            var indexResults = new List<IndexResult>();
            Console.WriteLine($"# items = {items.Count}");
            //indexResults.Add(Run(new RbushIndex<T>(4), items, queries));
            indexResults.Add(Run(new RbushIndex<T>(6), items, queries));
            //indexResults.Add(Run(new RbushIndex<T>(7), items, queries));
            indexResults.Add(Run(new RbushIndex<T>(8), items, queries));
            //indexResults.Add(Run(new RbushIndex<T>(9), items, queries));
            indexResults.Add(Run(new RbushIndex<T>(10), items, queries));
            //indexResults.Add(Run(new RbushIndex<T>(11), items, queries));
            indexResults.Add(Run(new RbushIndex<T>(12), items, queries));
            //indexResults.Add(Run(new RbushIndex<T>(13), items, queries));
            indexResults.Add(Run(new RbushIndex<T>(14), items, queries));
            //indexResults.Add(Run(new RbushIndex<T>(15), items, queries));
            indexResults.Add(Run(new RbushIndex<T>(16), items, queries));

            indexResults.Add(Run(new STRtreeIndex<T>(6), items, queries));
            indexResults.Add(Run(new STRtreeIndex<T>(10), items, queries));
            indexResults.Add(Run(new STRtreeIndex<T>(14), items, queries));
            //indexResults.Add(Run(new QuadtreeIndex<T>(), items, queries));
            //indexResults.add(run(new QXtreeIndex(), n));
            //indexResults.Add(Run(new EnvelopeListIndex(), items.Select(t => Tuple.Create(t.Item1, t.Item1)).ToArray()));
            return indexResults;
        }

        private IndexResult Run<T>(IIndex<T> index, IList<Tuple<Envelope, T>> items, IList<Envelope> queries)
        {
            return new IndexTester<T>(index).TestAll(items, queries);
        }

        private class STRtreeIndex<T> : IIndex<T>
        {
            public STRtreeIndex(int nodeCapacity)
            {
                _index = new STRtree<T>(nodeCapacity);
            }

            private readonly STRtree<T> _index;

            public void Insert(Envelope itemEnv, T item)
            {
                _index.Insert(itemEnv, item);
            }

            public IList<T> Query(Envelope searchEnv)
            {
                return _index.Query(searchEnv);
            }

            public void FinishInserting()
            {
                _index.Build();
            }

            public void Search(Envelope env, TextWriter @out)
            {
                
            }

            public override string ToString()
            {
                return $"STR[M={_index.NodeCapacity}]";
            }
        }


        class QuadtreeIndex<T> : IIndex<T>
        {
            private readonly Quadtree<T> index = new Quadtree<T>();

            public void Insert(Envelope itemEnv, T item)
            {
                index.Insert(itemEnv, item);
            }

            public IList<T> Query(Envelope searchEnv)
            {
                return index.Query(searchEnv);
            }

            public void FinishInserting()
            {
            }

            public void Search(Envelope env, TextWriter @out)
            {
                
            }

            public override string ToString()
            {
                return "Quad";
            }
        }

        class RbushIndex<T> : IIndex<T>
        {
            private readonly Rbush<T> _index;
            private readonly int _maxNodeCapacity;
            private readonly List<ItemBoundable<Envelope, T>> _items = new List<ItemBoundable<Envelope, T>>();

            public RbushIndex(int maxNodeCapacity)
            {
                _index = new Rbush<T>(_maxNodeCapacity=maxNodeCapacity);
            }

            public void Insert(Envelope itemEnv, T item)
            {
                _items.Add(new ItemBoundable<Envelope, T>(itemEnv, item));
            }

            public IList<T> Query(Envelope searchEnv)
            {
                return _index.Query(searchEnv);
            }

            public void FinishInserting()
            {
                _index.Load(_items);
                _items.Clear();
                
                //_index.IntegrityCheck();
            }

            public void Search(Envelope env, TextWriter @out)
            {
#if DEBUG
                @out.WriteLine();
                @out.WriteLine("Searching for {0}", env);

                _index.Search(env, @out);
#endif
            }

            public override string ToString()
            {
                return $"Rbush[M{_maxNodeCapacity}]";
            }
        }
    }
}
