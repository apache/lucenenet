using System;

namespace Lucene.Net.Util.Mutable
{
    public class MutableValueDate : MutableValueLong
    {
        public override Object ToObject()
        {
            return Exists ? new DateTime(Value) as Object : null;
        }

        public override MutableValue Duplicate()
        {
            return new MutableValueDate { Value = Value, Exists = Exists };
        }
    }
}
