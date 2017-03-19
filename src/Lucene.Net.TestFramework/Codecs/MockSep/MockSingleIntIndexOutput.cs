using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.MockSep
{
    /// <summary>
    /// Writes ints directly to the file (not in blocks) as
    /// vInt.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class MockSingleIntIndexOutput : Int32IndexOutput
    {
        private readonly IndexOutput @out;
        internal const string CODEC = "SINGLE_INTS";
        internal const int VERSION_START = 0;
        internal const int VERSION_CURRENT = VERSION_START;

        public MockSingleIntIndexOutput(Directory dir, string fileName, IOContext context)
        {
            @out = dir.CreateOutput(fileName, context);
            bool success = false;
            try
            {
                CodecUtil.WriteHeader(@out, CODEC, VERSION_CURRENT);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(@out);
                }
            }
        }

        public override void Write(int v)
        {
            @out.WriteVInt32(v);
        }

        public override AbstractIndex GetIndex()
        {
            return new MockSingleIntIndexOutputIndex(this);
        }

        public override void Dispose()
        {
            @out.Dispose();
        }

        public override string ToString()
        {
            return "MockSingleIntIndexOutput fp=" + @out.GetFilePointer();
        }

        private class MockSingleIntIndexOutputIndex : AbstractIndex
        {
            internal long fp;
            internal long lastFP;
            private readonly MockSingleIntIndexOutput outerClass;

            public MockSingleIntIndexOutputIndex(MockSingleIntIndexOutput outerClass)
            {
                this.outerClass = outerClass;
            }

            public override void Mark()
            {
                fp = outerClass.@out.GetFilePointer();
            }

            public override void CopyFrom(AbstractIndex other, bool copyLast)
            {
                fp = ((MockSingleIntIndexOutputIndex)other).fp;
                if (copyLast)
                {
                    lastFP = ((MockSingleIntIndexOutputIndex)other).fp;
                }
            }

            public override void Write(DataOutput indexOut, bool absolute)
            {
                if (absolute)
                {
                    indexOut.WriteVInt64(fp);
                }
                else
                {
                    indexOut.WriteVInt64(fp - lastFP);
                }
                lastFP = fp;
            }

            public override string ToString()
            {
                return fp.ToString();
            }
        }
    }
}
