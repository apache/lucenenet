using System;
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

	using DoubleField = Lucene.Net.Document.DoubleField; // for javadocs
	using FloatField = Lucene.Net.Document.FloatField; // for javadocs
	using IntField = Lucene.Net.Document.IntField; // for javadocs
	using LongField = Lucene.Net.Document.LongField; // for javadocs
	using AtomicReader = Lucene.Net.Index.AtomicReader; // for javadocs
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using SortedDocValues = Lucene.Net.Index.SortedDocValues;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using NumericUtils = Lucene.Net.Util.NumericUtils;

	/// <summary>
	/// A range filter built on top of a cached single term field (in <seealso cref="FieldCache"/>).
	/// 
	/// <p>{@code FieldCacheRangeFilter} builds a single cache for the field the first time it is used.
	/// Each subsequent {@code FieldCacheRangeFilter} on the same field then reuses this cache,
	/// even if the range itself changes. 
	/// 
	/// <p>this means that {@code FieldCacheRangeFilter} is much faster (sometimes more than 100x as fast) 
	/// as building a <seealso cref="TermRangeFilter"/>, if using a <seealso cref="#newStringRange"/>.
	/// However, if the range never changes it is slower (around 2x as slow) than building
	/// a CachingWrapperFilter on top of a single <seealso cref="TermRangeFilter"/>.
	/// 
	/// For numeric data types, this filter may be significantly faster than <seealso cref="NumericRangeFilter"/>.
	/// Furthermore, it does not need the numeric values encoded
	/// by <seealso cref="IntField"/>, <seealso cref="FloatField"/>, {@link
	/// LongField} or <seealso cref="DoubleField"/>. But
	/// it has the problem that it only works with exact one value/document (see below).
	/// 
	/// <p>As with all <seealso cref="FieldCache"/> based functionality, {@code FieldCacheRangeFilter} is only valid for 
	/// fields which exact one term for each document (except for <seealso cref="#newStringRange"/>
	/// where 0 terms are also allowed). Due to a restriction of <seealso cref="FieldCache"/>, for numeric ranges
	/// all terms that do not have a numeric value, 0 is assumed.
	/// 
	/// <p>Thus it works on dates, prices and other single value fields but will not work on
	/// regular text fields. It is preferable to use a <code>NOT_ANALYZED</code> field to ensure that
	/// there is only a single term. 
	/// 
	/// <p>this class does not have an constructor, use one of the static factory methods available,
	/// that create a correct instance for different data types supported by <seealso cref="FieldCache"/>.
	/// </summary>

	public abstract class FieldCacheRangeFilter<T> : Filter
	{
	  internal readonly string Field_Renamed;
	  internal readonly FieldCache_Parser Parser_Renamed;
	  internal readonly T LowerVal_Renamed;
	  internal readonly T UpperVal_Renamed;
	  internal readonly bool IncludeLower;
	  internal readonly bool IncludeUpper;

	  private FieldCacheRangeFilter(string field, FieldCache_Parser parser, T lowerVal, T upperVal, bool includeLower, bool includeUpper)
	  {
		this.Field_Renamed = field;
		this.Parser_Renamed = parser;
		this.LowerVal_Renamed = lowerVal;
		this.UpperVal_Renamed = upperVal;
		this.IncludeLower = includeLower;
		this.IncludeUpper = includeUpper;
	  }

	  /// <summary>
	  /// this method is implemented for each data type </summary>
	  public override abstract DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs);

	  /// <summary>
	  /// Creates a string range filter using <seealso cref="FieldCache#getTermsIndex"/>. this works with all
	  /// fields containing zero or one term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<string> NewStringRange(string field, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper(field, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper : FieldCacheRangeFilter<string>
	  {
		  private string Field;
		  private string LowerVal;
		  private string UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

		  public FieldCacheRangeFilterAnonymousInnerClassHelper(string field, string lowerVal, string upperVal, bool includeLower, bool includeUpper) : base(field, null, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			SortedDocValues fcsi = FieldCache_Fields.DEFAULT.GetTermsIndex(context.Reader(), Field);
			int lowerPoint = LowerVal == null ? - 1 : fcsi.LookupTerm(new BytesRef(LowerVal));
			int upperPoint = UpperVal == null ? - 1 : fcsi.LookupTerm(new BytesRef(UpperVal));

			int inclusiveLowerPoint, inclusiveUpperPoint;

			// Hints:
			// * binarySearchLookup returns -1, if value was null.
			// * the value is <0 if no exact hit was found, the returned value
			//   is (-(insertion point) - 1)
			if (lowerPoint == -1 && LowerVal == null)
			{
			  inclusiveLowerPoint = 0;
			}
			else if (IncludeLower && lowerPoint >= 0)
			{
			  inclusiveLowerPoint = lowerPoint;
			}
			else if (lowerPoint >= 0)
			{
			  inclusiveLowerPoint = lowerPoint + 1;
			}
			else
			{
			  inclusiveLowerPoint = Math.Max(0, -lowerPoint - 1);
			}

			if (upperPoint == -1 && UpperVal == null)
			{
			  inclusiveUpperPoint = int.MaxValue;
			}
			else if (IncludeUpper && upperPoint >= 0)
			{
			  inclusiveUpperPoint = upperPoint;
			}
			else if (upperPoint >= 0)
			{
			  inclusiveUpperPoint = upperPoint - 1;
			}
			else
			{
			  inclusiveUpperPoint = -upperPoint - 2;
			}

			if (inclusiveUpperPoint < 0 || inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

			Debug.Assert(inclusiveLowerPoint >= 0 && inclusiveUpperPoint >= 0);

			return new FieldCacheDocIdSetAnonymousInnerClassHelper(this, context.Reader().MaxDoc(), acceptDocs, fcsi, inclusiveLowerPoint, inclusiveUpperPoint);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper OuterInstance;

			  private SortedDocValues Fcsi;
			  private int InclusiveLowerPoint;
			  private int InclusiveUpperPoint;

			  public FieldCacheDocIdSetAnonymousInnerClassHelper(FieldCacheRangeFilterAnonymousInnerClassHelper outerInstance, int maxDoc, Bits acceptDocs, SortedDocValues fcsi, int inclusiveLowerPoint, int inclusiveUpperPoint) : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.Fcsi = fcsi;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
			  }

			  protected internal override sealed bool MatchDoc(int doc)
			  {
				int docOrd = Fcsi.GetOrd(doc);
				return docOrd >= InclusiveLowerPoint && docOrd <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a BytesRef range filter using <seealso cref="FieldCache#getTermsIndex"/>. this works with all
	  /// fields containing zero or one term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  // TODO: bogus that newStringRange doesnt share this code... generics hell
	  public static FieldCacheRangeFilter<BytesRef> NewBytesRefRange(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper2(field, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper2 : FieldCacheRangeFilter<BytesRef>
	  {
		  private string Field;
		  private BytesRef LowerVal;
		  private BytesRef UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

		  public FieldCacheRangeFilterAnonymousInnerClassHelper2(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper) : base(field, null, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			SortedDocValues fcsi = FieldCache_Fields.DEFAULT.GetTermsIndex(context.Reader(), Field);
			int lowerPoint = LowerVal == null ? - 1 : fcsi.LookupTerm(LowerVal);
			int upperPoint = UpperVal == null ? - 1 : fcsi.LookupTerm(UpperVal);

			int inclusiveLowerPoint, inclusiveUpperPoint;

			// Hints:
			// * binarySearchLookup returns -1, if value was null.
			// * the value is <0 if no exact hit was found, the returned value
			//   is (-(insertion point) - 1)
			if (lowerPoint == -1 && LowerVal == null)
			{
			  inclusiveLowerPoint = 0;
			}
			else if (IncludeLower && lowerPoint >= 0)
			{
			  inclusiveLowerPoint = lowerPoint;
			}
			else if (lowerPoint >= 0)
			{
			  inclusiveLowerPoint = lowerPoint + 1;
			}
			else
			{
			  inclusiveLowerPoint = Math.Max(0, -lowerPoint - 1);
			}

			if (upperPoint == -1 && UpperVal == null)
			{
			  inclusiveUpperPoint = int.MaxValue;
			}
			else if (IncludeUpper && upperPoint >= 0)
			{
			  inclusiveUpperPoint = upperPoint;
			}
			else if (upperPoint >= 0)
			{
			  inclusiveUpperPoint = upperPoint - 1;
			}
			else
			{
			  inclusiveUpperPoint = -upperPoint - 2;
			}

			if (inclusiveUpperPoint < 0 || inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

			Debug.Assert(inclusiveLowerPoint >= 0 && inclusiveUpperPoint >= 0);

			return new FieldCacheDocIdSetAnonymousInnerClassHelper2(this, context.Reader().MaxDoc(), acceptDocs, fcsi, inclusiveLowerPoint, inclusiveUpperPoint);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper2 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper2 OuterInstance;

			  private SortedDocValues Fcsi;
			  private int InclusiveLowerPoint;
			  private int InclusiveUpperPoint;

			  public FieldCacheDocIdSetAnonymousInnerClassHelper2(FieldCacheRangeFilterAnonymousInnerClassHelper2 outerInstance, int maxDoc, Bits acceptDocs, SortedDocValues fcsi, int inclusiveLowerPoint, int inclusiveUpperPoint) : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.Fcsi = fcsi;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
			  }

			  protected internal override sealed bool MatchDoc(int doc)
			  {
				int docOrd = Fcsi.GetOrd(doc);
				return docOrd >= InclusiveLowerPoint && docOrd <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getBytes(AtomicReader,String,boolean)"/>. this works with all
	  /// byte fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  [Obsolete]
	  public static FieldCacheRangeFilter<sbyte?> NewByteRange(string field, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewByteRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getBytes(AtomicReader,String,FieldCache.ByteParser,boolean)"/>. this works with all
	  /// byte fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  [Obsolete]
	  public static FieldCacheRangeFilter<sbyte?> NewByteRange(string field, FieldCache_ByteParser parser, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper3(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper3 : FieldCacheRangeFilter<sbyte?>
	  {
		  private string Field;
		  private Lucene.Net.Search.FieldCache_ByteParser Parser;
		  private sbyte? LowerVal;
		  private sbyte? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

		  public FieldCacheRangeFilterAnonymousInnerClassHelper3(string field, Lucene.Net.Search.FieldCache_ByteParser parser, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper) : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			sbyte inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  sbyte i = (sbyte)LowerVal;
			  if (!IncludeLower && i == sbyte.MaxValue)
			  {
				return null;
			  }
			  inclusiveLowerPoint = (sbyte)(IncludeLower ? i : (i + 1));
			}
			else
			{
			  inclusiveLowerPoint = sbyte.MinValue;
			}
			if (UpperVal != null)
			{
			  sbyte i = (sbyte)UpperVal;
			  if (!IncludeUpper && i == sbyte.MinValue)
			  {
				return null;
			  }
			  inclusiveUpperPoint = (sbyte)(IncludeUpper ? i : (i - 1));
			}
			else
			{
			  inclusiveUpperPoint = sbyte.MaxValue;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

			FieldCache_Bytes values = FieldCache_Fields.DEFAULT.GetBytes(context.Reader(), Field, (FieldCache_ByteParser) Parser, false);
			return new FieldCacheDocIdSetAnonymousInnerClassHelper3(this, context.Reader().MaxDoc(), acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper3 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper3 OuterInstance;

			  private sbyte InclusiveLowerPoint;
			  private sbyte InclusiveUpperPoint;
			  private Lucene.Net.Search.FieldCache_Bytes Values;

			  public FieldCacheDocIdSetAnonymousInnerClassHelper3(FieldCacheRangeFilterAnonymousInnerClassHelper3 outerInstance, int maxDoc, Bits acceptDocs, sbyte inclusiveLowerPoint, sbyte inclusiveUpperPoint, Lucene.Net.Search.FieldCache_Bytes values) : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				sbyte value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getShorts(AtomicReader,String,boolean)"/>. this works with all
	  /// short fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  [Obsolete]
	  public static FieldCacheRangeFilter<short?> NewShortRange(string field, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewShortRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getShorts(AtomicReader,String,FieldCache.ShortParser,boolean)"/>. this works with all
	  /// short fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  [Obsolete]
	  public static FieldCacheRangeFilter<short?> NewShortRange(string field, FieldCache_ShortParser parser, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper4(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper4 : FieldCacheRangeFilter<short?>
	  {
		  private string Field;
		  private Lucene.Net.Search.FieldCache_ShortParser Parser;
		  private short? LowerVal;
		  private short? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

		  public FieldCacheRangeFilterAnonymousInnerClassHelper4(string field, Lucene.Net.Search.FieldCache_ShortParser parser, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper) : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			short inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  short i = (short)LowerVal;
			  if (!IncludeLower && i == short.MaxValue)
			  {
				return null;
			  }
			  inclusiveLowerPoint = (short)(IncludeLower ? i : (i + 1));
			}
			else
			{
			  inclusiveLowerPoint = short.MinValue;
			}
			if (UpperVal != null)
			{
			  short i = (short)UpperVal;
			  if (!IncludeUpper && i == short.MinValue)
			  {
				return null;
			  }
			  inclusiveUpperPoint = (short)(IncludeUpper ? i : (i - 1));
			}
			else
			{
			  inclusiveUpperPoint = short.MaxValue;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

			FieldCache_Shorts values = FieldCache_Fields.DEFAULT.GetShorts(context.Reader(), Field, (FieldCache_ShortParser) Parser, false);
			return new FieldCacheDocIdSetAnonymousInnerClassHelper4(this, context.Reader().MaxDoc(), acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper4 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper4 OuterInstance;

			  private short InclusiveLowerPoint;
			  private short InclusiveUpperPoint;
			  private Lucene.Net.Search.FieldCache_Shorts Values;

			  public FieldCacheDocIdSetAnonymousInnerClassHelper4(FieldCacheRangeFilterAnonymousInnerClassHelper4 outerInstance, int maxDoc, Bits acceptDocs, short inclusiveLowerPoint, short inclusiveUpperPoint, Lucene.Net.Search.FieldCache_Shorts values) : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				short value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getInts(AtomicReader,String,boolean)"/>. this works with all
	  /// int fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<int?> NewIntRange(string field, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewIntRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getInts(AtomicReader,String,FieldCache.IntParser,boolean)"/>. this works with all
	  /// int fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<int?> NewIntRange(string field, FieldCache_IntParser parser, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper5(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper5 : FieldCacheRangeFilter<int?>
	  {
		  private string Field;
		  private Lucene.Net.Search.FieldCache_IntParser Parser;
		  private int? LowerVal;
		  private int? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

		  public FieldCacheRangeFilterAnonymousInnerClassHelper5(string field, Lucene.Net.Search.FieldCache_IntParser parser, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper) : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			int inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  int i = (int)LowerVal;
			  if (!IncludeLower && i == int.MaxValue)
			  {
				return null;
			  }
			  inclusiveLowerPoint = IncludeLower ? i : (i + 1);
			}
			else
			{
			  inclusiveLowerPoint = int.MinValue;
			}
			if (UpperVal != null)
			{
			  int i = (int)UpperVal;
			  if (!IncludeUpper && i == int.MinValue)
			  {
				return null;
			  }
			  inclusiveUpperPoint = IncludeUpper ? i : (i - 1);
			}
			else
			{
			  inclusiveUpperPoint = int.MaxValue;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

			FieldCache_Ints values = FieldCache_Fields.DEFAULT.GetInts(context.Reader(), Field, (FieldCache_IntParser) Parser, false);
			return new FieldCacheDocIdSetAnonymousInnerClassHelper5(this, context.Reader().MaxDoc(), acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper5 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper5 OuterInstance;

			  private int InclusiveLowerPoint;
			  private int InclusiveUpperPoint;
			  private Lucene.Net.Search.FieldCache_Ints Values;

			  public FieldCacheDocIdSetAnonymousInnerClassHelper5(FieldCacheRangeFilterAnonymousInnerClassHelper5 outerInstance, int maxDoc, Bits acceptDocs, int inclusiveLowerPoint, int inclusiveUpperPoint, Lucene.Net.Search.FieldCache_Ints values) : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				int value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getLongs(AtomicReader,String,boolean)"/>. this works with all
	  /// long fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<long?> NewLongRange(string field, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewLongRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getLongs(AtomicReader,String,FieldCache.LongParser,boolean)"/>. this works with all
	  /// long fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<long?> NewLongRange(string field, FieldCache_LongParser parser, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper6(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper6 : FieldCacheRangeFilter<long?>
	  {
		  private string Field;
		  private Lucene.Net.Search.FieldCache_LongParser Parser;
		  private long? LowerVal;
		  private long? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

		  public FieldCacheRangeFilterAnonymousInnerClassHelper6(string field, Lucene.Net.Search.FieldCache_LongParser parser, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper) : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			long inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  long i = (long)LowerVal;
			  if (!IncludeLower && i == long.MaxValue)
			  {
				return null;
			  }
			  inclusiveLowerPoint = IncludeLower ? i : (i + 1L);
			}
			else
			{
			  inclusiveLowerPoint = long.MinValue;
			}
			if (UpperVal != null)
			{
			  long i = (long)UpperVal;
			  if (!IncludeUpper && i == long.MinValue)
			  {
				return null;
			  }
			  inclusiveUpperPoint = IncludeUpper ? i : (i - 1L);
			}
			else
			{
			  inclusiveUpperPoint = long.MaxValue;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

			FieldCache_Longs values = FieldCache_Fields.DEFAULT.GetLongs(context.Reader(), Field, (FieldCache_LongParser) Parser, false);
			return new FieldCacheDocIdSetAnonymousInnerClassHelper6(this, context.Reader().MaxDoc(), acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper6 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper6 OuterInstance;

			  private long InclusiveLowerPoint;
			  private long InclusiveUpperPoint;
			  private Lucene.Net.Search.FieldCache_Longs Values;

			  public FieldCacheDocIdSetAnonymousInnerClassHelper6(FieldCacheRangeFilterAnonymousInnerClassHelper6 outerInstance, int maxDoc, Bits acceptDocs, long inclusiveLowerPoint, long inclusiveUpperPoint, Lucene.Net.Search.FieldCache_Longs values) : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				long value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getFloats(AtomicReader,String,boolean)"/>. this works with all
	  /// float fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<float?> NewFloatRange(string field, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewFloatRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getFloats(AtomicReader,String,FieldCache.FloatParser,boolean)"/>. this works with all
	  /// float fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<float?> NewFloatRange(string field, FieldCache_FloatParser parser, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper7(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper7 : FieldCacheRangeFilter<float?>
	  {
		  private string Field;
		  private Lucene.Net.Search.FieldCache_FloatParser Parser;
		  private float? LowerVal;
		  private float? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

		  public FieldCacheRangeFilterAnonymousInnerClassHelper7(string field, Lucene.Net.Search.FieldCache_FloatParser parser, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper) : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			// we transform the floating point numbers to sortable integers
			// using NumericUtils to easier find the next bigger/lower value
			float inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  float f = (float)LowerVal;
			  if (!IncludeUpper && f > 0.0f && float.IsInfinity(f))
			  {
				return null;
			  }
			  int i = NumericUtils.FloatToSortableInt(f);
			  inclusiveLowerPoint = NumericUtils.SortableIntToFloat(IncludeLower ? i : (i + 1));
			}
			else
			{
			  inclusiveLowerPoint = float.NegativeInfinity;
			}
			if (UpperVal != null)
			{
			  float f = (float)UpperVal;
			  if (!IncludeUpper && f < 0.0f && float.IsInfinity(f))
			  {
				return null;
			  }
			  int i = NumericUtils.FloatToSortableInt(f);
			  inclusiveUpperPoint = NumericUtils.SortableIntToFloat(IncludeUpper ? i : (i - 1));
			}
			else
			{
			  inclusiveUpperPoint = float.PositiveInfinity;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

			FieldCache_Floats values = FieldCache_Fields.DEFAULT.GetFloats(context.Reader(), Field, (FieldCache_FloatParser) Parser, false);
			return new FieldCacheDocIdSetAnonymousInnerClassHelper7(this, context.Reader().MaxDoc(), acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper7 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper7 OuterInstance;

			  private float InclusiveLowerPoint;
			  private float InclusiveUpperPoint;
			  private Lucene.Net.Search.FieldCache_Floats Values;

			  public FieldCacheDocIdSetAnonymousInnerClassHelper7(FieldCacheRangeFilterAnonymousInnerClassHelper7 outerInstance, int maxDoc, Bits acceptDocs, float inclusiveLowerPoint, float inclusiveUpperPoint, Lucene.Net.Search.FieldCache_Floats values) : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				float value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getDoubles(AtomicReader,String,boolean)"/>. this works with all
	  /// double fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<double?> NewDoubleRange(string field, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewDoubleRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getDoubles(AtomicReader,String,FieldCache.DoubleParser,boolean)"/>. this works with all
	  /// double fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<double?> NewDoubleRange(string field, FieldCache_DoubleParser parser, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper8(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper8 : FieldCacheRangeFilter<double?>
	  {
		  private string Field;
		  private Lucene.Net.Search.FieldCache_DoubleParser Parser;
		  private double? LowerVal;
		  private double? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

		  public FieldCacheRangeFilterAnonymousInnerClassHelper8(string field, Lucene.Net.Search.FieldCache_DoubleParser parser, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper) : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			// we transform the floating point numbers to sortable integers
			// using NumericUtils to easier find the next bigger/lower value
			double inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  double f = (double)LowerVal;
			  if (!IncludeUpper && f > 0.0 && double.IsInfinity(f))
			  {
				return null;
			  }
			  long i = NumericUtils.DoubleToSortableLong(f);
			  inclusiveLowerPoint = NumericUtils.SortableLongToDouble(IncludeLower ? i : (i + 1L));
			}
			else
			{
			  inclusiveLowerPoint = double.NegativeInfinity;
			}
			if (UpperVal != null)
			{
			  double f = (double)UpperVal;
			  if (!IncludeUpper && f < 0.0 && double.IsInfinity(f))
			  {
				return null;
			  }
			  long i = NumericUtils.DoubleToSortableLong(f);
			  inclusiveUpperPoint = NumericUtils.SortableLongToDouble(IncludeUpper ? i : (i - 1L));
			}
			else
			{
			  inclusiveUpperPoint = double.PositiveInfinity;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

			FieldCache_Doubles values = FieldCache_Fields.DEFAULT.GetDoubles(context.Reader(), Field, (FieldCache_DoubleParser) Parser, false);
			// ignore deleted docs if range doesn't contain 0
			return new FieldCacheDocIdSetAnonymousInnerClassHelper8(this, context.Reader().MaxDoc(), acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper8 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper8 OuterInstance;

			  private double InclusiveLowerPoint;
			  private double InclusiveUpperPoint;
			  private Lucene.Net.Search.FieldCache_Doubles Values;

			  public FieldCacheDocIdSetAnonymousInnerClassHelper8(FieldCacheRangeFilterAnonymousInnerClassHelper8 outerInstance, int maxDoc, Bits acceptDocs, double inclusiveLowerPoint, double inclusiveUpperPoint, Lucene.Net.Search.FieldCache_Doubles values) : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				double value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }

	  public override sealed string ToString()
	  {
		StringBuilder sb = (new StringBuilder(Field_Renamed)).Append(":");
		return sb.Append(IncludeLower ? '[' : '{').Append((LowerVal_Renamed == null) ? "*" : LowerVal_Renamed.ToString()).Append(" TO ").Append((UpperVal_Renamed == null) ? "*" : UpperVal_Renamed.ToString()).Append(IncludeUpper ? ']' : '}').ToString();
	  }

	  public override sealed bool Equals(object o)
	  {
		if (this == o)
		{
			return true;
		}
		if (!(o is FieldCacheRangeFilter<T>))
		{
			return false;
		}
		FieldCacheRangeFilter<T> other = (FieldCacheRangeFilter<T>) o;

		if (!this.Field_Renamed.Equals(other.Field_Renamed) || this.IncludeLower != other.IncludeLower || this.IncludeUpper != other.IncludeUpper)
		{
			return false;
		}
		if (this.LowerVal_Renamed != null ?!this.LowerVal_Renamed.Equals(other.LowerVal_Renamed) : other.LowerVal_Renamed != null)
		{
			return false;
		}
		if (this.UpperVal_Renamed != null ?!this.UpperVal_Renamed.Equals(other.UpperVal_Renamed) : other.UpperVal_Renamed != null)
		{
			return false;
		}
		if (this.Parser_Renamed != null ?!this.Parser_Renamed.Equals(other.Parser_Renamed) : other.Parser_Renamed != null)
		{
			return false;
		}
		return true;
	  }

	  public override sealed int GetHashCode()
	  {
		int h = Field_Renamed.GetHashCode();
        h ^= (LowerVal_Renamed != null) ? LowerVal_Renamed.GetHashCode() : 550356204;
		h = (h << 1) | ((int)((uint)h >> 31)); // rotate to distinguish lower from upper
        h ^= (UpperVal_Renamed != null) ? UpperVal_Renamed.GetHashCode() : -1674416163;
        h ^= (Parser_Renamed != null) ? Parser_Renamed.GetHashCode() : -1572457324;
		h ^= (IncludeLower ? 1549299360 : -365038026) ^ (IncludeUpper ? 1721088258 : 1948649653);
		return h;
	  }

	  /// <summary>
	  /// Returns the field name for this filter </summary>
	  public virtual string Field
	  {
		  get
		  {
			  return Field_Renamed;
		  }
	  }

	  /// <summary>
	  /// Returns <code>true</code> if the lower endpoint is inclusive </summary>
	  public virtual bool IncludesLower()
	  {
		  return IncludeLower;
	  }

	  /// <summary>
	  /// Returns <code>true</code> if the upper endpoint is inclusive </summary>
	  public virtual bool IncludesUpper()
	  {
		  return IncludeUpper;
	  }

	  /// <summary>
	  /// Returns the lower value of this range filter </summary>
	  public virtual T LowerVal
	  {
		  get
		  {
			  return LowerVal_Renamed;
		  }
	  }

	  /// <summary>
	  /// Returns the upper value of this range filter </summary>
	  public virtual T UpperVal
	  {
		  get
		  {
			  return UpperVal_Renamed;
		  }
	  }

	  /// <summary>
	  /// Returns the current numeric parser ({@code null} for {@code T} is {@code String}} </summary>
	  public virtual FieldCache_Parser Parser
	  {
		  get
		  {
			  return Parser_Renamed;
		  }
	  }
	}

}