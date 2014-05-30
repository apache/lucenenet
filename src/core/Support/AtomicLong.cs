using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Support
{
    class AtomicLong
    {
        private long value;
        public AtomicLong()
            : this(0)
        {
        }

        public AtomicLong(long value_)
        {
            Interlocked.Exchange(ref value, value_);
        }

        public long IncrementAndGet()
        {
            return Interlocked.Increment(ref value);
        }

        public long DecrementAndGet()
        {
            return Interlocked.Decrement(ref value);
        }

        public void Set(int value_)
        {
            Interlocked.Exchange(ref value, value_);
        }

        public long Get()
        {
            //LUCENE TO-DO read operations atomic in 64 bit
            return value;
        }

        public void CompareAndSet(int expect, int update)
        {
            Interlocked.CompareExchange(ref value, expect, update);
        }
    }
}
