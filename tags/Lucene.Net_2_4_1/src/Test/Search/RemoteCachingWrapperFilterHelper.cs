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

using NUnit.Framework;

using IndexReader = Lucene.Net.Index.IndexReader;

namespace Lucene.Net.Search
{
	
	/// <summary> A unit test helper class to help with RemoteCachingWrapperFilter testing and
	/// assert that it is working correctly.
	/// </summary>
	[Serializable]
	public class RemoteCachingWrapperFilterHelper : RemoteCachingWrapperFilter
	{
		
		private bool shouldHaveCache;
		
		public RemoteCachingWrapperFilterHelper(Filter filter, bool shouldHaveCache) : base(filter)
		{
			this.shouldHaveCache = shouldHaveCache;
		}
		
		public virtual void  ShouldHaveCache(bool shouldHaveCache)
		{
			this.shouldHaveCache = shouldHaveCache;
		}

        public override DocIdSet GetDocIdSet(IndexReader reader)
        {
            Filter cachedFilter = FilterManager.GetInstance().GetFilter(filter);

            Assert.IsNotNull(cachedFilter, "Filter should not be null");
            if (!shouldHaveCache)
            {
                Assert.AreSame(filter, cachedFilter, "First time filter should be the same ");
            }
            else
            {
                Assert.AreNotSame(filter, cachedFilter, "We should have a cached version of the filter");
            }

            if (filter is CachingWrapperFilterHelper)
            {
                ((CachingWrapperFilterHelper)cachedFilter).SetShouldHaveCache(shouldHaveCache);
            }
            return cachedFilter.GetDocIdSet(reader);
        }
    }
}