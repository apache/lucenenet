using System.Collections;

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
	using StrDocValues = org.apache.lucene.queries.function.docvalues.StrDocValues;
	using BytesRef = org.apache.lucene.util.BytesRef;



	/// <summary>
	/// Pass a the field value through as a String, no matter the type // Q: doesn't this mean it's a "string"?
	/// 
	/// 
	/// </summary>
	public class LiteralValueSource : ValueSource
	{
	  protected internal readonly string @string;
	  protected internal readonly BytesRef bytesRef;

	  public LiteralValueSource(string @string)
	  {
		this.@string = @string;
		this.bytesRef = new BytesRef(@string);
	  }

	  /// <summary>
	  /// returns the literal value </summary>
	  public virtual string Value
	  {
		  get
		  {
			return @string;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {

		return new StrDocValuesAnonymousInnerClassHelper(this, this);
	  }

	  private class StrDocValuesAnonymousInnerClassHelper : StrDocValues
	  {
		  private readonly LiteralValueSource outerInstance;

		  public StrDocValuesAnonymousInnerClassHelper(LiteralValueSource outerInstance, org.apache.lucene.queries.function.valuesource.LiteralValueSource this) : base(this)
		  {
			  this.outerInstance = outerInstance;
		  }

		  public override string strVal(int doc)
		  {
			return outerInstance.@string;
		  }

		  public override bool bytesVal(int doc, BytesRef target)
		  {
			target.copyBytes(outerInstance.bytesRef);
			return true;
		  }

		  public override string ToString(int doc)
		  {
			return outerInstance.@string;
		  }
	  }

	  public override string description()
	  {
		return "literal(" + @string + ")";
	  }

	  public override bool Equals(object o)
	  {
		if (this == o)
		{
			return true;
		}
		if (!(o is LiteralValueSource))
		{
			return false;
		}

		LiteralValueSource that = (LiteralValueSource) o;

		return @string.Equals(that.@string);

	  }

	  public static readonly int hash = typeof(LiteralValueSource).GetHashCode();
	  public override int GetHashCode()
	  {
		return hash + @string.GetHashCode();
	  }
	}

}