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

using IndexReader = Lucene.Net.Index.IndexReader;

namespace Lucene.Net.Search
{
	
	/// <summary> Expert: returns a comparator for sorting ScoreDocs.
	/// 
	/// <p>Created: Apr 21, 2004 3:49:28 PM
	/// 
	/// 
	/// </summary>
	/// <version>  $Id: SortComparatorSource.java 564236 2007-08-09 15:21:19Z gsingers $
	/// </version>
	/// <since>   1.4
	/// </since>
	public interface SortComparatorSource
	{
		
		/// <summary> Creates a comparator for the field in the given index.</summary>
		/// <param name="reader">Index to create comparator for.
		/// </param>
		/// <param name="fieldname"> Name of the field to create comparator for.
		/// </param>
		/// <returns> Comparator of ScoreDoc objects.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		ScoreDocComparator NewComparator(IndexReader reader, System.String fieldname);
	}
}