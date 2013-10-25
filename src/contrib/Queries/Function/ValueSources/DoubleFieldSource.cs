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
    public class DoubleFieldSource : FieldCacheSource
    {
        protected readonly FieldCache.IDoubleParser parser;

        public DoubleFieldSource(string field)
            : this(field, null)
        {
        }

        public DoubleFieldSource(string field, FieldCache.IDoubleParser parser)
            : base (field)
        {
            this.parser = parser;
        }

        public override string Description
        {
            get 
            {
                return "double(" + field + ')';
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FieldCache.Doubles arr = cache.GetDoubles(readerContext.AtomicReader, field, parser, true);
            IBits valid = cache.GetDocsWithField(readerContext.AtomicReader, field);
            return new AnonymousDoubleDocValues(this, arr, valid);
        }

        private sealed class AnonymousDelegatedValueSourceScorer : ValueSourceScorer
        {
            public AnonymousDelegatedValueSourceScorer(IndexReader reader, FunctionValues values, Func<int, bool> delegated)
                : base(reader, values)
            {
                this.delegated = delegated;
            }

            private readonly Func<int, bool> delegated;

            public override bool MatchesValue(int doc)
            {
                return delegated(doc);
            }
        }

        private sealed class AnonymousValueFiller : FunctionValues.ValueFiller
        {
            public AnonymousValueFiller(DoubleFieldSource parent, FieldCache.Doubles arr, IBits valid)
            {
                this.parent = parent;
                this.arr = arr;
                this.valid = valid;
            }

            private readonly DoubleFieldSource parent;
            private readonly FieldCache.Doubles arr;
            private readonly IBits valid;
            private readonly MutableValueDouble mval = new MutableValueDouble();

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

        private sealed class AnonymousDoubleDocValues : DoubleDocValues
        {
            public AnonymousDoubleDocValues(DoubleFieldSource parent, FieldCache.Doubles arr, IBits valid)
                : base(parent)
            {
                this.parent = parent;
                this.arr = arr;
                this.valid = valid;
            }

            private readonly DoubleFieldSource parent;
            private readonly FieldCache.Doubles arr;
            private readonly IBits valid;

            public override double DoubleVal(int doc)
            {
                return arr.Get(doc);
            }

            public override bool Exists(int doc)
            {
                return valid[doc];
            }

            public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
            {
                double lower, upper;
                if (lowerVal == null)
                {
                    lower = Double.NegativeInfinity;
                }
                else
                {
                    lower = Double.Parse(lowerVal);
                }

                if (upperVal == null)
                {
                    upper = Double.PositiveInfinity;
                }
                else
                {
                    upper = Double.Parse(upperVal);
                }

                double l = lower;
                double u = upper;
                if (includeLower && includeUpper)
                {
                    return new AnonymousDelegatedValueSourceScorer(reader, this, doc =>
                    {
                        double docVal = DoubleVal(doc);
                        return docVal >= l && docVal <= u;
                    });
                }
                else if (includeLower && !includeUpper)
                {
                    return new AnonymousDelegatedValueSourceScorer(reader, this, doc =>
                    {
                        double docVal = DoubleVal(doc);
                        return docVal >= l && docVal < u;
                    });
                }
                else if (!includeLower && includeUpper)
                {
                    return new AnonymousDelegatedValueSourceScorer(reader, this, doc =>
                    {
                        double docVal = DoubleVal(doc);
                        return docVal > l && docVal <= u;
                    });
                }
                else
                {
                    return new AnonymousDelegatedValueSourceScorer(reader, this, doc =>
                    {
                        double docVal = DoubleVal(doc);
                        return docVal > l && docVal < u;
                    });
                }
            }

            public override FunctionValues.ValueFiller GetValueFiller()
            {
                return new AnonymousValueFiller(parent, arr, valid);
            }
        }

        public override bool Equals(Object o)
        {
            if (o.GetType() != typeof (DoubleFieldSource))
                return false;
            DoubleFieldSource other = (DoubleFieldSource) o;
            return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = parser == null ? typeof(double).GetHashCode() : parser.GetType().GetHashCode();
            h = base.GetHashCode();
            return h;
        }
    }
}
