/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using System.Text;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Abstract
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// implementation which wraps multiple ValueSources
	/// and applies an extendible float function to their values.
	/// </summary>
	public abstract class MultiFloatFunction : ValueSource
	{
		protected internal readonly ValueSource[] sources;

		public MultiFloatFunction(ValueSource[] sources)
		{
			this.sources = sources;
		}

		protected internal abstract string Name();

		protected internal abstract float Func(int doc, FunctionValues[] valsArr);

		public override string Description()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Name()).Append('(');
			bool firstTime = true;
			foreach (ValueSource source in sources)
			{
				if (firstTime)
				{
					firstTime = false;
				}
				else
				{
					sb.Append(',');
				}
				sb.Append(source);
			}
			sb.Append(')');
			return sb.ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FunctionValues[] valsArr = new FunctionValues[sources.Length];
			for (int i = 0; i < sources.Length; i++)
			{
				valsArr[i] = sources[i].GetValues(context, readerContext);
			}
			return new _FloatDocValues_68(this, valsArr, this);
		}

		private sealed class _FloatDocValues_68 : FloatDocValues
		{
			public _FloatDocValues_68(MultiFloatFunction _enclosing, FunctionValues[] valsArr
				, ValueSource baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.valsArr = valsArr;
			}

			public override float FloatVal(int doc)
			{
				return this._enclosing.Func(doc, valsArr);
			}

			public override string ToString(int doc)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(this._enclosing.Name()).Append('(');
				bool firstTime = true;
				foreach (FunctionValues vals in valsArr)
				{
					if (firstTime)
					{
						firstTime = false;
					}
					else
					{
						sb.Append(',');
					}
					sb.Append(vals.ToString(doc));
				}
				sb.Append(')');
				return sb.ToString();
			}

			private readonly MultiFloatFunction _enclosing;

			private readonly FunctionValues[] valsArr;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			foreach (ValueSource source in sources)
			{
				source.CreateWeight(context, searcher);
			}
		}

		public override int GetHashCode()
		{
			return Arrays.HashCode(sources) + Name().GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (this.GetType() != o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.MultiFloatFunction other = (Org.Apache.Lucene.Queries.Function.Valuesource.MultiFloatFunction
				)o;
			return this.Name().Equals(other.Name()) && Arrays.Equals(this.sources, other.sources
				);
		}
	}
}
