using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Codecs.Facet42
{
    internal class Facet42BinaryDocValues : BinaryDocValues
    {
        private readonly sbyte[] bytes;
        private readonly PackedInts.IReader addresses;

        internal Facet42BinaryDocValues(DataInput in_renamed)
        {
            int totBytes = in_renamed.ReadVInt();
            bytes = new sbyte[totBytes];
            in_renamed.ReadBytes(bytes, 0, totBytes);
            addresses = PackedInts.GetReader(in_renamed);
        }

        public override void Get(int docID, BytesRef ret)
        {
            int start = (int)addresses.Get(docID);
            ret.bytes = bytes;
            ret.offset = start;
            ret.length = (int)(addresses.Get(docID + 1) - start);
        }
    }
}
