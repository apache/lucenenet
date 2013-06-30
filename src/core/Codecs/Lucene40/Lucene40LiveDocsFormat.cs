using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    public class Lucene40LiveDocsFormat : LiveDocsFormat
    {
        internal const string DELETES_EXTENSION = "del";

        public Lucene40LiveDocsFormat()
        {
        }

        public override IMutableBits NewLiveDocs(int size)
        {
            BitVector bitVector = new BitVector(size);
            bitVector.InvertAll();
            return bitVector;
        }

        public override IMutableBits NewLiveDocs(IBits existing)
        {
            BitVector liveDocs = (BitVector)existing;
            return (IMutableBits)liveDocs.Clone();
        }

        public override IBits ReadLiveDocs(Directory dir, SegmentInfoPerCommit info, IOContext context)
        {
            String filename = IndexFileNames.FileNameFromGeneration(info.info.name, DELETES_EXTENSION, info.DelGen);
            BitVector liveDocs = new BitVector(dir, filename, context);
            //assert liveDocs.count() == info.info.getDocCount() - info.getDelCount():
            //  "liveDocs.count()=" + liveDocs.count() + " info.docCount=" + info.info.getDocCount() + " info.getDelCount()=" + info.getDelCount();
            //assert liveDocs.length() == info.info.getDocCount();
            return liveDocs;
        }

        public override void WriteLiveDocs(IMutableBits bits, Directory dir, SegmentInfoPerCommit info, int newDelCount, IOContext context)
        {
            String filename = IndexFileNames.FileNameFromGeneration(info.info.name, DELETES_EXTENSION, info.NextDelGen);
            BitVector liveDocs = (BitVector)bits;
            //assert liveDocs.count() == info.info.getDocCount() - info.getDelCount() - newDelCount;
            //assert liveDocs.length() == info.info.getDocCount();
            liveDocs.Write(dir, filename, context);
        }

        public override void Files(SegmentInfoPerCommit info, ICollection<string> files)
        {
            if (info.HasDeletions)
            {
                files.Add(IndexFileNames.FileNameFromGeneration(info.info.name, DELETES_EXTENSION, info.DelGen));
            }
        }
    }
}
