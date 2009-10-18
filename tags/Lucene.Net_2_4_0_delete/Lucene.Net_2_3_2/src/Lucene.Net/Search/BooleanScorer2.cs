/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Lucene.Net.Search
{
	
	/// <summary>An alternative to BooleanScorer that also allows a minimum number
	/// of optional scorers that should match.
	/// <br>Implements skipTo(), and has no limitations on the numbers of added scorers.
	/// <br>Uses ConjunctionScorer, DisjunctionScorer, ReqOptScorer and ReqExclScorer.
	/// </summary>
	class BooleanScorer2 : Scorer
	{
		private class AnonymousClassDisjunctionSumScorer : DisjunctionSumScorer
		{
			private void  InitBlock(BooleanScorer2 enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private BooleanScorer2 enclosingInstance;
			public BooleanScorer2 Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal AnonymousClassDisjunctionSumScorer(BooleanScorer2 enclosingInstance, System.Collections.IList Param1, int Param2):base(Param1, Param2)
			{
				InitBlock(enclosingInstance);
			}
			private int lastScoredDoc = - 1;
			public override float Score()
			{
				if (this.Doc() >= lastScoredDoc)
				{
					lastScoredDoc = this.Doc();
					Enclosing_Instance.coordinator.nrMatchers += base.nrMatchers;
				}
				return base.Score();
			}
		}
		
		private class AnonymousClassConjunctionScorer : ConjunctionScorer
		{
			private void  InitBlock(int requiredNrMatchers, BooleanScorer2 enclosingInstance)
			{
				this.requiredNrMatchers = requiredNrMatchers;
				this.enclosingInstance = enclosingInstance;
			}
			
			private int requiredNrMatchers;
			private BooleanScorer2 enclosingInstance;
			
			public BooleanScorer2 Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal AnonymousClassConjunctionScorer(int requiredNrMatchers, BooleanScorer2 enclosingInstance, Lucene.Net.Search.Similarity Param1, System.Collections.ICollection scorers) : base(Param1, scorers)
			{
				InitBlock(requiredNrMatchers, enclosingInstance);
			}
			private int lastScoredDoc = - 1;
			
			public override float Score()
			{
				if (this.Doc() >= lastScoredDoc)
				{
					lastScoredDoc = this.Doc();
					Enclosing_Instance.coordinator.nrMatchers += requiredNrMatchers;
				}
				// All scorers match, so defaultSimilarity super.score() always has 1 as
				// the coordination factor.
				// Therefore the sum of the scores of the requiredScorers
				// is used as score.
				return base.Score();
			}
		}
		private System.Collections.ArrayList requiredScorers = new System.Collections.ArrayList();
		private System.Collections.ArrayList optionalScorers = new System.Collections.ArrayList();
		private System.Collections.ArrayList prohibitedScorers = new System.Collections.ArrayList();
		
		
		private class Coordinator
		{
			public Coordinator(BooleanScorer2 enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(BooleanScorer2 enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private BooleanScorer2 enclosingInstance;
			public BooleanScorer2 Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal int maxCoord = 0; // to be increased for each non prohibited scorer
			
			private float[] coordFactors = null;
			
			internal virtual void  Init()
			{
				// use after all scorers have been added.
				coordFactors = new float[maxCoord + 1];
				Similarity sim = Enclosing_Instance.GetSimilarity();
				for (int i = 0; i <= maxCoord; i++)
				{
					coordFactors[i] = sim.Coord(i, maxCoord);
				}
			}
			
			internal int nrMatchers; // to be increased by score() of match counting scorers.
			
			internal virtual void  InitDoc()
			{
				nrMatchers = 0;
			}
			
			internal virtual float CoordFactor()
			{
				return coordFactors[nrMatchers];
			}
		}
		
		private Coordinator coordinator;
		
		/// <summary>The scorer to which all scoring will be delegated,
		/// except for computing and using the coordination factor.
		/// </summary>
		private Scorer countingSumScorer = null;
		
		/// <summary>The number of optionalScorers that need to match (if there are any) </summary>
		private int minNrShouldMatch;
		
		/// <summary>Whether it is allowed to return documents out of order.
		/// This can accelerate the scoring of disjunction queries.  
		/// </summary>
		private bool allowDocsOutOfOrder;
		
		
		/// <summary>Create a BooleanScorer2.</summary>
		/// <param name="similarity">The similarity to be used.
		/// </param>
		/// <param name="minNrShouldMatch">The minimum number of optional added scorers
		/// that should match during the search.
		/// In case no required scorers are added,
		/// at least one of the optional scorers will have to
		/// match during the search.
		/// </param>
		/// <param name="allowDocsOutOfOrder">Whether it is allowed to return documents out of order.
		/// This can accelerate the scoring of disjunction queries.                         
		/// </param>
		public BooleanScorer2(Similarity similarity, int minNrShouldMatch, bool allowDocsOutOfOrder) : base(similarity)
		{
			if (minNrShouldMatch < 0)
			{
				throw new System.ArgumentException("Minimum number of optional scorers should not be negative");
			}
			coordinator = new Coordinator(this);
			this.minNrShouldMatch = minNrShouldMatch;
			this.allowDocsOutOfOrder = allowDocsOutOfOrder;
		}
		
		/// <summary>Create a BooleanScorer2.
		/// In no required scorers are added,
		/// at least one of the optional scorers will have to match during the search.
		/// </summary>
		/// <param name="similarity">The similarity to be used.
		/// </param>
		/// <param name="minNrShouldMatch">The minimum number of optional added scorers
		/// that should match during the search.
		/// In case no required scorers are added,
		/// at least one of the optional scorers will have to
		/// match during the search.
		/// </param>
		public BooleanScorer2(Similarity similarity, int minNrShouldMatch) : this(similarity, minNrShouldMatch, false)
		{
		}
		
		/// <summary>Create a BooleanScorer2.
		/// In no required scorers are added,
		/// at least one of the optional scorers will have to match during the search.
		/// </summary>
		/// <param name="similarity">The similarity to be used.
		/// </param>
		public BooleanScorer2(Similarity similarity):this(similarity, 0, false)
		{
		}
		
		public virtual void  Add(Scorer scorer, bool required, bool prohibited)
		{
			if (!prohibited)
			{
				coordinator.maxCoord++;
			}
			
			if (required)
			{
				if (prohibited)
				{
					throw new System.ArgumentException("scorer cannot be required and prohibited");
				}
				requiredScorers.Add(scorer);
			}
			else if (prohibited)
			{
				prohibitedScorers.Add(scorer);
			}
			else
			{
				optionalScorers.Add(scorer);
			}
		}
		
		/// <summary>Initialize the match counting scorer that sums all the
		/// scores. <p>
		/// When "counting" is used in a name it means counting the number
		/// of matching scorers.<br>
		/// When "sum" is used in a name it means score value summing
		/// over the matching scorers
		/// </summary>
		private void  InitCountingSumScorer()
		{
			coordinator.Init();
			countingSumScorer = MakeCountingSumScorer();
		}
		
		/// <summary>Count a scorer as a single match. </summary>
		private class SingleMatchScorer : Scorer
		{
			private void  InitBlock(BooleanScorer2 enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private BooleanScorer2 enclosingInstance;
			public BooleanScorer2 Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private Scorer scorer;
			private int lastScoredDoc = - 1;
			
			internal SingleMatchScorer(BooleanScorer2 enclosingInstance, Scorer scorer) : base(scorer.GetSimilarity())
			{
				InitBlock(enclosingInstance);
				this.scorer = scorer;
			}
			public override float Score()
			{
				if (this.Doc() >= lastScoredDoc)
				{
					lastScoredDoc = this.Doc();
					Enclosing_Instance.coordinator.nrMatchers++;
				}
				return scorer.Score();
			}
			public override int Doc()
			{
				return scorer.Doc();
			}
			public override bool Next()
			{
				return scorer.Next();
			}
			public override bool SkipTo(int docNr)
			{
				return scorer.SkipTo(docNr);
			}
			public override Explanation Explain(int docNr)
			{
				return scorer.Explain(docNr);
			}
		}
		
		private Scorer countingDisjunctionSumScorer(System.Collections.IList scorers, int minMrShouldMatch)
		// each scorer from the list counted as a single matcher
		{
			return new AnonymousClassDisjunctionSumScorer(this, scorers, minMrShouldMatch);
		}
		
		private static Similarity defaultSimilarity = new DefaultSimilarity();
		
		private Scorer CountingConjunctionSumScorer(System.Collections.IList requiredScorers)
		{
			// each scorer from the list counted as a single matcher
			int requiredNrMatchers = requiredScorers.Count;
			return new AnonymousClassConjunctionScorer(requiredNrMatchers, this, defaultSimilarity, requiredScorers);
		}
		
		private Scorer DualConjunctionSumScorer(Scorer req1, Scorer req2)
		{
			// non counting. 
			return new ConjunctionScorer(defaultSimilarity, new Scorer[]{req1, req2});
			// All scorers match, so defaultSimilarity always has 1 as
			// the coordination factor.
			// Therefore the sum of the scores of two scorers
			// is used as score.
		}
		
		/// <summary>Returns the scorer to be used for match counting and score summing.
		/// Uses requiredScorers, optionalScorers and prohibitedScorers.
		/// </summary>
		private Scorer MakeCountingSumScorer()
		{
			// each scorer counted as a single matcher
			return (requiredScorers.Count == 0) ? MakeCountingSumScorerNoReq() : MakeCountingSumScorerSomeReq();
		}
		
		private Scorer MakeCountingSumScorerNoReq()
		{
			// No required scorers
			if (optionalScorers.Count == 0)
			{
				return new NonMatchingScorer(); // no clauses or only prohibited clauses
			}
			else
			{
				// No required scorers. At least one optional scorer.
				// minNrShouldMatch optional scorers are required, but at least 1
				int nrOptRequired = (minNrShouldMatch < 1) ? 1 : minNrShouldMatch;
				if (optionalScorers.Count < nrOptRequired)
				{
					return new NonMatchingScorer(); // fewer optional clauses than minimum (at least 1) that should match
				}
				else
				{
					// optionalScorers.size() >= nrOptRequired, no required scorers
					Scorer requiredCountingSumScorer = (optionalScorers.Count > nrOptRequired) ? countingDisjunctionSumScorer(optionalScorers, nrOptRequired) : ((optionalScorers.Count == 1) ? new SingleMatchScorer(this, (Scorer) optionalScorers[0]) : CountingConjunctionSumScorer(optionalScorers));
					return AddProhibitedScorers(requiredCountingSumScorer);
				}
			}
		}
		
		private Scorer MakeCountingSumScorerSomeReq()
		{
			// At least one required scorer.
			if (optionalScorers.Count < minNrShouldMatch)
			{
				return new NonMatchingScorer(); // fewer optional clauses than minimum that should match
			}
			else if (optionalScorers.Count == minNrShouldMatch)
			{
				// all optional scorers also required.
				System.Collections.ArrayList allReq = new System.Collections.ArrayList(requiredScorers);
				allReq.AddRange(optionalScorers);
				return AddProhibitedScorers(CountingConjunctionSumScorer(allReq));
			}
			else
			{
				// optionalScorers.size() > minNrShouldMatch, and at least one required scorer
				Scorer requiredCountingSumScorer = (requiredScorers.Count == 1) ? new SingleMatchScorer(this, (Scorer) requiredScorers[0]) : CountingConjunctionSumScorer(requiredScorers);
				if (minNrShouldMatch > 0)
				{
					// use a required disjunction scorer over the optional scorers
					return AddProhibitedScorers(DualConjunctionSumScorer(requiredCountingSumScorer, countingDisjunctionSumScorer(optionalScorers, minNrShouldMatch)));
				}
				else
				{
					// minNrShouldMatch == 0
					return new ReqOptSumScorer(AddProhibitedScorers(requiredCountingSumScorer), ((optionalScorers.Count == 1) ? new SingleMatchScorer(this, (Scorer) optionalScorers[0]):countingDisjunctionSumScorer(optionalScorers, 1))); // require 1 in combined, optional scorer.
				}
			}
		}
		
		/// <summary>Returns the scorer to be used for match counting and score summing.
		/// Uses the given required scorer and the prohibitedScorers.
		/// </summary>
		/// <param name="requiredCountingSumScorer">A required scorer already built.
		/// </param>
		private Scorer AddProhibitedScorers(Scorer requiredCountingSumScorer)
		{
			return (prohibitedScorers.Count == 0) ? requiredCountingSumScorer : new ReqExclScorer(requiredCountingSumScorer, ((prohibitedScorers.Count == 1) ? (Scorer) prohibitedScorers[0] : new DisjunctionSumScorer(prohibitedScorers)));
		}
		
		/// <summary>Scores and collects all matching documents.</summary>
		/// <param name="hc">The collector to which all matching documents are passed through
		/// {@link HitCollector#Collect(int, float)}.
		/// <br>When this method is used the {@link #Explain(int)} method should not be used.
		/// </param>
		public override void  Score(HitCollector hc)
		{
			if (allowDocsOutOfOrder && requiredScorers.Count == 0 && prohibitedScorers.Count < 32)
			{
				// fall back to BooleanScorer, scores documents somewhat out of order
				BooleanScorer bs = new BooleanScorer(GetSimilarity(), minNrShouldMatch);
				System.Collections.IEnumerator si = optionalScorers.GetEnumerator();
				while (si.MoveNext())
				{
					bs.Add((Scorer) si.Current, false, false);
				}
				si = prohibitedScorers.GetEnumerator();
				while (si.MoveNext())
				{
					bs.Add((Scorer) si.Current, false, true);
				}
				bs.Score(hc);
			}
			else
			{
				if (countingSumScorer == null)
				{
					InitCountingSumScorer();
				}
				while (countingSumScorer.Next())
				{
					hc.Collect(countingSumScorer.Doc(), Score());
				}
			}
		}
		
		/// <summary>Expert: Collects matching documents in a range.
		/// <br>Note that {@link #Next()} must be called once before this method is
		/// called for the first time.
		/// </summary>
		/// <param name="hc">The collector to which all matching documents are passed through
		/// {@link HitCollector#Collect(int, float)}.
		/// </param>
		/// <param name="max">Do not score documents past this.
		/// </param>
		/// <returns> true if more matching documents may remain.
		/// </returns>
		protected internal override bool Score(HitCollector hc, int max)
		{
			// null pointer exception when next() was not called before:
			int docNr = countingSumScorer.Doc();
			while (docNr < max)
			{
				hc.Collect(docNr, Score());
				if (!countingSumScorer.Next())
				{
					return false;
				}
				docNr = countingSumScorer.Doc();
			}
			return true;
		}
		
		public override int Doc()
		{
			return countingSumScorer.Doc();
		}
		
		public override bool Next()
		{
			if (countingSumScorer == null)
			{
				InitCountingSumScorer();
			}
			return countingSumScorer.Next();
		}
		
		public override float Score()
		{
			coordinator.InitDoc();
			float sum = countingSumScorer.Score();
			return sum * coordinator.CoordFactor();
		}
		
		/// <summary>Skips to the first match beyond the current whose document number is
		/// greater than or equal to a given target.
		/// 
		/// <p>When this method is used the {@link #Explain(int)} method should not be used.
		/// 
		/// </summary>
		/// <param name="target">The target document number.
		/// </param>
		/// <returns> true iff there is such a match.
		/// </returns>
		public override bool SkipTo(int target)
		{
			if (countingSumScorer == null)
			{
				InitCountingSumScorer();
			}
			return countingSumScorer.SkipTo(target);
		}
		
		/// <summary>Throws an UnsupportedOperationException.
		/// TODO: Implement an explanation of the coordination factor.
		/// </summary>
		/// <param name="doc">The document number for the explanation.
		/// </param>
		/// <throws>  UnsupportedOperationException </throws>
		public override Explanation Explain(int doc)
		{
			throw new System.NotSupportedException();
			/* How to explain the coordination factor?
			initCountingSumScorer();
			return countingSumScorer.explain(doc); // misses coord factor. 
			*/
		}
	}
}