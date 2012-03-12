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
	
	/// <summary>Expert: Returned by low-level search implementations.</summary>
	/// <seealso cref="TopDocs">
	/// </seealso>
	[Serializable]
	public class ScoreDoc
	{
		private float score;
		private int doc;
		
		/// <summary>Expert: Constructs a ScoreDoc. </summary>
		public ScoreDoc(int doc, float score)
		{
			this.doc = doc;
			this.score = score;
		}

	    /// <summary>Expert: The score of this document for the query. </summary>
	    public float Score
	    {
	        get { return score; }
	        set { score = value; }
	    }

	    /// <summary>Expert: A hit document's number.</summary>
	    /// <seealso cref="Searcher.Doc(int)">
	    /// </seealso>
	    public int Doc
	    {
	        get { return doc; }
	        set { doc = value; }
	    }

	    // A convenience method for debugging.
		public override System.String ToString()
		{
			return "doc=" + doc + " score=" + score;
		}
	}
}