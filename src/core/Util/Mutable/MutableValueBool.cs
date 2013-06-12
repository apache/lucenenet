using System;

namespace Lucene.Net.Util.Mutable
{
    public class MutableValueBool : MutableValue
    {
        public Boolean Value { get; set; }

        public override Object ToObject()
        {
            return Exists ? (Object)Value : null;
        }

        public override void Copy(MutableValue source)
        {
            var s = source as MutableValueBool;
            Value = s.Value;
            Exists = s.Exists;
        }

        public override MutableValue Duplicate()
        {
            return new MutableValueBool {Value = Value, Exists = Exists};
        }

        public override Boolean EqualsSameType(object other)
        {
            var b = other as MutableValueBool;
            return Value = b.Value && Exists == b.Exists;
        }

        public override Int32 CompareSameType(object other)
        {
            var b = other as MutableValueBool;
            if (Value != b.Value) return Value ? 1 : 0;
            return Exists ? 1 : -1;
        }

        public override Int32 HashCode()
        {
            return Value ? 2 : (Exists ? 1 : 0);
        }
    }
}
