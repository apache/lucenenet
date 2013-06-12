using System;

namespace Lucene.Net.Util.Mutable
{
    public class MutableValueBool : MutableValue
    {
        public Boolean Value { get; set; }

        public override Object ToObject()
        {
            return Exists ? Value : null;
        }

        public override void Copy(MutableValue source)
        {
            if (!(source is MutableValueBool)) throw new ArgumentException("source must be of type MutableValueBool");
            var s = source as MutableValueBool;
            Value = s.Value;
            Exists = s.Exists;
        }

        public override MutableValue Duplicate()
        {
            return new MutableValueBool {Value = Value, Exists = Exists};
        }

        public override bool EqualsSameType(object other)
        {
            if (!(other is MutableValueBool)) throw new ArgumentException("source must be of type MutableValueBool");
            var b = other as MutableValueBool;
            return Value = b.Value && Exists == b.Exists;
        }

        public override int CompareSameType(object other)
        {
            if (!(other is MutableValueBool)) throw new ArgumentException("source must be of type MutableValueBool");
            var b = other as MutableValueBool;
            if (Value != b.Value) return Value ? 1 : 0;
            return Exists ? 1 : -1;
        }

        public override int HashCode()
        {
            return Value ? 2 : (Exists ? 1 : 0);
        }
    }
}
