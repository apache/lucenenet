/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Abstract
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// implementation which wraps multiple ValueSources
	/// and applies an extendible boolean function to their values.
	/// </summary>
	public abstract class MultiBoolFunction : BoolFunction
	{
		protected internal readonly IList<ValueSource> sources;

		public MultiBoolFunction(IList<ValueSource> sources)
		{
			this.sources = sources;
		}

		protected internal abstract string Name();

		protected internal abstract bool Func(int doc, FunctionValues[] vals);

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FunctionValues[] vals = new FunctionValues[sources.Count];
			int i = 0;
			foreach (ValueSource source in sources)
			{
				vals[i++] = source.GetValues(context, readerContext);
			}
			return new _BoolDocValues_53(this, vals, this);
		}

		private sealed class _BoolDocValues_53 : BoolDocValues
		{
			public _BoolDocValues_53(MultiBoolFunction _enclosing, FunctionValues[] vals, ValueSource
				 baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.vals = vals;
			}

			public override bool BoolVal(int doc)
			{
				return this._enclosing.Func(doc, vals);
			}

			public override string ToString(int doc)
			{
				StringBuilder sb = new StringBuilder(this._enclosing.Name());
				sb.Append('(');
				bool first = true;
				foreach (FunctionValues dv in vals)
				{
					if (first)
					{
						first = false;
					}
					else
					{
						sb.Append(',');
					}
					sb.Append(dv.ToString(doc));
				}
				return sb.ToString();
			}

			private readonly MultiBoolFunction _enclosing;

			private readonly FunctionValues[] vals;
		}

		public override string Description()
		{
			StringBuilder sb = new StringBuilder(Name());
			sb.Append('(');
			bool first = true;
			foreach (ValueSource source in sources)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					sb.Append(',');
				}
				sb.Append(source.Description());
			}
			return sb.ToString();
		}

		public override int GetHashCode()
		{
			return sources.GetHashCode() + Name().GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (this.GetType() != o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.MultiBoolFunction other = (Org.Apache.Lucene.Queries.Function.Valuesource.MultiBoolFunction
				)o;
			return this.sources.Equals(other.sources);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			foreach (ValueSource source in sources)
			{
				source.CreateWeight(context, searcher);
			}
		}
	}
}
