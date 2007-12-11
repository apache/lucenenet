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
		private Scorer[] scorers = new Scorer[2];
		private int length = 0;
		private int first = 0;
		private int last = - 1;
		private bool firstTime = true;
		private bool more = true;
		private float coord;
		
		public ConjunctionScorer(Similarity similarity):base(similarity)
		{
		}
		
		internal void  Add(Scorer scorer)
		{
			if (length >= scorers.Length)
			{
				// grow the array
				Scorer[] temps = new Scorer[scorers.Length * 2];
				Array.Copy(scorers, 0, temps, 0, length);
				scorers = temps;
			}
			last += 1;
			length += 1;
			scorers[last] = scorer;
		}
		
		public override int Doc()
		{
			return scorers[first].Doc();
		}
		
		public override bool Next()
		{
			if (firstTime)
			{
				Init(true);
			}
			else if (more)
			{
				more = scorers[last].Next(); // trigger further scanning
			}
			return DoNext();
		}
		
		private bool DoNext()
		{
			while (more && scorers[first].Doc() < scorers[last].Doc())
			{
				// find doc w/ all clauses
				more = scorers[first].SkipTo(scorers[last].Doc()); // skip first upto last
				last = first; // move first to last
				first = (first == length - 1) ? 0 : first + 1;
			}
			return more; // found a doc with all clauses
		}
		
		public override bool SkipTo(int target)
		{
			if (firstTime)
			{
				Init(false);
			}
			
			for (int i = 0, pos = first; i < length; i++)
			{
				if (!more)
					break;
				more = scorers[pos].SkipTo(target);
				pos = (pos == length - 1) ? 0 : pos + 1;
			}
			
			if (more)
				SortScorers(); // re-sort scorers
			
			return DoNext();
		}
		
		public override float Score()
		{
			float sum = 0.0f;
			for (int i = 0; i < length; i++)
			{
				sum += scorers[i].Score();
			}
			return sum * coord;
		}
		
		private void  Init(bool initScorers)
		{
			//  compute coord factor
			coord = GetSimilarity().Coord(length, length);
			
			more = length > 0;
			
			if (initScorers)
			{
				// move each scorer to its first entry
				for (int i = 0, pos = first; i < length; i++)
				{
					if (!more)
						break;
					more = scorers[pos].Next();
					pos = (pos == length - 1) ? 0 : pos + 1;
				}
				// initial sort of simulated list
				if (more)
					SortScorers();
			}
			
			firstTime = false;
		}
		
		private void  SortScorers()
		{
			// squeeze the array down for the sort
			if (length != scorers.Length)
			{
				Scorer[] temps = new Scorer[length];
				Array.Copy(scorers, 0, temps, 0, length);
				scorers = temps;
			}
			
			// note that this comparator is not consistent with equals!
			System.Array.Sort(scorers, new AnonymousClassComparator(this));
			
			first = 0;
			last = length - 1;
		}
		
		public override Explanation Explain(int doc)
		{
			throw new System.NotSupportedException();
		}
	}
}