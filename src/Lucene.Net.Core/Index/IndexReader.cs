using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using System.Runtime.CompilerServices;
    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
    using Directory = Lucene.Net.Store.Directory;

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

    using Document = Documents.Document;
    using DocumentStoredFieldVisitor = DocumentStoredFieldVisitor;
    using IOUtils = Lucene.Net.Util.IOUtils;

    // javadocs

    /// <summary>
    /// IndexReader is an abstract class, providing an interface for accessing an
    /// index.  Search of an index is done entirely through this abstract interface,
    /// so that any subclass which implements it is searchable.
    ///
    /// <p>There are two different types of IndexReaders:
    /// <ul>
    ///  <li><seealso cref="AtomicReader"/>: These indexes do not consist of several sub-readers,
    ///  they are atomic. They support retrieval of stored fields, doc values, terms,
    ///  and postings.
    ///  <li><seealso cref="CompositeReader"/>: Instances (like <seealso cref="DirectoryReader"/>)
    ///  of this reader can only
    ///  be used to get stored fields from the underlying AtomicReaders,
    ///  but it is not possible to directly retrieve postings. To do that, get
    ///  the sub-readers via <seealso cref="CompositeReader#getSequentialSubReaders"/>.
    ///  Alternatively, you can mimic an <seealso cref="AtomicReader"/> (with a serious slowdown),
    ///  by wrapping composite readers with <seealso cref="SlowCompositeReaderWrapper"/>.
    /// </ul>
    ///
    /// <p>IndexReader instances for indexes on disk are usually constructed
    /// with a call to one of the static <code>DirectoryReader.open()</code> methods,
    /// e.g. <seealso cref="DirectoryReader#open(Lucene.Net.Store.Directory)"/>. <seealso cref="DirectoryReader"/> implements
    /// the <seealso cref="CompositeReader"/> interface, it is not possible to directly get postings.
    ///
    /// <p> For efficiency, in this API documents are often referred to via
    /// <i>document numbers</i>, non-negative integers which each name a unique
    /// document in the index.  These document numbers are ephemeral -- they may change
    /// as documents are added to and deleted from an index.  Clients should thus not
    /// rely on a given document having the same number between sessions.
    ///
    /// <p>
    /// <a name="thread-safety"></a><p><b>NOTE</b>: {@link
    /// IndexReader} instances are completely thread
    /// safe, meaning multiple threads can call any of its methods,
    /// concurrently.  If your application requires external
    /// synchronization, you should <b>not</b> synchronize on the
    /// <code>IndexReader</code> instance; use your own
    /// (non-Lucene) objects instead.
    /// </summary>
    public abstract class IndexReader : IDisposable
    {
        private bool closed = false;
        private bool closedByChild = false;
        private readonly AtomicInteger refCount = new AtomicInteger(1);

        internal IndexReader()
        {
            if (!(this is CompositeReader || this is AtomicReader))
            {
                throw new Exception("IndexReader should never be directly extended, subclass AtomicReader or CompositeReader instead.");
            }
        }

        /// <summary>
        /// A custom listener that's invoked when the IndexReader
        /// is closed.
        ///
        /// @lucene.experimental
        /// </summary>
        public interface IReaderClosedListener
        {
            /// <summary>
            /// Invoked when the <seealso cref="IndexReader"/> is closed. </summary>
            void OnClose(IndexReader reader);
        }

        private readonly ISet<IReaderClosedListener> readerClosedListeners = new ConcurrentHashSet<IReaderClosedListener>();

        //LUCENE TO-DO
        //private readonly ISet<IndexReader> ParentReaders = Collections.synchronizedSet(Collections.newSetFromMap(new WeakHashMap<IndexReader, bool?>()));
        private readonly ISet<IdentityWeakReference<IndexReader>> parentReaders = new ConcurrentHashSet<IdentityWeakReference<IndexReader>>();

        /// <summary>
        /// Expert: adds a <seealso cref="IReaderClosedListener"/>.  The
        /// provided listener will be invoked when this reader is closed.
        ///
        /// @lucene.experimental
        /// </summary>
        public void AddReaderClosedListener(IReaderClosedListener listener)
        {
            EnsureOpen();
            readerClosedListeners.Add(listener);
        }

        /// <summary>
        /// Expert: remove a previously added <seealso cref="IReaderClosedListener"/>.
        ///
        /// @lucene.experimental
        /// </summary>
        public void RemoveReaderClosedListener(IReaderClosedListener listener)
        {
            EnsureOpen();
            readerClosedListeners.Remove(listener);
        }

        /// <summary>
        /// Expert: this method is called by {@code IndexReader}s which wrap other readers
        /// (e.g. <seealso cref="CompositeReader"/> or <seealso cref="FilterAtomicReader"/>) to register the parent
        /// at the child (this reader) on construction of the parent. When this reader is closed,
        /// it will mark all registered parents as closed, too. The references to parent readers
        /// are weak only, so they can be GCed once they are no longer in use.
        /// @lucene.experimental
        /// </summary>
        public void RegisterParentReader(IndexReader reader)
        {
            EnsureOpen();
            parentReaders.Add(new IdentityWeakReference<IndexReader>(reader));
        }

        private void NotifyReaderClosedListeners(Exception th)
        {
            lock (readerClosedListeners)
            {
                foreach (IReaderClosedListener listener in readerClosedListeners)
                {
                    try
                    {
                        listener.OnClose(this);
                    }
                    catch (Exception t)
                    {
                        if (th == null)
                        {
                            th = t;
                        }
                        else
                        {
                            //th.AddSuppressed(t);
                            // LUCENENET TODO - Figure out how to track these exceptions
                            // Drop the exception instead of wrapping in AggregateException.
                            // Wrapping will change the exception type and change flow control.
                        }
                    }
                }
                IOUtils.ReThrowUnchecked(th);
            }
        }

        private void ReportCloseToParentReaders()
        {
            lock (parentReaders)
            {
                foreach (IdentityWeakReference<IndexReader> parent in parentReaders)
                {
                    //Using weak references
                    IndexReader target = parent.Target;

                    if (target != null)
                    {
                        target.closedByChild = true;
                        // cross memory barrier by a fake write:
                        target.refCount.AddAndGet(0);
                        // recurse:
                        target.ReportCloseToParentReaders();
                    }
                }
            }
        }

        /// <summary>
        /// Expert: returns the current refCount for this reader </summary>
        public int RefCount
        {
            get
            {
                // NOTE: don't ensureOpen, so that callers can see
                // refCount is 0 (reader is closed)
                return refCount.Get();
            }
        }

        /// <summary>
        /// Expert: increments the refCount of this IndexReader
        /// instance.  RefCounts are used to determine when a
        /// reader can be closed safely, i.e. as soon as there are
        /// no more references.  Be sure to always call a
        /// corresponding <seealso cref="#decRef"/>, in a finally clause;
        /// otherwise the reader may never be closed.  Note that
        /// <seealso cref="#close"/> simply calls decRef(), which means that
        /// the IndexReader will not really be closed until {@link
        /// #decRef} has been called for all outstanding
        /// references.
        /// </summary>
        /// <seealso cref= #decRef </seealso>
        /// <seealso cref= #tryIncRef </seealso>
        public void IncRef()
        {
            if (!TryIncRef())
            {
                EnsureOpen();
            }
        }

        /// <summary>
        /// Expert: increments the refCount of this IndexReader
        /// instance only if the IndexReader has not been closed yet
        /// and returns <code>true</code> iff the refCount was
        /// successfully incremented, otherwise <code>false</code>.
        /// If this method returns <code>false</code> the reader is either
        /// already closed or is currently being closed. Either way this
        /// reader instance shouldn't be used by an application unless
        /// <code>true</code> is returned.
        /// <p>
        /// RefCounts are used to determine when a
        /// reader can be closed safely, i.e. as soon as there are
        /// no more references.  Be sure to always call a
        /// corresponding <seealso cref="#decRef"/>, in a finally clause;
        /// otherwise the reader may never be closed.  Note that
        /// <seealso cref="#close"/> simply calls decRef(), which means that
        /// the IndexReader will not really be closed until {@link
        /// #decRef} has been called for all outstanding
        /// references.
        /// </summary>
        /// <seealso cref= #decRef </seealso>
        /// <seealso cref= #incRef </seealso>
        public bool TryIncRef()
        {
            int count;
            while ((count = refCount.Get()) > 0)
            {
                if (refCount.CompareAndSet(count, count + 1))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Expert: decreases the refCount of this IndexReader
        /// instance.  If the refCount drops to 0, then this
        /// reader is closed.  If an exception is hit, the refCount
        /// is unchanged.
        /// </summary>
        /// <exception cref="IOException"> in case an IOException occurs in  doClose()
        /// </exception>
        /// <seealso cref= #incRef </seealso>
        public void DecRef()
        {
            // only check refcount here (don't call ensureOpen()), so we can
            // still close the reader if it was made invalid by a child:
            if (refCount.Get() <= 0)
            {
                throw new AlreadyClosedException("this IndexReader is closed");
            }

            int rc = refCount.DecrementAndGet();
            if (rc == 0)
            {
                closed = true;
                Exception throwable = null;
                try
                {
                    DoClose();
                }
                catch (Exception th)
                {
                    throwable = th;
                }
                finally
                {
                    try
                    {
                        ReportCloseToParentReaders();
                    }
                    finally
                    {
                        NotifyReaderClosedListeners(throwable);
                    }
                }
            }
            else if (rc < 0)
            {
                throw new InvalidOperationException("too many decRef calls: refCount is " + rc + " after decrement");
            }
        }

        /// <summary>
        /// Throws AlreadyClosedException if this IndexReader or any
        /// of its child readers is closed, otherwise returns.
        /// </summary>
        protected internal void EnsureOpen()
        {
            if (refCount.Get() <= 0)
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

        /// <summary>
        /// {@inheritDoc}
        /// <p>For caching purposes, {@code IndexReader} subclasses are not allowed
        /// to implement equals/hashCode, so methods are declared sealed.
        /// To lookup instances from caches use <seealso cref="#getCoreCacheKey"/> and
        /// <seealso cref="#getCombinedCoreAndDeletesKey"/>.
        /// </summary>
        public override sealed bool Equals(object obj)
        {
            return (this == obj);
        }

        /// <summary>
        /// {@inheritDoc}
        /// <p>For caching purposes, {@code IndexReader} subclasses are not allowed
        /// to implement equals/hashCode, so methods are declared final.
        /// To lookup instances from caches use <seealso cref="#getCoreCacheKey"/> and
        /// <seealso cref="#getCombinedCoreAndDeletesKey"/>.
        /// </summary>
        public override sealed int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        /// <summary>
        /// Returns a IndexReader reading the index in the given
        ///  Directory </summary>
        /// <param name="directory"> the index directory </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// @deprecated Use <seealso cref="DirectoryReader#open(Directory)"/>
        [Obsolete("Use DirectoryReader.Open(Directory)")]
        public static DirectoryReader Open(Directory directory)
        {
            return DirectoryReader.Open(directory);
        }

        /// <summary>
        /// Expert: Returns a IndexReader reading the index in the given
        ///  Directory with the given termInfosIndexDivisor. </summary>
        /// <param name="directory"> the index directory </param>
        /// <param name="termInfosIndexDivisor"> Subsamples which indexed
        ///  terms are loaded into RAM. this has the same effect as {@link
        ///  IndexWriterConfig#setTermIndexInterval} except that setting
        ///  must be done at indexing time while this setting can be
        ///  set per reader.  When set to N, then one in every
        ///  N*termIndexInterval terms in the index is loaded into
        ///  memory.  By setting this to a value > 1 you can reduce
        ///  memory usage, at the expense of higher latency when
        ///  loading a TermInfo.  The default value is 1.  Set this
        ///  to -1 to skip loading the terms index entirely. </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// @deprecated Use <seealso cref="DirectoryReader#open(Directory,int)"/>
        [Obsolete("Use DirectoryReader.Open(Directory,int)")]
        public static DirectoryReader Open(Directory directory, int termInfosIndexDivisor)
        {
            return DirectoryReader.Open(directory, termInfosIndexDivisor);
        }

        /// <summary>
        /// Open a near real time IndexReader from the <seealso cref="Lucene.Net.Index.IndexWriter"/>.
        /// </summary>
        /// <param name="writer"> The IndexWriter to open from </param>
        /// <param name="applyAllDeletes"> If true, all buffered deletes will
        /// be applied (made visible) in the returned reader.  If
        /// false, the deletes are not applied but remain buffered
        /// (in IndexWriter) so that they will be applied in the
        /// future.  Applying deletes can be costly, so if your app
        /// can tolerate deleted documents being returned you might
        /// gain some performance by passing false. </param>
        /// <returns> The new IndexReader </returns>
        /// <exception cref="IOException"> if there is a low-level IO error
        /// </exception>
        /// <seealso cref= DirectoryReader#openIfChanged(DirectoryReader,IndexWriter,boolean)
        ///
        /// @lucene.experimental </seealso>
        /// @deprecated Use <seealso cref="DirectoryReader#open(IndexWriter,boolean)"/>
        [Obsolete("Use DirectoryReader.Open(IndexWriter,bool)")]
        public static DirectoryReader Open(IndexWriter writer, bool applyAllDeletes)
        {
            return DirectoryReader.Open(writer, applyAllDeletes);
        }

        /// <summary>
        /// Expert: returns an IndexReader reading the index in the given
        ///  <seealso cref="IndexCommit"/>. </summary>
        /// <param name="commit"> the commit point to open </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// @deprecated Use <seealso cref="DirectoryReader#open(IndexCommit)"/>
        [Obsolete("Use DirectoryReader.Open(IndexCommit)")]
        public static DirectoryReader Open(IndexCommit commit)
        {
            return DirectoryReader.Open(commit);
        }

        /// <summary>
        /// Expert: returns an IndexReader reading the index in the given
        ///  <seealso cref="IndexCommit"/> and termInfosIndexDivisor. </summary>
        /// <param name="commit"> the commit point to open </param>
        /// <param name="termInfosIndexDivisor"> Subsamples which indexed
        ///  terms are loaded into RAM. this has the same effect as {@link
        ///  IndexWriterConfig#setTermIndexInterval} except that setting
        ///  must be done at indexing time while this setting can be
        ///  set per reader.  When set to N, then one in every
        ///  N*termIndexInterval terms in the index is loaded into
        ///  memory.  By setting this to a value > 1 you can reduce
        ///  memory usage, at the expense of higher latency when
        ///  loading a TermInfo.  The default value is 1.  Set this
        ///  to -1 to skip loading the terms index entirely. </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// @deprecated Use <seealso cref="DirectoryReader#open(IndexCommit,int)"/>
        [Obsolete("Use DirectoryReader.Open(IndexCommit,int)/>")]
        public static DirectoryReader Open(IndexCommit commit, int termInfosIndexDivisor)
        {
            return DirectoryReader.Open(commit, termInfosIndexDivisor);
        }

        /// <summary>
        /// Retrieve term vectors for this document, or null if
        ///  term vectors were not indexed.  The returned Fields
        ///  instance acts like a single-document inverted index
        ///  (the docID will be 0).
        /// </summary>
        public abstract Fields GetTermVectors(int docID);

        /// <summary>
        /// Retrieve term vector for this document and field, or
        ///  null if term vectors were not indexed.  The returned
        ///  Fields instance acts like a single-document inverted
        ///  index (the docID will be 0).
        /// </summary>
        public Terms GetTermVector(int docID, string field)
        {
            Fields vectors = GetTermVectors(docID);
            if (vectors == null)
            {
                return null;
            }
            return vectors.Terms(field);
        }

        /// <summary>
        /// Returns the number of documents in this index. </summary>
        public abstract int NumDocs { get; }

        /// <summary>
        /// Returns one greater than the largest possible document number.
        /// this may be used to, e.g., determine how big to allocate an array which
        /// will have an element for every document number in an index.
        /// </summary>
        public abstract int MaxDoc { get; }

        /// <summary>
        /// Returns the number of deleted documents. </summary>
        public int NumDeletedDocs
        {
            get { return MaxDoc - NumDocs; }
        }

        /// <summary>
        /// Expert: visits the fields of a stored document, for
        ///  custom processing/loading of each field.  If you
        ///  simply want to load all fields, use {@link
        ///  #document(int)}.  If you want to load a subset, use
        ///  <seealso cref="DocumentStoredFieldVisitor"/>.
        /// </summary>
        public abstract void Document(int docID, StoredFieldVisitor visitor);

        /// <summary>
        /// Returns the stored fields of the <code>n</code><sup>th</sup>
        /// <code>Document</code> in this index.  this is just
        /// sugar for using <seealso cref="DocumentStoredFieldVisitor"/>.
        /// <p>
        /// <b>NOTE:</b> for performance reasons, this method does not check if the
        /// requested document is deleted, and therefore asking for a deleted document
        /// may yield unspecified results. Usually this is not required, however you
        /// can test if the doc is deleted by checking the {@link
        /// Bits} returned from <seealso cref="MultiFields#getLiveDocs"/>.
        ///
        /// <b>NOTE:</b> only the content of a field is returned,
        /// if that field was stored during indexing.  Metadata
        /// like boost, omitNorm, IndexOptions, tokenized, etc.,
        /// are not preserved.
        /// </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        // TODO: we need a separate StoredField, so that the
        // Document returned here contains that class not
        // IndexableField
        public Document Document(int docID)
        {
            var visitor = new DocumentStoredFieldVisitor();
            Document(docID, visitor);
            return visitor.Document;
        }

        /// <summary>
        /// Like <seealso cref="#document(int)"/> but only loads the specified
        /// fields.  Note that this is simply sugar for {@link
        /// DocumentStoredFieldVisitor#DocumentStoredFieldVisitor(Set)}.
        /// </summary>
        public Document Document(int docID, ISet<string> fieldsToLoad)
        {
            var visitor = new DocumentStoredFieldVisitor(fieldsToLoad);
            Document(docID, visitor);
            return visitor.Document;
        }

        /// <summary>
        /// Returns true if any documents have been deleted. Implementers should
        ///  consider overriding this method if <seealso cref="#maxDoc()"/> or <seealso cref="#numDocs()"/>
        ///  are not constant-time operations.
        /// </summary>
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

        /// <summary>
        /// Implements close. </summary>
        protected internal abstract void DoClose();

        /// <summary>
        /// Expert: Returns the root <seealso cref="IndexReaderContext"/> for this
        /// <seealso cref="IndexReader"/>'s sub-reader tree.
        /// <p>
        /// Iff this reader is composed of sub
        /// readers, i.e. this reader being a composite reader, this method returns a
        /// <seealso cref="CompositeReaderContext"/> holding the reader's direct children as well as a
        /// view of the reader tree's atomic leaf contexts. All sub-
        /// <seealso cref="IndexReaderContext"/> instances referenced from this readers top-level
        /// context are private to this reader and are not shared with another context
        /// tree. For example, IndexSearcher uses this API to drive searching by one
        /// atomic leaf reader at a time. If this reader is not composed of child
        /// readers, this method returns an <seealso cref="AtomicReaderContext"/>.
        /// <p>
        /// Note: Any of the sub-<seealso cref="CompositeReaderContext"/> instances referenced
        /// from this top-level context do not support <seealso cref="CompositeReaderContext#leaves()"/>.
        /// Only the top-level context maintains the convenience leaf-view
        /// for performance reasons.
        /// </summary>
        public abstract IndexReaderContext Context { get; }

        /// <summary>
        /// Returns the reader's leaves, or itself if this reader is atomic.
        /// this is a convenience method calling {@code this.getContext().leaves()}. </summary>
        /// <seealso cref= IndexReaderContext#leaves() </seealso>
        public IList<AtomicReaderContext> Leaves
        {
            get { return Context.Leaves; }
        }

        /// <summary>
        /// Expert: Returns a key for this IndexReader, so FieldCache/CachingWrapperFilter can find
        /// it again.
        /// this key must not have equals()/hashCode() methods, so &quot;equals&quot; means &quot;identical&quot;.
        /// </summary>
        public virtual object CoreCacheKey
        {
            get
            {
                // Don't call ensureOpen since FC calls this (to evict)
                // on close
                return this;
            }
        }

        /// <summary>
        /// Expert: Returns a key for this IndexReader that also includes deletions,
        /// so FieldCache/CachingWrapperFilter can find it again.
        /// this key must not have equals()/hashCode() methods, so &quot;equals&quot; means &quot;identical&quot;.
        /// </summary>
        public virtual object CombinedCoreAndDeletesKey
        {
            get
            {
                // Don't call ensureOpen since FC calls this (to evict)
                // on close
                return this;
            }
        }

        /// <summary>
        /// Returns the number of documents containing the
        /// <code>term</code>.  this method returns 0 if the term or
        /// field does not exists.  this method does not take into
        /// account deleted documents that have not yet been merged
        /// away. </summary>
        /// <seealso cref= TermsEnum#docFreq() </seealso>
        public abstract int DocFreq(Term term);

        /// <summary>
        /// Returns the total number of occurrences of {@code term} across all
        /// documents (the sum of the freq() for each doc that has this term). this
        /// will be -1 if the codec doesn't support this measure. Note that, like other
        /// term measures, this measure does not take deleted documents into account.
        /// </summary>
        public abstract long TotalTermFreq(Term term);

        /// <summary>
        /// Returns the sum of <seealso cref="TermsEnum#docFreq()"/> for all terms in this field,
        /// or -1 if this measure isn't stored by the codec. Note that, just like other
        /// term measures, this measure does not take deleted documents into account.
        /// </summary>
        /// <seealso cref= Terms#getSumDocFreq() </seealso>
        public abstract long GetSumDocFreq(string field);

        /// <summary>
        /// Returns the number of documents that have at least one term for this field,
        /// or -1 if this measure isn't stored by the codec. Note that, just like other
        /// term measures, this measure does not take deleted documents into account.
        /// </summary>
        /// <seealso cref= Terms#getDocCount() </seealso>
        public abstract int GetDocCount(string field);

        /// <summary>
        /// Returns the sum of <seealso cref="TermsEnum#totalTermFreq"/> for all terms in this
        /// field, or -1 if this measure isn't stored by the codec (or if this fields
        /// omits term freq and positions). Note that, just like other term measures,
        /// this measure does not take deleted documents into account.
        /// </summary>
        /// <seealso cref= Terms#getSumTotalTermFreq() </seealso>
        public abstract long GetSumTotalTermFreq(string field);
    }
}