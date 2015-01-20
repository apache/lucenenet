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
	/// implementation which supports retrieving float values.
	/// Implementations can control how the float values are loaded through
	/// <see cref="FloatVal(int)">FloatVal(int)</see>
	/// }
	/// </summary>
	public abstract class FloatDocValues : FunctionValues
	{
		protected internal readonly ValueSource vs;

		public FloatDocValues(ValueSource vs)
		{
			this.vs = vs;
		}

		public override byte ByteVal(int doc)
		{
			return unchecked((byte)FloatVal(doc));
		}

		public override short ShortVal(int doc)
		{
			return (short)FloatVal(doc);
		}

		public abstract override float FloatVal(int doc);

		public override int IntVal(int doc)
		{
			return (int)FloatVal(doc);
		}

		public override long LongVal(int doc)
		{
			return (long)FloatVal(doc);
		}

		public override double DoubleVal(int doc)
		{
			return (double)FloatVal(doc);
		}

		public override string StrVal(int doc)
		{
			return float.ToString(FloatVal(doc));
		}

		public override object ObjectVal(int doc)
		{
			return Exists(doc) ? FloatVal(doc) : null;
		}

		public override string ToString(int doc)
		{
			return vs.Description() + '=' + StrVal(doc);
		}

		public override FunctionValues.ValueFiller GetValueFiller()
		{
			return new _ValueFiller_81(this);
		}

		private sealed class _ValueFiller_81 : FunctionValues.ValueFiller
		{
			public _ValueFiller_81(FloatDocValues _enclosing)
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
				this.mval.exists = this._enclosing.Exists(doc);
			}

			private readonly FloatDocValues _enclosing;
		}
	}
}
