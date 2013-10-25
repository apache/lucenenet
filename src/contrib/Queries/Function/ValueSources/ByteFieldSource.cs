using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class ByteFieldSource : FieldCacheSource
    {
        private readonly FieldCache.IByteParser parser;

        public ByteFieldSource(String field)
            : this(field, null)
        {
        }

        public ByteFieldSource(String field, FieldCache.IByteParser parser)
            : base(field)
        {
            this.parser = parser;
        }

        public override string Description
        {
            get
            {
                return "byte(" + field + ')';
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, Index.AtomicReaderContext readerContext)
        {
            FieldCache.Bytes arr = cache.GetBytes(readerContext.AtomicReader, field, parser, false);

            return new AnonymousFunctionValues(this, arr);
        }

        private sealed class AnonymousFunctionValues : FunctionValues
        {
            private readonly FieldCache.Bytes arr;
            private ByteFieldSource parent;

            public AnonymousFunctionValues(ByteFieldSource parent, FieldCache.Bytes arr)
            {
                this.parent = parent;
                this.arr = arr;
            }

            public override byte ByteVal(int doc)
            {
                return (byte)arr.Get(doc);
            }

            public override short ShortVal(int doc)
            {
                return (short)arr.Get(doc);
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
                return parent.Description + '=' + ByteVal(doc);
            }

            public override object ObjectVal(int doc)
            {
                return arr.Get(doc);  // TODO: valid?
            }
        }

        public override bool Equals(object o)
        {
            if (o.GetType() != typeof(ByteFieldSource)) return false;
            ByteFieldSource other = (ByteFieldSource)o;
            return base.Equals(other)
              && (this.parser == null ? other.parser == null :
                  this.parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = parser == null ? typeof(Byte).GetHashCode() : parser.GetType().GetHashCode();
            h += base.GetHashCode();
            return h;
        }
    }
}
