/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Obtains int field values from
	/// <see cref="Org.Apache.Lucene.Search.FieldCache.GetInts(Org.Apache.Lucene.Index.AtomicReader, string, bool)
	/// 	">Org.Apache.Lucene.Search.FieldCache.GetInts(Org.Apache.Lucene.Index.AtomicReader, string, bool)
	/// 	</see>
	/// and makes
	/// those values available as other numeric types, casting as needed.
	/// strVal of the value is not the int value, but its string (displayed) value
	/// </summary>
	public class EnumFieldSource : FieldCacheSource
	{
		internal static readonly int DEFAULT_VALUE = -1;

		internal readonly FieldCache.IntParser parser;

		internal readonly IDictionary<int, string> enumIntToStringMap;

		internal readonly IDictionary<string, int> enumStringToIntMap;

		public EnumFieldSource(string field, FieldCache.IntParser parser, IDictionary<int
			, string> enumIntToStringMap, IDictionary<string, int> enumStringToIntMap) : base
			(field)
		{
			this.parser = parser;
			this.enumIntToStringMap = enumIntToStringMap;
			this.enumStringToIntMap = enumStringToIntMap;
		}

		private static int TryParseInt(string valueStr)
		{
			int intValue = null;
			try
			{
				intValue = System.Convert.ToInt32(valueStr);
			}
			catch (FormatException)
			{
			}
			return intValue;
		}

		private string IntValueToStringValue(int intVal)
		{
			if (intVal == null)
			{
				return null;
			}
			string enumString = enumIntToStringMap.Get(intVal);
			if (enumString != null)
			{
				return enumString;
			}
			// can't find matching enum name - return DEFAULT_VALUE.toString()
			return DEFAULT_VALUE.ToString();
		}

		private int StringValueToIntValue(string stringVal)
		{
			if (stringVal == null)
			{
				return null;
			}
			int intValue;
			int enumInt = enumStringToIntMap.Get(stringVal);
			if (enumInt != null)
			{
				//enum int found for string
				return enumInt;
			}
			//enum int not found for string
			intValue = TryParseInt(stringVal);
			if (intValue == null)
			{
				//not Integer
				intValue = DEFAULT_VALUE;
			}
			string enumString = enumIntToStringMap.Get(intValue);
			if (enumString != null)
			{
				//has matching string
				return intValue;
			}
			return DEFAULT_VALUE;
		}

		public override string Description()
		{
			return "enum(" + field + ')';
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FieldCache.Ints arr = cache.GetInts(((AtomicReader)readerContext.Reader()), field
				, parser, true);
			Bits valid = cache.GetDocsWithField(((AtomicReader)readerContext.Reader()), field
				);
			return new _IntDocValues_104(this, arr, valid, this);
		}

		private sealed class _IntDocValues_104 : IntDocValues
		{
			public _IntDocValues_104(EnumFieldSource _enclosing, FieldCache.Ints arr, Bits valid
				, ValueSource baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.arr = arr;
				this.valid = valid;
				this.val = new MutableValueInt();
			}

			internal readonly MutableValueInt val;

			public override float FloatVal(int doc)
			{
				return (float)arr.Get(doc);
			}

			public override int IntVal(int doc)
			{
				return arr.Get(doc);
			}

			public override long LongVal(int doc)
			{
				return (long)arr.Get(doc);
			}

			public override double DoubleVal(int doc)
			{
				return (double)arr.Get(doc);
			}

			public override string StrVal(int doc)
			{
				int intValue = arr.Get(doc);
				return this._enclosing.IntValueToStringValue(intValue);
			}

			public override object ObjectVal(int doc)
			{
				return valid.Get(doc) ? arr.Get(doc) : null;
			}

			public override bool Exists(int doc)
			{
				return valid.Get(doc);
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Description() + '=' + this.StrVal(doc);
			}

			public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal
				, string upperVal, bool includeLower, bool includeUpper)
			{
				int lower = this._enclosing.StringValueToIntValue(lowerVal);
				int upper = this._enclosing.StringValueToIntValue(upperVal);
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
				int ll = lower;
				int uu = upper;
				return new _ValueSourceScorer_171(arr, ll, uu, reader, this);
			}

			private sealed class _ValueSourceScorer_171 : ValueSourceScorer
			{
				public _ValueSourceScorer_171(FieldCache.Ints arr, int ll, int uu, IndexReader baseArg1
					, FunctionValues baseArg2) : base(baseArg1, baseArg2)
				{
					this.arr = arr;
					this.ll = ll;
					this.uu = uu;
				}

				public override bool MatchesValue(int doc)
				{
					int val = arr.Get(doc);
					// only check for deleted if it's the default value
					// if (val==0 && reader.isDeleted(doc)) return false;
					return val >= ll && val <= uu;
				}

				private readonly FieldCache.Ints arr;

				private readonly int ll;

				private readonly int uu;
			}

			public override FunctionValues.ValueFiller GetValueFiller()
			{
				return new _ValueFiller_184(arr, valid);
			}

			private sealed class _ValueFiller_184 : FunctionValues.ValueFiller
			{
				public _ValueFiller_184(FieldCache.Ints arr, Bits valid)
				{
					this.arr = arr;
					this.valid = valid;
					this.mval = new MutableValueInt();
				}

				private readonly MutableValueInt mval;

				public override MutableValue GetValue()
				{
					return this.mval;
				}

				public override void FillValue(int doc)
				{
					this.mval.value = arr.Get(doc);
					this.mval.exists = valid.Get(doc);
				}

				private readonly FieldCache.Ints arr;

				private readonly Bits valid;
			}

			private readonly EnumFieldSource _enclosing;

			private readonly FieldCache.Ints arr;

			private readonly Bits valid;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o == null || GetType() != o.GetType())
			{
				return false;
			}
			if (!base.Equals(o))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.EnumFieldSource that = (Org.Apache.Lucene.Queries.Function.Valuesource.EnumFieldSource
				)o;
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
