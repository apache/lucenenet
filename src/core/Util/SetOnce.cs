using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Util
{
    public sealed class SetOnce<T>
    {
        public sealed class AlreadySetException : InvalidOperationException
        {
            public AlreadySetException()
                : base("The object cannot be set twice!")
            {
            }
        }

        private volatile T obj = default(T);
        private int set = 0; // using int instead of bool for Interlocked.CompareExchange compatibility

        public SetOnce()
        {
        }

        public SetOnce(T obj)
        {
            this.obj = obj;
            set = 1;
        }

        public void Set(T obj)
        {
            if (Interlocked.CompareExchange(ref set, 1, 0) == 0)
            {
                this.obj = obj;
            }
            else
            {
                throw new AlreadySetException();
            }
        }

        public T Get()
        {
            return obj;
        }
    }
}
