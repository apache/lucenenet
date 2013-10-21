using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function
{
    public abstract class FunctionValues
    {
        public virtual byte ByteVal(int doc) { throw new NotSupportedException(); }
        public virtual short ShortVal(int doc) { throw new NotSupportedException(); }

        public virtual float FloatVal(int doc) { throw new NotSupportedException(); }
        public virtual int IntVal(int doc) { throw new NotSupportedException(); }
        public virtual long LongVal(int doc) { throw new NotSupportedException(); }
        public virtual double DoubleVal(int doc) { throw new NotSupportedException(); }
        // TODO: should we make a termVal, returns BytesRef?
        public virtual string StrVal(int doc) { throw new NotSupportedException(); }

        public virtual bool BoolVal(int doc)
        {
            return IntVal(doc) != 0;
        }

        /** returns the bytes representation of the string val - TODO: should this return the indexed raw bytes not? */
        public virtual bool BytesVal(int doc, BytesRef target)
        {
            string s = StrVal(doc);
            if (s == null)
            {
                target.length = 0;
                return false;
            }
            target.CopyChars(s);
            return true;
        }

        /** Native .NET Object representation of the value */
        public virtual object ObjectVal(int doc)
        {
            // most FunctionValues are functions, so by default return a Float()
            return FloatVal(doc);
        }

        /** Returns true if there is a value for this document */
        public virtual bool Exists(int doc)
        {
            return true;
        }

        public virtual int OrdVal(int doc) { throw new NotSupportedException(); }

        public virtual int NumOrd() { throw new NotSupportedException(); }
        public abstract string ToString(int doc);

        public abstract class ValueFiller
        {
            /** MutableValue will be reused across calls */
            public abstract MutableValue Value { get; }

            /** MutableValue will be reused across calls.  Returns true if the value exists. */
            public abstract void FillValue(int doc);
        }

        public virtual ValueFiller GetValueFiller()
        {
            return new AnonymousDefaultValueFiller(this);
        }

        private sealed class AnonymousDefaultValueFiller : ValueFiller
        {
            private readonly MutableValueFloat mval = new MutableValueFloat();
            private readonly FunctionValues parent;

            public AnonymousDefaultValueFiller(FunctionValues parent)
            {
                this.parent = parent;
            }

            public override MutableValue Value
            {
                get { return mval; }
            }

            public override void FillValue(int doc)
            {
                mval.Value = parent.FloatVal(doc);
            }
        }

        //For Functions that can work with multiple values from the same document.  This does not apply to all functions
        public virtual void ByteVal(int doc, byte[] vals) { throw new NotSupportedException(); }
        public virtual void ShortVal(int doc, short[] vals) { throw new NotSupportedException(); }

        public virtual void FloatVal(int doc, float[] vals) { throw new NotSupportedException(); }
        public virtual void IntVal(int doc, int[] vals) { throw new NotSupportedException(); }
        public virtual void LongVal(int doc, long[] vals) { throw new NotSupportedException(); }
        public virtual void DoubleVal(int doc, double[] vals) { throw new NotSupportedException(); }

        // TODO: should we make a termVal, fills BytesRef[]?
        public void StrVal(int doc, string[] vals) { throw new NotSupportedException(); }

        public Explanation Explain(int doc)
        {
            return new Explanation(FloatVal(doc), ToString(doc));
        }

        public ValueSourceScorer GetScorer(IndexReader reader)
        {
            return new ValueSourceScorer(reader, this);
        }

        public ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
        {
            float lower;
            float upper;

            if (lowerVal == null)
            {
                lower = float.NegativeInfinity;
            }
            else
            {
                lower = float.Parse(lowerVal);
            }
            if (upperVal == null)
            {
                upper = float.PositiveInfinity;
            }
            else
            {
                upper = float.Parse(upperVal);
            }

            float l = lower;
            float u = upper;

            if (includeLower && includeUpper)
            {
                return new AnonymousValueSourceScorer(reader, this, doc =>
                {
                    float docVal = FloatVal(doc);
                    return docVal >= l && docVal <= u;
                });
            }
            else if (includeLower && !includeUpper)
            {
                return new AnonymousValueSourceScorer(reader, this, doc =>
                {
                    float docVal = FloatVal(doc);
                    return docVal >= l && docVal < u;
                });
            }
            else if (!includeLower && includeUpper)
            {
                return new AnonymousValueSourceScorer(reader, this, doc =>
                {
                    float docVal = FloatVal(doc);
                    return docVal > l && docVal <= u;
                });
            }
            else
            {
                return new AnonymousValueSourceScorer(reader, this, doc =>
                {
                    float docVal = FloatVal(doc);
                    return docVal > l && docVal < u;
                });
            }
        }

        private class AnonymousValueSourceScorer : ValueSourceScorer
        {
            protected readonly Func<int, bool> delegated;

            public AnonymousValueSourceScorer(IndexReader reader, FunctionValues parent, Func<int, bool> delegated)
                : base(reader, parent)
            {
                this.delegated = delegated;
            }

            public override bool MatchesValue(int doc)
            {
                return this.delegated(doc);
            }
        }
    }
}
