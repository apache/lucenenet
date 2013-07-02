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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lucene.Net.Util;
using TermEnum = Lucene.Net.Index.TermsEnum;
using StringHelper = Lucene.Net.Util.StringHelper;

namespace Lucene.Net.Search
{
	/// <summary> Stores information about how to sort documents by terms in an individual
	/// field.  Fields must be indexed in order to sort by them.
	/// 
	/// <p/>Created: Feb 11, 2004 1:25:29 PM
	/// </summary>
	/// <seealso cref="Sort"></seealso>
	[Serializable]
	public class SortField
	{
		/// <summary>Sort by document score (relevancy).  Sort values are Float and higher
		/// values are at the front. 
		/// </summary>
		public const int SCORE = 0;
		
		/// <summary>Sort by document number (index order).  Sort values are Integer and lower
		/// values are at the front. 
		/// </summary>
		public const int DOC = 1;
		
		// reserved, in Lucene 2.9, there was a constant: AUTO = 2
		
		/// <summary>Sort using term values as Strings.  Sort values are String and lower
		/// values are at the front. 
		/// </summary>
		public const int STRING = 3;
		
		/// <summary>Sort using term values as encoded Integers.  Sort values are Integer and
		/// lower values are at the front. 
		/// </summary>
		public const int INT = 4;
		
		/// <summary>Sort using term values as encoded Floats.  Sort values are Float and
		/// lower values are at the front. 
		/// </summary>
		public const int FLOAT = 5;
		
		/// <summary>Sort using term values as encoded Longs.  Sort values are Long and
		/// lower values are at the front. 
		/// </summary>
		public const int LONG = 6;
		
		/// <summary>Sort using term values as encoded Doubles.  Sort values are Double and
		/// lower values are at the front. 
		/// </summary>
		public const int DOUBLE = 7;
		
		/// <summary>Sort using term values as encoded Shorts.  Sort values are Short and
		/// lower values are at the front. 
		/// </summary>
		public const int SHORT = 8;
		
		/// <summary>Sort using a custom Comparator.  Sort values are any Comparable and
		/// sorting is done according to natural order. 
		/// </summary>
		public const int CUSTOM = 9;
		
		/// <summary>Sort using term values as encoded Bytes.  Sort values are Byte and
		/// lower values are at the front. 
		/// </summary>
		public const int BYTE = 10;
		
		/// <summary>Sort using term values as Strings, but comparing by
		/// value (using String.compareTo) for all comparisons.
		/// This is typically slower than <see cref="STRING" />, which
		/// uses ordinals to do the sorting. 
		/// </summary>
		public const int STRING_VAL = 11;

	    public const int BYTES = 12;

        public const int REWRITEABLE = 13;
		
		// IMPLEMENTATION NOTE: the FieldCache.STRING_INDEX is in the same "namespace"
		// as the above static int values.  Any new values must not have the same value
		// as FieldCache.STRING_INDEX.
		
		/// <summary>Represents sorting by document score (relevancy). </summary>
		public static readonly SortField FIELD_SCORE = new SortField(null, SCORE);
		
		/// <summary>Represents sorting by document number (index order). </summary>
		public static readonly SortField FIELD_DOC = new SortField(null, DOC);
		
		private String field;
		private int type; // defaults to determining type dynamically
		internal bool reverse = false; // defaults to natural order
        private FieldCache.Parser parser;
		
		// Used for CUSTOM sort
		private FieldComparatorSource comparatorSource;

	    public Object missingValue = null;

		/// <summary>Creates a sort by terms in the given field with the type of term
		/// values explicitly given.
		/// </summary>
		/// <param name="field"> Name of field to sort by.  Can be <c>null</c> if
		/// <c>type</c> is SCORE or DOC.
		/// </param>
		/// <param name="type">  Type of values in the terms.
		/// </param>
		public SortField(String field, int type)
		{
			InitFieldType(field, type);
		}
		
		/// <summary>Creates a sort, possibly in reverse, by terms in the given field with the
		/// type of term values explicitly given.
		/// </summary>
		/// <param name="field"> Name of field to sort by.  Can be <c>null</c> if
		/// <c>type</c> is SCORE or DOC.
		/// </param>
		/// <param name="type">  Type of values in the terms.
		/// </param>
		/// <param name="reverse">True if natural order should be reversed.
		/// </param>
		public SortField(String field, int type, bool reverse)
		{
			InitFieldType(field, type);
			this.reverse = reverse;
		}
		
		/// <summary>Creates a sort by terms in the given field, parsed
        /// to numeric values using a custom <see cref="Search.Parser" />.
		/// </summary>
		/// <param name="field"> Name of field to sort by.  Must not be null.
		/// </param>
        /// <param name="parser">Instance of a <see cref="Search.Parser" />,
		/// which must subclass one of the existing numeric
		/// parsers from <see cref="FieldCache" />. Sort type is inferred
		/// by testing which numeric parser the parser subclasses.
		/// </param>
		/// <throws>  IllegalArgumentException if the parser fails to </throws>
		/// <summary>  subclass an existing numeric parser, or field is null
		/// </summary>
		public SortField(String field, FieldCache.Parser parser):this(field, parser, false)
		{
		}
		
		/// <summary>Creates a sort, possibly in reverse, by terms in the given field, parsed
        /// to numeric values using a custom <see cref="Search.Parser" />.
		/// </summary>
		/// <param name="field"> Name of field to sort by.  Must not be null.
		/// </param>
		/// <param name="parser">Instance of a <see cref="Search.Parser" />,
		/// which must subclass one of the existing numeric
		/// parsers from <see cref="FieldCache" />. Sort type is inferred
		/// by testing which numeric parser the parser subclasses.
		/// </param>
		/// <param name="reverse">True if natural order should be reversed.
		/// </param>
		/// <throws>  IllegalArgumentException if the parser fails to </throws>
		/// <summary>  subclass an existing numeric parser, or field is null
		/// </summary>
		public SortField(String field, FieldCache.Parser parser, bool reverse)
		{
            if (parser is FieldCache.IntParser)
				InitFieldType(field, INT);
            else if (parser is FieldCache.FloatParser)
				InitFieldType(field, FLOAT);
            else if (parser is FieldCache.ShortParser)
				InitFieldType(field, SHORT);
            else if (parser is FieldCache.ByteParser)
				InitFieldType(field, BYTE);
            else if (parser is FieldCache.LongParser)
				InitFieldType(field, LONG);
            else if (parser is FieldCache.DoubleParser)
				InitFieldType(field, DOUBLE);
			else
			{
				throw new ArgumentException("Parser instance does not subclass existing numeric parser from FieldCache (got " + parser + ")");
			}
			
			this.reverse = reverse;
			this.parser = parser;
		}
		
				
		/// <summary>Creates a sort with a custom comparison function.</summary>
		/// <param name="field">Name of field to sort by; cannot be <c>null</c>.
		/// </param>
		/// <param name="comparator">Returns a comparator for sorting hits.
		/// </param>
		public SortField(String field, FieldComparatorSource comparator)
		{
			InitFieldType(field, CUSTOM);
			this.comparatorSource = comparator;
		}
		
		/// <summary>Creates a sort, possibly in reverse, with a custom comparison function.</summary>
		/// <param name="field">Name of field to sort by; cannot be <c>null</c>.
		/// </param>
		/// <param name="comparator">Returns a comparator for sorting hits.
		/// </param>
		/// <param name="reverse">True if natural order should be reversed.
		/// </param>
		public SortField(String field, FieldComparatorSource comparator, bool reverse)
		{
			InitFieldType(field, CUSTOM);
			this.reverse = reverse;
			this.comparatorSource = comparator;
		}
		
		// Sets field & type, and ensures field is not NULL unless
		// type is SCORE or DOC
		private void  InitFieldType(String field, int type)
		{
			this.type = type;
			if (field == null)
			{
				if (type != SCORE && type != DOC)
					throw new ArgumentException("field can only be null when type is SCORE or DOC");
			}
			else
			{
				this.field = field;
			}
		}

	    /// <summary>Returns the name of the field.  Could return <c>null</c>
	    /// if the sort is by SCORE or DOC.
	    /// </summary>
	    /// <value> Name of field, possibly &lt;c&gt;null&lt;/c&gt;. </value>
	    public virtual string Field
	    {
	        get { return field; }
	    }

	    /// <summary>Returns the type of contents in the field.</summary>
	    /// <value> One of the constants SCORE, DOC, STRING, INT or FLOAT. </value>
	    public virtual int Type
	    {
	        get { return type; }
	    }

	    /// <summary>Returns the instance of a <see cref="FieldCache" /> parser that fits to the given sort type.
	    /// May return <c>null</c> if no parser was specified. Sorting is using the default parser then.
	    /// </summary>
	    /// <value> An instance of a &lt;see cref=&quot;FieldCache&quot; /&gt; parser, or &lt;c&gt;null&lt;/c&gt;. </value>
	    public virtual FieldCache.Parser Parser
	    {
	        get { return parser; }
	    }

	    /// <summary>Returns whether the sort should be reversed.</summary>
	    /// <value> True if natural order should be reversed. </value>
	    public virtual bool Reverse
	    {
	        get { return reverse; }
	    }

	    /// <summary>
	    /// Returns the <see cref="FieldComparatorSource"/> used for
	    /// custom sorting
	    /// </summary>
	    public virtual FieldComparatorSource ComparatorSource
	    {
	        get { return comparatorSource; }
	    }

	    public override String ToString()
		{
			var buffer = new StringBuilder();
			switch (type)
			{
				
				case SCORE: 
					buffer.Append("<score>");
					break;
				
				case DOC: 
					buffer.Append("<doc>");
					break;
				
				case STRING: 
					buffer.Append("<string: \"").Append(field).Append("\">");
					break;
				
				case STRING_VAL: 
					buffer.Append("<string_val: \"").Append(field).Append("\">");
					break;
				
				case BYTE: 
					buffer.Append("<byte: \"").Append(field).Append("\">");
					break;
				
				case SHORT: 
					buffer.Append("<short: \"").Append(field).Append("\">");
					break;
				
				case INT: 
					buffer.Append("<int: \"").Append(field).Append("\">");
					break;
				
				case LONG: 
					buffer.Append("<long: \"").Append(field).Append("\">");
					break;
				
				case FLOAT: 
					buffer.Append("<float: \"").Append(field).Append("\">");
					break;
				
				case DOUBLE: 
					buffer.Append("<double: \"").Append(field).Append("\">");
					break;
				
				case CUSTOM: 
					buffer.Append("<custom:\"").Append(field).Append("\": ").Append(comparatorSource).Append('>');
					break;
				
                case REWRITEABLE:
			        buffer.Append("<???: \"").Append(field).Append("\">");
			        break;

				default: 
					buffer.Append("<???: \"").Append(field).Append("\">");
					break;
				
			}

			if (reverse)
				buffer.Append('!');
			
			return buffer.ToString();
		}
		
		/// <summary>Returns true if <c>o</c> is equal to this.  If a
		/// <see cref="FieldComparatorSource" />  or <see cref="Search.Parser" />
		/// was provided, it must properly
		/// implement equals (unless a singleton is always used). 
		/// </summary>
		public  override bool Equals(Object o)
		{
			if (this == o)
				return true;
			if (!(o is SortField))
				return false;
			var other = (SortField) o;
            return (
                  StringHelper.Equals(other.field, this.field)
                  && other.type == this.type
                  && other.reverse == this.reverse
                  && (other.comparatorSource == null ? this.comparatorSource == null : other.comparatorSource.Equals(this.comparatorSource))
                );
		}
		
		/// <summary>Returns true if <c>o</c> is equal to this.  If a
		/// <see cref="FieldComparatorSource" /> (deprecated) or <see cref="Search.Parser" />
		/// was provided, it must properly
		/// implement hashCode (unless a singleton is always
		/// used). 
		/// </summary>
		public override int GetHashCode()
		{
            int hash = (int) unchecked(type.GetHashCode() ^ 0x346565dd + reverse.GetHashCode() ^ 0xaf5998bb);
            if (field != null) hash += (int) unchecked(field.GetHashCode() ^ 0xff5685dd);
            if (comparatorSource != null) hash += comparatorSource.GetHashCode();
            return hash;
		}


	    private Comparer<BytesRef> bytesComparator = BytesRef.UTF8SortedAsUnicodeComparer;
	    public Comparer<BytesRef> BytesComparator
	    {
            get { return bytesComparator; }
            set { bytesComparator = value; }
	    } 

		/// <summary>Returns the <see cref="FieldComparator" /> to use for
		/// sorting.
		/// 
		/// <b>NOTE:</b> This API is experimental and might change in
		/// incompatible ways in the next release.
		/// 
		/// </summary>
		/// <param name="numHits">number of top hits the queue will store
		/// </param>
		/// <param name="sortPos">position of this SortField within <see cref="Sort" />
		///.  The comparator is primary if sortPos==0,
		/// secondary if sortPos==1, etc.  Some comparators can
		/// optimize themselves when they are the primary sort.
		/// </param>
		/// <returns> <see cref="FieldComparator" /> to use when sorting
		/// </returns>
		public virtual FieldComparator GetComparator(int numHits, int sortPos)
		{
			switch (type)
			{
				case SCORE: 
					return new FieldComparator.RelevanceComparator(numHits);
				
				case DOC: 
					return new FieldComparator.DocComparator(numHits);
				
				case INT: 
					return new FieldComparator.IntComparator(numHits, field, parser, (int) missingValue);
				
				case FLOAT: 
					return new FieldComparator.FloatComparator(numHits, field, parser, (float) missingValue);
				
				case LONG: 
					return new FieldComparator.LongComparator(numHits, field, parser, (long) missingValue);
				
				case DOUBLE: 
					return new FieldComparator.DoubleComparator(numHits, field, parser);
				
				case BYTE: 
					return new FieldComparator.ByteComparator(numHits, field, parser);
				
				case SHORT: 
					return new FieldComparator.ShortComparator(numHits, field, parser);
				
				case CUSTOM: 
					System.Diagnostics.Debug.Assert(comparatorSource != null);
					return comparatorSource.NewComparator(field, numHits, sortPos, reverse);
				
				case STRING: 
					return new FieldComparator.StringOrdValComparator(numHits, field, sortPos, reverse);
				
				case STRING_VAL: 
					return new FieldComparator.StringValComparator(numHits, field);
				
                case REWRITEABLE:
                    throw new InvalidOperationException("SortField needs to be rewritten through Sort.Rewrite(..) and SortField.Rewrite(..)");

				default:
                    throw new InvalidOperationException("Illegal sort type: " + type);
				
			}
		}

        public SortField Rewrite(IndexSearcher searcher)
        {
            return this;
        }
	}
}