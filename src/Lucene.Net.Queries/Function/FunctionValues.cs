using System;

namespace org.apache.lucene.queries.function
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

	using org.apache.lucene.search;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using MutableValue = org.apache.lucene.util.mutable.MutableValue;
	using MutableValueFloat = org.apache.lucene.util.mutable.MutableValueFloat;

	/// <summary>
	/// Represents field values as different types.
	/// Normally created via a <seealso cref="ValueSource"/> for a particular field and reader.
	/// 
	/// 
	/// </summary>

	// FunctionValues is distinct from ValueSource because
	// there needs to be an object created at query evaluation time that
	// is not referenced by the query itself because:
	// - Query objects should be MT safe
	// - For caching, Query objects are often used as keys... you don't
	//   want the Query carrying around big objects
	public abstract class FunctionValues
	{

	  public virtual sbyte byteVal(int doc)
	  {
		  throw new System.NotSupportedException();
	  }
	  public virtual short shortVal(int doc)
	  {
		  throw new System.NotSupportedException();
	  }

	  public virtual float floatVal(int doc)
	  {
		  throw new System.NotSupportedException();
	  }
	  public virtual int intVal(int doc)
	  {
		  throw new System.NotSupportedException();
	  }
	  public virtual long longVal(int doc)
	  {
		  throw new System.NotSupportedException();
	  }
	  public virtual double doubleVal(int doc)
	  {
		  throw new System.NotSupportedException();
	  }
	  // TODO: should we make a termVal, returns BytesRef?
	  public virtual string strVal(int doc)
	  {
		  throw new System.NotSupportedException();
	  }

	  public virtual bool boolVal(int doc)
	  {
		return intVal(doc) != 0;
	  }

	  /// <summary>
	  /// returns the bytes representation of the string val - TODO: should this return the indexed raw bytes not? </summary>
	  public virtual bool bytesVal(int doc, BytesRef target)
	  {
		string s = strVal(doc);
		if (s == null)
		{
		  target.length = 0;
		  return false;
		}
		target.copyChars(s);
		return true;
	  }

	  /// <summary>
	  /// Native Java Object representation of the value </summary>
	  public virtual object objectVal(int doc)
	  {
		// most FunctionValues are functions, so by default return a Float()
		return floatVal(doc);
	  }

	  /// <summary>
	  /// Returns true if there is a value for this document </summary>
	  public virtual bool exists(int doc)
	  {
		return true;
	  }

	  /// <param name="doc"> The doc to retrieve to sort ordinal for </param>
	  /// <returns> the sort ordinal for the specified doc
	  /// TODO: Maybe we can just use intVal for this... </returns>
	  public virtual int ordVal(int doc)
	  {
		  throw new System.NotSupportedException();
	  }

	  /// <returns> the number of unique sort ordinals this instance has </returns>
	  public virtual int numOrd()
	  {
		  throw new System.NotSupportedException();
	  }
	  public abstract string ToString(int doc);

	  /// <summary>
	  /// Abstraction of the logic required to fill the value of a specified doc into
	  /// a reusable <seealso cref="MutableValue"/>.  Implementations of <seealso cref="FunctionValues"/>
	  /// are encouraged to define their own implementations of ValueFiller if their
	  /// value is not a float.
	  /// 
	  /// @lucene.experimental
	  /// </summary>
	  public abstract class ValueFiller
	  {
		/// <summary>
		/// MutableValue will be reused across calls </summary>
		public abstract MutableValue Value {get;}

		/// <summary>
		/// MutableValue will be reused across calls.  Returns true if the value exists. </summary>
		public abstract void fillValue(int doc);
	  }

	  /// <summary>
	  /// @lucene.experimental </summary>
	  public virtual ValueFiller ValueFiller
	  {
		  get
		  {
			return new ValueFillerAnonymousInnerClassHelper(this);
		  }
	  }

	  private class ValueFillerAnonymousInnerClassHelper : ValueFiller
	  {
		  private readonly FunctionValues outerInstance;

		  public ValueFillerAnonymousInnerClassHelper(FunctionValues outerInstance)
		  {
			  this.outerInstance = outerInstance;
			  mval = new MutableValueFloat();
		  }

		  private readonly MutableValueFloat mval;

		  public override MutableValue Value
		  {
			  get
			  {
				return mval;
			  }
		  }

		  public override void fillValue(int doc)
		  {
			mval.value = outerInstance.floatVal(doc);
		  }
	  }

	  //For Functions that can work with multiple values from the same document.  This does not apply to all functions
	  public virtual void byteVal(int doc, sbyte[] vals)
	  {
		  throw new System.NotSupportedException();
	  }
	  public virtual void shortVal(int doc, short[] vals)
	  {
		  throw new System.NotSupportedException();
	  }

	  public virtual void floatVal(int doc, float[] vals)
	  {
		  throw new System.NotSupportedException();
	  }
	  public virtual void intVal(int doc, int[] vals)
	  {
		  throw new System.NotSupportedException();
	  }
	  public virtual void longVal(int doc, long[] vals)
	  {
		  throw new System.NotSupportedException();
	  }
	  public virtual void doubleVal(int doc, double[] vals)
	  {
		  throw new System.NotSupportedException();
	  }

	  // TODO: should we make a termVal, fills BytesRef[]?
	  public virtual void strVal(int doc, string[] vals)
	  {
		  throw new System.NotSupportedException();
	  }

	  public virtual Explanation explain(int doc)
	  {
		return new Explanation(floatVal(doc), ToString(doc));
	  }

	  public virtual ValueSourceScorer getScorer(IndexReader reader)
	  {
		return new ValueSourceScorer(reader, this);
	  }

	  // A RangeValueSource can't easily be a ValueSource that takes another ValueSource
	  // because it needs different behavior depending on the type of fields.  There is also
	  // a setup cost - parsing and normalizing params, and doing a binary search on the StringIndex.
	  // TODO: change "reader" to AtomicReaderContext
	  public virtual ValueSourceScorer getRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
	  {
		float lower;
		float upper;

		if (lowerVal == null)
		{
		  lower = float.NegativeInfinity;
		}
		else
		{
		  lower = Convert.ToSingle(lowerVal);
		}
		if (upperVal == null)
		{
		  upper = float.PositiveInfinity;
		}
		else
		{
		  upper = Convert.ToSingle(upperVal);
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float l = lower;
		float l = lower;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float u = upper;
		float u = upper;

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
		  private readonly FunctionValues outerInstance;

		  private float l;
		  private float u;

		  public ValueSourceScorerAnonymousInnerClassHelper(FunctionValues outerInstance, IndexReader reader, org.apache.lucene.queries.function.FunctionValues this, float l, float u) : base(reader, this)
		  {
			  this.outerInstance = outerInstance;
			  this.l = l;
			  this.u = u;
		  }

		  public override bool matchesValue(int doc)
		  {
			float docVal = outerInstance.floatVal(doc);
			return docVal >= l && docVal <= u;
		  }
	  }

	  private class ValueSourceScorerAnonymousInnerClassHelper2 : ValueSourceScorer
	  {
		  private readonly FunctionValues outerInstance;

		  private float l;
		  private float u;

		  public ValueSourceScorerAnonymousInnerClassHelper2(FunctionValues outerInstance, IndexReader reader, org.apache.lucene.queries.function.FunctionValues this, float l, float u) : base(reader, this)
		  {
			  this.outerInstance = outerInstance;
			  this.l = l;
			  this.u = u;
		  }

		  public override bool matchesValue(int doc)
		  {
			float docVal = outerInstance.floatVal(doc);
			return docVal >= l && docVal < u;
		  }
	  }

	  private class ValueSourceScorerAnonymousInnerClassHelper3 : ValueSourceScorer
	  {
		  private readonly FunctionValues outerInstance;

		  private float l;
		  private float u;

		  public ValueSourceScorerAnonymousInnerClassHelper3(FunctionValues outerInstance, IndexReader reader, org.apache.lucene.queries.function.FunctionValues this, float l, float u) : base(reader, this)
		  {
			  this.outerInstance = outerInstance;
			  this.l = l;
			  this.u = u;
		  }

		  public override bool matchesValue(int doc)
		  {
			float docVal = outerInstance.floatVal(doc);
			return docVal > l && docVal <= u;
		  }
	  }

	  private class ValueSourceScorerAnonymousInnerClassHelper4 : ValueSourceScorer
	  {
		  private readonly FunctionValues outerInstance;

		  private float l;
		  private float u;

		  public ValueSourceScorerAnonymousInnerClassHelper4(FunctionValues outerInstance, IndexReader reader, org.apache.lucene.queries.function.FunctionValues this, float l, float u) : base(reader, this)
		  {
			  this.outerInstance = outerInstance;
			  this.l = l;
			  this.u = u;
		  }

		  public override bool matchesValue(int doc)
		  {
			float docVal = outerInstance.floatVal(doc);
			return docVal > l && docVal < u;
		  }
	  }
	}




}