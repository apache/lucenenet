/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// implementation which only returns the values from the provided
	/// ValueSources which are available for a particular docId.  Consequently, when combined
	/// with a
	/// <see cref="ConstValueSource">ConstValueSource</see>
	/// , this function serves as a way to return a default
	/// value when the values for a field are unavailable.
	/// </summary>
	public class DefFunction : MultiFunction
	{
		public DefFunction(IList<ValueSource> sources) : base(sources)
		{
		}

		protected internal override string Name()
		{
			return "def";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary fcontext, AtomicReaderContext
			 readerContext)
		{
			return new _Values_51(ValsArr(sources, fcontext, readerContext));
		}

		private sealed class _Values_51 : MultiFunction.Values
		{
			public _Values_51(FunctionValues[] baseArg1) : base(baseArg1)
			{
				this.upto = this.valsArr.Length - 1;
			}

			internal readonly int upto;

			private FunctionValues Get(int doc)
			{
				for (int i = 0; i < this.upto; i++)
				{
					FunctionValues vals = this.valsArr[i];
					if (vals.Exists(doc))
					{
						return vals;
					}
				}
				return this.valsArr[this.upto];
			}

			public override byte ByteVal(int doc)
			{
				return this.Get(doc).ByteVal(doc);
			}

			public override short ShortVal(int doc)
			{
				return this.Get(doc).ShortVal(doc);
			}

			public override float FloatVal(int doc)
			{
				return this.Get(doc).FloatVal(doc);
			}

			public override int IntVal(int doc)
			{
				return this.Get(doc).IntVal(doc);
			}

			public override long LongVal(int doc)
			{
				return this.Get(doc).LongVal(doc);
			}

			public override double DoubleVal(int doc)
			{
				return this.Get(doc).DoubleVal(doc);
			}

			public override string StrVal(int doc)
			{
				return this.Get(doc).StrVal(doc);
			}

			public override bool BoolVal(int doc)
			{
				return this.Get(doc).BoolVal(doc);
			}

			public override bool BytesVal(int doc, BytesRef target)
			{
				return this.Get(doc).BytesVal(doc, target);
			}

			public override object ObjectVal(int doc)
			{
				return this.Get(doc).ObjectVal(doc);
			}

			public override bool Exists(int doc)
			{
				// return true if any source is exists?
				foreach (FunctionValues vals in this.valsArr)
				{
					if (vals.Exists(doc))
					{
						return true;
					}
				}
				return false;
			}

			public override FunctionValues.ValueFiller GetValueFiller()
			{
				// TODO: need ValueSource.type() to determine correct type
				return base.GetValueFiller();
			}
		}
	}
}
