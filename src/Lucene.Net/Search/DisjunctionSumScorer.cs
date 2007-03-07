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
using PriorityQueue = Lucene.Net.Util.PriorityQueue;

namespace Lucene.Net.Search
{
	
	/// <summary>A Scorer for OR like queries, counterpart of Lucene's <code>ConjunctionScorer</code>.
	/// This Scorer implements {@link Scorer#SkipTo(int)} and uses skipTo() on the given Scorers. 
	/// </summary>
	class DisjunctionSumScorer : Scorer
	{
		/// <summary>The number of subscorers. </summary>
		private int nrScorers;
		
		/// <summary>The subscorers. </summary>
		protected internal System.Collections.IList subScorers;
		
		/// <summary>The minimum number of scorers that should match. </summary>
		private int minimumNrMatchers;
		
		/// <summary>The scorerQueue contains all subscorers ordered by their current doc(),
		/// with the minimum at the top.
		/// <br>The scorerQueue is initialized the first time next() or skipTo() is called.
		/// <br>An exhausted scorer is immediately removed from the scorerQueue.
		/// <br>If less than the minimumNrMatchers scorers
		/// remain in the scorerQueue next() and skipTo() return false.
		/// <p>
		/// After each to call to next() or skipTo()
		/// <code>currentSumScore</code> is the total score of the current matching doc,
		/// <code>nrMatchers</code> is the number of matching scorers,
		/// and all scorers are after the matching doc, or are exhausted.
		/// </summary>
		private ScorerQueue scorerQueue = null;
		
		/// <summary>The document number of the current match. </summary>
		private int currentDoc = - 1;
		
		/// <summary>The number of subscorers that provide the current match. </summary>
		protected internal int nrMatchers = - 1;
		
		private float currentScore = System.Single.NaN;
		
		/// <summary>Construct a <code>DisjunctionScorer</code>.</summary>
		/// <param name="subScorers">A collection of at least two subscorers.
		/// </param>
		/// <param name="minimumNrMatchers">The positive minimum number of subscorers that should
		/// match to match this query.
		/// <br>When <code>minimumNrMatchers</code> is bigger than
		/// the number of <code>subScorers</code>,
		/// no matches will be produced.
		/// <br>When minimumNrMatchers equals the number of subScorers,
		/// it more efficient to use <code>ConjunctionScorer</code>.
		/// </param>
		public DisjunctionSumScorer(System.Collections.IList subScorers, int minimumNrMatchers) : base(null)
		{
			
			nrScorers = subScorers.Count;
			
			if (minimumNrMatchers <= 0)
			{
				throw new System.ArgumentException("Minimum nr of matchers must be positive");
			}
			if (nrScorers <= 1)
			{
				throw new System.ArgumentException("There must be at least 2 subScorers");
			}
			
			this.minimumNrMatchers = minimumNrMatchers;
			this.subScorers = subScorers;
		}
		
		/// <summary>Construct a <code>DisjunctionScorer</code>, using one as the minimum number
		/// of matching subscorers.
		/// </summary>
		public DisjunctionSumScorer(System.Collections.IList subScorers) : this(subScorers, 1)
		{
		}
		
		/// <summary>Called the first time next() or skipTo() is called to
		/// initialize <code>scorerQueue</code>.
		/// </summary>
		private void  InitScorerQueue()
		{
			System.Collections.IEnumerator si = subScorers.GetEnumerator();
			scorerQueue = new ScorerQueue(this, nrScorers);
			while (si.MoveNext())
			{
				Scorer se = (Scorer) si.Current;
				if (se.Next())
				{
					// doc() method will be used in scorerQueue.
					scorerQueue.Insert(se);
				}
			}
		}
		
		/// <summary>A <code>PriorityQueue</code> that orders by {@link Scorer#Doc()}. </summary>
		private class ScorerQueue : PriorityQueue
		{
			private void  InitBlock(DisjunctionSumScorer enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private DisjunctionSumScorer enclosingInstance;
			public DisjunctionSumScorer Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal ScorerQueue(DisjunctionSumScorer enclosingInstance, int size)
			{
				InitBlock(enclosingInstance);
				Initialize(size);
			}
			
			public override bool LessThan(System.Object o1, System.Object o2)
			{
				return ((Scorer) o1).Doc() < ((Scorer) o2).Doc();
			}
		}
		
		public override bool Next()
		{
			if (scorerQueue == null)
			{
				InitScorerQueue();
			}
			if (scorerQueue.Size() < minimumNrMatchers)
			{
				return false;
			}
			else
			{
				return AdvanceAfterCurrent();
			}
		}
		
		
		/// <summary>Advance all subscorers after the current document determined by the
		/// top of the <code>scorerQueue</code>.
		/// Repeat until at least the minimum number of subscorers match on the same
		/// document and all subscorers are after that document or are exhausted.
		/// <br>On entry the <code>scorerQueue</code> has at least <code>minimumNrMatchers</code>
		/// available. At least the scorer with the minimum document number will be advanced.
		/// </summary>
		/// <returns> true iff there is a match.
		/// <br>In case there is a match, </code>currentDoc</code>, </code>currentSumScore</code>,
		/// and </code>nrMatchers</code> describe the match.
		/// 
		/// </returns>
		/// <todo>  Investigate whether it is possible to use skipTo() when </todo>
		/// <summary> the minimum number of matchers is bigger than one, ie. try and use the
		/// character of ConjunctionScorer for the minimum number of matchers.
		/// </summary>
		protected internal virtual bool AdvanceAfterCurrent()
		{
			do 
			{
				// repeat until minimum nr of matchers
				Scorer top = (Scorer) scorerQueue.Top();
				currentDoc = top.Doc();
				currentScore = top.Score();
				nrMatchers = 1;
				do 
				{
					// Until all subscorers are after currentDoc
					if (top.Next())
					{
						scorerQueue.AdjustTop();
					}
					else
					{
						scorerQueue.Pop();
						if (scorerQueue.Size() < (minimumNrMatchers - nrMatchers))
						{
							// Not enough subscorers left for a match on this document,
							// and also no more chance of any further match.
							return false;
						}
						if (scorerQueue.Size() == 0)
						{
							break; // nothing more to advance, check for last match.
						}
					}
					top = (Scorer) scorerQueue.Top();
					if (top.Doc() != currentDoc)
					{
						break; // All remaining subscorers are after currentDoc.
					}
					else
					{
						currentScore += top.Score();
						nrMatchers++;
					}
				}
				while (true);
				
				if (nrMatchers >= minimumNrMatchers)
				{
					return true;
				}
				else if (scorerQueue.Size() < minimumNrMatchers)
				{
					return false;
				}
			}
			while (true);
		}
		
		/// <summary>Returns the score of the current document matching the query.
		/// Initially invalid, until {@link #Next()} is called the first time.
		/// </summary>
		public override float Score()
		{
			return currentScore;
		}
		
		public override int Doc()
		{
			return currentDoc;
		}
		
		/// <summary>Returns the number of subscorers matching the current document.
		/// Initially invalid, until {@link #Next()} is called the first time.
		/// </summary>
		public virtual int NrMatchers()
		{
			return nrMatchers;
		}
		
		/// <summary>Skips to the first match beyond the current whose document number is
		/// greater than or equal to a given target.
		/// <br>When this method is used the {@link #Explain(int)} method should not be used.
		/// <br>The implementation uses the skipTo() method on the subscorers.
		/// </summary>
		/// <param name="target">The target document number.
		/// </param>
		/// <returns> true iff there is such a match.
		/// </returns>
		public override bool SkipTo(int target)
		{
			if (scorerQueue == null)
			{
				InitScorerQueue();
			}
			if (scorerQueue.Size() < minimumNrMatchers)
			{
				return false;
			}
			if (target <= currentDoc)
			{
				return true;
			}
			do 
			{
				Scorer top = (Scorer) scorerQueue.Top();
				if (top.Doc() >= target)
				{
					return AdvanceAfterCurrent();
				}
				else if (top.SkipTo(target))
				{
					scorerQueue.AdjustTop();
				}
				else
				{
					scorerQueue.Pop();
					if (scorerQueue.Size() < minimumNrMatchers)
					{
						return false;
					}
				}
			}
			while (true);
		}
		
		/// <summary>Gives and explanation for the score of a given document.</summary>
		/// <todo>  Show the resulting score. See BooleanScorer.explain() on how to do this. </todo>
		public override Explanation Explain(int doc)
		{
			Explanation res = new Explanation();
			res.SetDescription("At least " + minimumNrMatchers + " of");
			System.Collections.IEnumerator ssi = subScorers.GetEnumerator();
			while (ssi.MoveNext())
			{
				res.AddDetail(((Scorer) ssi.Current).Explain(doc));
			}
			return res;
		}
	}
}