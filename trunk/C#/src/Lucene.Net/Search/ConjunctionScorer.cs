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
	
	/// <summary>Scorer for conjunctions, sets of queries, all of which are required. </summary>
	class ConjunctionScorer : Scorer
	{
		private class AnonymousClassComparator : System.Collections.IComparer
		{
			public AnonymousClassComparator(ConjunctionScorer enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(ConjunctionScorer enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private ConjunctionScorer enclosingInstance;
			public ConjunctionScorer Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			// sort the array
			public virtual int Compare(System.Object o1, System.Object o2)
			{
				return ((Scorer) o1).Doc() - ((Scorer) o2).Doc();
			}
		}
		private Scorer[] scorers;
		
		private bool firstTime = true;
		private bool more;
		private float coord;
		private int lastDoc = - 1;
		
		public ConjunctionScorer(Similarity similarity, System.Collections.ICollection scorers) : this(similarity, (Scorer[]) new System.Collections.ArrayList(scorers).ToArray(typeof(Scorer)))
		{
		}
		
		public ConjunctionScorer(Similarity similarity, Scorer[] scorers):base(similarity)
		{
			this.scorers = scorers;
			coord = GetSimilarity().Coord(this.scorers.Length, this.scorers.Length);
		}
		
		public override int Doc()
		{
			return lastDoc;
		}
		
		public override bool Next()
		{
			if (firstTime)
				return Init(0);
			else if (more)
				more = scorers[(scorers.Length - 1)].Next();
			return DoNext();
		}
		
		private bool DoNext()
		{
			int first = 0;
			Scorer lastScorer = scorers[scorers.Length - 1];
			Scorer firstScorer;
			while (more && (firstScorer = scorers[first]).Doc() < (lastDoc = lastScorer.Doc()))
			{
				more = firstScorer.SkipTo(lastDoc);
				lastScorer = firstScorer;
				first = (first == (scorers.Length - 1)) ? 0 : first + 1;
			}
			return more;
		}
		
		public override bool SkipTo(int target)
		{
			if (firstTime)
				return Init(target);
			else if (more)
				more = scorers[(scorers.Length - 1)].SkipTo(target);
			return DoNext();
		}
		
		// Note... most of this could be done in the constructor
		// thus skipping a check for firstTime per call to next() and skipTo()
		private bool Init(int target)
		{
			firstTime = false;
			more = scorers.Length > 1;
			for (int i = 0; i < scorers.Length; i++)
			{
				more = target == 0 ? scorers[i].Next() : scorers[i].SkipTo(target);
				if (!more)
					return false;
			}
			
			// Sort the array the first time...
			// We don't need to sort the array in any future calls because we know
			// it will already start off sorted (all scorers on same doc).
			
			// note that this comparator is not consistent with equals!
			System.Array.Sort(scorers, new AnonymousClassComparator(this));
			
			DoNext();
			
			// If first-time skip distance is any predictor of
			// scorer sparseness, then we should always try to skip first on
			// those scorers.
			// Keep last scorer in it's last place (it will be the first
			// to be skipped on), but reverse all of the others so that
			// they will be skipped on in order of original high skip.
			int end = (scorers.Length - 1) - 1;
			for (int i = 0; i < (end >> 1); i++)
			{
				Scorer tmp = scorers[i];
				scorers[i] = scorers[end - i];
				scorers[end - i] = tmp;
			}
			
			return more;
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
		
		public override Explanation Explain(int doc)
		{
			throw new System.NotSupportedException();
		}
	}
}