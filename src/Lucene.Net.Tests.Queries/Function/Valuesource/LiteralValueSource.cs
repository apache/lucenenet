/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>Pass a the field value through as a String, no matter the type // Q: doesn't this mean it's a "string"?
	/// 	</summary>
	public class LiteralValueSource : ValueSource
	{
		protected internal readonly string @string;

		protected internal readonly BytesRef bytesRef;

		public LiteralValueSource(string @string)
		{
			this.@string = @string;
			this.bytesRef = new BytesRef(@string);
		}

		/// <summary>returns the literal value</summary>
		public virtual string GetValue()
		{
			return @string;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			return new _StrDocValues_50(this, this);
		}

		private sealed class _StrDocValues_50 : StrDocValues
		{
			public _StrDocValues_50(LiteralValueSource _enclosing, ValueSource baseArg1) : base
				(baseArg1)
			{
				this._enclosing = _enclosing;
			}

			public override string StrVal(int doc)
			{
				return this._enclosing.@string;
			}

			public override bool BytesVal(int doc, BytesRef target)
			{
				target.CopyBytes(this._enclosing.bytesRef);
				return true;
			}

			public override string ToString(int doc)
			{
				return this._enclosing.@string;
			}

			private readonly LiteralValueSource _enclosing;
		}

		public override string Description()
		{
			return "literal(" + @string + ")";
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Org.Apache.Lucene.Queries.Function.Valuesource.LiteralValueSource))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.LiteralValueSource that = (Org.Apache.Lucene.Queries.Function.Valuesource.LiteralValueSource
				)o;
			return @string.Equals(that.@string);
		}

		public static readonly int hash = typeof(Org.Apache.Lucene.Queries.Function.Valuesource.LiteralValueSource
			).GetHashCode();

		public override int GetHashCode()
		{
			return hash + @string.GetHashCode();
		}
	}
}
