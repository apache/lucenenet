using System;
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
	using FieldCache = org.apache.lucene.search.FieldCache;

	/// <summary>
	/// Obtains int field values from the <seealso cref="org.apache.lucene.search.FieldCache"/>
	/// using <code>getInts()</code>
	/// and makes those values available as other numeric types, casting as needed. *
	/// 
	/// 
	/// </summary>
	[Obsolete]
	public class ByteFieldSource : FieldCacheSource
	{

	  private readonly FieldCache.ByteParser parser;

	  public ByteFieldSource(string field) : this(field, null)
	  {
	  }

	  public ByteFieldSource(string field, FieldCache.ByteParser parser) : base(field)
	  {
		this.parser = parser;
	  }

	  public override string description()
	  {
		return "byte(" + field + ')';
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.FieldCache.Bytes arr = cache.getBytes(readerContext.reader(), field, parser, false);
		FieldCache.Bytes arr = cache.getBytes(readerContext.reader(), field, parser, false);

		return new FunctionValuesAnonymousInnerClassHelper(this, arr);
	  }

	  private class FunctionValuesAnonymousInnerClassHelper : FunctionValues
	  {
		  private readonly ByteFieldSource outerInstance;

		  private FieldCache.Bytes arr;

		  public FunctionValuesAnonymousInnerClassHelper(ByteFieldSource outerInstance, FieldCache.Bytes arr)
		  {
			  this.outerInstance = outerInstance;
			  this.arr = arr;
		  }

		  public override sbyte byteVal(int doc)
		  {
			return arr.get(doc);
		  }

		  public override short shortVal(int doc)
		  {
			return (short) arr.get(doc);
		  }

		  public override float floatVal(int doc)
		  {
			return (float) arr.get(doc);
		  }

		  public override int intVal(int doc)
		  {
			return (int) arr.get(doc);
		  }

		  public override long longVal(int doc)
		  {
			return (long) arr.get(doc);
		  }

		  public override double doubleVal(int doc)
		  {
			return (double) arr.get(doc);
		  }

		  public override string strVal(int doc)
		  {
			return Convert.ToString(arr.get(doc));
		  }

		  public override string ToString(int doc)
		  {
			return outerInstance.description() + '=' + byteVal(doc);
		  }

		  public override object objectVal(int doc)
		  {
			return arr.get(doc); // TODO: valid?
		  }

	  }

	  public override bool Equals(object o)
	  {
		if (o.GetType() != typeof(ByteFieldSource))
		{
			return false;
		}
		ByteFieldSource other = (ByteFieldSource) o;
		return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
	  }

	  public override int GetHashCode()
	  {
		int h = parser == null ? typeof(sbyte?).GetHashCode() : parser.GetType().GetHashCode();
		h += base.GetHashCode();
		return h;
	  }
	}

}