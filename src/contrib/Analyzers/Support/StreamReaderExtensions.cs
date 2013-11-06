using System.IO;

namespace Lucene.Net.Analysis.Support
{
    public static class StreamReaderExtensions
    {
        public static void Reset(this StreamReader sr)
        {
            sr.BaseStream.Position = 0;
            sr.DiscardBufferedData();
        }
    }
}
