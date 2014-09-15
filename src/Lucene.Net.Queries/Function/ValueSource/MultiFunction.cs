using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace org.apache.lucene.queries.function.valuesource
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

	using AtomicReaderContext = org.apache.lucene.index.AtomicReaderContext;
	using IndexSearcher = org.apache.lucene.search.IndexSearcher;
	using BytesRef = org.apache.lucene.util.BytesRef;


	/// <summary>
	/// Abstract parent class for <seealso cref="ValueSource"/> implementations that wrap multiple
	/// ValueSources and apply their own logic.
	/// </summary>
	public abstract class MultiFunction : ValueSource
	{
	  protected internal readonly IList<ValueSource> sources;

	  public MultiFunction(IList<ValueSource> sources)
	  {
		this.sources = sources;
	  }

	  protected internal abstract string name();

	  public override string description()
	  {
		return description(name(), sources);
	  }

	  public static string description(string name, IList<ValueSource> sources)
	  {
		StringBuilder sb = new StringBuilder();
		sb.Append(name).Append('(');
		bool firstTime = true;
		foreach (ValueSource source in sources)
		{
		  if (firstTime)
		  {
			firstTime = false;
		  }
		  else
		  {
			sb.Append(',');
		  }
		  sb.Append(source);
		}
		sb.Append(')');
		return sb.ToString();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static org.apache.lucene.queries.function.FunctionValues[] valsArr(java.util.List<org.apache.lucene.queries.function.ValueSource> sources, java.util.Map fcontext, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public static FunctionValues[] valsArr(IList<ValueSource> sources, IDictionary fcontext, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues[] valsArr = new org.apache.lucene.queries.function.FunctionValues[sources.size()];
		FunctionValues[] valsArr = new FunctionValues[sources.Count];
		int i = 0;
		foreach (ValueSource source in sources)
		{
		  valsArr[i++] = source.getValues(fcontext, readerContext);
		}
		return valsArr;
	  }

	  public class Values : FunctionValues
	  {
		  private readonly MultiFunction outerInstance;

		internal readonly FunctionValues[] valsArr;

		public Values(MultiFunction outerInstance, FunctionValues[] valsArr)
		{
			this.outerInstance = outerInstance;
		  this.valsArr = valsArr;
		}

		public override string ToString(int doc)
		{
		  return MultiFunction.ToString(outerInstance.name(), valsArr, doc);
		}

		public override ValueFiller ValueFiller
		{
			get
			{
			  // TODO: need ValueSource.type() to determine correct type
			  return base.ValueFiller;
			}
		}
	  }


	  public static string ToString(string name, FunctionValues[] valsArr, int doc)
	  {
		StringBuilder sb = new StringBuilder();
		sb.Append(name).Append('(');
		bool firstTime = true;
		foreach (FunctionValues vals in valsArr)
		{
		  if (firstTime)
		  {
			firstTime = false;
		  }
		  else
		  {
			sb.Append(',');
		  }
		  sb.Append(vals.ToString(doc));
		}
		sb.Append(')');
		return sb.ToString();
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
		MultiFunction other = (MultiFunction)o;
		return this.sources.Equals(other.sources);
	  }
	}


}