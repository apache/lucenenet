using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Sandbox.Queries
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
    /// SortField for <see cref="SortedSetDocValues"/>.
    /// <para/>
    /// A <see cref="SortedSetDocValues"/> contains multiple values for a field, so sorting with
    /// this technique "selects" a value as the representative sort value for the document.
    /// <para/>
    /// By default, the minimum value in the set is selected as the sort value, but
    /// this can be customized. Selectors other than the default do have some limitations
    /// (see below) to ensure that all selections happen in constant-time for performance.
    /// <para/>
    /// Like sorting by string, this also supports sorting missing values as first or last,
    /// via <see cref="SortField.SetMissingValue(object)"/>.
    /// <para/>
    /// Limitations:
    /// <list type="bullet">
    ///     <item><description>
    ///     Fields containing <see cref="int.MaxValue"/> or more unique values
    ///     are unsupported.
    ///     </description></item>
    ///     <item><description>
    ///     Selectors other than the default <see cref="Selector.MIN"/> require 
    ///     optional codec support. However several codecs provided by Lucene,
    ///     including the current default codec, support this.
    ///     </description></item>
    /// </list>
    /// </summary>
    public class SortedSetSortField : SortField
    {
        // LUCENENET NOTE: Selector enum moved outside of this class to prevent
        // naming conflicts.

        private readonly Selector selector;

        /// <summary>
        /// Creates a sort, possibly in reverse, by the minimum value in the set 
        /// for the document.
        /// </summary>
        /// <param name="field">Name of field to sort by.  Must not be null.</param>
        /// <param name="reverse">True if natural order should be reversed.</param>
        public SortedSetSortField(string field, bool reverse)
                  : this(field, reverse, Selector.MIN)
        {
        }

        /// <summary>
        /// Creates a sort, possibly in reverse, specifying how the sort value from 
        /// the document's set is selected.
        /// </summary>
        /// <param name="field">Name of field to sort by.  Must not be null.</param>
        /// <param name="reverse">True if natural order should be reversed.</param>
        /// <param name="selector">
        /// custom selector for choosing the sort value from the set.
        /// <para/>
        /// NOTE: selectors other than <see cref="Selector.MIN"/> require optional codec support.
        /// </param>
        public SortedSetSortField(string field, bool reverse, Selector selector)
            : base(field, SortFieldType.CUSTOM, reverse)
        {
            // LUCENENET NOTE: Selector enum cannot be null in .NET, so we avoid this issue by not making the parameter nullable
            //if (selector is null)
            //{
            //    throw new NullReferenceException();
            //}
            this.selector = selector;
        }

        /// <summary>Returns the selector in use for this sort</summary>
        public Selector Selector => selector;

        public override int GetHashCode()
        {
            return 31 * base.GetHashCode() + selector.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (!base.Equals(obj)) return false;
            if (GetType() != obj.GetType()) return false;
            SortedSetSortField other = (SortedSetSortField)obj;
            if (selector != other.selector) return false;
            return true;
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("<sortedset" + ": \"").Append(Field).Append("\">");
            if (IsReverse) buffer.Append('!');
            if (MissingValue != null)
            {
                buffer.Append(" missingValue=");
                buffer.Append(MissingValue);
            }
            buffer.Append(" selector=");
            buffer.Append(selector);

            return buffer.ToString();
        }

        /// <summary>
        /// Set how missing values (the empty set) are sorted.
        /// <para/>
        /// Note that this must be <see cref="SortField.STRING_FIRST"/> or 
        /// <see cref="SortField.STRING_LAST"/>.
        /// </summary>
        public override void SetMissingValue(object value)
        {
            if (value != STRING_FIRST && value != STRING_LAST)
            {
                throw new ArgumentException("For SORTED_SET type, missing value must be either STRING_FIRST or STRING_LAST");
            }
            base.m_missingValue = value;
        }

        private sealed class TermOrdValComparerAnonymousClass : FieldComparer.TermOrdValComparer
        {
            private readonly SortedSetSortField outerInstance;

            public TermOrdValComparerAnonymousClass(SortedSetSortField outerInstance, int numHits)
                : base(numHits, outerInstance.Field, outerInstance.m_missingValue == STRING_LAST)
            {
                this.outerInstance = outerInstance;
            }

            protected override SortedDocValues GetSortedDocValues(AtomicReaderContext context, string field)
            {
                SortedSetDocValues sortedSet = FieldCache.DEFAULT.GetDocTermOrds(context.AtomicReader, field);

                if (sortedSet.ValueCount >= int.MaxValue)
                {
                    throw UnsupportedOperationException.Create("fields containing more than " + (int.MaxValue - 1) + " unique terms are unsupported");
                }

                SortedDocValues singleton = DocValues.UnwrapSingleton(sortedSet);
                if (singleton != null)
                {
                    // it's actually single-valued in practice, but indexed as multi-valued,
                    // so just sort on the underlying single-valued dv directly.
                    // regardless of selector type, this optimization is safe!
                    return singleton;
                }
                else if (outerInstance.selector == Selector.MIN)
                {
                    return new MinValue(sortedSet);
                }
                else
                {
                    if (sortedSet is RandomAccessOrds == false)
                    {
                        throw UnsupportedOperationException.Create("codec does not support random access ordinals, cannot use selector: " + outerInstance.selector);
                    }
                    RandomAccessOrds randomOrds = (RandomAccessOrds)sortedSet;
                    switch (outerInstance.selector)
                    {
                        case Selector.MAX: return new MaxValue(randomOrds);
                        case Selector.MIDDLE_MIN: return new MiddleMinValue(randomOrds);
                        case Selector.MIDDLE_MAX: return new MiddleMaxValue(randomOrds);
                        case Selector.MIN:
                        default:
                            throw AssertionError.Create();
                    }
                }
            }
        }

        public override FieldComparer GetComparer(int numHits, int sortPos)
        {
            return new TermOrdValComparerAnonymousClass(this, numHits);
        }

        /// <summary>Wraps a <see cref="SortedSetDocValues"/> and returns the first ordinal (min)</summary>
        internal class MinValue : SortedDocValues
        {
            internal readonly SortedSetDocValues @in;

            internal MinValue(SortedSetDocValues @in)
            {
                this.@in = @in;
            }

            public override int GetOrd(int docID)
            {
                @in.SetDocument(docID);
                return (int)@in.NextOrd();
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                @in.LookupOrd(ord, result);
            }

            public override int ValueCount => (int)@in.ValueCount;

            public override int LookupTerm(BytesRef key)
            {
                return (int)@in.LookupTerm(key);
            }
        }

        /// <summary>Wraps a <see cref="SortedSetDocValues"/> and returns the last ordinal (max)</summary>
        internal class MaxValue : SortedDocValues
        {
            internal readonly RandomAccessOrds @in;

            internal MaxValue(RandomAccessOrds @in)
            {
                this.@in = @in;
            }

            public override int GetOrd(int docID)
            {
                @in.SetDocument(docID);
                int count = @in.Cardinality;
                if (count == 0)
                {
                    return -1;
                }
                else
                {
                    return (int)@in.OrdAt(count - 1);
                }
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                @in.LookupOrd(ord, result);
            }

            public override int ValueCount => (int)@in.ValueCount;

            public override int LookupTerm(BytesRef key)
            {
                return (int)@in.LookupTerm(key);
            }
        }

        /// <summary>Wraps a <see cref="SortedSetDocValues"/> and returns the middle ordinal (or min of the two)</summary>
        internal class MiddleMinValue : SortedDocValues
        {
            internal readonly RandomAccessOrds @in;

            internal MiddleMinValue(RandomAccessOrds @in)
            {
                this.@in = @in;
            }

            public override int GetOrd(int docID)
            {
                @in.SetDocument(docID);
                int count = @in.Cardinality;
                if (count == 0)
                {
                    return -1;
                }
                else
                {
                    return (int)@in.OrdAt((count - 1).TripleShift(1));
                }
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                @in.LookupOrd(ord, result);
            }

            public override int ValueCount => (int)@in.ValueCount;

            public override int LookupTerm(BytesRef key)
            {
                return (int)@in.LookupTerm(key);
            }
        }

        /// <summary>Wraps a <see cref="SortedSetDocValues"/> and returns the middle ordinal (or max of the two)</summary>
        internal class MiddleMaxValue : SortedDocValues
        {
            internal readonly RandomAccessOrds @in;

            internal MiddleMaxValue(RandomAccessOrds @in)
            {
                this.@in = @in;
            }

            public override int GetOrd(int docID)
            {
                @in.SetDocument(docID);
                int count = @in.Cardinality;
                if (count == 0)
                {
                    return -1;
                }
                else
                {
                    return (int)@in.OrdAt(count.TripleShift(1));
                }
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                @in.LookupOrd(ord, result);
            }

            public override int ValueCount => (int)@in.ValueCount;

            public override int LookupTerm(BytesRef key)
            {
                return (int)@in.LookupTerm(key);
            }
        }
    }

    /// <summary>Selects a value from the document's set to use as the sort value</summary>
    public enum Selector
    {
        /// <summary>
        /// Selects the minimum value in the set 
        /// </summary>
        MIN,
        /// <summary>
        /// Selects the maximum value in the set 
        /// </summary>
        MAX,
        /// <summary>
        /// Selects the middle value in the set.
        /// <para/>
        /// If the set has an even number of values, the lower of the middle two is chosen.
        /// </summary>
        MIDDLE_MIN,
        /// <summary>
        /// Selects the middle value in the set.
        /// <para/>
        /// If the set has an even number of values, the higher of the middle two is chosen
        /// </summary>
        MIDDLE_MAX
    }
}
