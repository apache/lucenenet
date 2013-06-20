using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Index
{
    internal class BufferedDeletesStream
    {
        private readonly IList<FrozenBufferedDeletes> deletes = new List<FrozenBufferedDeletes>();

        // Starts at 1 so that SegmentInfos that have never had
        // deletes applied (whose bufferedDelGen defaults to 0)
        // will be correct:
        private long nextGen = 1;

        // used only by //assert
        private Term lastDeleteTerm;

        private readonly InfoStream infoStream;
        private long bytesUsed = 0L;
        private int numTerms = 0;

        public BufferedDeletesStream(InfoStream infoStream)
        {
            this.infoStream = infoStream;
        }

        public long Push(FrozenBufferedDeletes packet)
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
                packet.DelGen = nextGen++;
                ////assert packet.any();
                ////assert checkDeleteStats();
                ////assert packet.delGen() < nextGen;
                ////assert deletes.isEmpty() || deletes.get(deletes.size()-1).delGen() < packet.delGen() : "Delete packets must be in order";
                deletes.Add(packet);
                Interlocked.Add(ref numTerms, packet.numTermDeletes);
                Interlocked.Add(ref bytesUsed, packet.bytesUsed);
                if (infoStream.IsEnabled("BD"))
                {
                    infoStream.Message("BD", "push deletes " + packet + " delGen=" + packet.DelGen + " packetCount=" + deletes.Count + " totBytesUsed=" + Interlocked.Read(ref bytesUsed));
                }
                ////assert checkDeleteStats();
                return packet.DelGen;
            }
        }

        public void Clear()
        {
            lock (this)
            {
                deletes.Clear();
                nextGen = 1;
                Interlocked.Exchange(ref numTerms, 0);
                Interlocked.Exchange(ref bytesUsed, 0L);
            }
        }

        public bool Any()
        {
            return Interlocked.Read(ref bytesUsed) != 0;
        }

        public int NumTerms
        {
            get
            {
                return numTerms; // .NET: guaranteed atomic read for ints 
            }
        }

        public long BytesUsed
        {
            get
            {
                return Interlocked.Read(ref bytesUsed);
            }
        }

        public class ApplyDeletesResult
        {
            // True if any actual deletes took place:
            public readonly bool anyDeletes;

            // Current gen, for the merged segment:
            public readonly long gen;

            // If non-null, contains segments that are 100% deleted
            public readonly IList<SegmentInfoPerCommit> allDeleted;

            public ApplyDeletesResult(bool anyDeletes, long gen, List<SegmentInfoPerCommit> allDeleted)
            {
                this.anyDeletes = anyDeletes;
                this.gen = gen;
                this.allDeleted = allDeleted;
            }
        }

        private sealed class AnonymousSegmentInfoByDelGenComparer : IComparer<SegmentInfoPerCommit>
        {
            public int Compare(SegmentInfoPerCommit si1, SegmentInfoPerCommit si2)
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

        private static readonly IComparer<SegmentInfoPerCommit> sortSegInfoByDelGen = new AnonymousSegmentInfoByDelGenComparer();

        public ApplyDeletesResult ApplyDeletes(IndexWriter.ReaderPool readerPool, IList<SegmentInfoPerCommit> infos)
        {
            long t0 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            if (infos.Count == 0)
            {
                return new ApplyDeletesResult(false, nextGen++, null);
            }

            //assert checkDeleteStats();

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
                infoStream.Message("BD", "applyDeletes: infos=" + infos + " packetCount=" + deletes.Count);
            }

            long gen = nextGen++;

            List<SegmentInfoPerCommit> infos2 = new List<SegmentInfoPerCommit>();
            infos2.AddRange(infos);
            infos2.Sort(sortSegInfoByDelGen);

            CoalescedDeletes coalescedDeletes = null;
            bool anyNewDeletes = false;

            int infosIDX = infos2.Count - 1;
            int delIDX = deletes.Count - 1;

            List<SegmentInfoPerCommit> allDeleted = null;

            while (infosIDX >= 0)
            {
                //System.out.println("BD: cycle delIDX=" + delIDX + " infoIDX=" + infosIDX);

                FrozenBufferedDeletes packet = delIDX >= 0 ? deletes[delIDX] : null;
                SegmentInfoPerCommit info = infos2[infosIDX];
                long segGen = info.BufferedDeletesGen;

                if (packet != null && segGen < packet.DelGen)
                {
                    //System.out.println("  coalesce");
                    if (coalescedDeletes == null)
                    {
                        coalescedDeletes = new CoalescedDeletes();
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
                        coalescedDeletes.Update(packet);
                    }

                    delIDX--;
                }
                else if (packet != null && segGen == packet.DelGen)
                {
                    //assert packet.isSegmentPrivate : "Packet and Segments deletegen can only match on a segment private del packet gen=" + segGen;
                    //System.out.println("  eq");

                    // Lock order: IW -> BD -> RP
                    //assert readerPool.infoIsLive(info);
                    ReadersAndLiveDocs rld = readerPool.Get(info, true);
                    SegmentReader reader = rld.GetReader(IOContext.READ);
                    long delCount = 0;
                    bool segAllDeletes;
                    try
                    {
                        if (coalescedDeletes != null)
                        {
                            //System.out.println("    del coalesced");
                            delCount += ApplyTermDeletes(coalescedDeletes.TermsEnumerable, rld, reader);
                            delCount += ApplyQueryDeletes(coalescedDeletes.QueriesEnumerable, rld, reader);
                        }
                        //System.out.println("    del exact");
                        // Don't delete by Term here; DocumentsWriterPerThread
                        // already did that on flush:
                        delCount += ApplyQueryDeletes(packet.Queries, rld, reader);
                        int fullDelCount = rld.Info.DelCount + rld.GetPendingDeleteCount();
                        //assert fullDelCount <= rld.info.info.getDocCount();
                        segAllDeletes = fullDelCount == rld.Info.info.DocCount;
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
                            allDeleted = new List<SegmentInfoPerCommit>();
                        }
                        allDeleted.Add(info);
                    }

                    if (infoStream.IsEnabled("BD"))
                    {
                        infoStream.Message("BD", "seg=" + info + " segGen=" + segGen + " segDeletes=[" + packet + "]; coalesced deletes=[" + (coalescedDeletes == null ? "null" : coalescedDeletes.ToString()) + "] newDelCount=" + delCount + (segAllDeletes ? " 100% deleted" : ""));
                    }

                    if (coalescedDeletes == null)
                    {
                        coalescedDeletes = new CoalescedDeletes();
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

                    if (coalescedDeletes != null)
                    {
                        // Lock order: IW -> BD -> RP
                        //assert readerPool.infoIsLive(info);
                        ReadersAndLiveDocs rld = readerPool.Get(info, true);
                        SegmentReader reader = rld.GetReader(IOContext.READ);
                        long delCount = 0;
                        bool segAllDeletes;
                        try
                        {
                            delCount += ApplyTermDeletes(coalescedDeletes.TermsEnumerable, rld, reader);
                            delCount += ApplyQueryDeletes(coalescedDeletes.QueriesEnumerable, rld, reader);
                            int fullDelCount = rld.Info.DelCount + rld.GetPendingDeleteCount();
                            //assert fullDelCount <= rld.info.info.getDocCount();
                            segAllDeletes = fullDelCount == rld.Info.info.DocCount;
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
                                allDeleted = new List<SegmentInfoPerCommit>();
                            }
                            allDeleted.Add(info);
                        }

                        if (infoStream.IsEnabled("BD"))
                        {
                            infoStream.Message("BD", "seg=" + info + " segGen=" + segGen + " coalesced deletes=[" + (coalescedDeletes == null ? "null" : coalescedDeletes.ToString()) + "] newDelCount=" + delCount + (segAllDeletes ? " 100% deleted" : ""));
                        }
                    }
                    info.BufferedDeletesGen = gen;

                    infosIDX--;
                }
            }

            //assert checkDeleteStats();
            if (infoStream.IsEnabled("BD"))
            {
                infoStream.Message("BD", "applyDeletes took " + ((DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) - t0) + " msec");
            }
            // //assert infos != segmentInfos || !any() : "infos=" + infos + " segmentInfos=" + segmentInfos + " any=" + any;

            return new ApplyDeletesResult(anyNewDeletes, gen, allDeleted);
        }

        internal long NextGen
        {
            get
            {
                lock (this)
                {
                    return nextGen++;
                }
            }
        }

        public void Prune(SegmentInfos segmentInfos)
        {
            lock (this)
            {
                //assert checkDeleteStats();
                long minGen = long.MaxValue;
                foreach (SegmentInfoPerCommit info in segmentInfos)
                {
                    minGen = Math.Min(info.BufferedDeletesGen, minGen);
                }

                if (infoStream.IsEnabled("BD"))
                {
                    infoStream.Message("BD", "prune sis=" + segmentInfos + " minGen=" + minGen + " packetCount=" + deletes.Count);
                }
                int limit = deletes.Count;
                for (int delIDX = 0; delIDX < limit; delIDX++)
                {
                    if (deletes[delIDX].DelGen >= minGen)
                    {
                        Prune(delIDX);
                        //assert checkDeleteStats();
                        return;
                    }
                }

                // All deletes pruned
                Prune(limit);
                //assert !any();
                //assert checkDeleteStats();
            }
        }

        private void Prune(int count)
        {
            if (count > 0)
            {
                if (infoStream.IsEnabled("BD"))
                {
                    infoStream.Message("BD", "pruneDeletes: prune " + count + " packets; " + (deletes.Count - count) + " packets remain");
                }
                for (int delIDX = 0; delIDX < count; delIDX++)
                {
                    FrozenBufferedDeletes packet = deletes[delIDX];
                    Interlocked.Add(ref numTerms, -packet.numTermDeletes);
                    //assert numTerms.get() >= 0;
                    Interlocked.Add(ref bytesUsed, -packet.bytesUsed);
                    //assert bytesUsed.get() >= 0;
                }

                for (int i = 0; i < count; i++)
                {
                    deletes.RemoveAt(i);
                }
            }
        }

        private long ApplyTermDeletes(IEnumerable<Term> termsIter, ReadersAndLiveDocs rld, SegmentReader reader)
        {
            long delCount = 0;
            Fields fields = reader.Fields;
            if (fields == null)
            {
                // This reader has no postings
                return 0;
            }

            TermsEnum termsEnum = null;

            String currentField = null;
            DocsEnum docs = null;

            //assert checkDeleteTerm(null);

            bool any = false;

            //System.out.println(Thread.currentThread().getName() + " del terms reader=" + reader);
            foreach (Term term in termsIter)
            {
                // Since we visit terms sorted, we gain performance
                // by re-using the same TermsEnum and seeking only
                // forwards
                if (!term.Field.Equals(currentField))
                {
                    //assert currentField == null || currentField.compareTo(term.field()) < 0;
                    currentField = term.Field();
                    Terms terms = fields.Terms(currentField);
                    if (terms != null)
                    {
                        termsEnum = terms.Iterator(null);
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
                //assert checkDeleteTerm(term);

                // System.out.println("  term=" + term);

                if (termsEnum.SeekExact(term.Bytes, false))
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
                            // NOTE: there is no limit check on the docID
                            // when deleting by Term (unlike by Query)
                            // because on flush we apply all Term deletes to
                            // each segment.  So all Term deleting here is
                            // against prior segments:
                            if (!any)
                            {
                                rld.InitWritableLiveDocs();
                                any = true;
                            }
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

        public class QueryAndLimit
        {
            public readonly Query query;
            public readonly int limit;
            public QueryAndLimit(Query query, int limit)
            {
                this.query = query;
                this.limit = limit;
            }
        }

        private static long ApplyQueryDeletes(IEnumerable<QueryAndLimit> queriesIter, ReadersAndLiveDocs rld, SegmentReader reader)
        {
            long delCount = 0;
            AtomicReaderContext readerContext = reader.Context;
            bool any = false;
            foreach (QueryAndLimit ent in queriesIter)
            {
                Query query = ent.query;
                int limit = ent.limit;
                DocIdSet docs = new QueryWrapperFilter(query).GetDocIdSet(readerContext, reader.LiveDocs);
                if (docs != null)
                {
                    DocIdSetIterator it = docs.Iterator();
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

        private bool CheckDeleteTerm(Term term)
        {
            if (term != null)
            {
                //assert lastDeleteTerm == null || term.compareTo(lastDeleteTerm) > 0: "lastTerm=" + lastDeleteTerm + " vs term=" + term;
            }
            // TODO: we re-use term now in our merged iterable, but we shouldn't clone, instead copy for this assert
            lastDeleteTerm = term == null ? null : new Term(term.Field, BytesRef.DeepCopyOf(term.Bytes));
            return true;
        }

        private bool CheckDeleteStats()
        {
            int numTerms2 = 0;
            long bytesUsed2 = 0;
            foreach (FrozenBufferedDeletes packet in deletes)
            {
                numTerms2 += packet.numTermDeletes;
                bytesUsed2 += packet.bytesUsed;
            }
            //assert numTerms2 == numTerms.get(): "numTerms2=" + numTerms2 + " vs " + numTerms.get();
            //assert bytesUsed2 == bytesUsed.get(): "bytesUsed2=" + bytesUsed2 + " vs " + bytesUsed;
            return true;
        }
    }
}
