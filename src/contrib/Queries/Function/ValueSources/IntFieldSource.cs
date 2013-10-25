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
    public class IntFieldSource : FieldCacheSource
    {
        readonly FieldCache.IIntParser parser;
        
        public IntFieldSource(string field)
            : this (field, null)
        {
        }

        public IntFieldSource(string field, FieldCache.IIntParser parser)
            : base (field)
        {
            this.parser = parser;
        }

        public override string Description
        {
            get 
            {
                return @"int(" + field + ')';
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FieldCache.Ints arr = cache.GetInts(readerContext.AtomicReader, field, parser, true);
            IBits valid = cache.GetDocsWithField(readerContext.AtomicReader, field);
            return new AnonymousIntDocValues(this, arr, valid);
        }

        private sealed class AnonymousValueSourceScorer : ValueSourceScorer
        {
            public AnonymousValueSourceScorer(IntFieldSource parent, IndexReader reader, FunctionValues values, FieldCache.Ints arr, int ll, int uu)
                : base(reader, values)                
            {
                this.parent = parent;
                this.arr = arr;
                this.ll = ll;
                this.uu = uu;
            }

            private readonly IntFieldSource parent;
            private readonly FieldCache.Ints arr;
            private readonly int ll, uu;

            public override bool MatchesValue(int doc)
            {
                int val = arr.Get(doc);
                return val >= ll && val <= uu;
            }
        }

        private sealed class AnonymousValueFiller : FunctionValues.ValueFiller
        {
            public AnonymousValueFiller(IntFieldSource parent, FieldCache.Ints arr, IBits valid)
            {
                this.parent = parent;
                this.arr = arr;
                this.valid = valid;
            }

            private readonly IntFieldSource parent;
            private readonly FieldCache.Ints arr;
            private readonly IBits valid;

            private readonly MutableValueInt mval = new MutableValueInt();

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

        private sealed class AnonymousIntDocValues : IntDocValues
        {
            public AnonymousIntDocValues(IntFieldSource parent, FieldCache.Ints arr, IBits valid)
                : base(parent)
            {
                this.parent = parent;
                this.arr = arr;
                this.valid = valid;
            }

            private readonly IntFieldSource parent;
            private readonly FieldCache.Ints arr;
            private readonly IBits valid;

            readonly MutableValueInt val = new MutableValueInt();
            
            public override float FloatVal(int doc)
            {
                return (float)arr.Get(doc);
            }

            public override int IntVal(int doc)
            {
                return arr.Get(doc);
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

            public override Object ObjectVal(int doc)
            {
                return valid[doc] ? (object)arr.Get(doc) : null;
            }

            public override bool Exists(int doc)
            {
                return valid[doc];
            }

            public override string ToString(int doc)
            {
                return parent.Description + '=' + IntVal(doc);
            }

            public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
            {
                int lower, upper;
                if (lowerVal == null)
                {
                    lower = int.MinValue;
                }
                else
                {
                    lower = int.Parse(lowerVal);
                    if (!includeLower && lower < int.MaxValue)
                        lower++;
                }

                if (upperVal == null)
                {
                    upper = int.MaxValue;
                }
                else
                {
                    upper = int.Parse(upperVal);
                    if (!includeUpper && upper > int.MinValue)
                        upper--;
                }

                int ll = lower;
                int uu = upper;
                return new AnonymousValueSourceScorer(parent, reader, this, arr, ll, uu);
            }

            public override FunctionValues.ValueFiller GetValueFiller()
            {
                return new AnonymousValueFiller(parent, arr, valid);
            }
        }

        public override bool Equals(Object o)
        {
            if (o.GetType() != typeof (IntFieldSource))
                return false;
            IntFieldSource other = (IntFieldSource) o;
            return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = parser == null ? typeof(int).GetHashCode() : parser.GetType().GetHashCode();
            h = base.GetHashCode();
            return h;
        }
    }
}
