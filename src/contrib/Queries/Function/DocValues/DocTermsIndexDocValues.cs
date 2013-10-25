using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.DocValues
{
    public abstract class DocTermsIndexDocValues : FunctionValues
    {
        protected readonly SortedDocValues termsIndex;
        protected readonly ValueSource vs;
        protected readonly MutableValueStr val = new MutableValueStr();
        protected readonly BytesRef spare = new BytesRef();
        protected readonly CharsRef spareChars = new CharsRef();

        public DocTermsIndexDocValues(ValueSource vs, AtomicReaderContext context, String field)
        {
            try
            {
                termsIndex = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, field);
            }
            catch (Exception e)
            {
                throw new DocTermsIndexException(field, e);
            }
            this.vs = vs;
        }

        protected abstract String ToTerm(String readableValue);

        public override bool Exists(int doc)
        {
            return OrdVal(doc) >= 0;
        }

        public override int OrdVal(int doc)
        {
            return termsIndex.GetOrd(doc);
        }

        public override int NumOrd()
        {
            return termsIndex.ValueCount;
        }

        public override bool BytesVal(int doc, BytesRef target)
        {
            termsIndex.Get(doc, target);
            return target.length > 0;
        }

        public override string StrVal(int doc)
        {
            termsIndex.Get(doc, spare);
            if (spare.length == 0)
            {
                return null;
            }
            UnicodeUtil.UTF8toUTF16(spare, spareChars);
            return spareChars.ToString();
        }

        public override bool BoolVal(int doc)
        {
            return Exists(doc);
        }

        public abstract override object ObjectVal(int doc);

        public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
        {
            // TODO: are lowerVal and upperVal in indexed form or not?
            lowerVal = lowerVal == null ? null : ToTerm(lowerVal);
            upperVal = upperVal == null ? null : ToTerm(upperVal);

            int lower = int.MinValue;
            if (lowerVal != null)
            {
                lower = termsIndex.LookupTerm(new BytesRef(lowerVal));
                if (lower < 0)
                {
                    lower = -lower - 1;
                }
                else if (!includeLower)
                {
                    lower++;
                }
            }

            int upper = int.MaxValue;
            if (upperVal != null)
            {
                upper = termsIndex.LookupTerm(new BytesRef(upperVal));
                if (upper < 0)
                {
                    upper = -upper - 2;
                }
                else if (!includeUpper)
                {
                    upper--;
                }
            }

            int ll = lower;
            int uu = upper;

            return new AnonymousValueSourceScorer(reader, this, termsIndex, ll, uu);
        }

        private sealed class AnonymousValueSourceScorer : ValueSourceScorer
        {
            private readonly SortedDocValues termsIndex;
            private readonly int ll;
            private readonly int uu;

            public AnonymousValueSourceScorer(IndexReader reader, FunctionValues values, SortedDocValues termsIndex, int ll, int uu)
                : base(reader, values)
            {
                this.termsIndex = termsIndex;
                this.ll = ll;
                this.uu = uu;
            }

            public override bool MatchesValue(int doc)
            {
                int ord = termsIndex.GetOrd(doc);
                return ord >= ll && ord <= uu;
            }
        }

        public override string ToString(int doc)
        {
            return vs.Description + '=' + StrVal(doc);
        }

        public override ValueFiller GetValueFiller()
        {
            return new AnonymousValueFiller(termsIndex);
        }

        private sealed class AnonymousValueFiller : ValueFiller
        {
            private readonly MutableValueStr mval = new MutableValueStr();
            private readonly SortedDocValues termsIndex;

            public AnonymousValueFiller(SortedDocValues termsIndex)
            {
                this.termsIndex = termsIndex;
            }

            public override MutableValue Value
            {
                get { return mval; }
            }

            public override void FillValue(int doc)
            {
                termsIndex.Get(doc, mval.Value);
                mval.Exists = mval.Value.bytes != SortedDocValues.MISSING;
            }
        }

        public sealed class DocTermsIndexException : Exception
        {
            public DocTermsIndexException(String fieldName, Exception cause)
                : base("Can't initialize DocTermsIndex to generate (function) FunctionValues for field: " + fieldName, cause)
            {
            }
        }
    }
}
