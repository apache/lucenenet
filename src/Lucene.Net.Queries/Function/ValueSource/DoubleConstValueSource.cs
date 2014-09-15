using System;
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
	using DoubleDocValues = org.apache.lucene.queries.function.docvalues.DoubleDocValues;


	/// <summary>
	/// Function that returns a constant double value for every document.
	/// </summary>
	public class DoubleConstValueSource : ConstNumberSource
	{
	  internal readonly double constant;
	  private readonly float fv;
	  private readonly long lv;

	  public DoubleConstValueSource(double constant)
	  {
		this.constant = constant;
		this.fv = (float)constant;
		this.lv = (long)constant;
	  }

	  public override string description()
	  {
		return "const(" + constant + ")";
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
		return new DoubleDocValuesAnonymousInnerClassHelper(this, this);
	  }

	  private class DoubleDocValuesAnonymousInnerClassHelper : DoubleDocValues
	  {
		  private readonly DoubleConstValueSource outerInstance;

		  public DoubleDocValuesAnonymousInnerClassHelper(DoubleConstValueSource outerInstance, org.apache.lucene.queries.function.valuesource.DoubleConstValueSource this) : base(this)
		  {
			  this.outerInstance = outerInstance;
		  }

		  public override float floatVal(int doc)
		  {
			return outerInstance.fv;
		  }

		  public override int intVal(int doc)
		  {
			return (int) outerInstance.lv;
		  }

		  public override long longVal(int doc)
		  {
			return outerInstance.lv;
		  }

		  public override double doubleVal(int doc)
		  {
			return outerInstance.constant;
		  }

		  public override string strVal(int doc)
		  {
			return Convert.ToString(outerInstance.constant);
		  }

		  public override object objectVal(int doc)
		  {
			return outerInstance.constant;
		  }

		  public override string ToString(int doc)
		  {
			return outerInstance.description();
		  }
	  }

	  public override int GetHashCode()
	  {
		long bits = double.doubleToRawLongBits(constant);
		return (int)(bits ^ ((long)((ulong)bits >> 32)));
	  }

	  public override bool Equals(object o)
	  {
		if (!(o is DoubleConstValueSource))
		{
			return false;
		}
		DoubleConstValueSource other = (DoubleConstValueSource) o;
		return this.constant == other.constant;
	  }

	  public override int Int
	  {
		  get
		  {
			return (int)lv;
		  }
	  }

	  public override long Long
	  {
		  get
		  {
			return lv;
		  }
	  }

	  public override float Float
	  {
		  get
		  {
			return fv;
		  }
	  }

	  public override double Double
	  {
		  get
		  {
			return constant;
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
			return constant != 0;
		  }
	  }
	}

}