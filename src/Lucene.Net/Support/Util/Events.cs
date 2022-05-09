#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
using Lucene.Net.Index;
using Prism.Events;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Events are used in Lucene.NET to work around the fact that <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}"/>
    /// doesn't have an enumerator in .NET Framework or .NET Standard prior to 2.1. They are declared in this static class to avoid adding coupling.
    /// </summary>
    internal static class Events
    {
        #region GetParentReaders

        public class GetParentReadersEventArgs
        {
            public IList<IndexReader> ParentReaders { get; } = new List<IndexReader>();
        }

        /// <summary>
        /// Gets strong references to the parent readers of an <see cref="IndexReader"/>
        /// from a <see cref="ConditionalWeakTable{TKey, TValue}"/>.
        /// </summary>
        public class GetParentReadersEvent : PubSubEvent<GetParentReadersEventArgs> { }

        #endregion GetParentReaders

        #region GetCacheKeys

        public class GetCacheKeysEventArgs
        {
            public IList<object> CacheKeys { get; } = new List<object>();
        }

        /// <summary>
        /// Gets strong references to the cache keys in a <see cref="ConditionalWeakTable{TKey, TValue}"/>.
        /// </summary>
        public class GetCacheKeysEvent : PubSubEvent<GetCacheKeysEventArgs> { }

        #endregion GetCacheKeys
    }
}
#endif