using System;

namespace org.apache.lucene.queries.function.docvalues
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

	using MutableValue = org.apache.lucene.util.mutable.MutableValue;
	using MutableValueBool = org.apache.lucene.util.mutable.MutableValueBool;

	/// <summary>
	/// Abstract <seealso cref="FunctionValues"/> implementation which supports retrieving boolean values.
	/// Implementations can control how the boolean values are loaded through <seealso cref="#boolVal(int)"/>}
	/// </summary>
	public abstract class BoolDocValues : FunctionValues
	{
	  protected internal readonly ValueSource vs;

	  public BoolDocValues(ValueSource vs)
	  {
		this.vs = vs;
	  }

	  public override abstract bool boolVal(int doc);

	  public override sbyte byteVal(int doc)
	  {
		return boolVal(doc) ? (sbyte)1 : (sbyte)0;
	  }

	  public override short shortVal(int doc)
	  {
		return boolVal(doc) ? (short)1 : (short)0;
	  }

	  public override float floatVal(int doc)
	  {
		return boolVal(doc) ? (float)1 : (float)0;
	  }

	  public override int intVal(int doc)
	  {
		return boolVal(doc) ? 1 : 0;
	  }

	  public override long longVal(int doc)
	  {
		return boolVal(doc) ? (long)1 : (long)0;
	  }

	  public override double doubleVal(int doc)
	  {
		return boolVal(doc) ? (double)1 : (double)0;
	  }

	  public override string strVal(int doc)
	  {
		return Convert.ToString(boolVal(doc));
	  }

	  public override object objectVal(int doc)
	  {
		return exists(doc) ? boolVal(doc) : null;
	  }

	  public override string ToString(int doc)
	  {
		return vs.description() + '=' + strVal(doc);
	  }

	  public override ValueFiller ValueFiller
	  {
		  get
		  {
			return new ValueFillerAnonymousInnerClassHelper(this);
		  }
	  }

	  private class ValueFillerAnonymousInnerClassHelper : ValueFiller
	  {
		  private readonly BoolDocValues outerInstance;

		  public ValueFillerAnonymousInnerClassHelper(BoolDocValues outerInstance)
		  {
			  this.outerInstance = outerInstance;
			  mval = new MutableValueBool();
		  }

		  private readonly MutableValueBool mval;

		  public override MutableValue Value
		  {
			  get
			  {
				return mval;
			  }
		  }

		  public override void fillValue(int doc)
		  {
			mval.value = outerInstance.boolVal(doc);
			mval.exists = outerInstance.exists(doc);
		  }
	  }
	}

}