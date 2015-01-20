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
	/// implementation which supports retrieving double values.
	/// Implementations can control how the double values are loaded through
	/// <see cref="DoubleVal(int)">DoubleVal(int)</see>
	/// }
	/// </summary>
	public abstract class DoubleDocValues : FunctionValues
	{
		protected internal readonly ValueSource vs;

		public DoubleDocValues(ValueSource vs)
		{
			this.vs = vs;
		}

		public override byte ByteVal(int doc)
		{
			return unchecked((byte)DoubleVal(doc));
		}

		public override short ShortVal(int doc)
		{
			return (short)DoubleVal(doc);
		}

		public override float FloatVal(int doc)
		{
			return (float)DoubleVal(doc);
		}

		public override int IntVal(int doc)
		{
			return (int)DoubleVal(doc);
		}

		public override long LongVal(int doc)
		{
			return (long)DoubleVal(doc);
		}

		public override bool BoolVal(int doc)
		{
			return DoubleVal(doc) != 0;
		}

		public abstract override double DoubleVal(int doc);

		public override string StrVal(int doc)
		{
			return double.ToString(DoubleVal(doc));
		}

		public override object ObjectVal(int doc)
		{
			return Exists(doc) ? DoubleVal(doc) : null;
		}

		public override string ToString(int doc)
		{
			return vs.Description() + '=' + StrVal(doc);
		}

		public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal
			, string upperVal, bool includeLower, bool includeUpper)
		{
			double lower;
			double upper;
			if (lowerVal == null)
			{
				lower = double.NegativeInfinity;
			}
			else
			{
				lower = double.ParseDouble(lowerVal);
			}
			if (upperVal == null)
			{
				upper = double.PositiveInfinity;
			}
			else
			{
				upper = double.ParseDouble(upperVal);
			}
			double l = lower;
			double u = upper;
			if (includeLower && includeUpper)
			{
				return new _ValueSourceScorer_107(this, l, u, reader, this);
			}
			else
			{
				if (includeLower && !includeUpper)
				{
					return new _ValueSourceScorer_116(this, l, u, reader, this);
				}
				else
				{
					if (!includeLower && includeUpper)
					{
						return new _ValueSourceScorer_125(this, l, u, reader, this);
					}
					else
					{
						return new _ValueSourceScorer_134(this, l, u, reader, this);
					}
				}
			}
		}

		private sealed class _ValueSourceScorer_107 : ValueSourceScorer
		{
			public _ValueSourceScorer_107(DoubleDocValues _enclosing, double l, double u, IndexReader
				 baseArg1, FunctionValues baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.l = l;
				this.u = u;
			}

			public override bool MatchesValue(int doc)
			{
				double docVal = this._enclosing.DoubleVal(doc);
				return docVal >= l && docVal <= u;
			}

			private readonly DoubleDocValues _enclosing;

			private readonly double l;

			private readonly double u;
		}

		private sealed class _ValueSourceScorer_116 : ValueSourceScorer
		{
			public _ValueSourceScorer_116(DoubleDocValues _enclosing, double l, double u, IndexReader
				 baseArg1, FunctionValues baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.l = l;
				this.u = u;
			}

			public override bool MatchesValue(int doc)
			{
				double docVal = this._enclosing.DoubleVal(doc);
				return docVal >= l && docVal < u;
			}

			private readonly DoubleDocValues _enclosing;

			private readonly double l;

			private readonly double u;
		}

		private sealed class _ValueSourceScorer_125 : ValueSourceScorer
		{
			public _ValueSourceScorer_125(DoubleDocValues _enclosing, double l, double u, IndexReader
				 baseArg1, FunctionValues baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.l = l;
				this.u = u;
			}

			public override bool MatchesValue(int doc)
			{
				double docVal = this._enclosing.DoubleVal(doc);
				return docVal > l && docVal <= u;
			}

			private readonly DoubleDocValues _enclosing;

			private readonly double l;

			private readonly double u;
		}

		private sealed class _ValueSourceScorer_134 : ValueSourceScorer
		{
			public _ValueSourceScorer_134(DoubleDocValues _enclosing, double l, double u, IndexReader
				 baseArg1, FunctionValues baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.l = l;
				this.u = u;
			}

			public override bool MatchesValue(int doc)
			{
				double docVal = this._enclosing.DoubleVal(doc);
				return docVal > l && docVal < u;
			}

			private readonly DoubleDocValues _enclosing;

			private readonly double l;

			private readonly double u;
		}

		public override FunctionValues.ValueFiller GetValueFiller()
		{
			return new _ValueFiller_146(this);
		}

		private sealed class _ValueFiller_146 : FunctionValues.ValueFiller
		{
			public _ValueFiller_146(DoubleDocValues _enclosing)
			{
				this._enclosing = _enclosing;
				this.mval = new MutableValueDouble();
			}

			private readonly MutableValueDouble mval;

			public override MutableValue GetValue()
			{
				return this.mval;
			}

			public override void FillValue(int doc)
			{
				this.mval.value = this._enclosing.DoubleVal(doc);
				this.mval.exists = this._enclosing.Exists(doc);
			}

			private readonly DoubleDocValues _enclosing;
		}
	}
}
