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
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Abstract parent class for
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// implementations that wrap multiple
	/// ValueSources and apply their own logic.
	/// </summary>
	public abstract class MultiFunction : ValueSource
	{
		protected internal readonly IList<ValueSource> sources;

		public MultiFunction(IList<ValueSource> sources)
		{
			this.sources = sources;
		}

		protected internal abstract string Name();

		public override string Description()
		{
			return Description(Name(), sources);
		}

		public static string Description(string name, IList<ValueSource> sources)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(name).Append('(');
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
		public static FunctionValues[] ValsArr(IList<ValueSource> sources, IDictionary fcontext
			, AtomicReaderContext readerContext)
		{
			FunctionValues[] valsArr = new FunctionValues[sources.Count];
			int i = 0;
			foreach (ValueSource source in sources)
			{
				valsArr[i++] = source.GetValues(fcontext, readerContext);
			}
			return valsArr;
		}

		public class Values : FunctionValues
		{
			internal readonly FunctionValues[] valsArr;

			public Values(MultiFunction _enclosing, FunctionValues[] valsArr)
			{
				this._enclosing = _enclosing;
				this.valsArr = valsArr;
			}

			public override string ToString(int doc)
			{
				return MultiFunction.ToString(this._enclosing.Name(), this.valsArr, doc);
			}

			public override FunctionValues.ValueFiller GetValueFiller()
			{
				// TODO: need ValueSource.type() to determine correct type
				return base.GetValueFiller();
			}

			private readonly MultiFunction _enclosing;
		}

		public static string ToString(string name, FunctionValues[] valsArr, int doc)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(name).Append('(');
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
			return sources.GetHashCode() + Name().GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (this.GetType() != o.GetType())
			{
				return false;
			}
			MultiFunction other = (MultiFunction)o;
			return this.sources.Equals(other.sources);
		}
	}
}
