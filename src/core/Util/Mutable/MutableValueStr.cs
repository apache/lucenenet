using System;

namespace Lucene.Net.Util.Mutable
{
    public class MutableValueStr : MutableValue
    {
        public BytesRef Value { get; set; }

        public MutableValueStr()
        {
            Value = new BytesRef();
        }

        public override void Copy(MutableValue source)
        {
            var s = source as MutableValueStr;
            Exists = s.Exists;
            Value = s.Value;
        }

        public override MutableValue Duplicate()
        {
            return new MutableValueStr { Value = Value, Exists = Exists };
        }

        public override Boolean EqualsSameType(object other)
        {
            var b = other as MutableValueStr;
            return Value.Equals(b.Value) && Exists == b.Exists;
        }

        public override Int32 CompareSameType(object other)
        {
            var b = other as MutableValueStr;
            var c = Value.CompareTo(b.Value);
            if (c != 0) return c;
            if (Exists == b.Exists) return 0;
            return Exists ? 1 : -1;
        }

        public override Object ToObject()
        {
            return Exists ? Value.Utf8ToString() : null;
        }

        public override Int32 HashCode()
        {
            return Value.GetHashCode();
        }
    }
}
