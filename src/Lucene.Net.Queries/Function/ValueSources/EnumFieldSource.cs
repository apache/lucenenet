using System;
using System.Collections;
using System.Collections.Generic;
using org.apache.lucene.queries.function;
using org.apache.lucene.queries.function.docvalues;

namespace Lucene.Net.Queries.Function.ValueSources
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
    /// <summary>
	/// Obtains int field values from <seealso cref="FieldCache#getInts"/> and makes
	/// those values available as other numeric types, casting as needed.
	/// strVal of the value is not the int value, but its string (displayed) value
	/// </summary>
	public class EnumFieldSource : FieldCacheSource
	{
	  internal const int? DEFAULT_VALUE = -1;

	  internal readonly FieldCache.IntParser parser;
	  internal readonly IDictionary<int?, string> enumIntToStringMap;
	  internal readonly IDictionary<string, int?> enumStringToIntMap;

	  public EnumFieldSource(string field, FieldCache.IntParser parser, IDictionary<int?, string> enumIntToStringMap, IDictionary<string, int?> enumStringToIntMap) : base(field)
	  {
		this.parser = parser;
		this.enumIntToStringMap = enumIntToStringMap;
		this.enumStringToIntMap = enumStringToIntMap;
	  }

	  private static int? tryParseInt(string valueStr)
	  {
		int? intValue = null;
		try
		{
		  intValue = Convert.ToInt32(valueStr);
		}
		catch (NumberFormatException)
		{
		}
		return intValue;
	  }

	  private string intValueToStringValue(int? intVal)
	  {
		if (intVal == null)
		{
		  return null;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String enumString = enumIntToStringMap.get(intVal);
		string enumString = enumIntToStringMap[intVal];
		if (enumString != null)
		{
		  return enumString;
		}
		// can't find matching enum name - return DEFAULT_VALUE.toString()
		return DEFAULT_VALUE.ToString();
	  }

	  private int? stringValueToIntValue(string stringVal)
	  {
		if (stringVal == null)
		{
		  return null;
		}

		int? intValue;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Integer enumInt = enumStringToIntMap.get(stringVal);
		int? enumInt = enumStringToIntMap[stringVal];
		if (enumInt != null) //enum int found for string
		{
		  return enumInt;
		}

		//enum int not found for string
		intValue = tryParseInt(stringVal);
		if (intValue == null) //not Integer
		{
		  intValue = DEFAULT_VALUE;
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String enumString = enumIntToStringMap.get(intValue);
		string enumString = enumIntToStringMap[intValue];
		if (enumString != null) //has matching string
		{
		  return intValue;
		}

		return DEFAULT_VALUE;
	  }

	  public override string description()
	  {
		return "enum(" + field + ')';
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
		  private readonly EnumFieldSource outerInstance;

		  private FieldCache.Ints arr;
		  private Bits valid;

		  public IntDocValuesAnonymousInnerClassHelper(EnumFieldSource outerInstance, EnumFieldSource this, FieldCache.Ints arr, Bits valid) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.arr = arr;
			  this.valid = valid;
			  val = new MutableValueInt();
		  }

		  internal readonly MutableValueInt val;

		  public override float floatVal(int doc)
		  {
			return (float) arr.get(doc);
		  }

		  public override int intVal(int doc)
		  {
			return arr.get(doc);
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
			int? intValue = arr.get(doc);
			return outerInstance.intValueToStringValue(intValue);
		  }

		  public override object objectVal(int doc)
		  {
			return valid.get(doc) ? arr.get(doc) : null;
		  }

		  public override bool exists(int doc)
		  {
			return valid.get(doc);
		  }

		  public override string ToString(int doc)
		  {
			return outerInstance.description() + '=' + strVal(doc);
		  }


		  public override ValueSourceScorer getRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
		  {
			int? lower = outerInstance.stringValueToIntValue(lowerVal);
			int? upper = outerInstance.stringValueToIntValue(upperVal);

			// instead of using separate comparison functions, adjust the endpoints.

			if (lower == null)
			{
			  lower = int.MinValue;
			}
			else
			{
			  if (!includeLower && lower < int.MaxValue)
			  {
				  lower++;
			  }
			}

			if (upper == null)
			{
			  upper = int.MaxValue;
			}
			else
			{
			  if (!includeUpper && upper > int.MinValue)
			  {
				  upper--;
			  }
			}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ll = lower;
			int ll = lower.Value;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int uu = upper;
			int uu = upper.Value;

			return new ValueSourceScorerAnonymousInnerClassHelper(this, reader, this, ll, uu);
		  }

		  private class ValueSourceScorerAnonymousInnerClassHelper : ValueSourceScorer
		  {
			  private readonly IntDocValuesAnonymousInnerClassHelper outerInstance;

			  private int ll;
			  private int uu;

			  public ValueSourceScorerAnonymousInnerClassHelper(IntDocValuesAnonymousInnerClassHelper outerInstance, IndexReader reader, EnumFieldSource this, int ll, int uu) : base(reader, this)
			  {
				  this.outerInstance = outerInstance;
				  this.ll = ll;
				  this.uu = uu;
			  }

			  public override bool matchesValue(int doc)
			  {
				int val = outerInstance.arr.get(doc);
				// only check for deleted if it's the default value
				// if (val==0 && reader.isDeleted(doc)) return false;
				return val >= ll && val <= uu;
			  }
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
				mval.exists = outerInstance.valid.get(doc);
			  }
		  }


	  }

	  public override bool Equals(object o)
	  {
		if (this == o)
		{
			return true;
		}
		if (o == null || this.GetType() != o.GetType())
		{
			return false;
		}
		if (!base.Equals(o))
		{
			return false;
		}

		EnumFieldSource that = (EnumFieldSource) o;

		if (!enumIntToStringMap.Equals(that.enumIntToStringMap))
		{
			return false;
		}
		if (!enumStringToIntMap.Equals(that.enumStringToIntMap))
		{
			return false;
		}
		if (!parser.Equals(that.parser))
		{
			return false;
		}

		return true;
	  }

	  public override int GetHashCode()
	  {
		int result = base.GetHashCode();
		result = 31 * result + parser.GetHashCode();
		result = 31 * result + enumIntToStringMap.GetHashCode();
		result = 31 * result + enumStringToIntMap.GetHashCode();
		return result;
	  }
	}


}