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
	using MutableValueStr = org.apache.lucene.util.mutable.MutableValueStr;

	/// <summary>
	/// Abstract <seealso cref="FunctionValues"/> implementation which supports retrieving String values.
	/// Implementations can control how the String values are loaded through <seealso cref="#strVal(int)"/>}
	/// </summary>
	public abstract class StrDocValues : FunctionValues
	{
	  protected internal readonly ValueSource vs;

	  public StrDocValues(ValueSource vs)
	  {
		this.vs = vs;
	  }

	  public override abstract string strVal(int doc);

	  public override object objectVal(int doc)
	  {
		return exists(doc) ? strVal(doc) : null;
	  }

	  public override bool boolVal(int doc)
	  {
		return exists(doc);
	  }

	  public override string ToString(int doc)
	  {
		return vs.description() + "='" + strVal(doc) + "'";
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
		  private readonly StrDocValues outerInstance;

		  public ValueFillerAnonymousInnerClassHelper(StrDocValues outerInstance)
		  {
			  this.outerInstance = outerInstance;
			  mval = new MutableValueStr();
		  }

		  private readonly MutableValueStr mval;

		  public override MutableValue Value
		  {
			  get
			  {
				return mval;
			  }
		  }

		  public override void fillValue(int doc)
		  {
			mval.exists = outerInstance.bytesVal(doc, mval.value);
		  }
	  }
	}

}