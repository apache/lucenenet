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
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Threading;
using System.Collections.Concurrent;

namespace Lucene.Net.Index
{

    /// <summary>Holds buffered deletes, by docID, term or query.  We
    /// hold two instances of this class: one for the deletes
    /// prior to the last flush, the other for deletes after
    /// the last flush.  This is so if we need to abort
    /// (discard all buffered docs) we can also discard the
    /// buffered deletes yet keep the deletes done during
    /// previously flushed segments. 
    /// </summary>
    internal class BufferedDeletes
    {
        internal static readonly int BYTES_PER_DEL_TERM = 9 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 7 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 10 * RamUsageEstimator.NUM_BYTES_INT;

        internal static readonly int BYTES_PER_DEL_DOCID = 2 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_INT;

        internal static readonly int BYTES_PER_DEL_QUERY = 5 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 2 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 2 * RamUsageEstimator.NUM_BYTES_INT + 24;

        internal int numTermDeletes;
        internal IDictionary<Term, int?> terms = new ConcurrentHashMap<Term, int?>();
        internal IDictionary<Query, int?> queries = new ConcurrentHashMap<Query, int?>();
        internal ISet<int> docIDs = new ConcurrentHashSet<int>();

        public static readonly int MAX_INT = int.MaxValue;

        internal long bytesUsed;

        private static readonly bool VERBOSE_DELETES = false;

        internal long gen;

        public BufferedDeletes()
            : this(0L)
        {
        }

        internal BufferedDeletes(long bytesUsed)
        {
            // .NET port: using long with Interlocked instead of AtomicLong
            //assert bytesUsed != null;
            this.bytesUsed = bytesUsed;
        }

        public override string ToString()
        {
            if (VERBOSE_DELETES)
            {
                return "gen=" + gen + " numTerms=" + numTermDeletes + ", terms=" + terms
                  + ", queries=" + queries + ", docIDs=" + docIDs + ", bytesUsed="
                  + bytesUsed;
            }
            else
            {
                String s = "gen=" + gen;
                if (numTermDeletes != 0)
                {
                    s += " " + numTermDeletes + " deleted terms (unique count=" + terms.Count + ")";
                }
                if (queries.Count != 0)
                {
                    s += " " + queries.Count + " deleted queries";
                }
                if (docIDs.Count != 0)
                {
                    s += " " + docIDs.Count + " deleted docIDs";
                }
                long v = Interlocked.Read(ref bytesUsed);
                if (v != 0)
                {
                    s += " bytesUsed=" + v;
                }

                return s;
            }
        }

        public virtual void AddQuery(Query query, int docIDUpto)
        {
            int? current = queries[query];
            queries[query] = docIDUpto;

            if (current == null)
                Interlocked.Add(ref bytesUsed, BYTES_PER_DEL_QUERY);
        }

        public virtual void AddDocID(int docID)
        {
            docIDs.Add(docID);
            Interlocked.Add(ref bytesUsed, BYTES_PER_DEL_DOCID);
        }

        public virtual void AddTerm(Term term, int docIDUpto)
        {
            int? current = terms[term];
            if (current != null && docIDUpto < current)
            {
                // Only record the new number if it's greater than the
                // current one.  This is important because if multiple
                // threads are replacing the same doc at nearly the
                // same time, it's possible that one thread that got a
                // higher docID is scheduled before the other
                // threads.  If we blindly replace than we can
                // incorrectly get both docs indexed.
                return;
            }

            terms[term] = docIDUpto;
            Interlocked.Increment(ref numTermDeletes);
            if (current == null)
            {
                Interlocked.Add(ref bytesUsed, BYTES_PER_DEL_TERM + term.Bytes.length + (RamUsageEstimator.NUM_BYTES_CHAR * term.Field.Length));
            }
        }

        internal virtual void Clear()
        {
            terms.Clear();
            queries.Clear();
            docIDs.Clear();
            Interlocked.Exchange(ref numTermDeletes, 0);
            Interlocked.Exchange(ref bytesUsed, 0);
        }

        internal virtual void ClearDocIDs()
        {
            Interlocked.Add(ref bytesUsed, -docIDs.Count * BYTES_PER_DEL_DOCID);
            docIDs.Clear();
        }

        internal virtual bool Any()
        {
            return terms.Count > 0 || docIDs.Count > 0 || queries.Count > 0;
        }

    }
}