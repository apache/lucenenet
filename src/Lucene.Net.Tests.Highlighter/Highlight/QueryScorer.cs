/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Highlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>
	/// <see cref="Scorer">Scorer</see>
	/// implementation which scores text fragments by the number of
	/// unique query terms found. This class converts appropriate
	/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
	/// s to
	/// <see cref="Org.Apache.Lucene.Search.Spans.SpanQuery">Org.Apache.Lucene.Search.Spans.SpanQuery
	/// 	</see>
	/// s and attempts to score only those terms that participated in
	/// generating the 'hit' on the document.
	/// </summary>
	public class QueryScorer : Scorer
	{
		private float totalScore;

		private ICollection<string> foundTerms;

		private IDictionary<string, WeightedSpanTerm> fieldWeightedSpanTerms;

		private float maxTermWeight;

		private int position = -1;

		private string defaultField;

		private CharTermAttribute termAtt;

		private PositionIncrementAttribute posIncAtt;

		private bool expandMultiTermQuery = true;

		private Query query;

		private string field;

		private IndexReader reader;

		private bool skipInitExtractor;

		private bool wrapToCaching = true;

		private int maxCharsToAnalyze;

		/// <param name="query">Query to use for highlighting</param>
		public QueryScorer(Query query)
		{
			Init(query, null, null, true);
		}

		/// <param name="query">Query to use for highlighting</param>
		/// <param name="field">Field to highlight - pass null to ignore fields</param>
		public QueryScorer(Query query, string field)
		{
			Init(query, field, null, true);
		}

		/// <param name="query">Query to use for highlighting</param>
		/// <param name="field">Field to highlight - pass null to ignore fields</param>
		/// <param name="reader">
		/// 
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// to use for quasi tf/idf scoring
		/// </param>
		public QueryScorer(Query query, IndexReader reader, string field)
		{
			Init(query, field, reader, true);
		}

		/// <param name="query">to use for highlighting</param>
		/// <param name="reader">
		/// 
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// to use for quasi tf/idf scoring
		/// </param>
		/// <param name="field">to highlight - pass null to ignore fields</param>
		public QueryScorer(Query query, IndexReader reader, string field, string defaultField
			)
		{
			this.defaultField = defaultField;
			Init(query, field, reader, true);
		}

		/// <param name="defaultField">- The default field for queries with the field name unspecified
		/// 	</param>
		public QueryScorer(Query query, string field, string defaultField)
		{
			this.defaultField = defaultField;
			Init(query, field, null, true);
		}

		/// <param name="weightedTerms">
		/// an array of pre-created
		/// <see cref="WeightedSpanTerm">WeightedSpanTerm</see>
		/// s
		/// </param>
		public QueryScorer(WeightedSpanTerm[] weightedTerms)
		{
			this.fieldWeightedSpanTerms = new Dictionary<string, WeightedSpanTerm>(weightedTerms
				.Length);
			for (int i = 0; i < weightedTerms.Length; i++)
			{
				WeightedSpanTerm existingTerm = fieldWeightedSpanTerms.Get(weightedTerms[i].term);
				if ((existingTerm == null) || (existingTerm.weight < weightedTerms[i].weight))
				{
					// if a term is defined more than once, always use the highest
					// scoring weight
					fieldWeightedSpanTerms.Put(weightedTerms[i].term, weightedTerms[i]);
					maxTermWeight = Math.Max(maxTermWeight, weightedTerms[i].GetWeight());
				}
			}
			skipInitExtractor = true;
		}

		public virtual float GetFragmentScore()
		{
			return totalScore;
		}

		/// <returns>
		/// The highest weighted term (useful for passing to
		/// GradientFormatter to set top end of coloring scale).
		/// </returns>
		public virtual float GetMaxTermWeight()
		{
			return maxTermWeight;
		}

		public virtual float GetTokenScore()
		{
			position += posIncAtt.GetPositionIncrement();
			string termText = termAtt.ToString();
			WeightedSpanTerm weightedSpanTerm;
			if ((weightedSpanTerm = fieldWeightedSpanTerms.Get(termText)) == null)
			{
				return 0;
			}
			if (weightedSpanTerm.positionSensitive && !weightedSpanTerm.CheckPosition(position
				))
			{
				return 0;
			}
			float score = weightedSpanTerm.GetWeight();
			// found a query term - is it unique in this doc?
			if (!foundTerms.Contains(termText))
			{
				totalScore += score;
				foundTerms.AddItem(termText);
			}
			return score;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual TokenStream Init(TokenStream tokenStream)
		{
			position = -1;
			termAtt = tokenStream.AddAttribute<CharTermAttribute>();
			posIncAtt = tokenStream.AddAttribute<PositionIncrementAttribute>();
			if (!skipInitExtractor)
			{
				if (fieldWeightedSpanTerms != null)
				{
					fieldWeightedSpanTerms.Clear();
				}
				return InitExtractor(tokenStream);
			}
			return null;
		}

		/// <summary>
		/// Retrieve the
		/// <see cref="WeightedSpanTerm">WeightedSpanTerm</see>
		/// for the specified token. Useful for passing
		/// Span information to a
		/// <see cref="Fragmenter">Fragmenter</see>
		/// .
		/// </summary>
		/// <param name="token">
		/// to get
		/// <see cref="WeightedSpanTerm">WeightedSpanTerm</see>
		/// for
		/// </param>
		/// <returns>WeightedSpanTerm for token</returns>
		public virtual WeightedSpanTerm GetWeightedSpanTerm(string token)
		{
			return fieldWeightedSpanTerms.Get(token);
		}

		private void Init(Query query, string field, IndexReader reader, bool expandMultiTermQuery
			)
		{
			this.reader = reader;
			this.expandMultiTermQuery = expandMultiTermQuery;
			this.query = query;
			this.field = field;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private TokenStream InitExtractor(TokenStream tokenStream)
		{
			WeightedSpanTermExtractor qse = NewTermExtractor(defaultField);
			qse.SetMaxDocCharsToAnalyze(maxCharsToAnalyze);
			qse.SetExpandMultiTermQuery(expandMultiTermQuery);
			qse.SetWrapIfNotCachingTokenFilter(wrapToCaching);
			if (reader == null)
			{
				this.fieldWeightedSpanTerms = qse.GetWeightedSpanTerms(query, tokenStream, field);
			}
			else
			{
				this.fieldWeightedSpanTerms = qse.GetWeightedSpanTermsWithScores(query, tokenStream
					, field, reader);
			}
			if (qse.IsCachedTokenStream())
			{
				return qse.GetTokenStream();
			}
			return null;
		}

		protected internal virtual WeightedSpanTermExtractor NewTermExtractor(string defaultField
			)
		{
			return defaultField == null ? new WeightedSpanTermExtractor() : new WeightedSpanTermExtractor
				(defaultField);
		}

		public virtual void StartFragment(TextFragment newFragment)
		{
			foundTerms = new HashSet<string>();
			totalScore = 0;
		}

		/// <returns>true if multi-term queries should be expanded</returns>
		public virtual bool IsExpandMultiTermQuery()
		{
			return expandMultiTermQuery;
		}

		/// <summary>
		/// Controls whether or not multi-term queries are expanded
		/// against a
		/// <see cref="Org.Apache.Lucene.Index.Memory.MemoryIndex">Org.Apache.Lucene.Index.Memory.MemoryIndex
		/// 	</see>
		/// 
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="expandMultiTermQuery">true if multi-term queries should be expanded</param>
		public virtual void SetExpandMultiTermQuery(bool expandMultiTermQuery)
		{
			this.expandMultiTermQuery = expandMultiTermQuery;
		}

		/// <summary>
		/// By default,
		/// <see cref="Org.Apache.Lucene.Analysis.TokenStream">Org.Apache.Lucene.Analysis.TokenStream
		/// 	</see>
		/// s that are not of the type
		/// <see cref="Org.Apache.Lucene.Analysis.CachingTokenFilter">Org.Apache.Lucene.Analysis.CachingTokenFilter
		/// 	</see>
		/// are wrapped in a
		/// <see cref="Org.Apache.Lucene.Analysis.CachingTokenFilter">Org.Apache.Lucene.Analysis.CachingTokenFilter
		/// 	</see>
		/// to
		/// ensure an efficient reset - if you are already using a different caching
		/// <see cref="Org.Apache.Lucene.Analysis.TokenStream">Org.Apache.Lucene.Analysis.TokenStream
		/// 	</see>
		/// impl and you don't want it to be wrapped, set this to
		/// false.
		/// </summary>
		public virtual void SetWrapIfNotCachingTokenFilter(bool wrap)
		{
			this.wrapToCaching = wrap;
		}

		public virtual void SetMaxDocCharsToAnalyze(int maxDocCharsToAnalyze)
		{
			this.maxCharsToAnalyze = maxDocCharsToAnalyze;
		}
	}
}
