/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Config
{
	/// <summary>
	/// Configuration parameters for
	/// <see cref="Org.Apache.Lucene.Search.FuzzyQuery">Org.Apache.Lucene.Search.FuzzyQuery
	/// 	</see>
	/// s
	/// </summary>
	public class FuzzyConfig
	{
		private int prefixLength = FuzzyQuery.defaultPrefixLength;

		private float minSimilarity = FuzzyQuery.defaultMinSimilarity;

		public FuzzyConfig()
		{
		}

		public virtual int GetPrefixLength()
		{
			return prefixLength;
		}

		public virtual void SetPrefixLength(int prefixLength)
		{
			this.prefixLength = prefixLength;
		}

		public virtual float GetMinSimilarity()
		{
			return minSimilarity;
		}

		public virtual void SetMinSimilarity(float minSimilarity)
		{
			this.minSimilarity = minSimilarity;
		}
	}
}
