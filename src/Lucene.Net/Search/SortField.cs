using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using StringHelper = Lucene.Net.Util.StringHelper;

    /// <summary>
    /// Stores information about how to sort documents by terms in an individual
    /// field.  Fields must be indexed in order to sort by them.
    ///
    /// <para/>Created: Feb 11, 2004 1:25:29 PM
    /// <para/>
    /// @since   lucene 1.4 </summary>
    /// <seealso cref="Sort"/>
    public class SortField
    {
        // LUCENENET NOTE: de-nested the Type enum and renamed to SortFieldType to avoid potential naming collisions with System.Type

        /// <summary>
        /// Represents sorting by document score (relevance). </summary>
        public static readonly SortField FIELD_SCORE = new SortField(null, SortFieldType.SCORE);

        /// <summary>
        /// Represents sorting by document number (index order). </summary>
        public static readonly SortField FIELD_DOC = new SortField(null, SortFieldType.DOC);

        private string field;
        private SortFieldType type; // defaults to determining type dynamically
        internal bool reverse = false; // defaults to natural order
        private readonly FieldCache.IParser parser; // LUCENENET: marked readonly

        // Used for CUSTOM sort
        private readonly FieldComparerSource comparerSource; // LUCENENET: marked readonly

        // Used for 'sortMissingFirst/Last'
        public object MissingValue => m_missingValue;

        protected object m_missingValue = null; // LUCENENET NOTE: added protected backing field

        /// <summary>
        /// Creates a sort by terms in the given field with the type of term
        /// values explicitly given. </summary>
        /// <param name="field"> Name of field to sort by. Can be <c>null</c> if
        ///               <paramref name="type"/> is <see cref="SortFieldType.SCORE"/> or <see cref="SortFieldType.DOC"/>. </param>
        /// <param name="type"> Type of values in the terms. </param>
        public SortField(string field, SortFieldType type)
        {
            InitFieldType(field, type);
        }

        /// <summary>
        /// Creates a sort, possibly in reverse, by terms in the given field with the
        /// type of term values explicitly given. </summary>
        /// <param name="field">  Name of field to sort by.  Can be <c>null</c> if
        ///               <paramref name="type"/> is <see cref="SortFieldType.SCORE"/> or <see cref="SortFieldType.DOC"/>. </param>
        /// <param name="type">   Type of values in the terms. </param>
        /// <param name="reverse"> <c>True</c> if natural order should be reversed. </param>
        public SortField(string field, SortFieldType type, bool reverse)
        {
            InitFieldType(field, type);
            this.reverse = reverse;
        }

        /// <summary>
        /// Creates a sort by terms in the given field, parsed
        /// to numeric values using a custom <see cref="FieldCache.IParser"/>. </summary>
        /// <param name="field">  Name of field to sort by.  Must not be <c>null</c>. </param>
        /// <param name="parser"> Instance of a <see cref="FieldCache.IParser"/>,
        ///  which must subclass one of the existing numeric
        ///  parsers from <see cref="IFieldCache"/>. Sort type is inferred
        ///  by testing which numeric parser the parser subclasses. </param>
        /// <exception cref="ArgumentException"> if the parser fails to
        ///  subclass an existing numeric parser, or field is <c>null</c> </exception>
        public SortField(string field, FieldCache.IParser parser)
            : this(field, parser, false)
        {
        }

        /// <summary>
        /// Creates a sort, possibly in reverse, by terms in the given field, parsed
        /// to numeric values using a custom <see cref="FieldCache.IParser"/>. </summary>
        /// <param name="field">  Name of field to sort by.  Must not be <c>null</c>. </param>
        /// <param name="parser"> Instance of a <see cref="FieldCache.IParser"/>,
        ///  which must subclass one of the existing numeric
        ///  parsers from <see cref="IFieldCache"/>. Sort type is inferred
        ///  by testing which numeric parser the parser subclasses. </param>
        /// <param name="reverse"> <c>True</c> if natural order should be reversed. </param>
        /// <exception cref="ArgumentException"> if the parser fails to
        ///  subclass an existing numeric parser, or field is <c>null</c> </exception>
        public SortField(string field, FieldCache.IParser parser, bool reverse)
        {
            if (parser is FieldCache.IInt32Parser)
            {
                InitFieldType(field, SortFieldType.INT32);
            }
            else if (parser is FieldCache.ISingleParser)
            {
                InitFieldType(field, SortFieldType.SINGLE);
            }
#pragma warning disable 612, 618
            else if (parser is FieldCache.IInt16Parser)
            {
                InitFieldType(field, SortFieldType.INT16);
            }
            else if (parser is FieldCache.IByteParser)
            {
                InitFieldType(field, SortFieldType.BYTE);
#pragma warning restore 612, 618
            }
            else if (parser is FieldCache.IInt64Parser)
            {
                InitFieldType(field, SortFieldType.INT64);
            }
            else if (parser is FieldCache.IDoubleParser)
            {
                InitFieldType(field, SortFieldType.DOUBLE);
            }
            else
            {
                throw new ArgumentException("Parser instance does not subclass existing numeric parser from FieldCache (got " + parser + ")");
            }

            this.reverse = reverse;
            this.parser = parser;
        }

        /// <summary>
        /// Pass this to <see cref="MissingValue"/> to have missing
        /// string values sort first.
        /// </summary>
        public static readonly object STRING_FIRST = new ObjectAnonymousClass();

        private sealed class ObjectAnonymousClass : object
        {
            public ObjectAnonymousClass()
            {
            }

            public override string ToString()
            {
                return "SortField.STRING_FIRST";
            }
        }

        /// <summary>
        /// Pass this to <see cref="MissingValue"/> to have missing
        /// string values sort last.
        /// </summary>
        public static readonly object STRING_LAST = new ObjectAnonymousClass2();

        private sealed class ObjectAnonymousClass2 : object
        {
            public ObjectAnonymousClass2()
            {
            }

            public override string ToString()
            {
                return "SortField.STRING_LAST";
            }
        }

        public virtual void SetMissingValue(object value)
        {
            if (type == SortFieldType.STRING)
            {
                if (value != STRING_FIRST && value != STRING_LAST)
                {
                    throw new ArgumentException("For STRING type, missing value must be either STRING_FIRST or STRING_LAST");
                }
            }
#pragma warning disable 612, 618
            else if (type != SortFieldType.BYTE && type != SortFieldType.INT16
#pragma warning restore 612, 618
                && type != SortFieldType.INT32 && type != SortFieldType.SINGLE && type != SortFieldType.INT64 && type != SortFieldType.DOUBLE)
            {
                throw new ArgumentException("Missing value only works for numeric or STRING types");
            }
            this.m_missingValue = value;
        }

        [CLSCompliant(false)]
        [Obsolete]
        public void SetMissingValue(sbyte value)
        {
            SetMissingValue((object)J2N.Numerics.SByte.GetInstance(value));
        }

        [Obsolete]
        public void SetMissingValue(byte value)
        {
            SetMissingValue((object)J2N.Numerics.SByte.GetInstance((sbyte)value));
        }

        [Obsolete]
        public void SetMissingValue(short value)
        {
            SetMissingValue((object)J2N.Numerics.Int16.GetInstance(value));
        }

        public void SetMissingValue(int value)
        {
            SetMissingValue((object)J2N.Numerics.Int32.GetInstance(value));
        }

        public void SetMissingValue(long value)
        {
            SetMissingValue((object)J2N.Numerics.Int64.GetInstance(value));
        }

        public void SetMissingValue(double value)
        {
            SetMissingValue((object)J2N.Numerics.Double.GetInstance(value));
        }

        public void SetMissingValue(float value)
        {
            SetMissingValue((object)J2N.Numerics.Single.GetInstance(value));
        }

        /// <summary>
        /// Creates a sort with a custom comparison function. </summary>
        /// <param name="field"> Name of field to sort by; cannot be <c>null</c>. </param>
        /// <param name="comparer"> Returns a comparer for sorting hits. </param>
        public SortField(string field, FieldComparerSource comparer)
        {
            InitFieldType(field, SortFieldType.CUSTOM);
            this.comparerSource = comparer;
        }

        /// <summary>
        /// Creates a sort, possibly in reverse, with a custom comparison function. </summary>
        /// <param name="field"> Name of field to sort by; cannot be <c>null</c>. </param>
        /// <param name="comparer"> Returns a comparer for sorting hits. </param>
        /// <param name="reverse"> <c>True</c> if natural order should be reversed. </param>
        public SortField(string field, FieldComparerSource comparer, bool reverse)
        {
            InitFieldType(field, SortFieldType.CUSTOM);
            this.reverse = reverse;
            this.comparerSource = comparer;
        }

        // Sets field & type, and ensures field is not NULL unless
        // type is SCORE or DOC
        private void InitFieldType(string field, SortFieldType type)
        {
            this.type = type;
            if (field is null)
            {
                if (type != SortFieldType.SCORE && type != SortFieldType.DOC)
                {
                    throw new ArgumentException("field can only be null when type is SCORE or DOC");
                }
            }
            else
            {
                this.field = field;
            }
        }

        /// <summary>
        /// Returns the name of the field.  Could return <c>null</c>
        /// if the sort is by <see cref="SortFieldType.SCORE"/> or <see cref="SortFieldType.DOC"/>. </summary>
        /// <returns> Name of field, possibly <c>null</c>. </returns>
        public virtual string Field => field;

        /// <summary>
        /// Returns the type of contents in the field. </summary>
        /// <returns> One of <see cref="SortFieldType.SCORE"/>, <see cref="SortFieldType.DOC"/>, 
        /// <see cref="SortFieldType.STRING"/>, <see cref="SortFieldType.INT32"/> or <see cref="SortFieldType.SINGLE"/>. </returns>
        public virtual SortFieldType Type => type;

        /// <summary>
        /// Returns the instance of a <see cref="IFieldCache"/> parser that fits to the given sort type.
        /// May return <c>null</c> if no parser was specified. Sorting is using the default parser then. </summary>
        /// <returns> An instance of a <see cref="IFieldCache"/> parser, or <c>null</c>. </returns>
        public virtual FieldCache.IParser Parser => parser;

        /// <summary>
        /// Returns whether the sort should be reversed. </summary>
        /// <returns> <c>True</c> if natural order should be reversed. </returns>
        public virtual bool IsReverse => reverse;

        /// <summary>
        /// Returns the <see cref="FieldComparerSource"/> used for
        /// custom sorting.
        /// </summary>
        public virtual FieldComparerSource ComparerSource => comparerSource;

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            switch (type)
            {
                case SortFieldType.SCORE:
                    buffer.Append("<score>");
                    break;

                case SortFieldType.DOC:
                    buffer.Append("<doc>");
                    break;

                case SortFieldType.STRING:
                    buffer.Append("<string" + ": \"").Append(field).Append("\">");
                    break;

                case SortFieldType.STRING_VAL:
                    buffer.Append("<string_val" + ": \"").Append(field).Append("\">");
                    break;

#pragma warning disable 612, 618
                case SortFieldType.BYTE:
                    buffer.Append("<byte: \"").Append(field).Append("\">");
                    break;

                case SortFieldType.INT16:
#pragma warning restore 612, 618
                    buffer.Append("<short: \"").Append(field).Append("\">");
                    break;

                case SortFieldType.INT32:
                    buffer.Append("<int" + ": \"").Append(field).Append("\">");
                    break;

                case SortFieldType.INT64:
                    buffer.Append("<long: \"").Append(field).Append("\">");
                    break;

                case SortFieldType.SINGLE:
                    buffer.Append("<float" + ": \"").Append(field).Append("\">");
                    break;

                case SortFieldType.DOUBLE:
                    buffer.Append("<double" + ": \"").Append(field).Append("\">");
                    break;

                case SortFieldType.CUSTOM:
                    buffer.Append("<custom:\"").Append(field).Append("\": ").Append(comparerSource).Append('>');
                    break;

                case SortFieldType.REWRITEABLE:
                    buffer.Append("<rewriteable: \"").Append(field).Append("\">");
                    break;

                default:
                    buffer.Append("<???: \"").Append(field).Append("\">");
                    break;
            }

            if (reverse)
            {
                buffer.Append('!');
            }
            if (m_missingValue != null)
            {
                buffer.Append(" missingValue=");
                buffer.Append(m_missingValue);
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="o"/> is equal to this.  If a
        /// <see cref="FieldComparerSource"/> or 
        /// <see cref="FieldCache.IParser"/> was provided, it must properly
        /// implement equals (unless a singleton is always used).
        /// </summary>
        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is SortField))
            {
                return false;
            }
            SortField other = (SortField)o;
            return (StringHelper.Equals(other.field, this.field) 
                && other.type == this.type 
                && other.reverse == this.reverse 
                && (other.comparerSource is null ? this.comparerSource is null : other.comparerSource.Equals(this.comparerSource)));
        }

        /// <summary>
        /// Returns a hash code value for this object.  If a
        /// <see cref="FieldComparerSource"/> or
        /// <see cref="FieldCache.IParser"/> was provided, it must properly
        /// implement GetHashCode() (unless a singleton is always
        /// used).
        /// </summary>
        public override int GetHashCode()
        {
            int hash = (int)(type.GetHashCode() ^ 0x346565dd + reverse.GetHashCode() ^ 0xaf5998bb);
            if (field != null)
            {
                hash += (int)(field.GetHashCode() ^ 0xff5685dd);
            }
            if (comparerSource != null)
            {
                hash += comparerSource.GetHashCode();
            }
            return hash;
        }

        private IComparer<BytesRef> bytesComparer = BytesRef.UTF8SortedAsUnicodeComparer;

        public virtual IComparer<BytesRef> BytesComparer
        {
            get => bytesComparer;
            set => bytesComparer = value;
        }

        /// <summary>
        /// Returns the <see cref="FieldComparer"/> to use for
        /// sorting.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <param name="numHits"> Number of top hits the queue will store </param>
        /// <param name="sortPos"> Position of this <see cref="SortField"/> within 
        ///   <see cref="Sort"/>.  The comparer is primary if sortPos==0,
        ///   secondary if sortPos==1, etc.  Some comparers can
        ///   optimize themselves when they are the primary sort. </param>
        /// <returns> <see cref="FieldComparer"/> to use when sorting </returns>
        public virtual FieldComparer GetComparer(int numHits, int sortPos)
        {
            switch (type)
            {
                case SortFieldType.SCORE:
                    return new FieldComparer.RelevanceComparer(numHits);

                case SortFieldType.DOC:
                    return new FieldComparer.DocComparer(numHits);

                case SortFieldType.INT32:
                    return new FieldComparer.Int32Comparer(numHits, field, parser, (J2N.Numerics.Int32)m_missingValue);

                case SortFieldType.SINGLE:
                    return new FieldComparer.SingleComparer(numHits, field, parser, (J2N.Numerics.Single)m_missingValue);

                case SortFieldType.INT64:
                    return new FieldComparer.Int64Comparer(numHits, field, parser, (J2N.Numerics.Int64)m_missingValue);

                case SortFieldType.DOUBLE:
                    return new FieldComparer.DoubleComparer(numHits, field, parser, (J2N.Numerics.Double)m_missingValue);

#pragma warning disable 612, 618
                case SortFieldType.BYTE:
                    return new FieldComparer.ByteComparer(numHits, field, parser, (J2N.Numerics.SByte)m_missingValue);

                case SortFieldType.INT16:
                    return new FieldComparer.Int16Comparer(numHits, field, parser, (J2N.Numerics.Int16)m_missingValue);
#pragma warning restore 612, 618

                case SortFieldType.CUSTOM:
                    if (Debugging.AssertsEnabled) Debugging.Assert(comparerSource != null);
                    return comparerSource.NewComparer(field, numHits, sortPos, reverse);

                case SortFieldType.STRING:
                    return new FieldComparer.TermOrdValComparer(numHits, field, m_missingValue == STRING_LAST);

                case SortFieldType.STRING_VAL:
                    // TODO: should we remove this?  who really uses it?
                    return new FieldComparer.TermValComparer(numHits, field);

                case SortFieldType.REWRITEABLE:
                    throw IllegalStateException.Create("SortField needs to be rewritten through Sort.Rewrite(..) and SortField.Rewrite(..)");

                default:
                    throw IllegalStateException.Create("Illegal sort type: " + type);
            }
        }

        /// <summary>
        /// Rewrites this <see cref="SortField"/>, returning a new <see cref="SortField"/> if a change is made.
        /// Subclasses should override this define their rewriting behavior when this
        /// SortField is of type <see cref="SortFieldType.REWRITEABLE"/>.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <param name="searcher"> <see cref="IndexSearcher"/> to use during rewriting </param>
        /// <returns> New rewritten <see cref="SortField"/>, or <c>this</c> if nothing has changed. </returns>
        /// <exception cref="IOException"> Can be thrown by the rewriting </exception>
        public virtual SortField Rewrite(IndexSearcher searcher)
        {
            return this;
        }

        /// <summary>
        /// Whether the relevance score is needed to sort documents. </summary>
        public virtual bool NeedsScores => type == SortFieldType.SCORE;
    }

    /// <summary>
    /// Specifies the type of the terms to be sorted, or special types such as CUSTOM
    /// </summary>
    public enum SortFieldType // LUCENENET NOTE: de-nested and renamed from Type to avoid naming collision with Type property and with System.Type
    {
        /// <summary>
        /// Sort by document score (relevance).  Sort values are <see cref="float"/> and higher
        /// values are at the front.
        /// </summary>
        SCORE,

        /// <summary>
        /// Sort by document number (index order).  Sort values are <see cref="int"/> and lower
        /// values are at the front.
        /// </summary>
        DOC,

        /// <summary>
        /// Sort using term values as <see cref="string"/>s.  Sort values are <see cref="string"/>s and lower
        /// values are at the front.
        /// </summary>
        STRING,

        /// <summary>
        /// Sort using term values as encoded <see cref="int"/>s.  Sort values are <see cref="int"/> and
        /// lower values are at the front.
        /// <para/>
        /// NOTE: This was INT in Lucene
        /// </summary>
        INT32,

        /// <summary>
        /// Sort using term values as encoded <see cref="float"/>s.  Sort values are <see cref="float"/> and
        /// lower values are at the front.
        /// <para/>
        /// NOTE: This was FLOAT in Lucene
        /// </summary>
        SINGLE,

        /// <summary>
        /// Sort using term values as encoded <see cref="long"/>s.  Sort values are <see cref="long"/> and
        /// lower values are at the front.
        /// <para/>
        /// NOTE: This was LONG in Lucene
        /// </summary>
        INT64,

        /// <summary>
        /// Sort using term values as encoded <see cref="double"/>s.  Sort values are <see cref="double"/> and
        /// lower values are at the front.
        /// </summary>
        DOUBLE,

        /// <summary>
        /// Sort using term values as encoded <see cref="short"/>s.  Sort values are <see cref="short"/> and
        /// lower values are at the front.
        /// <para/>
        /// NOTE: This was SHORT in Lucene
        /// </summary>
        [System.Obsolete]
        INT16,

        /// <summary>
        /// Sort using a custom <see cref="IComparer{T}"/>.  Sort values are any <see cref="IComparable{T}"/> and
        /// sorting is done according to natural order.
        /// </summary>
        CUSTOM,

        /// <summary>
        /// Sort using term values as encoded <see cref="byte"/>s.  Sort values are <see cref="byte"/> and
        /// lower values are at the front.
        /// </summary>
        [System.Obsolete]
        BYTE,

        /// <summary>
        /// Sort using term values as <see cref="string"/>s, but comparing by
        /// value (using <see cref="BytesRef.CompareTo(BytesRef)"/>) for all comparisons.
        /// this is typically slower than <see cref="STRING"/>, which
        /// uses ordinals to do the sorting.
        /// </summary>
        STRING_VAL,

        /// <summary>
        /// Sort use <see cref="T:byte[]"/> index values. </summary>
        BYTES,

        /// <summary>
        /// Force rewriting of <see cref="SortField"/> using <see cref="SortField.Rewrite(IndexSearcher)"/>
        /// before it can be used for sorting
        /// </summary>
        REWRITEABLE
    }
}