/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Sharpen;

namespace Lucene.Net.Search.Highlight
{
	/// <summary>Lightweight class to hold term and a weight value used for scoring this term
	/// 	</summary>
	public class WeightedTerm
	{
		internal float weight;

		internal string term;

		public WeightedTerm(float weight, string term)
		{
			// multiplier
			//stemmed form
			this.weight = weight;
			this.term = term;
		}

		/// <returns>the term value (stemmed)</returns>
		public virtual string GetTerm()
		{
			return term;
		}

		/// <returns>the weight associated with this term</returns>
		public virtual float GetWeight()
		{
			return weight;
		}

		/// <param name="term">the term value (stemmed)</param>
		public virtual void SetTerm(string term)
		{
			this.term = term;
		}

		/// <param name="weight">the weight associated with this term</param>
		public virtual void SetWeight(float weight)
		{
			this.weight = weight;
		}
	}
}
