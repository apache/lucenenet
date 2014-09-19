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
	/// <code>ConstValueSource</code> returns a constant for all documents
	/// </summary>
	public class ConstValueSource : ConstNumberSource
	{
	  internal readonly float constant;
	  private readonly double dv;

	  public ConstValueSource(float constant)
	  {
		this.constant = constant;
		this.dv = constant;
	  }

	  public override string description()
	  {
		return "const(" + constant + ")";
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
		return new FloatDocValuesAnonymousInnerClassHelper(this, this);
	  }

	  private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
	  {
		  private readonly ConstValueSource outerInstance;

		  public FloatDocValuesAnonymousInnerClassHelper(ConstValueSource outerInstance, ConstValueSource this) : base(this)
		  {
			  this.outerInstance = outerInstance;
		  }

		  public override float floatVal(int doc)
		  {
			return outerInstance.constant;
		  }
		  public override int intVal(int doc)
		  {
			return (int)outerInstance.constant;
		  }
		  public override long longVal(int doc)
		  {
			return (long)outerInstance.constant;
		  }
		  public override double doubleVal(int doc)
		  {
			return outerInstance.dv;
		  }
		  public override string ToString(int doc)
		  {
			return outerInstance.description();
		  }
		  public override object objectVal(int doc)
		  {
			return outerInstance.constant;
		  }
		  public override bool boolVal(int doc)
		  {
			return outerInstance.constant != 0.0f;
		  }
	  }

	  public override int GetHashCode()
	  {
		return float.floatToIntBits(constant) * 31;
	  }

	  public override bool Equals(object o)
	  {
		if (!(o is ConstValueSource))
		{
			return false;
		}
		ConstValueSource other = (ConstValueSource)o;
		return this.constant == other.constant;
	  }

	  public override int Int
	  {
		  get
		  {
			return (int)constant;
		  }
	  }

	  public override long Long
	  {
		  get
		  {
			return (long)constant;
		  }
	  }

	  public override float Float
	  {
		  get
		  {
			return constant;
		  }
	  }

	  public override double Double
	  {
		  get
		  {
			return dv;
		  }
	  }

	  public override Number Number
	  {
		  get
		  {
			return constant;
		  }
	  }

	  public override bool Bool
	  {
		  get
		  {
			return constant != 0.0f;
		  }
	  }
	}

}