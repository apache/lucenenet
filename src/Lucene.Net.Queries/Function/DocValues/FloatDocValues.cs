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
	using MutableValueFloat = org.apache.lucene.util.mutable.MutableValueFloat;

	/// <summary>
	/// Abstract <seealso cref="FunctionValues"/> implementation which supports retrieving float values.
	/// Implementations can control how the float values are loaded through <seealso cref="#floatVal(int)"/>}
	/// </summary>
	public abstract class FloatDocValues : FunctionValues
	{
	  protected internal readonly ValueSource vs;

	  public FloatDocValues(ValueSource vs)
	  {
		this.vs = vs;
	  }

	  public override sbyte byteVal(int doc)
	  {
		return (sbyte)floatVal(doc);
	  }

	  public override short shortVal(int doc)
	  {
		return (short)floatVal(doc);
	  }

	  public override abstract float floatVal(int doc);

	  public override int intVal(int doc)
	  {
		return (int)floatVal(doc);
	  }

	  public override long longVal(int doc)
	  {
		return (long)floatVal(doc);
	  }

	  public override double doubleVal(int doc)
	  {
		return (double)floatVal(doc);
	  }

	  public override string strVal(int doc)
	  {
		return Convert.ToString(floatVal(doc));
	  }

	  public override object objectVal(int doc)
	  {
		return exists(doc) ? floatVal(doc) : null;
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
		  private readonly FloatDocValues outerInstance;

		  public ValueFillerAnonymousInnerClassHelper(FloatDocValues outerInstance)
		  {
			  this.outerInstance = outerInstance;
			  mval = new MutableValueFloat();
		  }

		  private readonly MutableValueFloat mval;

		  public override MutableValue Value
		  {
			  get
			  {
				return mval;
			  }
		  }

		  public override void fillValue(int doc)
		  {
			mval.value = outerInstance.floatVal(doc);
			mval.exists = outerInstance.exists(doc);
		  }
	  }
	}

}