/**
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

using Lucene.Net.Index;
using System;
using Lucene.Net.Util;
using System.Collections.Generic;

namespace Lucene.Net.Search
{
    public class FastFacetedSearch
    {
        public static KeyValuePair<string, long>[] Search(Query query, IndexSearcher s, string groupByfield, int maxGroupsCount = int.MaxValue)
        {
            var stringIndex = FieldCache_Fields.DEFAULT.GetMultiStringIndex(s.IndexReader, groupByfield);
            var c = new int[stringIndex.lookup.Length];
            var results = new FacetCollector(c, stringIndex);

            s.Search(query, results);
            var queue = new DictionaryEntryQueue(stringIndex.lookup.Length);

            for (int i = 1; i < stringIndex.lookup.Length; i++)
            {
                if (c[i] > 0 && stringIndex.lookup[i] != null && stringIndex.lookup[i] != "0")
                {             
                    queue.InsertWithOverflow(new FacetEntry(stringIndex.lookup[i], -c[i]));
                }
            }

            var resSize = Math.Min(queue.Size(), maxGroupsCount);
            var result = new KeyValuePair<string, long>[resSize];
            for (int i = 0; i < resSize; i++)
            {
                var entry = queue.Pop();
                result[i] = new KeyValuePair<string, long>(entry.Value, -entry.Count);
            }
            return result;
        }

        /// <summary>Helper class for order the resulting array in value order
        /// </summary>
        sealed class DictionaryEntryQueue : PriorityQueue<FacetEntry>
        {
            internal DictionaryEntryQueue(int size)
            {
                Initialize(size);
            }

            public override bool LessThan(FacetEntry a, FacetEntry b)
            {
                return a.Count < b.Count;
            }
        }

        /// <summary>collector that count the hits for every token 
        /// </summary>
        private class FacetCollector : Collector
        {
            private readonly int[] _counter;
            private readonly MultiStringIndex _si;
            private int _baseRenamed;

            public FacetCollector(int[] c, MultiStringIndex s)
            {
                _counter = c;
                _si = s;
            }

            public override void Collect(int doc)
            {
                var arr = _si.order[doc + _baseRenamed];
                if (arr != null)
                {
                    arr.ForEach(i => _counter[i]++);
                }
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }

            public override void SetNextReader(IndexReader reader, int docBase)
            {
                _baseRenamed = docBase;
            }

            public override void SetScorer(Scorer scorer)
            {
            }
        }

        /// <summary>class for work in the priority queue and avoid some boxing. 
        /// </summary>
        public class FacetEntry
        {
            public long Count;
            public string Value;

            public FacetEntry(string v, long c)
            {
                Value = v;
                Count = c;
            }
        }
    }
}



