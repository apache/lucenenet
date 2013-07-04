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
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
	
	/// <summary> A query that matches all documents.
	/// 
	/// </summary>
	[Serializable]
	public class MatchAllDocsQuery : Query
	{
		private class MatchAllScorer : Scorer
		{
			private MatchAllDocsQuery parent;

			internal readonly float score;
			private int doc = - 1;
		    private readonly int maxDoc;
		    private readonly IBits liveDocs;
			
			internal MatchAllScorer(MatchAllDocsQuery parent, IndexReader reader, IBits liveDocs, Weight w, float score)
                : base(w)
			{
			    this.parent = parent;
			    this.liveDocs = liveDocs;
			    this.score = score;
			    maxDoc = reader.MaxDoc;
			}

		    public override int DocID
		    {
		        get { return doc; }
		    }

		    public override int NextDoc()
			{
                doc++;
                while (liveDocs != null && doc < maxDoc && !liveDocs[doc])
                {
                    doc++;
                }
                if (doc == maxDoc)
                {
                    doc = NO_MORE_DOCS;
                }
                return doc;
			}
			
			public override float Score()
			{
				return score;
			}

            public override int Freq
            {
                get { return 1; }
            }

			public override int Advance(int target)
			{
				doc = target - 1;
			    return NextDoc();
			}

            public override long Cost
            {
                get { return maxDoc; }
            }
		}
		
		[Serializable]
		private class MatchAllDocsWeight : Weight
		{
			private MatchAllDocsQuery parent;

			private float queryWeight;
			private float queryNorm;
			
			public MatchAllDocsWeight(MatchAllDocsQuery parent, IndexSearcher searcher)
			{
			    this.parent = parent;
			}
			
			public override string ToString()
			{
                return "weight(" + parent + ")";
			}

		    public override Query Query
		    {
                get { return parent; }
		    }

            public override float ValueForNormalization
            {
                get
                {
                    queryWeight = parent.Boost;
                    return queryWeight*queryWeight;
                }
            }

		    public override void Normalize(float queryNorm, float topLevelBoost)
			{
				this.queryNorm = queryNorm * topLevelBoost;
				queryWeight *= this.queryNorm;
			}
			
			public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, IBits acceptDocs)
			{
                return new MatchAllScorer(parent, context.Reader, acceptDocs, this, queryWeight);
			}
			
			public override Explanation Explain(AtomicReaderContext reader, int doc)
			{
                // explain query weight
                Explanation queryExpl = new ComplexExplanation
                  (true, queryWeight, "MatchAllDocsQuery, product of:");
                if (parent.Boost != 1.0f)
                {
                    queryExpl.AddDetail(new Explanation(parent.Boost, "boost"));
                }
                queryExpl.AddDetail(new Explanation(queryNorm, "queryNorm"));

                return queryExpl;
			}
		}
		
		public override Weight CreateWeight(IndexSearcher searcher)
		{
			return new MatchAllDocsWeight(this, searcher);
		}
		
		public override void ExtractTerms(ISet<Term> terms)
		{
		}
		
		public override string ToString(string field)
		{
			var buffer = new StringBuilder();
			buffer.Append("*:*");
			buffer.Append(ToStringUtils.Boost(Boost));
			return buffer.ToString();
		}
		
		public  override bool Equals(object o)
		{
			if (!(o is MatchAllDocsQuery))
				return false;
			var other = (MatchAllDocsQuery) o;
			return Boost == other.Boost;
		}
		
		public override int GetHashCode()
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(Boost), 0) ^ 0x1AA71190;
		}
	}
}