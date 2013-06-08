using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Util
{
    public abstract class Counter
    {
        public abstract long AddAndGet(long delta);

        public abstract long Get();

        public static Counter NewCounter()
        {
            return NewCounter(false);
        }

        private static Counter NewCounter(bool threadSafe)
        {
            return threadSafe ? new AtomicCounter() : new SerialCounter();
        }


        private sealed class SerialCounter : Counter
        {
            private long count = 0;

            public override long AddAndGet(long delta)
            {
                return count += delta;
            }

            public override long Get()
            {
                return count;
            }
        }

        private sealed class AtomicCounter : Counter
        {
            private long count = 0L;

            public override long AddAndGet(long delta)
            {
                return Interlocked.Add(ref count, delta);
            }

            public override long Get()
            {
                return count;
            }
        }
    }
}
