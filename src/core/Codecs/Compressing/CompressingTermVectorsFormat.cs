using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Compressing
{
    public class CompressingTermVectorsFormat: TermVectorsFormat
    {
        private string formatName;
        private string segmentSuffix;
        private CompressionMode compressionMode;
        private int chunkSize;

        public CompressingTermVectorsFormat(String formatName, String segmentSuffix, 
            CompressionMode compressionMode, int chunkSize)
        {
            this.formatName = formatName;
            this.segmentSuffix = segmentSuffix;
            this.compressionMode = compressionMode;
            if (chunkSize < 1)
            {
                throw new ArgumentException("chunkSize must be >= 1");
            }
            this.chunkSize = chunkSize;
        }
    }
}
