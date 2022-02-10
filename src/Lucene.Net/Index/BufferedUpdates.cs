using J2N.Collections.Generic;
using J2N.Threading.Atomic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using JCG = J2N.Collections.Generic;
using SCG = System.Collections.Generic;

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

    /// <summary>
    /// Holds buffered deletes and updates, by docID, term or query for a
    /// single segment. this is used to hold buffered pending
    /// deletes and updates against the to-be-flushed segment.  Once the
    /// deletes and updates are pushed (on flush in <see cref="DocumentsWriter"/>), they
    /// are converted to a FrozenDeletes instance.
    /// <para/>
    /// NOTE: instances of this class are accessed either via a private
    /// instance on <see cref="DocumentsWriterPerThread"/>, or via sync'd code by
    /// <see cref="DocumentsWriterDeleteQueue"/>
    /// </summary>
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
        internal static readonly int BYTES_PER_DEL_TERM = 9 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 7 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 10 * RamUsageEstimator.NUM_BYTES_INT32;

        /* Rough logic: del docIDs are List<Integer>.  Say list
           allocates ~2X size (2*POINTER).  Integer is OBJ_HEADER
           + int */
        internal static readonly int BYTES_PER_DEL_DOCID = 2 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_INT32;

        /* Rough logic: HashMap has an array[Entry] w/ varying
           load factor (say 2 * POINTER).  Entry is object w/
           Query key, Integer val, int hash, Entry next
           (OBJ_HEADER + 3*POINTER + INT).  Query we often
           undercount (say 24 bytes).  Integer is OBJ_HEADER + INT. */
        internal static readonly int BYTES_PER_DEL_QUERY = 5 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 2 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 2 * RamUsageEstimator.NUM_BYTES_INT32 + 24;

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
        internal static readonly int BYTES_PER_NUMERIC_FIELD_ENTRY = 7 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 3 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + 5 * RamUsageEstimator.NUM_BYTES_INT32 + RamUsageEstimator.NUM_BYTES_SINGLE;

        /* Rough logic: Incremented when we see another Term for an already updated
         * field.
         * LinkedHashMap has an array[Entry] w/ varying load factor
         * (say 2*POINTER). Entry is an object w/ Term key, NumericUpdate val,
         * int hash, Entry next, Entry before, Entry after (OBJ_HEADER + 5*POINTER + INT).
         *
         * Term (key) is counted only as POINTER.
         * NumericUpdate (val) counts its own size and isn't accounted for here.
         */
        internal static readonly int BYTES_PER_NUMERIC_UPDATE_ENTRY = 7 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_INT32;

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
        internal static readonly int BYTES_PER_BINARY_FIELD_ENTRY = 7 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 3 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + 5 * RamUsageEstimator.NUM_BYTES_INT32 + RamUsageEstimator.NUM_BYTES_SINGLE;

        /* Rough logic: Incremented when we see another Term for an already updated
         * field.
         * LinkedHashMap has an array[Entry] w/ varying load factor
         * (say 2*POINTER). Entry is an object w/ Term key, BinaryUpdate val,
         * int hash, Entry next, Entry before, Entry after (OBJ_HEADER + 5*POINTER + INT).
         *
         * Term (key) is counted only as POINTER.
         * BinaryUpdate (val) counts its own size and isn't accounted for here.
         */
        internal static readonly int BYTES_PER_BINARY_UPDATE_ENTRY = 7 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_INT32;

        internal readonly AtomicInt32 numTermDeletes = new AtomicInt32();
        internal readonly AtomicInt32 numNumericUpdates = new AtomicInt32();
        internal readonly AtomicInt32 numBinaryUpdates = new AtomicInt32();
        internal readonly SCG.IDictionary<Term, int> terms = new Dictionary<Term, int>();
        internal readonly SCG.IDictionary<Query, int> queries = new Dictionary<Query, int>();
        internal readonly SCG.IList<int> docIDs = new JCG.List<int>();


        // Map<dvField,Map<updateTerm,NumericUpdate>>
        // For each field we keep an ordered list of NumericUpdates, key'd by the
        // update Term. LinkedHashMap guarantees we will later traverse the map in
        // insertion order (so that if two terms affect the same document, the last
        // one that came in wins), and helps us detect faster if the same Term is
        // used to update the same field multiple times (so we later traverse it
        // only once).
        internal readonly SCG.IDictionary<string, LinkedDictionary<Term, NumericDocValuesUpdate>> numericUpdates = new Dictionary<string, LinkedDictionary<Term, NumericDocValuesUpdate>>();

        // Map<dvField,Map<updateTerm,BinaryUpdate>>
        // For each field we keep an ordered list of BinaryUpdates, key'd by the
        // update Term. LinkedHashMap guarantees we will later traverse the map in
        // insertion order (so that if two terms affect the same document, the last
        // one that came in wins), and helps us detect faster if the same Term is
        // used to update the same field multiple times (so we later traverse it
        // only once).
        internal readonly SCG.IDictionary<string, LinkedDictionary<Term, BinaryDocValuesUpdate>> binaryUpdates = new Dictionary<string, LinkedDictionary<Term, BinaryDocValuesUpdate>>();

        /// <summary>
        /// NOTE: This was MAX_INT in Lucene
        /// </summary>
        internal static readonly int MAX_INT32 = int.MaxValue; // LUCENENET specific - Made internal rather than public, since this class is intended to be internal but couldn't be because it is exposed through a public API

        internal readonly AtomicInt64 bytesUsed;

#pragma warning disable CA1802 // Use literals where appropriate
        private static readonly bool VERBOSE_DELETES = false;
#pragma warning restore CA1802 // Use literals where appropriate

        internal long gen;

        internal BufferedUpdates() // LUCENENET specific - Made internal rather than public, since this class is intended to be internal but couldn't be because it is exposed through a public API
        {
            this.bytesUsed = new AtomicInt64();
        }

        public override string ToString()
        {
            if (VERBOSE_DELETES)
            {
                return "gen=" + gen + " numTerms=" + numTermDeletes + ", terms=" + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", terms) 
                    + ", queries=" + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", queries) + ", docIDs=" + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", docIDs) 
                    + ", numericUpdates=" + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", numericUpdates) 
                    + ", binaryUpdates=" + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", binaryUpdates) + ", bytesUsed=" + bytesUsed;
            }
            else
            {
                string s = "gen=" + gen;
                if (numTermDeletes != 0)
                {
                    s += " " + numTermDeletes.Value + " deleted terms (unique count=" + terms.Count + ")";
                }
                if (queries.Count != 0)
                {
                    s += " " + queries.Count + " deleted queries";
                }
                if (docIDs.Count != 0)
                {
                    s += " " + docIDs.Count + " deleted docIDs";
                }
                if (numNumericUpdates != 0)
                {
                    s += " " + numNumericUpdates + " numeric updates (unique count=" + numericUpdates.Count + ")";
                }
                if (numBinaryUpdates != 0)
                {
                    s += " " + numBinaryUpdates + " binary updates (unique count=" + binaryUpdates.Count + ")";
                }
                if (bytesUsed != 0)
                {
                    s += " bytesUsed=" + bytesUsed;
                }

                return s;
            }
        }

        internal virtual void AddQuery(Query query, int docIDUpto) // LUCENENET specific - Made internal rather than public, since this class is intended to be internal but couldn't be because it is exposed through a public API
        {
            bool prevExists = queries.TryGetValue(query, out _);
            queries[query] = docIDUpto;
            // increment bytes used only if the query wasn't added so far.
            if (!prevExists)
            {
                bytesUsed.AddAndGet(BYTES_PER_DEL_QUERY);
            }
        }

        internal virtual void AddDocID(int docID) // LUCENENET specific - Made internal rather than public, since this class is intended to be internal but couldn't be because it is exposed through a public API
        {
            docIDs.Add(docID);
            bytesUsed.AddAndGet(BYTES_PER_DEL_DOCID);
        }

        internal virtual void AddTerm(Term term, int docIDUpto) // LUCENENET specific - Made internal rather than public, since this class is intended to be internal but couldn't be because it is exposed through a public API
        {
            bool currentExists = terms.TryGetValue(term, out int current);
            if (currentExists && docIDUpto < current)
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

            terms[term] = docIDUpto;
            // note that if current != null then it means there's already a buffered
            // delete on that term, therefore we seem to over-count. this over-counting
            // is done to respect IndexWriterConfig.setMaxBufferedDeleteTerms.
            numTermDeletes.IncrementAndGet();
            if (!currentExists)
            {
                bytesUsed.AddAndGet(BYTES_PER_DEL_TERM + term.Bytes.Length + (RamUsageEstimator.NUM_BYTES_CHAR * term.Field.Length));
            }
        }

        internal virtual void AddNumericUpdate(NumericDocValuesUpdate update, int docIDUpto) // LUCENENET specific - Made internal rather than public, since this class is intended to be internal but couldn't be because it is exposed through a public API
        {
            if (!numericUpdates.TryGetValue(update.field, out LinkedDictionary<Term, NumericDocValuesUpdate> fieldUpdates))
            {
                fieldUpdates = new LinkedDictionary<Term, NumericDocValuesUpdate>();
                numericUpdates[update.field] = fieldUpdates;
                bytesUsed.AddAndGet(BYTES_PER_NUMERIC_FIELD_ENTRY);
            }

            if (fieldUpdates.TryGetValue(update.term, out NumericDocValuesUpdate current) && current != null && docIDUpto < current.docIDUpto)
            {
                // Only record the new number if it's greater than or equal to the current
                // one. this is important because if multiple threads are replacing the
                // same doc at nearly the same time, it's possible that one thread that
                // got a higher docID is scheduled before the other threads.
                return;
            }

            update.docIDUpto = docIDUpto;
            // since it's an LinkedHashMap, we must first remove the Term entry so that
            // it's added last (we're interested in insertion-order).
            if (current != null)
            {
                fieldUpdates.Remove(update.term);
            }
            fieldUpdates[update.term] = update;
            numNumericUpdates.IncrementAndGet();
            if (current is null)
            {
                bytesUsed.AddAndGet(BYTES_PER_NUMERIC_UPDATE_ENTRY + update.GetSizeInBytes());
            }
        }

        internal virtual void AddBinaryUpdate(BinaryDocValuesUpdate update, int docIDUpto) // LUCENENET specific - Made internal rather than public, since this class is intended to be internal but couldn't be because it is exposed through a public API
        {
            if (!binaryUpdates.TryGetValue(update.field, out LinkedDictionary<Term, BinaryDocValuesUpdate> fieldUpdates))
            {
                fieldUpdates = new LinkedDictionary<Term, BinaryDocValuesUpdate>();
                binaryUpdates[update.field] = fieldUpdates;
                bytesUsed.AddAndGet(BYTES_PER_BINARY_FIELD_ENTRY);
            }

            if (fieldUpdates.TryGetValue(update.term, out BinaryDocValuesUpdate current) && current != null && docIDUpto < current.docIDUpto)
            {
                // Only record the new number if it's greater than or equal to the current
                // one. this is important because if multiple threads are replacing the
                // same doc at nearly the same time, it's possible that one thread that
                // got a higher docID is scheduled before the other threads.
                return;
            }

            update.docIDUpto = docIDUpto;
            // since it's an LinkedHashMap, we must first remove the Term entry so that
            // it's added last (we're interested in insertion-order).
            if (current != null)
            {
                fieldUpdates.Remove(update.term);
            }
            fieldUpdates[update.term] = update;
            numBinaryUpdates.IncrementAndGet();
            if (current is null)
            {
                bytesUsed.AddAndGet(BYTES_PER_BINARY_UPDATE_ENTRY + update.GetSizeInBytes());
            }
        }

        internal virtual void Clear()
        {
            terms.Clear();
            queries.Clear();
            docIDs.Clear();
            numericUpdates.Clear();
            binaryUpdates.Clear();
            numTermDeletes.Value = 0;
            numNumericUpdates.Value = 0;
            numBinaryUpdates.Value = 0;
            bytesUsed.Value = 0;
        }

        internal virtual bool Any()
        {
            return terms.Count > 0 || docIDs.Count > 0 || queries.Count > 0 || numericUpdates.Count > 0 || binaryUpdates.Count > 0;
        }
    }
}