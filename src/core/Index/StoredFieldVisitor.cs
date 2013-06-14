using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class StoredFieldVisitor
    {
        protected StoredFieldVisitor()
        {
        }

        public virtual void BinaryField(FieldInfo fieldInfo, sbyte[] value)
        {
        }

        public virtual void StringField(FieldInfo fieldInfo, string value)
        {
        }

        public virtual void IntField(FieldInfo fieldInfo, int value)
        {
        }

        public virtual void LongField(FieldInfo fieldInfo, long value)
        {
        }

        public virtual void FloatField(FieldInfo fieldInfo, float value)
        {
        }

        public virtual void DoubleField(FieldInfo fieldInfo, double value)
        {
        }

        public abstract Status NeedsField(FieldInfo fieldInfo);

        public enum Status
        {
            YES,
            NO,
            STOP
        }
    }
}
