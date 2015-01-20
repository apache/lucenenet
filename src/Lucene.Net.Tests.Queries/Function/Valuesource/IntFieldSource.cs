/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
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
	/// and makes those
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

		public override string Description()
		{
			return "int(" + field + ')';
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FieldCache.Ints arr = cache.GetInts(((AtomicReader)readerContext.Reader()), field
				, parser, true);
			Bits valid = cache.GetDocsWithField(((AtomicReader)readerContext.Reader()), field
				);
			return new _IntDocValues_60(this, arr, valid, this);
		}

		private sealed class _IntDocValues_60 : IntDocValues
		{
			public _IntDocValues_60(IntFieldSource _enclosing, FieldCache.Ints arr, Bits valid
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
				return Sharpen.Extensions.ToString(arr.Get(doc));
			}

			public override object ObjectVal(int doc)
			{
				return valid.Get(doc) ? arr.Get(doc) : null;
			}

			public override bool Exists(int doc)
			{
				return arr.Get(doc) != 0 || valid.Get(doc);
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Description() + '=' + this.IntVal(doc);
			}

			public override FunctionValues.ValueFiller GetValueFiller()
			{
				return new _ValueFiller_105(arr, valid);
			}

			private sealed class _ValueFiller_105 : FunctionValues.ValueFiller
			{
				public _ValueFiller_105(FieldCache.Ints arr, Bits valid)
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
					this.mval.exists = this.mval.value != 0 || valid.Get(doc);
				}

				private readonly FieldCache.Ints arr;

				private readonly Bits valid;
			}

			private readonly IntFieldSource _enclosing;

			private readonly FieldCache.Ints arr;

			private readonly Bits valid;
		}

		public override bool Equals(object o)
		{
			if (o.GetType() != typeof(Org.Apache.Lucene.Queries.Function.Valuesource.IntFieldSource
				))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.IntFieldSource other = (Org.Apache.Lucene.Queries.Function.Valuesource.IntFieldSource
				)o;
			return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser
				.GetType() == other.parser.GetType());
		}

		public override int GetHashCode()
		{
			int h = parser == null ? typeof(int).GetHashCode() : parser.GetType().GetHashCode
				();
			h += base.GetHashCode();
			return h;
		}
	}
}
