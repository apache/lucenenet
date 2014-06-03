using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lucene.Net.Support
{
    public class IdentityWeakReference<T> : WeakReference
        where T : class
    {
        private readonly int hash;
        private static readonly object NULL = new object();

        public IdentityWeakReference(T target)
            : base(target == null ? NULL : target)
        {
            hash = RuntimeHelpers.GetHashCode(target);
        }

        public override int GetHashCode()
        {
            return hash;
        }

        public override bool Equals(object o)
        {
            if (ReferenceEquals(this, o))
            {
                return true;
            }
            if (o is IdentityWeakReference<T>)
            {
                IdentityWeakReference<T> iwr = (IdentityWeakReference<T>)o;
                if (ReferenceEquals(this.Target, iwr.Target))
                {
                    return true;
                }
            }
            return false;
        }

        public new T Target
        {
            get
            {
                // note: if this.NULL is the target, it will not cast to T, so the "as" will return null as we would expect.
                return base.Target as T;
            }
            set
            {
                base.Target = value;
            }
        }
    }
}