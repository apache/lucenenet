/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Support;
using IndexReader = Lucene.Net.Index.IndexReader;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{

    /// <summary> Expert: a FieldComparator compares hits so as to determine their
    /// sort order when collecting the top results with <see cref="TopFieldCollector" />
    ///.  The concrete public FieldComparator
    /// classes here correspond to the SortField types.
    /// 
    /// <p/>This API is designed to achieve high performance
    /// sorting, by exposing a tight interaction with <see cref="FieldValueHitQueue" />
    /// as it visits hits.  Whenever a hit is
    /// competitive, it's enrolled into a virtual slot, which is
    /// an int ranging from 0 to numHits-1.  The <see cref="FieldComparator" />
    /// is made aware of segment transitions
    /// during searching in case any internal state it's tracking
    /// needs to be recomputed during these transitions.<p/>
    /// 
    /// <p/>A comparator must define these functions:<p/>
    /// 
    /// <list type="bullet">
    /// 
    /// <item> <see cref="Compare" /> Compare a hit at 'slot a'
    /// with hit 'slot b'.</item>
    /// 
    /// <item> <see cref="SetBottom" /> This method is called by
    /// <see cref="FieldValueHitQueue" /> to notify the
    /// FieldComparator of the current weakest ("bottom")
    /// slot.  Note that this slot may not hold the weakest
    /// value according to your comparator, in cases where
    /// your comparator is not the primary one (ie, is only
    /// used to break ties from the comparators before it).</item>
    /// 
    /// <item> <see cref="CompareBottom" /> Compare a new hit (docID)
    /// against the "weakest" (bottom) entry in the queue.</item>
    /// 
    /// <item> <see cref="Copy" /> Installs a new hit into the
    /// priority queue.  The <see cref="FieldValueHitQueue" />
    /// calls this method when a new hit is competitive.</item>
    /// 
    /// <item> <see cref="SetNextReader" /> Invoked
    /// when the search is switching to the next segment.
    /// You may need to update internal state of the
    /// comparator, for example retrieving new values from
    /// the <see cref="FieldCache" />.</item>
    /// 
    /// <item> <see cref="P:Lucene.Net.Search.FieldComparator.Item(System.Int32)" /> Return the sort value stored in
    /// the specified slot.  This is only called at the end
    /// of the search, in order to populate <see cref="FieldDoc.fields" />
    /// when returning the top results.</item>
    /// </list>
    /// 
    /// <b>NOTE:</b> This API is experimental and might change in
    /// incompatible ways in the next release.
    /// </summary>
    public abstract class FieldComparator<T> : FieldComparator
    {
        // .NET Port: this class doesn't line-by-line match up with java due to use of non-generic casting.
        // see FieldComparator below.

        /// <summary> Set a new Reader. All doc correspond to the current Reader.
        /// 
        /// </summary>
        /// <param name="reader">current reader
        /// </param>
        /// <param name="docBase">docBase of this reader 
        /// </param>
        /// <throws>  IOException </throws>
        /// <throws>  IOException </throws>
        public abstract FieldComparator<T> SetNextReader(AtomicReaderContext context);
        
        /// <summary> Return the actual value in the slot.
        /// 
        /// </summary>
        /// <param name="slot">the value
        /// </param>
        /// <returns> value in this slot upgraded to Comparable
        /// </returns>
        public abstract override T Value(int slot);

        public T this[int slot]
        {
            get { return Value(slot); }
        }

        public virtual int CompareValues(T first, T second)
        {
            if (first == null)
            {
                if (second == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if (second == null)
            {
                return 1;
            }
            else
            {
                return ((IComparable<T>)first).CompareTo(second);
            }
        }

        public abstract int CompareDocToValue(int doc, T value);

    }

    // .NET Port: Using a non-generic class here so that we avoid having to use the 
    // type parameter to access these nested types. Also moving non-generic methods here for casting without generics.
    public abstract class FieldComparator
    {
        /// <summary> Compare hit at slot1 with hit at slot2.
        /// 
        /// </summary>
        /// <param name="slot1">first slot to compare
        /// </param>
        /// <param name="slot2">second slot to compare
        /// </param>
        /// <returns> any N &lt; 0 if slot2's value is sorted after
        /// slot1, any N > 0 if the slot2's value is sorted before
        /// slot1 and 0 if they are equal
        /// </returns>
        public abstract int Compare(int slot1, int slot2);

        /// <summary> Set the bottom slot, ie the "weakest" (sorted last)
        /// entry in the queue.  When <see cref="CompareBottom" /> is
        /// called, you should compare against this slot.  This
        /// will always be called before <see cref="CompareBottom" />.
        /// 
        /// </summary>
        /// <param name="slot">the currently weakest (sorted last) slot in the queue
        /// </param>
        public abstract void SetBottom(int slot);

        /// <summary> Compare the bottom of the queue with doc.  This will
        /// only invoked after setBottom has been called.  This
        /// should return the same result as <see cref="Compare(int,int)" />
        ///} as if bottom were slot1 and the new
        /// document were slot 2.
        /// 
        /// <p/>For a search that hits many results, this method
        /// will be the hotspot (invoked by far the most
        /// frequently).<p/>
        /// 
        /// </summary>
        /// <param name="doc">that was hit
        /// </param>
        /// <returns> any N &lt; 0 if the doc's value is sorted after
        /// the bottom entry (not competitive), any N > 0 if the
        /// doc's value is sorted before the bottom entry and 0 if
        /// they are equal.
        /// </returns>
        public abstract int CompareBottom(int doc);

        /// <summary> This method is called when a new hit is competitive.
        /// You should copy any state associated with this document
        /// that will be required for future comparisons, into the
        /// specified slot.
        /// 
        /// </summary>
        /// <param name="slot">which slot to copy the hit to
        /// </param>
        /// <param name="doc">docID relative to current reader
        /// </param>
        public abstract void Copy(int slot, int doc);

        /// <summary>Sets the Scorer to use in case a document's score is
        /// needed.
        /// 
        /// </summary>
        /// <param name="scorer">Scorer instance that you should use to
        /// obtain the current hit's score, if necessary. 
        /// </param>
        public virtual void SetScorer(Scorer scorer)
        {
            // Empty implementation since most comparators don't need the score. This
            // can be overridden by those that need it.
        }

        public abstract object Value(int slot);

        public abstract class NumericComparator<T> : FieldComparator<T>
            where T : struct
        {
            protected readonly T? missingValue;
            protected readonly string field;
            protected IBits docsWithField;

            public NumericComparator(string field, T missingValue)
            {
                this.field = field;
                this.missingValue = missingValue;
            }

            public override FieldComparator<T> SetNextReader(AtomicReaderContext context)
            {
                if (missingValue != null)
                {
                    docsWithField = FieldCache.DEFAULT.GetDocsWithField(context.Reader, field);
                    // optimization to remove unneeded checks on the bit interface:
                    if (docsWithField is Bits.MatchAllBits)
                    {
                        docsWithField = null;
                    }
                }
                else
                {
                    docsWithField = null;
                }
                return this;
            }
        }

        /// <summary>Parses field's values as byte (using <see cref="FieldCache.GetBytes(Lucene.Net.Index.IndexReader,string)" />
        /// and sorts by ascending value 
        /// </summary>
        public sealed class ByteComparator : NumericComparator<sbyte>
        {
            private readonly sbyte[] values;
            private readonly FieldCache.IByteParser parser;
            private FieldCache.Bytes currentReaderValues;
            private sbyte bottom;

            internal ByteComparator(int numHits, string field, FieldCache.IParser parser, sbyte missingValue)
                : base(field, missingValue)
            {
                values = new sbyte[numHits];
                this.parser = (FieldCache.IByteParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                return values[slot1] - values[slot2];
            }

            public override int CompareBottom(int doc)
            {
                sbyte v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }

                return bottom - v2;
            }

            public override void Copy(int slot, int doc)
            {
                sbyte v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }
                values[slot] = v2;
            }

            public override FieldComparator<sbyte> SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetBytes(context.Reader, field, parser, missingValue != null);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int bottom)
            {
                this.bottom = values[bottom];
            }

            public override sbyte Value(int slot)
            {
                return values[slot];
            }

            public override int CompareDocToValue(int doc, sbyte value)
            {
                sbyte docValue = currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && docValue == 0 && !docsWithField[doc])
                {
                    docValue = missingValue.GetValueOrDefault();
                }
                return docValue - value;
            }
        }


        /// <summary>Parses field's values as double (using <see cref="FieldCache.GetDoubles(Lucene.Net.Index.IndexReader,string)" />
        /// and sorts by ascending value 
        /// </summary>
        public sealed class DoubleComparator : NumericComparator<double>
        {
            private readonly double[] values;
            private readonly FieldCache.IDoubleParser parser;
            private FieldCache.Doubles currentReaderValues;
            private double bottom;

            internal DoubleComparator(int numHits, string field, FieldCache.IParser parser, double missingValue)
                : base(field, missingValue)
            {
                values = new double[numHits];
                this.parser = (FieldCache.IDoubleParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                return values[slot1].CompareTo(values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                double v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }

                return bottom.CompareTo(v2);
            }

            public override void Copy(int slot, int doc)
            {
                double v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }

                values[slot] = v2;
            }

            public override FieldComparator<double> SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetDoubles(context.Reader, field, parser, missingValue != null);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int bottom)
            {
                this.bottom = values[bottom];
            }

            public override double Value(int slot)
            {
                return values[slot];
            }

            public override int CompareDocToValue(int doc, double valueObj)
            {
                double value = valueObj;
                double docValue = currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && docValue == 0 && !docsWithField[doc])
                {
                    docValue = missingValue.GetValueOrDefault();
                }
                return docValue.CompareTo(value);
            }
        }

        /// <summary>Parses field's values as float (using <see cref="FieldCache.GetFloats(Lucene.Net.Index.IndexReader,string)" />
        /// and sorts by ascending value 
        /// </summary>
        public sealed class FloatComparator : NumericComparator<float>
        {
            private readonly float[] values;
            private readonly FieldCache.IFloatParser parser;
            private FieldCache.Floats currentReaderValues;
            private float bottom;

            internal FloatComparator(int numHits, string field, FieldCache.IParser parser, float missingValue)
                : base(field, missingValue)
            {
                values = new float[numHits];
                this.parser = (FieldCache.IFloatParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                return values[slot1].CompareTo(values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                // TODO: are there sneaky non-branch ways to compute sign of float?
                float v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }

                return bottom.CompareTo(v2);
            }

            public override void Copy(int slot, int doc)
            {
                float v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }

                values[slot] = v2;
            }

            public override FieldComparator<float> SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetFloats(context.Reader, field, parser, missingValue != null);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int bottom)
            {
                this.bottom = values[bottom];
            }

            public override float Value(int slot)
            {
                return values[slot];
            }

            public override int CompareDocToValue(int doc, float valueObj)
            {
                float value = valueObj;
                float docValue = currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && docValue == 0 && !docsWithField[doc])
                {
                    docValue = missingValue.GetValueOrDefault();
                }
                return docValue.CompareTo(value);
            }
        }

        /// <summary>Parses field's values as short (using <see cref="FieldCache.GetShorts(IndexReader, string)" />)
        /// and sorts by ascending value 
        /// </summary>
        public sealed class ShortComparator : NumericComparator<short>
        {
            private readonly short[] values;
            private readonly FieldCache.IShortParser parser;
            private FieldCache.Shorts currentReaderValues;
            private short bottom;

            internal ShortComparator(int numHits, string field, FieldCache.IParser parser, short missingValue)
                : base(field, missingValue)
            {
                values = new short[numHits];
                this.parser = (FieldCache.IShortParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                return values[slot1] - values[slot2];
            }

            public override int CompareBottom(int doc)
            {
                short v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }

                return bottom - v2;
            }

            public override void Copy(int slot, int doc)
            {
                short v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }

                values[slot] = v2;
            }

            public override FieldComparator<short> SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetShorts(context.Reader, field, parser, missingValue != null);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int bottom)
            {
                this.bottom = values[bottom];
            }

            public override short Value(int slot)
            {
                return values[slot];
            }

            public override int CompareDocToValue(int doc, short valueObj)
            {
                short value = valueObj;
                short docValue = currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && docValue == 0 && !docsWithField[doc])
                {
                    docValue = missingValue.GetValueOrDefault();
                }
                return docValue - value;
            }
        }

        /// <summary>Parses field's values as int (using <see cref="FieldCache.GetInts(Lucene.Net.Index.IndexReader,string)" />
        /// and sorts by ascending value 
        /// </summary>
        public sealed class IntComparator : NumericComparator<int>
        {
            private readonly int[] values;
            private readonly FieldCache.IIntParser parser;
            private FieldCache.Ints currentReaderValues;
            private int bottom;                           // Value of bottom of queue

            internal IntComparator(int numHits, string field, FieldCache.IParser parser, int missingValue)
                : base(field, missingValue)
            {
                values = new int[numHits];
                this.parser = (FieldCache.IIntParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                // TODO: there are sneaky non-branch ways to compute
                // -1/+1/0 sign
                // Cannot return values[slot1] - values[slot2] because that
                // may overflow
                int v1 = values[slot1];
                int v2 = values[slot2];
                if (v1 > v2)
                {
                    return 1;
                }
                else if (v1 < v2)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }

            public override int CompareBottom(int doc)
            {
                // TODO: there are sneaky non-branch ways to compute
                // -1/+1/0 sign
                // Cannot return bottom - values[slot2] because that
                // may overflow
                int v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }

                if (bottom > v2)
                {
                    return 1;
                }
                else if (bottom < v2)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }

            public override void Copy(int slot, int doc)
            {
                int v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }

                values[slot] = v2;
            }

            public override FieldComparator<int> SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetInts(context.Reader, field, parser, missingValue != null);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int bottom)
            {
                this.bottom = values[bottom];
            }

            public override int Value(int slot)
            {
                return values[slot];
            }

            public override int CompareDocToValue(int doc, int valueObj)
            {
                int value = valueObj;
                int docValue = currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && docValue == 0 && !docsWithField[doc])
                {
                    docValue = missingValue.GetValueOrDefault();
                }
                if (docValue < value)
                {
                    return -1;
                }
                else if (docValue > value)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>Parses field's values as long (using <see cref="FieldCache.GetLongs(Lucene.Net.Index.IndexReader,string)" />
        /// and sorts by ascending value 
        /// </summary>
        public sealed class LongComparator : NumericComparator<long>
        {
            private readonly long[] values;
            private readonly FieldCache.ILongParser parser;
            private FieldCache.Longs currentReaderValues;
            private long bottom;

            internal LongComparator(int numHits, string field, FieldCache.IParser parser, long missingValue)
                : base(field, missingValue)
            {
                values = new long[numHits];
                this.parser = (FieldCache.ILongParser)parser;
            }

            public override int Compare(int slot1, int slot2)
            {
                // TODO: there are sneaky non-branch ways to compute
                // -1/+1/0 sign
                long v1 = values[slot1];
                long v2 = values[slot2];
                if (v1 > v2)
                {
                    return 1;
                }
                else if (v1 < v2)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }

            public override int CompareBottom(int doc)
            {
                // TODO: there are sneaky non-branch ways to compute
                // -1/+1/0 sign
                long v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }

                if (bottom > v2)
                {
                    return 1;
                }
                else if (bottom < v2)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }

            public override void Copy(int slot, int doc)
            {
                long v2 = currentReaderValues.Get(doc);
                // Test for v2 == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && v2 == 0 && !docsWithField[doc])
                {
                    v2 = missingValue.GetValueOrDefault();
                }

                values[slot] = v2;
            }

            public override FieldComparator<long> SetNextReader(AtomicReaderContext context)
            {
                // NOTE: must do this before calling super otherwise
                // we compute the docsWithField Bits twice!
                currentReaderValues = FieldCache.DEFAULT.GetLongs(context.Reader, field, parser, missingValue != null);
                return base.SetNextReader(context);
            }

            public override void SetBottom(int bottom)
            {
                this.bottom = values[bottom];
            }

            public override long Value(int slot)
            {
                return values[slot];
            }

            public override int CompareDocToValue(int doc, long valueObj)
            {
                long value = valueObj;
                long docValue = currentReaderValues.Get(doc);
                // Test for docValue == 0 to save Bits.get method call for
                // the common case (doc has value and value is non-zero):
                if (docsWithField != null && docValue == 0 && !docsWithField[doc])
                {
                    docValue = missingValue.GetValueOrDefault();
                }
                if (docValue < value)
                {
                    return -1;
                }
                else if (docValue > value)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>Sorts by descending relevance.  NOTE: if you are
        /// sorting only by descending relevance and then
        /// secondarily by ascending docID, peformance is faster
        /// using <see cref="TopScoreDocCollector" /> directly (which <see cref="Searcher.Search(Query, int)" />
        /// uses when no <see cref="Sort" /> is
        /// specified). 
        /// </summary>
        public sealed class RelevanceComparator : FieldComparator<float>
        {
            private readonly float[] scores;
            private float bottom;
            private Scorer scorer;

            internal RelevanceComparator(int numHits)
            {
                scores = new float[numHits];
            }

            public override int Compare(int slot1, int slot2)
            {
                return scores[slot1].CompareTo(scores[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                float score = scorer.Score();
                //assert !Float.isNaN(score);
                return score.CompareTo(bottom);
            }

            public override void Copy(int slot, int doc)
            {
                scores[slot] = scorer.Score();
                //assert !Float.isNaN(scores[slot]);
            }

            public override FieldComparator<float> SetNextReader(AtomicReaderContext context)
            {
                return this;
            }

            public override void SetBottom(int bottom)
            {
                this.bottom = scores[bottom];
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

            public override float Value(int slot)
            {
                return scores[slot];
            }

            public override int CompareValues(float first, float second)
            {
                // Reversed intentionally because relevance by default
                // sorts descending:
                return second.CompareTo(first);
            }

            public override int CompareDocToValue(int doc, float valueObj)
            {
                float value = valueObj;
                float docValue = scorer.Score();
                //assert !Float.isNaN(docValue);
                return value.CompareTo(docValue);
            }
        }

        /// <summary>Sorts by ascending docID </summary>
        public sealed class DocComparator : FieldComparator<int>
        {
            private readonly int[] docIDs;
            private int docBase;
            private int bottom;

            internal DocComparator(int numHits)
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

            public override FieldComparator<int> SetNextReader(AtomicReaderContext context)
            {
                // TODO: can we "map" our docIDs to the current
                // reader? saves having to then subtract on every
                // compare call
                this.docBase = context.docBase;
                return this;
            }

            public override void SetBottom(int bottom)
            {
                this.bottom = docIDs[bottom];
            }

            public override int Value(int slot)
            {
                return docIDs[slot];
            }

            public override int CompareDocToValue(int doc, int valueObj)
            {
                int value = valueObj;
                int docValue = docBase + doc;
                if (docValue < value)
                {
                    return -1;
                }
                else if (docValue > value)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public sealed class TermOrdValComparator : FieldComparator<BytesRef>
        {
            internal readonly int[] ords;
            internal readonly BytesRef[] values;
            internal readonly int[] readerGen;
            internal int currentReaderGen = -1;
            internal SortedDocValues termsIndex;
            private readonly string field;
            internal int bottomSlot = -1;
            internal int bottomOrd;
            internal bool bottomSameReader;
            internal BytesRef bottomValue;
            internal readonly BytesRef tempBR = new BytesRef();

            public TermOrdValComparator(int numHits, string field)
            {
                ords = new int[numHits];
                values = new BytesRef[numHits];
                readerGen = new int[numHits];
                this.field = field;
            }

            public override int Compare(int slot1, int slot2)
            {
                if (readerGen[slot1] == readerGen[slot2])
                {
                    return ords[slot1] - ords[slot2];
                }

                BytesRef val1 = values[slot1];
                BytesRef val2 = values[slot2];
                if (val1 == null)
                {
                    if (val2 == null)
                    {
                        return 0;
                    }
                    return -1;
                }
                else if (val2 == null)
                {
                    return 1;
                }
                return val1.CompareTo(val2);
            }

            public override int CompareBottom(int doc)
            {
                throw new NotSupportedException();
            }

            public override void Copy(int slot, int doc)
            {
                throw new NotSupportedException();
            }

            public override int CompareDocToValue(int doc, BytesRef value)
            {
                int ord = termsIndex.GetOrd(doc);
                if (ord == -1)
                {
                    if (value == null)
                    {
                        return 0;
                    }
                    return -1;
                }
                else if (value == null)
                {
                    return 1;
                }
                termsIndex.LookupOrd(ord, tempBR);
                return tempBR.CompareTo(value);
            }

            internal abstract class PerSegmentComparator : FieldComparator<BytesRef>
            {
                protected readonly TermOrdValComparator parent;

                public PerSegmentComparator(TermOrdValComparator parent)
                {
                    this.parent = parent;
                }

                public override FieldComparator<BytesRef> SetNextReader(AtomicReaderContext context)
                {
                    return parent.SetNextReader(context);
                }

                public override int Compare(int slot1, int slot2)
                {
                    return parent.Compare(slot1, slot2);
                }

                public override void SetBottom(int slot)
                {
                    parent.SetBottom(slot);
                }

                public override BytesRef Value(int slot)
                {
                    return parent.Value(slot);
                }

                public override int CompareValues(BytesRef val1, BytesRef val2)
                {
                    if (val1 == null)
                    {
                        if (val2 == null)
                        {
                            return 0;
                        }
                        return -1;
                    }
                    else if (val2 == null)
                    {
                        return 1;
                    }
                    return val1.CompareTo(val2);
                }

                public override int CompareDocToValue(int doc, BytesRef value)
                {
                    return parent.CompareDocToValue(doc, value);
                }
            }

            private sealed class AnyOrdComparator : PerSegmentComparator
            {
                private readonly SortedDocValues termsIndex;
                private readonly int docBase;

                public AnyOrdComparator(TermOrdValComparator parent, SortedDocValues termsIndex, int docBase)
                    : base(parent)
                {
                    this.termsIndex = termsIndex;
                    this.docBase = docBase;
                }

                public override int CompareBottom(int doc)
                {
                    //assert bottomSlot != -1;
                    int docOrd = termsIndex.GetOrd(doc);
                    if (parent.bottomSameReader)
                    {
                        // ord is precisely comparable, even in the equal case
                        return parent.bottomOrd - docOrd;
                    }
                    else if (parent.bottomOrd >= docOrd)
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
                    parent.ords[slot] = ord;
                    if (ord == -1)
                    {
                        parent.values[slot] = null;
                    }
                    else
                    {
                        //assert ord >= 0;
                        if (parent.values[slot] == null)
                        {
                            parent.values[slot] = new BytesRef();
                        }
                        termsIndex.LookupOrd(ord, parent.values[slot]);
                    }
                    parent.readerGen[slot] = parent.currentReaderGen;
                }
            }

            public override FieldComparator<BytesRef> SetNextReader(AtomicReaderContext context)
            {
                int docBase = context.docBase;
                termsIndex = FieldCache.DEFAULT.GetTermsIndex(context.Reader, field);
                FieldComparator<BytesRef> perSegComp = new AnyOrdComparator(this, termsIndex, docBase);
                currentReaderGen++;
                if (bottomSlot != -1)
                {
                    perSegComp.SetBottom(bottomSlot);
                }

                return perSegComp;
            }

            public override void SetBottom(int bottom)
            {
                bottomSlot = bottom;

                bottomValue = values[bottomSlot];
                if (currentReaderGen == readerGen[bottomSlot])
                {
                    bottomOrd = ords[bottomSlot];
                    bottomSameReader = true;
                }
                else
                {
                    if (bottomValue == null)
                    {
                        // -1 ord is null for all segments
                        //assert ords[bottomSlot] == -1;
                        bottomOrd = -1;
                        bottomSameReader = true;
                        readerGen[bottomSlot] = currentReaderGen;
                    }
                    else
                    {
                        int index = termsIndex.LookupTerm(bottomValue);
                        if (index < 0)
                        {
                            bottomOrd = -index - 2;
                            bottomSameReader = false;
                        }
                        else
                        {
                            bottomOrd = index;
                            // exact value match
                            bottomSameReader = true;
                            readerGen[bottomSlot] = currentReaderGen;
                            ords[bottomSlot] = bottomOrd;
                        }
                    }
                }
            }

            public override BytesRef Value(int slot)
            {
                return values[slot];
            }
        }

        public sealed class TermValComparator : FieldComparator<BytesRef>
        {
            private BytesRef[] values;
            private BinaryDocValues docTerms;
            private readonly String field;
            private BytesRef bottom;
            private readonly BytesRef tempBR = new BytesRef();

            internal TermValComparator(int numHits, string field)
            {
                values = new BytesRef[numHits];
                this.field = field;
            }

            public override int Compare(int slot1, int slot2)
            {
                BytesRef val1 = values[slot1];
                BytesRef val2 = values[slot2];
                if (val1 == null)
                {
                    if (val2 == null)
                    {
                        return 0;
                    }
                    return -1;
                }
                else if (val2 == null)
                {
                    return 1;
                }

                return val1.CompareTo(val2);
            }

            public override int CompareBottom(int doc)
            {
                docTerms.Get(doc, tempBR);
                if (bottom.bytes == BinaryDocValues.MISSING)
                {
                    if (tempBR.bytes == BinaryDocValues.MISSING)
                    {
                        return 0;
                    }
                    return -1;
                }
                else if (tempBR.bytes == BinaryDocValues.MISSING)
                {
                    return 1;
                }
                return bottom.CompareTo(tempBR);
            }

            public override void Copy(int slot, int doc)
            {
                if (values[slot] == null)
                {
                    values[slot] = new BytesRef();
                }
                docTerms.Get(doc, values[slot]);
            }

            public override FieldComparator<BytesRef> SetNextReader(AtomicReaderContext context)
            {
                docTerms = FieldCache.DEFAULT.GetTerms(context.Reader, field);
                return this;
            }

            public override void SetBottom(int bottom)
            {
                this.bottom = values[bottom];
            }

            public override BytesRef Value(int slot)
            {
                return values[slot];
            }

            public override int CompareValues(BytesRef val1, BytesRef val2)
            {
                if (val1 == null)
                {
                    if (val2 == null)
                    {
                        return 0;
                    }
                    return -1;
                }
                else if (val2 == null)
                {
                    return 1;
                }
                return val1.CompareTo(val2);
            }

            public override int CompareDocToValue(int doc, BytesRef value)
            {
                docTerms.Get(doc, tempBR);
                return tempBR.CompareTo(value);
            }
        }
    }
}