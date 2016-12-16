using Lucene.Net.Util;
using System;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Analysis.Tokenattributes
{
    // LUCENENET TODO: This class does not exist in Java - Remove?
    public class TermToBytesRefAttribute : Attribute, ITermToBytesRefAttribute
    {
        private BytesRef Bytes;

        public void FillBytesRef()
        {
            throw new NotImplementedException("I'm not sure what this should do");
        }

        public BytesRef BytesRef { get; set; }

        public override void Clear()
        {
        }

        public override void CopyTo(Attribute target)
        {
            TermToBytesRefAttribute other = (TermToBytesRefAttribute)target;
            other.Bytes = Bytes;
        }
    }
}