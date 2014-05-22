using System.Collections.Generic;

namespace Lucene.Net.Search.Spans
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
	using Term = Lucene.Net.Index.Term;
	using TermContext = Lucene.Net.Index.TermContext;
	using Lucene.Net.Search;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using SimScorer = Lucene.Net.Search.Similarities.Similarity.SimScorer;
	using Bits = Lucene.Net.Util.Bits;


	/// <summary>
	/// Expert-only.  Public for use by other weight implementations
	/// </summary>
	public class SpanWeight : Weight
	{
	  protected internal Similarity Similarity;
	  protected internal IDictionary<Term, TermContext> TermContexts;
	  protected internal SpanQuery Query_Renamed;
	  protected internal Similarity.SimWeight Stats;

	  public SpanWeight(SpanQuery query, IndexSearcher searcher)
	  {
		this.Similarity = searcher.Similarity;
		this.Query_Renamed = query;

		TermContexts = new Dictionary<>();
		SortedSet<Term> terms = new SortedSet<Term>();
		query.ExtractTerms(terms);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.IndexReaderContext context = searcher.getTopReaderContext();
		IndexReaderContext context = searcher.TopReaderContext;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TermStatistics termStats[] = new TermStatistics[terms.size()];
		TermStatistics[] termStats = new TermStatistics[terms.Count];
		int i = 0;
		foreach (Term term in terms)
		{
		  TermContext state = TermContext.Build(context, term);
		  termStats[i] = searcher.TermStatistics(term, state);
		  TermContexts[term] = state;
		  i++;
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String field = query.getField();
		string field = query.Field;
		if (field != null)
		{
		  Stats = Similarity.ComputeWeight(query.Boost, searcher.CollectionStatistics(query.Field), termStats);
		}
	  }

	  public override Query Query
	  {
		  get
		  {
			  return Query_Renamed;
		  }
	  }
	  public override float ValueForNormalization
	  {
		  get
		  {
			return Stats == null ? 1.0f : Stats.ValueForNormalization;
		  }
	  }

	  public override void Normalize(float queryNorm, float topLevelBoost)
	  {
		if (Stats != null)
		{
		  Stats.Normalize(queryNorm, topLevelBoost);
		}
	  }

	  public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
	  {
		if (Stats == null)
		{
		  return null;
		}
		else
		{
		  return new SpanScorer(Query_Renamed.GetSpans(context, acceptDocs, TermContexts), this, Similarity.SimScorer(Stats, context));
		}
	  }

	  public override Explanation Explain(AtomicReaderContext context, int doc)
	  {
		SpanScorer scorer = (SpanScorer) Scorer(context, context.Reader().LiveDocs);
		if (scorer != null)
		{
		  int newDoc = scorer.Advance(doc);
		  if (newDoc == doc)
		  {
			float freq = scorer.SloppyFreq();
			Similarity.SimScorer docScorer = Similarity.SimScorer(Stats, context);
			ComplexExplanation result = new ComplexExplanation();
			result.Description = "weight(" + Query + " in " + doc + ") [" + Similarity.GetType().Name + "], result of:";
			Explanation scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
			result.AddDetail(scoreExplanation);
			result.Value = scoreExplanation.Value;
			result.Match = true;
			return result;
		  }
		}

		return new ComplexExplanation(false, 0.0f, "no matching term");
	  }
	}

}