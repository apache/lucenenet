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
using System.Collections;
using org.apache.lucene.queries.function;
using org.apache.lucene.queries.function.docvalues;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
	/// Abstract <seealso cref="ValueSource"/> implementation which wraps two ValueSources
	/// and applies an extendible float function to their values.
	/// 
	/// </summary>
	public abstract class DualFloatFunction : ValueSource
	{
	  protected internal readonly ValueSource a;
	  protected internal readonly ValueSource b;

	 /// <param name="a">  the base. </param>
	 /// <param name="b">  the exponent. </param>
	  public DualFloatFunction(ValueSource a, ValueSource b)
	  {
		this.a = a;
		this.b = b;
	  }

	  protected internal abstract string name();
	  protected internal abstract float func(int doc, FunctionValues aVals, FunctionValues bVals);

	  public override string description()
	  {
		return name() + "(" + a.description() + "," + b.description() + ")";
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues aVals = a.getValues(context, readerContext);
		FunctionValues aVals = a.getValues(context, readerContext);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues bVals = b.getValues(context, readerContext);
		FunctionValues bVals = b.getValues(context, readerContext);
		return new FloatDocValuesAnonymousInnerClassHelper(this, this, aVals, bVals);
	  }

	  private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
	  {
		  private readonly DualFloatFunction outerInstance;

		  private FunctionValues aVals;
		  private FunctionValues bVals;

		  public FloatDocValuesAnonymousInnerClassHelper(DualFloatFunction outerInstance, DualFloatFunction this, FunctionValues aVals, FunctionValues bVals) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.aVals = aVals;
			  this.bVals = bVals;
		  }

		  public override float floatVal(int doc)
		  {
			return outerInstance.func(doc, aVals, bVals);
		  }

		  public override string ToString(int doc)
		  {
			return outerInstance.name() + '(' + aVals.ToString(doc) + ',' + bVals.ToString(doc) + ')';
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void createWeight(java.util.Map context, org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public override void createWeight(IDictionary context, IndexSearcher searcher)
	  {
		a.createWeight(context,searcher);
		b.createWeight(context,searcher);
	  }

	  public override int GetHashCode()
	  {
		int h = a.GetHashCode();
		h ^= (h << 13) | ((int)((uint)h >> 20));
		h += b.GetHashCode();
		h ^= (h << 23) | ((int)((uint)h >> 10));
		h += name().GetHashCode();
		return h;
	  }

	  public override bool Equals(object o)
	  {
		if (this.GetType() != o.GetType())
		{
			return false;
		}
		DualFloatFunction other = (DualFloatFunction)o;
		return this.a.Equals(other.a) && this.b.Equals(other.b);
	  }
	}

}