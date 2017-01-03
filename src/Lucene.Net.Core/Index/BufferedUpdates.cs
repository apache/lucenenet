using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Lucene.Net.Index
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

    using BinaryDocValuesUpdate = Lucene.Net.Index.DocValuesUpdate.BinaryDocValuesUpdate;
    using NumericDocValuesUpdate = Lucene.Net.Index.DocValuesUpdate.NumericDocValuesUpdate;
    using Query = Lucene.Net.Search.Query;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /* Holds buffered deletes and updates, by docID, term or query for a
     * single segment. this is used to hold buffered pending
     * deletes and updates against the to-be-flushed segment.  Once the
     * deletes and updates are pushed (on flush in DocumentsWriter), they
     * are converted to a FrozenDeletes instance. */

    // NOTE: instances of this class are accessed either via a private
    // instance on DocumentWriterPerThread, or via sync'd code by
    // DocumentsWriterDeleteQueue

    public class BufferedUpdates // LUCENENET NOTE: Made public rather than internal because it is available through a public API
    {
        /* Rough logic: HashMap has an array[Entry] w/ varying
           load factor (say 2 * POINTER).  Entry is object w/ Term
           key, Integer val, int hash, Entry next
           (OBJ_HEADER + 3*POINTER + INT).  Term is object w/
           String field and String text (OBJ_HEADER + 2*POINTER).
           Term's field is String (OBJ_HEADER + 4*INT + POINTER +
           OBJ_HEADER + string.length*CHAR).
           Term's text is String (OBJ_HEADER + 4*INT + POINTER +
           OBJ_HEADER + string.length*CHAR).  Integer is
           OBJ_HEADER + INT. */
        internal static readonly int BYTES_PER_DEL_TERM = 9 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 7 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 10 * RamUsageEstimator.NUM_BYTES_INT;

        /* Rough logic: del docIDs are List<Integer>.  Say list
           allocates ~2X size (2*POINTER).  Integer is OBJ_HEADER
           + int */
        internal static readonly int BYTES_PER_DEL_DOCID = 2 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_INT;

        /* Rough logic: HashMap has an array[Entry] w/ varying
           load factor (say 2 * POINTER).  Entry is object w/
           Query key, Integer val, int hash, Entry next
           (OBJ_HEADER + 3*POINTER + INT).  Query we often
           undercount (say 24 bytes).  Integer is OBJ_HEADER + INT. */
        internal static readonly int BYTES_PER_DEL_QUERY = 5 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 2 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 2 * RamUsageEstimator.NUM_BYTES_INT + 24;

        /* Rough logic: NumericUpdate calculates its actual size,
         * including the update Term and DV field (String). The
         * per-field map holds a reference to the updated field, and
         * therefore we only account for the object reference and
         * map space itself. this is incremented when we first see
         * an updated field.
         *
         * HashMap has an array[Entry] w/ varying load
         * factor (say 2*POINTER). Entry is an object w/ String key,
         * LinkedHashMap val, int hash, Entry next (OBJ_HEADER + 3*POINTER + INT).
         *
         * LinkedHashMap (val) is counted as OBJ_HEADER, array[Entry] ref + header, 4*INT, 1*FLOAT,
         * Set (entrySet) (2*OBJ_HEADER + ARRAY_HEADER + 2*POINTER + 4*INT + FLOAT)
         */
        internal static readonly int BYTES_PER_NUMERIC_FIELD_ENTRY = 7 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 3 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + 5 * RamUsageEstimator.NUM_BYTES_INT + RamUsageEstimator.NUM_BYTES_FLOAT;

        /* Rough logic: Incremented when we see another Term for an already updated
         * field.
         * LinkedHashMap has an array[Entry] w/ varying load factor
         * (say 2*POINTER). Entry is an object w/ Term key, NumericUpdate val,
         * int hash, Entry next, Entry before, Entry after (OBJ_HEADER + 5*POINTER + INT).
         *
         * Term (key) is counted only as POINTER.
         * NumericUpdate (val) counts its own size and isn't accounted for here.
         */
        internal static readonly int BYTES_PER_NUMERIC_UPDATE_ENTRY = 7 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_INT;

        /* Rough logic: BinaryUpdate calculates its actual size,
         * including the update Term and DV field (String). The
         * per-field map holds a reference to the updated field, and
         * therefore we only account for the object reference and
         * map space itself. this is incremented when we first see
         * an updated field.
         *
         * HashMap has an array[Entry] w/ varying load
         * factor (say 2*POINTER). Entry is an object w/ String key,
         * LinkedHashMap val, int hash, Entry next (OBJ_HEADER + 3*POINTER + INT).
         *
         * LinkedHashMap (val) is counted as OBJ_HEADER, array[Entry] ref + header, 4*INT, 1*FLOAT,
         * Set (entrySet) (2*OBJ_HEADER + ARRAY_HEADER + 2*POINTER + 4*INT + FLOAT)
         */
        internal static readonly int BYTES_PER_BINARY_FIELD_ENTRY = 7 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 3 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + 5 * RamUsageEstimator.NUM_BYTES_INT + RamUsageEstimator.NUM_BYTES_FLOAT;

        /* Rough logic: Incremented when we see another Term for an already updated
         * field.
         * LinkedHashMap has an array[Entry] w/ varying load factor
         * (say 2*POINTER). Entry is an object w/ Term key, BinaryUpdate val,
         * int hash, Entry next, Entry before, Entry after (OBJ_HEADER + 5*POINTER + INT).
         *
         * Term (key) is counted only as POINTER.
         * BinaryUpdate (val) counts its own size and isn't accounted for here.
         */
        internal static readonly int BYTES_PER_BINARY_UPDATE_ENTRY = 7 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_INT;

        internal readonly AtomicInteger numTermDeletes = new AtomicInteger();
        internal readonly AtomicInteger numNumericUpdates = new AtomicInteger();
        internal readonly AtomicInteger numBinaryUpdates = new AtomicInteger();
        internal readonly IDictionary<Term, int?> terms = new Dictionary<Term, int?>();
        internal readonly IDictionary<Query, int?> queries = new Dictionary<Query, int?>();
        internal readonly IList<int?> docIDs = new List<int?>();
        

        // Map<dvField,Map<updateTerm,NumericUpdate>>
        // For each field we keep an ordered list of NumericUpdates, key'd by the
        // update Term. OrderedDictionary guarantees we will later traverse the map in
        // insertion order (so that if two terms affect the same document, the last
        // one that came in wins), and helps us detect faster if the same Term is
        // used to update the same field multiple times (so we later traverse it
        // only once).
        internal readonly IDictionary<string, OrderedDictionary> numericUpdates = new Dictionary<string, OrderedDictionary>();

        // Map<dvField,Map<updateTerm,BinaryUpdate>>
        // For each field we keep an ordered list of BinaryUpdates, key'd by the
        // update Term. OrderedDictionary guarantees we will later traverse the map in
        // insertion order (so that if two terms affect the same document, the last
        // one that came in wins), and helps us detect faster if the same Term is
        // used to update the same field multiple times (so we later traverse it
        // only once).
        internal readonly IDictionary<string, OrderedDictionary> binaryUpdates = new Dictionary<string, OrderedDictionary>();

        public static readonly int MAX_INT = Convert.ToInt32(int.MaxValue);

        internal readonly AtomicLong bytesUsed;

        private const bool VERBOSE_DELETES = false;

        internal long gen;

        internal BufferedUpdates() // LUCENENET NOTE: Made internal rather than public, since this class is intended to be internal but couldn't be because it is exposed through a public API
        {
            this.bytesUsed = new AtomicLong();
        }

        public override string ToString()
        {
            if (VERBOSE_DELETES)
            {
                return "gen=" + gen + " numTerms=" + numTermDeletes + ", terms=" + terms + ", queries=" + queries + ", docIDs=" + docIDs + ", numericUpdates=" + numericUpdates + ", binaryUpdates=" + binaryUpdates + ", bytesUsed=" + bytesUsed;
            }
            else
            {
                string s = "gen=" + gen;
                if (numTermDeletes.Get() != 0)
                {
                    s += " " + numTermDeletes.Get() + " deleted terms (unique count=" + terms.Count + ")";
                }
                if (queries.Count != 0)
                {
                    s += " " + queries.Count + " deleted queries";
                }
                if (docIDs.Count != 0)
                {
                    s += " " + docIDs.Count + " deleted docIDs";
                }
                if (numNumericUpdates.Get() != 0)
                {
                    s += " " + numNumericUpdates.Get() + " numeric updates (unique count=" + numericUpdates.Count + ")";
                }
                if (numBinaryUpdates.Get() != 0)
                {
                    s += " " + numBinaryUpdates.Get() + " binary updates (unique count=" + binaryUpdates.Count + ")";
                }
                if (bytesUsed.Get() != 0)
                {
                    s += " bytesUsed=" + bytesUsed.Get();
                }

                return s;
            }
        }

        public virtual void AddQuery(Query query, int docIDUpto)
        {
            int? prev;
            queries.TryGetValue(query, out prev);
            queries[query] = docIDUpto;
            // increment bytes used only if the query wasn't added so far.
            if (prev == null)
            {
                bytesUsed.AddAndGet(BYTES_PER_DEL_QUERY);
            }
        }

        public virtual void AddDocID(int docID)
        {
            docIDs.Add(Convert.ToInt32(docID));
            bytesUsed.AddAndGet(BYTES_PER_DEL_DOCID);
        }

        public virtual void AddTerm(Term term, int docIDUpto)
        {
            int? current;
            terms.TryGetValue(term, out current);
            if (current != null && docIDUpto < current)
            {
                // Only record the new number if it's greater than the
                // current one.  this is important because if multiple
                // threads are replacing the same doc at nearly the
                // same time, it's possible that one thread that got a
                // higher docID is scheduled before the other
                // threads.  If we blindly replace than we can
                // incorrectly get both docs indexed.
                return;
            }

            terms[term] = Convert.ToInt32(docIDUpto);
            // note that if current != null then it means there's already a buffered
            // delete on that term, therefore we seem to over-count. this over-counting
            // is done to respect IndexWriterConfig.setMaxBufferedDeleteTerms.
            numTermDeletes.IncrementAndGet();
            if (current == null)
            {
                bytesUsed.AddAndGet(BYTES_PER_DEL_TERM + term.Bytes.Length + (RamUsageEstimator.NUM_BYTES_CHAR * term.Field.Length));
            }
        }

        public virtual void AddNumericUpdate(NumericDocValuesUpdate update, int docIDUpto)
        {
            OrderedDictionary fieldUpdates = null;
            if (!numericUpdates.TryGetValue(update.field, out fieldUpdates))
            {
                fieldUpdates = new OrderedDictionary();
                numericUpdates[update.field] = fieldUpdates;
                bytesUsed.AddAndGet(BYTES_PER_NUMERIC_FIELD_ENTRY);
            }

            NumericDocValuesUpdate current = null;
            if (fieldUpdates.Contains(update.term))
            {
                current = fieldUpdates[update.term] as NumericDocValuesUpdate;
            }

            if (current != null && docIDUpto < current.docIDUpto)
            {
                // Only record the new number if it's greater than or equal to the current
                // one. this is important because if multiple threads are replacing the
                // same doc at nearly the same time, it's possible that one thread that
                // got a higher docID is scheduled before the other threads.
                return;
            }

            update.docIDUpto = docIDUpto;
            // since it's an OrderedDictionary, we must first remove the Term entry so that
            // it's added last (we're interested in insertion-order).
            if (current != null)
            {
                fieldUpdates.Remove(update.term);
            }
            fieldUpdates[update.term] = update;
            numNumericUpdates.IncrementAndGet();
            if (current == null)
            {
                bytesUsed.AddAndGet(BYTES_PER_NUMERIC_UPDATE_ENTRY + update.SizeInBytes());
            }
        }

        public virtual void AddBinaryUpdate(BinaryDocValuesUpdate update, int docIDUpto)
        {
            OrderedDictionary fieldUpdates;
            if (!binaryUpdates.TryGetValue(update.field, out fieldUpdates))
            {
                fieldUpdates = new OrderedDictionary();
                binaryUpdates[update.field] = fieldUpdates;
                bytesUsed.AddAndGet(BYTES_PER_BINARY_FIELD_ENTRY);
            }

            BinaryDocValuesUpdate current = null;
            if (fieldUpdates.Contains(update.term))
            {
                current = fieldUpdates[update.term] as BinaryDocValuesUpdate;
            }

            if (current != null && docIDUpto < current.docIDUpto)
            {
                // Only record the new number if it's greater than or equal to the current
                // one. this is important because if multiple threads are replacing the
                // same doc at nearly the same time, it's possible that one thread that
                // got a higher docID is scheduled before the other threads.
                return;
            }

            update.docIDUpto = docIDUpto;
            // since it's an OrderedDictionary, we must first remove the Term entry so that
            // it's added last (we're interested in insertion-order).
            if (current != null)
            {
                fieldUpdates.Remove(update.term);
            }
            fieldUpdates[update.term] = update;
            numBinaryUpdates.IncrementAndGet();
            if (current == null)
            {
                bytesUsed.AddAndGet(BYTES_PER_BINARY_UPDATE_ENTRY + update.SizeInBytes());
            }
        }

        internal virtual void Clear()
        {
            terms.Clear();
            queries.Clear();
            docIDs.Clear();
            numericUpdates.Clear();
            binaryUpdates.Clear();
            numTermDeletes.Set(0);
            numNumericUpdates.Set(0);
            numBinaryUpdates.Set(0);
            bytesUsed.Set(0);
        }

        internal virtual bool Any()
        {
            return terms.Count > 0 || docIDs.Count > 0 || queries.Count > 0 || numericUpdates.Count > 0 || binaryUpdates.Count > 0;
        }
    }
}