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
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>Function that returns a constant double value for every document.</summary>
	/// <remarks>Function that returns a constant double value for every document.</remarks>
	public class DoubleConstValueSource : ConstNumberSource
	{
		internal readonly double constant;

		private readonly float fv;

		private readonly long lv;

		public DoubleConstValueSource(double constant)
		{
			this.constant = constant;
			this.fv = (float)constant;
			this.lv = (long)constant;
		}

		public override string Description()
		{
			return "const(" + constant + ")";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			return new _DoubleDocValues_48(this, this);
		}

		private sealed class _DoubleDocValues_48 : DoubleDocValues
		{
			public _DoubleDocValues_48(DoubleConstValueSource _enclosing, ValueSource baseArg1
				) : base(baseArg1)
			{
				this._enclosing = _enclosing;
			}

			public override float FloatVal(int doc)
			{
				return this._enclosing.fv;
			}

			public override int IntVal(int doc)
			{
				return (int)this._enclosing.lv;
			}

			public override long LongVal(int doc)
			{
				return this._enclosing.lv;
			}

			public override double DoubleVal(int doc)
			{
				return this._enclosing.constant;
			}

			public override string StrVal(int doc)
			{
				return double.ToString(this._enclosing.constant);
			}

			public override object ObjectVal(int doc)
			{
				return this._enclosing.constant;
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Description();
			}

			private readonly DoubleConstValueSource _enclosing;
		}

		public override int GetHashCode()
		{
			long bits = double.DoubleToRawLongBits(constant);
			return (int)(bits ^ ((long)(((ulong)bits) >> 32)));
		}

		public override bool Equals(object o)
		{
			if (!(o is Org.Apache.Lucene.Queries.Function.Valuesource.DoubleConstValueSource))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.DoubleConstValueSource other = (Org.Apache.Lucene.Queries.Function.Valuesource.DoubleConstValueSource
				)o;
			return this.constant == other.constant;
		}

		public override int GetInt()
		{
			return (int)lv;
		}

		public override long GetLong()
		{
			return lv;
		}

		public override float GetFloat()
		{
			return fv;
		}

		public override double GetDouble()
		{
			return constant;
		}

		public override Number GetNumber()
		{
			return constant;
		}

		public override bool GetBool()
		{
			return constant != 0;
		}
	}
}
