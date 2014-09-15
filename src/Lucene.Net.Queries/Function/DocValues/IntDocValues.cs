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
	using MutableValueInt = org.apache.lucene.util.mutable.MutableValueInt;

	/// <summary>
	/// Abstract <seealso cref="FunctionValues"/> implementation which supports retrieving int values.
	/// Implementations can control how the int values are loaded through <seealso cref="#intVal(int)"/>
	/// </summary>
	public abstract class IntDocValues : FunctionValues
	{
	  protected internal readonly ValueSource vs;

	  public IntDocValues(ValueSource vs)
	  {
		this.vs = vs;
	  }

	  public override sbyte byteVal(int doc)
	  {
		return (sbyte)intVal(doc);
	  }

	  public override short shortVal(int doc)
	  {
		return (short)intVal(doc);
	  }

	  public override float floatVal(int doc)
	  {
		return (float)intVal(doc);
	  }

	  public override abstract int intVal(int doc);

	  public override long longVal(int doc)
	  {
		return (long)intVal(doc);
	  }

	  public override double doubleVal(int doc)
	  {
		return (double)intVal(doc);
	  }

	  public override string strVal(int doc)
	  {
		return Convert.ToString(intVal(doc));
	  }

	  public override object objectVal(int doc)
	  {
		return exists(doc) ? intVal(doc) : null;
	  }

	  public override string ToString(int doc)
	  {
		return vs.description() + '=' + strVal(doc);
	  }

	  public override ValueSourceScorer getRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
	  {
		int lower, upper;

		// instead of using separate comparison functions, adjust the endpoints.

		if (lowerVal == null)
		{
		  lower = int.MinValue;
		}
		else
		{
		  lower = Convert.ToInt32(lowerVal);
		  if (!includeLower && lower < int.MaxValue)
		  {
			  lower++;
		  }
		}

		 if (upperVal == null)
		 {
		  upper = int.MaxValue;
		 }
		else
		{
		  upper = Convert.ToInt32(upperVal);
		  if (!includeUpper && upper > int.MinValue)
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
		  private readonly IntDocValues outerInstance;

		  private int ll;
		  private int uu;

		  public ValueSourceScorerAnonymousInnerClassHelper(IntDocValues outerInstance, IndexReader reader, org.apache.lucene.queries.function.docvalues.IntDocValues this, int ll, int uu) : base(reader, this)
		  {
			  this.outerInstance = outerInstance;
			  this.ll = ll;
			  this.uu = uu;
		  }

		  public override bool matchesValue(int doc)
		  {
			int val = outerInstance.intVal(doc);
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
		  private readonly IntDocValues outerInstance;

		  public ValueFillerAnonymousInnerClassHelper(IntDocValues outerInstance)
		  {
			  this.outerInstance = outerInstance;
			  mval = new MutableValueInt();
		  }

		  private readonly MutableValueInt mval;

		  public override MutableValue Value
		  {
			  get
			  {
				return mval;
			  }
		  }

		  public override void fillValue(int doc)
		  {
			mval.value = outerInstance.intVal(doc);
			mval.exists = outerInstance.exists(doc);
		  }
	  }
	}

}