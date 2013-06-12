using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Mutable
{
    public class MutableValueDouble : MutableValue
    {
        public Double Value { get; set; }

        public override void Copy(MutableValue source)
        {
            var s = source as MutableValueDouble;
            Value = s.Value;
            Exists = s.Exists;
        }

        public override MutableValue Duplicate()
        {
            return new MutableValueDouble { Value = Value, Exists = Exists };
        }

        public override bool EqualsSameType(object other)
        {
            var b = other as MutableValueDouble;
            return Value == b.Value && Exists == b.Exists;
        }

        public override int CompareSameType(object other)
        {
            var b = other as MutableValueDouble;
            var c = Value.CompareTo(b.Value);
            if (c != 0) return c;
            if (!Exists) return -1;
            if (!b.Exists) return 1;
            return 0;
        }

        public override object ToObject()
        {
            return Exists ? Value as Object : null;
        }

        public override int HashCode()
        {
            var x = BitConverter.DoubleToInt64Bits(Value);
            return (int)x + (int)Support.Number.URShift(x, 32);
        }
    }
}
