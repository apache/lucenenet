// Lucene version compatibility level 4.8.1
using J2N.Runtime.CompilerServices;
using Lucene.Net.Index;
using Lucene.Net.Search;
using System;
using System.Collections;
using JCG = J2N.Collections.Generic;

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
    /// Instantiates <see cref="FunctionValues"/> for a particular reader.
    /// <para/>
    /// Often used when creating a <see cref="FunctionQuery"/>.
    /// </summary>
    public abstract class ValueSource
    {
        /// <summary>
        /// Gets the values for this reader and the context that was previously
        /// passed to <see cref="CreateWeight"/>
        /// </summary>
        public abstract FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext); // LUCENENET TODO: API - See if we can use generic IDictionary here instead

        public override abstract bool Equals(object o);

        public override abstract int GetHashCode();

        /// <summary>
        /// description of field, used in Explain()
        /// </summary>
        public abstract string GetDescription();

        public override string ToString()
        {
            return GetDescription();
        }


        /// <summary>
        /// Implementations should propagate CreateWeight to sub-ValueSources which can optionally store
        /// weight info in the context. The context object will be passed to GetValues()
        /// where this info can be retrieved.
        /// </summary>
        public virtual void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
        }

        /// <summary>
        /// Returns a new non-threadsafe context map.
        /// </summary>
        public static IDictionary NewContext(IndexSearcher searcher)
        {
            return new Hashtable(IdentityEqualityComparer<object>.Default)
            {
                ["searcher"] = searcher
            };
        }


        //
        // Sorting by function
        //

        /// <summary>
        /// EXPERIMENTAL: This method is subject to change.
        /// <para/>
        /// Get the <see cref="SortField"/> for this <see cref="ValueSource"/>.  Uses the <see cref="GetValues(IDictionary, AtomicReaderContext)"/>
        /// to populate the <see cref="SortField"/>.
        /// </summary>
        /// <param name="reverse"> <c>true</c> if this is a reverse sort. </param>
        /// <returns> The <see cref="SortField"/> for the <see cref="ValueSource"/> </returns>
        public virtual SortField GetSortField(bool reverse)
        {
            return new ValueSourceSortField(this, reverse);
        }

        internal class ValueSourceSortField : SortField
        {
            private readonly ValueSource outerInstance;

            public ValueSourceSortField(ValueSource outerInstance, bool reverse)
                : base(outerInstance.GetDescription(), SortFieldType.REWRITEABLE, reverse)
            {
                this.outerInstance = outerInstance;
            }

            public override SortField Rewrite(IndexSearcher searcher)
            {
                var context = NewContext(searcher);
                outerInstance.CreateWeight(context, searcher);
                return new SortField(Field, new ValueSourceComparerSource(outerInstance, context), IsReverse);
            }
        }

        internal class ValueSourceComparerSource : FieldComparerSource
        {
            private readonly ValueSource outerInstance;

            private readonly IDictionary context;

            public ValueSourceComparerSource(ValueSource outerInstance, IDictionary context)
            {
                this.outerInstance = outerInstance;
                this.context = context;
            }

            public override FieldComparer NewComparer(string fieldname, int numHits, int sortPos, bool reversed)
            {
                return new ValueSourceComparer(outerInstance, context, numHits);
            }
        }

        /// <summary>
        /// Implement a <see cref="FieldComparer"/> that works
        /// off of the <see cref="FunctionValues"/> for a <see cref="ValueSource"/>
        /// instead of the normal Lucene <see cref="FieldComparer"/> that works off of a <see cref="FieldCache"/>.
        /// </summary>
        internal class ValueSourceComparer : FieldComparer<J2N.Numerics.Double>
        {
            private readonly ValueSource outerInstance;

            private readonly double[] values;
            private FunctionValues docVals;
            private double bottom;
            private readonly IDictionary fcontext;
            private double topValue;

            internal ValueSourceComparer(ValueSource outerInstance, IDictionary fcontext, int numHits)
            {
                this.outerInstance = outerInstance;
                this.fcontext = fcontext;
                values = new double[numHits];
            }

            public override int Compare(int slot1, int slot2)
            {
                // LUCENENET specific - use JCG comparer to get the same logic as Java
                return JCG.Comparer<double>.Default.Compare(values[slot1], values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                // LUCENENET specific - use JCG comparer to get the same logic as Java
                return JCG.Comparer<double>.Default.Compare(bottom, docVals.DoubleVal(doc));
            }

            public override void Copy(int slot, int doc)
            {
                values[slot] = docVals.DoubleVal(doc);
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                docVals = outerInstance.GetValues(fcontext, context);
                return this;
            }

            public override void SetBottom(int slot)
            {
                this.bottom = values[slot];
            }

            public override void SetTopValue(J2N.Numerics.Double value)
            {
                this.topValue = value ?? throw new ArgumentNullException(nameof(value)); // LUCENENET specific - throw ArgumentNullException rather than getting a cast exception
            }

            // LUCENENET NOTE: This was value(int) in Lucene.
            public override J2N.Numerics.Double this[int slot] => values[slot]; // LUCENENET NOTE: Implicit cast will instantiate new instance

            public override int CompareTop(int doc)
            {
                double docValue = docVals.DoubleVal(doc);
                // LUCENENET specific - use JCG comparer to get the same logic as Java
                return JCG.Comparer<double>.Default.Compare(topValue, docValue);
            }
        }
    }
}