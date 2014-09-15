using System.Collections;
using System.Collections.Generic;

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
	/// <seealso cref="ValueSource"/> implementation which only returns the values from the provided
	/// ValueSources which are available for a particular docId.  Consequently, when combined
	/// with a <seealso cref="ConstValueSource"/>, this function serves as a way to return a default
	/// value when the values for a field are unavailable.
	/// </summary>
	public class DefFunction : MultiFunction
	{
	  public DefFunction(IList<ValueSource> sources) : base(sources)
	  {
	  }

	  protected internal override string name()
	  {
		return "def";
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map fcontext, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary fcontext, AtomicReaderContext readerContext)
	  {


		return new ValuesAnonymousInnerClassHelper(this, valsArr(sources, fcontext, readerContext));
	  }

	  private class ValuesAnonymousInnerClassHelper : Values
	  {
		  private readonly DefFunction outerInstance;

		  public ValuesAnonymousInnerClassHelper(DefFunction outerInstance, FunctionValues[] valsArr) : base(outerInstance, valsArr)
		  {
			  this.outerInstance = outerInstance;
			  upto = valsArr.Length - 1;
		  }

		  internal readonly int upto;

		  private FunctionValues get(int doc)
		  {
			for (int i = 0; i < upto; i++)
			{
			  FunctionValues vals = valsArr[i];
			  if (vals.exists(doc))
			  {
				return vals;
			  }
			}
			return valsArr[upto];
		  }

		  public override sbyte byteVal(int doc)
		  {
			return get(doc).byteVal(doc);
		  }

		  public override short shortVal(int doc)
		  {
			return get(doc).shortVal(doc);
		  }

		  public override float floatVal(int doc)
		  {
			return get(doc).floatVal(doc);
		  }

		  public override int intVal(int doc)
		  {
			return get(doc).intVal(doc);
		  }

		  public override long longVal(int doc)
		  {
			return get(doc).longVal(doc);
		  }

		  public override double doubleVal(int doc)
		  {
			return get(doc).doubleVal(doc);
		  }

		  public override string strVal(int doc)
		  {
			return get(doc).strVal(doc);
		  }

		  public override bool boolVal(int doc)
		  {
			return get(doc).boolVal(doc);
		  }

		  public override bool bytesVal(int doc, BytesRef target)
		  {
			return get(doc).bytesVal(doc, target);
		  }

		  public override object objectVal(int doc)
		  {
			return get(doc).objectVal(doc);
		  }

		  public override bool exists(int doc)
		  {
			// return true if any source is exists?
			foreach (FunctionValues vals in valsArr)
			{
			  if (vals.exists(doc))
			  {
				return true;
			  }
			}
			return false;
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
	}
}