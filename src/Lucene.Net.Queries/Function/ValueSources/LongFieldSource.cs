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
using System;
using System.Collections;
using Lucene.Net.Queries.Function.DocValues;
using org.apache.lucene.queries.function;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
	/// Obtains long field values from <seealso cref="FieldCache#getLongs"/> and makes those
	/// values available as other numeric types, casting as needed.
	/// </summary>
	public class LongFieldSource : FieldCacheSource
	{

	  protected internal readonly FieldCache.LongParser parser;

	  public LongFieldSource(string field) : this(field, null)
	  {
	  }

	  public LongFieldSource(string field, FieldCache.LongParser parser) : base(field)
	  {
		this.parser = parser;
	  }

	  public override string description()
	  {
		return "long(" + field + ')';
	  }

	  public virtual long externalToLong(string extVal)
	  {
		return Convert.ToInt64(extVal);
	  }

	  public virtual object longToObject(long val)
	  {
		return val;
	  }

	  public virtual string longToString(long val)
	  {
		return longToObject(val).ToString();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues GetValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.FieldCache.Longs arr = cache.getLongs(readerContext.reader(), field, parser, true);
		FieldCache.Longs arr = cache.getLongs(readerContext.reader(), field, parser, true);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.Bits valid = cache.getDocsWithField(readerContext.reader(), field);
		Bits valid = cache.getDocsWithField(readerContext.reader(), field);

		return new LongDocValuesAnonymousInnerClassHelper(this, this, arr, valid);
	  }

	  private class LongDocValuesAnonymousInnerClassHelper : LongDocValues
	  {
		  private readonly LongFieldSource outerInstance;

		  private FieldCache.Longs arr;
		  private Bits valid;

		  public LongDocValuesAnonymousInnerClassHelper(LongFieldSource outerInstance, LongFieldSource this, FieldCache.Longs arr, Bits valid) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.arr = arr;
			  this.valid = valid;
		  }

		  public override long LongVal(int doc)
		  {
			return arr.get(doc);
		  }

		  public override bool exists(int doc)
		  {
			return arr.get(doc) != 0 || valid.get(doc);
		  }

		  public override object objectVal(int doc)
		  {
			return valid.get(doc) ? outerInstance.longToObject(arr.get(doc)) : null;
		  }

		  public override string StrVal(int doc)
		  {
			return valid.get(doc) ? outerInstance.longToString(arr.get(doc)) : null;
		  }

		  protected internal override long externalToLong(string extVal)
		  {
			return outerInstance.externalToLong(extVal);
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
			  private readonly LongDocValuesAnonymousInnerClassHelper outerInstance;

			  public ValueFillerAnonymousInnerClassHelper(LongDocValuesAnonymousInnerClassHelper outerInstance)
			  {
				  this.outerInstance = outerInstance;
				  mval = outerInstance.outerInstance.newMutableValueLong();
			  }

			  private readonly MutableValueLong mval;

			  public override MutableValue Value
			  {
				  get
				  {
					return mval;
				  }
			  }

			  public override void fillValue(int doc)
			  {
				mval.value = outerInstance.arr.get(doc);
				mval.exists = mval.value != 0 || outerInstance.valid.get(doc);
			  }
		  }

	  }

	  protected internal virtual MutableValueLong newMutableValueLong()
	  {
		return new MutableValueLong();
	  }

	  public override bool Equals(object o)
	  {
		if (o.GetType() != this.GetType())
		{
			return false;
		}
		LongFieldSource other = (LongFieldSource) o;
		return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
	  }

	  public override int GetHashCode()
	  {
		int h = parser == null ? this.GetType().GetHashCode() : parser.GetType().GetHashCode();
		h += base.GetHashCode();
		return h;
	  }
	}

}