using System;

namespace Lucene.Net.Util.Mutable
{
    public abstract class MutableValue : IComparable<MutableValue>
    {
        public Boolean Exists { get; set; }

        public abstract void Copy(MutableValue source);
        public abstract MutableValue Duplicate();
        public abstract Boolean EqualsSameType(Object other);
        public abstract Int32 CompareSameType(Object other);
        public abstract Object ToObject();


        public int CompareTo(MutableValue other)
        {
            var c1 = this.GetType();
            var c2 = other.GetType();

            if (c1 != c2)
            {
                var c = c1.GetHashCode() - c2.GetHashCode();
                if (c == 0)
                {
                    c = c1.Name.CompareTo(c1.Name);
                }
                return c;
            }
            return CompareSameType(other);
        }

        public Boolean Equals(Object other)
        {
            return (this.GetType() == other.GetType() && this.EqualsSameType(other));
        }

        public abstract int HashCode();

        public override string ToString()
        {
            return Exists ? ToObject().ToString() : "(null)";
        }
    }
}
