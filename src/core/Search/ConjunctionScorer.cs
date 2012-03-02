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
using System.Linq;

namespace Lucene.Net.Search
{
	
	/// <summary>Scorer for conjunctions, sets of queries, all of which are required. </summary>
	class ConjunctionScorer:Scorer
	{
		private Scorer[] scorers;
		private float coord;
		private int lastDoc = - 1;
		
		public ConjunctionScorer(Similarity similarity, System.Collections.Generic.ICollection<Scorer> scorers)
            : this(similarity, scorers.ToArray())
		{
		}
		
		public ConjunctionScorer(Similarity similarity, params Scorer[] scorers):base(similarity)
		{
			this.scorers = scorers;
			coord = similarity.Coord(scorers.Length, scorers.Length);
			
			for (int i = 0; i < scorers.Length; i++)
			{
				if (scorers[i].NextDoc() == NO_MORE_DOCS)
				{
					// If even one of the sub-scorers does not have any documents, this
					// scorer should not attempt to do any more work.
					lastDoc = NO_MORE_DOCS;
					return ;
				}
			}
			
			// Sort the array the first time...
			// We don't need to sort the array in any future calls because we know
			// it will already start off sorted (all scorers on same doc).
			
			// note that this comparator is not consistent with equals!
		    System.Array.Sort(scorers, (a, b) => a.DocID() - b.DocID());
			
			// NOTE: doNext() must be called before the re-sorting of the array later on.
			// The reason is this: assume there are 5 scorers, whose first docs are 1,
			// 2, 3, 5, 5 respectively. Sorting (above) leaves the array as is. Calling
			// doNext() here advances all the first scorers to 5 (or a larger doc ID
			// they all agree on). 
			// However, if we re-sort before doNext() is called, the order will be 5, 3,
			// 2, 1, 5 and then doNext() will stop immediately, since the first scorer's
			// docs equals the last one. So the invariant that after calling doNext() 
			// all scorers are on the same doc ID is broken.);
			if (DoNext() == NO_MORE_DOCS)
			{
				// The scorers did not agree on any document.
				lastDoc = NO_MORE_DOCS;
				return ;
			}
			
			// If first-time skip distance is any predictor of
			// scorer sparseness, then we should always try to skip first on
			// those scorers.
			// Keep last scorer in it's last place (it will be the first
			// to be skipped on), but reverse all of the others so that
			// they will be skipped on in order of original high skip.
			int end = scorers.Length - 1;
			int max = end >> 1;
			for (int i = 0; i < max; i++)
			{
				Scorer tmp = scorers[i];
				int idx = end - i - 1;
				scorers[i] = scorers[idx];
				scorers[idx] = tmp;
			}
		}
		
		private int DoNext()
		{
			int first = 0;
			int doc = scorers[scorers.Length - 1].DocID();
			Scorer firstScorer;
			while ((firstScorer = scorers[first]).DocID() < doc)
			{
				doc = firstScorer.Advance(doc);
				first = first == scorers.Length - 1?0:first + 1;
			}
			return doc;
		}
		
		public override int Advance(int target)
		{
			if (lastDoc == NO_MORE_DOCS)
			{
				return lastDoc;
			}
			else if (scorers[(scorers.Length - 1)].DocID() < target)
			{
				scorers[(scorers.Length - 1)].Advance(target);
			}
			return lastDoc = DoNext();
		}
		
		public override int DocID()
		{
			return lastDoc;
		}
		
		public override int NextDoc()
		{
			if (lastDoc == NO_MORE_DOCS)
			{
				return lastDoc;
			}
			else if (lastDoc == - 1)
			{
				return lastDoc = scorers[scorers.Length - 1].DocID();
			}
			scorers[(scorers.Length - 1)].NextDoc();
			return lastDoc = DoNext();
		}
		
		public override float Score()
		{
			float sum = 0.0f;
			for (int i = 0; i < scorers.Length; i++)
			{
				sum += scorers[i].Score();
			}
			return sum * coord;
		}
	}
}