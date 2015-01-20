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
	/// <summary><code>ConstValueSource</code> returns a constant for all documents</summary>
	public class ConstValueSource : ConstNumberSource
	{
		internal readonly float constant;

		private readonly double dv;

		public ConstValueSource(float constant)
		{
			this.constant = constant;
			this.dv = constant;
		}

		public override string Description()
		{
			return "const(" + constant + ")";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			return new _FloatDocValues_46(this, this);
		}

		private sealed class _FloatDocValues_46 : FloatDocValues
		{
			public _FloatDocValues_46(ConstValueSource _enclosing, ValueSource baseArg1) : base
				(baseArg1)
			{
				this._enclosing = _enclosing;
			}

			public override float FloatVal(int doc)
			{
				return this._enclosing.constant;
			}

			public override int IntVal(int doc)
			{
				return (int)this._enclosing.constant;
			}

			public override long LongVal(int doc)
			{
				return (long)this._enclosing.constant;
			}

			public override double DoubleVal(int doc)
			{
				return this._enclosing.dv;
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Description();
			}

			public override object ObjectVal(int doc)
			{
				return this._enclosing.constant;
			}

			public override bool BoolVal(int doc)
			{
				return this._enclosing.constant != 0.0f;
			}

			private readonly ConstValueSource _enclosing;
		}

		public override int GetHashCode()
		{
			return Sharpen.Runtime.FloatToIntBits(constant) * 31;
		}

		public override bool Equals(object o)
		{
			if (!(o is Org.Apache.Lucene.Queries.Function.Valuesource.ConstValueSource))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.ConstValueSource other = (Org.Apache.Lucene.Queries.Function.Valuesource.ConstValueSource
				)o;
			return this.constant == other.constant;
		}

		public override int GetInt()
		{
			return (int)constant;
		}

		public override long GetLong()
		{
			return (long)constant;
		}

		public override float GetFloat()
		{
			return constant;
		}

		public override double GetDouble()
		{
			return dv;
		}

		public override Number GetNumber()
		{
			return constant;
		}

		public override bool GetBool()
		{
			return constant != 0.0f;
		}
	}
}
