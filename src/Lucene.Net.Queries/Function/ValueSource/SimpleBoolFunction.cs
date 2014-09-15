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
	using BoolDocValues = org.apache.lucene.queries.function.docvalues.BoolDocValues;
	using IndexSearcher = org.apache.lucene.search.IndexSearcher;


	/// <summary>
	/// <seealso cref="BoolFunction"/> implementation which applies an extendible boolean
	/// function to the values of a single wrapped <seealso cref="ValueSource"/>.
	/// 
	/// Functions this can be used for include whether a field has a value or not,
	/// or inverting the boolean value of the wrapped ValueSource.
	/// </summary>
	public abstract class SimpleBoolFunction : BoolFunction
	{
	  protected internal readonly ValueSource source;

	  public SimpleBoolFunction(ValueSource source)
	  {
		this.source = source;
	  }

	  protected internal abstract string name();

	  protected internal abstract bool func(int doc, FunctionValues vals);

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.docvalues.BoolDocValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override BoolDocValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues vals = source.getValues(context, readerContext);
		FunctionValues vals = source.getValues(context, readerContext);
		return new BoolDocValuesAnonymousInnerClassHelper(this, this, vals);
	  }

	  private class BoolDocValuesAnonymousInnerClassHelper : BoolDocValues
	  {
		  private readonly SimpleBoolFunction outerInstance;

		  private FunctionValues vals;

		  public BoolDocValuesAnonymousInnerClassHelper(SimpleBoolFunction outerInstance, org.apache.lucene.queries.function.valuesource.SimpleBoolFunction this, FunctionValues vals) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.vals = vals;
		  }

		  public override bool boolVal(int doc)
		  {
			return outerInstance.func(doc, vals);
		  }
		  public override string ToString(int doc)
		  {
			return outerInstance.name() + '(' + vals.ToString(doc) + ')';
		  }
	  }

	  public override string description()
	  {
		return name() + '(' + source.description() + ')';
	  }

	  public override int GetHashCode()
	  {
		return source.GetHashCode() + name().GetHashCode();
	  }

	  public override bool Equals(object o)
	  {
		if (this.GetType() != o.GetType())
		{
			return false;
		}
		SimpleBoolFunction other = (SimpleBoolFunction)o;
		return this.source.Equals(other.source);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void createWeight(java.util.Map context, org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public override void createWeight(IDictionary context, IndexSearcher searcher)
	  {
		source.createWeight(context, searcher);
	  }
	}

}