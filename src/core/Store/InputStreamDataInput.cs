using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    public class InputStreamDataInput : DataInput, IDisposable
    {
        private Stream inputstream;
        private bool disposed = false;

        public InputStreamDataInput(Stream inputstream)
        {
            this.inputstream = inputstream;
        }

        public override byte ReadByte()
        {
            int v = inputstream.ReadByte();
            if (v == -1) throw new EndOfStreamException();
            return (byte)v;
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            while (len > 0)
            {
                int cnt = inputstream.Read(b, offset, len);
                if (cnt < 0)
                {
                    // Partially read the input, but no more data available in the stream.
                    throw new EndOfStreamException();
                }
                len -= cnt;
                offset += cnt;
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    inputstream.Dispose();
                }

                inputstream = null;
                disposed = true;
            }
        }
    }
}
