using System;
using System.IO;
using Lucene.Net.Store;

namespace Lucene.Net.Support.Compatibility
{
    public class BinaryWriterDataOutput : DataOutput, IDisposable
    {
        private readonly BinaryWriter bw;

        public BinaryWriterDataOutput(BinaryWriter bw)
        {
            this.bw = bw;
        }

        public override void WriteByte(byte b)
        {
            bw.Write(b);
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            bw.Write(b, offset, length);
        }

        public void Dispose()
        {
            bw.Dispose();
        }
    }
}
