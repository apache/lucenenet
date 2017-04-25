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

    public static class DictionaryExtensions
    {
        public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> kvps)
        {
            foreach (var kvp in kvps)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        // LUCENENET TODO: Maybe factor this out? Dictionaries already expose their entries and there is
        // little point in putting them into a set just so you can enumerate them.
        public static ISet<KeyValuePair<TKey, TValue>> EntrySet<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            ISet<KeyValuePair<TKey, TValue>> iset = new HashSet<KeyValuePair<TKey, TValue>>();
            foreach (KeyValuePair<TKey, TValue> kvp in dict)
            {
                iset.Add(kvp);
            }
            return iset;
        }

        public static TValue Put<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict == null)
                return default(TValue);

            var oldValue = dict.ContainsKey(key) ? dict[key] : default(TValue);
            dict[key] = value;
            return oldValue;
        }
    }
}