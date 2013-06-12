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


        public Int32 CompareTo(MutableValue other)
        {
            var c1 = GetType();
            var c2 = other.GetType();

            if (c1 != c2)
            {
                var c = c1.GetHashCode() - c2.GetHashCode();
                if (c == 0)
                {
                    c = String.Compare(c1.Name, c2.Name, StringComparison.Ordinal);
                }
                return c;
            }
            return CompareSameType(other);
        }

        public override Boolean Equals(Object other)
        {
            return (GetType() == other.GetType() && EqualsSameType(other));
        }

        public abstract Int32 HashCode();

        public override String ToString()
        {
            return Exists ? ToObject().ToString() : "(null)";
        }
    }
}
