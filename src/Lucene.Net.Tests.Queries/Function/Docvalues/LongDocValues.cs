/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Docvalues
{
	/// <summary>
	/// Abstract
	/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionValues">Org.Apache.Lucene.Queries.Function.FunctionValues
	/// 	</see>
	/// implementation which supports retrieving long values.
	/// Implementations can control how the long values are loaded through
	/// <see cref="LongVal(int)">LongVal(int)</see>
	/// }
	/// </summary>
	public abstract class LongDocValues : FunctionValues
	{
		protected internal readonly ValueSource vs;

		public LongDocValues(ValueSource vs)
		{
			this.vs = vs;
		}

		public override byte ByteVal(int doc)
		{
			return unchecked((byte)LongVal(doc));
		}

		public override short ShortVal(int doc)
		{
			return (short)LongVal(doc);
		}

		public override float FloatVal(int doc)
		{
			return (float)LongVal(doc);
		}

		public override int IntVal(int doc)
		{
			return (int)LongVal(doc);
		}

		public abstract override long LongVal(int doc);

		public override double DoubleVal(int doc)
		{
			return (double)LongVal(doc);
		}

		public override bool BoolVal(int doc)
		{
			return LongVal(doc) != 0;
		}

		public override string StrVal(int doc)
		{
			return System.Convert.ToString(LongVal(doc));
		}

		public override object ObjectVal(int doc)
		{
			return Exists(doc) ? LongVal(doc) : null;
		}

		public override string ToString(int doc)
		{
			return vs.Description() + '=' + StrVal(doc);
		}

		protected internal virtual long ExternalToLong(string extVal)
		{
			return long.Parse(extVal);
		}

		public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal
			, string upperVal, bool includeLower, bool includeUpper)
		{
			long lower;
			long upper;
			// instead of using separate comparison functions, adjust the endpoints.
			if (lowerVal == null)
			{
				lower = long.MinValue;
			}
			else
			{
				lower = ExternalToLong(lowerVal);
				if (!includeLower && lower < long.MaxValue)
				{
					lower++;
				}
			}
			if (upperVal == null)
			{
				upper = long.MaxValue;
			}
			else
			{
				upper = ExternalToLong(upperVal);
				if (!includeUpper && upper > long.MinValue)
				{
					upper--;
				}
			}
			long ll = lower;
			long uu = upper;
			return new _ValueSourceScorer_113(this, ll, uu, reader, this);
		}

		private sealed class _ValueSourceScorer_113 : ValueSourceScorer
		{
			public _ValueSourceScorer_113(LongDocValues _enclosing, long ll, long uu, IndexReader
				 baseArg1, FunctionValues baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.ll = ll;
				this.uu = uu;
			}

			public override bool MatchesValue(int doc)
			{
				long val = this._enclosing.LongVal(doc);
				// only check for deleted if it's the default value
				// if (val==0 && reader.isDeleted(doc)) return false;
				return val >= ll && val <= uu;
			}

			private readonly LongDocValues _enclosing;

			private readonly long ll;

			private readonly long uu;
		}

		public override FunctionValues.ValueFiller GetValueFiller()
		{
			return new _ValueFiller_126(this);
		}

		private sealed class _ValueFiller_126 : FunctionValues.ValueFiller
		{
			public _ValueFiller_126(LongDocValues _enclosing)
			{
				this._enclosing = _enclosing;
				this.mval = new MutableValueLong();
			}

			private readonly MutableValueLong mval;

			public override MutableValue GetValue()
			{
				return this.mval;
			}

			public override void FillValue(int doc)
			{
				this.mval.value = this._enclosing.LongVal(doc);
				this.mval.exists = this._enclosing.Exists(doc);
			}

			private readonly LongDocValues _enclosing;
		}
	}
}
