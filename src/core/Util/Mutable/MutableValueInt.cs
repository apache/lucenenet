using System;

namespace Lucene.Net.Util.Mutable
{
    public class MutableValueInt : MutableValue
    {
        public Int32 Value { get; set; }

        public override void Copy(MutableValue source)
        {
            var s = source as MutableValueInt;
            Value = s.Value;
            Exists = s.Exists;
        }

        public override MutableValue Duplicate()
        {
            return new MutableValueInt { Value = Value, Exists = Exists };
        }

        public override Boolean EqualsSameType(object other)
        {
            var b = other as MutableValueInt;
            return Value == b.Value && Exists == b.Exists;
        }

        public override Int32 CompareSameType(object other)
        {
            var b = other as MutableValueInt;
            var ai = Value;
            var bi = b.Value;
            if (ai < bi) return -1;
            if (ai > bi) return 1;

            if (Exists == b.Exists) return 0;
            return Exists ? 1 : -1;
        }

        public override Object ToObject()
        {
            return Exists ? Value as Object : null;
        }

        public override Int32 HashCode()
        {
            return (Value >> 8) + (Value >> 16);
        }
    }
}
