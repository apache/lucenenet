/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>A function with a single argument</summary>
	public abstract class SingleFunction : ValueSource
	{
		protected internal readonly ValueSource source;

		public SingleFunction(ValueSource source)
		{
			this.source = source;
		}

		protected internal abstract string Name();

		public override string Description()
		{
			return Name() + '(' + source.Description() + ')';
		}

		public override int GetHashCode()
		{
			return source.GetHashCode() + Name().GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (this.GetType() != o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.SingleFunction other = (Org.Apache.Lucene.Queries.Function.Valuesource.SingleFunction
				)o;
			return this.Name().Equals(other.Name()) && this.source.Equals(other.source);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			source.CreateWeight(context, searcher);
		}
	}
}
