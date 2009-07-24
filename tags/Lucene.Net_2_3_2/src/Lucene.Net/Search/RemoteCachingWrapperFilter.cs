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
	
	/// <summary> Provides caching of {@link Filter}s themselves on the remote end of an RMI connection.
	/// The cache is keyed on Filter's hashCode(), so if it sees the same filter twice
	/// it will reuse the original version.
	/// <p/>
	/// NOTE: This does NOT cache the Filter bits, but rather the Filter itself.
	/// Thus, this works hand-in-hand with {@link CachingWrapperFilter} to keep both
	/// file Filter cache and the Filter bits on the remote end, close to the searcher.
	/// <p/>
	/// Usage:
	/// <p/>
	/// To cache a result you must do something like 
	/// RemoteCachingWrapperFilter f = new RemoteCachingWrapperFilter(new CachingWrapperFilter(myFilter));
	/// <p/>
	/// </summary>
	/// <author>  Matt Ericson
	/// </author>
	[Serializable]
	public class RemoteCachingWrapperFilter : Filter
	{
		protected internal Filter filter;
		
		public RemoteCachingWrapperFilter(Filter filter)
		{
			this.filter = filter;
		}
		
		/// <summary> Uses the {@link FilterManager} to keep the cache for a filter on the 
		/// searcher side of a remote connection.
		/// </summary>
		/// <param name="reader">the index reader for the Filter
		/// </param>
		/// <returns> the bitset
		/// </returns>
		public override System.Collections.BitArray Bits(IndexReader reader)
		{
			Filter cachedFilter = FilterManager.GetInstance().GetFilter(filter);
			return cachedFilter.Bits(reader);
		}
	}
}