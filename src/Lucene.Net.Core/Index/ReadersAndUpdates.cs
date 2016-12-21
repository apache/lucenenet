using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using BinaryDocValuesField = BinaryDocValuesField;
    using Bits = Lucene.Net.Util.Bits;
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

    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LiveDocsFormat = Lucene.Net.Codecs.LiveDocsFormat;
    using MutableBits = Lucene.Net.Util.MutableBits;
    using NumericDocValuesField = NumericDocValuesField;
    using TrackingDirectoryWrapper = Lucene.Net.Store.TrackingDirectoryWrapper;

    // Used by IndexWriter to hold open SegmentReaders (for
    // searching or merging), plus pending deletes and updates,
    // for a given segment
    internal class ReadersAndUpdates
    {
        // Not final because we replace (clone) when we need to
        // change it and it's been shared:
        public readonly SegmentCommitInfo Info; // LUCENENET TODO: Make property

        // Tracks how many consumers are using this instance:
        private readonly AtomicInteger RefCount_Renamed = new AtomicInteger(1);

        private readonly IndexWriter Writer;

        // Set once (null, and then maybe set, and never set again):
        private SegmentReader Reader;

        // TODO: it's sometimes wasteful that we hold open two
        // separate SRs (one for merging one for
        // reading)... maybe just use a single SR?  The gains of
        // not loading the terms index (for merging in the
        // non-NRT case) are far less now... and if the app has
        // any deletes it'll open real readers anyway.

        // Set once (null, and then maybe set, and never set again):
        private SegmentReader MergeReader;

        // Holds the current shared (readable and writable)
        // liveDocs.  this is null when there are no deleted
        // docs, and it's copy-on-write (cloned whenever we need
        // to change it but it's been shared to an external NRT
        // reader).
        private Bits LiveDocs_Renamed;

        // How many further deletions we've done against
        // liveDocs vs when we loaded it or last wrote it:
        private int PendingDeleteCount_Renamed;

        // True if the current liveDocs is referenced by an
        // external NRT reader:
        private bool LiveDocsShared;

        // Indicates whether this segment is currently being merged. While a segment
        // is merging, all field updates are also registered in the
        // mergingNumericUpdates map. Also, calls to writeFieldUpdates merge the
        // updates with mergingNumericUpdates.
        // That way, when the segment is done merging, IndexWriter can apply the
        // updates on the merged segment too.
        private bool IsMerging = false;

        private readonly IDictionary<string, AbstractDocValuesFieldUpdates> MergingDVUpdates = new Dictionary<string, AbstractDocValuesFieldUpdates>();

        public ReadersAndUpdates(IndexWriter writer, SegmentCommitInfo info)
        {
            this.Info = info;
            this.Writer = writer;
            LiveDocsShared = true;
        }

        public virtual void IncRef()
        {
            int rc = RefCount_Renamed.IncrementAndGet();
            Debug.Assert(rc > 1);
        }

        public virtual void DecRef()
        {
            int rc = RefCount_Renamed.DecrementAndGet();
            Debug.Assert(rc >= 0);
        }

        public virtual int RefCount()
        {
            int rc = RefCount_Renamed.Get();
            Debug.Assert(rc >= 0);
            return rc;
        }

        public virtual int PendingDeleteCount
        {
            get
            {
                lock (this)
                {
                    return PendingDeleteCount_Renamed;
                }
            }
        }

        // Call only from assert!
        public virtual bool VerifyDocCounts()
        {
            lock (this)
            {
                int count;
                if (LiveDocs_Renamed != null)
                {
                    count = 0;
                    for (int docID = 0; docID < Info.Info.DocCount; docID++)
                    {
                        if (LiveDocs_Renamed.Get(docID))
                        {
                            count++;
                        }
                    }
                }
                else
                {
                    count = Info.Info.DocCount;
                }

                Debug.Assert(Info.Info.DocCount - Info.DelCount - PendingDeleteCount_Renamed == count, "info.docCount=" + Info.Info.DocCount + " info.getDelCount()=" + Info.DelCount + " pendingDeleteCount=" + PendingDeleteCount_Renamed + " count=" + count);
                return true;
            }
        }

        /// <summary>
        /// Returns a <seealso cref="SegmentReader"/>. </summary>
        public virtual SegmentReader GetReader(IOContext context)
        {
            if (Reader == null)
            {
                // We steal returned ref:
                Reader = new SegmentReader(Info, Writer.Config.ReaderTermsIndexDivisor, context);
                if (LiveDocs_Renamed == null)
                {
                    LiveDocs_Renamed = Reader.LiveDocs;
                }
            }

            // Ref for caller
            Reader.IncRef();
            return Reader;
        }

        // Get reader for merging (does not load the terms
        // index):
        public virtual SegmentReader GetMergeReader(IOContext context)
        {
            lock (this)
            {
                //System.out.println("  livedocs=" + rld.liveDocs);

                if (MergeReader == null)
                {
                    if (Reader != null)
                    {
                        // Just use the already opened non-merge reader
                        // for merging.  In the NRT case this saves us
                        // pointless double-open:
                        //System.out.println("PROMOTE non-merge reader seg=" + rld.info);
                        // Ref for us:
                        Reader.IncRef();
                        MergeReader = Reader;
                        //System.out.println(Thread.currentThread().getName() + ": getMergeReader share seg=" + info.name);
                    }
                    else
                    {
                        //System.out.println(Thread.currentThread().getName() + ": getMergeReader seg=" + info.name);
                        // We steal returned ref:
                        MergeReader = new SegmentReader(Info, -1, context);
                        if (LiveDocs_Renamed == null)
                        {
                            LiveDocs_Renamed = MergeReader.LiveDocs;
                        }
                    }
                }

                // Ref for caller
                MergeReader.IncRef();
                return MergeReader;
            }
        }

        public virtual void Release(SegmentReader sr)
        {
            lock (this)
            {
                Debug.Assert(Info == sr.SegmentInfo);
                sr.DecRef();
            }
        }

        public virtual bool Delete(int docID)
        {
            lock (this)
            {
                Debug.Assert(LiveDocs_Renamed != null);
                //Debug.Assert(Thread.holdsLock(Writer));
                Debug.Assert(docID >= 0 && docID < LiveDocs_Renamed.Length(), "out of bounds: docid=" + docID + " liveDocsLength=" + LiveDocs_Renamed.Length() + " seg=" + Info.Info.Name + " docCount=" + Info.Info.DocCount);
                Debug.Assert(!LiveDocsShared);
                bool didDelete = LiveDocs_Renamed.Get(docID);
                if (didDelete)
                {
                    ((MutableBits)LiveDocs_Renamed).Clear(docID);
                    PendingDeleteCount_Renamed++;
                    //System.out.println("  new del seg=" + info + " docID=" + docID + " pendingDelCount=" + pendingDeleteCount + " totDelCount=" + (info.docCount-liveDocs.count()));
                }
                return didDelete;
            }
        }

        // NOTE: removes callers ref
        public virtual void DropReaders()
        {
            lock (this)
            {
                // TODO: can we somehow use IOUtils here...?  problem is
                // we are calling .decRef not .close)...
                try
                {
                    if (Reader != null)
                    {
                        //System.out.println("  pool.drop info=" + info + " rc=" + reader.getRefCount());
                        try
                        {
                            Reader.DecRef();
                        }
                        finally
                        {
                            Reader = null;
                        }
                    }
                }
                finally
                {
                    if (MergeReader != null)
                    {
                        //System.out.println("  pool.drop info=" + info + " merge rc=" + mergeReader.getRefCount());
                        try
                        {
                            MergeReader.DecRef();
                        }
                        finally
                        {
                            MergeReader = null;
                        }
                    }
                }

                DecRef();
            }
        }

        /// <summary>
        /// Returns a ref to a clone. NOTE: you should decRef() the reader when you're
        /// dont (ie do not call close()).
        /// </summary>
        public virtual SegmentReader GetReadOnlyClone(IOContext context)
        {
            lock (this)
            {
                if (Reader == null)
                {
                    GetReader(context).DecRef();
                    Debug.Assert(Reader != null);
                }
                LiveDocsShared = true;
                if (LiveDocs_Renamed != null)
                {
                    return new SegmentReader(Reader.SegmentInfo, Reader, LiveDocs_Renamed, Info.Info.DocCount - Info.DelCount - PendingDeleteCount_Renamed);
                }
                else
                {
                    Debug.Assert(Reader.LiveDocs == LiveDocs_Renamed);
                    Reader.IncRef();
                    return Reader;
                }
            }
        }

        public virtual void InitWritableLiveDocs()
        {
            lock (this)
            {
                //Debug.Assert(Thread.holdsLock(Writer));
                Debug.Assert(Info.Info.DocCount > 0);
                //System.out.println("initWritableLivedocs seg=" + info + " liveDocs=" + liveDocs + " shared=" + shared);
                if (LiveDocsShared)
                {
                    // Copy on write: this means we've cloned a
                    // SegmentReader sharing the current liveDocs
                    // instance; must now make a private clone so we can
                    // change it:
                    LiveDocsFormat liveDocsFormat = Info.Info.Codec.LiveDocsFormat;
                    if (LiveDocs_Renamed == null)
                    {
                        //System.out.println("create BV seg=" + info);
                        LiveDocs_Renamed = liveDocsFormat.NewLiveDocs(Info.Info.DocCount);
                    }
                    else
                    {
                        LiveDocs_Renamed = liveDocsFormat.NewLiveDocs(LiveDocs_Renamed);
                    }
                    LiveDocsShared = false;
                }
            }
        }

        public virtual Bits LiveDocs
        {
            get
            {
                lock (this)
                {
                    //Debug.Assert(Thread.holdsLock(Writer));
                    return LiveDocs_Renamed;
                }
            }
        }

        public virtual Bits ReadOnlyLiveDocs // LUCENENET TODO: Make GetReadOnlyLiveDocs() (has side-effect)
        {
            get
            {
                lock (this)
                {
                    //System.out.println("getROLiveDocs seg=" + info);
                    //Debug.Assert(Thread.holdsLock(Writer));
                    LiveDocsShared = true;
                    //if (liveDocs != null) {
                    //System.out.println("  liveCount=" + liveDocs.count());
                    //}
                    return LiveDocs_Renamed;
                }
            }
        }

        public virtual void DropChanges()
        {
            lock (this)
            {
                // Discard (don't save) changes when we are dropping
                // the reader; this is used only on the sub-readers
                // after a successful merge.  If deletes had
                // accumulated on those sub-readers while the merge
                // is running, by now we have carried forward those
                // deletes onto the newly merged segment, so we can
                // discard them on the sub-readers:
                PendingDeleteCount_Renamed = 0;
                DropMergingUpdates();
            }
        }

        // Commit live docs (writes new _X_N.del files) and field updates (writes new
        // _X_N updates files) to the directory; returns true if it wrote any file
        // and false if there were no new deletes or updates to write:
        // TODO (DVU_RENAME) to writeDeletesAndUpdates
        public virtual bool WriteLiveDocs(Directory dir)
        {
            lock (this)
            {
                //Debug.Assert(Thread.holdsLock(Writer));
                //System.out.println("rld.writeLiveDocs seg=" + info + " pendingDelCount=" + pendingDeleteCount + " numericUpdates=" + numericUpdates);
                if (PendingDeleteCount_Renamed == 0)
                {
                    return false;
                }

                // We have new deletes
                Debug.Assert(LiveDocs_Renamed.Length() == Info.Info.DocCount);

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
                    codec.LiveDocsFormat.WriteLiveDocs((MutableBits)LiveDocs_Renamed, trackingDir, Info, PendingDeleteCount_Renamed, IOContext.DEFAULT);
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
                            catch (Exception)
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
                Info.DelCount = Info.DelCount + PendingDeleteCount_Renamed;
                PendingDeleteCount_Renamed = 0;

                return true;
            }
        }

        // Writes field updates (new _X_N updates files) to the directory
        public virtual void WriteFieldUpdates(Directory dir, AbstractDocValuesFieldUpdates.Container dvUpdates)
        {
            lock (this)
            {
                //Debug.Assert(Thread.holdsLock(Writer));
                //System.out.println("rld.writeFieldUpdates: seg=" + info + " numericFieldUpdates=" + numericFieldUpdates);

                Debug.Assert(dvUpdates.Any());

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
                    SegmentReader reader = this.Reader == null ? new SegmentReader(Info, Writer.Config.ReaderTermsIndexDivisor, IOContext.READONCE) : this.Reader;
                    try
                    {
                        // clone FieldInfos so that we can update their dvGen separately from
                        // the reader's infos and write them to a new fieldInfos_gen file
                        FieldInfos.Builder builder = new FieldInfos.Builder(Writer.GlobalFieldNumberMap);
                        // cannot use builder.add(reader.getFieldInfos()) because it does not
                        // clone FI.attributes as well FI.dvGen
                        foreach (FieldInfo fi in reader.FieldInfos)
                        {
                            FieldInfo clone = builder.Add(fi);
                            // copy the stuff FieldInfos.Builder doesn't copy
                            if (fi.Attributes() != null)
                            {
                                foreach (KeyValuePair<string, string> e in fi.Attributes())
                                {
                                    clone.PutAttribute(e.Key, e.Value);
                                }
                            }
                            clone.DocValuesGen = fi.DocValuesGen;
                        }
                        // create new fields or update existing ones to have NumericDV type
                        foreach (string f in dvUpdates.NumericDVUpdates.Keys)
                        {
                            builder.AddOrUpdate(f, NumericDocValuesField.TYPE);
                        }
                        // create new fields or update existing ones to have BinaryDV type
                        foreach (string f in dvUpdates.BinaryDVUpdates.Keys)
                        {
                            builder.AddOrUpdate(f, BinaryDocValuesField.fType);
                        }

                        fieldInfos = builder.Finish();
                        long nextFieldInfosGen = Info.NextFieldInfosGen;
                        string segmentSuffix = nextFieldInfosGen.ToString(CultureInfo.InvariantCulture);//Convert.ToString(nextFieldInfosGen, Character.MAX_RADIX));
                        SegmentWriteState state = new SegmentWriteState(null, trackingDir, Info.Info, fieldInfos, Writer.Config.TermIndexInterval, null, IOContext.DEFAULT, segmentSuffix);
                        DocValuesFormat docValuesFormat = codec.DocValuesFormat;
                        DocValuesConsumer fieldsConsumer = docValuesFormat.FieldsConsumer(state);
                        bool fieldsConsumerSuccess = false;
                        try
                        {
                            //          System.out.println("[" + Thread.currentThread().getName() + "] RLD.writeFieldUpdates: applying numeric updates; seg=" + info + " updates=" + numericFieldUpdates);
                            foreach (KeyValuePair<string, NumericDocValuesFieldUpdates> e in dvUpdates.NumericDVUpdates)
                            {
                                string field = e.Key;
                                NumericDocValuesFieldUpdates fieldUpdates = e.Value;
                                FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                                Debug.Assert(fieldInfo != null);

                                fieldInfo.DocValuesGen = nextFieldInfosGen;
                                // write the numeric updates to a new gen'd docvalues file
                                fieldsConsumer.AddNumericField(fieldInfo, GetLongEnumerable(reader, field, fieldUpdates));
                            }

                            //        System.out.println("[" + Thread.currentThread().getName() + "] RAU.writeFieldUpdates: applying binary updates; seg=" + info + " updates=" + dvUpdates.binaryDVUpdates);
                            foreach (KeyValuePair<string, BinaryDocValuesFieldUpdates> e in dvUpdates.BinaryDVUpdates)
                            {
                                string field = e.Key;
                                BinaryDocValuesFieldUpdates dvFieldUpdates = e.Value;
                                FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                                Debug.Assert(fieldInfo != null);

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
                                IOUtils.CloseWhileHandlingException(fieldsConsumer);
                            }
                        }
                    }
                    finally
                    {
                        if (reader != this.Reader)
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
                            catch (Exception)
                            {
                                // Ignore so we throw only the first exc
                            }
                        }
                    }
                }

                Info.AdvanceFieldInfosGen();
                // copy all the updates to mergingUpdates, so they can later be applied to the merged segment
                if (IsMerging)
                {
                    foreach (KeyValuePair<string, NumericDocValuesFieldUpdates> e in dvUpdates.NumericDVUpdates)
                    {
                        AbstractDocValuesFieldUpdates updates;
                        if (!MergingDVUpdates.TryGetValue(e.Key, out updates))
                        {
                            MergingDVUpdates[e.Key] = e.Value;
                        }
                        else
                        {
                            updates.Merge(e.Value);
                        }
                    }
                    foreach (KeyValuePair<string, BinaryDocValuesFieldUpdates> e in dvUpdates.BinaryDVUpdates)
                    {
                        AbstractDocValuesFieldUpdates updates;
                        if (!MergingDVUpdates.TryGetValue(e.Key, out updates))
                        {
                            MergingDVUpdates[e.Key] = e.Value;
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

                Info.GenUpdatesFiles = newGenUpdatesFiles;

                // wrote new files, should checkpoint()
                Writer.Checkpoint();

                // if there is a reader open, reopen it to reflect the updates
                if (Reader != null)
                {
                    SegmentReader newReader = new SegmentReader(Info, Reader, LiveDocs_Renamed, Info.Info.DocCount - Info.DelCount - PendingDeleteCount_Renamed);
                    bool reopened = false;
                    try
                    {
                        Reader.DecRef();
                        Reader = newReader;
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
        }

        private IEnumerable<long?> GetLongEnumerable(SegmentReader reader, string field, NumericDocValuesFieldUpdates fieldUpdates)
        {
            int maxDoc = reader.MaxDoc;
            Bits DocsWithField = reader.GetDocsWithField(field);
            NumericDocValues currentValues = reader.GetNumericDocValues(field);
            NumericDocValuesFieldUpdates.Iterator iter = (NumericDocValuesFieldUpdates.Iterator)fieldUpdates.GetIterator();
            int updateDoc = iter.NextDoc();

            for (int curDoc = 0; curDoc < maxDoc; ++curDoc)
            {
                if (curDoc == updateDoc) //document has an updated value
                {
                    long? value = (long?)iter.Value(); // either null or updated
                    updateDoc = iter.NextDoc(); //prepare for next round
                    yield return value;
                }
                else
                {   // no update for this document
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

        private IEnumerable<BytesRef> GetBytesRefEnumerable(SegmentReader reader, string field, BinaryDocValuesFieldUpdates fieldUpdates)
        {
            BinaryDocValues currentValues = reader.GetBinaryDocValues(field);
            Bits DocsWithField = reader.GetDocsWithField(field);
            int maxDoc = reader.MaxDoc;
            var iter = (BinaryDocValuesFieldUpdates.Iterator)fieldUpdates.GetIterator();
            int updateDoc = iter.NextDoc();

            for (int curDoc = 0; curDoc < maxDoc; ++curDoc)
            {
                if (curDoc == updateDoc) //document has an updated value
                {
                    BytesRef value = (BytesRef)iter.Value(); // either null or updated
                    updateDoc = iter.NextDoc(); //prepare for next round
                    yield return value;
                }
                else
                {   // no update for this document
                    if (currentValues != null && DocsWithField.Get(curDoc))
                    {
                        var scratch = new BytesRef();
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

        /*
	  private class IterableAnonymousInnerClassHelper : IEnumerable<Number>
	  {
		  private readonly ReadersAndUpdates OuterInstance;

		  private Lucene.Net.Index.SegmentReader Reader;
		  private string Field;
		  private Lucene.Net.Index.NumericDocValuesFieldUpdates FieldUpdates;

		  public IterableAnonymousInnerClassHelper(ReadersAndUpdates outerInstance, Lucene.Net.Index.SegmentReader reader, string field, Lucene.Net.Index.NumericDocValuesFieldUpdates fieldUpdates)
		  {
			  this.OuterInstance = outerInstance;
			  this.Reader = reader;
			  this.Field = field;
			  this.FieldUpdates = fieldUpdates;
			  currentValues = reader.GetNumericDocValues(field);
			  docsWithField = reader.GetDocsWithField(field);
			  maxDoc = reader.MaxDoc;
			  updatesIter = fieldUpdates.Iterator();
		  }

		  internal readonly NumericDocValues currentValues;
		  internal readonly Bits docsWithField;
		  internal readonly int maxDoc;
		  internal readonly NumericDocValuesFieldUpdates.Iterator updatesIter;
		  public virtual IEnumerator<Number> GetEnumerator()
		  {
			updatesIter.Reset();
			return new IteratorAnonymousInnerClassHelper(this);
		  }

		  private class IteratorAnonymousInnerClassHelper : IEnumerator<Number>
		  {
			  private readonly IterableAnonymousInnerClassHelper OuterInstance;

			  public IteratorAnonymousInnerClassHelper(IterableAnonymousInnerClassHelper outerInstance)
			  {
                  this.OuterInstance = outerInstance;
				  curDoc = -1;
				  updateDoc = updatesIter.NextDoc();
			  }

			  internal int curDoc;
			  internal int updateDoc;

			  public virtual bool HasNext()
			  {
				return curDoc < maxDoc - 1;
			  }

			  public virtual Number Next()
			  {
				if (++curDoc >= maxDoc)
				{
				  throw new NoSuchElementException("no more documents to return values for");
				}
				if (curDoc == updateDoc) // this document has an updated value
				{
				  long? value = updatesIter.value(); // either null (unset value) or updated value
				  updateDoc = updatesIter.nextDoc(); // prepare for next round
				  return value;
				}
				else
				{
				  // no update for this document
				  Debug.Assert(curDoc < updateDoc);
				  if (currentValues != null && docsWithField.Get(curDoc))
				  {
					// only read the current value if the document had a value before
					return currentValues.Get(curDoc);
				  }
				  else
				  {
					return null;
				  }
				}
			  }

			  public virtual void Remove()
			  {
				throw new System.NotSupportedException("this iterator does not support removing elements");
			  }
		  }
	  }*/
        /*
	  private class IterableAnonymousInnerClassHelper2 : IEnumerable<BytesRef>
	  {
		  private readonly ReadersAndUpdates OuterInstance;

		  private Lucene.Net.Index.SegmentReader Reader;
		  private string Field;
		  private Lucene.Net.Index.BinaryDocValuesFieldUpdates DvFieldUpdates;

		  public IterableAnonymousInnerClassHelper2(ReadersAndUpdates outerInstance, Lucene.Net.Index.SegmentReader reader, string field, Lucene.Net.Index.BinaryDocValuesFieldUpdates dvFieldUpdates)
		  {
			  this.OuterInstance = outerInstance;
			  this.Reader = reader;
			  this.Field = field;
			  this.DvFieldUpdates = dvFieldUpdates;
			  currentValues = reader.GetBinaryDocValues(field);
			  docsWithField = reader.GetDocsWithField(field);
			  maxDoc = reader.MaxDoc;
			  updatesIter = dvFieldUpdates.Iterator();
		  }

		  internal readonly BinaryDocValues currentValues;
		  internal readonly Bits docsWithField;
		  internal readonly int maxDoc;
		  internal readonly BinaryDocValuesFieldUpdates.Iterator updatesIter;
		  public virtual IEnumerator<BytesRef> GetEnumerator()
		  {
			updatesIter.Reset();
			return new IteratorAnonymousInnerClassHelper2(this);
		  }

		  private class IteratorAnonymousInnerClassHelper2 : IEnumerator<BytesRef>
		  {
			  private readonly IterableAnonymousInnerClassHelper2 OuterInstance;

			  public IteratorAnonymousInnerClassHelper2(IterableAnonymousInnerClassHelper2 outerInstance)
			  {
                  this.OuterInstance = outerInstance;
				  curDoc = -1;
				  updateDoc = updatesIter.nextDoc();
				  scratch = new BytesRef();
			  }

			  internal int curDoc;
			  internal int updateDoc;
			  internal BytesRef scratch;

			  public virtual bool HasNext()
			  {
				return curDoc < maxDoc - 1;
			  }

			  public virtual BytesRef Next()
			  {
				if (++curDoc >= maxDoc)
				{
				  throw new NoSuchElementException("no more documents to return values for");
				}
				if (curDoc == updateDoc) // this document has an updated value
				{
				  BytesRef value = updatesIter.value(); // either null (unset value) or updated value
				  updateDoc = updatesIter.nextDoc(); // prepare for next round
				  return value;
				}
				else
				{
				  // no update for this document
				  Debug.Assert(curDoc < updateDoc);
				  if (currentValues != null && docsWithField.get(curDoc))
				  {
					// only read the current value if the document had a value before
					currentValues.get(curDoc, scratch);
					return scratch;
				  }
				  else
				  {
					return null;
				  }
				}
			  }

			  public virtual void Remove()
			  {
				throw new System.NotSupportedException("this iterator does not support removing elements");
			  }
		  }
	  }*/

        /// <summary>
        /// Returns a reader for merge. this method applies field updates if there are
        /// any and marks that this segment is currently merging.
        /// </summary>
        internal virtual SegmentReader GetReaderForMerge(IOContext context)
        {
            lock (this)
            {
                //Debug.Assert(Thread.holdsLock(Writer));
                // must execute these two statements as atomic operation, otherwise we
                // could lose updates if e.g. another thread calls writeFieldUpdates in
                // between, or the updates are applied to the obtained reader, but then
                // re-applied in IW.commitMergedDeletes (unnecessary work and potential
                // bugs).
                IsMerging = true;
                return GetReader(context);
            }
        }

        /// <summary>
        /// Drops all merging updates. Called from IndexWriter after this segment
        /// finished merging (whether successfully or not).
        /// </summary>
        public virtual void DropMergingUpdates()
        {
            lock (this)
            {
                MergingDVUpdates.Clear();
                IsMerging = false;
            }
        }

        /// <summary>
        /// Returns updates that came in while this segment was merging. </summary>
        public virtual IDictionary<string, AbstractDocValuesFieldUpdates> MergingFieldUpdates
        {
            get
            {
                lock (this)
                {
                    return MergingDVUpdates;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ReadersAndLiveDocs(seg=").Append(Info);
            sb.Append(" pendingDeleteCount=").Append(PendingDeleteCount_Renamed);
            sb.Append(" liveDocsShared=").Append(LiveDocsShared);
            return sb.ToString();
        }
    }
}