using J2N.Text;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using IOContext = Lucene.Net.Store.IOContext;
    using Query = Lucene.Net.Search.Query;
    using QueryWrapperFilter = Lucene.Net.Search.QueryWrapperFilter;

    /// <summary>
    /// Tracks the stream of BufferedDeletes.
    /// When <see cref="DocumentsWriterPerThread"/> flushes, its buffered
    /// deletes and updates are appended to this stream.  We later
    /// apply them (resolve them to the actual
    /// docIDs, per segment) when a merge is started
    /// (only to the to-be-merged segments).  We
    /// also apply to all segments when NRT reader is pulled,
    /// commit/close is called, or when too many deletes or  updates are
    /// buffered and must be flushed (by RAM usage or by count).
    /// <para/>
    /// Each packet is assigned a generation, and each flushed or
    /// merged segment is also assigned a generation, so we can
    /// track which BufferedDeletes packets to apply to any given
    /// segment.
    /// </summary>
    internal class BufferedUpdatesStream
    {
        // TODO: maybe linked list?
        private readonly JCG.List<FrozenBufferedUpdates> updates = new JCG.List<FrozenBufferedUpdates>();

        // Starts at 1 so that SegmentInfos that have never had
        // deletes applied (whose bufferedDelGen defaults to 0)
        // will be correct:
        private long nextGen = 1;

        // used only by assert
        private Term lastDeleteTerm;

        private readonly InfoStream infoStream;
        private readonly AtomicInt64 bytesUsed = new AtomicInt64();
        private readonly AtomicInt32 numTerms = new AtomicInt32();

        public BufferedUpdatesStream(InfoStream infoStream)
        {
            this.infoStream = infoStream;
        }

        /// <summary>
        /// Appends a new packet of buffered deletes to the stream,
        /// setting its generation:
        /// </summary>
        public virtual long Push(FrozenBufferedUpdates packet)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                /*
                 * The insert operation must be atomic. If we let threads increment the gen
                 * and push the packet afterwards we risk that packets are out of order.
                 * With DWPT this is possible if two or more flushes are racing for pushing
                 * updates. If the pushed packets get our of order would loose documents
                 * since deletes are applied to the wrong segments.
                 */
                packet.DelGen = nextGen++;
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(packet.Any());
                    Debugging.Assert(CheckDeleteStats());
                    Debugging.Assert(packet.DelGen < nextGen);
                    Debugging.Assert(updates.Count == 0 || updates[updates.Count - 1].DelGen < packet.DelGen, "Delete packets must be in order");
                }
                updates.Add(packet);
                numTerms.AddAndGet(packet.numTermDeletes);
                bytesUsed.AddAndGet(packet.bytesUsed);
                if (infoStream.IsEnabled("BD"))
                {
                    infoStream.Message("BD", "push deletes " + packet + " delGen=" + packet.DelGen + " packetCount=" + updates.Count + " totBytesUsed=" + bytesUsed);
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(CheckDeleteStats());
                return packet.DelGen;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual void Clear()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                updates.Clear();
                nextGen = 1;
                numTerms.Value = 0;
                bytesUsed.Value = 0;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual bool Any() => bytesUsed != 0;

        public virtual int NumTerms => numTerms;

        public virtual long BytesUsed => bytesUsed;

        public class ApplyDeletesResult
        {
            // True if any actual deletes took place:
            public bool AnyDeletes { get; private set; }

            // Current gen, for the merged segment:
            public long Gen { get; private set; }

            // If non-null, contains segments that are 100% deleted
            public IList<SegmentCommitInfo> AllDeleted { get; private set; }

            internal ApplyDeletesResult(bool anyDeletes, long gen, IList<SegmentCommitInfo> allDeleted)
            {
                this.AnyDeletes = anyDeletes;
                this.Gen = gen;
                this.AllDeleted = allDeleted;
            }
        }

        // Sorts SegmentInfos from smallest to biggest bufferedDelGen:
        private static readonly IComparer<SegmentCommitInfo> sortSegInfoByDelGen = Comparer<SegmentCommitInfo>.Create((si1, si2) =>
            {
                long cmp = si1.BufferedDeletesGen - si2.BufferedDeletesGen;
                if (cmp > 0)
                {
                    return 1;
                }
                else if (cmp < 0)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            });

        /// <summary>
        /// Resolves the buffered deleted Term/Query/docIDs, into
        /// actual deleted docIDs in the liveDocs <see cref="Util.IMutableBits"/> for
        /// each <see cref="SegmentReader"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual ApplyDeletesResult ApplyDeletesAndUpdates(IndexWriter.ReaderPool readerPool, IList<SegmentCommitInfo> infos)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                long t0 = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

                if (infos.Count == 0)
                {
                    return new ApplyDeletesResult(false, nextGen++, null);
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(CheckDeleteStats());

                if (!Any())
                {
                    if (infoStream.IsEnabled("BD"))
                    {
                        infoStream.Message("BD", "applyDeletes: no deletes; skipping");
                    }
                    return new ApplyDeletesResult(false, nextGen++, null);
                }

                if (infoStream.IsEnabled("BD"))
                {
                    infoStream.Message("BD", "applyDeletes: infos=" + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", infos) + " packetCount=" + updates.Count);
                }

                long gen = nextGen++;

                JCG.List<SegmentCommitInfo> infos2 = new JCG.List<SegmentCommitInfo>();
                infos2.AddRange(infos);
                infos2.Sort(sortSegInfoByDelGen);

                CoalescedUpdates coalescedUpdates = null;
                bool anyNewDeletes = false;

                int infosIDX = infos2.Count - 1;
                int delIDX = updates.Count - 1;

                IList<SegmentCommitInfo> allDeleted = null;

                while (infosIDX >= 0)
                {
                    //System.out.println("BD: cycle delIDX=" + delIDX + " infoIDX=" + infosIDX);

                    FrozenBufferedUpdates packet = delIDX >= 0 ? updates[delIDX] : null;
                    SegmentCommitInfo info = infos2[infosIDX];
                    long segGen = info.BufferedDeletesGen;

                    if (packet != null && segGen < packet.DelGen)
                    {
                        //        System.out.println("  coalesce");
                        if (coalescedUpdates is null)
                        {
                            coalescedUpdates = new CoalescedUpdates();
                        }
                        if (!packet.isSegmentPrivate)
                        {
                            /*
                             * Only coalesce if we are NOT on a segment private del packet: the segment private del packet
                             * must only applied to segments with the same delGen.  Yet, if a segment is already deleted
                             * from the SI since it had no more documents remaining after some del packets younger than
                             * its segPrivate packet (higher delGen) have been applied, the segPrivate packet has not been
                             * removed.
                             */
                            coalescedUpdates.Update(packet);
                        }

                        delIDX--;
                    }
                    else if (packet != null && segGen == packet.DelGen)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(packet.isSegmentPrivate,"Packet and Segments deletegen can only match on a segment private del packet gen={0}", segGen);
                        //System.out.println("  eq");

                        // Lock order: IW -> BD -> RP
                        if (Debugging.AssertsEnabled) Debugging.Assert(readerPool.InfoIsLive(info));
                        ReadersAndUpdates rld = readerPool.Get(info, true);
                        SegmentReader reader = rld.GetReader(IOContext.READ);
                        int delCount = 0;
                        bool segAllDeletes;
                        try
                        {
                            DocValuesFieldUpdates.Container dvUpdates = new DocValuesFieldUpdates.Container();
                            if (coalescedUpdates != null)
                            {
                                //System.out.println("    del coalesced");
                                delCount += (int)ApplyTermDeletes(coalescedUpdates.GetTermsEnumerable(), rld, reader);
                                delCount += (int)ApplyQueryDeletes(coalescedUpdates.GetQueriesEnumerable(), rld, reader);
                                ApplyDocValuesUpdates(coalescedUpdates.numericDVUpdates, rld, reader, dvUpdates);
                                ApplyDocValuesUpdates(coalescedUpdates.binaryDVUpdates, rld, reader, dvUpdates);
                            }
                            //System.out.println("    del exact");
                            // Don't delete by Term here; DocumentsWriterPerThread
                            // already did that on flush:
                            delCount += (int)ApplyQueryDeletes(packet.GetQueriesEnumerable(), rld, reader);
                            ApplyDocValuesUpdates(packet.numericDVUpdates, rld, reader, dvUpdates);
                            ApplyDocValuesUpdates(packet.binaryDVUpdates, rld, reader, dvUpdates);
                            if (dvUpdates.Any())
                            {
                                rld.WriteFieldUpdates(info.Info.Dir, dvUpdates);
                            }
                            int fullDelCount = rld.Info.DelCount + rld.PendingDeleteCount;
                            if (Debugging.AssertsEnabled) Debugging.Assert(fullDelCount <= rld.Info.Info.DocCount);
                            segAllDeletes = fullDelCount == rld.Info.Info.DocCount;
                        }
                        finally
                        {
                            rld.Release(reader);
                            readerPool.Release(rld);
                        }
                        anyNewDeletes |= delCount > 0;

                        if (segAllDeletes)
                        {
                            if (allDeleted is null)
                            {
                                allDeleted = new JCG.List<SegmentCommitInfo>();
                            }
                            allDeleted.Add(info);
                        }

                        if (infoStream.IsEnabled("BD"))
                        {
                            infoStream.Message("BD", "seg=" + info + " segGen=" + segGen + " segDeletes=[" + packet + "]; coalesced deletes=[" + (coalescedUpdates is null ? "null" : coalescedUpdates.ToString()) + "] newDelCount=" + delCount + (segAllDeletes ? " 100% deleted" : ""));
                        }

                        if (coalescedUpdates is null)
                        {
                            coalescedUpdates = new CoalescedUpdates();
                        }

                        /*
                         * Since we are on a segment private del packet we must not
                         * update the coalescedDeletes here! We can simply advance to the
                         * next packet and seginfo.
                         */
                        delIDX--;
                        infosIDX--;
                        info.SetBufferedDeletesGen(gen);
                    }
                    else
                    {
                        //System.out.println("  gt");

                        if (coalescedUpdates != null)
                        {
                            // Lock order: IW -> BD -> RP
                            if (Debugging.AssertsEnabled) Debugging.Assert(readerPool.InfoIsLive(info));
                            ReadersAndUpdates rld = readerPool.Get(info, true);
                            SegmentReader reader = rld.GetReader(IOContext.READ);
                            int delCount = 0;
                            bool segAllDeletes;
                            try
                            {
                                delCount += (int)ApplyTermDeletes(coalescedUpdates.GetTermsEnumerable(), rld, reader);
                                delCount += (int)ApplyQueryDeletes(coalescedUpdates.GetQueriesEnumerable(), rld, reader);
                                DocValuesFieldUpdates.Container dvUpdates = new DocValuesFieldUpdates.Container();
                                ApplyDocValuesUpdates(coalescedUpdates.numericDVUpdates, rld, reader, dvUpdates);
                                ApplyDocValuesUpdates(coalescedUpdates.binaryDVUpdates, rld, reader, dvUpdates);
                                if (dvUpdates.Any())
                                {
                                    rld.WriteFieldUpdates(info.Info.Dir, dvUpdates);
                                }
                                int fullDelCount = rld.Info.DelCount + rld.PendingDeleteCount;
                                if (Debugging.AssertsEnabled) Debugging.Assert(fullDelCount <= rld.Info.Info.DocCount);
                                segAllDeletes = fullDelCount == rld.Info.Info.DocCount;
                            }
                            finally
                            {
                                rld.Release(reader);
                                readerPool.Release(rld);
                            }
                            anyNewDeletes |= delCount > 0;

                            if (segAllDeletes)
                            {
                                if (allDeleted is null)
                                {
                                    allDeleted = new JCG.List<SegmentCommitInfo>();
                                }
                                allDeleted.Add(info);
                            }

                            if (infoStream.IsEnabled("BD"))
                            {
                                infoStream.Message("BD", "seg=" + info + " segGen=" + segGen + " coalesced deletes=[" + coalescedUpdates + "] newDelCount=" + delCount + (segAllDeletes ? " 100% deleted" : ""));
                            }
                        }
                        info.SetBufferedDeletesGen(gen);

                        infosIDX--;
                    }
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(CheckDeleteStats());
                if (infoStream.IsEnabled("BD"))
                {
                    infoStream.Message("BD", "applyDeletes took " + ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - t0) + " msec"); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                }
                // assert infos != segmentInfos || !any() : "infos=" + infos + " segmentInfos=" + segmentInfos + " any=" + any;

                return new ApplyDeletesResult(anyNewDeletes, gen, allDeleted);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal virtual long GetNextGen()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                return nextGen++;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // Lock order IW -> BD

        /// <summary>
        /// Removes any BufferedDeletes that we no longer need to
        /// store because all segments in the index have had the
        /// deletes applied.
        /// </summary>
        public virtual void Prune(SegmentInfos segmentInfos)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(CheckDeleteStats());
                long minGen = long.MaxValue;
                foreach (SegmentCommitInfo info in segmentInfos.Segments)
                {
                    minGen = Math.Min(info.BufferedDeletesGen, minGen);
                }

                if (infoStream.IsEnabled("BD"))
                {
                    infoStream.Message("BD", "prune sis=" + segmentInfos + " minGen=" + minGen + " packetCount=" + updates.Count);
                }
                int limit = updates.Count;
                for (int delIDX = 0; delIDX < limit; delIDX++)
                {
                    if (updates[delIDX].DelGen >= minGen)
                    {
                        Prune(delIDX);
                        if (Debugging.AssertsEnabled) Debugging.Assert(CheckDeleteStats());
                        return;
                    }
                }

                // All deletes pruned
                Prune(limit);
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(!Any());
                    Debugging.Assert(CheckDeleteStats());
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private void Prune(int count)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (count > 0)
                {
                    if (infoStream.IsEnabled("BD"))
                    {
                        infoStream.Message("BD", "pruneDeletes: prune " + count + " packets; " + (updates.Count - count) + " packets remain");
                    }
                    for (int delIDX = 0; delIDX < count; delIDX++)
                    {
                        FrozenBufferedUpdates packet = updates[delIDX];
                        numTerms.AddAndGet(-packet.numTermDeletes);
                        if (Debugging.AssertsEnabled) Debugging.Assert(numTerms >= 0);
                        bytesUsed.AddAndGet(-packet.bytesUsed);
                        if (Debugging.AssertsEnabled) Debugging.Assert(bytesUsed >= 0);
                    }
                    updates.RemoveRange(0, count); // LUCENENET: Checked count parameter for correctness
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // Delete by Term
        private long ApplyTermDeletes(IEnumerable<Term> termsIter, ReadersAndUpdates rld, SegmentReader reader)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                long delCount = 0;
                Fields fields = reader.Fields;
                if (fields is null)
                {
                    // this reader has no postings
                    return 0;
                }

                TermsEnum termsEnum = null;

                string currentField = null;
                DocsEnum docs = null;

                if (Debugging.AssertsEnabled) Debugging.Assert(CheckDeleteTerm(null));

                bool any = false;

                //System.out.println(Thread.currentThread().getName() + " del terms reader=" + reader);
                foreach (Term term in termsIter)
                {
                    // Since we visit terms sorted, we gain performance
                    // by re-using the same TermsEnum and seeking only
                    // forwards
                    if (!string.Equals(term.Field, currentField, StringComparison.Ordinal))
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(currentField is null || currentField.CompareToOrdinal(term.Field) < 0);
                        currentField = term.Field;
                        Terms terms = fields.GetTerms(currentField);
                        if (terms != null)
                        {
                            termsEnum = terms.GetEnumerator(termsEnum);
                        }
                        else
                        {
                            termsEnum = null;
                        }
                    }

                    if (termsEnum is null)
                    {
                        continue;
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(CheckDeleteTerm(term));

                    // System.out.println("  term=" + term);

                    if (termsEnum.SeekExact(term.Bytes))
                    {
                        // we don't need term frequencies for this
                        DocsEnum docsEnum = termsEnum.Docs(rld.LiveDocs, docs, DocsFlags.NONE);
                        //System.out.println("BDS: got docsEnum=" + docsEnum);

                        if (docsEnum != null)
                        {
                            while (true)
                            {
                                int docID = docsEnum.NextDoc();
                                //System.out.println(Thread.currentThread().getName() + " del term=" + term + " doc=" + docID);
                                if (docID == DocIdSetIterator.NO_MORE_DOCS)
                                {
                                    break;
                                }
                                if (!any)
                                {
                                    rld.InitWritableLiveDocs();
                                    any = true;
                                }
                                // NOTE: there is no limit check on the docID
                                // when deleting by Term (unlike by Query)
                                // because on flush we apply all Term deletes to
                                // each segment.  So all Term deleting here is
                                // against prior segments:
                                if (rld.Delete(docID))
                                {
                                    delCount++;
                                }
                            }
                        }
                    }
                }

                return delCount;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // DocValues updates
        private void ApplyDocValuesUpdates<T1>(IEnumerable<T1> updates, ReadersAndUpdates rld, SegmentReader reader, DocValuesFieldUpdates.Container dvUpdatesContainer) where T1 : DocValuesUpdate
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                Fields fields = reader.Fields;
                if (fields is null)
                {
                    // this reader has no postings
                    return;
                }

                // TODO: we can process the updates per DV field, from last to first so that
                // if multiple terms affect same document for the same field, we add an update
                // only once (that of the last term). To do that, we can keep a bitset which
                // marks which documents have already been updated. So e.g. if term T1
                // updates doc 7, and then we process term T2 and it updates doc 7 as well,
                // we don't apply the update since we know T1 came last and therefore wins
                // the update.
                // We can also use that bitset as 'liveDocs' to pass to TermEnum.docs(), so
                // that these documents aren't even returned.

                string currentField = null;
                TermsEnum termsEnum = null;
                DocsEnum docs = null;

                //System.out.println(Thread.currentThread().getName() + " numericDVUpdate reader=" + reader);
                foreach (DocValuesUpdate update in updates)
                {
                    Term term = update.term;
                    int limit = update.docIDUpto;

                    // TODO: we traverse the terms in update order (not term order) so that we
                    // apply the updates in the correct order, i.e. if two terms udpate the
                    // same document, the last one that came in wins, irrespective of the
                    // terms lexical order.
                    // we can apply the updates in terms order if we keep an updatesGen (and
                    // increment it with every update) and attach it to each NumericUpdate. Note
                    // that we cannot rely only on docIDUpto because an app may send two updates
                    // which will get same docIDUpto, yet will still need to respect the order
                    // those updates arrived.

                    if (!string.Equals(term.Field, currentField, StringComparison.Ordinal))
                    {
                        // if we change the code to process updates in terms order, enable this assert
                        //        assert currentField is null || currentField.CompareToOrdinal(term.Field) < 0;
                        currentField = term.Field;
                        Terms terms = fields.GetTerms(currentField);
                        if (terms != null)
                        {
                            termsEnum = terms.GetEnumerator(termsEnum);
                        }
                        else
                        {
                            termsEnum = null;
                            continue; // no terms in that field
                        }
                    }

                    if (termsEnum is null)
                    {
                        continue;
                    }
                    // System.out.println("  term=" + term);

                    if (termsEnum.SeekExact(term.Bytes))
                    {
                        // we don't need term frequencies for this
                        DocsEnum docsEnum = termsEnum.Docs(rld.LiveDocs, docs, DocsFlags.NONE);

                        //System.out.println("BDS: got docsEnum=" + docsEnum);

                        DocValuesFieldUpdates dvUpdates = dvUpdatesContainer.GetUpdates(update.field, update.type);
                        if (dvUpdates is null)
                        {
                            dvUpdates = dvUpdatesContainer.NewUpdates(update.field, update.type, reader.MaxDoc);
                        }
                        int doc;
                        while ((doc = docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            //System.out.println(Thread.currentThread().getName() + " numericDVUpdate term=" + term + " doc=" + docID);
                            if (doc >= limit)
                            {
                                break; // no more docs that can be updated for this term
                            }
                            // LUCENENET specific handling - dvUpdates handles getting the value so we don't
                            // have to deal with boxing/unboxing
                            dvUpdates.AddFromUpdate(doc, update);
                        }
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public class QueryAndLimit
        {
            public Query Query { get; private set; }
            public int Limit { get; private set; }

            public QueryAndLimit(Query query, int limit)
            {
                this.Query = query;
                this.Limit = limit;
            }
        }

        // Delete by query
        private static long ApplyQueryDeletes(IEnumerable<QueryAndLimit> queriesIter, ReadersAndUpdates rld, SegmentReader reader)
        {
            long delCount = 0;
            AtomicReaderContext readerContext = reader.AtomicContext;
            bool any = false;
            foreach (QueryAndLimit ent in queriesIter)
            {
                Query query = ent.Query;
                int limit = ent.Limit;
                DocIdSet docs = (new QueryWrapperFilter(query)).GetDocIdSet(readerContext, reader.LiveDocs);
                if (docs != null)
                {
                    DocIdSetIterator it = docs.GetIterator();
                    if (it != null)
                    {
                        while (true)
                        {
                            int doc = it.NextDoc();
                            if (doc >= limit)
                            {
                                break;
                            }

                            if (!any)
                            {
                                rld.InitWritableLiveDocs();
                                any = true;
                            }

                            if (rld.Delete(doc))
                            {
                                delCount++;
                            }
                        }
                    }
                }
            }

            return delCount;
        }

        // used only by assert
        private bool CheckDeleteTerm(Term term)
        {
            if (term != null)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(lastDeleteTerm is null || term.CompareTo(lastDeleteTerm) > 0, "lastTerm={0} vs term={1}", lastDeleteTerm, term);
            }
            // TODO: we re-use term now in our merged iterable, but we shouldn't clone, instead copy for this assert
            lastDeleteTerm = term is null ? null : new Term(term.Field, BytesRef.DeepCopyOf(term.Bytes));
            return true;
        }

        // only for assert
        private bool CheckDeleteStats()
        {
            int numTerms2 = 0;
            long bytesUsed2 = 0;
            foreach (FrozenBufferedUpdates packet in updates)
            {
                numTerms2 += packet.numTermDeletes;
                bytesUsed2 += packet.bytesUsed;
            }
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(numTerms2 == numTerms,"numTerms2={0} vs {1}", numTerms2, numTerms);
                Debugging.Assert(bytesUsed2 == bytesUsed,"bytesUsed2={0} vs {1}", bytesUsed2, bytesUsed);
            }
            return true;
        }
    }
}