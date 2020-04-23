using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;

namespace NetTopologySuite.Index.Bushes
{
    public partial class Rbush<T>
    {
        /// <summary>
        /// A node in an <see cref="Rbush{T}"/> tree.
        /// </summary>
        [Serializable]
        private class Node : IBoundable<Envelope, T>, ISerializable
        {
            /// <summary>
            /// Creates an instance of this class
            /// </summary>
            /// <param name="items"></param>
            public Node(Span<IBoundable<Envelope, T>> items)
            {
                Children = new List<IBoundable<Envelope,T>>(items.ToArray());
                IsLeaf = true;
                Height = 1;
            }

            /// <summary>
            /// Creates a node from serialization
            /// </summary>
            /// <param name="info">The serialization object.</param>
            /// <param name="context">The streaming context.</param>
            public Node(SerializationInfo info, StreamingContext context)
            {
                IsLeaf = info.GetBoolean("isLeaf");
                Height = info.GetInt32("height");
                Bounds = (Envelope) info.GetValue("bounds", typeof(Envelope));
                int numChildren = info.GetInt32("numChildren");
                Children = new List<IBoundable<Envelope, T>>(numChildren);
                for (int i = 0; i < numChildren; i++)
                    Children.Add((IBoundable<Envelope, T>)info.GetValue($"c{i}", typeof(object)));
            }

            /// <summary>
            /// Gets the children of this node. If this node is a leaf,
            /// children are of type <see cref="ItemBoundable{T,TItem}"/>,
            /// otherwise they are <see cref="Node"/>s.
            /// </summary>
            public List<IBoundable<Envelope, T>> Children { get; }

            /// <summary>
            /// Gets a value indicating the height of this node.
            /// </summary>
            public int Height { get; internal set; }

            /// <summary>
            /// Gets a value indicating if this node is a leaf
            /// </summary>
            public bool IsLeaf { get; internal set; }

            /// <inheritdoc cref="IBoundable{T,TItem}.Bounds"/>
            public Envelope Bounds { get; } = new Envelope();

            /// <inheritdoc cref="IBoundable{T,TItem}.Item"/>
            public T Item
            {
                get => default;
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("isLeaf", IsLeaf);
                info.AddValue("height", Height);
                info.AddValue("bounds", Bounds);
                info.AddValue("numChildren", Children.Count);
                for (int i = 0; i < Children.Count; i++)
                    info.AddValue($"c{i}", Children[i], typeof(object));
            }

            public override string ToString()
            {
                return $"{{Node(isLeaf={IsLeaf}, height={Height}, bounds={Bounds}, children={Children.Count}}}";
            }

#if DEBUG
            public bool IntegrityCheck
            {
                get
                {

                    for (int i = 0; i < Children.Count; i++)
                    {
                        if (!Bounds.Contains(Children[i].Bounds))
                            return false;
                    }

                    /*
                    if (!IsLeaf)
                    {
                        for (int i = 0; i < Children.Count; i++)
                            for (int j = i+1; j < Children.Count; j++)
                                if (Children[i].Bounds.Intersects(Children[j].Bounds))
                                    return false;
                    }
                     */
                    return true;
                }
            }
#endif
        }
    }
}
