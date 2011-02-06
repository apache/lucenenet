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

using IndexReader = Lucene.Net.Index.IndexReader;
using Query = Lucene.Net.Search.Query;
using Weight = Lucene.Net.Search.Weight;
using Searcher = Lucene.Net.Search.Searcher;

using System.Collections.Generic;

namespace Lucene.Net.Search.Spans
{
	
	/// <summary>Base class for span-based queries. </summary>
	[System.Serializable]
	public abstract class SpanQuery : Query
	{
		/// <summary>Expert: Returns the matches for this query in an index.  Used internally
		/// to search for spans. 
		/// </summary>
		public abstract Spans GetSpans(IndexReader reader);

        /// <summary>
        /// Returns the matches for this query in an index, including access to any payloads
        /// at thos positions.  Implementin classes that want access to the payloads will need
        /// to implement this.
        /// <para>
        /// WARNING: The status of the Payloads feature is experimental.
        /// The APIs introduced here might change in the future and will not be
        /// supported anymore in such a cse.
        /// </para>
        /// </summary>
        /// <param name="reader">the reader to use to access spans/payloads</param>
        /// <returns>null</returns>
        public virtual PayloadSpans GetPayloadSpans(IndexReader reader)
        {
            return null;
        }

		/// <summary>Returns the name of the field matched by this query.</summary>
		public abstract System.String GetField();
		
		/// <summary>Returns a collection of all terms matched by this query.</summary>
		/// <deprecated> use extractTerms instead
		/// </deprecated>
		/// <seealso cref="Query#ExtractTerms(Set)">
		/// </seealso>
		public abstract System.Collections.ICollection GetTerms();
		
		protected internal override Weight CreateWeight(Searcher searcher)
		{
			return new SpanWeight(this, searcher);
		}

        public Weight CreateWeight_ForNUnitTest(Searcher searcher)
        {
            return new SpanWeight(this, searcher);
        }
    }
}