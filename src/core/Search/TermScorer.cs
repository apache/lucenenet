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

using Lucene.Net.Search.Similarities;
using Lucene.Net.Index;

namespace Lucene.Net.Search
{
	
	/// <summary>Expert: A <c>Scorer</c> for documents matching a <c>Term</c>.</summary>
	public sealed class TermScorer : Scorer
	{
	    private readonly DocsEnum docsEnum;
	    private readonly Similarity.ExactSimScorer docScorer;

	    public TermScorer(Weight weight, DocsEnum td, Similarity.ExactSimScorer docScorer)
	        : base(weight)
	    {
	        this.docScorer = docScorer;
	        this.docsEnum = td;
	    }
		
		public override int DocID
		{
		    get { return docsEnum.DocID; }
		}

	    public override int Freq
	    {
            get { return docsEnum.Freq; }
	    }
		
		/// <summary> Advances to the next document matching the query. <br/>
		/// The iterator over the matching documents is buffered using
		/// <see cref="TermDocs.Read(int[],int[])" />.
		/// 
		/// </summary>
		/// <returns> the document matching the query or -1 if there are no more documents.
		/// </returns>
		public override int NextDoc()
		{
		    return docsEnum.NextDoc();
		}
		
		public override float Score()
		{
			// assert DocID != NO_MORE_DOCS
		    return docScorer.Score(docsEnum.DocID, docsEnum.Freq);
		}
		
		/// <summary> Advances to the first match beyond the current whose document number is
		/// greater than or equal to a given target. <br/>
		/// The implementation uses <see cref="TermDocs.SkipTo(int)" />.
		/// 
		/// </summary>
		/// <param name="target">The target document number.
		/// </param>
		/// <returns> the matching document or -1 if none exist.
		/// </returns>
		public override int Advance(int target)
		{
		    return docsEnum.Advance(target);
		}

        public override long Cost
        {
            get { return docsEnum.Cost; }
        }
		
		/// <summary>Returns a string representation of this <c>TermScorer</c>. </summary>
		public override string ToString()
		{
			return "scorer(" + Weight + ")";
		}
	}
}