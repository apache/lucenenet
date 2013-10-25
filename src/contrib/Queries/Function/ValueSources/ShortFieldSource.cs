using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class ShortFieldSource : FieldCacheSource
    {
        readonly FieldCache.IShortParser parser;

        public ShortFieldSource(string field)
            : this(field, null)
        {
        }

        public ShortFieldSource(string field, FieldCache.IShortParser parser)
            : base(field)
        {
            this.parser = parser;
        }

        public override string Description
        {
            get
            {
                return @"short(" + field + ')';
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FieldCache.Shorts arr = cache.GetShorts(readerContext.AtomicReader, field, parser, false);
            return new AnonymousFunctionValues(this, arr);
        }

        private sealed class AnonymousFunctionValues : FunctionValues
        {
            public AnonymousFunctionValues(ShortFieldSource parent, FieldCache.Shorts arr)
            {
                this.parent = parent;
                this.arr = arr;
            }

            private readonly ShortFieldSource parent;
            private readonly FieldCache.Shorts arr;

            public override byte ByteVal(int doc)
            {
                return (byte)arr.Get(doc);
            }

            public override short ShortVal(int doc)
            {
                return arr.Get(doc);
            }

            public override float FloatVal(int doc)
            {
                return (float)arr.Get(doc);
            }

            public override int IntVal(int doc)
            {
                return (int)arr.Get(doc);
            }

            public override long LongVal(int doc)
            {
                return (long)arr.Get(doc);
            }

            public override double DoubleVal(int doc)
            {
                return (double)arr.Get(doc);
            }

            public override string StrVal(int doc)
            {
                return arr.Get(doc).ToString();
            }

            public override string ToString(int doc)
            {
                return parent.Description + '=' + ShortVal(doc);
            }
        }

        public override bool Equals(Object o)
        {
            if (o.GetType() != typeof(ShortFieldSource))
                return false;
            ShortFieldSource other = (ShortFieldSource)o;
            return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = parser == null ? typeof(short).GetHashCode() : parser.GetType().GetHashCode();
            h = base.GetHashCode();
            return h;
        }
    }
}
