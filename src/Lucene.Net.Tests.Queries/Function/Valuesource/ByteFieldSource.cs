/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Obtains int field values from the
	/// <see cref="Org.Apache.Lucene.Search.FieldCache">Org.Apache.Lucene.Search.FieldCache
	/// 	</see>
	/// using <code>getInts()</code>
	/// and makes those values available as other numeric types, casting as needed.
	/// </summary>
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

		public override string Description()
		{
			return "byte(" + field + ')';
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FieldCache.Bytes arr = cache.GetBytes(((AtomicReader)readerContext.Reader()), field
				, parser, false);
			return new _FunctionValues_56(this, arr);
		}

		private sealed class _FunctionValues_56 : FunctionValues
		{
			public _FunctionValues_56(ByteFieldSource _enclosing, FieldCache.Bytes arr)
			{
				this._enclosing = _enclosing;
				this.arr = arr;
			}

			public override byte ByteVal(int doc)
			{
				return arr.Get(doc);
			}

			public override short ShortVal(int doc)
			{
				return (short)arr.Get(doc);
			}

			public override float FloatVal(int doc)
			{
				return (float)arr.Get(doc);
			}

			public override int IntVal(int doc)
			{
				return (int)arr.Get(doc);
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
				return byte.ToString(arr.Get(doc));
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Description() + '=' + this.ByteVal(doc);
			}

			public override object ObjectVal(int doc)
			{
				return arr.Get(doc);
			}

			private readonly ByteFieldSource _enclosing;

			private readonly FieldCache.Bytes arr;
		}

		// TODO: valid?
		public override bool Equals(object o)
		{
			if (o.GetType() != typeof(Org.Apache.Lucene.Queries.Function.Valuesource.ByteFieldSource
				))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.ByteFieldSource other = (Org.Apache.Lucene.Queries.Function.Valuesource.ByteFieldSource
				)o;
			return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser
				.GetType() == other.parser.GetType());
		}

		public override int GetHashCode()
		{
			int h = parser == null ? typeof(byte).GetHashCode() : parser.GetType().GetHashCode
				();
			h += base.GetHashCode();
			return h;
		}
	}
}
