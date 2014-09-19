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

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
	/// Depending on the boolean value of the <code>ifSource</code> function,
	/// returns the value of the <code>trueSource</code> or <code>falseSource</code> function.
	/// </summary>
	public class IfFunction : BoolFunction
	{
	  private readonly ValueSource ifSource;
	  private readonly ValueSource trueSource;
	  private readonly ValueSource falseSource;


	  public IfFunction(ValueSource ifSource, ValueSource trueSource, ValueSource falseSource)
	  {
		this.ifSource = ifSource;
		this.trueSource = trueSource;
		this.falseSource = falseSource;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues GetValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues ifVals = ifSource.GetValues(context, readerContext);
		FunctionValues ifVals = ifSource.GetValues(context, readerContext);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues trueVals = trueSource.GetValues(context, readerContext);
		FunctionValues trueVals = trueSource.GetValues(context, readerContext);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues falseVals = falseSource.GetValues(context, readerContext);
		FunctionValues falseVals = falseSource.GetValues(context, readerContext);

		return new FunctionValuesAnonymousInnerClassHelper(this, ifVals, trueVals, falseVals);

	  }

	  private class FunctionValuesAnonymousInnerClassHelper : FunctionValues
	  {
		  private readonly IfFunction outerInstance;

		  private FunctionValues ifVals;
		  private FunctionValues trueVals;
		  private FunctionValues falseVals;

		  public FunctionValuesAnonymousInnerClassHelper(IfFunction outerInstance, FunctionValues ifVals, FunctionValues trueVals, FunctionValues falseVals)
		  {
			  this.outerInstance = outerInstance;
			  this.ifVals = ifVals;
			  this.trueVals = trueVals;
			  this.falseVals = falseVals;
		  }

		  public override sbyte ByteVal(int doc)
		  {
			return ifVals.boolVal(doc) ? trueVals.ByteVal(doc) : falseVals.ByteVal(doc);
		  }

		  public override short ShortVal(int doc)
		  {
			return ifVals.boolVal(doc) ? trueVals.ShortVal(doc) : falseVals.ShortVal(doc);
		  }

		  public override float FloatVal(int doc)
		  {
			return ifVals.boolVal(doc) ? trueVals.FloatVal(doc) : falseVals.FloatVal(doc);
		  }

		  public override int intVal(int doc)
		  {
			return ifVals.boolVal(doc) ? trueVals.intVal(doc) : falseVals.intVal(doc);
		  }

		  public override long LongVal(int doc)
		  {
			return ifVals.boolVal(doc) ? trueVals.LongVal(doc) : falseVals.LongVal(doc);
		  }

		  public override double DoubleVal(int doc)
		  {
			return ifVals.boolVal(doc) ? trueVals.DoubleVal(doc) : falseVals.DoubleVal(doc);
		  }

		  public override string StrVal(int doc)
		  {
			return ifVals.boolVal(doc) ? trueVals.StrVal(doc) : falseVals.StrVal(doc);
		  }

		  public override bool boolVal(int doc)
		  {
			return ifVals.boolVal(doc) ? trueVals.boolVal(doc) : falseVals.boolVal(doc);
		  }

		  public override bool bytesVal(int doc, BytesRef target)
		  {
			return ifVals.boolVal(doc) ? trueVals.bytesVal(doc, target) : falseVals.bytesVal(doc, target);
		  }

		  public override object objectVal(int doc)
		  {
			return ifVals.boolVal(doc) ? trueVals.objectVal(doc) : falseVals.objectVal(doc);
		  }

		  public override bool exists(int doc)
		  {
			return true; // TODO: flow through to any sub-sources?
		  }

		  public override ValueFiller ValueFiller
		  {
			  get
			  {
				// TODO: we need types of trueSource / falseSource to handle this
				// for now, use float.
				return base.ValueFiller;
			  }
		  }

		  public override string ToString(int doc)
		  {
			return "if(" + ifVals.ToString(doc) + ',' + trueVals.ToString(doc) + ',' + falseVals.ToString(doc) + ')';
		  }
	  }

	  public override string description()
	  {
		return "if(" + ifSource.description() + ',' + trueSource.description() + ',' + falseSource + ')';
	  }

	  public override int GetHashCode()
	  {
		int h = ifSource.GetHashCode();
		h = h * 31 + trueSource.GetHashCode();
		h = h * 31 + falseSource.GetHashCode();
		return h;
	  }

	  public override bool Equals(object o)
	  {
		if (!(o is IfFunction))
		{
			return false;
		}
		IfFunction other = (IfFunction)o;
		return ifSource.Equals(other.ifSource) && trueSource.Equals(other.trueSource) && falseSource.Equals(other.falseSource);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void CreateWeight(java.util.Map context, org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public override void CreateWeight(IDictionary context, IndexSearcher searcher)
	  {
		ifSource.CreateWeight(context, searcher);
		trueSource.CreateWeight(context, searcher);
		falseSource.CreateWeight(context, searcher);
	  }
	}
}