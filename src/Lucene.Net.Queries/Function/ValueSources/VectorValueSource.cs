using System.Collections;
using System.Collections.Generic;
using System.Text;
using org.apache.lucene.queries.function;

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
	/// Converts individual ValueSource instances to leverage the FunctionValues *Val functions that work with multiple values,
	/// i.e. <seealso cref="org.apache.lucene.queries.function.FunctionValues#doubleVal(int, double[])"/>
	/// </summary>
	//Not crazy about the name, but...
	public class VectorValueSource : MultiValueSource
	{
	  protected internal readonly IList<ValueSource> sources;


	  public VectorValueSource(IList<ValueSource> sources)
	  {
		this.sources = sources;
	  }

	  public virtual IList<ValueSource> Sources
	  {
		  get
		  {
			return sources;
		  }
	  }

	  public override int dimension()
	  {
		return sources.Count;
	  }

	  public virtual string name()
	  {
		return "vector";
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
		int size = sources.Count;

		// special-case x,y and lat,lon since it's so common
		if (size == 2)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues x = sources.get(0).getValues(context, readerContext);
		  FunctionValues x = sources[0].getValues(context, readerContext);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues y = sources.get(1).getValues(context, readerContext);
		  FunctionValues y = sources[1].getValues(context, readerContext);
		  return new FunctionValuesAnonymousInnerClassHelper(this, x, y);
		}


//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues[] valsArr = new org.apache.lucene.queries.function.FunctionValues[size];
		FunctionValues[] valsArr = new FunctionValues[size];
		for (int i = 0; i < size; i++)
		{
		  valsArr[i] = sources[i].getValues(context, readerContext);
		}

		return new FunctionValuesAnonymousInnerClassHelper2(this, valsArr);
	  }

	  private class FunctionValuesAnonymousInnerClassHelper : FunctionValues
	  {
		  private readonly VectorValueSource outerInstance;

		  private FunctionValues x;
		  private FunctionValues y;

		  public FunctionValuesAnonymousInnerClassHelper(VectorValueSource outerInstance, FunctionValues x, FunctionValues y)
		  {
			  this.outerInstance = outerInstance;
			  this.x = x;
			  this.y = y;
		  }

		  public override void byteVal(int doc, sbyte[] vals)
		  {
			vals[0] = x.byteVal(doc);
			vals[1] = y.byteVal(doc);
		  }

		  public override void shortVal(int doc, short[] vals)
		  {
			vals[0] = x.shortVal(doc);
			vals[1] = y.shortVal(doc);
		  }
		  public override void intVal(int doc, int[] vals)
		  {
			vals[0] = x.intVal(doc);
			vals[1] = y.intVal(doc);
		  }
		  public override void longVal(int doc, long[] vals)
		  {
			vals[0] = x.longVal(doc);
			vals[1] = y.longVal(doc);
		  }
		  public override void floatVal(int doc, float[] vals)
		  {
			vals[0] = x.floatVal(doc);
			vals[1] = y.floatVal(doc);
		  }
		  public override void doubleVal(int doc, double[] vals)
		  {
			vals[0] = x.doubleVal(doc);
			vals[1] = y.doubleVal(doc);
		  }
		  public override void strVal(int doc, string[] vals)
		  {
			vals[0] = x.strVal(doc);
			vals[1] = y.strVal(doc);
		  }
		  public override string ToString(int doc)
		  {
			return outerInstance.name() + "(" + x.ToString(doc) + "," + y.ToString(doc) + ")";
		  }
	  }

	  private class FunctionValuesAnonymousInnerClassHelper2 : FunctionValues
	  {
		  private readonly VectorValueSource outerInstance;

		  private FunctionValues[] valsArr;

		  public FunctionValuesAnonymousInnerClassHelper2(VectorValueSource outerInstance, FunctionValues[] valsArr)
		  {
			  this.outerInstance = outerInstance;
			  this.valsArr = valsArr;
		  }

		  public override void byteVal(int doc, sbyte[] vals)
		  {
			for (int i = 0; i < valsArr.Length; i++)
			{
			  vals[i] = valsArr[i].byteVal(doc);
			}
		  }

		  public override void shortVal(int doc, short[] vals)
		  {
			for (int i = 0; i < valsArr.Length; i++)
			{
			  vals[i] = valsArr[i].shortVal(doc);
			}
		  }

		  public override void floatVal(int doc, float[] vals)
		  {
			for (int i = 0; i < valsArr.Length; i++)
			{
			  vals[i] = valsArr[i].floatVal(doc);
			}
		  }

		  public override void intVal(int doc, int[] vals)
		  {
			for (int i = 0; i < valsArr.Length; i++)
			{
			  vals[i] = valsArr[i].intVal(doc);
			}
		  }

		  public override void longVal(int doc, long[] vals)
		  {
			for (int i = 0; i < valsArr.Length; i++)
			{
			  vals[i] = valsArr[i].longVal(doc);
			}
		  }

		  public override void doubleVal(int doc, double[] vals)
		  {
			for (int i = 0; i < valsArr.Length; i++)
			{
			  vals[i] = valsArr[i].doubleVal(doc);
			}
		  }

		  public override void strVal(int doc, string[] vals)
		  {
			for (int i = 0; i < valsArr.Length; i++)
			{
			  vals[i] = valsArr[i].strVal(doc);
			}
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
		  sb.Append(source);
		}
		sb.Append(")");
		return sb.ToString();
	  }

	  public override bool Equals(object o)
	  {
		if (this == o)
		{
			return true;
		}
		if (!(o is VectorValueSource))
		{
			return false;
		}

		VectorValueSource that = (VectorValueSource) o;

		return sources.Equals(that.sources);

	  }

	  public override int GetHashCode()
	  {
		return sources.GetHashCode();
	  }
	}

}