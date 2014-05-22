using System;
using System.Diagnostics;
using System.Collections.Generic;
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

	  /// <summary>
	  /// Specifies the type of the terms to be sorted, or special types such as CUSTOM
	  /// </summary>
	  public enum Type
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
		INT,

		/// <summary>
		/// Sort using term values as encoded Floats.  Sort values are Float and
		/// lower values are at the front. 
		/// </summary>
		FLOAT,

		/// <summary>
		/// Sort using term values as encoded Longs.  Sort values are Long and
		/// lower values are at the front. 
		/// </summary>
		LONG,

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
		SHORT,

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

	  /// <summary>
	  /// Represents sorting by document score (relevance). </summary>
	  public static readonly SortField FIELD_SCORE = new SortField(null, Type.SCORE);

	  /// <summary>
	  /// Represents sorting by document number (index order). </summary>
	  public static readonly SortField FIELD_DOC = new SortField(null, Type.DOC);

	  private string Field_Renamed;
	  private Type Type_Renamed; // defaults to determining type dynamically
	  internal bool Reverse_Renamed = false; // defaults to natural order
	  private FieldCache_Parser Parser_Renamed;

	  // Used for CUSTOM sort
	  private FieldComparatorSource ComparatorSource_Renamed;

	  // Used for 'sortMissingFirst/Last'
	  public object MissingValue_Renamed = null;

	  /// <summary>
	  /// Creates a sort by terms in the given field with the type of term
	  /// values explicitly given. </summary>
	  /// <param name="field">  Name of field to sort by.  Can be <code>null</code> if
	  ///               <code>type</code> is SCORE or DOC. </param>
	  /// <param name="type">   Type of values in the terms. </param>
	  public SortField(string field, Type type)
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
	  public SortField(string field, Type type, bool reverse)
	  {
		InitFieldType(field, type);
		this.Reverse_Renamed = reverse;
	  }

	  /// <summary>
	  /// Creates a sort by terms in the given field, parsed
	  /// to numeric values using a custom <seealso cref="FieldCache.Parser"/>. </summary>
	  /// <param name="field">  Name of field to sort by.  Must not be null. </param>
	  /// <param name="parser"> Instance of a <seealso cref="FieldCache.Parser"/>,
	  ///  which must subclass one of the existing numeric
	  ///  parsers from <seealso cref="FieldCache"/>. Sort type is inferred
	  ///  by testing which numeric parser the parser subclasses. </param>
	  /// <exception cref="IllegalArgumentException"> if the parser fails to
	  ///  subclass an existing numeric parser, or field is null </exception>
	  public SortField(string field, FieldCache_Parser parser) : this(field, parser, false)
	  {
	  }

	  /// <summary>
	  /// Creates a sort, possibly in reverse, by terms in the given field, parsed
	  /// to numeric values using a custom <seealso cref="FieldCache.Parser"/>. </summary>
	  /// <param name="field">  Name of field to sort by.  Must not be null. </param>
	  /// <param name="parser"> Instance of a <seealso cref="FieldCache.Parser"/>,
	  ///  which must subclass one of the existing numeric
	  ///  parsers from <seealso cref="FieldCache"/>. Sort type is inferred
	  ///  by testing which numeric parser the parser subclasses. </param>
	  /// <param name="reverse"> True if natural order should be reversed. </param>
	  /// <exception cref="IllegalArgumentException"> if the parser fails to
	  ///  subclass an existing numeric parser, or field is null </exception>
	  public SortField(string field, FieldCache_Parser parser, bool reverse)
	  {
		if (parser is FieldCache_IntParser)
		{
			InitFieldType(field, Type.INT);
		}
		else if (parser is FieldCache_FloatParser)
		{
			InitFieldType(field, Type.FLOAT);
		}
		else if (parser is FieldCache_ShortParser)
		{
			InitFieldType(field, Type.SHORT);
		}
		else if (parser is FieldCache_ByteParser)
		{
			InitFieldType(field, Type.BYTE);
		}
		else if (parser is FieldCache_LongParser)
		{
			InitFieldType(field, Type.LONG);
		}
		else if (parser is FieldCache_DoubleParser)
		{
			InitFieldType(field, Type.DOUBLE);
		}
		else
		{
		  throw new System.ArgumentException("Parser instance does not subclass existing numeric parser from FieldCache (got " + parser + ")");
		}

		this.Reverse_Renamed = reverse;
		this.Parser_Renamed = parser;
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

	  public virtual object MissingValue
	  {
		  set
		  {
			if (Type_Renamed == Type.STRING)
			{
			  if (value != STRING_FIRST && value != STRING_LAST)
			  {
				throw new System.ArgumentException("For STRING type, missing value must be either STRING_FIRST or STRING_LAST");
			  }
			}
			else if (Type_Renamed != Type.BYTE && Type_Renamed != Type.SHORT && Type_Renamed != Type.INT && Type_Renamed != Type.FLOAT && Type_Renamed != Type.LONG && Type_Renamed != Type.DOUBLE)
			{
			  throw new System.ArgumentException("Missing value only works for numeric or STRING types");
			}
			this.MissingValue_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// Creates a sort with a custom comparison function. </summary>
	  /// <param name="field"> Name of field to sort by; cannot be <code>null</code>. </param>
	  /// <param name="comparator"> Returns a comparator for sorting hits. </param>
	  public SortField(string field, FieldComparatorSource comparator)
	  {
		InitFieldType(field, Type.CUSTOM);
		this.ComparatorSource_Renamed = comparator;
	  }

	  /// <summary>
	  /// Creates a sort, possibly in reverse, with a custom comparison function. </summary>
	  /// <param name="field"> Name of field to sort by; cannot be <code>null</code>. </param>
	  /// <param name="comparator"> Returns a comparator for sorting hits. </param>
	  /// <param name="reverse"> True if natural order should be reversed. </param>
	  public SortField(string field, FieldComparatorSource comparator, bool reverse)
	  {
		InitFieldType(field, Type.CUSTOM);
		this.Reverse_Renamed = reverse;
		this.ComparatorSource_Renamed = comparator;
	  }

	  // Sets field & type, and ensures field is not NULL unless
	  // type is SCORE or DOC
	  private void InitFieldType(string field, Type type)
	  {
		this.Type_Renamed = type;
		if (field == null)
		{
		  if (type != Type.SCORE && type != Type.DOC)
		  {
			throw new System.ArgumentException("field can only be null when type is SCORE or DOC");
		  }
		}
		else
		{
		  this.Field_Renamed = field;
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
			return Field_Renamed;
		  }
	  }

	  /// <summary>
	  /// Returns the type of contents in the field. </summary>
	  /// <returns> One of the constants SCORE, DOC, STRING, INT or FLOAT. </returns>
	  public virtual Type Type
	  {
		  get
		  {
			return Type_Renamed;
		  }
	  }

	  /// <summary>
	  /// Returns the instance of a <seealso cref="FieldCache"/> parser that fits to the given sort type.
	  /// May return <code>null</code> if no parser was specified. Sorting is using the default parser then. </summary>
	  /// <returns> An instance of a <seealso cref="FieldCache"/> parser, or <code>null</code>. </returns>
	  public virtual FieldCache_Parser Parser
	  {
		  get
		  {
			return Parser_Renamed;
		  }
	  }

	  /// <summary>
	  /// Returns whether the sort should be reversed. </summary>
	  /// <returns>  True if natural order should be reversed. </returns>
	  public virtual bool Reverse
	  {
		  get
		  {
			return Reverse_Renamed;
		  }
	  }

	  /// <summary>
	  /// Returns the <seealso cref="FieldComparatorSource"/> used for
	  /// custom sorting
	  /// </summary>
	  public virtual FieldComparatorSource ComparatorSource
	  {
		  get
		  {
			return ComparatorSource_Renamed;
		  }
	  }

	  public override string ToString()
	  {
		StringBuilder buffer = new StringBuilder();
		switch (Type_Renamed)
		{
		  case Lucene.Net.Search.SortField.Type.SCORE:
			buffer.Append("<score>");
			break;

		  case Lucene.Net.Search.SortField.Type.DOC:
			buffer.Append("<doc>");
			break;

		  case Lucene.Net.Search.SortField.Type.STRING:
			buffer.Append("<string" + ": \"").Append(Field_Renamed).Append("\">");
			break;

		  case Lucene.Net.Search.SortField.Type.STRING_VAL:
			buffer.Append("<string_val" + ": \"").Append(Field_Renamed).Append("\">");
			break;

		  case Lucene.Net.Search.SortField.Type.BYTE:
			buffer.Append("<byte: \"").Append(Field_Renamed).Append("\">");
			break;

		  case Lucene.Net.Search.SortField.Type.SHORT:
			buffer.Append("<short: \"").Append(Field_Renamed).Append("\">");
			break;

		  case Lucene.Net.Search.SortField.Type.INT:
			buffer.Append("<int" + ": \"").Append(Field_Renamed).Append("\">");
			break;

		  case Lucene.Net.Search.SortField.Type.LONG:
			buffer.Append("<long: \"").Append(Field_Renamed).Append("\">");
			break;

		  case Lucene.Net.Search.SortField.Type.FLOAT:
			buffer.Append("<float" + ": \"").Append(Field_Renamed).Append("\">");
			break;

		  case Lucene.Net.Search.SortField.Type.DOUBLE:
			buffer.Append("<double" + ": \"").Append(Field_Renamed).Append("\">");
			break;

		  case Lucene.Net.Search.SortField.Type.CUSTOM:
			buffer.Append("<custom:\"").Append(Field_Renamed).Append("\": ").Append(ComparatorSource_Renamed).Append('>');
			break;

		  case Lucene.Net.Search.SortField.Type.REWRITEABLE:
			buffer.Append("<rewriteable: \"").Append(Field_Renamed).Append("\">");
			break;

		  default:
			buffer.Append("<???: \"").Append(Field_Renamed).Append("\">");
			break;
		}

		if (Reverse_Renamed)
		{
			buffer.Append('!');
		}
		if (MissingValue_Renamed != null)
		{
		  buffer.Append(" missingValue=");
		  buffer.Append(MissingValue_Renamed);
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
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SortField other = (SortField)o;
		SortField other = (SortField)o;
		return (StringHelper.Equals(other.Field_Renamed, this.Field_Renamed) && other.Type_Renamed == this.Type_Renamed && other.Reverse_Renamed == this.Reverse_Renamed && (other.ComparatorSource_Renamed == null ? this.ComparatorSource_Renamed == null : other.ComparatorSource_Renamed.Equals(this.ComparatorSource_Renamed)));
	  }

	  /// <summary>
	  /// Returns true if <code>o</code> is equal to this.  If a
	  ///  <seealso cref="FieldComparatorSource"/> or {@link
	  ///  FieldCache.Parser} was provided, it must properly
	  ///  implement hashCode (unless a singleton is always
	  ///  used). 
	  /// </summary>
	  public override int HashCode()
	  {
		int hash = Type_Renamed.HashCode() ^ 0x346565dd + Convert.ToBoolean(Reverse_Renamed).HashCode() ^ 0xaf5998bb;
		if (Field_Renamed != null)
		{
			hash += Field_Renamed.HashCode() ^ 0xff5685dd;
		}
		if (ComparatorSource_Renamed != null)
		{
			hash += ComparatorSource_Renamed.HashCode();
		}
		return hash;
	  }

	  private IComparer<BytesRef> BytesComparator_Renamed = BytesRef.UTF8SortedAsUnicodeComparator;

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
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: public FieldComparator<?> getComparator(final int numHits, final int sortPos) throws java.io.IOException
	  public virtual FieldComparator<?> GetComparator(int numHits, int sortPos)
	  {

		switch (Type_Renamed)
		{
		case Lucene.Net.Search.SortField.Type.SCORE:
		  return new FieldComparator.RelevanceComparator(numHits);

		case Lucene.Net.Search.SortField.Type.DOC:
		  return new FieldComparator.DocComparator(numHits);

		case Lucene.Net.Search.SortField.Type.INT:
		  return new FieldComparator.IntComparator(numHits, Field_Renamed, Parser_Renamed, (int?) MissingValue_Renamed);

		case Lucene.Net.Search.SortField.Type.FLOAT:
		  return new FieldComparator.FloatComparator(numHits, Field_Renamed, Parser_Renamed, (float?) MissingValue_Renamed);

		case Lucene.Net.Search.SortField.Type.LONG:
		  return new FieldComparator.LongComparator(numHits, Field_Renamed, Parser_Renamed, (long?) MissingValue_Renamed);

		case Lucene.Net.Search.SortField.Type.DOUBLE:
		  return new FieldComparator.DoubleComparator(numHits, Field_Renamed, Parser_Renamed, (double?) MissingValue_Renamed);

		case Lucene.Net.Search.SortField.Type.BYTE:
		  return new FieldComparator.ByteComparator(numHits, Field_Renamed, Parser_Renamed, (sbyte?) MissingValue_Renamed);

		case Lucene.Net.Search.SortField.Type.SHORT:
		  return new FieldComparator.ShortComparator(numHits, Field_Renamed, Parser_Renamed, (short?) MissingValue_Renamed);

		case Lucene.Net.Search.SortField.Type.CUSTOM:
		  Debug.Assert(ComparatorSource_Renamed != null);
		  return ComparatorSource_Renamed.NewComparator(Field_Renamed, numHits, sortPos, Reverse_Renamed);

		case Lucene.Net.Search.SortField.Type.STRING:
		  return new FieldComparator.TermOrdValComparator(numHits, Field_Renamed, MissingValue_Renamed == STRING_LAST);

		case Lucene.Net.Search.SortField.Type.STRING_VAL:
		  // TODO: should we remove this?  who really uses it?
		  return new FieldComparator.TermValComparator(numHits, Field_Renamed);

		case Lucene.Net.Search.SortField.Type.REWRITEABLE:
		  throw new IllegalStateException("SortField needs to be rewritten through Sort.rewrite(..) and SortField.rewrite(..)");

		default:
		  throw new IllegalStateException("Illegal sort type: " + Type_Renamed);
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
	  public virtual bool NeedsScores()
	  {
		return Type_Renamed == Type.SCORE;
	  }
	}

}