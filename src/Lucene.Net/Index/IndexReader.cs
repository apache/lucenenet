using J2N.Threading.Atomic;
using Lucene.Net.Documents;
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using DocumentStoredFieldVisitor = DocumentStoredFieldVisitor;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// <see cref="IndexReader"/> is an abstract class, providing an interface for accessing an
    /// index.  Search of an index is done entirely through this abstract interface,
    /// so that any subclass which implements it is searchable.
    ///
    /// <para/>There are two different types of <see cref="IndexReader"/>s:
    /// <list type="bullet">
    ///     <item><description><see cref="AtomicReader"/>: These indexes do not consist of several sub-readers,
    ///         they are atomic. They support retrieval of stored fields, doc values, terms,
    ///         and postings.</description></item>
    ///     <item><description><see cref="CompositeReader"/>: Instances (like <see cref="DirectoryReader"/>)
    ///         of this reader can only
    ///         be used to get stored fields from the underlying <see cref="AtomicReader"/>s,
    ///         but it is not possible to directly retrieve postings. To do that, get
    ///         the sub-readers via <see cref="CompositeReader.GetSequentialSubReaders()"/>.
    ///         Alternatively, you can mimic an <see cref="AtomicReader"/> (with a serious slowdown),
    ///         by wrapping composite readers with <see cref="SlowCompositeReaderWrapper"/>.</description></item>
    /// </list>
    ///
    /// <para/><see cref="IndexReader"/> instances for indexes on disk are usually constructed
    /// with a call to one of the static <c>DirectoryReader.Open()</c> methods,
    /// e.g. <seealso cref="DirectoryReader.Open(Lucene.Net.Store.Directory)"/>. <see cref="DirectoryReader"/> inherits
    /// the <see cref="CompositeReader"/> abstract class, it is not possible to directly get postings.
    ///
    /// <para/> For efficiency, in this API documents are often referred to via
    /// <i>document numbers</i>, non-negative integers which each name a unique
    /// document in the index.  These document numbers are ephemeral -- they may change
    /// as documents are added to and deleted from an index.  Clients should thus not
    /// rely on a given document having the same number between sessions.
    ///
    /// <para/>
    /// <b>NOTE</b>: <see cref="IndexReader"/> instances are completely thread
    /// safe, meaning multiple threads can call any of its methods,
    /// concurrently.  If your application requires external
    /// synchronization, you should <b>not</b> synchronize on the
    /// <see cref="IndexReader"/> instance; use your own
    /// (non-Lucene) objects instead.
    /// </summary>
    public abstract class IndexReader : IDisposable
    {
        private bool closed = false;
        private bool closedByChild = false;
        private readonly AtomicInt32 refCount = new AtomicInt32(1);

        internal IndexReader()
        {
            if (!(this is CompositeReader || this is AtomicReader))
            {
                throw new Exception("IndexReader should never be directly extended, subclass AtomicReader or CompositeReader instead.");
            }
        }

        /// <summary>
        /// A custom listener that's invoked when the <see cref="IndexReader"/>
        /// is closed.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public interface IReaderClosedListener
        {
            /// <summary>
            /// Invoked when the <see cref="IndexReader"/> is closed. </summary>
            void OnClose(IndexReader reader);
        }

        private readonly ISet<IReaderClosedListener> readerClosedListeners = new JCG.LinkedHashSet<IReaderClosedListener>().AsConcurrent();

        private readonly ISet<IdentityWeakReference<IndexReader>> parentReaders = new ConcurrentHashSet<IdentityWeakReference<IndexReader>>();

        /// <summary>
        /// Expert: adds a <see cref="IReaderClosedListener"/>.  The
        /// provided listener will be invoked when this reader is closed.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public void AddReaderClosedListener(IReaderClosedListener listener)
        {
            EnsureOpen();
            readerClosedListeners.Add(listener);
        }

        /// <summary>
        /// Expert: remove a previously added <see cref="IReaderClosedListener"/>.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public void RemoveReaderClosedListener(IReaderClosedListener listener)
        {
            EnsureOpen();
            readerClosedListeners.Remove(listener);
        }

        /// <summary>
        /// Expert: this method is called by <see cref="IndexReader"/>s which wrap other readers
        /// (e.g. <see cref="CompositeReader"/> or <see cref="FilterAtomicReader"/>) to register the parent
        /// at the child (this reader) on construction of the parent. When this reader is disposed,
        /// it will mark all registered parents as disposed, too. The references to parent readers
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
            lock (((ICollection)readerClosedListeners).SyncRoot) // LUCENENET: Ensure we sync on the SyncRoot of ConcurrentSet<T>
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
                            th.AddSuppressed(t);
                        }
                    }
                }
                IOUtils.ReThrowUnchecked(th);
            }
        }

        private void ReportCloseToParentReaders()
        {
            lock (parentReaders) // LUCENENET: This does not actually synchronize the set, it only ensures this method can only be entered by 1 thread
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
                return refCount;
            }
        }

        /// <summary>
        /// Expert: increments the <see cref="RefCount"/> of this <see cref="IndexReader"/>
        /// instance.  <see cref="RefCount"/>s are used to determine when a
        /// reader can be disposed safely, i.e. as soon as there are
        /// no more references.  Be sure to always call a
        /// corresponding <see cref="DecRef"/>, in a finally clause;
        /// otherwise the reader may never be disposed.  Note that
        /// <see cref="Dispose(bool)"/> simply calls <see cref="DecRef()"/>, which means that
        /// the <see cref="IndexReader"/> will not really be disposed until
        /// <see cref="DecRef()"/> has been called for all outstanding
        /// references.
        /// </summary>
        /// <seealso cref="DecRef"/>
        /// <seealso cref="TryIncRef"/>
        public void IncRef()
        {
            if (!TryIncRef())
            {
                EnsureOpen();
            }
        }

        /// <summary>
        /// Expert: increments the <see cref="RefCount"/> of this <see cref="IndexReader"/>
        /// instance only if the <see cref="IndexReader"/> has not been disposed yet
        /// and returns <c>true</c> iff the <see cref="RefCount"/> was
        /// successfully incremented, otherwise <c>false</c>.
        /// If this method returns <c>false</c> the reader is either
        /// already disposed or is currently being disposed. Either way this
        /// reader instance shouldn't be used by an application unless
        /// <c>true</c> is returned.
        /// <para/>
        /// <see cref="RefCount"/>s are used to determine when a
        /// reader can be disposed safely, i.e. as soon as there are
        /// no more references.  Be sure to always call a
        /// corresponding <see cref="DecRef"/>, in a finally clause;
        /// otherwise the reader may never be disposed.  Note that
        /// <see cref="Dispose(bool)"/> simply calls <see cref="DecRef()"/>, which means that
        /// the <see cref="IndexReader"/> will not really be disposed until
        /// <see cref="DecRef()"/> has been called for all outstanding
        /// references.
        /// </summary>
        /// <seealso cref="DecRef"/>
        /// <seealso cref="IncRef"/>
        public bool TryIncRef()
        {
            int count;
            while ((count = refCount) > 0)
            {
                if (refCount.CompareAndSet(count, count + 1))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Expert: decreases the <see cref="RefCount"/> of this <see cref="IndexReader"/>
        /// instance.  If the <see cref="RefCount"/> drops to 0, then this
        /// reader is disposed.  If an exception is hit, the <see cref="RefCount"/>
        /// is unchanged.
        /// </summary>
        /// <exception cref="System.IO.IOException"> in case an <see cref="System.IO.IOException"/> occurs in <see cref="DoClose()"/>
        /// </exception>
        /// <seealso cref="IncRef"/>
        public void DecRef()
        {
            // only check refcount here (don't call ensureOpen()), so we can
            // still close the reader if it was made invalid by a child:
            if (refCount <= 0)
            {
                throw new ObjectDisposedException(this.GetType().GetTypeInfo().FullName, "this IndexReader is closed");
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
        /// Throws <see cref="ObjectDisposedException"/> if this <see cref="IndexReader"/> or any
        /// of its child readers is disposed, otherwise returns.
        /// </summary>
        protected internal void EnsureOpen()
        {
            if (refCount <= 0)
            {
                throw new ObjectDisposedException(this.GetType().GetTypeInfo().FullName, "this IndexReader is closed");
            }
            // the happens before rule on reading the refCount, which must be after the fake write,
            // ensures that we see the value:
            if (closedByChild)
            {
                throw new ObjectDisposedException(this.GetType().GetTypeInfo().FullName, "this IndexReader cannot be used anymore as one of its child readers was closed");
            }
        }

        /// <summary>
        /// Determines whether two object instances are equal.
        /// <para/>For caching purposes, <see cref="IndexReader"/> subclasses are not allowed
        /// to implement Equals/GetHashCode, so methods are declared sealed.
        /// To lookup instances from caches use <see cref="CoreCacheKey"/> and
        /// <see cref="CombinedCoreAndDeletesKey"/>.
        /// </summary>
        public override sealed bool Equals(object obj)
        {
            return (this == obj);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// <para/>For caching purposes, <see cref="IndexReader"/> subclasses are not allowed
        /// to implement Equals/GetHashCode, so methods are declared sealed.
        /// To lookup instances from caches use <see cref="CoreCacheKey"/> and
        /// <see cref="CombinedCoreAndDeletesKey"/>.
        /// </summary>
        public override sealed int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        /// <summary>
        /// Returns a <see cref="IndexReader"/> reading the index in the given
        /// <see cref="Directory"/> </summary>
        /// <param name="directory"> the index directory </param>
        /// <exception cref="System.IO.IOException"> if there is a low-level IO error </exception>
        [Obsolete("Use DirectoryReader.Open(Directory)")]
        public static DirectoryReader Open(Directory directory)
        {
            return DirectoryReader.Open(directory);
        }

        /// <summary>
        /// Expert: Returns a <see cref="IndexReader"/> reading the index in the given
        /// <see cref="Directory"/> with the given <paramref name="termInfosIndexDivisor"/>. </summary>
        /// <param name="directory"> the index directory </param>
        /// <param name="termInfosIndexDivisor"> Subsamples which indexed
        ///  terms are loaded into RAM. this has the same effect as
        ///  <see cref="LiveIndexWriterConfig.TermIndexInterval"/> 
        ///  (which can be set on <see cref="IndexWriterConfig"/>) except that setting
        ///  must be done at indexing time while this setting can be
        ///  set per reader.  When set to <c>N</c>, then one in every
        ///  <c>N*termIndexInterval</c> terms in the index is loaded into
        ///  memory.  By setting this to a value <c>&gt; 1</c> you can reduce
        ///  memory usage, at the expense of higher latency when
        ///  loading a TermInfo.  The default value is 1.  Set this
        ///  to -1 to skip loading the terms index entirely. </param>
        /// <exception cref="System.IO.IOException"> if there is a low-level IO error </exception>
        [Obsolete("Use DirectoryReader.Open(Directory, int)")]
        public static DirectoryReader Open(Directory directory, int termInfosIndexDivisor)
        {
            return DirectoryReader.Open(directory, termInfosIndexDivisor);
        }

        /// <summary>
        /// Open a near real time <see cref="IndexReader"/> from the <see cref="IndexWriter"/>.
        /// </summary>
        /// <param name="writer"> The <see cref="IndexWriter"/> to open from </param>
        /// <param name="applyAllDeletes"> If true, all buffered deletes will
        /// be applied (made visible) in the returned reader.  If
        /// false, the deletes are not applied but remain buffered
        /// (in <see cref="IndexWriter"/>) so that they will be applied in the
        /// future.  Applying deletes can be costly, so if your app
        /// can tolerate deleted documents being returned you might
        /// gain some performance by passing false. </param>
        /// <returns> The new <see cref="IndexReader"/> </returns>
        /// <exception cref="System.IO.IOException"> if there is a low-level IO error
        /// </exception>
        /// <seealso cref="DirectoryReader.OpenIfChanged(DirectoryReader, IndexWriter, bool)"/>
        ///
        /// @lucene.experimental
        [Obsolete("Use DirectoryReader.Open(IndexWriter, bool)")]
        public static DirectoryReader Open(IndexWriter writer, bool applyAllDeletes)
        {
            return DirectoryReader.Open(writer, applyAllDeletes);
        }

        /// <summary>
        /// Expert: returns an <see cref="IndexReader"/> reading the index in the given
        /// <see cref="IndexCommit"/>. 
        /// </summary>
        /// <param name="commit"> the commit point to open </param>
        /// <exception cref="System.IO.IOException"> if there is a low-level IO error </exception>
        [Obsolete("Use DirectoryReader.Open(IndexCommit)")]
        public static DirectoryReader Open(IndexCommit commit)
        {
            return DirectoryReader.Open(commit);
        }

        /// <summary>
        /// Expert: returns an <see cref="IndexReader"/> reading the index in the given
        /// <see cref="IndexCommit"/> and <paramref name="termInfosIndexDivisor"/>. </summary>
        /// <param name="commit"> the commit point to open </param>
        /// <param name="termInfosIndexDivisor"> Subsamples which indexed
        ///  terms are loaded into RAM. this has the same effect as
        ///  <see cref="LiveIndexWriterConfig.TermIndexInterval"/> 
        ///  (which can be set in <see cref="IndexWriterConfig"/>) except that setting
        ///  must be done at indexing time while this setting can be
        ///  set per reader.  When set to <c>N</c>, then one in every
        ///  <c>N*termIndexInterval</c> terms in the index is loaded into
        ///  memory.  By setting this to a value <c>&gt; 1</c> you can reduce
        ///  memory usage, at the expense of higher latency when
        ///  loading a TermInfo.  The default value is 1.  Set this
        ///  to -1 to skip loading the terms index entirely. </param>
        /// <exception cref="System.IO.IOException"> if there is a low-level IO error </exception>
        [Obsolete("Use DirectoryReader.Open(IndexCommit, int)/>")]
        public static DirectoryReader Open(IndexCommit commit, int termInfosIndexDivisor)
        {
            return DirectoryReader.Open(commit, termInfosIndexDivisor);
        }

        /// <summary>
        /// Retrieve term vectors for this document, or <c>null</c> if
        /// term vectors were not indexed. The returned <see cref="Fields"/>
        /// instance acts like a single-document inverted index
        /// (the docID will be 0).
        /// </summary>
        public abstract Fields GetTermVectors(int docID);

        /// <summary>
        /// Retrieve term vector for this document and field, or
        /// <c>null</c> if term vectors were not indexed. The returned
        /// <see cref="Fields"/> instance acts like a single-document inverted
        /// index (the docID will be 0).
        /// </summary>
        public Terms GetTermVector(int docID, string field)
        {
            Fields vectors = GetTermVectors(docID);
            if (vectors == null)
            {
                return null;
            }
            return vectors.GetTerms(field);
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
        /// custom processing/loading of each field. If you
        /// simply want to load all fields, use
        /// <see cref="Document(int)"/>. If you want to load a subset, use
        /// <see cref="DocumentStoredFieldVisitor"/>.
        /// </summary>
        public abstract void Document(int docID, StoredFieldVisitor visitor);

        /// <summary>
        /// Returns the stored fields of the <c>n</c><sup>th</sup>
        /// <see cref="Documents.Document"/> in this index.  This is just
        /// sugar for using <see cref="DocumentStoredFieldVisitor"/>.
        /// <para/>
        /// <b>NOTE:</b> for performance reasons, this method does not check if the
        /// requested document is deleted, and therefore asking for a deleted document
        /// may yield unspecified results. Usually this is not required, however you
        /// can test if the doc is deleted by checking the 
        /// <see cref="Util.IBits"/> returned from <see cref="MultiFields.GetLiveDocs"/>.
        /// <para/>
        /// <b>NOTE:</b> only the content of a field is returned,
        /// if that field was stored during indexing.  Metadata
        /// like boost, omitNorm, IndexOptions, tokenized, etc.,
        /// are not preserved.
        /// </summary>
        /// <exception cref="System.IO.IOException"> if there is a low-level IO error </exception>
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
        /// Like <see cref="Document(int)"/> but only loads the specified
        /// fields.  Note that this is simply sugar for
        /// <see cref="DocumentStoredFieldVisitor.DocumentStoredFieldVisitor(ISet{string})"/>.
        /// </summary>
        public Document Document(int docID, ISet<string> fieldsToLoad)
        {
            var visitor = new DocumentStoredFieldVisitor(fieldsToLoad);
            Document(docID, visitor);
            return visitor.Document;
        }

        /// <summary>
        /// Returns <c>true</c> if any documents have been deleted. Implementers should
        /// consider overriding this property if <see cref="MaxDoc"/> or <see cref="NumDocs"/>
        /// are not constant-time operations.
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

        /// <summary>
        /// Closes files associated with this index.
        /// This method implements the disposable pattern. 
        /// It may be overridden to dispose any managed or unmanaged resources,
        /// but be sure to call <c>base.Dispose(disposing)</c> to close files associated with the
        /// underlying <see cref="IndexReader"/>.
        /// </summary>
        /// <param name="disposing"><c>true</c> indicates to dispose all managed 
        /// and unmanaged resources, <c>false</c> indicates dispose unmanaged 
        /// resources only</param>
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
        /// Expert: Returns the root <see cref="IndexReaderContext"/> for this
        /// <see cref="IndexReader"/>'s sub-reader tree.
        /// <para/>
        /// Iff this reader is composed of sub
        /// readers, i.e. this reader being a composite reader, this method returns a
        /// <see cref="CompositeReaderContext"/> holding the reader's direct children as well as a
        /// view of the reader tree's atomic leaf contexts. All sub-
        /// <see cref="IndexReaderContext"/> instances referenced from this readers top-level
        /// context are private to this reader and are not shared with another context
        /// tree. For example, <see cref="Search.IndexSearcher"/> uses this API to drive searching by one
        /// atomic leaf reader at a time. If this reader is not composed of child
        /// readers, this method returns an <see cref="AtomicReaderContext"/>.
        /// <para/>
        /// Note: Any of the sub-<see cref="CompositeReaderContext"/> instances referenced
        /// from this top-level context do not support <see cref="CompositeReaderContext.Leaves"/>.
        /// Only the top-level context maintains the convenience leaf-view
        /// for performance reasons.
        /// </summary>
        public abstract IndexReaderContext Context { get; }

        /// <summary>
        /// Returns the reader's leaves, or itself if this reader is atomic.
        /// This is a convenience method calling <c>this.Context.Leaves</c>.
        /// </summary>
        /// <seealso cref="IndexReaderContext.Leaves"/>
        public IList<AtomicReaderContext> Leaves
        {
            get { return Context.Leaves; }
        }

        /// <summary>
        /// Expert: Returns a key for this <see cref="IndexReader"/>, so 
        /// <see cref="Search.IFieldCache"/>/<see cref="Search.CachingWrapperFilter"/> can find
        /// it again.
        /// This key must not have Equals()/GetHashCode() methods, 
        /// so &quot;equals&quot; means &quot;identical&quot;.
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
        /// Expert: Returns a key for this <see cref="IndexReader"/> that also includes deletions,
        /// so <see cref="Search.IFieldCache"/>/<see cref="Search.CachingWrapperFilter"/> can find it again.
        /// This key must not have Equals()/GetHashCode() methods, 
        /// so &quot;equals&quot; means &quot;identical&quot;.
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
        /// <paramref name="term"/>.  This method returns 0 if the term or
        /// field does not exist.  This method does not take into
        /// account deleted documents that have not yet been merged
        /// away. </summary>
        /// <seealso cref="TermsEnum.DocFreq"/>
        public abstract int DocFreq(Term term);

        /// <summary>
        /// Returns the total number of occurrences of <paramref name="term"/> across all
        /// documents (the sum of the Freq for each doc that has this term). This
        /// will be -1 if the codec doesn't support this measure. Note that, like other
        /// term measures, this measure does not take deleted documents into account.
        /// </summary>
        public abstract long TotalTermFreq(Term term);

        /// <summary>
        /// Returns the sum of <see cref="TermsEnum.DocFreq"/> for all terms in this field,
        /// or -1 if this measure isn't stored by the codec. Note that, just like other
        /// term measures, this measure does not take deleted documents into account.
        /// </summary>
        /// <seealso cref="Terms.SumDocFreq"/>
        public abstract long GetSumDocFreq(string field);

        /// <summary>
        /// Returns the number of documents that have at least one term for this field,
        /// or -1 if this measure isn't stored by the codec. Note that, just like other
        /// term measures, this measure does not take deleted documents into account.
        /// </summary>
        /// <seealso cref="Terms.DocCount"/>
        public abstract int GetDocCount(string field);

        /// <summary>
        /// Returns the sum of <see cref="TermsEnum.TotalTermFreq"/> for all terms in this
        /// field, or -1 if this measure isn't stored by the codec (or if this fields
        /// omits term freq and positions). Note that, just like other term measures,
        /// this measure does not take deleted documents into account.
        /// </summary>
        /// <seealso cref="Terms.SumTotalTermFreq"/>
        public abstract long GetSumTotalTermFreq(string field);
    }
}