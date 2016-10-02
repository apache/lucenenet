using System.Threading;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Mimics Java's AtomicReferenceArray class (partial implementation)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AtomicReferenceArray<T> where T : class
    {
        private T[] _array;

        public AtomicReferenceArray(int length)
        {
            _array = new T[length];
        }

        public int Length
        {
            get { return _array.Length; }
        }

        public T this[int index]
        {
            get
            {
                return Volatile.Read(ref _array[index]);
            }
            set
            {
                Volatile.Write(ref _array[index], value);
            }
        }
    }
}
