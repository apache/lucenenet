using System.Threading;

namespace Lucene.Net.Support
{
    public class AtomicLong
    {
        private long value;

        public AtomicLong()
            : this(0)
        {
        }

        public AtomicLong(long value)
        {
            Interlocked.Exchange(ref this.value, value);
        }

        public long IncrementAndGet()
        {
            return Interlocked.Increment(ref value);
        }

        public long DecrementAndGet()
        {
            return Interlocked.Decrement(ref value);
        }

        public void Set(int value)
        {
            Interlocked.Exchange(ref this.value, value);
        }

        public long AddAndGet(long value)
        {
            return Interlocked.Add(ref this.value, value);
        }

        public long Get()
        {
            //LUCENE TO-DO read operations atomic in 64 bit
            return value;
        }

        public bool CompareAndSet(long expect, long update)
        {
            long rc = Interlocked.CompareExchange(ref value, update, expect);
            return rc == expect;
        }
    }
}