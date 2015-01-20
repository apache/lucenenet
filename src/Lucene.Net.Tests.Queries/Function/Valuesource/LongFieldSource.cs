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
	/// Obtains long field values from
	/// <see cref="Org.Apache.Lucene.Search.FieldCache.GetLongs(Org.Apache.Lucene.Index.AtomicReader, string, bool)
	/// 	">Org.Apache.Lucene.Search.FieldCache.GetLongs(Org.Apache.Lucene.Index.AtomicReader, string, bool)
	/// 	</see>
	/// and makes those
	/// values available as other numeric types, casting as needed.
	/// </summary>
	public class LongFieldSource : FieldCacheSource
	{
		protected internal readonly FieldCache.LongParser parser;

		public LongFieldSource(string field) : this(field, null)
		{
		}

		public LongFieldSource(string field, FieldCache.LongParser parser) : base(field)
		{
			this.parser = parser;
		}

		public override string Description()
		{
			return "long(" + field + ')';
		}

		public virtual long ExternalToLong(string extVal)
		{
			return long.Parse(extVal);
		}

		public virtual object LongToObject(long val)
		{
			return val;
		}

		public virtual string LongToString(long val)
		{
			return LongToObject(val).ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FieldCache.Longs arr = cache.GetLongs(((AtomicReader)readerContext.Reader()), field
				, parser, true);
			Bits valid = cache.GetDocsWithField(((AtomicReader)readerContext.Reader()), field
				);
			return new _LongDocValues_72(this, arr, valid, this);
		}

		private sealed class _LongDocValues_72 : LongDocValues
		{
			public _LongDocValues_72(LongFieldSource _enclosing, FieldCache.Longs arr, Bits valid
				, ValueSource baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.arr = arr;
				this.valid = valid;
			}

			public override long LongVal(int doc)
			{
				return arr.Get(doc);
			}

			public override bool Exists(int doc)
			{
				return arr.Get(doc) != 0 || valid.Get(doc);
			}

			public override object ObjectVal(int doc)
			{
				return valid.Get(doc) ? this._enclosing.LongToObject(arr.Get(doc)) : null;
			}

			public override string StrVal(int doc)
			{
				return valid.Get(doc) ? this._enclosing.LongToString(arr.Get(doc)) : null;
			}

			protected internal override long ExternalToLong(string extVal)
			{
				return this._enclosing.ExternalToLong(extVal);
			}

			public override FunctionValues.ValueFiller GetValueFiller()
			{
				return new _ValueFiller_100(this, arr, valid);
			}

			private sealed class _ValueFiller_100 : FunctionValues.ValueFiller
			{
				public _ValueFiller_100(FieldCache.Longs arr, Bits valid)
				{
					this.arr = arr;
					this.valid = valid;
					this.mval = this._enclosing._enclosing.NewMutableValueLong();
				}

				private readonly MutableValueLong mval;

				public override MutableValue GetValue()
				{
					return this.mval;
				}

				public override void FillValue(int doc)
				{
					this.mval.value = arr.Get(doc);
					this.mval.exists = this.mval.value != 0 || valid.Get(doc);
				}

				private readonly FieldCache.Longs arr;

				private readonly Bits valid;
			}

			private readonly LongFieldSource _enclosing;

			private readonly FieldCache.Longs arr;

			private readonly Bits valid;
		}

		protected internal virtual MutableValueLong NewMutableValueLong()
		{
			return new MutableValueLong();
		}

		public override bool Equals(object o)
		{
			if (o.GetType() != this.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.LongFieldSource other = (Org.Apache.Lucene.Queries.Function.Valuesource.LongFieldSource
				)o;
			return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser
				.GetType() == other.parser.GetType());
		}

		public override int GetHashCode()
		{
			int h = parser == null ? this.GetType().GetHashCode() : parser.GetType().GetHashCode
				();
			h += base.GetHashCode();
			return h;
		}
	}
}
