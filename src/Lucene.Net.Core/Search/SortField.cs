using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <p>Created: Feb 11, 2004 1:25:29 PM
    ///
    /// @since   lucene 1.4 </summary>
    /// <seealso cref= Sort </seealso>
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
        private FieldCache.IParser parser;

        // Used for CUSTOM sort
        private FieldComparatorSource comparatorSource;

        // Used for 'sortMissingFirst/Last'
        public object missingValue = null;

        /// <summary>
        /// Creates a sort by terms in the given field with the type of term
        /// values explicitly given. </summary>
        /// <param name="field">  Name of field to sort by.  Can be <code>null</code> if
        ///               <code>type</code> is SCORE or DOC. </param>
        /// <param name="type">   Type of values in the terms. </param>
        public SortField(string field, SortFieldType type)
        {
            InitFieldType(field, type);
        }

        /// <summary>
        /// Creates a sort, possibly in reverse, by terms in the given field with the
        /// type of term values explicitly given. </summary>
        /// <param name="field">  Name of field to sort by.  Can be <code>null</code> if
        ///               <code>type</code> is SCORE or DOC. </param>
        /// <param name="type">   Type of values in the terms. </param>
        /// <param name="reverse"> True if natural order should be reversed. </param>
        public SortField(string field, SortFieldType type, bool reverse)
        {
            InitFieldType(field, type);
            this.reverse = reverse;
        }

        /// <summary>
        /// Creates a sort by terms in the given field, parsed
        /// to numeric values using a custom <seealso cref="IFieldCache.Parser"/>. </summary>
        /// <param name="field">  Name of field to sort by.  Must not be null. </param>
        /// <param name="parser"> Instance of a <seealso cref="IFieldCache.Parser"/>,
        ///  which must subclass one of the existing numeric
        ///  parsers from <seealso cref="IFieldCache"/>. Sort type is inferred
        ///  by testing which numeric parser the parser subclasses. </param>
        /// <exception cref="IllegalArgumentException"> if the parser fails to
        ///  subclass an existing numeric parser, or field is null </exception>
        public SortField(string field, FieldCache.IParser parser)
            : this(field, parser, false)
        {
        }

        /// <summary>
        /// Creates a sort, possibly in reverse, by terms in the given field, parsed
        /// to numeric values using a custom <seealso cref="IFieldCache.Parser"/>. </summary>
        /// <param name="field">  Name of field to sort by.  Must not be null. </param>
        /// <param name="parser"> Instance of a <seealso cref="IFieldCache.Parser"/>,
        ///  which must subclass one of the existing numeric
        ///  parsers from <seealso cref="IFieldCache"/>. Sort type is inferred
        ///  by testing which numeric parser the parser subclasses. </param>
        /// <param name="reverse"> True if natural order should be reversed. </param>
        /// <exception cref="IllegalArgumentException"> if the parser fails to
        ///  subclass an existing numeric parser, or field is null </exception>
        public SortField(string field, FieldCache.IParser parser, bool reverse)
        {
            if (parser is FieldCache.IIntParser)
            {
                InitFieldType(field, SortFieldType.INT);
            }
            else if (parser is FieldCache.IFloatParser)
            {
                InitFieldType(field, SortFieldType.FLOAT);
            }
            else if (parser is FieldCache.IShortParser)
            {
                InitFieldType(field, SortFieldType.SHORT);
            }
            else if (parser is FieldCache.IByteParser)
            {
                InitFieldType(field, SortFieldType.BYTE);
            }
            else if (parser is FieldCache.ILongParser)
            {
                InitFieldType(field, SortFieldType.LONG);
            }
            else if (parser is FieldCache.IDoubleParser)
            {
                InitFieldType(field, SortFieldType.DOUBLE);
            }
            else
            {
                throw new System.ArgumentException("Parser instance does not subclass existing numeric parser from FieldCache (got " + parser + ")");
            }

            this.reverse = reverse;
            this.parser = parser;
        }

        /// <summary>
        /// Pass this to <seealso cref="#setMissingValue"/> to have missing
        ///  string values sort first.
        /// </summary>
        public static readonly object STRING_FIRST = new ObjectAnonymousInnerClassHelper();

        private class ObjectAnonymousInnerClassHelper : object
        {
            public ObjectAnonymousInnerClassHelper()
            {
            }

            public override string ToString()
            {
                return "SortField.STRING_FIRST";
            }
        }

        /// <summary>
        /// Pass this to <seealso cref="#setMissingValue"/> to have missing
        ///  string values sort last.
        /// </summary>
        public static readonly object STRING_LAST = new ObjectAnonymousInnerClassHelper2();

        private class ObjectAnonymousInnerClassHelper2 : object
        {
            public ObjectAnonymousInnerClassHelper2()
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
                    throw new System.ArgumentException("For STRING type, missing value must be either STRING_FIRST or STRING_LAST");
                }
            }
            else if (type != SortFieldType.BYTE && type != SortFieldType.SHORT && type != SortFieldType.INT && type != SortFieldType.FLOAT && type != SortFieldType.LONG && type != SortFieldType.DOUBLE)
            {
                throw new System.ArgumentException("Missing value only works for numeric or STRING types");
            }
            this.missingValue = value;
        }

        /// <summary>
        /// Creates a sort with a custom comparison function. </summary>
        /// <param name="field"> Name of field to sort by; cannot be <code>null</code>. </param>
        /// <param name="comparator"> Returns a comparator for sorting hits. </param>
        public SortField(string field, FieldComparatorSource comparator)
        {
            InitFieldType(field, SortFieldType.CUSTOM);
            this.comparatorSource = comparator;
        }

        /// <summary>
        /// Creates a sort, possibly in reverse, with a custom comparison function. </summary>
        /// <param name="field"> Name of field to sort by; cannot be <code>null</code>. </param>
        /// <param name="comparator"> Returns a comparator for sorting hits. </param>
        /// <param name="reverse"> True if natural order should be reversed. </param>
        public SortField(string field, FieldComparatorSource comparator, bool reverse)
        {
            InitFieldType(field, SortFieldType.CUSTOM);
            this.reverse = reverse;
            this.comparatorSource = comparator;
        }

        // Sets field & type, and ensures field is not NULL unless
        // type is SCORE or DOC
        private void InitFieldType(string field, SortFieldType type)
        {
            this.type = type;
            if (field == null)
            {
                if (type != SortFieldType.SCORE && type != SortFieldType.DOC)
                {
                    throw new System.ArgumentException("field can only be null when type is SCORE or DOC");
                }
            }
            else
            {
                this.field = field;
            }
        }

        /// <summary>
        /// Returns the name of the field.  Could return <code>null</code>
        /// if the sort is by SCORE or DOC. </summary>
        /// <returns> Name of field, possibly <code>null</code>. </returns>
        public virtual string Field
        {
            get
            {
                return field;
            }
        }

        /// <summary>
        /// Returns the type of contents in the field. </summary>
        /// <returns> One of the constants SCORE, DOC, STRING, INT or FLOAT. </returns>
        public virtual SortFieldType Type
        {
            get
            {
                return type;
            }
        }

        /// <summary>
        /// Returns the instance of a <seealso cref="IFieldCache"/> parser that fits to the given sort type.
        /// May return <code>null</code> if no parser was specified. Sorting is using the default parser then. </summary>
        /// <returns> An instance of a <seealso cref="IFieldCache"/> parser, or <code>null</code>. </returns>
        public virtual FieldCache.IParser Parser
        {
            get
            {
                return parser;
            }
        }

        /// <summary>
        /// Returns whether the sort should be reversed. </summary>
        /// <returns>  True if natural order should be reversed. </returns>
        public virtual bool Reverse // LUCENENET TODO: Rename IsReverse (consistency)
        {
            get
            {
                return reverse;
            }
        }

        /// <summary>
        /// Returns the <seealso cref="FieldComparatorSource"/> used for
        /// custom sorting
        /// </summary>
        public virtual FieldComparatorSource ComparatorSource // LUCENENET TODO: Rename ComparerSource ?
        {
            get
            {
                return comparatorSource;
            }
        }

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

                case SortFieldType.BYTE:
                    buffer.Append("<byte: \"").Append(field).Append("\">");
                    break;

                case SortFieldType.SHORT:
                    buffer.Append("<short: \"").Append(field).Append("\">");
                    break;

                case SortFieldType.INT:
                    buffer.Append("<int" + ": \"").Append(field).Append("\">");
                    break;

                case SortFieldType.LONG:
                    buffer.Append("<long: \"").Append(field).Append("\">");
                    break;

                case SortFieldType.FLOAT:
                    buffer.Append("<float" + ": \"").Append(field).Append("\">");
                    break;

                case SortFieldType.DOUBLE:
                    buffer.Append("<double" + ": \"").Append(field).Append("\">");
                    break;

                case SortFieldType.CUSTOM:
                    buffer.Append("<custom:\"").Append(field).Append("\": ").Append(comparatorSource).Append('>');
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
            if (missingValue != null)
            {
                buffer.Append(" missingValue=");
                buffer.Append(missingValue);
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Returns true if <code>o</code> is equal to this.  If a
        ///  <seealso cref="FieldComparatorSource"/> or {@link
        ///  FieldCache.Parser} was provided, it must properly
        ///  implement equals (unless a singleton is always used).
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
            return (StringHelper.Equals(other.field, this.field) && other.type == this.type && other.reverse == this.reverse && (other.comparatorSource == null ? this.comparatorSource == null : other.comparatorSource.Equals(this.comparatorSource)));
        }

        /// <summary>
        /// Returns true if <code>o</code> is equal to this.  If a
        ///  <seealso cref="FieldComparatorSource"/> or {@link
        ///  FieldCache.Parser} was provided, it must properly
        ///  implement hashCode (unless a singleton is always
        ///  used).
        /// </summary>
        public override int GetHashCode()
        {
            int hash = (int)(type.GetHashCode() ^ 0x346565dd + reverse.GetHashCode() ^ 0xaf5998bb);
            if (field != null)
            {
                hash += (int)(field.GetHashCode() ^ 0xff5685dd);
            }
            if (comparatorSource != null)
            {
                hash += comparatorSource.GetHashCode();
            }
            return hash;
        }

        private IComparer<BytesRef> BytesComparator_Renamed = BytesRef.UTF8SortedAsUnicodeComparer; // LUCENENET TODO: Rename (private)

        // LUCENENET TODO: Rename BytesComparer ?
        public virtual IComparer<BytesRef> BytesComparator
        {
            set
            {
                BytesComparator_Renamed = value;
            }
            get
            {
                return BytesComparator_Renamed;
            }
        }

        /// <summary>
        /// Returns the <seealso cref="FieldComparator"/> to use for
        /// sorting.
        ///
        /// @lucene.experimental
        /// </summary>
        /// <param name="numHits"> number of top hits the queue will store </param>
        /// <param name="sortPos"> position of this SortField within {@link
        ///   Sort}.  The comparator is primary if sortPos==0,
        ///   secondary if sortPos==1, etc.  Some comparators can
        ///   optimize themselves when they are the primary sort. </param>
        /// <returns> <seealso cref="FieldComparator"/> to use when sorting </returns>
         // LUCENENET TODO: Rename GetComparer ?
        public virtual FieldComparator GetComparator(int numHits, int sortPos)
        {
            switch (type)
            {
                case SortFieldType.SCORE:
                    return new FieldComparator.RelevanceComparator(numHits);

                case SortFieldType.DOC:
                    return new FieldComparator.DocComparator(numHits);

                case SortFieldType.INT:
                    return new FieldComparator.IntComparator(numHits, field, parser, (int?)missingValue);

                case SortFieldType.FLOAT:
                    return new FieldComparator.FloatComparator(numHits, field, parser, (float?)missingValue);

                case SortFieldType.LONG:
                    return new FieldComparator.LongComparator(numHits, field, parser, (long?)missingValue);

                case SortFieldType.DOUBLE:
                    return new FieldComparator.DoubleComparator(numHits, field, parser, (double?)missingValue);

                case SortFieldType.BYTE:
                    return new FieldComparator.ByteComparator(numHits, field, parser, (sbyte?)missingValue);

                case SortFieldType.SHORT:
                    return new FieldComparator.ShortComparator(numHits, field, parser, (short?)missingValue);

                case SortFieldType.CUSTOM:
                    Debug.Assert(comparatorSource != null);
                    return comparatorSource.NewComparator(field, numHits, sortPos, reverse);

                case SortFieldType.STRING:
                    return new FieldComparator.TermOrdValComparator(numHits, field, missingValue == STRING_LAST);

                case SortFieldType.STRING_VAL:
                    // TODO: should we remove this?  who really uses it?
                    return new FieldComparator.TermValComparator(numHits, field);

                case SortFieldType.REWRITEABLE:
                    throw new InvalidOperationException("SortField needs to be rewritten through Sort.rewrite(..) and SortField.rewrite(..)");

                default:
                    throw new InvalidOperationException("Illegal sort type: " + type);
            }
        }

        /// <summary>
        /// Rewrites this SortField, returning a new SortField if a change is made.
        /// Subclasses should override this define their rewriting behavior when this
        /// SortField is of type <seealso cref="SortField.Type#REWRITEABLE"/>
        /// </summary>
        /// <param name="searcher"> IndexSearcher to use during rewriting </param>
        /// <returns> New rewritten SortField, or {@code this} if nothing has changed. </returns>
        /// <exception cref="IOException"> Can be thrown by the rewriting
        /// @lucene.experimental </exception>
        public virtual SortField Rewrite(IndexSearcher searcher)
        {
            return this;
        }

        /// <summary>
        /// Whether the relevance score is needed to sort documents. </summary>
        public virtual bool NeedsScores
        {
            get { return type == SortFieldType.SCORE; }
        }
    }

    /// <summary>
    /// Specifies the type of the terms to be sorted, or special types such as CUSTOM
    /// </summary>
    public enum SortFieldType // LUCENENET NOTE: de-nested and renamed from Type to avoid naming collision with Type property and with System.Type
    {
        /// <summary>
        /// Sort by document score (relevance).  Sort values are Float and higher
        /// values are at the front.
        /// </summary>
        SCORE,

        /// <summary>
        /// Sort by document number (index order).  Sort values are Integer and lower
        /// values are at the front.
        /// </summary>
        DOC,

        /// <summary>
        /// Sort using term values as Strings.  Sort values are String and lower
        /// values are at the front.
        /// </summary>
        STRING,

        /// <summary>
        /// Sort using term values as encoded Integers.  Sort values are Integer and
        /// lower values are at the front.
        /// </summary>
        INT, // LUCENENET TODO: Rename to INT32 ?

        /// <summary>
        /// Sort using term values as encoded Floats.  Sort values are Float and
        /// lower values are at the front.
        /// </summary>
        FLOAT, // LUCENENET TODO: Rename to SINGLE ?

        /// <summary>
        /// Sort using term values as encoded Longs.  Sort values are Long and
        /// lower values are at the front.
        /// </summary>
        LONG,  // LUCENENET TODO: Rename to INT64 ?

        /// <summary>
        /// Sort using term values as encoded Doubles.  Sort values are Double and
        /// lower values are at the front.
        /// </summary>
        DOUBLE,

        /// <summary>
        /// Sort using term values as encoded Shorts.  Sort values are Short and
        /// lower values are at the front.
        /// </summary>
        [System.Obsolete]
        SHORT, // LUCENENET TODO: Rename to INT16 ?

        /// <summary>
        /// Sort using a custom Comparator.  Sort values are any Comparable and
        /// sorting is done according to natural order.
        /// </summary>
        CUSTOM,

        /// <summary>
        /// Sort using term values as encoded Bytes.  Sort values are Byte and
        /// lower values are at the front.
        /// </summary>
        [System.Obsolete]
        BYTE,

        /// <summary>
        /// Sort using term values as Strings, but comparing by
        /// value (using String.compareTo) for all comparisons.
        /// this is typically slower than <seealso cref="#STRING"/>, which
        /// uses ordinals to do the sorting.
        /// </summary>
        STRING_VAL,

        /// <summary>
        /// Sort use byte[] index values. </summary>
        BYTES,

        /// <summary>
        /// Force rewriting of SortField using <seealso cref="SortField#rewrite(IndexSearcher)"/>
        /// before it can be used for sorting
        /// </summary>
        REWRITEABLE
    }
}