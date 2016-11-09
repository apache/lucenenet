using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Java's HashSet is unlike .NET's in that its equals() and hashcode() methods 
    /// are setup to compare the values of the sets, where in .NET we only check that
    /// the references are the same. <see cref="ValueHashSet{T}"/> acts more like the
    /// HashSet type in Java by comparing the sets for value equality.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueHashSet<T> : HashSet<T>
    {
        public ValueHashSet()
            : base()
        { }

        public ValueHashSet(IEnumerable<T> collection)
            : base(collection)
        { }

        public ValueHashSet(IEqualityComparer<T> comparer)
            : base(comparer)
        { }

#if FEATURE_SERIALIZABLE
        public ValueHashSet(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
#endif

        public ValueHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
            : base(collection, comparer)
        { }


        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is ISet<T>))
                return false;
            ICollection<T> c = obj as ICollection<T>;
            if (c.Count != Count)
                return false;

            // Check to ensure the sets values are the same
            return SetEquals(c);
        }

        public override int GetHashCode()
        {
            int h = 0;
            var i = GetEnumerator();
            while (i.MoveNext())
            {
                T obj = i.Current;
                if (!EqualityComparer<T>.Default.Equals(obj, default(T)))
                {
                    h += obj.GetHashCode();
                }
            }
            return h;
        }
    }
}
