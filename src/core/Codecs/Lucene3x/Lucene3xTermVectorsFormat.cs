using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal class Lucene3xTermVectorsFormat : TermVectorsFormat
    {
        public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
        {
            String fileName = IndexFileNames.SegmentFileName(Lucene3xSegmentInfoFormat.GetDocStoreSegment(segmentInfo), "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION);

            // Unfortunately, for 3.x indices, each segment's
            // FieldInfos can lie about hasVectors (claim it's true
            // when really it's false).... so we have to carefully
            // check if the files really exist before trying to open
            // them (4.x has fixed this):
            bool exists;
            if (Lucene3xSegmentInfoFormat.GetDocStoreOffset(segmentInfo) != -1 && Lucene3xSegmentInfoFormat.GetDocStoreIsCompoundFile(segmentInfo))
            {
                String cfxFileName = IndexFileNames.SegmentFileName(Lucene3xSegmentInfoFormat.GetDocStoreSegment(segmentInfo), "", Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION);
                if (segmentInfo.dir.FileExists(cfxFileName))
                {
                    Directory cfsDir = new CompoundFileDirectory(segmentInfo.dir, cfxFileName, context, false);
                    try
                    {
                        exists = cfsDir.FileExists(fileName);
                    }
                    finally
                    {
                        cfsDir.Dispose();
                    }
                }
                else
                {
                    exists = false;
                }
            }
            else
            {
                exists = directory.FileExists(fileName);
            }

            if (!exists)
            {
                // 3x's FieldInfos sometimes lies and claims a segment
                // has vectors when it doesn't:
                return null;
            }
            else
            {
                return new Lucene3xTermVectorsReader(directory, segmentInfo, fieldInfos, context);
            }
        }

        public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context)
        {
            throw new NotSupportedException("this codec can only be used for reading");
        }
    }
}
