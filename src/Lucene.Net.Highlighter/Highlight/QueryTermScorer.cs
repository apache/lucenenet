/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Sharpen;

namespace Lucene.Net.Search.Highlight
{
	/// <summary>
	/// <see cref="Scorer">Scorer</see>
	/// implementation which scores text fragments by the number of
	/// unique query terms found. This class uses the
	/// <see cref="QueryTermExtractor">QueryTermExtractor</see>
	/// class to process determine the query terms and their boosts to be used.
	/// </summary>
	public class QueryTermScorer : Scorer
	{
		internal TextFragment currentTextFragment = null;

		internal HashSet<string> uniqueTermsInFragment;

		internal float totalScore = 0;

		internal float maxTermWeight = 0;

		private Dictionary<string, WeightedTerm> termsToFind;

		private CharTermAttribute termAtt;

		/// <param name="query">
		/// a Lucene query (ideally rewritten using query.rewrite before
		/// being passed to this class and the searcher)
		/// </param>
		public QueryTermScorer(Query query) : this(QueryTermExtractor.GetTerms(query))
		{
		}

		/// <param name="query">
		/// a Lucene query (ideally rewritten using query.rewrite before
		/// being passed to this class and the searcher)
		/// </param>
		/// <param name="fieldName">the Field name which is used to match Query terms</param>
		public QueryTermScorer(Query query, string fieldName) : this(QueryTermExtractor.GetTerms
			(query, false, fieldName))
		{
		}

		/// <param name="query">
		/// a Lucene query (ideally rewritten using query.rewrite before
		/// being passed to this class and the searcher)
		/// </param>
		/// <param name="reader">
		/// used to compute IDF which can be used to a) score selected
		/// fragments better b) use graded highlights eg set font color
		/// intensity
		/// </param>
		/// <param name="fieldName">
		/// the field on which Inverse Document Frequency (IDF)
		/// calculations are based
		/// </param>
		public QueryTermScorer(Query query, IndexReader reader, string fieldName) : this(
			QueryTermExtractor.GetIdfWeightedTerms(query, reader, fieldName))
		{
		}

		public QueryTermScorer(WeightedTerm[] weightedTerms)
		{
			// TODO: provide option to boost score of fragments near beginning of document
			// based on fragment.getFragNum()
			termsToFind = new Dictionary<string, WeightedTerm>();
			for (int i = 0; i < weightedTerms.Length; i++)
			{
				WeightedTerm existingTerm = termsToFind.Get(weightedTerms[i].term);
				if ((existingTerm == null) || (existingTerm.weight < weightedTerms[i].weight))
				{
					// if a term is defined more than once, always use the highest scoring
					// weight
					termsToFind.Put(weightedTerms[i].term, weightedTerms[i]);
					maxTermWeight = Math.Max(maxTermWeight, weightedTerms[i].GetWeight());
				}
			}
		}

		public virtual TokenStream Init(TokenStream tokenStream)
		{
			termAtt = tokenStream.AddAttribute<CharTermAttribute>();
			return null;
		}

		public virtual void StartFragment(TextFragment newFragment)
		{
			uniqueTermsInFragment = new HashSet<string>();
			currentTextFragment = newFragment;
			totalScore = 0;
		}

		public virtual float GetTokenScore()
		{
			string termText = termAtt.ToString();
			WeightedTerm queryTerm = termsToFind.Get(termText);
			if (queryTerm == null)
			{
				// not a query term - return
				return 0;
			}
			// found a query term - is it unique in this doc?
			if (!uniqueTermsInFragment.Contains(termText))
			{
				totalScore += queryTerm.GetWeight();
				uniqueTermsInFragment.AddItem(termText);
			}
			return queryTerm.GetWeight();
		}

		public virtual float GetFragmentScore()
		{
			return totalScore;
		}

		public virtual void AllFragmentsProcessed()
		{
		}

		// this class has no special operations to perform at end of processing
		/// <returns>
		/// The highest weighted term (useful for passing to GradientFormatter
		/// to set top end of coloring scale.
		/// </returns>
		public virtual float GetMaxTermWeight()
		{
			return maxTermWeight;
		}
	}
}
