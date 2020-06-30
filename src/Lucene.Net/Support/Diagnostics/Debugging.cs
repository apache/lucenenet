using Lucene.Net.Util;

namespace Lucene.Net.Diagnostics
{
    // LUCENENET: This can only be named Debug if we merge it with the Debug
    // class from Lucene.Net.TestFramework because it is in the same namespace.
    // But that class is dependent upon AssertionException, which is only for testing.
    internal static class Debugging
    {
        /// <summary>
        /// Allows toggling "assertions" on/off even in release builds. The default is <c>false</c>.
        /// <para/>
        /// This allows loggers and testing frameworks to enable test point messages ("TP")
        /// from <see cref="Index.IndexWriter"/>, <see cref="Index.DocumentsWriterPerThread"/>,
        /// <see cref="Index.FreqProxTermsWriterPerField"/>, <see cref="Index.StoredFieldsProcessor"/>,
        /// <see cref="Index.TermVectorsConsumer"/>, and <see cref="Index.TermVectorsConsumerPerField"/>.
        /// </summary>
        public static bool AssertsEnabled { get; set; } = SystemProperties.GetPropertyAsBoolean("assert", false);
    }
}
