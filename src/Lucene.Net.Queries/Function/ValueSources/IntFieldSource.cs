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
using System;
using System.Collections;
using org.apache.lucene.queries.function;
using org.apache.lucene.queries.function.docvalues;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
	/// Obtains int field values from <seealso cref="FieldCache#getInts"/> and makes those
	/// values available as other numeric types, casting as needed.
	/// </summary>
	public class IntFieldSource : FieldCacheSource
	{
	  internal readonly FieldCache.IntParser parser;

	  public IntFieldSource(string field) : this(field, null)
	  {
	  }

	  public IntFieldSource(string field, FieldCache.IntParser parser) : base(field)
	  {
		this.parser = parser;
	  }

	  public override string description()
	  {
		return "int(" + field + ')';
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.FieldCache.Ints arr = cache.getInts(readerContext.reader(), field, parser, true);
		FieldCache.Ints arr = cache.getInts(readerContext.reader(), field, parser, true);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.Bits valid = cache.getDocsWithField(readerContext.reader(), field);
		Bits valid = cache.getDocsWithField(readerContext.reader(), field);

		return new IntDocValuesAnonymousInnerClassHelper(this, this, arr, valid);
	  }

	  private class IntDocValuesAnonymousInnerClassHelper : IntDocValues
	  {
		  private readonly IntFieldSource outerInstance;

		  private FieldCache.Ints arr;
		  private Bits valid;

		  public IntDocValuesAnonymousInnerClassHelper(IntFieldSource outerInstance, IntFieldSource this, FieldCache.Ints arr, Bits valid) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.arr = arr;
			  this.valid = valid;
			  val = new MutableValueInt();
		  }

		  internal readonly MutableValueInt val;

		  public override float floatVal(int doc)
		  {
			return (float)arr.get(doc);
		  }

		  public override int intVal(int doc)
		  {
			return arr.get(doc);
		  }

		  public override long longVal(int doc)
		  {
			return (long)arr.get(doc);
		  }

		  public override double doubleVal(int doc)
		  {
			return (double)arr.get(doc);
		  }

		  public override string strVal(int doc)
		  {
			return Convert.ToString(arr.get(doc));
		  }

		  public override object objectVal(int doc)
		  {
			return valid.get(doc) ? arr.get(doc) : null;
		  }

		  public override bool exists(int doc)
		  {
			return arr.get(doc) != 0 || valid.get(doc);
		  }

		  public override string ToString(int doc)
		  {
			return outerInstance.description() + '=' + intVal(doc);
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
			  private readonly IntDocValuesAnonymousInnerClassHelper outerInstance;

			  public ValueFillerAnonymousInnerClassHelper(IntDocValuesAnonymousInnerClassHelper outerInstance)
			  {
				  this.outerInstance = outerInstance;
				  mval = new MutableValueInt();
			  }

			  private readonly MutableValueInt mval;

			  public override MutableValue Value
			  {
				  get
				  {
					return mval;
				  }
			  }

			  public override void fillValue(int doc)
			  {
				mval.value = outerInstance.arr.get(doc);
				mval.exists = mval.value != 0 || outerInstance.valid.get(doc);
			  }
		  }


	  }

	  public override bool Equals(object o)
	  {
		if (o.GetType() != typeof(IntFieldSource))
		{
			return false;
		}
		IntFieldSource other = (IntFieldSource)o;
		return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
	  }

	  public override int GetHashCode()
	  {
		int h = parser == null ? typeof(int?).GetHashCode() : parser.GetType().GetHashCode();
		h += base.GetHashCode();
		return h;
	  }
	}

}