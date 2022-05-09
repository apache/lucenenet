// Lucene version compatibility level 4.8.1
using System;
using System.Globalization;
using J2N.Globalization;
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
    /// Normally created via a <see cref="ValueSource"/> for a particular field and reader.
    /// </summary>

    // FunctionValues is distinct from ValueSource because
    // there needs to be an object created at query evaluation time that
    // is not referenced by the query itself because:
    // - Query objects should be MT safe
    // - For caching, Query objects are often used as keys... you don't
    //   want the Query carrying around big objects
    public abstract class FunctionValues
    {
        public virtual byte ByteVal(int doc)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// NOTE: This was shortVal() in Lucene
        /// </summary>
        public virtual short Int16Val(int doc) 
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// NOTE: This was floatVal() in Lucene
        /// </summary>
        public virtual float SingleVal(int doc)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// NOTE: This was intVal() in Lucene
        /// </summary>
        public virtual int Int32Val(int doc)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// NOTE: This was longVal() in Lucene
        /// </summary>
        public virtual long Int64Val(int doc)
        {
            throw UnsupportedOperationException.Create();
        }

        public virtual double DoubleVal(int doc)
        {
            throw UnsupportedOperationException.Create();
        }

        // TODO: should we make a termVal, returns BytesRef?
        public virtual string StrVal(int doc)
        {
            throw UnsupportedOperationException.Create();
        }

        public virtual bool BoolVal(int doc)
        {
            return Int32Val(doc) != 0;
        }

        /// <summary>
        /// returns the bytes representation of the str val - TODO: should this return the indexed raw bytes not? </summary>
        public virtual bool BytesVal(int doc, BytesRef target)
        {
            string s = StrVal(doc);
            if (s is null)
            {
                target.Length = 0;
                return false;
            }
            target.CopyChars(s);
            return true;
        }

        /// <summary>
        /// Native <see cref="object"/> representation of the value </summary>
        public virtual object ObjectVal(int doc)
        {
            // most FunctionValues are functions, so by default return a Float()
            return J2N.Numerics.Single.GetInstance(SingleVal(doc)); // LUCENENET: In Java, the conversion to instance of java.util.Float is implicit, but we need to do an explicit conversion
        }

        /// <summary>
        /// Returns <c>true</c> if there is a value for this document </summary>
        public virtual bool Exists(int doc)
        {
            return true;
        }

        /// <param name="doc"> The doc to retrieve to sort ordinal for </param>
        /// <returns> the sort ordinal for the specified doc
        /// TODO: Maybe we can just use intVal for this... </returns>
        public virtual int OrdVal(int doc)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <returns> the number of unique sort ordinals this instance has </returns>
        public virtual int NumOrd => throw UnsupportedOperationException.Create();

        public abstract string ToString(int doc);

        /// <summary>
        /// Abstraction of the logic required to fill the value of a specified doc into
        /// a reusable <see cref="MutableValue"/>.  Implementations of <see cref="FunctionValues"/>
        /// are encouraged to define their own implementations of <see cref="ValueFiller"/> if their
        /// value is not a <see cref="float"/>.
        /// 
        /// @lucene.experimental
        /// </summary>
        public abstract class ValueFiller
        {
            /// <summary>
            /// <see cref="MutableValue"/> will be reused across calls </summary>
            public abstract MutableValue Value { get; }

            /// <summary>
            /// <see cref="MutableValue"/> will be reused across calls.  Returns <c>true</c> if the value exists. </summary>
            public abstract void FillValue(int doc);

            /// <summary>
            /// This class may be used to create <see cref="ValueFiller"/> instances anonymously.
            /// </summary>
            // LUCENENET specific - used to mimick the inline class behavior in Java.
            internal class AnonymousValueFiller<T> : ValueFiller where T : MutableValue
            {
                private readonly T mutableValue;
                private readonly Action<int, T> fillValue;

                public AnonymousValueFiller(T mutableValue, Action<int, T> fillValue)
                {
                    this.mutableValue = mutableValue ?? throw new ArgumentNullException(nameof(mutableValue));
                    this.fillValue = fillValue ?? throw new ArgumentNullException(nameof(fillValue));
                }

                public override MutableValue Value => mutableValue;

                public override void FillValue(int doc)
                {
                    fillValue(doc, mutableValue);
                }
            }
        }

        /// <summary>
        /// @lucene.experimental </summary>
        public virtual ValueFiller GetValueFiller()
        {
            return new ValueFiller.AnonymousValueFiller<MutableValueSingle>(new MutableValueSingle(), fillValue: (doc, mutableValue) =>
            {
                mutableValue.Value = SingleVal(doc);
            });
        }

        //For Functions that can work with multiple values from the same document.  This does not apply to all functions
        public virtual void ByteVal(int doc, byte[] vals)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// NOTE: This was shortVal() in Lucene
        /// </summary>
        public virtual void Int16Val(int doc, short[] vals)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// NOTE: This was floatVal() in Lucene
        /// </summary>
        public virtual void SingleVal(int doc, float[] vals)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// NOTE: This was intVal() in Lucene
        /// </summary>
        public virtual void Int32Val(int doc, int[] vals)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// NOTE: This was longVal() in Lucene
        /// </summary>
        public virtual void Int64Val(int doc, long[] vals)
        {
            throw UnsupportedOperationException.Create();
        }

        public virtual void DoubleVal(int doc, double[] vals)
        {
            throw UnsupportedOperationException.Create();
        }

        // TODO: should we make a termVal, fills BytesRef[]?
        public virtual void StrVal(int doc, string[] vals)
        {
            throw UnsupportedOperationException.Create();
        }

        public virtual Explanation Explain(int doc)
        {
            return new Explanation(SingleVal(doc), ToString(doc));
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

            if (lowerVal is null)
            {
                lower = float.NegativeInfinity;
            }
            else
            {
                lower = J2N.Numerics.Single.Parse(lowerVal, NumberStyle.Float, NumberFormatInfo.InvariantInfo);
            }
            if (upperVal is null)
            {
                upper = float.PositiveInfinity;
            }
            else
            {
                upper = J2N.Numerics.Single.Parse(upperVal, NumberStyle.Float, NumberFormatInfo.InvariantInfo);
            }

            float l = lower;
            float u = upper;

            if (includeLower && includeUpper)
            {
                return new ValueSourceScorer.AnonymousValueSourceScorer(reader, this, matchesValue: (doc) =>
                {
                    float docVal = SingleVal(doc);
                    return docVal >= l && docVal <= u;
                });
            }
            else if (includeLower && !includeUpper)
            {
                return new ValueSourceScorer.AnonymousValueSourceScorer(reader, this, matchesValue: (doc) =>
                {
                    float docVal = SingleVal(doc);
                    return docVal >= l && docVal < u;
                });
            }
            else if (!includeLower && includeUpper)
            {
                return new ValueSourceScorer.AnonymousValueSourceScorer(reader, this, matchesValue: (doc) =>
                {
                    float docVal = SingleVal(doc);
                    return docVal > l && docVal <= u;
                });
            }
            else
            {
                return new ValueSourceScorer.AnonymousValueSourceScorer(reader, this, matchesValue: (doc) =>
                {
                    float docVal = SingleVal(doc);
                    return docVal > l && docVal < u;
                });
            }
        }
    }
}