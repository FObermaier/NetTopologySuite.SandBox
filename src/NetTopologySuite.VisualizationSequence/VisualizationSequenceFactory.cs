namespace NetTopologySuite.Geometries.Implementation
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class VisualizationSequenceFactory : CoordinateSequenceFactory
    {
        public override CoordinateSequence Create(int size, int dimension, int measures)
        {
            return new VisualizationSequence(size, dimension, measures);
        }
    }
}
