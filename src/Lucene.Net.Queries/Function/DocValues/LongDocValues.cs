using System;

namespace org.apache.lucene.queries.function.docvalues
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

	using IndexReader = org.apache.lucene.index.IndexReader;
	using MutableValue = org.apache.lucene.util.mutable.MutableValue;
	using MutableValueLong = org.apache.lucene.util.mutable.MutableValueLong;

	/// <summary>
	/// Abstract <seealso cref="FunctionValues"/> implementation which supports retrieving long values.
	/// Implementations can control how the long values are loaded through <seealso cref="#longVal(int)"/>}
	/// </summary>
	public abstract class LongDocValues : FunctionValues
	{
	  protected internal readonly ValueSource vs;

	  public LongDocValues(ValueSource vs)
	  {
		this.vs = vs;
	  }

	  public override sbyte byteVal(int doc)
	  {
		return (sbyte)longVal(doc);
	  }

	  public override short shortVal(int doc)
	  {
		return (short)longVal(doc);
	  }

	  public override float floatVal(int doc)
	  {
		return (float)longVal(doc);
	  }

	  public override int intVal(int doc)
	  {
		return (int)longVal(doc);
	  }

	  public override abstract long longVal(int doc);

	  public override double doubleVal(int doc)
	  {
		return (double)longVal(doc);
	  }

	  public override bool boolVal(int doc)
	  {
		return longVal(doc) != 0;
	  }

	  public override string strVal(int doc)
	  {
		return Convert.ToString(longVal(doc));
	  }

	  public override object objectVal(int doc)
	  {
		return exists(doc) ? longVal(doc) : null;
	  }

	  public override string ToString(int doc)
	  {
		return vs.description() + '=' + strVal(doc);
	  }

	  protected internal virtual long externalToLong(string extVal)
	  {
		return Convert.ToInt64(extVal);
	  }

	  public override ValueSourceScorer getRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
	  {
		long lower, upper;

		// instead of using separate comparison functions, adjust the endpoints.

		if (lowerVal == null)
		{
		  lower = long.MinValue;
		}
		else
		{
		  lower = externalToLong(lowerVal);
		  if (!includeLower && lower < long.MaxValue)
		  {
			  lower++;
		  }
		}

		 if (upperVal == null)
		 {
		  upper = long.MaxValue;
		 }
		else
		{
		  upper = externalToLong(upperVal);
		  if (!includeUpper && upper > long.MinValue)
		  {
			  upper--;
		  }
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long ll = lower;
		long ll = lower;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long uu = upper;
		long uu = upper;

		return new ValueSourceScorerAnonymousInnerClassHelper(this, reader, this, ll, uu);
	  }

	  private class ValueSourceScorerAnonymousInnerClassHelper : ValueSourceScorer
	  {
		  private readonly LongDocValues outerInstance;

		  private long ll;
		  private long uu;

		  public ValueSourceScorerAnonymousInnerClassHelper(LongDocValues outerInstance, IndexReader reader, org.apache.lucene.queries.function.docvalues.LongDocValues this, long ll, long uu) : base(reader, this)
		  {
			  this.outerInstance = outerInstance;
			  this.ll = ll;
			  this.uu = uu;
		  }

		  public override bool matchesValue(int doc)
		  {
			long val = outerInstance.longVal(doc);
			// only check for deleted if it's the default value
			// if (val==0 && reader.isDeleted(doc)) return false;
			return val >= ll && val <= uu;
		  }
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
		  private readonly LongDocValues outerInstance;

		  public ValueFillerAnonymousInnerClassHelper(LongDocValues outerInstance)
		  {
			  this.outerInstance = outerInstance;
			  mval = new MutableValueLong();
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
			mval.value = outerInstance.longVal(doc);
			mval.exists = outerInstance.exists(doc);
		  }
	  }
	}

}