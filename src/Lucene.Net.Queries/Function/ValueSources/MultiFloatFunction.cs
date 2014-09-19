using System.Collections;
using System.Text;
using org.apache.lucene.queries.function;
using org.apache.lucene.queries.function.docvalues;

namespace Lucene.Net.Queries.Function.ValueSources
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
    /// <summary>
	/// Abstract <seealso cref="ValueSource"/> implementation which wraps multiple ValueSources
	/// and applies an extendible float function to their values.
	/// 
	/// </summary>
	public abstract class MultiFloatFunction : ValueSource
	{
	  protected internal readonly ValueSource[] sources;

	  public MultiFloatFunction(ValueSource[] sources)
	  {
		this.sources = sources;
	  }

	  protected internal abstract string name();
	  protected internal abstract float func(int doc, FunctionValues[] valsArr);

	  public override string description()
	  {
		StringBuilder sb = new StringBuilder();
		sb.Append(name()).Append('(');
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
		  sb.Append((object) source);
		}
		sb.Append(')');
		return sb.ToString();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues[] valsArr = new org.apache.lucene.queries.function.FunctionValues[sources.length];
		FunctionValues[] valsArr = new FunctionValues[sources.Length];
		for (int i = 0; i < sources.Length; i++)
		{
		  valsArr[i] = sources[i].getValues(context, readerContext);
		}

		return new FloatDocValuesAnonymousInnerClassHelper(this, this, valsArr);
	  }

	  private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
	  {
		  private readonly MultiFloatFunction outerInstance;

		  private FunctionValues[] valsArr;

		  public FloatDocValuesAnonymousInnerClassHelper(MultiFloatFunction outerInstance, MultiFloatFunction this, FunctionValues[] valsArr) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.valsArr = valsArr;
		  }

		  public override float floatVal(int doc)
		  {
			return outerInstance.func(doc, valsArr);
		  }
		   public override string ToString(int doc)
		   {
			StringBuilder sb = new StringBuilder();
			sb.Append(outerInstance.name()).Append('(');
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
		return Arrays.GetHashCode(sources) + name().GetHashCode();
	  }

	  public override bool Equals(object o)
	  {
		if (this.GetType() != o.GetType())
		{
			return false;
		}
		MultiFloatFunction other = (MultiFloatFunction)o;
		return this.name().Equals(other.name()) && Arrays.Equals(this.sources, other.sources);
	  }
	}

}