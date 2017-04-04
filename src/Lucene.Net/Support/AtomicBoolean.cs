using System;
using System.Threading;

namespace Lucene.Net.Support
{
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class AtomicBoolean
    {
        private int value = 0;

        public AtomicBoolean()
            : this(false)
        {
        }

        public AtomicBoolean(bool initialValue)
        {
            value = initialValue ? 1 : 0;
        }

        public bool Get()
        {
            return value == 1 ? true : false;
        }

        public bool CompareAndSet(bool expect, bool update)
        {
            int e = expect ? 1 : 0;
            int u = update ? 1 : 0;

            int original = Interlocked.CompareExchange(ref value, u, e);

            return original == e;
        }

        public void Set(bool newValue)
        {
            Interlocked.Exchange(ref value, newValue ? 1 : 0);
        }

        public bool GetAndSet(bool newValue)
        {
            return Interlocked.Exchange(ref value, newValue ? 1 : 0) == 1;
        }

        public override string ToString()
        {
            return value == 1 ? bool.TrueString : bool.FalseString;
        }
    }
}