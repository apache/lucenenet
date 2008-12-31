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

using System.Runtime.InteropServices;
using IndexReader = Lucene.Net.Index.IndexReader;

namespace Lucene.Net.Search
{
	
	/// <summary> Wraps another filter's result and caches it.  The purpose is to allow
	/// filters to simply filter, and then wrap with this class to add caching.
	/// </summary>
	[Serializable]
	public class CachingWrapperFilter : Filter
	{
		protected internal Filter filter;
		
		/// <summary> A transient Filter cache.  To cache Filters even when using {@link RemoteSearchable} use
		/// {@link RemoteCachingWrapperFilter} instead.
		/// </summary>
		[NonSerialized]
		protected internal System.Collections.IDictionary cache;
		
		/// <param name="filter">Filter to cache results of
		/// </param>
		public CachingWrapperFilter(Filter filter)
		{
			this.filter = filter;
		}
		
		public override System.Collections.BitArray Bits(IndexReader reader)
		{
			if (cache == null)
			{
				cache = new SupportClass.WeakHashTable();
			}
			
			lock (cache.SyncRoot)
			{
				// check cache
				System.Collections.BitArray cached = (System.Collections.BitArray) cache[reader];
				if (cached != null)
				{
					return cached;
				}
			}
			
			System.Collections.BitArray bits = filter.Bits(reader);
			
			lock (cache.SyncRoot)
			{
				// update cache
				cache[reader] = bits;
			}
			
			return bits;
		}
		
		public override System.String ToString()
		{
			return "CachingWrapperFilter(" + filter + ")";
		}
		
		public  override bool Equals(System.Object o)
		{
			if (!(o is CachingWrapperFilter))
				return false;
			return this.filter.Equals(((CachingWrapperFilter) o).filter);
		}
		
		public override int GetHashCode()
		{
			return filter.GetHashCode() ^ 0x1117BF25;
		}
	}
}