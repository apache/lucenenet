using System;

namespace Lucene.Net.Util.Mutable
{
    public class MutableValueFloat : MutableValue
    {
        public Single Value { get; set; }

        public override void Copy(MutableValue source)
        {
            var s = source as MutableValueFloat;
            Value = s.Value;
            Exists = s.Exists;
        }

        public override MutableValue Duplicate()
        {
            return new MutableValueFloat { Value = Value, Exists = Exists };
        }

        public override Boolean EqualsSameType(object other)
        {
            var b = other as MutableValueFloat;
            return Value == b.Value && Exists == b.Exists;
        }

        public override Int32 CompareSameType(object other)
        {
            var b = other as MutableValueFloat;
            var c = Value.CompareTo(b.Value);
            if (c != 0) return c;
            if (Exists == b.Exists) return 0;
            return Exists ? 1 : -1;
        }

        public override Object ToObject()
        {
            return Exists ? Value as Object : null;
        }

        public override Int32 HashCode()
        {
            return (Int32)BitConverter.DoubleToInt64Bits(Value);
        }
    }
}
