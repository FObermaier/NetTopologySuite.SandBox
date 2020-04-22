using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.Index.Rbush
{
    /// <summary>
    /// Static 2d spatial index implemented using packed Hilbert R-tree.
    /// <para/>
    /// This is an adaptation of Jedidiah Buck McCready's 
    /// <a href="https://github.com/jbuckmccready/Flatbush">Flatbush</a> index
    /// to fit in the <see cref="ISpatialIndex{T}"/> ecosystem.<br/>
    /// Flatbush was released under MIT License.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class Flatbush<T> : ISpatialIndex<T>
    {
        private readonly int _numItems;
        private readonly int _nodeSize;
        private readonly int[] _levelBounds;

        private readonly Envelope[] _boxes;
        private readonly T[] _items;
        private readonly int[] _indices;
        private int _pos;

        private readonly Envelope _bounds;

        /// <summary>
        /// Create a new static 2d spatial index.
        /// </summary>
        /// <param name="numItems">The fixed number of 2d boxes to be included in the index.</param>
        /// <param name="nodeSize">Size of the tree node, adjust to tune for particular use case performance.</param>
        public Flatbush(int numItems, int nodeSize = 16)
        {
            if (numItems <= 0)
            {
                throw new ArgumentException("numItems must be greater than zero", nameof(numItems));
            }

            _numItems = numItems;
            _nodeSize = Math.Min(Math.Max(nodeSize, 2), 65535);

            // calculate the total number of nodes in the R-tree to allocate space for
            // and the index of each tree level (used in search later)
            int n = numItems;
            int numNodes = n;
            var levelBounds = new List<int> {n};
            do
            {
                n = (int)Math.Ceiling((double)n / _nodeSize);
                numNodes += n;
                levelBounds.Add(numNodes);
            } while (n != 1);

            _levelBounds = levelBounds.ToArray();

            _boxes = new Envelope[numNodes];
            _indices = new int[numNodes];
            _items = new T[numItems];

            _pos = 0;
            _bounds = new Envelope();
        }

        /// <summary>
        /// Gets a value indicating the area covered by the tree
        /// </summary>
        public Envelope Bounds => _bounds.Copy();

        /// <summary>
        /// Gets a value indicating the number of items in the tree
        /// </summary>
        public int Count
        {
            get { return _pos < _numItems ? _pos : _numItems; }
        }

        /// <summary>
        /// Add a new 2d box to the spatial index, must not go over the static size given at time of construction.
        /// </summary>
        /// <param name="itemBounds">The bounds for <paramref name="item"/></param>
        /// <param name="item">The item</param>
        public void Insert(Envelope itemBounds, T item)
        {
            if (_pos == _boxes.Length)
                throw new InvalidOperationException("Adding items to built tree is not allowed.");
            if (_pos >= _numItems)
                throw new InvalidOperationException($"Adding more than {_numItems} items is not allowed.");

            _indices[_pos] = _pos;

            _boxes[_pos] = itemBounds;
            _items[_pos++] = item;

            _bounds.ExpandToInclude(itemBounds);
        }

        /// <inheritdoc cref="ISpatialIndex{T}.Remove"/>
        /// <remarks>
        /// This function always does nothing and returns <c>false</c> under all circumstances.
        /// The nature of <see cref="Flatbush{T}"/> prohibits removing items from the tree.
        /// </remarks>
        public bool Remove(Envelope itemEnv, T item)
        {
            return false;
        }

        /// <summary>
        /// Method to perform the indexing, to be called after adding all the boundable items via <see cref="Insert"/>.
        /// </summary>
        public void Build()
        {
            if (_pos != _numItems)
            {
                throw new InvalidOperationException($"Added {_pos} items when expected {_numItems}.");
            }

            // if number of items is less than node size then skip sorting since each node of boxes must be
            // fully scanned regardless and there is only one node
            if (_numItems <= _nodeSize)
            {
                // fill root box with total extents
                _boxes[_pos++] = _bounds;
                return;
            }

            double width = _bounds.Width;
            double height = _bounds.Height;
            uint[] hilbertValues = new uint[_numItems];
            int pos;

            // map item centers into Hilbert coordinate space and calculate Hilbert values
            for (int i = 0; i < _numItems; i++)
            {
                var bounds = _boxes[i];

                const int n = 1 << 16;
                // hilbert max input value for x and y
                const int hilbertMax = n - 1;
                // mapping the x and y coordinates of the center of the box to values in the range [0 -> n - 1] such that
                // the min of the entire set of bounding boxes maps to 0 and the max of the entire set of bounding boxes maps to n - 1
                // our 2d space is x: [0 -> n-1] and y: [0 -> n-1], our 1d hilbert curve value space is d: [0 -> n^2 - 1]
                var centre = bounds.Centre;
                uint x = (uint)Math.Floor(hilbertMax * (centre.X / 2d - _bounds.MinX) / width);
                uint y = (uint)Math.Floor(hilbertMax * (centre.Y / 2d - _bounds.MinY) / height);
                hilbertValues[i] = Hilbert(x, y);
            }

            // sort items by their Hilbert value (for packing later)
            Sort(hilbertValues, _boxes, _indices, 0, _numItems - 1);

            // generate nodes at each tree level, bottom-up
            pos = 0;
            for (int i = 0; i < _levelBounds.Length - 1; i++)
            {
                int end = _levelBounds[i];

                // generate a parent node for each block of consecutive <nodeSize> nodes
                while (pos < end)
                {
                    var nodeBounds = new Envelope();
                    int nodeIndex = pos;

                    // calculate bounds for the new node
                    for (int j = 0; j < _nodeSize && pos < end; j++)
                        nodeBounds.ExpandToInclude(_boxes[pos++]);

                    // add the new node to the tree data
                    _indices[_pos] = nodeIndex;
                    _boxes[_pos++] = nodeBounds;
                }
            }
        }

        /// <inheritdoc cref="ISpatialIndex{T}.Query(Envelope)"/>
        public IList<T> Query(Envelope searchBounds)
        {
            var visitor = new ArrayListVisitor<T>();
            Query(searchBounds, visitor);
            return visitor.Items;
        }

        /// <inheritdoc cref="ISpatialIndex{T}.Query(Envelope, IItemVisitor{T})"/>
        public void Query(Envelope searchBounds, IItemVisitor<T> visitor)
        {
            // if the tree has not been built, do it now.
            if (_pos != _boxes.Length) Build();

            int nodeIndex = _boxes.Length - 1;
            int level = _levelBounds.Length - 1;

            // stack for traversing nodes
            var stack = new Stack<int>();
            bool done = false;

            while (!done)
            {
                // find the end index of the node
                int end = Math.Min(nodeIndex + _nodeSize, _levelBounds[level]);

                // search through child nodes
                for (int pos = nodeIndex; pos < end; pos++)
                {
                    int index = _indices[pos];

                    // check if node bbox intersects with query bbox
                    if (!searchBounds.Intersects(_boxes[pos]))
                        continue;

                    if (nodeIndex < _numItems)
                    {
                        visitor.VisitItem(_items[index]);
                    }
                    else
                    {
                        // push node index and level for further traversal
                        stack.Push(index);
                        stack.Push(level - 1);
                    }
                }

                if (stack.Count > 1)
                {
                    level = stack.Pop();
                    nodeIndex = stack.Pop();
                }
                else
                {
                    done = true;
                }
            }

        }

        // custom quicksort that sorts bbox data alongside the hilbert values
        private static void Sort(uint[] values, Envelope[] boxes, int[] indices, int left, int right)
        {
            if (left >= right) return;

            uint pivot = values[(left + right) >> 1];
            int i = left - 1;
            int j = right + 1;

            while (true)
            {
                do i++; while (values[i] < pivot);
                do j--; while (values[j] > pivot);
                if (i >= j) break;
                Swap(values, boxes, indices, i, j);
            }

            Sort(values, boxes, indices, left, j);
            Sort(values, boxes, indices, j + 1, right);
        }

        // swap two values and two corresponding boxes
        private static void Swap(uint[] values, Envelope[] boxes, int[] indices, int i, int j)
        {
            uint temp = values[i];
            values[i] = values[j];
            values[j] = temp;

            var a = boxes[i];
            boxes[i] = boxes[j];
            boxes[j] = a;

            int e = indices[i];
            indices[i] = indices[j];
            indices[j] = e;
        }

        // Fast Hilbert curve algorithm by http://threadlocalmutex.com/
        // Ported from C++ https://github.com/rawrunprotected/hilbert_curves (public domain)
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static uint Hilbert(uint x, uint y)
        {

            uint a = x ^ y;
            uint b = 0xFFFF ^ a;
            uint c = 0xFFFF ^ (x | y);
            uint d = x & (y ^ 0xFFFF);

            uint A = a | (b >> 1);
            uint B = (a >> 1) ^ a;
            uint C = ((c >> 1) ^ (b & (d >> 1))) ^ c;
            uint D = ((a & (c >> 1)) ^ (d >> 1)) ^ d;

            a = A; b = B; c = C; d = D;
            A = (a & (a >> 2)) ^ (b & (b >> 2));
            B = (a & (b >> 2)) ^ (b & ((a ^ b) >> 2));
            C ^= (a & (c >> 2)) ^ (b & (d >> 2));
            D ^= (b & (c >> 2)) ^ ((a ^ b) & (d >> 2));

            a = A; b = B; c = C; d = D;
            A = (a & (a >> 4)) ^ (b & (b >> 4));
            B = (a & (b >> 4)) ^ (b & ((a ^ b) >> 4));
            C ^= (a & (c >> 4)) ^ (b & (d >> 4));
            D ^= (b & (c >> 4)) ^ ((a ^ b) & (d >> 4));

            a = A; b = B; c = C; d = D;
            C ^= (a & (c >> 8)) ^ (b & (d >> 8));
            D ^= (b & (c >> 8)) ^ ((a ^ b) & (d >> 8));

            a = C ^ (C >> 1);
            b = D ^ (D >> 1);

            uint i0 = x ^ y;
            uint i1 = b | (0xFFFF ^ (i0 | a));

            i0 = (i0 | (i0 << 8)) & 0x00FF00FF;
            i0 = (i0 | (i0 << 4)) & 0x0F0F0F0F;
            i0 = (i0 | (i0 << 2)) & 0x33333333;
            i0 = (i0 | (i0 << 1)) & 0x55555555;

            i1 = (i1 | (i1 << 8)) & 0x00FF00FF;
            i1 = (i1 | (i1 << 4)) & 0x0F0F0F0F;
            i1 = (i1 | (i1 << 2)) & 0x33333333;
            i1 = (i1 | (i1 << 1)) & 0x55555555;

            return (i1 << 1) | i0;
        }
    }
}
