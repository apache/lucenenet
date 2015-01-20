/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Docvalues
{
	/// <summary>
	/// Abstract
	/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionValues">Org.Apache.Lucene.Queries.Function.FunctionValues
	/// 	</see>
	/// implementation which supports retrieving boolean values.
	/// Implementations can control how the boolean values are loaded through
	/// <see cref="BoolVal(int)">BoolVal(int)</see>
	/// }
	/// </summary>
	public abstract class BoolDocValues : FunctionValues
	{
		protected internal readonly ValueSource vs;

		public BoolDocValues(ValueSource vs)
		{
			this.vs = vs;
		}

		public abstract override bool BoolVal(int doc);

		public override byte ByteVal(int doc)
		{
			return BoolVal(doc) ? unchecked((byte)1) : unchecked((byte)0);
		}

		public override short ShortVal(int doc)
		{
			return BoolVal(doc) ? (short)1 : (short)0;
		}

		public override float FloatVal(int doc)
		{
			return BoolVal(doc) ? (float)1 : (float)0;
		}

		public override int IntVal(int doc)
		{
			return BoolVal(doc) ? 1 : 0;
		}

		public override long LongVal(int doc)
		{
			return BoolVal(doc) ? (long)1 : (long)0;
		}

		public override double DoubleVal(int doc)
		{
			return BoolVal(doc) ? (double)1 : (double)0;
		}

		public override string StrVal(int doc)
		{
			return bool.ToString(BoolVal(doc));
		}

		public override object ObjectVal(int doc)
		{
			return Exists(doc) ? BoolVal(doc) : null;
		}

		public override string ToString(int doc)
		{
			return vs.Description() + '=' + StrVal(doc);
		}

		public override FunctionValues.ValueFiller GetValueFiller()
		{
			return new _ValueFiller_86(this);
		}

		private sealed class _ValueFiller_86 : FunctionValues.ValueFiller
		{
			public _ValueFiller_86(BoolDocValues _enclosing)
			{
				this._enclosing = _enclosing;
				this.mval = new MutableValueBool();
			}

			private readonly MutableValueBool mval;

			public override MutableValue GetValue()
			{
				return this.mval;
			}

			public override void FillValue(int doc)
			{
				this.mval.value = this._enclosing.BoolVal(doc);
				this.mval.exists = this._enclosing.Exists(doc);
			}

			private readonly BoolDocValues _enclosing;
		}
	}
}
