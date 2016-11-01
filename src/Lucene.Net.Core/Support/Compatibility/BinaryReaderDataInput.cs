using Lucene.Net.Store;
using System;
using System.IO;

namespace Lucene.Net.Support.Compatibility
{
    public class BinaryReaderDataInput : DataInput, IDisposable
    {
        private readonly BinaryReader br;
        public BinaryReaderDataInput(BinaryReader br)
        {
            this.br = br;
        }
       
        public override byte ReadByte()
        {
            return br.ReadByte();
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            byte[] temp = br.ReadBytes(len);
            for (int i = offset; i < (offset + len) && i < temp.Length; i++)
            {
                b[i] = temp[i];
            }
        }

        public void Dispose()
        {
            br.Dispose();
        }
    }
}
