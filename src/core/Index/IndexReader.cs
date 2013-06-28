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

using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Document = Lucene.Net.Documents.Document;
using Lucene.Net.Store;
using System.Collections.Concurrent;
using Lucene.Net.Support;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Index
{

    /// <summary>IndexReader is an abstract class, providing an interface for accessing an
    /// index.  Search of an index is done entirely through this abstract interface,
    /// so that any subclass which implements it is searchable.
    /// <p/> Concrete subclasses of IndexReader are usually constructed with a call to
    /// one of the static <c>open()</c> methods, e.g. <see cref="Open(Lucene.Net.Store.Directory, bool)" />
    ///.
    /// <p/> For efficiency, in this API documents are often referred to via
    /// <i>document numbers</i>, non-negative integers which each name a unique
    /// document in the index.  These document numbers are ephemeral--they may change
    /// as documents are added to and deleted from an index.  Clients should thus not
    /// rely on a given document having the same number between sessions.
    /// <p/> An IndexReader can be opened on a directory for which an IndexWriter is
    /// opened already, but it cannot be used to delete documents from the index then.
    /// <p/>
    /// <b>NOTE</b>: for backwards API compatibility, several methods are not listed 
    /// as abstract, but have no useful implementations in this base class and 
    /// instead always throw UnsupportedOperationException.  Subclasses are 
    /// strongly encouraged to override these methods, but in many cases may not 
    /// need to.
    /// <p/>
    /// <p/>
    /// <b>NOTE</b>: as of 2.4, it's possible to open a read-only
    /// IndexReader using the static open methods that accepts the
    /// boolean readOnly parameter.  Such a reader has better
    /// better concurrency as it's not necessary to synchronize on the
    /// isDeleted method.  You must explicitly specify false
    /// if you want to make changes with the resulting IndexReader.
    /// <p/>
    /// <a name="thread-safety"></a><p/><b>NOTE</b>: <see cref="IndexReader" />
    /// instances are completely thread
    /// safe, meaning multiple threads can call any of its methods,
    /// concurrently.  If your application requires external
    /// synchronization, you should <b>not</b> synchronize on the
    /// <c>IndexReader</c> instance; use your own
    /// (non-Lucene) objects instead.
    /// </summary>
    public abstract class IndexReader : IDisposable
    {
        private bool closed = false;
        private bool closedByChild = false;
        private int refCount = 1;

        protected IndexReader()
        {
            if (!(this is CompositeReader || this is AtomicReader))
                throw new Exception("IndexReader should never be directly extended, subclass AtomicReader or CompositeReader instead.");
        }

        public interface IReaderClosedListener
        {
            void OnClose(IndexReader reader);
        }

        private readonly ISet<IReaderClosedListener> readerClosedListeners = new ConcurrentHashSet<IReaderClosedListener>();

        // .NET port: since the java version uses a backing map of WeakHashMap, this is a similar approach. However, WeakHashMap auto-removes 
        // dead items, but this doesn't... so we may want to think about changing this to be a set equivalent of WeakIdentityMap.
        private readonly ISet<IdentityWeakReference<IndexReader>> parentReaders = new ConcurrentHashSet<IdentityWeakReference<IndexReader>>();

        public void AddReaderClosedListener(IReaderClosedListener listener)
        {
            EnsureOpen();
            readerClosedListeners.Add(listener);
        }

        public void RemoveReaderClosedListener(IReaderClosedListener listener)
        {
            EnsureOpen();
            readerClosedListeners.Remove(listener);
        }

        public void RegisterParentReader(IndexReader reader)
        {
            EnsureOpen();

            // .NET port: need to make this a weak reference, see note on parentReaders above.
            var wr = new IdentityWeakReference<IndexReader>(reader);
            parentReaders.Add(wr);
        }

        private void NotifyReaderClosedListeners()
        {
            lock (readerClosedListeners) // .NET port: is this necessary?
            {
                foreach (IReaderClosedListener listener in readerClosedListeners)
                {
                    listener.OnClose(this);
                }
            }
        }

        private void ReportCloseToParentReaders()
        {
            lock (parentReaders)
            {
                foreach (IdentityWeakReference<IndexReader> parent in parentReaders)
                {
                    // .NET port: using a weak reference. see note on parentReaders above.
                    IndexReader target = parent.Target;

                    if (target != null)
                    {
                        target.closedByChild = true;
                        // cross memory barrier by a fake write: -- .NET port: is this necessary?
                        Interlocked.Add(ref target.refCount, 0);
                        // recurse:
                        target.ReportCloseToParentReaders();
                    }
                }
            }
        }

        /// <summary>Expert: returns the current refCount for this reader </summary>
        public virtual int RefCount
        {
            get
            {
                return refCount;
            }
        }

        /// <summary> Expert: increments the refCount of this IndexReader
        /// instance.  RefCounts are used to determine when a
        /// reader can be closed safely, i.e. as soon as there are
        /// no more references.  Be sure to always call a
        /// corresponding <see cref="DecRef" />, in a finally clause;
        /// otherwise the reader may never be closed.  Note that
        /// <see cref="Close" /> simply calls decRef(), which means that
        /// the IndexReader will not really be closed until <see cref="DecRef" />
        /// has been called for all outstanding
        /// references.
        /// 
        /// </summary>
        /// <seealso cref="DecRef">
        /// </seealso>
        public virtual void IncRef()
        {
            // .NET port: using Interlocked instead of lock(this)
            EnsureOpen();
            Interlocked.Increment(ref refCount);
        }

        public bool TryIncRef()
        {
            // .NET port: using Interlocked instead of lock(this)
            int count;

            while ((count = refCount) > 0)
            {
                // .NET port: atomic integer parameters are backwards from compareexchange. 
                // AtomicInteger.compareAndSet(expect, update) returns true if original == expect.
                // CompareExchange(location, value, comparand) returns the original value, and only updates location to value if original == comparand, so
                // we can detect if it was updated by comparing the returned value to the comparand.
                if (Interlocked.CompareExchange(ref refCount, count + 1, count) == count)
                    return true;
            }

            return false;
        }

        /// <summary> Expert: decreases the refCount of this IndexReader
        /// instance.  If the refCount drops to 0, then pending
        /// changes (if any) are committed to the index and this
        /// reader is closed.
        /// 
        /// </summary>
        /// <throws>  IOException in case an IOException occurs in commit() or doClose() </throws>
        /// <summary> 
        /// </summary>
        /// <seealso cref="IncRef">
        /// </seealso>
        public virtual void DecRef()
        {
            if (refCount <= 0)
                throw new AlreadyClosedException("this IndexReader is closed");

            int rc = Interlocked.Decrement(ref refCount);
            if (rc == 0)
            {
                bool success = false;
                try
                {
                    DoClose();
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // put reference back on failure
                        Interlocked.Increment(ref refCount);
                    }
                }

                ReportCloseToParentReaders();
                NotifyReaderClosedListeners();
            }
            else if (rc < 0)
            {
                throw new InvalidOperationException("too many decRef calls: refCount is " + rc + " after decrement");
            }
        }
        
        /// <throws>  AlreadyClosedException if this IndexReader is closed </throws>
        protected internal void EnsureOpen()
        {
            if (refCount <= 0)
            {
                throw new AlreadyClosedException("this IndexReader is closed");
            }

            // the happens before rule on reading the refCount, which must be after the fake write,
            // ensures that we see the value:
            if (closedByChild)
            {
                throw new AlreadyClosedException("this IndexReader cannot be used anymore as one of its child readers was closed");
            }
        }

        public sealed override bool Equals(object obj)
        {
            return this == obj;
        }

        public sealed override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }
                
        [Obsolete("Use DirectoryReader.Open")]
        public static DirectoryReader Open(Directory directory)
        {
            return DirectoryReader.Open(directory);
        }

        [Obsolete("Use DirectoryReader.Open")]
        public static DirectoryReader Open(Directory directory, int termInfosIndexDivisor)
        {
            return DirectoryReader.Open(directory, termInfosIndexDivisor);
        }

        [Obsolete("Use DirectoryReader.Open")]
        public static DirectoryReader Open(IndexWriter writer, bool applyAllDeletes)
        {
            return DirectoryReader.Open(writer, applyAllDeletes);
        }

        [Obsolete("Use DirectoryReader.Open")]
        public static DirectoryReader Open(IndexCommit commit)
        {
            return DirectoryReader.Open(commit);
        }

        [Obsolete("Use DirectoryReader.Open")]
        public static DirectoryReader Open(IndexCommit commit, int termInfosIndexDivisor)
        {
            return DirectoryReader.Open(commit, termInfosIndexDivisor);
        }
        
        public abstract Fields GetTermVectors(int docID);

        public Terms GetTermVector(int docID, string field)
        {
            Fields vectors = GetTermVectors(docID);

            if (vectors == null)
                return null;

            return vectors.Terms(field);
        }

        /// <summary>Returns the number of documents in this index. </summary>
        public abstract int NumDocs { get; }

        /// <summary>Returns one greater than the largest possible document number.
        /// This may be used to, e.g., determine how big to allocate an array which
        /// will have an element for every document number in an index.
        /// </summary>
        public abstract int MaxDoc { get; }

        /// <summary>Returns the number of deleted documents. </summary>
        public virtual int NumDeletedDocs
        {
            get { return MaxDoc - NumDocs; }
        }

        public abstract void Document(int docID, StoredFieldVisitor visitor);

        /// <summary> Returns the stored fields of the <c>n</c><sup>th</sup>
        /// <c>Document</c> in this index.
        /// <p/>
        /// <b>NOTE:</b> for performance reasons, this method does not check if the
        /// requested document is deleted, and therefore asking for a deleted document
        /// may yield unspecified results. Usually this is not required, however you
        /// can call <see cref="IsDeleted(int)" /> with the requested document ID to verify
        /// the document is not deleted.
        /// 
        /// </summary>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <exception cref="System.IO.IOException">If there is a low-level IO error</exception>
        public Document Document(int docID)
        {
            DocumentStoredFieldVisitor visitor = new DocumentStoredFieldVisitor();
            Document(docID, visitor);
            return visitor.Document;
        }

        public Document Document(int docID, ISet<string> fieldsToLoad)
        {
            DocumentStoredFieldVisitor visitor = new DocumentStoredFieldVisitor(fieldsToLoad);
            Document(docID, visitor);
            return visitor.Document;
        }

        /// <summary> Returns the stored fields of the <c>n</c><sup>th</sup>
        /// <c>Document</c> in this index.
        /// <p/>
        /// <b>NOTE:</b> for performance reasons, this method does not check if the
        /// requested document is deleted, and therefore asking for a deleted document
        /// may yield unspecified results. Usually this is not required, however you
        /// can call <see cref="IsDeleted(int)" /> with the requested document ID to verify
        /// the document is not deleted.
        /// 
        /// </summary>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <exception cref="System.IO.IOException">If there is a low-level IO error</exception>
        public Document this[int doc]
        {
            get { return Document(doc); }
        }
        
        /// <summary>Returns true if any documents have been deleted </summary>
        public virtual bool HasDeletions
        {
            get
            {
                return NumDeletedDocs > 0;
            }
        }
        
        /// <summary> Closes files associated with this index.
        /// Also saves any new deletions to disk.
        /// No other methods should be called after this has been called.
        /// </summary>
        /// <exception cref="System.IO.IOException">If there is a low-level IO error</exception>
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this) 
                {
                    if (!closed)
                    {
                        DecRef();
                        closed = true;
                    }
                }
            }
        }

        /// <summary>Implements close. </summary>
        protected abstract void DoClose();

        public abstract IndexReaderContext Context { get; }

        public IList<AtomicReaderContext> Leaves
        {
            get
            {
                return Context.Leaves;
            }
        }

        public virtual object CoreCacheKey
        {
            get { return this; }
        }

        public virtual object CombinedCoreAndDeletesKey
        {
            get { return this; }
        }

        public abstract int DocFreq(Term term);

        public abstract long TotalTermFreq(Term term);

        public abstract long GetSumDocFreq(string field);

        public abstract int GetDocCount(string field);

        public abstract long GetSumTotalTermFreq(string field);
    }
}