using System;

namespace Lucene.Net.Spatial
{
    /// <summary>
    /// LUCENENET: Exception thrown when an operation fails in a SpatialTest.
    /// Replaces generic ApplicationException that is not supported on .NET
    /// Core.
    /// </summary>
    public class SpatialTestException : Exception
    {
        public SpatialTestException(string message) 
            : base(message)
        { }

        public SpatialTestException(string message, Exception innerException) 
            : base(message, innerException)
        { }
    }
}
