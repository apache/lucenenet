//using Lucene.Net.Util;
//using System;
//using Attribute = Lucene.Net.Util.Attribute;

//namespace Lucene.Net.Analysis.TokenAttributes
//{
//    // LUCENENET TODO: This class does not exist in Java - Remove?
//    public class TermToBytesRefAttribute : Attribute, ITermToBytesRefAttribute
//    {
//        private BytesRef bytes;

//        public void FillBytesRef()
//        {
//            throw new NotImplementedException("I'm not sure what this should do");
//        }

//        public BytesRef BytesRef { get; set; }

//        public override void Clear()
//        {
//        }

//        public override void CopyTo(IAttribute target)
//        {
//            TermToBytesRefAttribute other = (TermToBytesRefAttribute)target;
//            other.bytes = bytes;
//        }
//    }
//}