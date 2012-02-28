using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Lucene.Net.Util
{
    /// <summary>
    /// A class that mimics Java's IdentityHashMap in that it determines
    /// object equality solely on ReferenceEquals rather than (possibly overloaded)
    /// object.Equals().
    /// 
    /// NOTE: Java's documentation on IdentityHashMap says that it also uses
    ///       ReferenceEquals on it's Values as well.  This class does not follow this behavior
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
    public class IdentityDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public IdentityDictionary(IDictionary<TKey, TValue> other) : base(other, new IdentityComparer())
        { }

        public IdentityDictionary(int capacity) : base(capacity, new IdentityComparer())
        { }

        public IdentityDictionary() : this(16)
        { }

        class IdentityComparer : IEqualityComparer<TKey>
        {
            public bool Equals(TKey x, TKey y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(TKey obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
