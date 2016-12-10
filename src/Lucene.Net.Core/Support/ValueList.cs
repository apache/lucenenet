using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Java's ArrayList is unlike .NET's <see cref="List{T}"/> in that its equals() and hashcode() methods 
    /// are setup to compare the values of the sets, where in .NET we only check that
    /// the references are the same. <see cref="ValueList{T}"/> acts more like the
    /// HashSet type in Java by comparing the sets for value equality.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="ValueHashSet{T}"/>
    public class ValueList<T> : List<T>
    {
        public ValueList()
            : base()
        { }

        public ValueList(int capacity)
            : base(capacity)
        { }

        public ValueList(IEnumerable<T> collection)
            : base(collection)
        { }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is List<T>))
                return false;
            ICollection<T> c = obj as ICollection<T>;
            if (c.Count != Count)
                return false;

            // Check to ensure the sets values are the same
            return this.SequenceEqual(c);
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
                    h = HashHelpers.CombineHashCodes(h, obj.GetHashCode());
                }
            }
            return h;
        }
    }
}
