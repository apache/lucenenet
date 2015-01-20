/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function
{
	/// <summary>Represents field values as different types.</summary>
	/// <remarks>
	/// Represents field values as different types.
	/// Normally created via a
	/// <see cref="ValueSource">ValueSource</see>
	/// for a particular field and reader.
	/// </remarks>
	public abstract class FunctionValues
	{
		// FunctionValues is distinct from ValueSource because
		// there needs to be an object created at query evaluation time that
		// is not referenced by the query itself because:
		// - Query objects should be MT safe
		// - For caching, Query objects are often used as keys... you don't
		//   want the Query carrying around big objects
		public virtual byte ByteVal(int doc)
		{
			throw new NotSupportedException();
		}

		public virtual short ShortVal(int doc)
		{
			throw new NotSupportedException();
		}

		public virtual float FloatVal(int doc)
		{
			throw new NotSupportedException();
		}

		public virtual int IntVal(int doc)
		{
			throw new NotSupportedException();
		}

		public virtual long LongVal(int doc)
		{
			throw new NotSupportedException();
		}

		public virtual double DoubleVal(int doc)
		{
			throw new NotSupportedException();
		}

		// TODO: should we make a termVal, returns BytesRef?
		public virtual string StrVal(int doc)
		{
			throw new NotSupportedException();
		}

		public virtual bool BoolVal(int doc)
		{
			return IntVal(doc) != 0;
		}

		/// <summary>returns the bytes representation of the string val - TODO: should this return the indexed raw bytes not?
		/// 	</summary>
		public virtual bool BytesVal(int doc, BytesRef target)
		{
			string s = StrVal(doc);
			if (s == null)
			{
				target.length = 0;
				return false;
			}
			target.CopyChars(s);
			return true;
		}

		/// <summary>Native Java Object representation of the value</summary>
		public virtual object ObjectVal(int doc)
		{
			// most FunctionValues are functions, so by default return a Float()
			return FloatVal(doc);
		}

		/// <summary>Returns true if there is a value for this document</summary>
		public virtual bool Exists(int doc)
		{
			return true;
		}

		/// <param name="doc">The doc to retrieve to sort ordinal for</param>
		/// <returns>
		/// the sort ordinal for the specified doc
		/// TODO: Maybe we can just use intVal for this...
		/// </returns>
		public virtual int OrdVal(int doc)
		{
			throw new NotSupportedException();
		}

		/// <returns>the number of unique sort ordinals this instance has</returns>
		public virtual int NumOrd()
		{
			throw new NotSupportedException();
		}

		public abstract string ToString(int doc);

		/// <summary>
		/// Abstraction of the logic required to fill the value of a specified doc into
		/// a reusable
		/// <see cref="Org.Apache.Lucene.Util.Mutable.MutableValue">Org.Apache.Lucene.Util.Mutable.MutableValue
		/// 	</see>
		/// .  Implementations of
		/// <see cref="FunctionValues">FunctionValues</see>
		/// are encouraged to define their own implementations of ValueFiller if their
		/// value is not a float.
		/// </summary>
		/// <lucene.experimental></lucene.experimental>
		public abstract class ValueFiller
		{
			/// <summary>MutableValue will be reused across calls</summary>
			public abstract MutableValue GetValue();

			/// <summary>MutableValue will be reused across calls.</summary>
			/// <remarks>MutableValue will be reused across calls.  Returns true if the value exists.
			/// 	</remarks>
			public abstract void FillValue(int doc);
		}

		/// <lucene.experimental></lucene.experimental>
		public virtual FunctionValues.ValueFiller GetValueFiller()
		{
			return new _ValueFiller_108(this);
		}

		private sealed class _ValueFiller_108 : FunctionValues.ValueFiller
		{
			public _ValueFiller_108(FunctionValues _enclosing)
			{
				this._enclosing = _enclosing;
				this.mval = new MutableValueFloat();
			}

			private readonly MutableValueFloat mval;

			public override MutableValue GetValue()
			{
				return this.mval;
			}

			public override void FillValue(int doc)
			{
				this.mval.value = this._enclosing.FloatVal(doc);
			}

			private readonly FunctionValues _enclosing;
		}

		//For Functions that can work with multiple values from the same document.  This does not apply to all functions
		public virtual void ByteVal(int doc, byte[] vals)
		{
			throw new NotSupportedException();
		}

		public virtual void ShortVal(int doc, short[] vals)
		{
			throw new NotSupportedException();
		}

		public virtual void FloatVal(int doc, float[] vals)
		{
			throw new NotSupportedException();
		}

		public virtual void IntVal(int doc, int[] vals)
		{
			throw new NotSupportedException();
		}

		public virtual void LongVal(int doc, long[] vals)
		{
			throw new NotSupportedException();
		}

		public virtual void DoubleVal(int doc, double[] vals)
		{
			throw new NotSupportedException();
		}

		// TODO: should we make a termVal, fills BytesRef[]?
		public virtual void StrVal(int doc, string[] vals)
		{
			throw new NotSupportedException();
		}

		public virtual Explanation Explain(int doc)
		{
			return new Explanation(FloatVal(doc), ToString(doc));
		}

		public virtual ValueSourceScorer GetScorer(IndexReader reader)
		{
			return new ValueSourceScorer(reader, this);
		}

		// A RangeValueSource can't easily be a ValueSource that takes another ValueSource
		// because it needs different behavior depending on the type of fields.  There is also
		// a setup cost - parsing and normalizing params, and doing a binary search on the StringIndex.
		// TODO: change "reader" to AtomicReaderContext
		public virtual ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal
			, string upperVal, bool includeLower, bool includeUpper)
		{
			float lower;
			float upper;
			if (lowerVal == null)
			{
				lower = float.NegativeInfinity;
			}
			else
			{
				lower = float.ParseFloat(lowerVal);
			}
			if (upperVal == null)
			{
				upper = float.PositiveInfinity;
			}
			else
			{
				upper = float.ParseFloat(upperVal);
			}
			float l = lower;
			float u = upper;
			if (includeLower && includeUpper)
			{
				return new _ValueSourceScorer_166(this, l, u, reader, this);
			}
			else
			{
				if (includeLower && !includeUpper)
				{
					return new _ValueSourceScorer_175(this, l, u, reader, this);
				}
				else
				{
					if (!includeLower && includeUpper)
					{
						return new _ValueSourceScorer_184(this, l, u, reader, this);
					}
					else
					{
						return new _ValueSourceScorer_193(this, l, u, reader, this);
					}
				}
			}
		}

		private sealed class _ValueSourceScorer_166 : ValueSourceScorer
		{
			public _ValueSourceScorer_166(FunctionValues _enclosing, float l, float u, IndexReader
				 baseArg1, FunctionValues baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.l = l;
				this.u = u;
			}

			public override bool MatchesValue(int doc)
			{
				float docVal = this._enclosing.FloatVal(doc);
				return docVal >= l && docVal <= u;
			}

			private readonly FunctionValues _enclosing;

			private readonly float l;

			private readonly float u;
		}

		private sealed class _ValueSourceScorer_175 : ValueSourceScorer
		{
			public _ValueSourceScorer_175(FunctionValues _enclosing, float l, float u, IndexReader
				 baseArg1, FunctionValues baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.l = l;
				this.u = u;
			}

			public override bool MatchesValue(int doc)
			{
				float docVal = this._enclosing.FloatVal(doc);
				return docVal >= l && docVal < u;
			}

			private readonly FunctionValues _enclosing;

			private readonly float l;

			private readonly float u;
		}

		private sealed class _ValueSourceScorer_184 : ValueSourceScorer
		{
			public _ValueSourceScorer_184(FunctionValues _enclosing, float l, float u, IndexReader
				 baseArg1, FunctionValues baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.l = l;
				this.u = u;
			}

			public override bool MatchesValue(int doc)
			{
				float docVal = this._enclosing.FloatVal(doc);
				return docVal > l && docVal <= u;
			}

			private readonly FunctionValues _enclosing;

			private readonly float l;

			private readonly float u;
		}

		private sealed class _ValueSourceScorer_193 : ValueSourceScorer
		{
			public _ValueSourceScorer_193(FunctionValues _enclosing, float l, float u, IndexReader
				 baseArg1, FunctionValues baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.l = l;
				this.u = u;
			}

			public override bool MatchesValue(int doc)
			{
				float docVal = this._enclosing.FloatVal(doc);
				return docVal > l && docVal < u;
			}

			private readonly FunctionValues _enclosing;

			private readonly float l;

			private readonly float u;
		}
	}
}
