using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Support;

namespace Lucene.Net.Store
{
    public sealed class RateLimitedDirectoryWrapper : Directory
    {
        private readonly Directory del;

        private IDictionary<IOContext.Context, RateLimiter> contextRateLimiters = new ConcurrentHashMap<IOContext.Context, RateLimiter>();

        public RateLimitedDirectoryWrapper(Directory wrapped)
        {
            this.del = wrapped;
        }

        public Directory Delegate
        {
            get { return del; }
        }

        public override string[] ListAll()
        {
            EnsureOpen();
            return del.ListAll();
        }

        public override bool FileExists(string name)
        {
            EnsureOpen();
            return del.FileExists(name);
        }

        public override void DeleteFile(string name)
        {
            EnsureOpen();
            del.DeleteFile(name);
        }

        public override long FileLength(string name)
        {
            EnsureOpen();
            return del.FileLength(name);
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();
            IndexOutput output = del.CreateOutput(name, context);
            RateLimiter limiter = GetRateLimiter(context.context);
            if (limiter != null)
            {
                return new RateLimitedIndexOutput(limiter, output);
            }
            return output;
        }

        public override void Sync(ICollection<string> names)
        {
            EnsureOpen();
            del.Sync(names);
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            EnsureOpen();
            return del.OpenInput(name, context);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                del.Dispose();
            }

            isOpen = false;
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            return del.CreateSlicer(name, context);
        }

        public override Lock MakeLock(string name)
        {
            EnsureOpen();
            return del.MakeLock(name);
        }

        public override void ClearLock(string name)
        {
            EnsureOpen();
            del.ClearLock(name);
        }

        public override LockFactory LockFactory
        {
            get
            {
                EnsureOpen();
                return del.LockFactory;
            }
            set
            {
                EnsureOpen();
                del.LockFactory = value;
            }
        }

        public override string LockId
        {
            get
            {
                EnsureOpen();
                return del.LockId;
            }
        }

        public override string ToString()
        {
            return "RateLimitedDirectoryWrapper(" + del.ToString() + ")";
        }

        public override void Copy(Directory to, string src, string dest, IOContext context)
        {
            EnsureOpen();
            del.Copy(to, src, dest, context);
        }

        private RateLimiter GetRateLimiter(IOContext.Context context)
        {
            //assert context != null;
            return contextRateLimiters[context];
        }

        public void SetMaxWriteMBPerSec(double mbPerSec, IOContext.Context context)
        {
            EnsureOpen();
            if (context == null)
            {
                throw new ArgumentException("Context must not be null");
            }
            RateLimiter limiter = contextRateLimiters[context];
            if (mbPerSec == null)
            {
                if (limiter != null)
                {
                    limiter.MbPerSec = double.MaxValue;
                    contextRateLimiters[context] = null;
                }
            }
            else if (limiter != null)
            {
                limiter.MbPerSec = mbPerSec;
                contextRateLimiters[context] = limiter; // cross the mem barrier again
            }
            else
            {
                contextRateLimiters[context] = new RateLimiter.SimpleRateLimiter(mbPerSec);
            }
        }

        public void SetRateLimiter(RateLimiter mergeWriteRateLimiter, IOContext.Context context)
        {
            EnsureOpen();

            contextRateLimiters[context] = mergeWriteRateLimiter;
        }

        public double? GetMaxWriteMBPerSec(IOContext.Context context)
        {
            EnsureOpen();

            RateLimiter limiter = GetRateLimiter(context);
            return limiter == null ? (double?)null : limiter.MbPerSec;
        }
    }
}
