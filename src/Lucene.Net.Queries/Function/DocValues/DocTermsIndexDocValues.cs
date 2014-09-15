using System;

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

namespace org.apache.lucene.queries.function.docvalues
{

	using AtomicReaderContext = org.apache.lucene.index.AtomicReaderContext;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using SortedDocValues = org.apache.lucene.index.SortedDocValues;
	using FieldCache = org.apache.lucene.search.FieldCache;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using CharsRef = org.apache.lucene.util.CharsRef;
	using UnicodeUtil = org.apache.lucene.util.UnicodeUtil;
	using MutableValue = org.apache.lucene.util.mutable.MutableValue;
	using MutableValueStr = org.apache.lucene.util.mutable.MutableValueStr;

	/// <summary>
	/// Serves as base class for FunctionValues based on DocTermsIndex.
	/// @lucene.internal
	/// </summary>
	public abstract class DocTermsIndexDocValues : FunctionValues
	{
	  protected internal readonly SortedDocValues termsIndex;
	  protected internal readonly ValueSource vs;
	  protected internal readonly MutableValueStr val = new MutableValueStr();
	  protected internal readonly BytesRef spare = new BytesRef();
	  protected internal readonly CharsRef spareChars = new CharsRef();

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public DocTermsIndexDocValues(org.apache.lucene.queries.function.ValueSource vs, org.apache.lucene.index.AtomicReaderContext context, String field) throws java.io.IOException
	  public DocTermsIndexDocValues(ValueSource vs, AtomicReaderContext context, string field)
	  {
		try
		{
		  termsIndex = FieldCache.DEFAULT.getTermsIndex(context.reader(), field);
		}
		catch (Exception e)
		{
		  throw new DocTermsIndexException(field, e);
		}
		this.vs = vs;
	  }

	  protected internal abstract string toTerm(string readableValue);

	  public override bool exists(int doc)
	  {
		return ordVal(doc) >= 0;
	  }

	  public override int ordVal(int doc)
	  {
		return termsIndex.getOrd(doc);
	  }

	  public override int numOrd()
	  {
		return termsIndex.ValueCount;
	  }

	  public override bool bytesVal(int doc, BytesRef target)
	  {
		termsIndex.get(doc, target);
		return target.length > 0;
	  }

	  public override string strVal(int doc)
	  {
		termsIndex.get(doc, spare);
		if (spare.length == 0)
		{
		  return null;
		}
		UnicodeUtil.UTF8toUTF16(spare, spareChars);
		return spareChars.ToString();
	  }

	  public override bool boolVal(int doc)
	  {
		return exists(doc);
	  }

	  public override abstract object objectVal(int doc); // force subclasses to override

	  public override ValueSourceScorer getRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
	  {
		// TODO: are lowerVal and upperVal in indexed form or not?
		lowerVal = lowerVal == null ? null : toTerm(lowerVal);
		upperVal = upperVal == null ? null : toTerm(upperVal);

		int lower = int.MinValue;
		if (lowerVal != null)
		{
		  lower = termsIndex.lookupTerm(new BytesRef(lowerVal));
		  if (lower < 0)
		  {
			lower = -lower - 1;
		  }
		  else if (!includeLower)
		  {
			lower++;
		  }
		}

		int upper = int.MaxValue;
		if (upperVal != null)
		{
		  upper = termsIndex.lookupTerm(new BytesRef(upperVal));
		  if (upper < 0)
		  {
			upper = -upper - 2;
		  }
		  else if (!includeUpper)
		  {
			upper--;
		  }
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ll = lower;
		int ll = lower;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int uu = upper;
		int uu = upper;

		return new ValueSourceScorerAnonymousInnerClassHelper(this, reader, this, ll, uu);
	  }

	  private class ValueSourceScorerAnonymousInnerClassHelper : ValueSourceScorer
	  {
		  private readonly DocTermsIndexDocValues outerInstance;

		  private int ll;
		  private int uu;

		  public ValueSourceScorerAnonymousInnerClassHelper(DocTermsIndexDocValues outerInstance, IndexReader reader, org.apache.lucene.queries.function.docvalues.DocTermsIndexDocValues this, int ll, int uu) : base(reader, this)
		  {
			  this.outerInstance = outerInstance;
			  this.ll = ll;
			  this.uu = uu;
		  }

		  public override bool matchesValue(int doc)
		  {
			int ord = outerInstance.termsIndex.getOrd(doc);
			return ord >= ll && ord <= uu;
		  }
	  }

	  public override string ToString(int doc)
	  {
		return vs.description() + '=' + strVal(doc);
	  }

	  public override ValueFiller ValueFiller
	  {
		  get
		  {
			return new ValueFillerAnonymousInnerClassHelper(this);
		  }
	  }

	  private class ValueFillerAnonymousInnerClassHelper : ValueFiller
	  {
		  private readonly DocTermsIndexDocValues outerInstance;

		  public ValueFillerAnonymousInnerClassHelper(DocTermsIndexDocValues outerInstance)
		  {
			  this.outerInstance = outerInstance;
			  mval = new MutableValueStr();
		  }

		  private readonly MutableValueStr mval;

		  public override MutableValue Value
		  {
			  get
			  {
				return mval;
			  }
		  }

		  public override void fillValue(int doc)
		  {
			int ord = outerInstance.termsIndex.getOrd(doc);
			if (ord == -1)
			{
			  mval.value.bytes = BytesRef.EMPTY_BYTES;
			  mval.value.offset = 0;
			  mval.value.length = 0;
			  mval.exists = false;
			}
			else
			{
			  outerInstance.termsIndex.lookupOrd(ord, mval.value);
			  mval.exists = true;
			}
		  }
	  }

	  /// <summary>
	  /// Custom Exception to be thrown when the DocTermsIndex for a field cannot be generated
	  /// </summary>
	  public sealed class DocTermsIndexException : Exception
	  {

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public DocTermsIndexException(final String fieldName, final RuntimeException cause)
		public DocTermsIndexException(string fieldName, Exception cause) : base("Can't initialize DocTermsIndex to generate (function) FunctionValues for field: " + fieldName, cause)
		{
		}

	  }


	}

}