using System;
using System.Diagnostics;

namespace Lucene.Net.Search
{
    using Lucene.Net.Index;
    using Lucene.Net.Support;

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
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;

    /// <summary>
    /// Expert: a FieldComparator compares hits so as to determine their
    /// sort order when collecting the top results with {@link
    /// TopFieldCollector}.  The concrete public FieldComparator
    /// classes here correspond to the SortField types.
    ///
    /// <p>this API is designed to achieve high performance
    /// sorting, by exposing a tight interaction with {@link
    /// FieldValueHitQueue} as it visits hits.  Whenever a hit is
    /// competitive, it's enrolled into a virtual slot, which is
    /// an int ranging from 0 to numHits-1.  The {@link
    /// FieldComparator} is made aware of segment transitions
    /// during searching in case any internal state it's tracking
    /// needs to be recomputed during these transitions.</p>
    ///
    /// <p>A comparator must define these functions:</p>
    ///
    /// <ul>
    ///
    ///  <li> <seealso cref="#compare"/> Compare a hit at 'slot a'
    ///       with hit 'slot b'.
    ///
    ///  <li> <seealso cref="#setBottom"/> this method is called by
    ///       <seealso cref="FieldValueHitQueue"/> to notify the
    ///       FieldComparator of the current weakest ("bottom")
    ///       slot.  Note that this slot may not hold the weakest
    ///       value according to your comparator, in cases where
    ///       your comparator is not the primary one (ie, is only
    ///       used to break ties from the comparators before it).
    ///
    ///  <li> <seealso cref="#compareBottom"/> Compare a new hit (docID)
    ///       against the "weakest" (bottom) entry in the queue.
    ///
    ///  <li> <seealso cref="#setTopValue"/> this method is called by
    ///       <seealso cref="TopFieldCollector"/> to notify the
    ///       FieldComparator of the top most value, which is
    ///       used by future calls to <seealso cref="#compareTop"/>.
    ///
    ///  <li> <seealso cref="#compareBottom"/> Compare a new hit (docID)
    ///       against the "weakest" (bottom) entry in the queue.
    ///
    ///  <li> <seealso cref="#compareTop"/> Compare a new hit (docID)
    ///       against the top value previously set by a call to
    ///       <seealso cref="#setTopValue"/>.
    ///
    ///  <li> <seealso cref="#copy"/> Installs a new hit into the
    ///       priority queue.  The <seealso cref="FieldValueHitQueue"/>
    ///       calls this method when a new hit is competitive.
    ///
    ///  <li> <seealso cref="#setNextReader(AtomicReaderContext)"/> Invoked
    ///       when the search is switching to the next segment.
    ///       You may need to update internal state of the
    ///       comparator, for example retrieving new values from
    ///       the <seealso cref="FieldCache"/>.
    ///
    ///  <li> <seealso cref="#value"/> Return the sort value stored in
    ///       the specified slot.  this is only called at the end
    ///       of the search, in order to populate {@link
    ///       FieldDoc#fields} when returning the top results.
    /// </ul>
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class FieldComparator<T> : FieldComparator
    {
        /// <summary>
        /// Compare hit at slot1 with hit at slot2.
        /// </summary>
        /// <param name="slot1"> first slot to compare </param>
        /// <param name="slot2"> second slot to compare </param>
        /// <returns> any N < 0 if slot2's value is sorted after
        /// slot1, any N > 0 if the slot2's value is sorted before
        /// slot1 and 0 if they are equal </returns>
        public abstract override int Compare(int slot1, int slot2);

        /// <summary>
        /// Set the bottom slot, ie the "weakest" (sorted last)
        /// entry in the queue.  When <seealso cref="#compareBottom"/> is
        /// called, you should compare against this slot.  this
        /// will always be called before <seealso cref="#compareBottom"/>.
        /// </summary>
        /// <param name="slot"> the currently weakest (sorted last) slot in the queue </param>
        public abstract override int Bottom { set; }

        /// <summary>
        /// Record the top value, for future calls to {@link
        /// #compareTop}.  this is only called for searches that
        /// use searchAfter (deep paging), and is called before any
        /// calls to <seealso cref="#setNextReader"/>.
        /// </summary>
        public abstract override object TopValue { set; }

        /// <summary>
        /// Compare the bottom of the queue with this doc.  this will
        /// only invoked after setBottom has been called.  this
        /// should return the same result as {@link
        /// #compare(int,int)}} as if bottom were slot1 and the new
        /// document were slot 2.
        ///
        /// <p>For a search that hits many results, this method
        /// will be the hotspot (invoked by far the most
        /// frequently).</p>
        /// </summary>
        /// <param name="doc"> that was hit </param>
        /// <returns> any N < 0 if the doc's value is sorted after
        /// the bottom entry (not competitive), any N > 0 if the
        /// doc's value is sorted before the bottom entry and 0 if
        /// they are equal. </returns>
        public abstract override int CompareBottom(int doc);

        /// <summary>
        /// Compare the top value with this doc.  this will
        /// only invoked after setTopValue has been called.  this
        /// should return the same result as {@link
        /// #compare(int,int)}} as if topValue were slot1 and the new
        /// document were slot 2.  this is only called for searches that
        /// use searchAfter (deep paging).
        /// </summary>
        /// <param name="doc"> that was hit </param>
        /// <returns> any N < 0 if the doc's value is sorted after
        /// the bottom entry (not competitive), any N > 0 if the
        /// doc's value is sorted before the bottom entry and 0 if
        /// they are equal. </returns>
        public abstract override int CompareTop(int doc);

        /// <summary>
        /// this method is called when a new hit is competitive.
        /// You should copy any state associated with this document
        /// that will be required for future comparisons, into the
        /// specified slot.
        /// </summary>
        /// <param name="slot"> which slot to copy the hit to </param>
        /// <param name="doc"> docID relative to current reader </param>
        public abstract override void Copy(int slot, int doc);

        /// <summary>
        /// Set a new <seealso cref="AtomicReaderContext"/>. All subsequent docIDs are relative to
        /// the current reader (you must add docBase if you need to
        /// map it to a top-level docID).
        /// </summary>
        /// <param name="context"> current reader context </param>
        /// <returns> the comparator to use for this segment; most
        ///   comparators can just return "this" to reuse the same
        ///   comparator across segments </returns>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public abstract override FieldComparator SetNextReader(AtomicReaderContext context);

        /// <summary>
        /// Returns -1 if first is less than second.  Default
        ///  impl to assume the type implements Comparable and
        ///  invoke .compareTo; be sure to override this method if
        ///  your FieldComparator's type isn't a Comparable or
        ///  if your values may sometimes be null
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
            else if (object.ReferenceEquals(first, second))
            {
                return 0;
            }
            else
            {
                return ((IComparable<T>)first).CompareTo(second);
            }
        }
    }

    // .NET Port: Using a non-generic class here so that we avoid having to use the
    // type parameter to access these nested types. Also moving non-generic methods here for casting without generics.
    public abstract class FieldComparator
    {
        //Set up abstract methods
        /// <summary>
        /// Compare hit at slot1 with hit at slot2.
        /// </summary>
        /// <param name="slot1"> first slot to compare </param>
        /// <param name="slot2"> second slot to compare </param>
        /// <returns> any N < 0 if slot2's value is sorted after
        /// slot1, any N > 0 if the slot2's value is sorted before
        /// slot1 and 0 if they are equal </returns>
        public abstract int Compare(int slot1, int slot2);

        /// <summary>
        /// Set the bottom slot, ie the "weakest" (sorted last)
        /// entry in the queue.  When <seealso cref="#compareBottom"/> is
        /// called, you should compare against this slot.  this
        /// will always be called before <seealso cref="#compareBottom"/>.
        /// </summary>
        /// <param name="slot"> the currently weakest (sorted last) slot in the queue </param>
        public abstract int Bottom { set; }

        /// <summary>
        /// Record the top value, for future calls to {@link
        /// #compareTop}.  this is only called for searches that
        /// use searchAfter (deep paging), and is called before any
        /// calls to <seealso cref="#setNextReader"/>.
        /// </summary>
        public abstract object TopValue { set; }

        /// <summary>
        /// Compare the bottom of the queue with this doc.  this will
        /// only invoked after setBottom has been called.  this
        /// should return the same result as {@link
        /// #compare(int,int)}} as if bottom were slot1 and the new
        /// document were slot 2.
        ///
        /// <p>For a search that hits many results, this method
        /// will be the hotspot (invoked by far the most
        /// frequently).</p>
        /// </summary>
        /// <param name="doc"> that was hit </param>
        /// <returns> any N < 0 if the doc's value is sorted after
        /// the bottom entry (not competitive), any N > 0 if the
        /// doc's value is sorted before the bottom entry and 0 if
        /// they are equal. </returns>
        public abstract int CompareBottom(int doc);

        /// <summary>
        /// Compare the top value with this doc.  this will
        /// only invoked after setTopValue has been called.  this
        /// should return the same result as {@link
        /// #compare(int,int)}} as if topValue were slot1 and the new
        /// document were slot 2.  this is only called for searches that
        /// use searchAfter (deep paging).
        /// </summary>
        /// <param name="doc"> that was hit </param>
        /// <returns> any N < 0 if the doc's value is sorted after
        /// the bottom entry (not competitive), any N > 0 if the
        /// doc's value is sorted before the bottom entry and 0 if
        /// they are equal. </returns>
        public abstract int CompareTop(int doc);

        /// <summary>
        /// this method is called when a new hit is competitive.
        /// You should copy any state associated with this document
        /// that will be required for future comparisons, into the
        /// specified slot.
        /// </summary>
        /// <param name="slot"> which slot to copy the hit to </param>
        /// <param name="doc"> docID relative to current reader </param>
        public abstract void Copy(int slot, int doc);

        /// <summary>
        /// Set a new <seealso cref="AtomicReaderContext"/>. All subsequent docIDs are relative to
        /// the current reader (you must add docBase if you need to
        /// map it to a top-level docID).
        /// </summary>
        /// <param name="context"> current reader context </param>
        /// <returns> the comparator to use for this segment; most
        ///   comparators can just return "this" to reuse the same
        ///   comparator across segments </returns>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public abstract FieldComparator SetNextReader(AtomicReaderContext context);

        /// <summary>
        /// Sets the Scorer to use in case a document's score is
        ///  needed.
        /// </summary>
        /// <param name="scorer"> Scorer instance that you should use to
        /// obtain the current hit's score, if necessary.  </param>
        public virtual Scorer Scorer
        {
            set
            {
                // Empty implementation since most comparators don't need the score. this
                // can be overridden by those that need it.
            }
        }

        /// <summary>
        /// Return the actual value in the slot.
        /// </summary>
        /// <param name="slot"> the value </param>
        /// <returns> value in this slot </returns>
        public abstract IComparable Value(int slot);

        public int CompareValues(IComparable first, IComparable second)
        {
            return (first).CompareTo(second);
        }

        /// <summary>
        /// Base FieldComparator class for numeric types
        /// </summary>
        public abstract class NumericComparator<T> : FieldComparator<T>
            where T : struct
        {
            protected internal readonly T? MissingValue;
            protected internal readonly string Field;
            protected internal Bits DocsWithField;

            public NumericComparator(string field, T? missingValue)
            {
                this.Field = field;
                this.MissingValue = missingValue;
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                if (MissingValue != null)
                {
                    DocsWithField = FieldCache_Fields.DEFAULT.GetDocsWithField(((AtomicReader)context.Reader()), Field);
                    // optimization to remove unneeded checks on the bit interface:
                    if (DocsWithField is Lucene.Net.Util.Bits_MatchAllBits)
                    {
                        DocsWithField = null;
                    }
                }
                else
                {
                    DocsWithField = null;
                }
                return this;
            }
        }

        /// <summary>
        /// Parses field's values as byte (using {@link
        ///  FieldCache#getBytes} and sorts by ascending value
        /// </summary>
        [Obsolete]
        public sealed class ByteComparator : NumericComparator<sbyte>
        {
            internal readonly sbyte[] Values;
            internal readonly FieldCache_Fields.IByteParser Parser;
            internal FieldCache_Fields.Bytes CurrentReaderValues;
            internal sbyte bottom;
            internal sbyte topValue;

            internal ByteComparator(int numHits, string field, FieldCache_Fields.IParser parser, sbyte? missingValue)
                : base(field, missingValue)
            {
                Values = new sbyte[numHits];
                this.Parser = (FieldCache_Fields.IByteParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                //LUCENE TO-DO
                return Number.Signum(Values[slot1], Values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                sbyte v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }
                //LUCENE TO-DO
                return Number.Signum(bottom, v2);
            }

            public override void Copy(int slot, int doc)
            {
                sbyte v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }
                Values[slot] = v2;
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                CurrentReaderValues = FieldCache_Fields.DEFAULT.GetBytes(((AtomicReader)context.Reader()), Field, Parser, MissingValue != null);
                return base.SetNextReader(context);
            }

            public override int Bottom
            {
                set
                {
                    this.bottom = Values[value];
                }
            }

            public override object TopValue
            {
                set
                {
                    topValue = (sbyte)value;
                }
            }

            public override IComparable Value(int slot)
            {
                return Values[slot];
            }

            public override int CompareTop(int doc)
            {
                sbyte docValue = CurrentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && docValue == 0 && !DocsWithField.Get(doc))
                {
                    docValue = MissingValue.GetValueOrDefault();
                }
                //LUCENE TO-DO
                return Number.Signum(topValue, docValue);
            }
        }

        /// <summary>
        /// Parses field's values as double (using {@link
        ///  FieldCache#getDoubles} and sorts by ascending value
        /// </summary>
        public sealed class DoubleComparator : NumericComparator<double>
        {
            internal readonly double[] Values;
            internal readonly FieldCache_Fields.IDoubleParser Parser;
            internal FieldCache_Fields.Doubles CurrentReaderValues;
            internal double Bottom_Renamed;
            internal double TopValue_Renamed;

            internal DoubleComparator(int numHits, string field, FieldCache_Fields.IParser parser, double? missingValue)
                : base(field, missingValue)
            {
                Values = new double[numHits];
                this.Parser = (FieldCache_Fields.IDoubleParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                return Values[slot1].CompareTo(Values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                double v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0.0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }

                return Bottom_Renamed.CompareTo(v2);
            }

            public override void Copy(int slot, int doc)
            {
                double v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0.0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }

                Values[slot] = v2;
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                CurrentReaderValues = FieldCache_Fields.DEFAULT.GetDoubles(((AtomicReader)context.Reader()), Field, Parser, MissingValue != null);
                return base.SetNextReader(context);
            }

            public override int Bottom
            {
                set
                {
                    this.Bottom_Renamed = Values[value];
                }
            }

            public override object TopValue
            {
                set
                {
                    TopValue_Renamed = (double)value;
                }
            }

            public override IComparable Value(int slot)
            {
                return Values[slot];
            }

            public override int CompareTop(int doc)
            {
                double docValue = CurrentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && docValue == 0 && !DocsWithField.Get(doc))
                {
                    docValue = MissingValue.GetValueOrDefault();
                }
                return TopValue_Renamed.CompareTo(docValue);
            }
        }

        /// <summary>
        /// Parses field's values as float (using {@link
        ///  FieldCache#getFloats} and sorts by ascending value
        /// </summary>
        public sealed class FloatComparator : NumericComparator<float>
        {
            internal readonly float[] Values;
            internal readonly FieldCache_Fields.IFloatParser Parser;
            internal FieldCache_Fields.Floats CurrentReaderValues;
            internal float Bottom_Renamed;
            internal float TopValue_Renamed;

            internal FloatComparator(int numHits, string field, FieldCache_Fields.IParser parser, float? missingValue)
                : base(field, missingValue)
            {
                Values = new float[numHits];
                this.Parser = (FieldCache_Fields.IFloatParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                return Values[slot1].CompareTo(Values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                // TODO: are there sneaky non-branch ways to compute sign of float?
                float v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }

                return Bottom_Renamed.CompareTo(v2);
            }

            public override void Copy(int slot, int doc)
            {
                float v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }

                Values[slot] = v2;
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                CurrentReaderValues = FieldCache_Fields.DEFAULT.GetFloats(((AtomicReader)context.Reader()), Field, Parser, MissingValue != null);
                return base.SetNextReader(context);
            }

            public override int Bottom
            {
                set
                {
                    this.Bottom_Renamed = Values[value];
                }
            }

            public override object TopValue
            {
                set
                {
                    TopValue_Renamed = (float)value;
                }
            }

            public override IComparable Value(int slot)
            {
                return Values[slot];
            }

            public override int CompareTop(int doc)
            {
                float docValue = CurrentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && docValue == 0 && !DocsWithField.Get(doc))
                {
                    docValue = MissingValue.GetValueOrDefault();
                }
                return TopValue_Renamed.CompareTo(docValue);
            }
        }

        /// <summary>
        /// Parses field's values as short (using {@link
        ///  FieldCache#getShorts} and sorts by ascending value
        /// </summary>
        [Obsolete]
        public sealed class ShortComparator : NumericComparator<short>
        {
            internal readonly short[] Values;
            internal readonly FieldCache_Fields.IShortParser Parser;
            internal FieldCache_Fields.Shorts CurrentReaderValues;
            internal short Bottom_Renamed;
            internal short TopValue_Renamed;

            internal ShortComparator(int numHits, string field, FieldCache_Fields.IParser parser, short? missingValue)
                : base(field, missingValue)
            {
                Values = new short[numHits];
                this.Parser = (FieldCache_Fields.IShortParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                //LUCENE TO-DO
                return Number.Signum(Values[slot1], Values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                short v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }

                //LUCENE TO-DO
                return Number.Signum(Bottom_Renamed, v2);
            }

            public override void Copy(int slot, int doc)
            {
                short v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }

                Values[slot] = v2;
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                CurrentReaderValues = FieldCache_Fields.DEFAULT.GetShorts(((AtomicReader)context.Reader()), Field, Parser, MissingValue != null);
                return base.SetNextReader(context);
            }

            public override int Bottom
            {
                set
                {
                    this.Bottom_Renamed = Values[value];
                }
            }

            public override object TopValue
            {
                set
                {
                    TopValue_Renamed = (short)value;
                }
            }

            public override IComparable Value(int slot)
            {
                return Values[slot];
            }

            public override int CompareTop(int doc)
            {
                short docValue = CurrentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && docValue == 0 && !DocsWithField.Get(doc))
                {
                    docValue = MissingValue.GetValueOrDefault();
                }
                return TopValue_Renamed.CompareTo(docValue);
            }
        }

        /// <summary>
        /// Parses field's values as int (using {@link
        ///  FieldCache#getInts} and sorts by ascending value
        /// </summary>
        public sealed class IntComparator : NumericComparator<int>
        {
            internal readonly int[] Values;
            internal readonly FieldCache_Fields.IIntParser Parser;
            internal FieldCache_Fields.Ints CurrentReaderValues;
            internal int Bottom_Renamed; // Value of bottom of queue
            internal int TopValue_Renamed;

            internal IntComparator(int numHits, string field, FieldCache_Fields.IParser parser, int? missingValue)
                : base(field, missingValue)
            {
                Values = new int[numHits];
                this.Parser = (FieldCache_Fields.IIntParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                return Values[slot1].CompareTo(Values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                int v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }
                return Bottom_Renamed.CompareTo(v2);
            }

            public override void Copy(int slot, int doc)
            {
                int v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }

                Values[slot] = v2;
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                CurrentReaderValues = FieldCache_Fields.DEFAULT.GetInts(((AtomicReader)context.Reader()), Field, Parser, MissingValue != null);
                return base.SetNextReader(context);
            }

            public override int Bottom
            {
                set
                {
                    this.Bottom_Renamed = Values[value];
                }
            }

            public override object TopValue
            {
                set
                {
                    TopValue_Renamed = (int)value;
                }
            }

            public override IComparable Value(int slot)
            {
                return Values[slot];
            }

            public override int CompareTop(int doc)
            {
                int docValue = CurrentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && docValue == 0 && !DocsWithField.Get(doc))
                {
                    docValue = MissingValue.GetValueOrDefault();
                }
                return TopValue_Renamed.CompareTo(docValue);
            }
        }

        /// <summary>
        /// Parses field's values as long (using {@link
        ///  FieldCache#getLongs} and sorts by ascending value
        /// </summary>
        public sealed class LongComparator : NumericComparator<long>
        {
            internal readonly long[] Values;
            internal readonly FieldCache_Fields.ILongParser Parser;
            internal FieldCache_Fields.Longs CurrentReaderValues;
            internal long Bottom_Renamed;
            internal long TopValue_Renamed;

            internal LongComparator(int numHits, string field, FieldCache_Fields.IParser parser, long? missingValue)
                : base(field, missingValue)
            {
                Values = new long[numHits];
                this.Parser = (FieldCache_Fields.ILongParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                return Number.Signum(Values[slot1], Values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                // TODO: there are sneaky non-branch ways to compute
                // -1/+1/0 sign
                long v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }

                return Number.Signum(Bottom_Renamed, v2);
            }

            public override void Copy(int slot, int doc)
            {
                long v2 = CurrentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && v2 == 0 && !DocsWithField.Get(doc))
                {
                    v2 = MissingValue.GetValueOrDefault();
                }

                Values[slot] = v2;
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                CurrentReaderValues = FieldCache_Fields.DEFAULT.GetLongs(((AtomicReader)context.Reader()), Field, Parser, MissingValue != null);
                return base.SetNextReader(context);
            }

            public override int Bottom
            {
                set
                {
                    this.Bottom_Renamed = Values[value];
                }
            }

            public override object TopValue
            {
                set
                {
                    TopValue_Renamed = (long)value;
                }
            }

            public override IComparable Value(int slot)
            {
                return Values[slot];
            }

            public override int CompareTop(int doc)
            {
                long docValue = CurrentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (DocsWithField != null && docValue == 0 && !DocsWithField.Get(doc))
                {
                    docValue = MissingValue.GetValueOrDefault();
                }
                return TopValue_Renamed.CompareTo(docValue);
            }
        }

        /// <summary>
        /// Sorts by descending relevance.  NOTE: if you are
        ///  sorting only by descending relevance and then
        ///  secondarily by ascending docID, performance is faster
        ///  using <seealso cref="TopScoreDocCollector"/> directly (which {@link
        ///  IndexSearcher#search} uses when no <seealso cref="Sort"/> is
        ///  specified).
        /// </summary>
        public sealed class RelevanceComparator : FieldComparator<float>
        {
            internal readonly float[] Scores;
            internal float Bottom_Renamed;
            internal Scorer Scorer_Renamed;
            internal float TopValue_Renamed;

            internal RelevanceComparator(int numHits)
            {
                Scores = new float[numHits];
            }

            public override int Compare(int slot1, int slot2)
            {
                return Scores[slot2].CompareTo(Scores[slot1]);
            }

            public override int CompareBottom(int doc)
            {
                float score = Scorer_Renamed.Score();
                Debug.Assert(!float.IsNaN(score));
                return score.CompareTo(Bottom_Renamed);
            }

            public override void Copy(int slot, int doc)
            {
                Scores[slot] = Scorer_Renamed.Score();
                Debug.Assert(!float.IsNaN(Scores[slot]));
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                return this;
            }

            public override int Bottom
            {
                set
                {
                    this.Bottom_Renamed = Scores[value];
                }
            }

            public override object TopValue
            {
                set
                {
                    TopValue_Renamed = (float)value;
                }
            }

            public override Scorer Scorer
            {
                set
                {
                    // wrap with a ScoreCachingWrappingScorer so that successive calls to
                    // score() will not incur score computation over and
                    // over again.
                    if (!(value is ScoreCachingWrappingScorer))
                    {
                        this.Scorer_Renamed = new ScoreCachingWrappingScorer(value);
                    }
                    else
                    {
                        this.Scorer_Renamed = value;
                    }
                }
            }

            public override IComparable Value(int slot)
            {
                return Convert.ToSingle(Scores[slot]);
            }

            // Override because we sort reverse of natural Float order:
            public override int CompareValues(float first, float second)
            {
                // Reversed intentionally because relevance by default
                // sorts descending:
                return second.CompareTo(first);
            }

            public override int CompareTop(int doc)
            {
                float docValue = Scorer_Renamed.Score();
                Debug.Assert(!float.IsNaN(docValue));
                return docValue.CompareTo(TopValue_Renamed);
            }
        }

        /// <summary>
        /// Sorts by ascending docID </summary>
        public sealed class DocComparator : FieldComparator<int?>
        {
            internal readonly int[] DocIDs;
            internal int DocBase;
            internal int Bottom_Renamed;
            internal int TopValue_Renamed;

            internal DocComparator(int numHits)
            {
                DocIDs = new int[numHits];
            }

            public override int Compare(int slot1, int slot2)
            {
                // No overflow risk because docIDs are non-negative
                return DocIDs[slot1] - DocIDs[slot2];
            }

            public override int CompareBottom(int doc)
            {
                // No overflow risk because docIDs are non-negative
                return Bottom_Renamed - (DocBase + doc);
            }

            public override void Copy(int slot, int doc)
            {
                DocIDs[slot] = DocBase + doc;
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                // TODO: can we "map" our docIDs to the current
                // reader? saves having to then subtract on every
                // compare call
                this.DocBase = context.DocBase;
                return this;
            }

            public override int Bottom
            {
                set
                {
                    this.Bottom_Renamed = DocIDs[value];
                }
            }

            public override object TopValue
            {
                set
                {
                    TopValue_Renamed = (int)value;
                }
            }

            public override IComparable Value(int slot)
            {
                return Convert.ToInt32(DocIDs[slot]);
            }

            public override int CompareTop(int doc)
            {
                int docValue = DocBase + doc;
                return Number.Signum(TopValue_Renamed, docValue);
            }
        }

        /// <summary>
        /// Sorts by field's natural Term sort order, using
        ///  ordinals.  this is functionally equivalent to {@link
        ///  Lucene.Net.Search.FieldComparator.TermValComparator}, but it first resolves the string
        ///  to their relative ordinal positions (using the index
        ///  returned by <seealso cref="FieldCache#getTermsIndex"/>), and
        ///  does most comparisons using the ordinals.  For medium
        ///  to large results, this comparator will be much faster
        ///  than <seealso cref="Lucene.Net.Search.FieldComparator.TermValComparator"/>.  For very small
        ///  result sets it may be slower.
        /// </summary>
        public class TermOrdValComparator : FieldComparator<BytesRef>
        {
            /* Ords for each slot.
	            @lucene.internal */
            internal readonly int[] Ords;

            /* Values for each slot.
	            @lucene.internal */
            internal readonly BytesRef[] Values;

            /* Which reader last copied a value into the slot. When
	            we compare two slots, we just compare-by-ord if the
	            readerGen is the same; else we must compare the
	            values (slower).
	            @lucene.internal */
            internal readonly int[] ReaderGen;

            /* Gen of current reader we are on.
	            @lucene.internal */
            internal int CurrentReaderGen = -1;

            /* Current reader's doc ord/values.
	            @lucene.internal */
            internal SortedDocValues TermsIndex;

            internal readonly string Field;

            /* Bottom slot, or -1 if queue isn't full yet
	            @lucene.internal */
            internal int BottomSlot = -1;

            /* Bottom ord (same as ords[bottomSlot] once bottomSlot
	            is set).  Cached for faster compares.
	            @lucene.internal */
            internal int BottomOrd;

            /* True if current bottom slot matches the current
	            reader.
	            @lucene.internal */
            internal bool BottomSameReader;

            /* Bottom value (same as values[bottomSlot] once
	            bottomSlot is set).  Cached for faster compares.
	            @lucene.internal */
            internal BytesRef BottomValue;

            /// <summary>
            /// Set by setTopValue. </summary>
            internal BytesRef TopValue_Renamed;

            internal bool TopSameReader;
            internal int TopOrd;

            internal readonly BytesRef TempBR = new BytesRef();

            /// <summary>
            /// -1 if missing values are sorted first, 1 if they are
            ///  sorted last
            /// </summary>
            internal readonly int MissingSortCmp;

            /// <summary>
            /// Which ordinal to use for a missing value. </summary>
            internal readonly int MissingOrd;

            /// <summary>
            /// Creates this, sorting missing values first. </summary>
            public TermOrdValComparator(int numHits, string field)
                : this(numHits, field, false)
            {
            }

            /// <summary>
            /// Creates this, with control over how missing values
            ///  are sorted.  Pass sortMissingLast=true to put
            ///  missing values at the end.
            /// </summary>
            public TermOrdValComparator(int numHits, string field, bool sortMissingLast)
            {
                Ords = new int[numHits];
                Values = new BytesRef[numHits];
                ReaderGen = new int[numHits];
                this.Field = field;
                if (sortMissingLast)
                {
                    MissingSortCmp = 1;
                    MissingOrd = int.MaxValue;
                }
                else
                {
                    MissingSortCmp = -1;
                    MissingOrd = -1;
                }
            }

            public override int Compare(int slot1, int slot2)
            {
                if (ReaderGen[slot1] == ReaderGen[slot2])
                {
                    return Ords[slot1] - Ords[slot2];
                }

                BytesRef val1 = Values[slot1];
                BytesRef val2 = Values[slot2];
                if (val1 == null)
                {
                    if (val2 == null)
                    {
                        return 0;
                    }
                    return MissingSortCmp;
                }
                else if (val2 == null)
                {
                    return -MissingSortCmp;
                }
                return val1.CompareTo(val2);
            }

            public override int CompareBottom(int doc)
            {
                Debug.Assert(BottomSlot != -1);
                int docOrd = TermsIndex.GetOrd(doc);
                if (docOrd == -1)
                {
                    docOrd = MissingOrd;
                }
                if (BottomSameReader)
                {
                    // ord is precisely comparable, even in the equal case
                    return BottomOrd - docOrd;
                }
                else if (BottomOrd >= docOrd)
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
                int ord = TermsIndex.GetOrd(doc);
                if (ord == -1)
                {
                    ord = MissingOrd;
                    Values[slot] = null;
                }
                else
                {
                    Debug.Assert(ord >= 0);
                    if (Values[slot] == null)
                    {
                        Values[slot] = new BytesRef();
                    }
                    TermsIndex.LookupOrd(ord, Values[slot]);
                }
                Ords[slot] = ord;
                ReaderGen[slot] = CurrentReaderGen;
            }

            /// <summary>
            /// Retrieves the SortedDocValues for the field in this segment </summary>
            protected internal virtual SortedDocValues GetSortedDocValues(AtomicReaderContext context, string field)
            {
                return FieldCache_Fields.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader()), field);
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                TermsIndex = GetSortedDocValues(context, Field);
                CurrentReaderGen++;

                if (TopValue_Renamed != null)
                {
                    // Recompute topOrd/SameReader
                    int ord = TermsIndex.LookupTerm(TopValue_Renamed);
                    if (ord >= 0)
                    {
                        TopSameReader = true;
                        TopOrd = ord;
                    }
                    else
                    {
                        TopSameReader = false;
                        TopOrd = -ord - 2;
                    }
                }
                else
                {
                    TopOrd = MissingOrd;
                    TopSameReader = true;
                }
                //System.out.println("  setNextReader topOrd=" + topOrd + " topSameReader=" + topSameReader);

                if (BottomSlot != -1)
                {
                    // Recompute bottomOrd/SameReader
                    Bottom = BottomSlot;
                }

                return this;
            }

            public override int Bottom
            {
                set
                {
                    BottomSlot = value;

                    BottomValue = Values[BottomSlot];
                    if (CurrentReaderGen == ReaderGen[BottomSlot])
                    {
                        BottomOrd = Ords[BottomSlot];
                        BottomSameReader = true;
                    }
                    else
                    {
                        if (BottomValue == null)
                        {
                            // missingOrd is null for all segments
                            Debug.Assert(Ords[BottomSlot] == MissingOrd);
                            BottomOrd = MissingOrd;
                            BottomSameReader = true;
                            ReaderGen[BottomSlot] = CurrentReaderGen;
                        }
                        else
                        {
                            int ord = TermsIndex.LookupTerm(BottomValue);
                            if (ord < 0)
                            {
                                BottomOrd = -ord - 2;
                                BottomSameReader = false;
                            }
                            else
                            {
                                BottomOrd = ord;
                                // exact value match
                                BottomSameReader = true;
                                ReaderGen[BottomSlot] = CurrentReaderGen;
                                Ords[BottomSlot] = BottomOrd;
                            }
                        }
                    }
                }
            }

            public override object TopValue
            {
                set
                {
                    // null is fine: it means the last doc of the prior
                    // search was missing this value
                    TopValue_Renamed = (BytesRef)value;
                    //System.out.println("setTopValue " + topValue);
                }
            }

            public override IComparable Value(int slot)
            {
                return Values[slot];
            }

            public override int CompareTop(int doc)
            {
                int ord = TermsIndex.GetOrd(doc);
                if (ord == -1)
                {
                    ord = MissingOrd;
                }

                if (TopSameReader)
                {
                    // ord is precisely comparable, even in the equal
                    // case
                    //System.out.println("compareTop doc=" + doc + " ord=" + ord + " ret=" + (topOrd-ord));
                    return TopOrd - ord;
                }
                else if (ord <= TopOrd)
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
                if (val1 == null)
                {
                    if (val2 == null)
                    {
                        return 0;
                    }
                    return MissingSortCmp;
                }
                else if (val2 == null)
                {
                    return -MissingSortCmp;
                }
                return val1.CompareTo(val2);
            }
        }

        /// <summary>
        /// Sorts by field's natural Term sort order.  All
        ///  comparisons are done using BytesRef.compareTo, which is
        ///  slow for medium to large result sets but possibly
        ///  very fast for very small results sets.
        /// </summary>
        // TODO: should we remove this?  who really uses it?
        public sealed class TermValComparator : FieldComparator<BytesRef>
        {
            // sentinels, just used internally in this comparator
            internal static readonly sbyte[] MISSING_BYTES = new sbyte[0];

            internal static readonly sbyte[] NON_MISSING_BYTES = new sbyte[0];

            internal BytesRef[] Values;
            internal BinaryDocValues DocTerms;
            internal Bits DocsWithField;
            internal readonly string Field;
            internal BytesRef Bottom_Renamed;
            internal BytesRef TopValue_Renamed;
            internal readonly BytesRef TempBR = new BytesRef();

            // TODO: add missing first/last support here?

            /// <summary>
            /// Sole constructor. </summary>
            internal TermValComparator(int numHits, string field)
            {
                Values = new BytesRef[numHits];
                this.Field = field;
            }

            public override int Compare(int slot1, int slot2)
            {
                BytesRef val1 = Values[slot1];
                BytesRef val2 = Values[slot2];
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
                DocTerms.Get(doc, TempBR);
                SetMissingBytes(doc, TempBR);
                return CompareValues(Bottom_Renamed, TempBR);
            }

            public override void Copy(int slot, int doc)
            {
                if (Values[slot] == null)
                {
                    Values[slot] = new BytesRef();
                }
                DocTerms.Get(doc, Values[slot]);
                SetMissingBytes(doc, Values[slot]);
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                DocTerms = FieldCache_Fields.DEFAULT.GetTerms(((AtomicReader)context.Reader()), Field, true);
                DocsWithField = FieldCache_Fields.DEFAULT.GetDocsWithField(((AtomicReader)context.Reader()), Field);
                return this;
            }

            public override int Bottom
            {
                set
                {
                    this.Bottom_Renamed = Values[value];
                }
            }

            public override object TopValue
            {
                set
                {
                    if (value == null)
                    {
                        throw new System.ArgumentException("value cannot be null");
                    }
                    TopValue_Renamed = (BytesRef)value;
                }
            }

            public override IComparable Value(int slot)
            {
                return Values[slot];
            }

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
                DocTerms.Get(doc, TempBR);
                SetMissingBytes(doc, TempBR);
                return CompareValues(TopValue_Renamed, TempBR);
            }

            internal void SetMissingBytes(int doc, BytesRef br)
            {
                if (br.Length == 0)
                {
                    br.Offset = 0;
                    if (DocsWithField.Get(doc) == false)
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