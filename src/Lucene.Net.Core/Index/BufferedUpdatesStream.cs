using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using IOContext = Lucene.Net.Store.IOContext;
    using Query = Lucene.Net.Search.Query;
    using QueryWrapperFilter = Lucene.Net.Search.QueryWrapperFilter;

    /* Tracks the stream of {@link BufferedDeletes}.
     * When DocumentsWriterPerThread flushes, its buffered
     * deletes and updates are appended to this stream.  We later
     * apply them (resolve them to the actual
     * docIDs, per segment) when a merge is started
     * (only to the to-be-merged segments).  We
     * also apply to all segments when NRT reader is pulled,
     * commit/close is called, or when too many deletes or  updates are
     * buffered and must be flushed (by RAM usage or by count).
     *
     * Each packet is assigned a generation, and each flushed or
     * merged segment is also assigned a generation, so we can
     * track which BufferedDeletes packets to apply to any given
     * segment. */

    internal class BufferedUpdatesStream
    {
        // TODO: maybe linked list?
        private readonly IList<FrozenBufferedUpdates> Updates = new List<FrozenBufferedUpdates>();

        // Starts at 1 so that SegmentInfos that have never had
        // deletes applied (whose bufferedDelGen defaults to 0)
        // will be correct:
        private long NextGen_Renamed = 1;

        // used only by assert
        private Term LastDeleteTerm;

        private readonly InfoStream InfoStream;
        private readonly AtomicLong bytesUsed = new AtomicLong();
        private readonly AtomicInteger numTerms = new AtomicInteger();

        public BufferedUpdatesStream(InfoStream infoStream)
        {
            this.InfoStream = infoStream;
        }

        // Appends a new packet of buffered deletes to the stream,
        // setting its generation:
        public virtual long Push(FrozenBufferedUpdates packet)
        {
            lock (this)
            {
                /*
                 * The insert operation must be atomic. If we let threads increment the gen
                 * and push the packet afterwards we risk that packets are out of order.
                 * With DWPT this is possible if two or more flushes are racing for pushing
                 * updates. If the pushed packets get our of order would loose documents
                 * since deletes are applied to the wrong segments.
                 */
                packet.DelGen = NextGen_Renamed++;
                Debug.Assert(packet.Any());
                Debug.Assert(CheckDeleteStats());
                Debug.Assert(packet.DelGen < NextGen_Renamed);
                Debug.Assert(Updates.Count == 0 || Updates[Updates.Count - 1].DelGen < packet.DelGen, "Delete packets must be in order");
                Updates.Add(packet);
                numTerms.AddAndGet(packet.NumTermDeletes);
                bytesUsed.AddAndGet(packet.BytesUsed);
                if (InfoStream.IsEnabled("BD"))
                {
                    InfoStream.Message("BD", "push deletes " + packet + " delGen=" + packet.DelGen + " packetCount=" + Updates.Count + " totBytesUsed=" + bytesUsed.Get());
                }
                Debug.Assert(CheckDeleteStats());
                return packet.DelGen;
            }
        }

        public virtual void Clear()
        {
            lock (this)
            {
                Updates.Clear();
                NextGen_Renamed = 1;
                numTerms.Set(0);
                bytesUsed.Set(0);
            }
        }

        public virtual bool Any() // LUCENENET TODO: This should be a property, except for Any() is already a common API in .NET so it might make sense to keep it
        {
            return bytesUsed.Get() != 0;
        }

        public virtual int NumTerms
        {
            get { return numTerms.Get(); }
        }

        public virtual long BytesUsed
        {
            get { return bytesUsed.Get(); }
        }

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
        private static readonly IComparer<SegmentCommitInfo> sortSegInfoByDelGen = new ComparatorAnonymousInnerClassHelper();

        private class ComparatorAnonymousInnerClassHelper : IComparer<SegmentCommitInfo>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(SegmentCommitInfo si1, SegmentCommitInfo si2)
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
            }
        }

        /// <summary>
        /// Resolves the buffered deleted Term/Query/docIDs, into
        ///  actual deleted docIDs in the liveDocs MutableBits for
        ///  each SegmentReader.
        /// </summary>
        public virtual ApplyDeletesResult ApplyDeletesAndUpdates(IndexWriter.ReaderPool readerPool, IList<SegmentCommitInfo> infos)
        {
            lock (this)
            {
                long t0 = Environment.TickCount;

                if (infos.Count == 0)
                {
                    return new ApplyDeletesResult(false, NextGen_Renamed++, null);
                }

                Debug.Assert(CheckDeleteStats());

                if (!Any())
                {
                    if (InfoStream.IsEnabled("BD"))
                    {
                        InfoStream.Message("BD", "applyDeletes: no deletes; skipping");
                    }
                    return new ApplyDeletesResult(false, NextGen_Renamed++, null);
                }

                if (InfoStream.IsEnabled("BD"))
                {
                    InfoStream.Message("BD", "applyDeletes: infos=" + infos + " packetCount=" + Updates.Count);
                }

                long gen = NextGen_Renamed++;

                List<SegmentCommitInfo> infos2 = new List<SegmentCommitInfo>();
                infos2.AddRange(infos);
                infos2.Sort(sortSegInfoByDelGen);

                CoalescedUpdates coalescedUpdates = null;
                bool anyNewDeletes = false;

                int infosIDX = infos2.Count - 1;
                int delIDX = Updates.Count - 1;

                IList<SegmentCommitInfo> allDeleted = null;

                while (infosIDX >= 0)
                {
                    //System.out.println("BD: cycle delIDX=" + delIDX + " infoIDX=" + infosIDX);

                    FrozenBufferedUpdates packet = delIDX >= 0 ? Updates[delIDX] : null;
                    SegmentCommitInfo info = infos2[infosIDX];
                    long segGen = info.BufferedDeletesGen;

                    if (packet != null && segGen < packet.DelGen)
                    {
                        //        System.out.println("  coalesce");
                        if (coalescedUpdates == null)
                        {
                            coalescedUpdates = new CoalescedUpdates();
                        }
                        if (!packet.IsSegmentPrivate)
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
                        Debug.Assert(packet.IsSegmentPrivate, "Packet and Segments deletegen can only match on a segment private del packet gen=" + segGen);
                        //System.out.println("  eq");

                        // Lock order: IW -> BD -> RP
                        Debug.Assert(readerPool.InfoIsLive(info));
                        ReadersAndUpdates rld = readerPool.Get(info, true);
                        SegmentReader reader = rld.GetReader(IOContext.READ);
                        int delCount = 0;
                        bool segAllDeletes;
                        try
                        {
                            AbstractDocValuesFieldUpdates.Container dvUpdates = new AbstractDocValuesFieldUpdates.Container();
                            if (coalescedUpdates != null)
                            {
                                //System.out.println("    del coalesced");
                                delCount += (int)ApplyTermDeletes(coalescedUpdates.TermsIterable(), rld, reader);
                                delCount += (int)ApplyQueryDeletes(coalescedUpdates.QueriesIterable(), rld, reader);
                                ApplyDocValuesUpdates(coalescedUpdates.NumericDVUpdates, rld, reader, dvUpdates);
                                ApplyDocValuesUpdates(coalescedUpdates.BinaryDVUpdates, rld, reader, dvUpdates);
                            }
                            //System.out.println("    del exact");
                            // Don't delete by Term here; DocumentsWriterPerThread
                            // already did that on flush:
                            delCount += (int)ApplyQueryDeletes(packet.QueriesIterable(), rld, reader);
                            ApplyDocValuesUpdates(Arrays.AsList(packet.NumericDVUpdates), rld, reader, dvUpdates);
                            ApplyDocValuesUpdates(Arrays.AsList(packet.BinaryDVUpdates), rld, reader, dvUpdates);
                            if (dvUpdates.Any())
                            {
                                rld.WriteFieldUpdates(info.Info.Dir, dvUpdates);
                            }
                            int fullDelCount = rld.Info.DelCount + rld.PendingDeleteCount;
                            Debug.Assert(fullDelCount <= rld.Info.Info.DocCount);
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
                            if (allDeleted == null)
                            {
                                allDeleted = new List<SegmentCommitInfo>();
                            }
                            allDeleted.Add(info);
                        }

                        if (InfoStream.IsEnabled("BD"))
                        {
                            InfoStream.Message("BD", "seg=" + info + " segGen=" + segGen + " segDeletes=[" + packet + "]; coalesced deletes=[" + (coalescedUpdates == null ? "null" : coalescedUpdates.ToString()) + "] newDelCount=" + delCount + (segAllDeletes ? " 100% deleted" : ""));
                        }

                        if (coalescedUpdates == null)
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
                        info.BufferedDeletesGen = gen;
                    }
                    else
                    {
                        //System.out.println("  gt");

                        if (coalescedUpdates != null)
                        {
                            // Lock order: IW -> BD -> RP
                            Debug.Assert(readerPool.InfoIsLive(info));
                            ReadersAndUpdates rld = readerPool.Get(info, true);
                            SegmentReader reader = rld.GetReader(IOContext.READ);
                            int delCount = 0;
                            bool segAllDeletes;
                            try
                            {
                                delCount += (int)ApplyTermDeletes(coalescedUpdates.TermsIterable(), rld, reader);
                                delCount += (int)ApplyQueryDeletes(coalescedUpdates.QueriesIterable(), rld, reader);
                                AbstractDocValuesFieldUpdates.Container dvUpdates = new AbstractDocValuesFieldUpdates.Container();
                                ApplyDocValuesUpdates(coalescedUpdates.NumericDVUpdates, rld, reader, dvUpdates);
                                ApplyDocValuesUpdates(coalescedUpdates.BinaryDVUpdates, rld, reader, dvUpdates);
                                if (dvUpdates.Any())
                                {
                                    rld.WriteFieldUpdates(info.Info.Dir, dvUpdates);
                                }
                                int fullDelCount = rld.Info.DelCount + rld.PendingDeleteCount;
                                Debug.Assert(fullDelCount <= rld.Info.Info.DocCount);
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
                                if (allDeleted == null)
                                {
                                    allDeleted = new List<SegmentCommitInfo>();
                                }
                                allDeleted.Add(info);
                            }

                            if (InfoStream.IsEnabled("BD"))
                            {
                                InfoStream.Message("BD", "seg=" + info + " segGen=" + segGen + " coalesced deletes=[" + coalescedUpdates + "] newDelCount=" + delCount + (segAllDeletes ? " 100% deleted" : ""));
                            }
                        }
                        info.BufferedDeletesGen = gen;

                        infosIDX--;
                    }
                }

                Debug.Assert(CheckDeleteStats());
                if (InfoStream.IsEnabled("BD"))
                {
                    InfoStream.Message("BD", "applyDeletes took " + (Environment.TickCount - t0) + " msec");
                }
                // assert infos != segmentInfos || !any() : "infos=" + infos + " segmentInfos=" + segmentInfos + " any=" + any;

                return new ApplyDeletesResult(anyNewDeletes, gen, allDeleted);
            }
        }

        internal virtual long GetNextGen()
        {
            lock (this)
            {
                return NextGen_Renamed++;
            }
        }

        // Lock order IW -> BD
        /* Removes any BufferedDeletes that we no longer need to
         * store because all segments in the index have had the
         * deletes applied. */

        public virtual void Prune(SegmentInfos segmentInfos)
        {
            lock (this)
            {
                Debug.Assert(CheckDeleteStats());
                long minGen = long.MaxValue;
                foreach (SegmentCommitInfo info in segmentInfos.Segments)
                {
                    minGen = Math.Min(info.BufferedDeletesGen, minGen);
                }

                if (InfoStream.IsEnabled("BD"))
                {
                    InfoStream.Message("BD", "prune sis=" + segmentInfos + " minGen=" + minGen + " packetCount=" + Updates.Count);
                }
                int limit = Updates.Count;
                for (int delIDX = 0; delIDX < limit; delIDX++)
                {
                    if (Updates[delIDX].DelGen >= minGen)
                    {
                        Prune(delIDX);
                        Debug.Assert(CheckDeleteStats());
                        return;
                    }
                }

                // All deletes pruned
                Prune(limit);
                Debug.Assert(!Any());
                Debug.Assert(CheckDeleteStats());
            }
        }

        private void Prune(int count)
        {
            lock (this)
            {
                if (count > 0)
                {
                    if (InfoStream.IsEnabled("BD"))
                    {
                        InfoStream.Message("BD", "pruneDeletes: prune " + count + " packets; " + (Updates.Count - count) + " packets remain");
                    }
                    for (int delIDX = 0; delIDX < count; delIDX++)
                    {
                        FrozenBufferedUpdates packet = Updates[delIDX];
                        numTerms.AddAndGet(-packet.NumTermDeletes);
                        Debug.Assert(numTerms.Get() >= 0);
                        bytesUsed.AddAndGet(-packet.BytesUsed);
                        Debug.Assert(bytesUsed.Get() >= 0);
                    }
                    Updates.SubList(0, count).Clear();
                }
            }
        }

        // Delete by Term
        private long ApplyTermDeletes(IEnumerable<Term> termsIter, ReadersAndUpdates rld, SegmentReader reader)
        {
            lock (this)
            {
                long delCount = 0;
                Fields fields = reader.Fields;
                if (fields == null)
                {
                    // this reader has no postings
                    return 0;
                }

                TermsEnum termsEnum = null;

                string currentField = null;
                DocsEnum docs = null;

                Debug.Assert(CheckDeleteTerm(null));

                bool any = false;

                //System.out.println(Thread.currentThread().getName() + " del terms reader=" + reader);
                foreach (Term term in termsIter)
                {
                    // Since we visit terms sorted, we gain performance
                    // by re-using the same TermsEnum and seeking only
                    // forwards
                    if (!string.Equals(term.Field, currentField, StringComparison.Ordinal))
                    {
                        Debug.Assert(currentField == null || currentField.CompareTo(term.Field) < 0);
                        currentField = term.Field;
                        Terms terms = fields.Terms(currentField);
                        if (terms != null)
                        {
                            termsEnum = terms.Iterator(termsEnum);
                        }
                        else
                        {
                            termsEnum = null;
                        }
                    }

                    if (termsEnum == null)
                    {
                        continue;
                    }
                    Debug.Assert(CheckDeleteTerm(term));

                    // System.out.println("  term=" + term);

                    if (termsEnum.SeekExact(term.Bytes))
                    {
                        // we don't need term frequencies for this
                        DocsEnum docsEnum = termsEnum.Docs(rld.LiveDocs, docs, DocsEnum.FLAG_NONE);
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
        }

        // DocValues updates
        private void ApplyDocValuesUpdates<T1>(IEnumerable<T1> updates, ReadersAndUpdates rld, SegmentReader reader, AbstractDocValuesFieldUpdates.Container dvUpdatesContainer) where T1 : DocValuesUpdate
        {
            lock (this)
            {
                Fields fields = reader.Fields;
                if (fields == null)
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
                    Term term = update.Term;
                    int limit = update.DocIDUpto;

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
                        //        assert currentField == null || currentField.compareTo(term.field()) < 0;
                        currentField = term.Field;
                        Terms terms = fields.Terms(currentField);
                        if (terms != null)
                        {
                            termsEnum = terms.Iterator(termsEnum);
                        }
                        else
                        {
                            termsEnum = null;
                            continue; // no terms in that field
                        }
                    }

                    if (termsEnum == null)
                    {
                        continue;
                    }
                    // System.out.println("  term=" + term);

                    if (termsEnum.SeekExact(term.Bytes))
                    {
                        // we don't need term frequencies for this
                        DocsEnum docsEnum = termsEnum.Docs(rld.LiveDocs, docs, DocsEnum.FLAG_NONE);

                        //System.out.println("BDS: got docsEnum=" + docsEnum);

                        AbstractDocValuesFieldUpdates dvUpdates = dvUpdatesContainer.GetUpdates(update.Field, update.Type);
                        if (dvUpdates == null)
                        {
                            dvUpdates = dvUpdatesContainer.NewUpdates(update.Field, update.Type, reader.MaxDoc);
                        }
                        int doc;
                        while ((doc = docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            //System.out.println(Thread.currentThread().getName() + " numericDVUpdate term=" + term + " doc=" + docID);
                            if (doc >= limit)
                            {
                                break; // no more docs that can be updated for this term
                            }
                            dvUpdates.Add(doc, update.Value);
                        }
                    }
                }
            }
        }

        public class QueryAndLimit
        {
            public Query Query { get; private set; }
            public int? Limit { get; private set; } // LUCENENET TODO: check if we can work without the nullable

            public QueryAndLimit(Query query, int? limit)
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
                int? limit = ent.Limit;
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
                Debug.Assert(LastDeleteTerm == null || term.CompareTo(LastDeleteTerm) > 0, "lastTerm=" + LastDeleteTerm + " vs term=" + term);
            }
            // TODO: we re-use term now in our merged iterable, but we shouldn't clone, instead copy for this assert
            LastDeleteTerm = term == null ? null : new Term(term.Field, BytesRef.DeepCopyOf(term.Bytes));
            return true;
        }

        // only for assert
        private bool CheckDeleteStats()
        {
            int numTerms2 = 0;
            long bytesUsed2 = 0;
            foreach (FrozenBufferedUpdates packet in Updates)
            {
                numTerms2 += packet.NumTermDeletes;
                bytesUsed2 += packet.BytesUsed;
            }
            Debug.Assert(numTerms2 == numTerms.Get(), "numTerms2=" + numTerms2 + " vs " + numTerms.Get());
            Debug.Assert(bytesUsed2 == bytesUsed.Get(), "bytesUsed2=" + bytesUsed2 + " vs " + bytesUsed);
            return true;
        }
    }
}