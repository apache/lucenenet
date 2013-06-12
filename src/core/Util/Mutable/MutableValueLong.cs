using System;

namespace Lucene.Net.Util.Mutable
{
    public class MutableValueLong : MutableValue
    {
        public Int64 Value { get; set; }

        public override void Copy(MutableValue source)
        {
            var s = source as MutableValueLong;
            Value = s.Value;
            Exists = s.Exists;
        }

        public override MutableValue Duplicate()
        {
            return new MutableValueLong { Value = Value, Exists = Exists };
        }

        public override Boolean EqualsSameType(object other)
        {
            var b = other as MutableValueLong;
            return Value == b.Value && Exists == b.Exists;
        }

        public override Int32 CompareSameType(object other)
        {
            var b = other as MutableValueLong;
            var bv = b.Value;
            if (Value < bv) return -1;
            if (Value > bv) return 1;
            if (Exists == b.Exists) return 0;
            return Exists ? 1 : -1;
        }

        public override Object ToObject()
        {
            return Exists ? (Object)Value : null;
        }

        public override Int32 HashCode()
        {
            return (Int32)Value + (Int32)(Value >> 32);
        }
    }
}
