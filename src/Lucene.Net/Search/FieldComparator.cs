using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using IBits = Lucene.Net.Util.IBits;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;

    /// <summary>
    /// Expert: a <see cref="FieldComparer"/> compares hits so as to determine their
    /// sort order when collecting the top results with
    /// <see cref="TopFieldCollector"/>.  The concrete public <see cref="FieldComparer"/>
    /// classes here correspond to the <see cref="SortField"/> types.
    ///
    /// <para>This API is designed to achieve high performance
    /// sorting, by exposing a tight interaction with 
    /// <see cref="FieldValueHitQueue"/> as it visits hits.  Whenever a hit is
    /// competitive, it's enrolled into a virtual slot, which is
    /// an <see cref="int"/> ranging from 0 to numHits-1.  The 
    /// <see cref="FieldComparer"/> is made aware of segment transitions
    /// during searching in case any internal state it's tracking
    /// needs to be recomputed during these transitions.</para>
    ///
    /// <para>A comparer must define these functions:</para>
    ///
    /// <list type="bullet">
    ///
    ///  <item><term><see cref="Compare(int, int)"/></term> <description> Compare a hit at 'slot a'
    ///       with hit 'slot b'.</description></item>
    ///
    ///  <item><term><see cref="SetBottom(int)"/></term> <description>This method is called by
    ///       <see cref="FieldValueHitQueue"/> to notify the
    ///       <see cref="FieldComparer"/> of the current weakest ("bottom")
    ///       slot.  Note that this slot may not hold the weakest
    ///       value according to your comparer, in cases where
    ///       your comparer is not the primary one (ie, is only
    ///       used to break ties from the comparers before it).</description></item>
    ///
    ///  <item><term><see cref="CompareBottom(int)"/></term> <description>Compare a new hit (docID)
    ///       against the "weakest" (bottom) entry in the queue.</description></item>
    ///
    ///  <item><term><see cref="SetTopValue(object)"/></term> <description>This method is called by
    ///       <see cref="TopFieldCollector"/> to notify the
    ///       <see cref="FieldComparer"/> of the top most value, which is
    ///       used by future calls to <see cref="CompareTop(int)"/>.</description></item>
    ///
    ///  <item><term><see cref="CompareTop(int)"/></term> <description>Compare a new hit (docID)
    ///       against the top value previously set by a call to
    ///       <see cref="SetTopValue(object)"/>.</description></item>
    ///
    ///  <item><term><see cref="Copy(int, int)"/></term> <description>Installs a new hit into the
    ///       priority queue.  The <see cref="FieldValueHitQueue"/>
    ///       calls this method when a new hit is competitive.</description></item>
    ///
    ///  <item><term><see cref="SetNextReader(AtomicReaderContext)"/></term> <description>Invoked
    ///       when the search is switching to the next segment.
    ///       You may need to update internal state of the
    ///       comparer, for example retrieving new values from
    ///       the <see cref="IFieldCache"/>.</description></item>
    ///
    ///  <item><term><see cref="FieldComparer.this[int]"/></term> <description>Return the sort value stored in
    ///       the specified slot.  This is only called at the end
    ///       of the search, in order to populate
    ///       <see cref="FieldDoc.Fields"/> when returning the top results.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class FieldComparer<T> : FieldComparer
    {
        /// <summary>
        /// Compare hit at <paramref name="slot1"/> with hit at <paramref name="slot2"/>.
        /// </summary>
        /// <param name="slot1"> first slot to compare </param>
        /// <param name="slot2"> second slot to compare </param>
        /// <returns> any N &lt; 0 if <paramref name="slot2"/>'s value is sorted after
        /// <paramref name="slot1"/>, any N &gt; 0 if the <paramref name="slot2"/>'s value is sorted before
        /// <paramref name="slot1"/> and 0 if they are equal </returns>
        public abstract override int Compare(int slot1, int slot2);

        /// <summary>
        /// Set the bottom slot, ie the "weakest" (sorted last)
        /// entry in the queue.  When <see cref="CompareBottom(int)"/> is
        /// called, you should compare against this slot.  This
        /// will always be called before <see cref="CompareBottom(int)"/>.
        /// </summary>
        /// <param name="slot"> the currently weakest (sorted last) slot in the queue </param>
        public abstract override void SetBottom(int slot);

        /// <summary>
        /// Record the top value, for future calls to 
        /// <see cref="CompareTop(int)"/>.  This is only called for searches that
        /// use SearchAfter (deep paging), and is called before any
        /// calls to <see cref="SetNextReader(AtomicReaderContext)"/>.
        /// </summary>
        public abstract override void SetTopValue(object value);

        /// <summary>
        /// Compare the bottom of the queue with this doc.  This will
        /// only invoked after <see cref="SetBottom(int)"/> has been called.  This
        /// should return the same result as 
        /// <see cref="Compare(int, int)"/> as if bottom were slot1 and the new
        /// document were slot 2.
        ///
        /// <para>For a search that hits many results, this method
        /// will be the hotspot (invoked by far the most
        /// frequently).</para>
        /// </summary>
        /// <param name="doc"> Doc that was hit </param>
        /// <returns> Any N &lt; 0 if the doc's value is sorted after
        /// the bottom entry (not competitive), any N &gt; 0 if the
        /// doc's value is sorted before the bottom entry and 0 if
        /// they are equal. </returns>
        public abstract override int CompareBottom(int doc);

        /// <summary>
        /// Compare the top value with this doc.  This will
        /// only invoked after <see cref="SetTopValue(object)"/> has been called.  This
        /// should return the same result as 
        /// <see cref="Compare(int, int)"/> as if topValue were slot1 and the new
        /// document were slot 2.  This is only called for searches that
        /// use SearchAfter (deep paging).
        /// </summary>
        /// <param name="doc"> Doc that was hit </param>
        /// <returns> Any N &lt; 0 if the doc's value is sorted after
        /// the bottom entry (not competitive), any N &gt; 0 if the
        /// doc's value is sorted before the bottom entry and 0 if
        /// they are equal. </returns>
        public abstract override int CompareTop(int doc);

        /// <summary>
        /// This method is called when a new hit is competitive.
        /// You should copy any state associated with this document
        /// that will be required for future comparisons, into the
        /// specified slot.
        /// </summary>
        /// <param name="slot"> Which slot to copy the hit to </param>
        /// <param name="doc"> DocID relative to current reader </param>
        public abstract override void Copy(int slot, int doc);

        /// <summary>
        /// Set a new <see cref="AtomicReaderContext"/>. All subsequent docIDs are relative to
        /// the current reader (you must add docBase if you need to
        /// map it to a top-level docID).
        /// </summary>
        /// <param name="context"> Current reader context </param>
        /// <returns> The comparer to use for this segment; most
        ///   comparers can just return "this" to reuse the same
        ///   comparer across segments </returns>
        /// <exception cref="IOException"> If there is a low-level IO error </exception>
        public abstract override FieldComparer SetNextReader(AtomicReaderContext context);

        /// <summary>
        /// Returns -1 if first is less than second.  Default
        /// impl to assume the type implements <see cref="IComparable{T}"/> and
        /// invoke <see cref="IComparable{T}.CompareTo(T)"/>; be sure to override this method if
        /// your FieldComparer's type isn't a <see cref="IComparable{T}"/> or
        /// if your values may sometimes be <c>null</c>
        /// </summary>
        public virtual int CompareValues(T first, T second)
        {
            if (object.ReferenceEquals(first, default(T)))
            {
                return object.ReferenceEquals(second, default(T)) ? 0 : -1;
            }
            else if (object.ReferenceEquals(second, default(T)))
            {
                return 1;
            }
            else
            {
                // LUCENENET NOTE: We need to compare using JCG.Comparer<T>.Default
                // to ensure that if comparing strings we do so in Ordinal sort order
                return JCG.Comparer<T>.Default.Compare(first, second);
            }
        }

        /// <summary>
        /// Returns -1 if first is less than second.  Default
        /// impl to assume the type implements <see cref="IComparable{T}"/> and
        /// invoke <see cref="IComparable{T}.CompareTo(T)"/>; be sure to override this method if
        /// your FieldComparer's type isn't a <see cref="IComparable{T}"/> or
        /// if your values may sometimes be <c>null</c>
        /// </summary>
        public override int CompareValues(object first, object second)
        {
            return CompareValues((T)first, (T)second);
        }
    }

    // .NET Port: Using a non-generic class here so that we avoid having to use the
    // type parameter to access these nested types. Also moving non-generic methods here for casting without generics.
    public abstract class FieldComparer
    {
        public abstract int CompareValues(object first, object second);

        //Set up abstract methods
        /// <summary>
        /// Compare hit at <paramref name="slot1"/> with hit at <paramref name="slot2"/>.
        /// </summary>
        /// <param name="slot1"> first slot to compare </param>
        /// <param name="slot2"> second slot to compare </param>
        /// <returns> any N &lt; 0 if <paramref name="slot2"/>'s value is sorted after
        /// <paramref name="slot1"/>, any N &gt; 0 if the <paramref name="slot2"/>'s value is sorted before
        /// <paramref name="slot1"/> and 0 if they are equal </returns>
        public abstract int Compare(int slot1, int slot2);

        /// <summary>
        /// Set the bottom slot, ie the "weakest" (sorted last)
        /// entry in the queue.  When <see cref="CompareBottom(int)"/> is
        /// called, you should compare against this slot.  This
        /// will always be called before <see cref="CompareBottom(int)"/>.
        /// </summary>
        /// <param name="slot"> The currently weakest (sorted last) slot in the queue </param>
        public abstract void SetBottom(int slot);

        /// <summary>
        /// Record the top value, for future calls to 
        /// <see cref="CompareTop(int)"/>.  This is only called for searches that
        /// use SearchAfter (deep paging), and is called before any
        /// calls to <see cref="SetNextReader(AtomicReaderContext)"/>.
        /// </summary>
        public abstract void SetTopValue(object value);

        /// <summary>
        /// Compare the bottom of the queue with this doc.  This will
        /// only invoked after setBottom has been called.  This
        /// should return the same result as 
        /// <see cref="Compare(int, int)"/> as if bottom were slot1 and the new
        /// document were slot 2.
        ///
        /// <para>For a search that hits many results, this method
        /// will be the hotspot (invoked by far the most
        /// frequently).</para>
        /// </summary>
        /// <param name="doc"> Doc that was hit </param>
        /// <returns> Any N &lt; 0 if the doc's value is sorted after
        /// the bottom entry (not competitive), any N &gt; 0 if the
        /// doc's value is sorted before the bottom entry and 0 if
        /// they are equal. </returns>
        public abstract int CompareBottom(int doc);

        /// <summary>
        /// Compare the top value with this doc.  This will
        /// only invoked after <see cref="SetTopValue(object)"/> has been called.  This
        /// should return the same result as 
        /// <see cref="Compare(int, int)"/> as if topValue were slot1 and the new
        /// document were slot 2.  This is only called for searches that
        /// use SearchAfter (deep paging).
        /// </summary>
        /// <param name="doc"> Doc that was hit </param>
        /// <returns> Any N &lt; 0 if the doc's value is sorted after
        /// the bottom entry (not competitive), any N &gt; 0 if the
        /// doc's value is sorted before the bottom entry and 0 if
        /// they are equal. </returns>
        public abstract int CompareTop(int doc);

        /// <summary>
        /// This method is called when a new hit is competitive.
        /// You should copy any state associated with this document
        /// that will be required for future comparisons, into the
        /// specified slot.
        /// </summary>
        /// <param name="slot"> Which slot to copy the hit to </param>
        /// <param name="doc"> DocID relative to current reader </param>
        public abstract void Copy(int slot, int doc);

        /// <summary>
        /// Set a new <see cref="AtomicReaderContext"/>. All subsequent docIDs are relative to
        /// the current reader (you must add docBase if you need to
        /// map it to a top-level docID).
        /// </summary>
        /// <param name="context"> Current reader context </param>
        /// <returns> The comparer to use for this segment; most
        ///   comparers can just return "this" to reuse the same
        ///   comparer across segments </returns>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public abstract FieldComparer SetNextReader(AtomicReaderContext context);

        /// <summary>
        /// Sets the <see cref="Scorer"/> to use in case a document's score is
        /// needed.
        /// </summary>
        /// <param name="scorer"> <see cref="Scorer"/> instance that you should use to
        /// obtain the current hit's score, if necessary.  </param>
        public virtual void SetScorer(Scorer scorer)
        {
            // Empty implementation since most comparers don't need the score. this
            // can be overridden by those that need it.
        }

        /// <summary>
        /// Return the actual value in the slot.
        /// LUCENENET NOTE: This was value(int) in Lucene.
        /// </summary>
        /// <param name="slot"> The value </param>
        /// <returns> Value in this slot </returns>
        public abstract IComparable this[int slot] { get; }

        /// <summary>
        /// Base FieldComparer class for numeric types
        /// </summary>
        public abstract class NumericComparer<T> : FieldComparer<T>
            where T : struct
        {
            protected readonly T? m_missingValue;
            protected readonly string m_field;
            protected IBits m_docsWithField;

            public NumericComparer(string field, T? missingValue)
            {
                this.m_field = field;
                this.m_missingValue = missingValue;
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                if (m_missingValue is object)
                {
                    m_docsWithField = FieldCache.DEFAULT.GetDocsWithField((context.AtomicReader), m_field);
                    // optimization to remove unneeded checks on the bit interface:
                    if (m_docsWithField is Lucene.Net.Util.Bits.MatchAllBits)
                    {
                        m_docsWithField = null;
                    }
                }
                else
                {
                    m_docsWithField = null;
                }
                return this;
            }
        }

        /// <summary>
        /// Parses field's values as <see cref="byte"/> (using 
        /// <see cref="IFieldCache.GetBytes(Index.AtomicReader, string, FieldCache.IByteParser, bool)"/> and sorts by ascending value
        /// </summary>
        [Obsolete, CLSCompliant(false)] // LUCENENET NOTE: marking non-CLS compliant because of sbyte - it is obsolete, anyway
        public sealed class ByteComparer : NumericComparer<sbyte>
        {
            private readonly sbyte[] values;
            private readonly FieldCache.IByteParser parser; 
            private FieldCache.Bytes currentReaderValues;
            private sbyte bottom;
            private sbyte topValue;

            internal ByteComparer(int numHits, string field, FieldCache.IParser parser, sbyte? missingValue)
                : base(field, missingValue)
            {
                values = new sbyte[numHits];
                this.parser = (FieldCache.IByteParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                // LUCENENET NOTE: Same logic as the Byte.compare() method in Java
                return JCG.Comparer<sbyte>.Default.Compare(values[slot1], values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                sbyte v2 = (sbyte)currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }
                // LUCENENET NOTE: Same logic as the Byte.compare() method in Java
                return JCG.Comparer<sbyte>.Default.Compare(bottom, v2);
            }

            public override void Copy(int slot, int doc)
            {
                sbyte v2 = (sbyte)currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }
                values[slot] = v2;
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetBytes((context.AtomicReader), m_field, parser, m_missingValue is object);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int slot)
            {
                this.bottom = values[slot];
            }

            public override void SetTopValue(object value)
            {
                topValue = (sbyte)value;
            }

            public override IComparable this[int slot] => values[slot];

            public override int CompareTop(int doc)
            {
                sbyte docValue = (sbyte)currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && docValue == 0 && !m_docsWithField.Get(doc))
                {
                    docValue = m_missingValue.GetValueOrDefault();
                }
                // LUCENENET NOTE: Same logic as the Byte.compare() method in Java
                return JCG.Comparer<sbyte>.Default.Compare(topValue, docValue);
            }
        }

        /// <summary>
        /// Parses field's values as <see cref="double"/> (using 
        /// <see cref="IFieldCache.GetDoubles(Index.AtomicReader, string, FieldCache.IDoubleParser, bool)"/> and sorts by ascending value
        /// </summary>
        public sealed class DoubleComparer : NumericComparer<double>
        {
            private readonly double[] values;
            private readonly FieldCache.IDoubleParser parser;
            private FieldCache.Doubles currentReaderValues;
            private double bottom;
            private double topValue;

            internal DoubleComparer(int numHits, string field, FieldCache.IParser parser, double? missingValue)
                : base(field, missingValue)
            {
                values = new double[numHits];
                this.parser = (FieldCache.IDoubleParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                double v1 = values[slot1];
                double v2 = values[slot2];

                // LUCENENET specific special case:
                // In case of zero, we may have a "positive 0" or "negative 0"
                // to tie-break. So, we use JCG.Comparer<double> to do the comparison.
                return JCG.Comparer<double>.Default.Compare(v1, v2);
            }

            public override int CompareBottom(int doc)
            {
                double v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0.0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }

                // LUCENENET specific special case:
                // In case of zero, we may have a "positive 0" or "negative 0"
                // to tie-break. So, we use JCG.Comparer<double> to do the comparison.
                return JCG.Comparer<double>.Default.Compare(bottom, v2);
            }

            public override void Copy(int slot, int doc)
            {
                double v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0.0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }

                values[slot] = v2;
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetDoubles((context.AtomicReader), m_field, parser, m_missingValue is object);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int slot)
            {
                this.bottom = values[slot];
            }

            public override void SetTopValue(object value)
            {
                topValue = (double)value;
            }

            public override IComparable this[int slot] => values[slot];

            public override int CompareTop(int doc)
            {
                double docValue = currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && docValue == 0 && !m_docsWithField.Get(doc))
                {
                    docValue = m_missingValue.GetValueOrDefault();
                }

                // LUCENENET specific special case:
                // In case of zero, we may have a "positive 0" or "negative 0"
                // to tie-break. So, we use JCG.Comparer<double> to do the comparison.
                return JCG.Comparer<double>.Default.Compare(topValue, docValue);
            }
        }

        /// <summary>
        /// Parses field's values as <see cref="float"/> (using 
        /// <see cref="IFieldCache.GetSingles(Index.AtomicReader, string, FieldCache.ISingleParser, bool)"/>  and sorts by ascending value
        /// <para/>
        /// NOTE: This was FloatComparator in Lucene
        /// </summary>
        public sealed class SingleComparer : NumericComparer<float>
        {
            private readonly float[] values;
            private readonly FieldCache.ISingleParser parser;
            private FieldCache.Singles currentReaderValues;
            private float bottom;
            private float topValue;

            internal SingleComparer(int numHits, string field, FieldCache.IParser parser, float? missingValue)
                : base(field, missingValue)
            {
                values = new float[numHits];
                this.parser = (FieldCache.ISingleParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                float v1 = values[slot1];
                float v2 = values[slot2];

                // LUCENENET specific special case:
                // In case of zero, we may have a "positive 0" or "negative 0"
                // to tie-break. So, we use JCG.Comparer<double> to do the comparison.
                return JCG.Comparer<float>.Default.Compare(v1, v2);
            }

            public override int CompareBottom(int doc)
            {
                // TODO: are there sneaky non-branch ways to compute sign of float?
                float v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }

                // LUCENENET specific special case:
                // In case of zero, we may have a "positive 0" or "negative 0"
                // to tie-break. So, we use JCG.Comparer<double> to do the comparison.
                return JCG.Comparer<float>.Default.Compare(bottom, v2);
            }

            public override void Copy(int slot, int doc)
            {
                float v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }

                values[slot] = v2;
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetSingles((context.AtomicReader), m_field, parser, m_missingValue is object);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int slot)
            {
                this.bottom = values[slot];
            }

            public override void SetTopValue(object value)
            {
                topValue = (float)value;
            }

            public override IComparable this[int slot] => values[slot];

            public override int CompareTop(int doc)
            {
                float docValue = currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && docValue == 0 && !m_docsWithField.Get(doc))
                {
                    docValue = m_missingValue.GetValueOrDefault();
                }

                // LUCENENET specific special case:
                // In case of zero, we may have a "positive 0" or "negative 0"
                // to tie-break. So, we use JCG.Comparer<double> to do the comparison.
                return JCG.Comparer<float>.Default.Compare(topValue, docValue);
            }
        }

        /// <summary>
        /// Parses field's values as <see cref="short"/> (using 
        /// <see cref="IFieldCache.GetInt16s(Index.AtomicReader, string, FieldCache.IInt16Parser, bool)"/> and sorts by ascending value
        /// <para/>
        /// NOTE: This was ShortComparator in Lucene
        /// </summary>
        [Obsolete]
        public sealed class Int16Comparer : NumericComparer<short>
        {
            private readonly short[] values;
            private readonly FieldCache.IInt16Parser parser;
            private FieldCache.Int16s currentReaderValues;
            private short bottom;
            private short topValue;

            internal Int16Comparer(int numHits, string field, FieldCache.IParser parser, short? missingValue)
                : base(field, missingValue)
            {
                values = new short[numHits];
                this.parser = (FieldCache.IInt16Parser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                // LUCENENET NOTE: Same logic as the Byte.compare() method in Java
                return JCG.Comparer<short>.Default.Compare(values[slot1], values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                short v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }

                // LUCENENET NOTE: Same logic as the Byte.compare() method in Java
                return JCG.Comparer<short>.Default.Compare(bottom, v2);
            }

            public override void Copy(int slot, int doc)
            {
                short v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }

                values[slot] = v2;
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetInt16s((context.AtomicReader), m_field, parser, m_missingValue is object);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int slot)
            {
                this.bottom = values[slot];
            }

            public override void SetTopValue(object value)
            {
                topValue = (short)value;
            }

            public override IComparable this[int slot] => values[slot];

            public override int CompareTop(int doc)
            {
                short docValue = currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && docValue == 0 && !m_docsWithField.Get(doc))
                {
                    docValue = m_missingValue.GetValueOrDefault();
                }
                return JCG.Comparer<short>.Default.Compare(topValue, docValue);
            }
        }

        /// <summary>
        /// Parses field's values as <see cref="int"/> (using 
        /// <see cref="IFieldCache.GetInt32s(Index.AtomicReader, string, FieldCache.IInt32Parser, bool)"/> and sorts by ascending value
        /// <para/>
        /// NOTE: This was IntComparator in Lucene
        /// </summary>
        public sealed class Int32Comparer : NumericComparer<int>
        {
            private readonly int[] values;
            private readonly FieldCache.IInt32Parser parser;
            private FieldCache.Int32s currentReaderValues;
            private int bottom; // Value of bottom of queue
            private int topValue;

            internal Int32Comparer(int numHits, string field, FieldCache.IParser parser, int? missingValue)
                : base(field, missingValue)
            {
                values = new int[numHits];
                this.parser = (FieldCache.IInt32Parser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                return JCG.Comparer<int>.Default.Compare(values[slot1], values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                int v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }
                return JCG.Comparer<int>.Default.Compare(bottom, v2);
            }

            public override void Copy(int slot, int doc)
            {
                int v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }

                values[slot] = v2;
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetInt32s((context.AtomicReader), m_field, parser, m_missingValue is object);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int slot)
            {
                this.bottom = values[slot];
            }

            public override void SetTopValue(object value)
            {
                topValue = (int)value;
            }

            public override IComparable this[int slot] => values[slot];

            public override int CompareTop(int doc)
            {
                int docValue = currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && docValue == 0 && !m_docsWithField.Get(doc))
                {
                    docValue = m_missingValue.GetValueOrDefault();
                }
                return JCG.Comparer<int>.Default.Compare(topValue, docValue);
            }
        }

        /// <summary>
        /// Parses field's values as <see cref="long"/> (using
        /// <see cref="IFieldCache.GetInt64s(Index.AtomicReader, string, FieldCache.IInt64Parser, bool)"/> and sorts by ascending value
        /// <para/>
        /// NOTE: This was LongComparator in Lucene
        /// </summary>
        public sealed class Int64Comparer : NumericComparer<long>
        {
            private readonly long[] values;
            private readonly FieldCache.IInt64Parser parser;
            private FieldCache.Int64s currentReaderValues;
            private long bottom;
            private long topValue;

            internal Int64Comparer(int numHits, string field, FieldCache.IParser parser, long? missingValue)
                : base(field, missingValue)
            {
                values = new long[numHits];
                this.parser = (FieldCache.IInt64Parser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                // LUCENENET NOTE: Same logic as the Long.compare() method in Java
                return JCG.Comparer<long>.Default.Compare(values[slot1], values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                // TODO: there are sneaky non-branch ways to compute
                // -1/+1/0 sign
                long v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }

                // LUCENENET NOTE: Same logic as the Long.compare() method in Java
                return JCG.Comparer<long>.Default.Compare(bottom, v2);
            }

            public override void Copy(int slot, int doc)
            {
                long v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && v2 == 0 && !m_docsWithField.Get(doc))
                {
                    v2 = m_missingValue.GetValueOrDefault();
                }

                values[slot] = v2;
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetInt64s((context.AtomicReader), m_field, parser, m_missingValue is object);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int slot)
            {
                this.bottom = values[slot];
            }

            public override void SetTopValue(object value)
            {
                topValue = (long)value;
            }

            public override IComparable this[int slot] => values[slot];

            public override int CompareTop(int doc)
            {
                long docValue = currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (m_docsWithField is object && docValue == 0 && !m_docsWithField.Get(doc))
                {
                    docValue = m_missingValue.GetValueOrDefault();
                }
                return JCG.Comparer<long>.Default.Compare(topValue, docValue);
            }
        }

        /// <summary>
        /// Sorts by descending relevance.  NOTE: if you are
        /// sorting only by descending relevance and then
        /// secondarily by ascending docID, performance is faster
        /// using <see cref="TopScoreDocCollector"/> directly (which all overloads of
        /// <see cref="IndexSearcher.Search(Query, int)"/> use when no <see cref="Sort"/> is
        /// specified).
        /// </summary>
        public sealed class RelevanceComparer : FieldComparer<float>
        {
            private readonly float[] scores;
            private float bottom;
            private Scorer scorer;
            private float topValue;

            internal RelevanceComparer(int numHits)
            {
                scores = new float[numHits];
            }

            public override int Compare(int slot1, int slot2)
            {
                // LUCENENET specific special case:
                // In case of zero, we may have a "positive 0" or "negative 0"
                // to tie-break. So, we use JCG.Comparer<double> to do the comparison.
                return JCG.Comparer<float>.Default.Compare(scores[slot2], scores[slot1]);
            }

            public override int CompareBottom(int doc)
            {
                float score = scorer.GetScore();
                if (Debugging.AssertsEnabled) Debugging.Assert(!float.IsNaN(score));

                // LUCENENET specific special case:
                // In case of zero, we may have a "positive 0" or "negative 0"
                // to tie-break. So, we use JCG.Comparer<double> to do the comparison.
                return JCG.Comparer<float>.Default.Compare(score, bottom);
            }

            public override void Copy(int slot, int doc)
            {
                scores[slot] = scorer.GetScore();
                if (Debugging.AssertsEnabled) Debugging.Assert(!float.IsNaN(scores[slot]));
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                return this;
            }

            public override void SetBottom(int slot)
            {
                this.bottom = scores[slot];
            }

            public override void SetTopValue(object value)
            {
                topValue = (float)value;
            }

            public override void SetScorer(Scorer scorer)
            {
                // wrap with a ScoreCachingWrappingScorer so that successive calls to
                // score() will not incur score computation over and
                // over again.
                if (!(scorer is ScoreCachingWrappingScorer))
                {
                    this.scorer = new ScoreCachingWrappingScorer(scorer);
                }
                else
                {
                    this.scorer = scorer;
                }
            }

            public override IComparable this[int slot] => scores[slot];

            // Override because we sort reverse of natural Float order:
            public override int CompareValues(float first, float second)
            {
                // Reversed intentionally because relevance by default
                // sorts descending:

                // LUCENENET specific special case:
                // In case of zero, we may have a "positive 0" or "negative 0"
                // to tie-break. So, we use JCG.Comparer<float> to do the comparison.
                return JCG.Comparer<float>.Default.Compare(second, first);
            }

            public override int CompareTop(int doc)
            {
                float docValue = scorer.GetScore();
                if (Debugging.AssertsEnabled) Debugging.Assert(!float.IsNaN(docValue));

                // LUCENENET specific special case:
                // In case of zero, we may have a "positive 0" or "negative 0"
                // to tie-break. So, we use JCG.Comparer<float> to do the comparison.
                return JCG.Comparer<float>.Default.Compare(docValue, topValue);
            }
        }

        /// <summary>
        /// Sorts by ascending docID </summary>
        public sealed class DocComparer : FieldComparer<int?>
        {
            private readonly int[] docIDs;
            private int docBase;
            private int bottom;
            private int topValue;

            internal DocComparer(int numHits)
            {
                docIDs = new int[numHits];
            }

            public override int Compare(int slot1, int slot2)
            {
                // No overflow risk because docIDs are non-negative
                return docIDs[slot1] - docIDs[slot2];
            }

            public override int CompareBottom(int doc)
            {
                // No overflow risk because docIDs are non-negative
                return bottom - (docBase + doc);
            }

            public override void Copy(int slot, int doc)
            {
                docIDs[slot] = docBase + doc;
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                // TODO: can we "map" our docIDs to the current
                // reader? saves having to then subtract on every
                // compare call
                this.docBase = context.DocBase;
                return this;
            }

            public override void SetBottom(int slot)
            {
                this.bottom = docIDs[slot];
            }

            public override void SetTopValue(object value)
            {
                topValue = (int)value;
            }

            public override IComparable this[int slot] => docIDs[slot];

            public override int CompareTop(int doc)
            {
                int docValue = docBase + doc;
                // LUCENENET NOTE: Same logic as the Integer.compare() method in Java
                return JCG.Comparer<int>.Default.Compare(topValue, docValue);
            }
        }

        /// <summary>
        /// Sorts by field's natural <see cref="Index.Term"/> sort order, using
        /// ordinals.  This is functionally equivalent to 
        /// <see cref="Lucene.Net.Search.FieldComparer.TermValComparer"/>, but it first resolves the string
        /// to their relative ordinal positions (using the index
        /// returned by <see cref="IFieldCache.GetTermsIndex(Index.AtomicReader, string, float)"/>), and
        /// does most comparisons using the ordinals.  For medium
        /// to large results, this comparer will be much faster
        /// than <see cref="Lucene.Net.Search.FieldComparer.TermValComparer"/>.  For very small
        /// result sets it may be slower.
        /// </summary>
        public class TermOrdValComparer : FieldComparer<BytesRef>
        {
            /// <summary>
            /// Ords for each slot.
            /// <para/>
            /// @lucene.internal
            /// </summary>
            internal readonly int[] ords;

            /// <summary>
            /// Values for each slot.
            /// <para/>
            /// @lucene.internal
            /// </summary>
            internal readonly BytesRef[] values;

            /// <summary>
            /// Which reader last copied a value into the slot. When
            /// we compare two slots, we just compare-by-ord if the
            /// readerGen is the same; else we must compare the
            /// values(slower).
            /// <para/>
            /// @lucene.internal
            /// </summary>
            internal readonly int[] readerGen;

            /// <summary>
            /// Gen of current reader we are on.
            /// <para/>
            /// @lucene.internal
            /// </summary>
            internal int currentReaderGen = -1;

            /// <summary>
            /// Current reader's doc ord/values.
            /// <para/>
            /// @lucene.internal
            /// </summary>
            internal SortedDocValues termsIndex;

            internal readonly string field;

            /// <summary>
            /// Bottom slot, or -1 if queue isn't full yet
            /// <para/>
            /// @lucene.internal
            /// </summary>
            internal int bottomSlot = -1;

            /// <summary>
            /// Bottom ord (same as ords[bottomSlot] once bottomSlot
            /// is set).  Cached for faster compares.
            /// <para/>
            /// @lucene.internal
            /// </summary>
            internal int bottomOrd;

            /// <summary>
            /// True if current bottom slot matches the current
            /// reader.
            /// <para/>
            /// @lucene.internal
            /// </summary>
            internal bool bottomSameReader;

            /// <summary>
            /// Bottom value (same as values[bottomSlot] once
            /// bottomSlot is set).  Cached for faster compares.
            /// <para/>
            /// @lucene.internal
            /// </summary>
            internal BytesRef bottomValue;

            /// <summary>
            /// Set by setTopValue. </summary>
            internal BytesRef topValue;

            internal bool topSameReader;
            internal int topOrd;

            internal readonly BytesRef tempBR = new BytesRef();

            /// <summary>
            /// -1 if missing values are sorted first, 1 if they are
            ///  sorted last
            /// </summary>
            internal readonly int missingSortCmp;

            /// <summary>
            /// Which ordinal to use for a missing value. </summary>
            internal readonly int missingOrd;

            /// <summary>
            /// Creates this, sorting missing values first. </summary>
            public TermOrdValComparer(int numHits, string field)
                : this(numHits, field, false)
            {
            }

            /// <summary>
            /// Creates this, with control over how missing values
            /// are sorted.  Pass true for <paramref name="sortMissingLast"/> to put
            /// missing values at the end.
            /// </summary>
            public TermOrdValComparer(int numHits, string field, bool sortMissingLast)
            {
                ords = new int[numHits];
                values = new BytesRef[numHits];
                readerGen = new int[numHits];
                this.field = field;
                if (sortMissingLast)
                {
                    missingSortCmp = 1;
                    missingOrd = int.MaxValue;
                }
                else
                {
                    missingSortCmp = -1;
                    missingOrd = -1;
                }
            }

            public override int Compare(int slot1, int slot2)
            {
                if (readerGen[slot1] == readerGen[slot2])
                {
                    return ords[slot1] - ords[slot2];
                }

                BytesRef val1 = values[slot1];
                BytesRef val2 = values[slot2];
                if (val1 is null)
                {
                    if (val2 is null)
                    {
                        return 0;
                    }
                    return missingSortCmp;
                }
                else if (val2 is null)
                {
                    return -missingSortCmp;
                }
                return val1.CompareTo(val2);
            }

            public override int CompareBottom(int doc)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(bottomSlot != -1);
                int docOrd = termsIndex.GetOrd(doc);
                if (docOrd == -1)
                {
                    docOrd = missingOrd;
                }
                if (bottomSameReader)
                {
                    // ord is precisely comparable, even in the equal case
                    return bottomOrd - docOrd;
                }
                else if (bottomOrd >= docOrd)
                {
                    // the equals case always means bottom is > doc
                    // (because we set bottomOrd to the lower bound in
                    // setBottom):
                    return 1;
                }
                else
                {
                    return -1;
                }
            }

            public override void Copy(int slot, int doc)
            {
                int ord = termsIndex.GetOrd(doc);
                if (ord == -1)
                {
                    ord = missingOrd;
                    values[slot] = null;
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(ord >= 0);
                    if (values[slot] is null)
                    {
                        values[slot] = new BytesRef();
                    }
                    termsIndex.LookupOrd(ord, values[slot]);
                }
                ords[slot] = ord;
                readerGen[slot] = currentReaderGen;
            }

            /// <summary>
            /// Retrieves the <see cref="SortedDocValues"/> for the field in this segment </summary>
            protected virtual SortedDocValues GetSortedDocValues(AtomicReaderContext context, string field)
            {
                return FieldCache.DEFAULT.GetTermsIndex((context.AtomicReader), field);
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                termsIndex = GetSortedDocValues(context, field);
                currentReaderGen++;

                if (topValue is object)
                {
                    // Recompute topOrd/SameReader
                    int ord = termsIndex.LookupTerm(topValue);
                    if (ord >= 0)
                    {
                        topSameReader = true;
                        topOrd = ord;
                    }
                    else
                    {
                        topSameReader = false;
                        topOrd = -ord - 2;
                    }
                }
                else
                {
                    topOrd = missingOrd;
                    topSameReader = true;
                }
                //System.out.println("  setNextReader topOrd=" + topOrd + " topSameReader=" + topSameReader);

                if (bottomSlot != -1)
                {
                    // Recompute bottomOrd/SameReader
                    SetBottom(bottomSlot);
                }

                return this;
            }

            public override void SetBottom(int slot)
            {
                bottomSlot = slot;

                bottomValue = values[bottomSlot];
                if (currentReaderGen == readerGen[bottomSlot])
                {
                    bottomOrd = ords[bottomSlot];
                    bottomSameReader = true;
                }
                else
                {
                    if (bottomValue is null)
                    {
                        // missingOrd is null for all segments
                        if (Debugging.AssertsEnabled) Debugging.Assert(ords[bottomSlot] == missingOrd);
                        bottomOrd = missingOrd;
                        bottomSameReader = true;
                        readerGen[bottomSlot] = currentReaderGen;
                    }
                    else
                    {
                        int ord = termsIndex.LookupTerm(bottomValue);
                        if (ord < 0)
                        {
                            bottomOrd = -ord - 2;
                            bottomSameReader = false;
                        }
                        else
                        {
                            bottomOrd = ord;
                            // exact value match
                            bottomSameReader = true;
                            readerGen[bottomSlot] = currentReaderGen;
                            ords[bottomSlot] = bottomOrd;
                        }
                    }
                }
            }

            public override void SetTopValue(object value)
            {
                // null is fine: it means the last doc of the prior
                // search was missing this value
                topValue = (BytesRef)value;
                //System.out.println("setTopValue " + topValue);
            }

            public override IComparable this[int slot] => values[slot];

            public override int CompareTop(int doc)
            {
                int ord = termsIndex.GetOrd(doc);
                if (ord == -1)
                {
                    ord = missingOrd;
                }

                if (topSameReader)
                {
                    // ord is precisely comparable, even in the equal
                    // case
                    //System.out.println("compareTop doc=" + doc + " ord=" + ord + " ret=" + (topOrd-ord));
                    return topOrd - ord;
                }
                else if (ord <= topOrd)
                {
                    // the equals case always means doc is < value
                    // (because we set lastOrd to the lower bound)
                    return 1;
                }
                else
                {
                    return -1;
                }
            }

            public override int CompareValues(BytesRef val1, BytesRef val2)
            {
                if (val1 is null)
                {
                    if (val2 is null)
                    {
                        return 0;
                    }
                    return missingSortCmp;
                }
                else if (val2 is null)
                {
                    return -missingSortCmp;
                }
                return val1.CompareTo(val2);
            }
        }

        /// <summary>
        /// Sorts by field's natural <see cref="Index.Term"/> sort order.  All
        /// comparisons are done using <see cref="BytesRef.CompareTo(BytesRef)"/>, which is
        /// slow for medium to large result sets but possibly
        /// very fast for very small results sets.
        /// </summary>
        // TODO: should we remove this?  who really uses it?
        public sealed class TermValComparer : FieldComparer<BytesRef>
        {
            // sentinels, just used internally in this comparer
            private static readonly byte[] MISSING_BYTES = Arrays.Empty<byte>();

            private static readonly byte[] NON_MISSING_BYTES = Arrays.Empty<byte>();

            private readonly BytesRef[] values;
            private BinaryDocValues docTerms;
            private IBits docsWithField;
            private readonly string field;
            private BytesRef bottom;
            private BytesRef topValue;
            private readonly BytesRef tempBR = new BytesRef();

            // TODO: add missing first/last support here?

            /// <summary>
            /// Sole constructor. </summary>
            internal TermValComparer(int numHits, string field)
            {
                values = new BytesRef[numHits];
                this.field = field;
            }

            public override int Compare(int slot1, int slot2)
            {
                BytesRef val1 = values[slot1];
                BytesRef val2 = values[slot2];
                if (val1.Bytes == MISSING_BYTES)
                {
                    if (val2.Bytes == MISSING_BYTES)
                    {
                        return 0;
                    }
                    return -1;
                }
                else if (val2.Bytes == MISSING_BYTES)
                {
                    return 1;
                }

                return val1.CompareTo(val2);
            }

            public override int CompareBottom(int doc)
            {
                docTerms.Get(doc, tempBR);
                SetMissingBytes(doc, tempBR);
                return CompareValues(bottom, tempBR);
            }

            public override void Copy(int slot, int doc)
            {
                if (values[slot] is null)
                {
                    values[slot] = new BytesRef();
                }
                docTerms.Get(doc, values[slot]);
                SetMissingBytes(doc, values[slot]);
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                docTerms = FieldCache.DEFAULT.GetTerms((context.AtomicReader), field, true);
                docsWithField = FieldCache.DEFAULT.GetDocsWithField((context.AtomicReader), field);
                return this;
            }

            public override void SetBottom(int slot)
            {
                this.bottom = values[slot];
            }

            public override void SetTopValue(object value)
            {
                if (value is null)
                {
                    throw new ArgumentException("value cannot be null");
                }
                topValue = (BytesRef)value;
            }

            public override IComparable this[int slot] => values[slot];

            public override int CompareValues(BytesRef val1, BytesRef val2)
            {
                // missing always sorts first:
                if (val1.Bytes == MISSING_BYTES)
                {
                    if (val2.Bytes == MISSING_BYTES)
                    {
                        return 0;
                    }
                    return -1;
                }
                else if (val2.Bytes == MISSING_BYTES)
                {
                    return 1;
                }
                return val1.CompareTo(val2);
            }

            public override int CompareTop(int doc)
            {
                docTerms.Get(doc, tempBR);
                SetMissingBytes(doc, tempBR);
                return CompareValues(topValue, tempBR);
            }

            private void SetMissingBytes(int doc, BytesRef br)
            {
                if (br.Length == 0)
                {
                    br.Offset = 0;
                    if (docsWithField.Get(doc) == false)
                    {
                        br.Bytes = MISSING_BYTES;
                    }
                    else
                    {
                        br.Bytes = NON_MISSING_BYTES;
                    }
                }
            }
        }
    }
}