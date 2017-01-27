using System;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Queries.Function
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Represents field values as different types.
    /// Normally created via a <seealso cref="ValueSource"/> for a particular field and reader.
    /// 
    /// 
    /// </summary>

    // FunctionValues is distinct from ValueSource because
    // there needs to be an object created at query evaluation time that
    // is not referenced by the query itself because:
    // - Query objects should be MT safe
    // - For caching, Query objects are often used as keys... you don't
    //   want the Query carrying around big objects
    public abstract class FunctionValues
    {
        public virtual sbyte ByteVal(int doc)
        {
            throw new System.NotSupportedException();
        }

        public virtual short ShortVal(int doc) // LUCENENET TODO: Rename Int16Val ?
        {
            throw new System.NotSupportedException();
        }

        public virtual float FloatVal(int doc) // LUCENENET TODO: Rename SingleVal ?
        {
            throw new System.NotSupportedException();
        }

        public virtual int IntVal(int doc) // LUCENENET TODO: Rename Int32Val ?
        {
            throw new System.NotSupportedException();
        }

        public virtual long LongVal(int doc) // LUCENENET TODO: Rename Int64Val ?
        {
            throw new System.NotSupportedException();
        }

        public virtual double DoubleVal(int doc)
        {
            throw new System.NotSupportedException();
        }

        // TODO: should we make a termVal, returns BytesRef?
        public virtual string StrVal(int doc)
        {
            throw new System.NotSupportedException();
        }

        public virtual bool BoolVal(int doc)
        {
            return IntVal(doc) != 0;
        }

        /// <summary>
        /// returns the bytes representation of the str val - TODO: should this return the indexed raw bytes not? </summary>
        public virtual bool BytesVal(int doc, BytesRef target)
        {
            string s = StrVal(doc);
            if (s == null)
            {
                target.Length = 0;
                return false;
            }
            target.CopyChars(s);
            return true;
        }

        /// <summary>
        /// Native Object representation of the value </summary>
        public virtual object ObjectVal(int doc)
        {
            // most FunctionValues are functions, so by default return a Float()
            return FloatVal(doc);
        }

        /// <summary>
        /// Returns true if there is a value for this document </summary>
        public virtual bool Exists(int doc)
        {
            return true;
        }

        /// <param name="doc"> The doc to retrieve to sort ordinal for </param>
        /// <returns> the sort ordinal for the specified doc
        /// TODO: Maybe we can just use intVal for this... </returns>
        public virtual int OrdVal(int doc)
        {
            throw new System.NotSupportedException();
        }

        /// <returns> the number of unique sort ordinals this instance has </returns>
        public virtual int NumOrd() // LUCENENET TODO: Make property ?
        {
            throw new System.NotSupportedException();
        }

        public abstract string ToString(int doc);

        /// <summary>
        /// Abstraction of the logic required to fill the value of a specified doc into
        /// a reusable <seealso cref="MutableValue"/>.  Implementations of <seealso cref="FunctionValues"/>
        /// are encouraged to define their own implementations of ValueFiller if their
        /// value is not a float.
        /// 
        /// @lucene.experimental
        /// </summary>
        public abstract class AbstractValueFiller // LUCENENET TODO: Rename ValueFiller (after renaming property ValueFiller)
        {
            /// <summary>
            /// MutableValue will be reused across calls </summary>
            public abstract MutableValue Value { get; }

            /// <summary>
            /// MutableValue will be reused across calls.  Returns true if the value exists. </summary>
            public abstract void FillValue(int doc);
        }

        /// <summary>
        /// @lucene.experimental </summary>
        public virtual AbstractValueFiller ValueFiller // LUCENENET TODO: Make GetValueFiller() (returns new object)
        {
            get { return new ValueFillerAnonymousInnerClassHelper(this); }
        }

        private class ValueFillerAnonymousInnerClassHelper : AbstractValueFiller
        {
            private readonly FunctionValues outerInstance;

            public ValueFillerAnonymousInnerClassHelper(FunctionValues outerInstance)
            {
                this.outerInstance = outerInstance;
                mval = new MutableValueFloat();
            }

            private readonly MutableValueFloat mval;

            public override MutableValue Value
            {
                get { return mval; }
            }

            public override void FillValue(int doc)
            {
                mval.Value = outerInstance.FloatVal(doc);
            }
        }

        //For Functions that can work with multiple values from the same document.  This does not apply to all functions
        public virtual void ByteVal(int doc, sbyte[] vals)
        {
            throw new System.NotSupportedException();
        }

        public virtual void ShortVal(int doc, short[] vals) // LUCENENET TODO: Rename Int16Val ?
        {
            throw new System.NotSupportedException();
        }

        public virtual void FloatVal(int doc, float[] vals) // LUCENENET TODO: Rename SingleVal ?
        {
            throw new System.NotSupportedException();
        }

        public virtual void IntVal(int doc, int[] vals) // LUCENENET TODO: Rename Int32Val ?
        {
            throw new System.NotSupportedException();
        }

        public virtual void LongVal(int doc, long[] vals) // LUCENENET TODO: Rename Int64Val ?
        {
            throw new System.NotSupportedException();
        }

        public virtual void DoubleVal(int doc, double[] vals)
        {
            throw new System.NotSupportedException();
        }

        // TODO: should we make a termVal, fills BytesRef[]?
        public virtual void StrVal(int doc, string[] vals)
        {
            throw new System.NotSupportedException();
        }

        public virtual Explanation Explain(int doc)
        {
            return new Explanation(FloatVal(doc), ToString(doc));
        }

        public virtual ValueSourceScorer GetScorer(IndexReader reader)
        {
            return new ValueSourceScorer(reader, this);
        }

        // A RangeValueSource can't easily be a ValueSource that takes another ValueSource
        // because it needs different behavior depending on the type of fields.  There is also
        // a setup cost - parsing and normalizing params, and doing a binary search on the StringIndex.
        // TODO: change "reader" to AtomicReaderContext
        public virtual ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal,
            bool includeLower, bool includeUpper)
        {
            float lower;
            float upper;

            if (lowerVal == null)
            {
                lower = float.NegativeInfinity;
            }
            else
            {
                lower = Convert.ToSingle(lowerVal);
            }
            if (upperVal == null)
            {
                upper = float.PositiveInfinity;
            }
            else
            {
                upper = Convert.ToSingle(upperVal);
            }

            float l = lower;
            float u = upper;

            if (includeLower && includeUpper)
            {
                return new ValueSourceScorerAnonymousInnerClassHelper(this, reader, this, l, u);
            }
            else if (includeLower && !includeUpper)
            {
                return new ValueSourceScorerAnonymousInnerClassHelper2(this, reader, this, l, u);
            }
            else if (!includeLower && includeUpper)
            {
                return new ValueSourceScorerAnonymousInnerClassHelper3(this, reader, this, l, u);
            }
            else
            {
                return new ValueSourceScorerAnonymousInnerClassHelper4(this, reader, this, l, u);
            }
        }

        private class ValueSourceScorerAnonymousInnerClassHelper : ValueSourceScorer
        {
            private readonly FunctionValues outerInstance;

            private readonly float l;
            private readonly float u;

            public ValueSourceScorerAnonymousInnerClassHelper(FunctionValues outerInstance, IndexReader reader,
                FunctionValues @this, float l, float u)
                : base(reader, @this)
            {
                this.outerInstance = outerInstance;
                this.l = l;
                this.u = u;
            }

            public override bool MatchesValue(int doc)
            {
                float docVal = outerInstance.FloatVal(doc);
                return docVal >= l && docVal <= u;
            }
        }

        private class ValueSourceScorerAnonymousInnerClassHelper2 : ValueSourceScorer
        {
            private readonly FunctionValues outerInstance;

            private readonly float l;
            private readonly float u;

            public ValueSourceScorerAnonymousInnerClassHelper2(FunctionValues outerInstance, IndexReader reader,
                FunctionValues @this, float l, float u)
                : base(reader, @this)
            {
                this.outerInstance = outerInstance;
                this.l = l;
                this.u = u;
            }

            public override bool MatchesValue(int doc)
            {
                float docVal = outerInstance.FloatVal(doc);
                return docVal >= l && docVal < u;
            }
        }

        private class ValueSourceScorerAnonymousInnerClassHelper3 : ValueSourceScorer
        {
            private readonly FunctionValues outerInstance;

            private readonly float l;
            private readonly float u;

            public ValueSourceScorerAnonymousInnerClassHelper3(FunctionValues outerInstance, IndexReader reader,
                FunctionValues @this, float l, float u)
                : base(reader, @this)
            {
                this.outerInstance = outerInstance;
                this.l = l;
                this.u = u;
            }

            public override bool MatchesValue(int doc)
            {
                float docVal = outerInstance.FloatVal(doc);
                return docVal > l && docVal <= u;
            }
        }

        private class ValueSourceScorerAnonymousInnerClassHelper4 : ValueSourceScorer
        {
            private readonly FunctionValues outerInstance;

            private readonly float l;
            private readonly float u;

            public ValueSourceScorerAnonymousInnerClassHelper4(FunctionValues outerInstance, IndexReader reader,
                FunctionValues @this, float l, float u)
                : base(reader, @this)
            {
                this.outerInstance = outerInstance;
                this.l = l;
                this.u = u;
            }

            public override bool MatchesValue(int doc)
            {
                float docVal = outerInstance.FloatVal(doc);
                return docVal > l && docVal < u;
            }
        }
    }
}