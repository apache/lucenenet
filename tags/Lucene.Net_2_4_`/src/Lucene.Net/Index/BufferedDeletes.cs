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

using System.Collections.Generic;

namespace Lucene.Net.Index
{
    /// <summary>
    /// Holds buffered deletes, by docID, term or query.  We
    /// hold two instances of this class: one for the deletes
    /// prior to the last flush, the other for deletes after
    /// the last flush.  This is so if we need to abort
    /// (discard all buffered docs) we can also discard the
    /// buffered deletes yet keep the deletes done during
    /// previously flushed segments.
    /// </summary>
    internal class BufferedDeletes
    {
        internal int numTerms;
        internal IDictionary<object, object> terms = new Dictionary<object, object>();
        internal IDictionary<object, object> queries = new Dictionary<object, object>();
        internal IList<object> docIDs = new List<object>();

        // Number of documents a delete term applies to.
        internal sealed class Num
        {
            private int num;

            internal Num(int num)
            {
                this.num = num;
            }

            internal int GetNum()
            {
                return num;
            }

            internal void SetNum(int num)
            {
                // Only record the new number if it's greater than the
                // current one.  This is important because if multiple
                // threads are replacing the same doc at nearly the
                // same time, it's possible that one thread that got a
                // higher docID is scheduled before the other
                // threads.
                if (num > this.num)
                    this.num = num;
            }
        }

        internal void Update(BufferedDeletes in_Renamed)
        {
            numTerms += in_Renamed.numTerms;
            SupportClass.CollectionsSupport.PutAll(in_Renamed.terms, terms);
            SupportClass.CollectionsSupport.PutAll(in_Renamed.queries, queries);
            SupportClass.CollectionsSupport.AddAll(in_Renamed.docIDs, docIDs);
            in_Renamed.terms.Clear();
            in_Renamed.numTerms = 0;
            in_Renamed.queries.Clear();
            in_Renamed.docIDs.Clear();
        }

        internal void Clear()
        {
            terms.Clear();
            queries.Clear();
            docIDs.Clear();
            numTerms = 0;
        }

        internal bool Any()
        {
            return terms.Count > 0 || docIDs.Count > 0 || queries.Count > 0;
        }

        // Remaps all buffered deletes based on a completed
        // merge
        internal void Remap(MergeDocIDRemapper mapper, SegmentInfos infos, int[][] docMaps, int[] delCounts, MergePolicy.OneMerge merge, int mergeDocCount)
        {
            lock (this)
            {
                IDictionary<object, object> newDeleteTerms;

                // Remap delete-by-term
                if (terms.Count > 0)
                {
                    newDeleteTerms = new Dictionary<object, object>();
                    IEnumerator<KeyValuePair<object, object>> iter = terms.GetEnumerator();
                    while (iter.MoveNext())
                    {
                        KeyValuePair<object, object> entry = (KeyValuePair<object, object>)iter.Current;
                        Num num = (Num)entry.Value;
                        newDeleteTerms[entry.Key] = new Num(mapper.Remap(num.GetNum()));
                    }
                }
                else
                    newDeleteTerms = null;

                // Remap delete-by-docID
                IList<object> newDeleteDocIDs;

                if (docIDs.Count > 0)
                {
                    newDeleteDocIDs = new List<object>(docIDs.Count);
                    IEnumerator<object> iter = docIDs.GetEnumerator();
                    while (iter.MoveNext())
                    {
                        int num = (int)iter.Current;
                        newDeleteDocIDs.Add(mapper.Remap(num));
                    }
                }
                else
                    newDeleteDocIDs = null;

                // Remap delete-by-query
                IDictionary<object, object> newDeleteQueries;

                if (queries.Count > 0)
                {
                    newDeleteQueries = new Dictionary<object, object>(queries.Count);
                    IEnumerator<KeyValuePair<object, object>> iter = queries.GetEnumerator();
                    while (iter.MoveNext())
                    {
                        KeyValuePair<object, object> entry = (KeyValuePair<object, object>)iter.Current;
                        int num = (int)entry.Value;
                        newDeleteQueries[entry.Key] = mapper.Remap(num);
                    }
                }
                else
                    newDeleteQueries = null;

                if (newDeleteTerms != null)
                    terms = newDeleteTerms;
                if (newDeleteDocIDs != null)
                    docIDs = newDeleteDocIDs;
                if (newDeleteQueries != null)
                    queries = newDeleteQueries;
            }
        }

    }
}
