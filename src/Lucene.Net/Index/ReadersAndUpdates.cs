using J2N;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

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

    using BinaryDocValuesField = BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;
    using IBits = Lucene.Net.Util.IBits;
    using IMutableBits = Lucene.Net.Util.IMutableBits;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LiveDocsFormat = Lucene.Net.Codecs.LiveDocsFormat;
    using NumericDocValuesField = NumericDocValuesField;
    using TrackingDirectoryWrapper = Lucene.Net.Store.TrackingDirectoryWrapper;

    /// <summary>
    /// Used by <see cref="IndexWriter"/> to hold open <see cref="SegmentReader"/>s (for
    /// searching or merging), plus pending deletes and updates,
    /// for a given segment
    /// </summary>
    internal class ReadersAndUpdates
    {
        // Not final because we replace (clone) when we need to
        // change it and it's been shared:
        public SegmentCommitInfo Info { get; private set; }

        // Tracks how many consumers are using this instance:
        private readonly AtomicInt32 refCount = new AtomicInt32(1);

        private readonly IndexWriter writer;

        // Set once (null, and then maybe set, and never set again):
        private SegmentReader reader;

        // TODO: it's sometimes wasteful that we hold open two
        // separate SRs (one for merging one for
        // reading)... maybe just use a single SR?  The gains of
        // not loading the terms index (for merging in the
        // non-NRT case) are far less now... and if the app has
        // any deletes it'll open real readers anyway.

        // Set once (null, and then maybe set, and never set again):
        private SegmentReader mergeReader;

        // Holds the current shared (readable and writable)
        // liveDocs.  this is null when there are no deleted
        // docs, and it's copy-on-write (cloned whenever we need
        // to change it but it's been shared to an external NRT
        // reader).
        private IBits liveDocs;

        // How many further deletions we've done against
        // liveDocs vs when we loaded it or last wrote it:
        private int pendingDeleteCount;

        // True if the current liveDocs is referenced by an
        // external NRT reader:
        private bool liveDocsShared;

        // Indicates whether this segment is currently being merged. While a segment
        // is merging, all field updates are also registered in the
        // mergingNumericUpdates map. Also, calls to writeFieldUpdates merge the
        // updates with mergingNumericUpdates.
        // That way, when the segment is done merging, IndexWriter can apply the
        // updates on the merged segment too.
        private bool isMerging = false;

        private readonly IDictionary<string, DocValuesFieldUpdates> mergingDVUpdates = new Dictionary<string, DocValuesFieldUpdates>();

        public ReadersAndUpdates(IndexWriter writer, SegmentCommitInfo info)
        {
            this.Info = info;
            this.writer = writer;
            liveDocsShared = true;
        }

        public virtual void IncRef()
        {
            int rc = refCount.IncrementAndGet();
            if (Debugging.AssertsEnabled) Debugging.Assert(rc > 1);
        }

        public virtual void DecRef()
        {
            int rc = refCount.DecrementAndGet();
            if (Debugging.AssertsEnabled) Debugging.Assert(rc >= 0);
        }

        public virtual int RefCount()
        {
            int rc = refCount;
            if (Debugging.AssertsEnabled) Debugging.Assert(rc >= 0);
            return rc;
        }

        public virtual int PendingDeleteCount
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return pendingDeleteCount;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        // Call only from assert!
        public virtual bool VerifyDocCounts()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                int count;
                if (liveDocs != null)
                {
                    count = 0;
                    for (int docID = 0; docID < Info.Info.DocCount; docID++)
                    {
                        if (liveDocs.Get(docID))
                        {
                            count++;
                        }
                    }
                }
                else
                {
                    count = Info.Info.DocCount;
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(Info.Info.DocCount - Info.DelCount - pendingDeleteCount == count, "info.docCount={0} info.DelCount={1} pendingDeleteCount={2} count={3}", Info.Info.DocCount, Info.DelCount, pendingDeleteCount, count);
                return true;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns a <see cref="SegmentReader"/>. </summary>
        public virtual SegmentReader GetReader(IOContext context)
        {
            if (reader is null)
            {
                // We steal returned ref:
                reader = new SegmentReader(Info, writer.Config.ReaderTermsIndexDivisor, context);
                if (liveDocs is null)
                {
                    liveDocs = reader.LiveDocs;
                }
            }

            // Ref for caller
            reader.IncRef();
            return reader;
        }

        // Get reader for merging (does not load the terms
        // index):
        public virtual SegmentReader GetMergeReader(IOContext context)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                //System.out.println("  livedocs=" + rld.liveDocs);

                if (mergeReader is null)
                {
                    if (reader != null)
                    {
                        // Just use the already opened non-merge reader
                        // for merging.  In the NRT case this saves us
                        // pointless double-open:
                        //System.out.println("PROMOTE non-merge reader seg=" + rld.info);
                        // Ref for us:
                        reader.IncRef();
                        mergeReader = reader;
                        //System.out.println(Thread.currentThread().getName() + ": getMergeReader share seg=" + info.name);
                    }
                    else
                    {
                        //System.out.println(Thread.currentThread().getName() + ": getMergeReader seg=" + info.name);
                        // We steal returned ref:
                        mergeReader = new SegmentReader(Info, -1, context);
                        if (liveDocs is null)
                        {
                            liveDocs = mergeReader.LiveDocs;
                        }
                    }
                }

                // Ref for caller
                mergeReader.IncRef();
                return mergeReader;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual void Release(SegmentReader sr)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(Info == sr.SegmentInfo);
                sr.DecRef();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual bool Delete(int docID)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(liveDocs != null);
                    Debugging.Assert(UninterruptableMonitor.IsEntered(writer));
                    Debugging.Assert(docID >= 0 && docID < liveDocs.Length, "out of bounds: docid={0} liveDocsLength={1} seg={2} docCount={3}", docID, liveDocs.Length, Info.Info.Name, Info.Info.DocCount);
                    Debugging.Assert(!liveDocsShared);
                }
                bool didDelete = liveDocs.Get(docID);
                if (didDelete)
                {
                    ((IMutableBits)liveDocs).Clear(docID);
                    pendingDeleteCount++;
                    //System.out.println("  new del seg=" + info + " docID=" + docID + " pendingDelCount=" + pendingDeleteCount + " totDelCount=" + (info.docCount-liveDocs.count()));
                }
                return didDelete;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // NOTE: removes callers ref
        public virtual void DropReaders()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // TODO: can we somehow use IOUtils here...?  problem is
                // we are calling .decRef not .close)...
                try
                {
                    if (reader != null)
                    {
                        //System.out.println("  pool.drop info=" + info + " rc=" + reader.getRefCount());
                        try
                        {
                            reader.DecRef();
                        }
                        finally
                        {
                            reader = null;
                        }
                    }
                }
                finally
                {
                    if (mergeReader != null)
                    {
                        //System.out.println("  pool.drop info=" + info + " merge rc=" + mergeReader.getRefCount());
                        try
                        {
                            mergeReader.DecRef();
                        }
                        finally
                        {
                            mergeReader = null;
                        }
                    }
                }

                DecRef();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns a ref to a clone. NOTE: you should <see cref="DecRef()"/> the reader when you're
        /// done (ie do not call <see cref="IndexReader.Dispose()"/>).
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual SegmentReader GetReadOnlyClone(IOContext context)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (reader is null)
                {
                    GetReader(context).DecRef();
                    if (Debugging.AssertsEnabled) Debugging.Assert(reader != null);
                }
                liveDocsShared = true;
                if (liveDocs != null)
                {
                    return new SegmentReader(reader.SegmentInfo, reader, liveDocs, Info.Info.DocCount - Info.DelCount - pendingDeleteCount);
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(reader.LiveDocs == liveDocs);
                    reader.IncRef();
                    return reader;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual void InitWritableLiveDocs()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(UninterruptableMonitor.IsEntered(writer));
                    Debugging.Assert(Info.Info.DocCount > 0);
                }
                //System.out.println("initWritableLivedocs seg=" + info + " liveDocs=" + liveDocs + " shared=" + shared);
                if (liveDocsShared)
                {
                    // Copy on write: this means we've cloned a
                    // SegmentReader sharing the current liveDocs
                    // instance; must now make a private clone so we can
                    // change it:
                    LiveDocsFormat liveDocsFormat = Info.Info.Codec.LiveDocsFormat;
                    if (liveDocs is null)
                    {
                        //System.out.println("create BV seg=" + info);
                        liveDocs = liveDocsFormat.NewLiveDocs(Info.Info.DocCount);
                    }
                    else
                    {
                        liveDocs = liveDocsFormat.NewLiveDocs(liveDocs);
                    }
                    liveDocsShared = false;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual IBits LiveDocs
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(UninterruptableMonitor.IsEntered(writer));
                    return liveDocs;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        public virtual IBits GetReadOnlyLiveDocs()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                //System.out.println("getROLiveDocs seg=" + info);
                if (Debugging.AssertsEnabled) Debugging.Assert(UninterruptableMonitor.IsEntered(writer));
                liveDocsShared = true;
                //if (liveDocs != null) {
                //System.out.println("  liveCount=" + liveDocs.count());
                //}
                return liveDocs;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual void DropChanges()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // Discard (don't save) changes when we are dropping
                // the reader; this is used only on the sub-readers
                // after a successful merge.  If deletes had
                // accumulated on those sub-readers while the merge
                // is running, by now we have carried forward those
                // deletes onto the newly merged segment, so we can
                // discard them on the sub-readers:
                pendingDeleteCount = 0;
                DropMergingUpdates();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // Commit live docs (writes new _X_N.del files) and field updates (writes new
        // _X_N updates files) to the directory; returns true if it wrote any file
        // and false if there were no new deletes or updates to write:
        // TODO (DVU_RENAME) to writeDeletesAndUpdates
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual bool WriteLiveDocs(Directory dir)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(UninterruptableMonitor.IsEntered(writer));
                //System.out.println("rld.writeLiveDocs seg=" + info + " pendingDelCount=" + pendingDeleteCount + " numericUpdates=" + numericUpdates);
                if (pendingDeleteCount == 0)
                {
                    return false;
                }

                // We have new deletes
                if (Debugging.AssertsEnabled) Debugging.Assert(liveDocs.Length == Info.Info.DocCount);

                // Do this so we can delete any created files on
                // exception; this saves all codecs from having to do
                // it:
                TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(dir);

                // We can write directly to the actual name (vs to a
                // .tmp & renaming it) because the file is not live
                // until segments file is written:
                bool success = false;
                try
                {
                    Codec codec = Info.Info.Codec;
                    codec.LiveDocsFormat.WriteLiveDocs((IMutableBits)liveDocs, trackingDir, Info, pendingDeleteCount, IOContext.DEFAULT);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // Advance only the nextWriteDelGen so that a 2nd
                        // attempt to write will write to a new file
                        Info.AdvanceNextWriteDelGen();

                        // Delete any partially created file(s):
                        foreach (string fileName in trackingDir.CreatedFiles)
                        {
                            try
                            {
                                dir.DeleteFile(fileName);
                            }
                            catch (Exception t) when (t.IsThrowable())
                            {
                                // Ignore so we throw only the first exc
                            }
                        }
                    }
                }

                // If we hit an exc in the line above (eg disk full)
                // then info's delGen remains pointing to the previous
                // (successfully written) del docs:
                Info.AdvanceDelGen();
                Info.DelCount = Info.DelCount + pendingDeleteCount;
                pendingDeleteCount = 0;

                return true;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // Writes field updates (new _X_N updates files) to the directory
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void WriteFieldUpdates(Directory dir, DocValuesFieldUpdates.Container dvUpdates)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(UninterruptableMonitor.IsEntered(writer));
                //System.out.println("rld.writeFieldUpdates: seg=" + info + " numericFieldUpdates=" + numericFieldUpdates);

                if (Debugging.AssertsEnabled) Debugging.Assert(dvUpdates.Any());

                // Do this so we can delete any created files on
                // exception; this saves all codecs from having to do
                // it:
                TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(dir);

                FieldInfos fieldInfos = null;
                bool success = false;
                try
                {
                    Codec codec = Info.Info.Codec;

                    // reader could be null e.g. for a just merged segment (from
                    // IndexWriter.commitMergedDeletes).
                    SegmentReader reader = this.reader ?? new SegmentReader(Info, writer.Config.ReaderTermsIndexDivisor, IOContext.READ_ONCE);
                    try
                    {
                        // clone FieldInfos so that we can update their dvGen separately from
                        // the reader's infos and write them to a new fieldInfos_gen file
                        FieldInfos.Builder builder = new FieldInfos.Builder(writer.globalFieldNumberMap);
                        // cannot use builder.add(reader.getFieldInfos()) because it does not
                        // clone FI.attributes as well FI.dvGen
                        foreach (FieldInfo fi in reader.FieldInfos)
                        {
                            FieldInfo clone = builder.Add(fi);
                            // copy the stuff FieldInfos.Builder doesn't copy
                            if (fi.Attributes != null)
                            {
                                foreach (KeyValuePair<string, string> e in fi.Attributes)
                                {
                                    clone.PutAttribute(e.Key, e.Value);
                                }
                            }
                            clone.DocValuesGen = fi.DocValuesGen;
                        }
                        // create new fields or update existing ones to have NumericDV type
                        foreach (string f in dvUpdates.numericDVUpdates.Keys)
                        {
                            builder.AddOrUpdate(f, NumericDocValuesField.TYPE);
                        }
                        // create new fields or update existing ones to have BinaryDV type
                        foreach (string f in dvUpdates.binaryDVUpdates.Keys)
                        {
                            builder.AddOrUpdate(f, BinaryDocValuesField.TYPE);
                        }

                        fieldInfos = builder.Finish();
                        long nextFieldInfosGen = Info.NextFieldInfosGen;
                        // LUCENENET specific: We created the segments names wrong in 4.8.0-beta00001 - 4.8.0-beta00015,
                        // so we added a switch to be able to read these indexes in later versions. This logic as well as an
                        // optimization on the first 100 segment values is implmeneted in SegmentInfos.SegmentNumberToString().
                        string segmentSuffix = SegmentInfos.SegmentNumberToString(nextFieldInfosGen);
                        SegmentWriteState state = new SegmentWriteState(null, trackingDir, Info.Info, fieldInfos, writer.Config.TermIndexInterval, null, IOContext.DEFAULT, segmentSuffix);
                        DocValuesFormat docValuesFormat = codec.DocValuesFormat;
                        DocValuesConsumer fieldsConsumer = docValuesFormat.FieldsConsumer(state);
                        bool fieldsConsumerSuccess = false;
                        try
                        {
                            //          System.out.println("[" + Thread.currentThread().getName() + "] RLD.writeFieldUpdates: applying numeric updates; seg=" + info + " updates=" + numericFieldUpdates);
                            foreach (KeyValuePair<string, NumericDocValuesFieldUpdates> e in dvUpdates.numericDVUpdates)
                            {
                                string field = e.Key;
                                NumericDocValuesFieldUpdates fieldUpdates = e.Value;
                                FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                                if (Debugging.AssertsEnabled) Debugging.Assert(fieldInfo != null);

                                fieldInfo.DocValuesGen = nextFieldInfosGen;
                                // write the numeric updates to a new gen'd docvalues file
                                fieldsConsumer.AddNumericField(fieldInfo, GetInt64Enumerable(reader, field, fieldUpdates));
                            }

                            //        System.out.println("[" + Thread.currentThread().getName() + "] RAU.writeFieldUpdates: applying binary updates; seg=" + info + " updates=" + dvUpdates.binaryDVUpdates);
                            foreach (KeyValuePair<string, BinaryDocValuesFieldUpdates> e in dvUpdates.binaryDVUpdates)
                            {
                                string field = e.Key;
                                BinaryDocValuesFieldUpdates dvFieldUpdates = e.Value;
                                FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                                if (Debugging.AssertsEnabled) Debugging.Assert(fieldInfo != null);

                                //          System.out.println("[" + Thread.currentThread().getName() + "] RAU.writeFieldUpdates: applying binary updates; seg=" + info + " f=" + dvFieldUpdates + ", updates=" + dvFieldUpdates);

                                fieldInfo.DocValuesGen = nextFieldInfosGen;
                                // write the numeric updates to a new gen'd docvalues file
                                fieldsConsumer.AddBinaryField(fieldInfo, GetBytesRefEnumerable(reader, field, dvFieldUpdates));
                            }

                            codec.FieldInfosFormat.FieldInfosWriter.Write(trackingDir, Info.Info.Name, segmentSuffix, fieldInfos, IOContext.DEFAULT);
                            fieldsConsumerSuccess = true;
                        }
                        finally
                        {
                            if (fieldsConsumerSuccess)
                            {
                                fieldsConsumer.Dispose();
                            }
                            else
                            {
                                IOUtils.DisposeWhileHandlingException(fieldsConsumer);
                            }
                        }
                    }
                    finally
                    {
                        if (reader != this.reader)
                        {
                            //          System.out.println("[" + Thread.currentThread().getName() + "] RLD.writeLiveDocs: closeReader " + reader);
                            reader.Dispose();
                        }
                    }

                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // Advance only the nextWriteDocValuesGen so that a 2nd
                        // attempt to write will write to a new file
                        Info.AdvanceNextWriteFieldInfosGen();

                        // Delete any partially created file(s):
                        foreach (string fileName in trackingDir.CreatedFiles)
                        {
                            try
                            {
                                dir.DeleteFile(fileName);
                            }
                            catch (Exception t) when (t.IsThrowable())
                            {
                                // Ignore so we throw only the first exc
                            }
                        }
                    }
                }

                Info.AdvanceFieldInfosGen();
                // copy all the updates to mergingUpdates, so they can later be applied to the merged segment
                if (isMerging)
                {
                    foreach (KeyValuePair<string, NumericDocValuesFieldUpdates> e in dvUpdates.numericDVUpdates)
                    {
                        if (!mergingDVUpdates.TryGetValue(e.Key, out DocValuesFieldUpdates updates))
                        {
                            mergingDVUpdates[e.Key] = e.Value;
                        }
                        else
                        {
                            updates.Merge(e.Value);
                        }
                    }
                    foreach (KeyValuePair<string, BinaryDocValuesFieldUpdates> e in dvUpdates.binaryDVUpdates)
                    {
                        if (!mergingDVUpdates.TryGetValue(e.Key, out DocValuesFieldUpdates updates))
                        {
                            mergingDVUpdates[e.Key] = e.Value;
                        }
                        else
                        {
                            updates.Merge(e.Value);
                        }
                    }
                }

                // create a new map, keeping only the gens that are in use
                IDictionary<long, ISet<string>> genUpdatesFiles = Info.UpdatesFiles;
                IDictionary<long, ISet<string>> newGenUpdatesFiles = new Dictionary<long, ISet<string>>();
                long fieldInfosGen = Info.FieldInfosGen;
                foreach (FieldInfo fi in fieldInfos)
                {
                    long dvGen = fi.DocValuesGen;
                    if (dvGen != -1 && !newGenUpdatesFiles.ContainsKey(dvGen))
                    {
                        if (dvGen == fieldInfosGen)
                        {
                            newGenUpdatesFiles[fieldInfosGen] = trackingDir.CreatedFiles;
                        }
                        else
                        {
                            newGenUpdatesFiles[dvGen] = genUpdatesFiles[dvGen];
                        }
                    }
                }

                Info.SetGenUpdatesFiles(newGenUpdatesFiles);

                // wrote new files, should checkpoint()
                writer.Checkpoint();

                // if there is a reader open, reopen it to reflect the updates
                if (reader != null)
                {
                    SegmentReader newReader = new SegmentReader(Info, reader, liveDocs, Info.Info.DocCount - Info.DelCount - pendingDeleteCount);
                    bool reopened = false;
                    try
                    {
                        reader.DecRef();
                        reader = newReader;
                        reopened = true;
                    }
                    finally
                    {
                        if (!reopened)
                        {
                            newReader.DecRef();
                        }
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// NOTE: This was getLongEnumerable() in Lucene
        /// </summary>
        private static IEnumerable<long?> GetInt64Enumerable(SegmentReader reader, string field, NumericDocValuesFieldUpdates fieldUpdates) // LUCENENET: CA1822: Mark members as static
        {
            int maxDoc = reader.MaxDoc;
            IBits DocsWithField = reader.GetDocsWithField(field);
            NumericDocValues currentValues = reader.GetNumericDocValues(field);
            NumericDocValuesFieldUpdates.Iterator iter = (NumericDocValuesFieldUpdates.Iterator)fieldUpdates.GetIterator();
            int updateDoc = iter.NextDoc();

            for (int curDoc = 0; curDoc < maxDoc; ++curDoc)
            {
                if (curDoc == updateDoc) //document has an updated value
                {
                    long? value = iter.Value; // either null or updated
                    updateDoc = iter.NextDoc(); //prepare for next round
                    yield return value;
                }
                else
                {   // no update for this document
                    if (Debugging.AssertsEnabled) Debugging.Assert(curDoc < updateDoc);
                    if (currentValues != null && DocsWithField.Get(curDoc))
                    {
                        // only read the current value if the document had a value before
                        yield return currentValues.Get(curDoc);
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }
        }

        private static IEnumerable<BytesRef> GetBytesRefEnumerable(SegmentReader reader, string field, BinaryDocValuesFieldUpdates fieldUpdates) // LUCENENET: CA1822: Mark members as static
        {
            BinaryDocValues currentValues = reader.GetBinaryDocValues(field);
            IBits DocsWithField = reader.GetDocsWithField(field);
            int maxDoc = reader.MaxDoc;
            var iter = (BinaryDocValuesFieldUpdates.Iterator)fieldUpdates.GetIterator();
            int updateDoc = iter.NextDoc();
            var scratch = new BytesRef();

            for (int curDoc = 0; curDoc < maxDoc; ++curDoc)
            {
                if (curDoc == updateDoc) //document has an updated value
                {
                    BytesRef value = (BytesRef)iter.Value; // either null or updated
                    updateDoc = iter.NextDoc(); //prepare for next round
                    yield return value;
                }
                else
                {   // no update for this document
                    if (Debugging.AssertsEnabled) Debugging.Assert(curDoc < updateDoc);
                    if (currentValues != null && DocsWithField.Get(curDoc))
                    {
                        // only read the current value if the document had a value before
                        currentValues.Get(curDoc, scratch);
                        yield return scratch;
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a reader for merge. this method applies field updates if there are
        /// any and marks that this segment is currently merging.
        /// </summary>
        internal virtual SegmentReader GetReaderForMerge(IOContext context)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(UninterruptableMonitor.IsEntered(writer));
                // must execute these two statements as atomic operation, otherwise we
                // could lose updates if e.g. another thread calls writeFieldUpdates in
                // between, or the updates are applied to the obtained reader, but then
                // re-applied in IW.commitMergedDeletes (unnecessary work and potential
                // bugs).
                isMerging = true;
                return GetReader(context);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Drops all merging updates. Called from IndexWriter after this segment
        /// finished merging (whether successfully or not).
        /// </summary>
        public virtual void DropMergingUpdates()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                mergingDVUpdates.Clear();
                isMerging = false;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns updates that came in while this segment was merging. </summary>
        public virtual IDictionary<string, DocValuesFieldUpdates> MergingFieldUpdates
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return mergingDVUpdates;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ReadersAndLiveDocs(seg=").Append(Info);
            sb.Append(" pendingDeleteCount=").Append(pendingDeleteCount);
            sb.Append(" liveDocsShared=").Append(liveDocsShared);
            return sb.ToString();
        }
    }
}