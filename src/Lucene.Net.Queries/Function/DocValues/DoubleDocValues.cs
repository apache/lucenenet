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
	using MutableValueDouble = org.apache.lucene.util.mutable.MutableValueDouble;

	/// <summary>
	/// Abstract <seealso cref="FunctionValues"/> implementation which supports retrieving double values.
	/// Implementations can control how the double values are loaded through <seealso cref="#doubleVal(int)"/>}
	/// </summary>
	public abstract class DoubleDocValues : FunctionValues
	{
	  protected internal readonly ValueSource vs;

	  public DoubleDocValues(ValueSource vs)
	  {
		this.vs = vs;
	  }

	  public override sbyte byteVal(int doc)
	  {
		return (sbyte)doubleVal(doc);
	  }

	  public override short shortVal(int doc)
	  {
		return (short)doubleVal(doc);
	  }

	  public override float floatVal(int doc)
	  {
		return (float)doubleVal(doc);
	  }

	  public override int intVal(int doc)
	  {
		return (int)doubleVal(doc);
	  }

	  public override long longVal(int doc)
	  {
		return (long)doubleVal(doc);
	  }

	  public override bool boolVal(int doc)
	  {
		return doubleVal(doc) != 0;
	  }

	  public override abstract double doubleVal(int doc);

	  public override string strVal(int doc)
	  {
		return Convert.ToString(doubleVal(doc));
	  }

	  public override object objectVal(int doc)
	  {
		return exists(doc) ? doubleVal(doc) : null;
	  }

	  public override string ToString(int doc)
	  {
		return vs.description() + '=' + strVal(doc);
	  }

	  public override ValueSourceScorer getRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
	  {
		double lower, upper;

		if (lowerVal == null)
		{
		  lower = double.NegativeInfinity;
		}
		else
		{
		  lower = Convert.ToDouble(lowerVal);
		}

		 if (upperVal == null)
		 {
		  upper = double.PositiveInfinity;
		 }
		else
		{
		  upper = Convert.ToDouble(upperVal);
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double l = lower;
		double l = lower;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double u = upper;
		double u = upper;


		if (includeLower && includeUpper)
		{
		  return new ValueSourceScorerAnonymousInnerClassHelper(this, reader, this, l, u);
		}
		else if (includeLower && !includeUpper)
		{
		  return new ValueSourceScorerAnonymousInnerClassHelper2(this, reader, this, l, u);
		}
		else if (!includeLower && includeUpper)
		{
		  return new ValueSourceScorerAnonymousInnerClassHelper3(this, reader, this, l, u);
		}
		else
		{
		  return new ValueSourceScorerAnonymousInnerClassHelper4(this, reader, this, l, u);
		}
	  }

	  private class ValueSourceScorerAnonymousInnerClassHelper : ValueSourceScorer
	  {
		  private readonly DoubleDocValues outerInstance;

		  private double l;
		  private double u;

		  public ValueSourceScorerAnonymousInnerClassHelper(DoubleDocValues outerInstance, IndexReader reader, org.apache.lucene.queries.function.docvalues.DoubleDocValues this, double l, double u) : base(reader, this)
		  {
			  this.outerInstance = outerInstance;
			  this.l = l;
			  this.u = u;
		  }

		  public override bool matchesValue(int doc)
		  {
			double docVal = outerInstance.doubleVal(doc);
			return docVal >= l && docVal <= u;
		  }
	  }

	  private class ValueSourceScorerAnonymousInnerClassHelper2 : ValueSourceScorer
	  {
		  private readonly DoubleDocValues outerInstance;

		  private double l;
		  private double u;

		  public ValueSourceScorerAnonymousInnerClassHelper2(DoubleDocValues outerInstance, IndexReader reader, org.apache.lucene.queries.function.docvalues.DoubleDocValues this, double l, double u) : base(reader, this)
		  {
			  this.outerInstance = outerInstance;
			  this.l = l;
			  this.u = u;
		  }

		  public override bool matchesValue(int doc)
		  {
			double docVal = outerInstance.doubleVal(doc);
			return docVal >= l && docVal < u;
		  }
	  }

	  private class ValueSourceScorerAnonymousInnerClassHelper3 : ValueSourceScorer
	  {
		  private readonly DoubleDocValues outerInstance;

		  private double l;
		  private double u;

		  public ValueSourceScorerAnonymousInnerClassHelper3(DoubleDocValues outerInstance, IndexReader reader, org.apache.lucene.queries.function.docvalues.DoubleDocValues this, double l, double u) : base(reader, this)
		  {
			  this.outerInstance = outerInstance;
			  this.l = l;
			  this.u = u;
		  }

		  public override bool matchesValue(int doc)
		  {
			double docVal = outerInstance.doubleVal(doc);
			return docVal > l && docVal <= u;
		  }
	  }

	  private class ValueSourceScorerAnonymousInnerClassHelper4 : ValueSourceScorer
	  {
		  private readonly DoubleDocValues outerInstance;

		  private double l;
		  private double u;

		  public ValueSourceScorerAnonymousInnerClassHelper4(DoubleDocValues outerInstance, IndexReader reader, org.apache.lucene.queries.function.docvalues.DoubleDocValues this, double l, double u) : base(reader, this)
		  {
			  this.outerInstance = outerInstance;
			  this.l = l;
			  this.u = u;
		  }

		  public override bool matchesValue(int doc)
		  {
			double docVal = outerInstance.doubleVal(doc);
			return docVal > l && docVal < u;
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
		  private readonly DoubleDocValues outerInstance;

		  public ValueFillerAnonymousInnerClassHelper(DoubleDocValues outerInstance)
		  {
			  this.outerInstance = outerInstance;
			  mval = new MutableValueDouble();
		  }

		  private readonly MutableValueDouble mval;

		  public override MutableValue Value
		  {
			  get
			  {
				return mval;
			  }
		  }

		  public override void fillValue(int doc)
		  {
			mval.value = outerInstance.doubleVal(doc);
			mval.exists = outerInstance.exists(doc);
		  }
	  }

	}

}