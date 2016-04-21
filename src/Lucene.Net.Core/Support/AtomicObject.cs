using System;
using System.Threading;

namespace Lucene.Net.Support
{
    public class AtomicObject<T> where T : class
    {
        private T _value;
        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public AtomicObject() : this(default(T))
        { }

        public AtomicObject(T initial)
        {
            _value = initial;
        }

        public T Value
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _value;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            set
            {
                Interlocked.Exchange(ref _value, value);
            }
        }
    }
}
