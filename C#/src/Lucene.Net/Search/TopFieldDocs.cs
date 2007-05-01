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
	
	
	/// <summary> Expert: Returned by low-level sorted search implementations.
	/// 
	/// <p>Created: Feb 12, 2004 8:58:46 AM 
	/// 
	/// </summary>
	/// <author>   Tim Jones (Nacimiento Software)
	/// </author>
	/// <since>   lucene 1.4
	/// </since>
	/// <version>  $Id: TopFieldDocs.java 472959 2006-11-09 16:21:50Z yonik $
	/// </version>
	/// <seealso cref="Searcher#Search(Query,Filter,int,Sort)">
	/// </seealso>
	[Serializable]
	public class TopFieldDocs : TopDocs
	{
		
		/// <summary>The fields which were used to sort results by. </summary>
		public SortField[] fields;
		
		/// <summary>Creates one of these objects.</summary>
		/// <param name="totalHits"> Total number of hits for the query.
		/// </param>
		/// <param name="scoreDocs"> The top hits for the query.
		/// </param>
		/// <param name="fields">    The sort criteria used to find the top hits.
		/// </param>
		/// <param name="maxScore">  The maximum score encountered.
		/// </param>
		internal TopFieldDocs(int totalHits, ScoreDoc[] scoreDocs, SortField[] fields, float maxScore) : base(totalHits, scoreDocs, maxScore)
		{
			this.fields = fields;
		}
	}
}