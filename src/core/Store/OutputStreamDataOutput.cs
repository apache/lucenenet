using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    public class OutputStreamDataOutput : DataOutput, IDisposable
    {
        private Stream os;
        private bool isDisposed;

        public OutputStreamDataOutput(Stream os)
        {
            this.os = os;
        }

        public override void WriteByte(byte b)
        {
            os.WriteByte(b);
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            os.Write(b, offset, length);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    os.Dispose();
                }

                os = null;
            }
        }
    }
}
