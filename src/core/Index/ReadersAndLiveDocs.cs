using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Index
{
    internal class ReadersAndLiveDocs
    {
        // Not final because we replace (clone) when we need to
        // change it and it's been shared:
        private readonly SegmentInfoPerCommit info;

        // Tracks how many consumers are using this instance:
        private int refCount = 1;

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

        // Holds the current shared (readable and writable
        // liveDocs).  This is null when there are no deleted
        // docs, and it's copy-on-write (cloned whenever we need
        // to change it but it's been shared to an external NRT
        // reader).
        private IBits liveDocs;

        // How many further deletions we've done against
        // liveDocs vs when we loaded it or last wrote it:
        private int pendingDeleteCount;

        // True if the current liveDocs is referenced by an
        // external NRT reader:
        private bool shared;

        // Not final because we replace (clone) when we need to
        // change it and it's been shared:
        public SegmentInfoPerCommit Info
        {
            get
            {
                return this.info;
            }
        }
        
        private IndexWriter Writer
        {
            get
            {
                return this.writer;
            }
        }

        public ReadersAndLiveDocs(IndexWriter writer, SegmentInfoPerCommit info)
        {
            this.info = info;
            this.writer = writer;
            shared = true;
        }

        public void IncRef()
        {
            Interlocked.Increment(ref refCount);
        }

        public void DecRef()
        {
            Interlocked.Decrement(ref refCount);
        }

        // Tracks how many consumers are using this instance:
        public int RefCount
        {
            get
            {
                return this.refCount;
            }
            private set
            {
                Interlocked.Exchange(ref refCount, value);
            }
        }

        public int GetPendingDeleteCount()
        {
            lock (this)
            {
                return pendingDeleteCount;
            }
        }

        // Call only from assert!
        public bool VerifyDocCounts()
        {
            lock (this)
            {
                int count;
                if (liveDocs != null)
                {
                    count = 0;
                    for (int docID = 0; docID < this.Info.Info.DocCount; docID++)
                    {
                        if (liveDocs[docID])
                        {
                            count++;
                        }
                    }
                }
                else
                {
                    count = this.Info.Info.DocCount;
                }

                //assert info.info.getDocCount() - info.getDelCount() - pendingDeleteCount == count: "info.docCount=" + info.info.getDocCount() + " info.getDelCount()=" + info.getDelCount() + " pendingDeleteCount=" + pendingDeleteCount + " count=" + count;
                return true;
            }
        }

        // Get reader for searching/deleting
        public SegmentReader GetReader(IOContext context)
        {
            lock (this)
            {
                if (reader == null)
                {
                    // We steal returned ref:
                    reader = new SegmentReader(this.Info, this.Writer.Config.ReaderTermsIndexDivisor, context);
                    if (liveDocs == null)
                    {
                        liveDocs = reader.GetLiveDocs();
                    }
                }

                // Ref for caller
                reader.IncRef();
                return reader;
            }
        }

        // Get reader for merging (does not load the terms
        // index):
        public SegmentReader GetMergeReader(IOContext context)
        {
            lock (this)
            {
                if (mergeReader == null)
                {
                    if (reader != null)
                    {
                        // Just use the already opened non-merge reader
                        // for merging.  In the NRT case this saves us
                        // pointless double-open:
                        // Ref for us:
                        reader.IncRef();
                        mergeReader = reader;
                    }
                    else
                    {
                        //System.out.println(Thread.currentThread().getName() + ": getMergeReader seg=" + info.name);
                        // We steal returned ref:
                        mergeReader = new SegmentReader(this.Info, -1, context);

                        if (liveDocs == null)
                        {
                            liveDocs = mergeReader.GetLiveDocs();
                        }
                    }
                }

                // Ref for caller
                mergeReader.IncRef();
                return mergeReader;
            }
        }

        public void Release(SegmentReader sr)
        {
            lock (this)
            {
                //assert info == sr.getSegmentInfo();
                sr.DecRef();
            }
        }

        public bool Delete(int docID)
        {
            lock (this)
            {
                //assert liveDocs != null;
                //assert Thread.holdsLock(writer);
                //assert docID >= 0 && docID < liveDocs.length() : "out of bounds: docid=" + docID + " liveDocsLength=" + liveDocs.length() + " seg=" + info.info.name + " docCount=" + info.info.getDocCount();
                //assert !shared;
                bool didDelete = liveDocs[docID];
                if (didDelete)
                {
                    ((IMutableBits)liveDocs).Clear(docID);
                    pendingDeleteCount++;
                    //System.out.println("  new del seg=" + info + " docID=" + docID + " pendingDelCount=" + pendingDeleteCount + " totDelCount=" + (info.docCount-liveDocs.count()));
                }

                return didDelete;
            }
        }

        // NOTE: removes callers ref
        public void DropReaders()
        {
            // TODO: can we somehow use IOUtils here...?  problem is
            // we are calling .decRef not .close)...
            lock (this)
            {
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
        }

        /**
        * Returns a ref to a clone.  NOTE: this clone is not
        * enrolled in the pool, so you should simply close()
        * it when you're done (ie, do not call release()).
        */
        public SegmentReader GetReadOnlyClone(IOContext context)
        {
            lock (this)
            {
                if (reader == null)
                {
                    GetReader(context).DecRef();
                    //assert reader != null;
                }
                shared = true;
                if (liveDocs != null)
                {
                    return new SegmentReader(reader.GetSegmentInfo(), reader.core, liveDocs, this.Info.info.getDocCount() - this.Info.GetDelCount() - pendingDeleteCount);
                }
                else
                {
                    //assert reader.getLiveDocs() == liveDocs;
                    reader.IncRef();
                    return reader;
                }
            }
        }

        public void InitWritableLiveDocs()
        {
            lock (this)
            {
                //assert Thread.holdsLock(writer);
                //assert info.info.getDocCount() > 0;
                //System.out.println("initWritableLivedocs seg=" + info + " liveDocs=" + liveDocs + " shared=" + shared);
                if (shared)
                {
                    // Copy on write: this means we've cloned a
                    // SegmentReader sharing the current liveDocs
                    // instance; must now make a private clone so we can
                    // change it:
                    LiveDocsFormat liveDocsFormat = info.info.getCodec().liveDocsFormat();
                    if (liveDocs == null)
                    {
                        //System.out.println("create BV seg=" + info);
                        liveDocs = liveDocsFormat.newLiveDocs(info.info.getDocCount());
                    }
                    else
                    {
                        liveDocs = liveDocsFormat.newLiveDocs(liveDocs);
                    }
                    shared = false;
                }
                else
                {
                    //assert liveDocs != null;
                }
            }
        }

        public IBits LiveDocs
        {
            get
            {
                lock (this)
                {
                    //assert Thread.holdsLock(writer);
                    return liveDocs;
                }
            }
        }

        public IBits ReadOnlyLiveDocs
        {
            get
            {
                lock (this)
                {
                    //System.out.println("getROLiveDocs seg=" + info);
                    //assert Thread.holdsLock(writer);
                    shared = true;
                    //if (liveDocs != null) {
                    //System.out.println("  liveCount=" + liveDocs.count());
                    //}
                    return liveDocs;
                }
            }
        }

        public void dropChanges()
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

                pendingDeleteCount = 0;
            }
        }

        // Commit live docs to the directory (writes new
        // _X_N.del files); returns true if it wrote the file
        // and false if there were no new deletes to write:
        public bool WriteLiveDocs(Directory dir)
        {
            //System.out.println("rld.writeLiveDocs seg=" + info + " pendingDelCount=" + pendingDeleteCount);
            if (pendingDeleteCount != 0)
            {
                // We have new deletes
                //assert liveDocs.length() == info.info.getDocCount();

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
                    info.info.getCodec().liveDocsFormat().writeLiveDocs((IMutableBits)liveDocs, trackingDir, info, pendingDeleteCount, IOContext.DEFAULT);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // Advance only the nextWriteDelGen so that a 2nd
                        // attempt to write will write to a new file
                        info.advanceNextWriteDelGen();

                        // Delete any partially created file(s):
                        foreach (string fileName in trackingDir.GetCreatedFiles())
                        {
                            try
                            {
                                dir.deleteFile(fileName);
                            }
                            catch (Throwable t)
                            {
                                // Ignore so we throw only the first exc
                            }
                        }
                    }
                }

                // If we hit an exc in the line above (eg disk full)
                // then info's delGen remains pointing to the previous
                // (successfully written) del docs:
                info.advanceDelGen();
                info.setDelCount(info.getDelCount() + pendingDeleteCount);

                pendingDeleteCount = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual string ToString()
        {
            return "ReadersAndLiveDocs(seg=" + info + " pendingDeleteCount=" + pendingDeleteCount + " shared=" + shared + ")";
        }
    }
}
