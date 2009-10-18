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

using TermDocs = Lucene.Net.Index.TermDocs;

namespace Lucene.Net.Search
{
	
	/// <summary>Expert: A <code>Scorer</code> for documents matching a <code>Term</code>.</summary>
	sealed public class TermScorer : Scorer
	{
		private Weight weight;
		private TermDocs termDocs;
		private byte[] norms;
		private float weightValue;
		private int doc;
		
		private int[] docs = new int[32]; // buffered doc numbers
		private int[] freqs = new int[32]; // buffered term freqs
		private int pointer;
		private int pointerMax;
		
		private const int SCORE_CACHE_SIZE = 32;
		private float[] scoreCache = new float[SCORE_CACHE_SIZE];
		
		/// <summary>Construct a <code>TermScorer</code>.</summary>
		/// <param name="weight">The weight of the <code>Term</code> in the query.
		/// </param>
		/// <param name="td">An iterator over the documents matching the <code>Term</code>.
		/// </param>
		/// <param name="similarity">The </code>Similarity</code> implementation to be used for score computations.
		/// </param>
		/// <param name="norms">The field norms of the document fields for the <code>Term</code>.
		/// </param>
		public TermScorer(Weight weight, TermDocs td, Similarity similarity, byte[] norms) : base(similarity)
		{
			this.weight = weight;
			this.termDocs = td;
			this.norms = norms;
			this.weightValue = weight.GetValue();
			
			for (int i = 0; i < SCORE_CACHE_SIZE; i++)
				scoreCache[i] = GetSimilarity().Tf(i) * weightValue;
		}
		
		public override void  Score(HitCollector hc)
		{
			Next();
			Score(hc, System.Int32.MaxValue);
		}
		
		protected internal override bool Score(HitCollector c, int end)
		{
			Similarity similarity = GetSimilarity(); // cache sim in local
			float[] normDecoder = Similarity.GetNormDecoder();
			while (doc < end)
			{
				// for docs in window
				int f = freqs[pointer];
				float score = f < SCORE_CACHE_SIZE ? scoreCache[f] : similarity.Tf(f) * weightValue; // cache miss
				
				score *= normDecoder[norms[doc] & 0xFF]; // normalize for field
				
				c.Collect(doc, score); // collect score
				
				if (++pointer >= pointerMax)
				{
					pointerMax = termDocs.Read(docs, freqs); // refill buffers
					if (pointerMax != 0)
					{
						pointer = 0;
					}
					else
					{
						termDocs.Close(); // close stream
						doc = System.Int32.MaxValue; // set to sentinel value
						return false;
					}
				}
				doc = docs[pointer];
			}
			return true;
		}
		
		/// <summary>Returns the current document number matching the query.
		/// Initially invalid, until {@link #next()} is called the first time.
		/// </summary>
		public override int Doc()
		{
			return doc;
		}
		
		/// <summary>Advances to the next document matching the query.
		/// <br>The iterator over the matching documents is buffered using
		/// {@link TermDocs#Read(int[],int[])}.
		/// </summary>
		/// <returns> true iff there is another document matching the query.
		/// </returns>
		public override bool Next()
		{
			pointer++;
			if (pointer >= pointerMax)
			{
				pointerMax = termDocs.Read(docs, freqs); // refill buffer
				if (pointerMax != 0)
				{
					pointer = 0;
				}
				else
				{
					termDocs.Close(); // close stream
					doc = System.Int32.MaxValue; // set to sentinel value
					return false;
				}
			}
			doc = docs[pointer];
			return true;
		}
		
		public override float Score()
		{
			int f = freqs[pointer];
			float raw = f < SCORE_CACHE_SIZE ? scoreCache[f] : GetSimilarity().Tf(f) * weightValue; // cache miss
			
			return raw * Similarity.DecodeNorm(norms[doc]); // normalize for field
		}
		
		/// <summary>Skips to the first match beyond the current whose document number is
		/// greater than or equal to a given target. 
		/// <br>The implementation uses {@link TermDocs#SkipTo(int)}.
		/// </summary>
		/// <param name="target">The target document number.
		/// </param>
		/// <returns> true iff there is such a match.
		/// </returns>
		public override bool SkipTo(int target)
		{
			// first scan in cache
			for (pointer++; pointer < pointerMax; pointer++)
			{
				if (docs[pointer] >= target)
				{
					doc = docs[pointer];
					return true;
				}
			}
			
			// not found in cache, seek underlying stream
			bool result = termDocs.SkipTo(target);
			if (result)
			{
				pointerMax = 1;
				pointer = 0;
				docs[pointer] = doc = termDocs.Doc();
				freqs[pointer] = termDocs.Freq();
			}
			else
			{
				doc = System.Int32.MaxValue;
			}
			return result;
		}
		
		/// <summary>Returns an explanation of the score for a document.
		/// <br>When this method is used, the {@link #next()} method
		/// and the {@link #Score(HitCollector)} method should not be used.
		/// </summary>
		/// <param name="doc">The document number for the explanation.
		/// </param>
		public override Explanation Explain(int doc)
		{
			TermQuery query = (TermQuery) weight.GetQuery();
			Explanation tfExplanation = new Explanation();
			int tf = 0;
			while (pointer < pointerMax)
			{
				if (docs[pointer] == doc)
					tf = freqs[pointer];
				pointer++;
			}
			if (tf == 0)
			{
				if (termDocs.SkipTo(doc))
				{
					if (termDocs.Doc() == doc)
					{
						tf = termDocs.Freq();
					}
				}
			}
			termDocs.Close();
			tfExplanation.SetValue(GetSimilarity().Tf(tf));
			tfExplanation.SetDescription("tf(termFreq(" + query.GetTerm() + ")=" + tf + ")");
			
			return tfExplanation;
		}
		
		/// <summary>Returns a string representation of this <code>TermScorer</code>. </summary>
		public override System.String ToString()
		{
			return "scorer(" + weight + ")";
		}
	}
}