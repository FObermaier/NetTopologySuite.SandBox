using System.Collections.Generic;
using System.IO;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.Index.Rbush.Test
{
    /// <summary>Adapter for different kinds of indexes</summary>
    public interface IIndex<T>
    {
        void Insert(Envelope itemEnv, T item);
        IList<T> Query(Envelope searchEnv);
        void FinishInserting();

        void Search(Envelope env, TextWriter @out);
    }
}
