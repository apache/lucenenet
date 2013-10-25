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
    public class LongFieldSource : FieldCacheSource
    {
        protected readonly FieldCache.ILongParser parser;

        public LongFieldSource(string field)
            : this(field, null)
        {
        }

        public LongFieldSource(string field, FieldCache.ILongParser parser)
            : base(field)
        {
            this.parser = parser;
        }

        public override string Description
        {
            get
            {
                return @"long(" + field + ')';
            }
        }

        public virtual long ExternalToLong(string extVal)
        {
            return long.Parse(extVal);
        }

        public virtual Object LongToObject(long val)
        {
            return val;
        }

        public virtual string LongToString(long val)
        {
            return LongToObject(val).ToString();
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FieldCache.Longs arr = cache.GetLongs(readerContext.AtomicReader, field, parser, true);
            IBits valid = cache.GetDocsWithField(readerContext.AtomicReader, field);
            return new AnonymousLongDocValues(this, arr, valid);
        }

        private sealed class AnonymousValueSourceScorer : ValueSourceScorer
        {
            public AnonymousValueSourceScorer(LongFieldSource parent, IndexReader reader, FunctionValues values, FieldCache.Longs arr, long ll, long uu)
                : base(reader, values)
            {
                this.parent = parent;
                this.arr = arr;
                this.ll = ll;
                this.uu = uu;
            }

            private readonly LongFieldSource parent;
            private readonly FieldCache.Longs arr;
            private readonly long ll, uu;

            public override bool MatchesValue(int doc)
            {
                long val = arr.Get(doc);
                return val >= ll && val <= uu;
            }
        }

        private sealed class AnonymousValueFiller : FunctionValues.ValueFiller
        {
            public AnonymousValueFiller(LongFieldSource parent, FieldCache.Longs arr, IBits valid)
            {
                this.parent = parent;
                this.arr = arr;
                this.valid = valid;

                mval = parent.NewMutableValueLong();
            }

            private readonly LongFieldSource parent;
            private readonly FieldCache.Longs arr;
            private readonly IBits valid;
            
            private readonly MutableValueLong mval;

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

        private sealed class AnonymousLongDocValues : LongDocValues
        {
            public AnonymousLongDocValues(LongFieldSource parent, FieldCache.Longs arr, IBits valid)
                : base(parent)
            {
                this.parent = parent;
                this.arr = arr;
                this.valid = valid;
            }

            private readonly LongFieldSource parent;
            private readonly FieldCache.Longs arr;
            private readonly IBits valid;

            public override long LongVal(int doc)
            {
                return arr.Get(doc);
            }

            public override bool Exists(int doc)
            {
                return valid[doc];
            }

            public override Object ObjectVal(int doc)
            {
                return valid[doc] ? parent.LongToObject(arr.Get(doc)) : null;
            }

            public override string StrVal(int doc)
            {
                return valid[doc] ? parent.LongToString(arr.Get(doc)) : null;
            }

            public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
            {
                long lower, upper;
                if (lowerVal == null)
                {
                    lower = long.MinValue;
                }
                else
                {
                    lower = parent.ExternalToLong(lowerVal);
                    if (!includeLower && lower < long.MaxValue)
                        lower++;
                }

                if (upperVal == null)
                {
                    upper = long.MaxValue;
                }
                else
                {
                    upper = parent.ExternalToLong(upperVal);
                    if (!includeUpper && upper > long.MinValue)
                        upper--;
                }

                long ll = lower;
                long uu = upper;
                return new AnonymousValueSourceScorer(parent, reader, this, arr, ll, uu);
            }

            public override ValueFiller GetValueFiller()
            {
                return new AnonymousValueFiller(parent, arr, valid);
            }
        }

        protected virtual MutableValueLong NewMutableValueLong()
        {
            return new MutableValueLong();
        }

        public override bool Equals(Object o)
        {
            if (o.GetType() != this.GetType())
                return false;
            LongFieldSource other = (LongFieldSource)o;
            return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = parser == null ? this.GetType().GetHashCode() : parser.GetType().GetHashCode();
            h = base.GetHashCode();
            return h;
        }
    }
}
