using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;

namespace NetTopologySuite.Index.Rbush
{
    public partial class Rbush<T> : ISpatialIndex<T>
    {
        private readonly int _maxEntries;
        private readonly int _minEntries;

        private Node _data;
        private readonly IEqualityComparer<T> _itemEqualityComparer;

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        /// <param name="maxEntries">The maximum number of entries per node</param>
        /// <param name="equalityComparer">An equality comparer for <see cref="T"/>-items</param>
        public Rbush(int maxEntries = 9, IEqualityComparer<T> equalityComparer = null)
        {
            _maxEntries = maxEntries;
            _minEntries = Math.Max(2, (int) Math.Ceiling(this._maxEntries * 0.4));
            _itemEqualityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            Clear();
        }

        /// <summary>
        /// Gets a value indicating the height of the bush.
        /// </summary>
        public int Height
        {
            get => _data.Height;
        }

        /// <inheritdoc cref="ISpatialIndex{T}.Insert"/>>
        public void Insert(Envelope itemEnv, T item)
        {
            Insert(new ItemBoundable<Envelope, T>(itemEnv, item));
        }

        /// <inheritdoc cref="ISpatialIndex{T}.Query(Envelope)"/>>
        public IList<T> Query(Envelope searchEnv)
        {
            var visitor = new ArrayListVisitor<T>();
            Query(searchEnv, visitor);
            return visitor.Items;
        }

        /// <inheritdoc cref="ISpatialIndex{T}.Query(Envelope, IItemVisitor{T})"/>>
        public void Query(Envelope searchEnv, IItemVisitor<T> visitor)
        {
            var node = _data;
            if (!node.Bounds.Intersects(searchEnv)) return;

            var nodesToSearch = new List<Node>();
            while (node != null)
            {
                //Debug.Assert(node.IntegrityCheck);

                for (int i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    var childBounds = child.Bounds;
                    if (searchEnv.Intersects(childBounds))
                    {
                        if (node.IsLeaf)
                            visitor.VisitItem(child.Item);
                        else if(searchEnv.Contains(childBounds))
                        {
                            foreach (T item in GetAll((Node)child))
                                visitor.VisitItem(item);
                        }
                        else
                            nodesToSearch.Add((Node)child);
                    }
                }
                node = Pop(nodesToSearch);
            }
        }

        /// <summary>
        /// Gets the number of items in the index
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                foreach (var item in GetAll(_data))
                    count++;

                return count;
            }
        }

        /// <summary>
        /// Gets all <see cref="ItemBoundable{T,TItem}.Item"/>s that are contained in <paramref name="node"/>
        /// </summary>
        /// <param name="node">A node</param>
        /// <returns>An enumeration of <typeparamref name="T"/>-items.</returns>
        private static IEnumerable<T> GetAll(Node node)
        {
            var stack = new Stack<IBoundable<Envelope, T>>(node.Children);
            while(stack.Count > 0)
            {
                var child = stack.Pop();
                if (child is Node nodeChild)
                    foreach (var grandChild in nodeChild.Children)
                        stack.Push(grandChild);
                else
                    yield return child.Item;
            }
        }

        /// <inheritdoc cref="ISpatialIndex{T}.Remove"/>
        public bool Remove(Envelope itemEnv, T item)
        {
            if (!typeof(T).IsValueType && 
                _itemEqualityComparer.Equals(item, default))
                return false;

            var node = _data;
            var path = new List<Node>();
            var indexes = new List<int>();
            int i = 0;
            Node parent = null;
            bool goingUp = false;
            // depth-first iterative tree traversal
            while (node != null || path.Count > 0)
            {

                if (node == null)
                { // go up
                    node = Pop(path);
                    parent = path[path.Count - 1];
                    i = Pop(indexes);
                    goingUp = true;
                }

                if (node.IsLeaf) {
                    // check current node
                    int index = node.Children.FindIndex(t => _itemEqualityComparer.Equals(item, t.Item));

                    if (index >= 0)
                    {
                        // item found, remove the item and condense tree upwards
                        node.Children.RemoveAt(index);
                        path.Add(node);
                        Condense(path);
                        return true;
                    }
                }

                if (!goingUp && !node.IsLeaf && node.Bounds.Contains(itemEnv)) {
                    // go down
                    path.Add(node);
                    indexes.Add(i);
                    i = 0;
                    parent = node;
                    node = (Node)node.Children[0];

                }
                else if (parent != null) {
                    // go right
                    i++;
                    node = (Node)parent.Children[i];
                    goingUp = false;

                }
                else node = null; // nothing found
            }

            return false;
        }

        private void Condense(IList<Node> path)
        {
            // go through the path, removing empty nodes and updating bboxes
            for (int i = path.Count - 1; i >= 0; i--)
            {
                if (path[i].Children.Count == 0)
                {
                    if (i > 0)
                    {
                        var siblings = path[i - 1].Children;
                        siblings.RemoveAt(siblings.IndexOf(path[i]));

                    }
                    else Clear();

                }
                else CalculateBounds(path[i]);
            }
        }

        /// <summary>
        /// Method to clear the whole Rbush-index
        /// </summary>
        public void Clear()
        {
            _data = new Node(Span<IBoundable<Envelope, T>>.Empty);
        }

        /// <summary>
        /// Utility method to remove the last item of a list
        /// </summary>
        /// <typeparam name="TT">The type of the items kept in the list</typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        private static TT Pop<TT>(IList<TT> items)
        {
            var lastIndex = items.Count - 1;
            if (lastIndex < 0)
                return default;

            var res = items[lastIndex];
            items.RemoveAt(lastIndex);
            return res;
        }

        /// <summary>
        /// Utility method to load items to this index
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <returns>This index.</returns>
        public Rbush<T> Load(params (Envelope, T)[] items)
        {
            var casted = new List<IBoundable<Envelope, T>>();
            foreach (var item in items)
                casted.Add(new ItemBoundable<Envelope, T>(item.Item1, item.Item2));

            return Load(casted.ToArray().AsSpan());
        }

        /// <summary>
        /// Utility method to load items to this index
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <returns>This index.</returns>
        public Rbush<T> Load(IEnumerable<ItemBoundable<Envelope, T>> items)
        {
            return Load(new Span<IBoundable<Envelope, T>>(items.Cast<IBoundable<Envelope, T>>().ToArray()));
        }

        /// <summary>
        /// Utility method to load items to this index
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <returns>This index.</returns>
        private Rbush<T> Load(Span<IBoundable<Envelope, T>> items)
        {
            if (items.IsEmpty)
                return this;

            if (items.Length < _minEntries)
            {
                foreach (var item in items)
                    Insert(item);
                return this;
            }

            // recursively build the tree with the given data from scratch using OMT algorithm
            var node = Build(items, 0, items.Length - 1, 0);

            if (_data.Children.Count == 0)
            {
                // save as is if tree is empty
                _data = node;

            }
            else if (_data.Height == node.Height)
            {
                // split root if trees have the same height
                SplitRoot(_data, node);

            }
            else
            {
                if (_data.Height < node.Height)
                {
                    // swap trees if inserted one is bigger
                    var tmpNode = _data;
                    _data = node;
                    node = tmpNode;
                }

                // insert the small tree into the large tree at appropriate level
                Insert(node, _data.Height - node.Height - 1);
            }

            return this;

        }

        /// <summary>
        /// Method to insert a boundable item to the index
        /// </summary>
        /// <param name="item">A boundable item</param>
        private void Insert(IBoundable<Envelope, T> item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            Insert(item, _data.Height-1);
        }

        /// <summary>
        /// Method to insert a boundable item to the index at a specific level
        /// </summary>
        /// <param name="item">A boundable item</param>
        /// <param name="level">The level to add the boundable at.</param>
        private void Insert(IBoundable<Envelope, T> item, int level)
        {
            //var boundable = item;

            // find the best node for accommodating the item, saving all nodes along the path too
            var node = ChooseSubtree(item, _data, level, out var insertPath);

            // put the item into the node
            node.Children.Add(item);
            ExpandBounds(node, item);

            // split on node overflow; propagate upwards if necessary
            while (level >= 0)
            {
                if (insertPath[level].Children.Count > _maxEntries)
                {
                    Split(insertPath, level);
                    level--;
                }
                else break;
            }

            // adjust bboxes along the insertion path
            AdjustParentBounds(item, insertPath, level);
        }

        private Node ChooseSubtree(IBoundable<Envelope, T> boundable, Node node, int level, out IList<Node> path)
        {
            path = new List<Node>();
            while (true)
            {
                path.Add(node);
                if (node.IsLeaf || path.Count - 1 == level) break;

                double minArea = double.PositiveInfinity;
                double minEnlargement = double.PositiveInfinity;
                Node targetNode = null;

                for (int i = 0; i < node.Children.Count; i++)
                {
                    var child = (Node)node.Children[i];
                    double area = child.Bounds.Area;
                    double enlargement = EnlargedArea(boundable, child) - area;

                    // choose entry with the least area enlargement
                    if (enlargement < minEnlargement)
                    {
                        minEnlargement = enlargement;
                        minArea = area < minArea ? area : minArea;
                        targetNode = child;
                    }
                    else if (enlargement == minEnlargement)
                    {
                        // otherwise choose one with the smallest area
                        if (area < minArea)
                        {
                            minArea = area;
                            targetNode = child;
                        }
                    }
                }
                node = targetNode ?? (Node)node.Children[0];
            }

            return node;
        }

        private static double EnlargedArea(IBoundable<Envelope, T> a, IBoundable<Envelope, T> b)
        {
            var ab = a.Bounds.Copy();
            ab.ExpandToInclude(b.Bounds);
            return ab.Area;
        }

        // split overflowed node into two
        private void Split(IList<Node> insertPath, int level)
        {
            var node = insertPath[level];
            int M = node.Children.Count;
            int m = _minEntries;

            ChooseSplitAxis(node, m, M);

            int splitIndex = ChooseSplitIndex(node, m, M);

            int splitCount = node.Children.Count - splitIndex;
            var newNode = new Node(Splice(node.Children, splitIndex, splitCount)) {
                Height = node.Height,
                IsLeaf = node.IsLeaf
            };

            CalculateBounds(node);
            CalculateBounds(newNode);

            if (level > 0)
                insertPath[level - 1].Children.Add(newNode);
            else
                SplitRoot(node, newNode);
        }

        private void SplitRoot(Node node, Node newNode)
        {
            // split root node
            _data = new Node(new[] {node, newNode}) {Height = node.Height + 1, IsLeaf = false};
            CalculateBounds(_data);
        }

        private static Span<IBoundable<Envelope, T>> Splice(List<IBoundable<Envelope, T>> source, int splitIndex,
            int splitCount)
        {
            var res = source.GetRange(splitIndex, splitCount);
            source.RemoveRange(splitIndex, splitCount);
            return res.ToArray().AsSpan();
        }

        private int ChooseSplitIndex(Node node, int m, int M)
        {
            int? index = null;
            double minOverlap = double.PositiveInfinity;
            double minArea = double.PositiveInfinity;

            for (int i = m; i <= M - m; i++)
            {
                var bbox1 = CalculatePartialBounds(node, 0, i).Bounds;
                var bbox2 = CalculatePartialBounds(node, i, M).Bounds;

                double overlap = bbox1.Intersection(bbox2).Area;
                double area = bbox1.Area + bbox2.Area;

                // choose distribution with minimum overlap
                if (overlap < minOverlap)
                {
                    minOverlap = overlap;
                    index = i;

                    minArea = area < minArea ? area : minArea;

                }
                else if (overlap == minOverlap)
                {
                    // otherwise choose distribution with minimum area
                    if (area < minArea)
                    {
                        minArea = area;
                        index = i;
                    }
                }
            }

            return index ?? M - m;
        }

        // sorts node children by the best axis for split
        /// <summary>
        /// Sorts node children by the best axis for split
        /// </summary>
        /// <param name="node">A node</param>
        /// <param name="m"></param>
        /// <param name="M"></param>
        void ChooseSplitAxis(Node node, int m, int M)
        {
            double xMargin = AllDistMargin(node, m, M, BoundableComparer.CompareMinX);
            double yMargin = AllDistMargin(node, m, M, BoundableComparer.CompareMinY);

            // if total distributions margin value is minimal for x, sort by minX,
            // otherwise it's already sorted by minY
            if (xMargin < yMargin)
                node.Children.Sort(BoundableComparer.CompareMinX);
        }

        // total margin of all possible split distributions where each node is at least m full
        double AllDistMargin(Node node, int m, int M, IComparer<IBoundable<Envelope, T>> compare)
        {
            node.Children.Sort(compare);

            var leftBoundable = CalculatePartialBounds(node, 0, m);
            var leftBBox = leftBoundable.Bounds;
            var rightBoundable = CalculatePartialBounds(node, M - m, M);
            var rightBBox = rightBoundable.Bounds;
            double margin = BoundsMargin(leftBBox) + BoundsMargin(rightBBox);

            for (int i = m; i < M - m; i++)
            {
                var child = node.Children[i];
                ExpandBounds(leftBoundable, child);
                margin += BoundsMargin(leftBBox);
            }

            for (int i = M - m - 1; i >= m; i--)
            {
                var child = node.Children[i];
                ExpandBounds(rightBoundable, child);
                margin += BoundsMargin(rightBBox);
            }

            return margin;
        }

        void AdjustParentBounds(IBoundable<Envelope,T> boundable, IList<Node> path, int level)
        {
            // adjust bboxes along the given tree path
            for (int i = level; i >= 0; i--)
            {
                ExpandBounds(path[i], boundable);
            }
        }

        private static double BoundsMargin(Envelope a)
        {
           return (a.MaxX - a.MinX) + (a.MaxY - a.MinY); 
        }

        private Node Build(Span<IBoundable<Envelope, T>> items, int left, int right, int height)
        {

            int N = right - left + 1;
            int M = _maxEntries;
            Node node;

            if (N <= M)
            {
                // reached leaf level; return leaf
                node = new Node(items.Slice(left, N));
                CalculateBounds(node);
                return node;
            }

            if (height == 0)
            {
                // target height of the bulk-loaded tree
                height = (int)Math.Ceiling(Math.Log(N) / Math.Log(M));

                // target number of root entries to maximize storage utilization
                M = (int)Math.Ceiling(N / Math.Pow(M, height - 1));
            }

            node = new Node(Span<IBoundable<Envelope, T>>.Empty) {
                IsLeaf = false,
                Height = height
            };

            // split the items into M mostly square tiles

            int N2 = (int)Math.Ceiling((double)N / M);
            int N1 = N2 * (int)Math.Ceiling(Math.Sqrt(M));

            MultiSelect(items, left, right, N1, BoundableComparer.CompareMinX);

            for (int i = left; i <= right; i += N1)
            {

                int right2 = Math.Min(i + N1 - 1, right);

                MultiSelect(items, i, right2, N2, BoundableComparer.CompareMinY);

                for (int j = i; j <= right2; j += N2)
                {

                    int right3 = Math.Min(j + N2 - 1, right2);

                    // pack each entry recursively
                    node.Children.Add(Build(items, j, right3, height - 1));
                }
            }

            CalculateBounds(node);

            return node;
        }

        private static void MultiSelect(Span<IBoundable<Envelope, T>> arr, int left, int right, int n,
            IComparer<IBoundable<Envelope, T>> comparer)
        {
            var stack = new Stack<int>(new []{left, right});
            while (true)
            {
                right = stack.Pop();
                left = stack.Pop();

                if (right - left <= 0) break;
                int mid = left + (int) Math.Ceiling(((double)(right - left) / n / 2) * n);
                Quick<IBoundable<Envelope, T>>.Select(arr, mid, left, right, comparer);
                stack.Push(left);
                stack.Push(mid);
                stack.Push(mid);
                stack.Push(right);
            }
        }

        /// <summary>
        /// Calculate the bounds of a node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static Envelope CalculateBounds(Node node)
        {
            return CalculatePartialBounds(node, 0, node.Children.Count, node).Bounds;
        }

        /// <summary>
        /// Calculates the partial extent of a node from its children
        /// </summary>
        /// <param name="node">The node</param>
        /// <param name="startIndex">The start index</param>
        /// <param name="excludedStopIndex">The stop index</param>
        /// <param name="destNode">The node that takes the updated extent. If <c>null</c> a new node will be created.</param>
        /// <returns></returns>
        private static Node CalculatePartialBounds(Node node, int startIndex, int excludedStopIndex, Node destNode = null)
        {
            if (destNode == null)
                destNode = new Node(null);
            else
                destNode.Bounds.Init();

            for (int i = startIndex; i < excludedStopIndex; i++)
            {
                var child = node.Children[i];
                ExpandBounds(destNode, child);
            }

            return destNode;
        }

        /// <summary>
        /// Expand the bounds of a node to include the bounds of another node
        /// </summary>
        /// <param name="a">The node which bounds are to be expanded</param>
        /// <param name="b">A node</param>
        private static void ExpandBounds(Node a, IBoundable<Envelope, T> b)
        {
            a.Bounds.ExpandToInclude(b.Bounds);
        }

        /// <summary>
        /// A comparer for <see cref="IBoundable{T,TItem}"/>s.
        /// </summary>
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private abstract class BoundableComparer : IComparer<IBoundable<Envelope, T>>
        {
            public static IComparer<IBoundable<Envelope, T>> CompareMinX { get; } = new MinXBoundableComparer();
            public static IComparer<IBoundable<Envelope, T>> CompareMinY { get; } = new MinYBoundableComparer();

            public abstract int Compare(IBoundable<Envelope, T> x, IBoundable<Envelope, T> y);

            private sealed class MinXBoundableComparer : BoundableComparer
            {
                public override int Compare(IBoundable<Envelope, T> x, IBoundable<Envelope, T> y)
                {
                    return Math.Sign(x.Bounds.MinX - y.Bounds.MinX);
                }
            }

            private sealed class MinYBoundableComparer : BoundableComparer
            {
                public override int Compare(IBoundable<Envelope, T> x, IBoundable<Envelope, T> y)
                {
                    return Math.Sign(x.Bounds.MinY - y.Bounds.MinY);
                }
            }
        }

#if DEBUG
        public void IntegrityCheck()
        {
            var path = new List<Tuple<int, Node>>();
            IntegrityCheck(-1, _data, path);
        }

        private void IntegrityCheck(int index, Node node, IList<Tuple<int, Node>> path) {

            if (!node.IntegrityCheck)
            {
                foreach (var pn in path)
                    Console.WriteLine(pn);
                throw new InvalidDataException();
            }

            if (!node.IsLeaf)
            {
                path.Add(Tuple.Create(index, node));
                int i = 0;
                foreach (Node child in node.Children)
                    IntegrityCheck(i++, child, path);
                Pop(path);
            }
        }

        public void Search(Envelope searchEnvelope, TextWriter @out)
        {
            var path = new List<Tuple<int, Node>>();
            Search(searchEnvelope, -1, _data, path, @out);
        }

        private void Search(Envelope searchEnvelope, int index, Node node, IList<Tuple<int, Node>> path, TextWriter @out)
        {

            if (!searchEnvelope.Intersects(node.Bounds))
                return;

            if (node.IsLeaf)
            {
                foreach (Tuple<int, Node> tuple in path)
                    @out.WriteLine("N{0} - {1}", tuple.Item1, tuple.Item2);
                @out.WriteLine("N{0} - {1}", index, node);
                int i = 0;
                foreach (var child in node.Children)
                    @out.WriteLine("I{0} - {1} - {2}", i++, searchEnvelope.Intersects(child.Bounds), child.Bounds);
            }
            else
            {
                path.Add(Tuple.Create(index, node));
                int i = 0;
                foreach (Node child in node.Children)
                {
                    Search(searchEnvelope, i++, child, path, @out);
                }
                Pop(path);
            }
        }
#endif
    }
}
