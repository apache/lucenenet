using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Facet.Taxonomy
{
    public abstract class TaxonomyReader : IDisposable
    {
        public class ChildrenIterator
        {
            private readonly int[] siblings;
            private int child;

            internal ChildrenIterator(int child, int[] siblings)
            {
                this.siblings = siblings;
                this.child = child;
            }

            public virtual int Next()
            {
                int res = child;
                if (child != TaxonomyReader.INVALID_ORDINAL)
                {
                    child = siblings[child];
                }

                return res;
            }
        }

        public const int ROOT_ORDINAL = 0;
        public const int INVALID_ORDINAL = -1;

        public static T OpenIfChanged<T>(T oldTaxoReader)
            where T : TaxonomyReader
        {
            T newTaxoReader = (T)oldTaxoReader.DoOpenIfChanged();
            return newTaxoReader;
        }

        private volatile bool closed = false;

        private int refCount = 1;

        protected abstract void DoClose();

        protected abstract TaxonomyReader DoOpenIfChanged();

        protected void EnsureOpen()
        {
            if (RefCount <= 0)
            {
                throw new ObjectDisposedException(@"this TaxonomyReader is closed");
            }
        }

        public void Dispose()
        {
            if (!closed)
            {
                lock (this)
                {
                    if (!closed)
                    {
                        DecRef();
                        closed = true;
                    }
                }
            }
        }

        public void DecRef()
        {
            EnsureOpen();
            int rc = Interlocked.Decrement(ref refCount);
            if (rc == 0)
            {
                bool success = false;
                try
                {
                    DoClose();
                    closed = true;
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        Interlocked.Increment(ref refCount);
                    }
                }
            }
            else if (rc < 0)
            {
                throw new InvalidOperationException(@"too many decRef calls: refCount is " + rc + @" after decrement");
            }
        }

        public abstract ParallelTaxonomyArrays GetParallelTaxonomyArrays();

        public virtual ChildrenIterator GetChildren(int ordinal)
        {
            ParallelTaxonomyArrays arrays = GetParallelTaxonomyArrays();
            int child = ordinal >= 0 ? arrays.Children[ordinal] : INVALID_ORDINAL;
            return new ChildrenIterator(child, arrays.Siblings);
        }

        public abstract IDictionary<String, String> GetCommitUserData();

        public abstract int GetOrdinal(CategoryPath categoryPath);

        public abstract CategoryPath GetPath(int ordinal);

        public int RefCount
        {
            get
            {
                return refCount;
            }
        }

        public abstract int Size { get; }

        public void IncRef()
        {
            EnsureOpen();
            Interlocked.Increment(ref refCount);
        }

        public bool TryIncRef()
        {
            int count;
            while ((count = refCount) > 0)
            {
                if (Interlocked.CompareExchange(ref refCount, count + 1, count) == count)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
