using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
     public class FloatFieldSource : FieldCacheSource
    {
        protected readonly FieldCache.IFloatParser parser;

        public FloatFieldSource(string field)
            : this (field, null)
        {
        }

        public FloatFieldSource(string field, FieldCache.IFloatParser parser)
            : base (field)
        {
            this.parser = parser;
        }

        public override string Description
        {
            get
            {
                return "float(" + field + ')';
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FieldCache.Floats arr = cache.GetFloats(readerContext.AtomicReader, field, parser, true);
            IBits valid = cache.GetDocsWithField(readerContext.AtomicReader, field);
            return new AnonymousFloatDocValues(this, arr, valid);
        }

        private sealed class AnonymousValueFiller : FunctionValues.ValueFiller
        {
            public AnonymousValueFiller(FloatFieldSource parent, FieldCache.Floats arr, IBits valid)
            {
                this.parent = parent;
                this.arr = arr;
                this.valid = valid;
            }

            private readonly FloatFieldSource parent;
            private readonly FieldCache.Floats arr;
            private readonly IBits valid;

            private readonly MutableValueFloat mval = new MutableValueFloat();
            
            public override MutableValue Value
            {
                get
                {
                    return mval;
                }
            }

            public override void FillValue(int doc)
            {
                mval.Value = arr.Get(doc);
                mval.Exists = valid[doc];
            }
        }

        private sealed class AnonymousFloatDocValues : FloatDocValues
        {
            public AnonymousFloatDocValues(FloatFieldSource parent, FieldCache.Floats arr, IBits valid)
                : base(parent)
            {
                this.parent = parent;
                this.arr = arr;
                this.valid = valid;
            }

            private readonly FloatFieldSource parent;

            private readonly FieldCache.Floats arr;
            private readonly IBits valid;

            public override float FloatVal(int doc)
            {
                return arr.Get(doc);
            }

            public override object ObjectVal(int doc)
            {
                return valid[doc] ? (object)arr.Get(doc) : null;
            }

            public override bool Exists(int doc)
            {
                return valid[doc];
            }

            public override ValueFiller GetValueFiller()
            {
                return new AnonymousValueFiller(parent, arr, valid);
            }
        }

        public override bool Equals(Object o)
        {
            if (o.GetType() != typeof (FloatFieldSource))
                return false;
            FloatFieldSource other = (FloatFieldSource) o;
            return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = parser == null ? typeof(float).GetHashCode() : parser.GetType().GetHashCode();
            h = base.GetHashCode();
            return h;
        }
    }
}
