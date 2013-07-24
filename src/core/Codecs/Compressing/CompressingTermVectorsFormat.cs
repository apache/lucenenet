using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Compressing
{
    public class CompressingTermVectorsFormat : TermVectorsFormat
    {
        private readonly string formatName;
        private readonly string segmentSuffix;
        private readonly CompressionMode compressionMode;
        private readonly int chunkSize;

        public CompressingTermVectorsFormat(string formatName, string segmentSuffix,
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

        public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
        {
            return new CompressingTermVectorsReader(directory, segmentInfo, segmentSuffix,
                fieldInfos, context, formatName, compressionMode);
        }

        public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context)
        {
            return new CompressingTermVectorsWriter(directory, segmentInfo, segmentSuffix,
                context, formatName, compressionMode, chunkSize);
        }

        public override string ToString()
        {
            return GetType().Name + "(compressionMode=" + compressionMode
                + ", chunkSize=" + chunkSize + ")";
        }
    }
}
