using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
    public class EnumEnumerator<T> : IEnumerator<T>, IEnumerable<T>
    {
        public static EnumEnumerator<T> CreateWithCapturedNext(Func<T> next)
        {
            T current = default(T);
            return new EnumEnumerator<T>(() => { current = next(); return current != null; }, () => current);
        }

        private readonly Func<bool> next;
        private readonly Action dispose;
        private readonly Func<T> currentFactory;

        private bool started = false;

        public EnumEnumerator(Func<bool> next, Func<T> currentFactory, Action dispose = null)
        {
            this.next = next;
            this.dispose = dispose;
            this.currentFactory = currentFactory;
        }

        public T Current => started ? currentFactory() : default(T);

        object IEnumerator.Current => Current;

        public bool MoveNext() => (started = next());

        public void Reset() => throw new NotImplementedException();

        public void Dispose() => dispose?.Invoke();

        public IEnumerator<T> GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}