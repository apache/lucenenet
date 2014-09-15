using System.Collections;
using System.Collections.Generic;
using System.Text;

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
	/// Abstract <seealso cref="ValueSource"/> implementation which wraps multiple ValueSources
	/// and applies an extendible boolean function to their values.
	/// 
	/// </summary>
	public abstract class MultiBoolFunction : BoolFunction
	{
	  protected internal readonly IList<ValueSource> sources;

	  public MultiBoolFunction(IList<ValueSource> sources)
	  {
		this.sources = sources;
	  }

	  protected internal abstract string name();

	  protected internal abstract bool func(int doc, FunctionValues[] vals);

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.docvalues.BoolDocValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override BoolDocValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues[] vals = new org.apache.lucene.queries.function.FunctionValues[sources.size()];
		FunctionValues[] vals = new FunctionValues[sources.Count];
		int i = 0;
		foreach (ValueSource source in sources)
		{
		  vals[i++] = source.getValues(context, readerContext);
		}

		return new BoolDocValuesAnonymousInnerClassHelper(this, this, vals);
	  }

	  private class BoolDocValuesAnonymousInnerClassHelper : BoolDocValues
	  {
		  private readonly MultiBoolFunction outerInstance;

		  private FunctionValues[] vals;

		  public BoolDocValuesAnonymousInnerClassHelper(MultiBoolFunction outerInstance, org.apache.lucene.queries.function.valuesource.MultiBoolFunction this, FunctionValues[] vals) : base(this)
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
			StringBuilder sb = new StringBuilder(outerInstance.name());
			sb.Append('(');
			bool first = true;
			foreach (FunctionValues dv in vals)
			{
			  if (first)
			  {
				first = false;
			  }
			  else
			  {
				sb.Append(',');
			  }
			  sb.Append(dv.ToString(doc));
			}
			return sb.ToString();
		  }
	  }

	  public override string description()
	  {
		StringBuilder sb = new StringBuilder(name());
		sb.Append('(');
		bool first = true;
		foreach (ValueSource source in sources)
		{
		  if (first)
		  {
			first = false;
		  }
		  else
		  {
			sb.Append(',');
		  }
		  sb.Append(source.description());
		}
		return sb.ToString();
	  }

	  public override int GetHashCode()
	  {
		return sources.GetHashCode() + name().GetHashCode();
	  }

	  public override bool Equals(object o)
	  {
		if (this.GetType() != o.GetType())
		{
			return false;
		}
		MultiBoolFunction other = (MultiBoolFunction)o;
		return this.sources.Equals(other.sources);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void createWeight(java.util.Map context, org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public override void createWeight(IDictionary context, IndexSearcher searcher)
	  {
		foreach (ValueSource source in sources)
		{
		  source.createWeight(context, searcher);
		}
	  }
	}

}