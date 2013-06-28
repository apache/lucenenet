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

using Lucene.Net.Index;

namespace Lucene.Net.Search
{
	
	/// <summary> Position of a term in a document that takes into account the term offset within the phrase. </summary>
	sealed class PhrasePositions
	{
		internal int doc; // current doc
		internal int position; // position in doc
		internal int count; // remaining pos in this doc
		internal int offset; // position in phrase
	    internal readonly int ord;
		internal readonly DocsAndPositionsEnum postings; // stream of positions
		internal PhrasePositions next; // used to make lists
	    internal int rptGroup = -1;
	    internal int rptInd;
	    internal readonly Term[] terms;

        internal PhrasePositions(DocsAndPositionsEnum postings, int o, int ord, Term[] terms)
        {
            this.postings = postings;
            offset = o;
            this.ord = ord;
            this.terms = terms;
        }
		
		internal bool Next()
		{
		    doc = postings.NextDoc();
            return doc != DocIdSetIterator.NO_MORE_DOCS;
		}
		
		internal bool SkipTo(int target)
		{
		    doc = postings.Advance(target);
		    return doc != DocIdSetIterator.NO_MORE_DOCS;
		}
		
		
		internal void  FirstPosition()
		{
		    count = postings.Freq;
		    NextPosition();
		}
		
		/// <summary> Go to next location of this term current document, and set 
		/// <c>position</c> as <c>location - offset</c>, so that a 
		/// matching exact phrase is easily identified when all PhrasePositions 
		/// have exactly the same <c>position</c>.
		/// </summary>
		internal bool NextPosition()
		{
			if (count-- > 0)
			{
			    position = postings.NextPosition() - offset;
			    return true;
			}
			else
			{
			    return false;
			}
		}

        public override string ToString()
        {
            var s = "d:" + doc + " o:" + offset + " p:" + position + " c:" + count;
            if (rptGroup >= 0)
            {
                s += " rpt:" + rptGroup + ",i" + rptInd;
            }
            return s;
        }
	}
}