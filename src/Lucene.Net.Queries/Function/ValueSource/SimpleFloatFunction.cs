using System.Collections;

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

namespace org.apache.lucene.queries.function.valuesource
{

	using AtomicReaderContext = org.apache.lucene.index.AtomicReaderContext;
	using FloatDocValues = org.apache.lucene.queries.function.docvalues.FloatDocValues;


	/// <summary>
	/// A simple float function with a single argument
	/// </summary>
	 public abstract class SimpleFloatFunction : SingleFunction
	 {
	  public SimpleFloatFunction(ValueSource source) : base(source)
	  {
	  }

	  protected internal abstract float func(int doc, FunctionValues vals);

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues vals = source.getValues(context, readerContext);
		FunctionValues vals = source.getValues(context, readerContext);
		return new FloatDocValuesAnonymousInnerClassHelper(this, this, vals);
	  }

	  private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
	  {
		  private readonly SimpleFloatFunction outerInstance;

		  private FunctionValues vals;

		  public FloatDocValuesAnonymousInnerClassHelper(SimpleFloatFunction outerInstance, org.apache.lucene.queries.function.valuesource.SimpleFloatFunction this, FunctionValues vals) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.vals = vals;
		  }

		  public override float floatVal(int doc)
		  {
			return outerInstance.func(doc, vals);
		  }
		  public override string ToString(int doc)
		  {
			return outerInstance.name() + '(' + vals.ToString(doc) + ')';
		  }
	  }
	 }

}