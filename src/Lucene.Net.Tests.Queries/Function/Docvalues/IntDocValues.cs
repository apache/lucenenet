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
	/// implementation which supports retrieving int values.
	/// Implementations can control how the int values are loaded through
	/// <see cref="IntVal(int)">IntVal(int)</see>
	/// </summary>
	public abstract class IntDocValues : FunctionValues
	{
		protected internal readonly ValueSource vs;

		public IntDocValues(ValueSource vs)
		{
			this.vs = vs;
		}

		public override byte ByteVal(int doc)
		{
			return unchecked((byte)IntVal(doc));
		}

		public override short ShortVal(int doc)
		{
			return (short)IntVal(doc);
		}

		public override float FloatVal(int doc)
		{
			return (float)IntVal(doc);
		}

		public abstract override int IntVal(int doc);

		public override long LongVal(int doc)
		{
			return (long)IntVal(doc);
		}

		public override double DoubleVal(int doc)
		{
			return (double)IntVal(doc);
		}

		public override string StrVal(int doc)
		{
			return Sharpen.Extensions.ToString(IntVal(doc));
		}

		public override object ObjectVal(int doc)
		{
			return Exists(doc) ? IntVal(doc) : null;
		}

		public override string ToString(int doc)
		{
			return vs.Description() + '=' + StrVal(doc);
		}

		public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal
			, string upperVal, bool includeLower, bool includeUpper)
		{
			int lower;
			int upper;
			// instead of using separate comparison functions, adjust the endpoints.
			if (lowerVal == null)
			{
				lower = int.MinValue;
			}
			else
			{
				lower = System.Convert.ToInt32(lowerVal);
				if (!includeLower && lower < int.MaxValue)
				{
					lower++;
				}
			}
			if (upperVal == null)
			{
				upper = int.MaxValue;
			}
			else
			{
				upper = System.Convert.ToInt32(upperVal);
				if (!includeUpper && upper > int.MinValue)
				{
					upper--;
				}
			}
			int ll = lower;
			int uu = upper;
			return new _ValueSourceScorer_104(this, ll, uu, reader, this);
		}

		private sealed class _ValueSourceScorer_104 : ValueSourceScorer
		{
			public _ValueSourceScorer_104(IntDocValues _enclosing, int ll, int uu, IndexReader
				 baseArg1, FunctionValues baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.ll = ll;
				this.uu = uu;
			}

			public override bool MatchesValue(int doc)
			{
				int val = this._enclosing.IntVal(doc);
				// only check for deleted if it's the default value
				// if (val==0 && reader.isDeleted(doc)) return false;
				return val >= ll && val <= uu;
			}

			private readonly IntDocValues _enclosing;

			private readonly int ll;

			private readonly int uu;
		}

		public override FunctionValues.ValueFiller GetValueFiller()
		{
			return new _ValueFiller_117(this);
		}

		private sealed class _ValueFiller_117 : FunctionValues.ValueFiller
		{
			public _ValueFiller_117(IntDocValues _enclosing)
			{
				this._enclosing = _enclosing;
				this.mval = new MutableValueInt();
			}

			private readonly MutableValueInt mval;

			public override MutableValue GetValue()
			{
				return this.mval;
			}

			public override void FillValue(int doc)
			{
				this.mval.value = this._enclosing.IntVal(doc);
				this.mval.exists = this._enclosing.Exists(doc);
			}

			private readonly IntDocValues _enclosing;
		}
	}
}
