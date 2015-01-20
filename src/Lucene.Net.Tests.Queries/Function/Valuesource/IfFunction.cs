/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Depending on the boolean value of the <code>ifSource</code> function,
	/// returns the value of the <code>trueSource</code> or <code>falseSource</code> function.
	/// </summary>
	/// <remarks>
	/// Depending on the boolean value of the <code>ifSource</code> function,
	/// returns the value of the <code>trueSource</code> or <code>falseSource</code> function.
	/// </remarks>
	public class IfFunction : BoolFunction
	{
		private readonly ValueSource ifSource;

		private readonly ValueSource trueSource;

		private readonly ValueSource falseSource;

		public IfFunction(ValueSource ifSource, ValueSource trueSource, ValueSource falseSource
			)
		{
			this.ifSource = ifSource;
			this.trueSource = trueSource;
			this.falseSource = falseSource;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FunctionValues ifVals = ifSource.GetValues(context, readerContext);
			FunctionValues trueVals = trueSource.GetValues(context, readerContext);
			FunctionValues falseVals = falseSource.GetValues(context, readerContext);
			return new _FunctionValues_55(ifVals, trueVals, falseVals);
		}

		private sealed class _FunctionValues_55 : FunctionValues
		{
			public _FunctionValues_55(FunctionValues ifVals, FunctionValues trueVals, FunctionValues
				 falseVals)
			{
				this.ifVals = ifVals;
				this.trueVals = trueVals;
				this.falseVals = falseVals;
			}

			public override byte ByteVal(int doc)
			{
				return ifVals.BoolVal(doc) ? trueVals.ByteVal(doc) : falseVals.ByteVal(doc);
			}

			public override short ShortVal(int doc)
			{
				return ifVals.BoolVal(doc) ? trueVals.ShortVal(doc) : falseVals.ShortVal(doc);
			}

			public override float FloatVal(int doc)
			{
				return ifVals.BoolVal(doc) ? trueVals.FloatVal(doc) : falseVals.FloatVal(doc);
			}

			public override int IntVal(int doc)
			{
				return ifVals.BoolVal(doc) ? trueVals.IntVal(doc) : falseVals.IntVal(doc);
			}

			public override long LongVal(int doc)
			{
				return ifVals.BoolVal(doc) ? trueVals.LongVal(doc) : falseVals.LongVal(doc);
			}

			public override double DoubleVal(int doc)
			{
				return ifVals.BoolVal(doc) ? trueVals.DoubleVal(doc) : falseVals.DoubleVal(doc);
			}

			public override string StrVal(int doc)
			{
				return ifVals.BoolVal(doc) ? trueVals.StrVal(doc) : falseVals.StrVal(doc);
			}

			public override bool BoolVal(int doc)
			{
				return ifVals.BoolVal(doc) ? trueVals.BoolVal(doc) : falseVals.BoolVal(doc);
			}

			public override bool BytesVal(int doc, BytesRef target)
			{
				return ifVals.BoolVal(doc) ? trueVals.BytesVal(doc, target) : falseVals.BytesVal(
					doc, target);
			}

			public override object ObjectVal(int doc)
			{
				return ifVals.BoolVal(doc) ? trueVals.ObjectVal(doc) : falseVals.ObjectVal(doc);
			}

			public override bool Exists(int doc)
			{
				return true;
			}

			// TODO: flow through to any sub-sources?
			public override FunctionValues.ValueFiller GetValueFiller()
			{
				// TODO: we need types of trueSource / falseSource to handle this
				// for now, use float.
				return base.GetValueFiller();
			}

			public override string ToString(int doc)
			{
				return "if(" + ifVals.ToString(doc) + ',' + trueVals.ToString(doc) + ',' + falseVals
					.ToString(doc) + ')';
			}

			private readonly FunctionValues ifVals;

			private readonly FunctionValues trueVals;

			private readonly FunctionValues falseVals;
		}

		public override string Description()
		{
			return "if(" + ifSource.Description() + ',' + trueSource.Description() + ',' + falseSource
				 + ')';
		}

		public override int GetHashCode()
		{
			int h = ifSource.GetHashCode();
			h = h * 31 + trueSource.GetHashCode();
			h = h * 31 + falseSource.GetHashCode();
			return h;
		}

		public override bool Equals(object o)
		{
			if (!(o is Org.Apache.Lucene.Queries.Function.Valuesource.IfFunction))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.IfFunction other = (Org.Apache.Lucene.Queries.Function.Valuesource.IfFunction
				)o;
			return ifSource.Equals(other.ifSource) && trueSource.Equals(other.trueSource) && 
				falseSource.Equals(other.falseSource);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			ifSource.CreateWeight(context, searcher);
			trueSource.CreateWeight(context, searcher);
			falseSource.CreateWeight(context, searcher);
		}
	}
}
