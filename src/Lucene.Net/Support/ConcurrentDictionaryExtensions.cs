#if !FEATURE_CONCURRENTDICTIONARY_TRYREMOVE_KEYVALUEPAIR
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lucene.Net.Support
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
    /// Polyfill for <see cref="ConcurrentDictionary{TKey,TValue}"/>'s
    /// <c>TryRemove(KeyValuePair&lt;TKey,TValue&gt;)</c> overload, which was
    /// introduced in .NET 5. On earlier targets we fall back to the
    /// <see cref="ICollection{T}"/> interface implementation, which provides
    /// the same atomic "remove only if key and value both match" semantics.
    /// </summary>
    internal static class ConcurrentDictionaryExtensions
    {
        public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(item);
        }
    }
}
#endif
