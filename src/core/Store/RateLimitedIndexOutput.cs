using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    internal sealed class RateLimitedIndexOutput : BufferedIndexOutput
    {
        private readonly IndexOutput del;
        private readonly BufferedIndexOutput bufferedDelegate;
        private readonly RateLimiter rateLimiter;

        internal RateLimitedIndexOutput(RateLimiter rateLimiter, IndexOutput del)
        {
            if (del is BufferedIndexOutput)
            {
                bufferedDelegate = (BufferedIndexOutput)del;
                this.del = del;
            }
            else
            {
                this.del = del;
                bufferedDelegate = null;
            }

            this.rateLimiter = rateLimiter;
        }

        public override void FlushBuffer(byte[] b, int offset, int len)
        {
            rateLimiter.Pause(len);
            if (bufferedDelegate != null)
            {
                bufferedDelegate.FlushBuffer(b, offset, len);
            }
            else
            {
                del.WriteBytes(b, offset, len);
            }
        }

        public override long Length
        {
            get { return del.Length; }
        }

        public override void Seek(long pos)
        {
            Flush();
            del.Seek(pos);
        }

        public override void Flush()
        {
            try
            {
                base.Flush();
            }
            finally
            {
                del.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                del.Dispose();
            }
        }
    }
}
