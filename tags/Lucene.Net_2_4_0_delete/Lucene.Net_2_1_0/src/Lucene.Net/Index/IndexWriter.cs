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
using Analyzer = Lucene.Net.Analysis.Analyzer;
using Document = Lucene.Net.Documents.Document;
using Similarity = Lucene.Net.Search.Similarity;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using Lock = Lucene.Net.Store.Lock;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Index
{
	
	/// <summary> An IndexWriter creates and maintains an index.
	/// 
	/// <p>
	/// The third argument (<code>create</code>) to the <a
	/// href="#IndexWriter(Lucene.Net.Store.Directory,
	/// Lucene.Net.Analysis.Analyzer, boolean)"><b>constructor</b></a>
	/// determines whether a new index is created, or whether an existing index is
	/// opened for the addition of new documents. Note that you can open an index
	/// with create=true even while readers are using the index. The old readers will
	/// continue to search the "point in time" snapshot they had opened, and won't
	/// see the newly created index until they re-open.
	/// </p>
	/// 
	/// <p>
	/// In either case, documents are added with the <a
	/// href="#addDocument(Lucene.Net.Documents.Document)"><b>addDocument</b></a>
	/// method. When finished adding documents, <a href="#close()"><b>close</b></a>
	/// should be called.
	/// </p>
	/// 
	/// <p>
	/// If an index will not have more documents added for a while and optimal search
	/// performance is desired, then the <a href="#optimize()"><b>optimize</b></a>
	/// method should be called before the index is closed.
	/// </p>
	/// 
	/// <p>
	/// Opening an IndexWriter creates a lock file for the directory in use. Trying
	/// to open another IndexWriter on the same directory will lead to an
	/// IOException. The IOException is also thrown if an IndexReader on the same
	/// directory is used to delete documents from the index.
	/// </p>
	/// 
	/// <p>
	/// As of <b>2.1</b>, IndexWriter can now delete documents by {@link Term} (see
	/// {@link #deleteDocuments} ) and update (delete then add) documents (see
	/// {@link #updateDocument}). Deletes are buffered until {@link
	/// #setMaxBufferedDeleteTerms} <code>Terms</code> at which point they are
	/// flushed to the index. Note that a flush occurs when there are enough buffered
	/// deletes or enough added documents, whichever is sooner. When a flush occurs,
	/// both pending deletes and added documents are flushed to the index.
	/// </p>
	/// </summary>
	
	public class IndexWriter
	{
		private void  InitBlock()
		{
			similarity = Similarity.GetDefault();
		}
		
		/// <summary> Default value for the write lock timeout (1,000).
		/// 
		/// </summary>
		/// <seealso cref="#setDefaultWriteLockTimeout">
		/// </seealso>
		public static long WRITE_LOCK_TIMEOUT = 1000;
		
		private long writeLockTimeout = WRITE_LOCK_TIMEOUT;
		
		public const System.String WRITE_LOCK_NAME = "write.lock";
		
		/// <summary> Default value is 10. Change using {@link #SetMergeFactor(int)}.</summary>
		public const int DEFAULT_MERGE_FACTOR = 10;
		
		/// <summary> Default value is 10. Change using {@link #SetMaxBufferedDocs(int)}.</summary>
		public const int DEFAULT_MAX_BUFFERED_DOCS = 10;
		
		/// <summary> Default value is 1000. Change using
		/// {@link #SetMaxBufferedDeleteTerms(int)}.
		/// </summary>
		public const int DEFAULT_MAX_BUFFERED_DELETE_TERMS = 1000;
		
		/// <summary> Default value is {@link Integer#MAX_VALUE}. Change using
		/// {@link #SetMaxMergeDocs(int)}.
		/// </summary>
		public static readonly int DEFAULT_MAX_MERGE_DOCS = System.Int32.MaxValue;
		
		/// <summary> Default value is 10,000. Change using {@link #SetMaxFieldLength(int)}.</summary>
		public const int DEFAULT_MAX_FIELD_LENGTH = 10000;
		
		/// <summary> Default value is 128. Change using {@link #SetTermIndexInterval(int)}.</summary>
		public const int DEFAULT_TERM_INDEX_INTERVAL = 128;
		
		private Directory directory; // where this index resides
		
		private Analyzer analyzer; // how to analyze text
		
		private Similarity similarity; // how to
		// normalize
		
		private bool inTransaction = false; // true iff we are in a transaction
		
		private bool commitPending; // true if segmentInfos has changes not yet
		// committed
		
		private System.Collections.Hashtable protectedSegments; // segment names that should not be
		// deleted until commit
		
		private SegmentInfos rollbackSegmentInfos; // segmentInfos we will fallback
		// to if the commit fails
		
		internal SegmentInfos segmentInfos = new SegmentInfos(); // the segments
		
		internal SegmentInfos ramSegmentInfos = new SegmentInfos(); // the segments in
		// ramDirectory
		
		private RAMDirectory ramDirectory = new RAMDirectory(); // for temp
		// segs
		
		private IndexFileDeleter deleter;
		
		private Lock writeLock;
		
		private int termIndexInterval = DEFAULT_TERM_INDEX_INTERVAL;
		
		// The max number of delete terms that can be buffered before
		// they must be flushed to disk.
		private int maxBufferedDeleteTerms = DEFAULT_MAX_BUFFERED_DELETE_TERMS;
		
		// This Hashmap buffers delete terms in ram before they are applied.
		// The key is delete term; the value is number of ram
		// segments the term applies to.
		private System.Collections.Hashtable bufferedDeleteTerms = new System.Collections.Hashtable();
		
		private int numBufferedDeleteTerms = 0;
		
		/// <summary> Use compound file setting. Defaults to true, minimizing the number of
		/// files used. Setting this to false may improve indexing performance, but
		/// may also cause file handle problems.
		/// </summary>
		private bool useCompoundFile = true;
		
		private bool closeDir;
		
		/// <summary> Get the current setting of whether to use the compound file format. Note
		/// that this just returns the value you set with setUseCompoundFile(boolean)
		/// or the default. You cannot use this to query the status of an existing
		/// index.
		/// 
		/// </summary>
		/// <seealso cref="#SetUseCompoundFile(boolean)">
		/// </seealso>
		public virtual bool GetUseCompoundFile()
		{
			return useCompoundFile;
		}
		
		/// <summary> Setting to turn on usage of a compound file. When on, multiple files for
		/// each segment are merged into a single file once the segment creation is
		/// finished. This is done regardless of what directory is in use.
		/// </summary>
		public virtual void  SetUseCompoundFile(bool value_Renamed)
		{
			useCompoundFile = value_Renamed;
		}
		
		/// <summary> Expert: Set the Similarity implementation used by this IndexWriter.
		/// 
		/// </summary>
		/// <seealso cref="Similarity#SetDefault(Similarity)">
		/// </seealso>
		public virtual void  SetSimilarity(Similarity similarity)
		{
			this.similarity = similarity;
		}
		
		/// <summary> Expert: Return the Similarity implementation used by this IndexWriter.
		/// 
		/// <p>
		/// This defaults to the current value of {@link Similarity#GetDefault()}.
		/// </summary>
		public virtual Similarity GetSimilarity()
		{
			return this.similarity;
		}
		
		/// <summary> Expert: Set the interval between indexed terms. Large values cause less
		/// memory to be used by IndexReader, but slow random-access to terms. Small
		/// values cause more memory to be used by an IndexReader, and speed
		/// random-access to terms.
		/// 
		/// This parameter determines the amount of computation required per query
		/// term, regardless of the number of documents that contain that term. In
		/// particular, it is the maximum number of other terms that must be scanned
		/// before a term is located and its frequency and position information may
		/// be processed. In a large index with user-entered query terms, query
		/// processing time is likely to be dominated not by term lookup but rather
		/// by the processing of frequency and positional data. In a small index or
		/// when many uncommon query terms are generated (e.g., by wildcard queries)
		/// term lookup may become a dominant cost.
		/// 
		/// In particular, <code>numUniqueTerms/interval</code> terms are read into
		/// memory by an IndexReader, and, on average, <code>interval/2</code>
		/// terms must be scanned for each random term access.
		/// 
		/// </summary>
		/// <seealso cref="#DEFAULT_TERM_INDEX_INTERVAL">
		/// </seealso>
		public virtual void  SetTermIndexInterval(int interval)
		{
			this.termIndexInterval = interval;
		}
		
		/// <summary> Expert: Return the interval between indexed terms.
		/// 
		/// </summary>
		/// <seealso cref="#SetTermIndexInterval(int)">
		/// </seealso>
		public virtual int GetTermIndexInterval()
		{
			return termIndexInterval;
		}
		
		/// <summary> Constructs an IndexWriter for the index in <code>path</code>. Text
		/// will be analyzed with <code>a</code>. If <code>create</code> is
		/// true, then a new, empty index will be created in <code>path</code>,
		/// replacing the index already there, if any.
		/// 
		/// </summary>
		/// <param name="">path
		/// the path to the index directory
		/// </param>
		/// <param name="">a
		/// the analyzer to use
		/// </param>
		/// <param name="">create
		/// <code>true</code> to create the index or overwrite the
		/// existing one; <code>false</code> to append to the existing
		/// index
		/// </param>
		/// <throws>  IOException </throws>
		/// <summary>             if the directory cannot be read/written to, or if it does not
		/// exist, and <code>create</code> is <code>false</code>
		/// </summary>
		public IndexWriter(System.String path, Analyzer a, bool create)
		{
			InitBlock();
			Init(path, a, create);
		}
		
		/// <summary> Constructs an IndexWriter for the index in <code>path</code>. Text
		/// will be analyzed with <code>a</code>. If <code>create</code> is
		/// true, then a new, empty index will be created in <code>path</code>,
		/// replacing the index already there, if any.
		/// 
		/// </summary>
		/// <param name="">path
		/// the path to the index directory
		/// </param>
		/// <param name="">a
		/// the analyzer to use
		/// </param>
		/// <param name="">create
		/// <code>true</code> to create the index or overwrite the
		/// existing one; <code>false</code> to append to the existing
		/// index
		/// </param>
		/// <throws>  IOException </throws>
		/// <summary>             if the directory cannot be read/written to, or if it does not
		/// exist, and <code>create</code> is <code>false</code>
		/// </summary>
		public IndexWriter(System.IO.FileInfo path, Analyzer a, bool create)
		{
			InitBlock();
			Init(path, a, create);
		}
		
		/// <summary> Constructs an IndexWriter for the index in <code>d</code>. Text will
		/// be analyzed with <code>a</code>. If <code>create</code> is true,
		/// then a new, empty index will be created in <code>d</code>, replacing
		/// the index already there, if any.
		/// 
		/// </summary>
		/// <param name="">d
		/// the index directory
		/// </param>
		/// <param name="">a
		/// the analyzer to use
		/// </param>
		/// <param name="">create
		/// <code>true</code> to create the index or overwrite the
		/// existing one; <code>false</code> to append to the existing
		/// index
		/// </param>
		/// <throws>  IOException </throws>
		/// <summary>             if the directory cannot be read/written to, or if it does not
		/// exist, and <code>create</code> is <code>false</code>
		/// </summary>
		public IndexWriter(Directory d, Analyzer a, bool create)
		{
			InitBlock();
			Init(d, a, create, false);
		}
		
		/// <summary> Constructs an IndexWriter for the index in <code>path</code>, creating
		/// it first if it does not already exist, otherwise appending to the
		/// existing index. Text will be analyzed with <code>a</code>.
		/// 
		/// </summary>
		/// <param name="">path
		/// the path to the index directory
		/// </param>
		/// <param name="">a
		/// the analyzer to use
		/// </param>
		/// <throws>  IOException </throws>
		/// <summary>             if the directory cannot be created or read/written to
		/// </summary>
		public IndexWriter(System.String path, Analyzer a)
		{
			InitBlock();
			if (IndexReader.IndexExists(path))
			{
				Init(path, a, false);
			}
			else
			{
				Init(path, a, true);
			}
		}
		
		/// <summary> Constructs an IndexWriter for the index in <code>path</code>, creating
		/// it first if it does not already exist, otherwise appending to the
		/// existing index. Text will be analyzed with <code>a</code>.
		/// 
		/// </summary>
		/// <param name="">path
		/// the path to the index directory
		/// </param>
		/// <param name="">a
		/// the analyzer to use
		/// </param>
		/// <throws>  IOException </throws>
		/// <summary>             if the directory cannot be created or read/written to
		/// </summary>
		public IndexWriter(System.IO.FileInfo path, Analyzer a)
		{
			InitBlock();
			if (IndexReader.IndexExists(path))
			{
				Init(path, a, false);
			}
			else
			{
				Init(path, a, true);
			}
		}
		
		/// <summary> Constructs an IndexWriter for the index in <code>d</code>, creating it
		/// first if it does not already exist, otherwise appending to the existing
		/// index. Text will be analyzed with <code>a</code>.
		/// 
		/// </summary>
		/// <param name="">d
		/// the index directory
		/// </param>
		/// <param name="">a
		/// the analyzer to use
		/// </param>
		/// <throws>  IOException </throws>
		/// <summary>             if the directory cannot be created or read/written to
		/// </summary>
		public IndexWriter(Directory d, Analyzer a)
		{
			InitBlock();
			if (IndexReader.IndexExists(d))
			{
				Init(d, a, false, false);
			}
			else
			{
				Init(d, a, true, false);
			}
		}
		
		private IndexWriter(Directory d, Analyzer a, bool create, bool closeDir)
		{
			InitBlock();
			Init(d, a, create, closeDir);
		}
		
		private void  Init(System.String path, Analyzer a, bool create)
		{
			Init(FSDirectory.GetDirectory(path), a, create, true);
		}
		
		private void  Init(System.IO.FileInfo path, Analyzer a, bool create)
		{
			Init(FSDirectory.GetDirectory(path), a, create, true);
		}
		
		private void  Init(Directory d, Analyzer a, bool create, bool closeDir)
		{
			this.closeDir = closeDir;
			directory = d;
			analyzer = a;
			
			if (create)
			{
				// Clear the write lock in case it's leftover:
				directory.ClearLock(IndexWriter.WRITE_LOCK_NAME);
			}
			
			Lock writeLock = directory.MakeLock(IndexWriter.WRITE_LOCK_NAME);
			if (!writeLock.Obtain(writeLockTimeout))
			// obtain write lock
			{
				throw new System.IO.IOException("Index locked for write: " + writeLock);
			}
			this.writeLock = writeLock; // save it
			
			try
			{
				if (create)
				{
					// Try to read first. This is to allow create
					// against an index that's currently open for
					// searching. In this case we write the next
					// segments_N file with no segments:
					try
					{
						segmentInfos.Read(directory);
						segmentInfos.Clear();
					}
					catch (System.IO.IOException e)
					{
						// Likely this means it's a fresh directory
					}
					segmentInfos.Write(directory);
				}
				else
				{
					segmentInfos.Read(directory);
				}
				
				// Create a deleter to keep track of which files can
				// be deleted:
				deleter = new IndexFileDeleter(segmentInfos, directory);
				deleter.SetInfoStream(infoStream);
				deleter.FindDeletableFiles();
				deleter.DeleteFiles();
			}
			catch (System.IO.IOException e)
			{
				this.writeLock.Release();
				this.writeLock = null;
				throw e;
			}
		}
		
		/// <summary> Determines the largest number of documents ever merged by addDocument().
		/// Small values (e.g., less than 10,000) are best for interactive indexing,
		/// as this limits the length of pauses while indexing to a few seconds.
		/// Larger values are best for batched indexing and speedier searches.
		/// 
		/// <p>
		/// The default value is {@link Integer#MAX_VALUE}.
		/// </summary>
		public virtual void  SetMaxMergeDocs(int maxMergeDocs)
		{
			this.maxMergeDocs = maxMergeDocs;
		}
		
		/// <seealso cref="#setMaxMergeDocs">
		/// </seealso>
		public virtual int GetMaxMergeDocs()
		{
			return maxMergeDocs;
		}
		
		/// <summary> The maximum number of terms that will be indexed for a single field in a
		/// document. This limits the amount of memory required for indexing, so that
		/// collections with very large files will not crash the indexing process by
		/// running out of memory.<p/> Note that this effectively truncates large
		/// documents, excluding from the index terms that occur further in the
		/// document. If you know your source documents are large, be sure to set
		/// this value high enough to accomodate the expected size. If you set it to
		/// Integer.MAX_VALUE, then the only limit is your memory, but you should
		/// anticipate an OutOfMemoryError.<p/> By default, no more than 10,000
		/// terms will be indexed for a field.
		/// </summary>
		public virtual void  SetMaxFieldLength(int maxFieldLength)
		{
			this.maxFieldLength = maxFieldLength;
		}
		
		/// <seealso cref="#setMaxFieldLength">
		/// </seealso>
		public virtual int GetMaxFieldLength()
		{
			return maxFieldLength;
		}
		
		/// <summary> Determines the minimal number of documents required before the buffered
		/// in-memory documents are merged and a new Segment is created. Since
		/// Documents are merged in a {@link Lucene.Net.Store.RAMDirectory},
		/// large value gives faster indexing. At the same time, mergeFactor limits
		/// the number of files open in a FSDirectory.
		/// 
		/// <p>
		/// The default value is 10.
		/// 
		/// </summary>
		/// <throws>  IllegalArgumentException </throws>
		/// <summary>             if maxBufferedDocs is smaller than 2
		/// </summary>
		public virtual void  SetMaxBufferedDocs(int maxBufferedDocs)
		{
			if (maxBufferedDocs < 2)
				throw new System.ArgumentException("maxBufferedDocs must at least be 2");
			this.minMergeDocs = maxBufferedDocs;
		}
		
		/// <seealso cref="#setMaxBufferedDocs">
		/// </seealso>
		public virtual int GetMaxBufferedDocs()
		{
			return minMergeDocs;
		}
		
		/// <summary> <p>
		/// Determines the minimal number of delete terms required before the
		/// buffered in-memory delete terms are applied and flushed. If there are
		/// documents buffered in memory at the time, they are merged and a new
		/// segment is created.
		/// </p>
		/// 
		/// <p>
		/// The default value is {@link #DEFAULT_MAX_BUFFERED_DELETE_TERMS}.
		/// 
		/// </summary>
		/// <throws>  IllegalArgumentException </throws>
		/// <summary>             if maxBufferedDeleteTerms is smaller than 1
		/// </p>
		/// </summary>
		public virtual void  SetMaxBufferedDeleteTerms(int maxBufferedDeleteTerms)
		{
			if (maxBufferedDeleteTerms < 1)
				throw new System.ArgumentException("maxBufferedDeleteTerms must at least be 1");
			this.maxBufferedDeleteTerms = maxBufferedDeleteTerms;
		}
		
		/// <seealso cref="#setMaxBufferedDeleteTerms">
		/// </seealso>
		public virtual int GetMaxBufferedDeleteTerms()
		{
			return maxBufferedDeleteTerms;
		}
		
		/// <summary> Determines how often segment indices are merged by addDocument(). With
		/// smaller values, less RAM is used while indexing, and searches on
		/// unoptimized indices are faster, but indexing speed is slower. With larger
		/// values, more RAM is used during indexing, and while searches on
		/// unoptimized indices are slower, indexing is faster. Thus larger values (>
		/// 10) are best for batch index creation, and smaller values (< 10) for
		/// indices that are interactively maintained.
		/// 
		/// <p>
		/// This must never be less than 2. The default value is 10.
		/// </summary>
		public virtual void  SetMergeFactor(int mergeFactor)
		{
			if (mergeFactor < 2)
				throw new System.ArgumentException("mergeFactor cannot be less than 2");
			this.mergeFactor = mergeFactor;
		}
		
		/// <seealso cref="#setMergeFactor">
		/// </seealso>
		public virtual int GetMergeFactor()
		{
			return mergeFactor;
		}
		
		/// <summary> If non-null, information about merges and a message when maxFieldLength
		/// is reached will be printed to this.
		/// </summary>
		public virtual void  SetInfoStream(System.IO.TextWriter infoStream)
		{
			this.infoStream = infoStream;
		}
		
		/// <seealso cref="#setInfoStream">
		/// </seealso>
		public virtual System.IO.TextWriter GetInfoStream()
		{
			return infoStream;
		}
		
		/// <summary> Sets the maximum time to wait for a write lock (in milliseconds) for this
		/// instance of IndexWriter.
		/// 
		/// </summary>
		/// <seealso cref="">
		/// </seealso>
		/// <seealso cref="to change the default value for all">
		/// instances of IndexWriter.
		/// </seealso>
		public virtual void  SetWriteLockTimeout(long writeLockTimeout)
		{
			this.writeLockTimeout = writeLockTimeout;
		}
		
		/// <seealso cref="#setWriteLockTimeout">
		/// </seealso>
		public virtual long GetWriteLockTimeout()
		{
			return writeLockTimeout;
		}
		
		/// <summary> Sets the default (for any instance of IndexWriter) maximum time to wait
		/// for a write lock (in milliseconds).
		/// </summary>
		public static void  SetDefaultWriteLockTimeout(long writeLockTimeout)
		{
			IndexWriter.WRITE_LOCK_TIMEOUT = writeLockTimeout;
		}
		
		/// <seealso cref="#setDefaultWriteLockTimeout">
		/// </seealso>
		public static long GetDefaultWriteLockTimeout()
		{
			return IndexWriter.WRITE_LOCK_TIMEOUT;
		}
		
		/// <summary> Flushes all changes to an index and closes all associated files.
		/// 
		/// <p>
		/// If an Exception is hit during close, eg due to disk full or some other
		/// reason, then both the on-disk index and the internal state of the
		/// IndexWriter instance will be consistent. However, the close will not be
		/// complete even though part of it (flushing buffered documents) may have
		/// succeeded, so the write lock will still be held.
		/// </p>
		/// 
		/// <p>
		/// If you can correct the underlying cause (eg free up some disk space) then
		/// you can call close() again. Failing that, if you want to force the write
		/// lock to be released (dangerous, because you may then lose buffered docs
		/// in the IndexWriter instance) then you can do something like this:
		/// </p>
		/// 
		/// <pre>
		/// try {
		/// writer.close();
		/// } finally {
		/// if (IndexReader.isLocked(directory)) {
		/// IndexReader.unlock(directory);
		/// }
		/// }
		/// </pre>
		/// 
		/// after which, you must be certain not to use the writer instance anymore.
		/// </p>
		/// </summary>
		public virtual void  Close()
		{
			lock (this)
			{
				FlushRamSegments();
				ramDirectory.Close();
				if (writeLock != null)
				{
					writeLock.Release(); // release write lock
					writeLock = null;
				}
				if (closeDir)
					directory.Close();
			}
		}
		
		/// <summary>Release the write lock, if needed. </summary>
		~IndexWriter()
		{
			try
			{
				if (writeLock != null)
				{
					writeLock.Release(); // release write lock
					writeLock = null;
				}
			}
			finally
			{
			}
		}
		
		/// <summary>Returns the Directory used by this index. </summary>
		public virtual Directory GetDirectory()
		{
			return directory;
		}
		
		/// <summary>Returns the analyzer used by this index. </summary>
		public virtual Analyzer GetAnalyzer()
		{
			return analyzer;
		}
		
		/// <summary>Returns the number of documents currently in this index. </summary>
		public virtual int DocCount()
		{
			lock (this)
			{
				int count = ramSegmentInfos.Count;
				for (int i = 0; i < segmentInfos.Count; i++)
				{
					SegmentInfo si = segmentInfos.Info(i);
					count += si.docCount;
				}
				return count;
			}
		}
		
		/// <summary> The maximum number of terms that will be indexed for a single field in a
		/// document. This limits the amount of memory required for indexing, so that
		/// collections with very large files will not crash the indexing process by
		/// running out of memory.<p/> Note that this effectively truncates large
		/// documents, excluding from the index terms that occur further in the
		/// document. If you know your source documents are large, be sure to set
		/// this value high enough to accomodate the expected size. If you set it to
		/// Integer.MAX_VALUE, then the only limit is your memory, but you should
		/// anticipate an OutOfMemoryError.<p/> By default, no more than 10,000
		/// terms will be indexed for a field.
		/// 
		/// </summary>
		private int maxFieldLength = DEFAULT_MAX_FIELD_LENGTH;
		
		/// <summary> Adds a document to this index. If the document contains more than
		/// {@link #SetMaxFieldLength(int)} terms for a given field, the remainder
		/// are discarded.
		/// 
		/// <p>
		/// Note that if an Exception is hit (for example disk full) then the index
		/// will be consistent, but this document may not have been added.
		/// Furthermore, it's possible the index will have one segment in
		/// non-compound format even when using compound files (when a merge has
		/// partially succeeded).
		/// </p>
		/// 
		/// <p>
		/// This method periodically flushes pending documents to the Directory
		/// (every {@link #setMaxBufferedDocs}), and also periodically merges
		/// segments in the index (every {@link #setMergeFactor} flushes). When this
		/// occurs, the method will take more time to run (possibly a long time if
		/// the index is large), and will require free temporary space in the
		/// Directory to do the merging.
		/// </p>
		/// 
		/// <p>
		/// The amount of free space required when a merge is triggered is up to 1X
		/// the size of all segments being merged, when no readers/searchers are open
		/// against the index, and up to 2X the size of all segments being merged
		/// when readers/searchers are open against the index (see
		/// {@link #Optimize()} for details). Most merges are small (merging the
		/// smallest segments together), but whenever a full merge occurs (all
		/// segments in the index, which is the worst case for temporary space usage)
		/// then the maximum free disk space required is the same as
		/// {@link #optimize}.
		/// </p>
		/// </summary>
		public virtual void  AddDocument(Document doc)
		{
			AddDocument(doc, analyzer);
		}
		
		/// <summary> Adds a document to this index, using the provided analyzer instead of the
		/// value of {@link #GetAnalyzer()}. If the document contains more than
		/// {@link #SetMaxFieldLength(int)} terms for a given field, the remainder
		/// are discarded.
		/// 
		/// <p>
		/// See {@link #AddDocument(Document)} for details on index and IndexWriter
		/// state after an Exception, and flushing/merging temporary free space
		/// requirements.
		/// </p>
		/// </summary>
		public virtual void  AddDocument(Document doc, Analyzer analyzer)
		{
			SegmentInfo newSegmentInfo = BuildSingleDocSegment(doc, analyzer);
			lock (this)
			{
				ramSegmentInfos.Add(newSegmentInfo);
				MaybeFlushRamSegments();
			}
		}
		
		internal virtual SegmentInfo BuildSingleDocSegment(Document doc, Analyzer analyzer)
		{
			DocumentWriter dw = new DocumentWriter(ramDirectory, analyzer, this);
			dw.SetInfoStream(infoStream);
			System.String segmentName = NewRamSegmentName();
			dw.AddDocument(segmentName, doc);
			return new SegmentInfo(segmentName, 1, ramDirectory, false, false);
		}
		
		/// <summary> Deletes the document(s) containing <code>term</code>.
		/// 
		/// </summary>
		/// <param name="">term
		/// the term to identify the documents to be deleted
		/// </param>
		public virtual void  DeleteDocuments(Term term)
		{
			lock (this)
			{
				BufferDeleteTerm(term);
				MaybeFlushRamSegments();
			}
		}
		
		/// <summary> Deletes the document(s) containing any of the terms. All deletes are
		/// flushed at the same time.
		/// 
		/// </summary>
		/// <param name="">terms
		/// array of terms to identify the documents to be deleted
		/// </param>
		public virtual void  DeleteDocuments(Term[] terms)
		{
			lock (this)
			{
				for (int i = 0; i < terms.Length; i++)
				{
					BufferDeleteTerm(terms[i]);
				}
				MaybeFlushRamSegments();
			}
		}
		
		/// <summary> Updates a document by first deleting the document(s) containing
		/// <code>term</code> and then adding the new document. The delete and then
		/// add are atomic as seen by a reader on the same index (flush may happen
		/// only after the add).
		/// 
		/// </summary>
		/// <param name="">term
		/// the term to identify the document(s) to be deleted
		/// </param>
		/// <param name="">doc
		/// the document to be added
		/// </param>
		public virtual void  UpdateDocument(Term term, Document doc)
		{
			UpdateDocument(term, doc, GetAnalyzer());
		}
		
		/// <summary> Updates a document by first deleting the document(s) containing
		/// <code>term</code> and then adding the new document. The delete and then
		/// add are atomic as seen by a reader on the same index (flush may happen
		/// only after the add).
		/// 
		/// </summary>
		/// <param name="">term
		/// the term to identify the document(s) to be deleted
		/// </param>
		/// <param name="">doc
		/// the document to be added
		/// </param>
		/// <param name="">analyzer
		/// the analyzer to use when analyzing the document
		/// </param>
		public virtual void  UpdateDocument(Term term, Document doc, Analyzer analyzer)
		{
			SegmentInfo newSegmentInfo = BuildSingleDocSegment(doc, analyzer);
			lock (this)
			{
				BufferDeleteTerm(term);
				ramSegmentInfos.Add(newSegmentInfo);
				MaybeFlushRamSegments();
			}
		}
		
		internal System.String NewRamSegmentName()
		{
			lock (this)
			{
#if !PRE_LUCENE_NET_2_0_0_COMPATIBLE
                return "_ram_" + Lucene.Net.Documents.NumberTools.ToString(ramSegmentInfos.counter++);
#else
				return "_ram_" + System.Convert.ToString(ramSegmentInfos.counter++, 16);
#endif
			}
		}
		
		// for test purpose
		public int GetSegmentCount()
		{
			lock (this)
			{
				return segmentInfos.Count;
			}
		}
		
		// for test purpose
		public int GetRamSegmentCount()
		{
			lock (this)
			{
				return ramSegmentInfos.Count;
			}
		}
		
		// for test purpose
		public int GetDocCount(int i)
		{
			lock (this)
			{
				if (i >= 0 && i < segmentInfos.Count)
				{
					return segmentInfos.Info(i).docCount;
				}
				else
				{
					return - 1;
				}
			}
		}
		
		internal System.String NewSegmentName()
		{
			lock (this)
			{
#if !PRE_LUCENE_NET_2_0_0_COMPATIBLE
                return "_" + Lucene.Net.Documents.NumberTools.ToString(segmentInfos.counter++);
#else
				return "_" + System.Convert.ToString(segmentInfos.counter++, 16);
#endif
			}
		}
		
		/// <summary> Determines how often segment indices are merged by addDocument(). With
		/// smaller values, less RAM is used while indexing, and searches on
		/// unoptimized indices are faster, but indexing speed is slower. With larger
		/// values, more RAM is used during indexing, and while searches on
		/// unoptimized indices are slower, indexing is faster. Thus larger values (>
		/// 10) are best for batch index creation, and smaller values (< 10) for
		/// indices that are interactively maintained.
		/// 
		/// <p>
		/// This must never be less than 2. The default value is
		/// {@link #DEFAULT_MERGE_FACTOR}.
		/// 
		/// </summary>
		private int mergeFactor = DEFAULT_MERGE_FACTOR;
		
		/// <summary> Determines the minimal number of documents required before the buffered
		/// in-memory documents are merging and a new Segment is created. Since
		/// Documents are merged in a {@link Lucene.Net.Store.RAMDirectory},
		/// large value gives faster indexing. At the same time, mergeFactor limits
		/// the number of files open in a FSDirectory.
		/// 
		/// <p>
		/// The default value is {@link #DEFAULT_MAX_BUFFERED_DOCS}.
		/// 
		/// </summary>
		private int minMergeDocs = DEFAULT_MAX_BUFFERED_DOCS;
		
		/// <summary> Determines the largest number of documents ever merged by addDocument().
		/// Small values (e.g., less than 10,000) are best for interactive indexing,
		/// as this limits the length of pauses while indexing to a few seconds.
		/// Larger values are best for batched indexing and speedier searches.
		/// 
		/// <p>
		/// The default value is {@link #DEFAULT_MAX_MERGE_DOCS}.
		/// 
		/// </summary>
		private int maxMergeDocs = DEFAULT_MAX_MERGE_DOCS;
		
		/// <summary> If non-null, information about merges will be printed to this.
		/// 
		/// </summary>
		private System.IO.TextWriter infoStream = null;
		
		/// <summary> Merges all segments together into a single segment, optimizing an index
		/// for search.
		/// 
		/// <p>
		/// Note that this requires substantial temporary free space in the Directory
		/// (see <a target="_top"
		/// href="http://issues.apache.org/jira/browse/LUCENE-764">LUCENE-764</a>
		/// for details):
		/// </p>
		/// 
		/// <ul>
		/// <li>
		/// 
		/// <p>
		/// If no readers/searchers are open against the index, then free space
		/// required is up to 1X the total size of the starting index. For example,
		/// if the starting index is 10 GB, then you must have up to 10 GB of free
		/// space before calling optimize.
		/// </p>
		/// 
		/// <li>
		/// 
		/// <p>
		/// If readers/searchers are using the index, then free space required is up
		/// to 2X the size of the starting index. This is because in addition to the
		/// 1X used by optimize, the original 1X of the starting index is still
		/// consuming space in the Directory as the readers are holding the segments
		/// files open. Even on Unix, where it will appear as if the files are gone
		/// ("ls" won't list them), they still consume storage due to "delete on last
		/// close" semantics.
		/// </p>
		/// 
		/// <p>
		/// Furthermore, if some but not all readers re-open while the optimize is
		/// underway, this will cause > 2X temporary space to be consumed as those
		/// new readers will then hold open the partially optimized segments at that
		/// time. It is best not to re-open readers while optimize is running.
		/// </p>
		/// 
		/// </ul>
		/// 
		/// <p>
		/// The actual temporary usage could be much less than these figures (it
		/// depends on many factors).
		/// </p>
		/// 
		/// <p>
		/// Once the optimize completes, the total size of the index will be less
		/// than the size of the starting index. It could be quite a bit smaller (if
		/// there were many pending deletes) or just slightly smaller.
		/// </p>
		/// 
		/// <p>
		/// If an Exception is hit during optimize(), for example due to disk full,
		/// the index will not be corrupt and no documents will have been lost.
		/// However, it may have been partially optimized (some segments were merged
		/// but not all), and it's possible that one of the segments in the index
		/// will be in non-compound format even when using compound file format. This
		/// will occur when the Exception is hit during conversion of the segment
		/// into compound format.
		/// </p>
		/// </summary>
		public virtual void  Optimize()
		{
			lock (this)
			{
				FlushRamSegments();
				while (segmentInfos.Count > 1 || (segmentInfos.Count == 1 && (SegmentReader.HasDeletions(segmentInfos.Info(0)) || SegmentReader.HasSeparateNorms(segmentInfos.Info(0)) || segmentInfos.Info(0).dir != directory || (useCompoundFile && (!SegmentReader.UsesCompoundFile(segmentInfos.Info(0)))))))
				{
					int minSegment = segmentInfos.Count - mergeFactor;
					MergeSegments(segmentInfos, minSegment < 0?0:minSegment, segmentInfos.Count);
				}
			}
		}
		
		/*
		* Begin a transaction. During a transaction, any segment merges that happen
		* (or ram segments flushed) will not write a new segments file and will not
		* remove any files that were present at the start of the transaction. You
		* must make a matched (try/finall) call to commitTransaction() or
		* rollbackTransaction() to finish the transaction.
		*/
		private void  StartTransaction()
		{
			if (inTransaction)
			{
				throw new System.IO.IOException("transaction is already in process");
			}
			rollbackSegmentInfos = (SegmentInfos) segmentInfos.Clone();
			protectedSegments = new System.Collections.Hashtable();
			for (int i = 0; i < segmentInfos.Count; i++)
			{
				SegmentInfo si = (SegmentInfo) segmentInfos[i];
				protectedSegments.Add(si.name, si.name);
			}
			inTransaction = true;
		}
		
		/*
		* Rolls back the transaction and restores state to where we were at the
		* start.
		*/
		private void  RollbackTransaction()
		{
			
			// Keep the same segmentInfos instance but replace all
			// of its SegmentInfo instances. This is so the next
			// attempt to commit using this instance of IndexWriter
			// will always write to a new generation ("write once").
			segmentInfos.Clear();
			segmentInfos.AddRange(rollbackSegmentInfos);
			
			// Ask deleter to locate unreferenced files & remove
			// them:
			deleter.ClearPendingFiles();
			deleter.FindDeletableFiles();
			deleter.DeleteFiles();
			
			ClearTransaction();
		}
		
		/*
		* Commits the transaction. This will write the new segments file and remove
		* and pending deletions we have accumulated during the transaction
		*/
		private void  CommitTransaction()
		{
			if (commitPending)
			{
				bool success = false;
				try
				{
					// If we hit eg disk full during this write we have
					// to rollback.:
					segmentInfos.Write(directory); // commit changes
					success = true;
				}
				finally
				{
					if (!success)
					{
						RollbackTransaction();
					}
				}
				deleter.CommitPendingFiles();
				commitPending = false;
			}
			
			ClearTransaction();
		}
		
		/*
		* Should only be called by rollbackTransaction & commitTransaction
		*/
		private void  ClearTransaction()
		{
			protectedSegments = null;
			rollbackSegmentInfos = null;
			inTransaction = false;
		}
		
		/// <summary> Merges all segments from an array of indexes into this index.
		/// 
		/// <p>
		/// This may be used to parallelize batch indexing. A large document
		/// collection can be broken into sub-collections. Each sub-collection can be
		/// indexed in parallel, on a different thread, process or machine. The
		/// complete index can then be created by merging sub-collection indexes with
		/// this method.
		/// 
		/// <p>
		/// After this completes, the index is optimized.
		/// 
		/// <p>
		/// This method is transactional in how Exceptions are handled: it does not
		/// commit a new segments_N file until all indexes are added. This means if
		/// an Exception occurs (for example disk full), then either no indexes will
		/// have been added or they all will have been.
		/// </p>
		/// 
		/// <p>
		/// If an Exception is hit, it's still possible that all indexes were
		/// successfully added. This happens when the Exception is hit when trying to
		/// build a CFS file. In this case, one segment in the index will be in
		/// non-CFS format, even when using compound file format.
		/// </p>
		/// 
		/// <p>
		/// Also note that on an Exception, the index may still have been partially
		/// or fully optimized even though none of the input indexes were added.
		/// </p>
		/// 
		/// <p>
		/// Note that this requires temporary free space in the Directory up to 2X
		/// the sum of all input indexes (including the starting index). If
		/// readers/searchers are open against the starting index, then temporary
		/// free space required will be higher by the size of the starting index (see
		/// {@link #Optimize()} for details).
		/// </p>
		/// 
		/// <p>
		/// Once this completes, the final size of the index will be less than the
		/// sum of all input index sizes (including the starting index). It could be
		/// quite a bit smaller (if there were many pending deletes) or just slightly
		/// smaller.
		/// </p>
		/// 
		/// <p>
		/// See <a target="_top"
		/// href="http://issues.apache.org/jira/browse/LUCENE-702">LUCENE-702</a>
		/// for details.
		/// </p>
		/// </summary>
		public virtual void  AddIndexes(Directory[] dirs)
		{
			lock (this)
			{
				
				Optimize(); // start with zero or 1 seg
				
				int start = segmentInfos.Count;
				
				bool success = false;
				
				StartTransaction();
				
				try
				{
					for (int i = 0; i < dirs.Length; i++)
					{
						SegmentInfos sis = new SegmentInfos(); // read infos from dir
						sis.Read(dirs[i]);
						for (int j = 0; j < sis.Count; j++)
						{
							segmentInfos.Add(sis.Info(j)); // add each info
						}
					}
					
					// merge newly added segments in log(n) passes
					while (segmentInfos.Count > start + mergeFactor)
					{
						for (int base_Renamed = start; base_Renamed < segmentInfos.Count; base_Renamed++)
						{
							int end = System.Math.Min(segmentInfos.Count, base_Renamed + mergeFactor);
							if (end - base_Renamed > 1)
							{
								MergeSegments(segmentInfos, base_Renamed, end);
							}
						}
					}
					success = true;
				}
				finally
				{
					if (success)
					{
						CommitTransaction();
					}
					else
					{
						RollbackTransaction();
					}
				}
				
				Optimize(); // final cleanup
			}
		}
		
		/// <summary> Merges all segments from an array of indexes into this index.
		/// <p>
		/// This is similar to addIndexes(Directory[]). However, no optimize() is
		/// called either at the beginning or at the end. Instead, merges are carried
		/// out as necessary.
		/// <p>
		/// This requires this index not be among those to be added, and the upper
		/// bound* of those segment doc counts not exceed maxMergeDocs.
		/// 
		/// <p>
		/// See {@link #AddIndexes(Directory[])} for details on transactional
		/// semantics, temporary free space required in the Directory, and non-CFS
		/// segments on an Exception.
		/// </p>
		/// </summary>
		public virtual void  AddIndexesNoOptimize(Directory[] dirs)
		{
			lock (this)
			{
				// Adding indexes can be viewed as adding a sequence of segments S to
				// a sequence of segments T. Segments in T follow the invariants but
				// segments in S may not since they could come from multiple indexes.
				// Here is the merge algorithm for addIndexesNoOptimize():
				//
				// 1 Flush ram segments.
				// 2 Consider a combined sequence with segments from T followed
				// by segments from S (same as current addIndexes(Directory[])).
				// 3 Assume the highest level for segments in S is h. Call
				// maybeMergeSegments(), but instead of starting w/ lowerBound = -1
				// and upperBound = maxBufferedDocs, start w/ lowerBound = -1 and
				// upperBound = upperBound of level h. After this, the invariants
				// are guaranteed except for the last < M segments whose levels <= h.
				// 4 If the invariants hold for the last < M segments whose levels <= h,
				// if some of those < M segments are from S (not merged in step 3),
				// properly copy them over*, otherwise done.
				// Otherwise, simply merge those segments. If the merge results in
				// a segment of level <= h, done. Otherwise, it's of level h+1 and call
				// maybeMergeSegments() starting w/ upperBound = upperBound of level
				// h+1.
				//
				// * Ideally, we want to simply copy a segment. However, directory does
				// not support copy yet. In addition, source may use compound file or
				// not
				// and target may use compound file or not. So we use mergeSegments() to
				// copy a segment, which may cause doc count to change because deleted
				// docs are garbage collected.
				
				// 1 flush ram segments
				
				FlushRamSegments();
				
				// 2 copy segment infos and find the highest level from dirs
				int start = segmentInfos.Count;
				int startUpperBound = minMergeDocs;
				
				bool success = false;
				
				StartTransaction();
				
				try
				{
					
					try
					{
						for (int i = 0; i < dirs.Length; i++)
						{
							if (directory == dirs[i])
							{
								// cannot add this index: segments may be deleted in
								// merge before added
								throw new System.ArgumentException("Cannot add this index to itself");
							}
							
							SegmentInfos sis = new SegmentInfos(); // read infos from
							// dir
							sis.Read(dirs[i]);
							for (int j = 0; j < sis.Count; j++)
							{
								SegmentInfo info = sis.Info(j);
								segmentInfos.Add(info); // add each info
								
								while (startUpperBound < info.docCount)
								{
									startUpperBound *= mergeFactor; // find the highest
									// level from dirs
									if (startUpperBound > maxMergeDocs)
									{
										// upper bound cannot exceed maxMergeDocs
										throw new System.ArgumentException("Upper bound cannot exceed maxMergeDocs");
									}
								}
							}
						}
					}
					catch (System.ArgumentException e)
					{
						for (int i = segmentInfos.Count - 1; i >= start; i--)
						{
							segmentInfos.RemoveAt(i);
						}
						throw e;
					}
					
					// 3 maybe merge segments starting from the highest level from dirs
					MaybeMergeSegments(startUpperBound);
					
					// get the tail segments whose levels <= h
					int segmentCount = segmentInfos.Count;
					int numTailSegments = 0;
					while (numTailSegments < segmentCount && startUpperBound >= segmentInfos.Info(segmentCount - 1 - numTailSegments).docCount)
					{
						numTailSegments++;
					}
					if (numTailSegments == 0)
					{
						success = true;
						return ;
					}
					
					// 4 make sure invariants hold for the tail segments whose levels <=
					// h
					if (CheckNonDecreasingLevels(segmentCount - numTailSegments))
					{
						// identify the segments from S to be copied (not merged in 3)
						int numSegmentsToCopy = 0;
						while (numSegmentsToCopy < segmentCount && directory != segmentInfos.Info(segmentCount - 1 - numSegmentsToCopy).dir)
						{
							numSegmentsToCopy++;
						}
						if (numSegmentsToCopy == 0)
						{
							success = true;
							return ;
						}
						
						// copy those segments from S
						for (int i = segmentCount - numSegmentsToCopy; i < segmentCount; i++)
						{
							MergeSegments(segmentInfos, i, i + 1);
						}
						if (CheckNonDecreasingLevels(segmentCount - numSegmentsToCopy))
						{
							success = true;
							return ;
						}
					}
					
					// invariants do not hold, simply merge those segments
					MergeSegments(segmentInfos, segmentCount - numTailSegments, segmentCount);
					
					// maybe merge segments again if necessary
					if (segmentInfos.Info(segmentInfos.Count - 1).docCount > startUpperBound)
					{
						MaybeMergeSegments(startUpperBound * mergeFactor);
					}
					
					success = true;
				}
				finally
				{
					if (success)
					{
						CommitTransaction();
					}
					else
					{
						RollbackTransaction();
					}
				}
			}
		}
		
		/// <summary> Merges the provided indexes into this index.
		/// <p>
		/// After this completes, the index is optimized.
		/// </p>
		/// <p>
		/// The provided IndexReaders are not closed.
		/// </p>
		/// 
		/// <p>
		/// See {@link #AddIndexes(Directory[])} for details on transactional
		/// semantics, temporary free space required in the Directory, and non-CFS
		/// segments on an Exception.
		/// </p>
		/// </summary>
		public virtual void  AddIndexes(IndexReader[] readers)
		{
			lock (this)
			{
				
				Optimize(); // start with zero or 1 seg
				
				System.String mergedName = NewSegmentName();
				SegmentMerger merger = new SegmentMerger(this, mergedName);
				
				System.Collections.ArrayList segmentsToDelete = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
				IndexReader sReader = null;
				if (segmentInfos.Count == 1)
				{
					// add existing index, if any
					sReader = SegmentReader.Get(segmentInfos.Info(0));
					merger.Add(sReader);
					segmentsToDelete.Add(sReader); // queue segment for
					// deletion
				}
				
				for (int i = 0; i < readers.Length; i++)
				// add new indexes
					merger.Add(readers[i]);
				
				SegmentInfo info;
				
				System.String segmentsInfosFileName = segmentInfos.GetCurrentSegmentFileName();
				
				bool success = false;
				
				StartTransaction();
				
				try
				{
					int docCount = merger.Merge(); // merge 'em
					
					segmentInfos.RemoveRange(0, segmentInfos.Count); // pop old infos & add new
					info = new SegmentInfo(mergedName, docCount, directory, false, true);
					segmentInfos.Add(info);
					commitPending = true;
					
					if (sReader != null)
						sReader.Close();
					
					success = true;
				}
				finally
				{
					if (!success)
					{
						RollbackTransaction();
					}
					else
					{
						CommitTransaction();
					}
				}
				
				deleter.DeleteFile(segmentsInfosFileName); // delete old segments_N
				// file
				deleter.DeleteSegments(segmentsToDelete); // delete now-unused
				// segments
				
				if (useCompoundFile)
				{
					success = false;
					
					segmentsInfosFileName = segmentInfos.GetCurrentSegmentFileName();
					System.Collections.ArrayList filesToDelete;
					
					StartTransaction();
					
					try
					{
						
						filesToDelete = merger.CreateCompoundFile(mergedName + ".cfs");
						
						info.SetUseCompoundFile(true);
						commitPending = true;
						success = true;
					}
					finally
					{
						if (!success)
						{
							RollbackTransaction();
						}
						else
						{
							CommitTransaction();
						}
					}
					
					deleter.DeleteFile(segmentsInfosFileName); // delete old segments_N
					// file
					deleter.DeleteFiles(filesToDelete); // delete now unused files of
					// segment
				}
			}
		}
		
		// Overview of merge policy:
		//
		// A flush is triggered either by close() or by the number of ram segments
		// reaching maxBufferedDocs. After a disk segment is created by the flush,
		// further merges may be triggered.
		//
		// LowerBound and upperBound set the limits on the doc count of a segment
		// which may be merged. Initially, lowerBound is set to 0 and upperBound
		// to maxBufferedDocs. Starting from the rightmost* segment whose doc count
		// > lowerBound and <= upperBound, count the number of consecutive segments
		// whose doc count <= upperBound.
		//
		// Case 1: number of worthy segments < mergeFactor, no merge, done.
		// Case 2: number of worthy segments == mergeFactor, merge these segments.
		// If the doc count of the merged segment <= upperBound, done.
		// Otherwise, set lowerBound to upperBound, and multiply upperBound
		// by mergeFactor, go through the process again.
		// Case 3: number of worthy segments > mergeFactor (in the case mergeFactor
		// M changes), merge the leftmost* M segments. If the doc count of
		// the merged segment <= upperBound, consider the merged segment for
		// further merges on this same level. Merge the now leftmost* M
		// segments, and so on, until number of worthy segments < mergeFactor.
		// If the doc count of all the merged segments <= upperBound, done.
		// Otherwise, set lowerBound to upperBound, and multiply upperBound
		// by mergeFactor, go through the process again.
		// Note that case 2 can be considerd as a special case of case 3.
		//
		// This merge policy guarantees two invariants if M does not change and
		// segment doc count is not reaching maxMergeDocs:
		// B for maxBufferedDocs, f(n) defined as ceil(log_M(ceil(n/B)))
		// 1: If i (left*) and i+1 (right*) are two consecutive segments of doc
		// counts x and y, then f(x) >= f(y).
		// 2: The number of committed segments on the same level (f(n)) <= M.
		
		// This is called after pending added and deleted
		// documents have been flushed to the Directory but before
		// the change is committed (new segments_N file written).
		internal virtual void  DoAfterFlush()
		{
		}
		
		protected internal void  MaybeFlushRamSegments()
		{
			// A flush is triggered if enough new documents are buffered or
			// if enough delete terms are buffered
			if (ramSegmentInfos.Count >= minMergeDocs || numBufferedDeleteTerms >= maxBufferedDeleteTerms)
			{
				FlushRamSegments();
			}
		}
		
		/// <summary> Expert: Flushes all RAM-resident segments (buffered documents), then may
		/// merge segments.
		/// </summary>
		private void  FlushRamSegments()
		{
			lock (this)
			{
				if (ramSegmentInfos.Count > 0 || bufferedDeleteTerms.Count > 0)
				{
					MergeSegments(ramSegmentInfos, 0, ramSegmentInfos.Count);
					MaybeMergeSegments(minMergeDocs);
				}
			}
		}
		
		/// <summary> Flush all in-memory buffered updates (adds and deletes) to the Directory.
		/// 
		/// </summary>
		/// <throws>  IOException </throws>
		public void  Flush()
		{
			lock (this)
			{
				FlushRamSegments();
			}
		}
		
		/// <summary> Expert: Return the total size of all index files currently cached in
		/// memory. Useful for size management with flushRamDocs()
		/// </summary>
		public long RamSizeInBytes()
		{
			return ramDirectory.SizeInBytes();
		}
		
		/// <summary> Expert: Return the number of documents whose segments are currently
		/// cached in memory. Useful when calling flushRamSegments()
		/// </summary>
		public int NumRamDocs()
		{
			lock (this)
			{
				return ramSegmentInfos.Count;
			}
		}
		
		/// <summary>Incremental segment merger. </summary>
		private void  MaybeMergeSegments(int startUpperBound)
		{
			long lowerBound = - 1;
			long upperBound = startUpperBound;
			
			while (upperBound < maxMergeDocs)
			{
				int minSegment = segmentInfos.Count;
				int maxSegment = - 1;
				
				// find merge-worthy segments
				while (--minSegment >= 0)
				{
					SegmentInfo si = segmentInfos.Info(minSegment);
					
					if (maxSegment == - 1 && si.docCount > lowerBound && si.docCount <= upperBound)
					{
						// start from the rightmost* segment whose doc count is in
						// bounds
						maxSegment = minSegment;
					}
					else if (si.docCount > upperBound)
					{
						// until the segment whose doc count exceeds upperBound
						break;
					}
				}
				
				minSegment++;
				maxSegment++;
				int numSegments = maxSegment - minSegment;
				
				if (numSegments < mergeFactor)
				{
					break;
				}
				else
				{
					bool exceedsUpperLimit = false;
					
					// number of merge-worthy segments may exceed mergeFactor when
					// mergeFactor and/or maxBufferedDocs change(s)
					while (numSegments >= mergeFactor)
					{
						// merge the leftmost* mergeFactor segments
						
						int docCount = MergeSegments(segmentInfos, minSegment, minSegment + mergeFactor);
						numSegments -= mergeFactor;
						
						if (docCount > upperBound)
						{
							// continue to merge the rest of the worthy segments on
							// this level
							minSegment++;
							exceedsUpperLimit = true;
						}
						else
						{
							// if the merged segment does not exceed upperBound,
							// consider
							// this segment for further merges on this same level
							numSegments++;
						}
					}
					
					if (!exceedsUpperLimit)
					{
						// if none of the merged segments exceed upperBound, done
						break;
					}
				}
				
				lowerBound = upperBound;
				upperBound *= mergeFactor;
			}
		}
		
		/// <summary> Merges the named range of segments, replacing them in the stack with a
		/// single segment.
		/// </summary>
		private int MergeSegments(SegmentInfos sourceSegments, int minSegment, int end)
		{
			
			// We may be called solely because there are deletes
			// pending, in which case doMerge is false:
			bool doMerge = end > 0;
			System.String mergedName = NewSegmentName();
			SegmentMerger merger = null;
			
			System.Collections.ArrayList segmentsToDelete = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			
			System.String segmentsInfosFileName = segmentInfos.GetCurrentSegmentFileName();
			System.String nextSegmentsFileName = segmentInfos.GetNextSegmentFileName();
			
			SegmentInfo newSegment = null;
			
			int mergedDocCount = 0;
			
			// This is try/finally to make sure merger's readers are closed:
			try
			{
				
				if (doMerge)
				{
					if (infoStream != null)
						infoStream.Write("merging segments");
					merger = new SegmentMerger(this, mergedName);
					
					for (int i = minSegment; i < end; i++)
					{
						SegmentInfo si = sourceSegments.Info(i);
						if (infoStream != null)
							infoStream.Write(" " + si.name + " (" + si.docCount + " docs)");
						IndexReader reader = SegmentReader.Get(si); // no need to
						// set deleter
						// (yet)
						merger.Add(reader);
						if ((reader.Directory() == this.directory) || (reader.Directory() == this.ramDirectory))
							segmentsToDelete.Add(reader); // queue segment
						// for deletion
					}
				}
				
				SegmentInfos rollback = null;
				bool success = false;
				
				// This is try/finally to rollback our internal state
				// if we hit exception when doing the merge:
				try
				{
					
					if (doMerge)
					{
						mergedDocCount = merger.Merge();
						
						if (infoStream != null)
						{
							infoStream.WriteLine(" into " + mergedName + " (" + mergedDocCount + " docs)");
						}
						
						newSegment = new SegmentInfo(mergedName, mergedDocCount, directory, false, true);
					}
					
					if (!inTransaction && (sourceSegments != ramSegmentInfos || bufferedDeleteTerms.Count > 0))
					{
						// Now save the SegmentInfo instances that
						// we are replacing:
						rollback = (SegmentInfos) segmentInfos.Clone();
					}
					
					if (doMerge)
					{
						if (sourceSegments == ramSegmentInfos)
						{
							segmentInfos.Add(newSegment);
						}
						else
						{
							for (int i = end - 1; i > minSegment; i--)
							// remove old infos & add new
								sourceSegments.RemoveAt(i);
							
							segmentInfos[minSegment] = newSegment;
						}
					}
					
					if (sourceSegments == ramSegmentInfos)
					{
						// Should not be necessary: no prior commit should
						// have left pending files, so just defensive:
						deleter.ClearPendingFiles();
						MaybeApplyDeletes(doMerge);
						DoAfterFlush();
					}
					
					if (!inTransaction)
					{
						segmentInfos.Write(directory); // commit before deleting
					}
					else
					{
						commitPending = true;
					}
					
					success = true;
				}
				finally
				{
					
					if (success)
					{
						// The non-ram-segments case is already committed
						// (above), so all the remains for ram segments case
						// is to clear the ram segments:
						if (sourceSegments == ramSegmentInfos)
						{
							ramSegmentInfos.Clear();
						}
					}
					else if (!inTransaction)
					{
						
						// Must rollback so our state matches index:
						
						if (sourceSegments == ramSegmentInfos && 0 == bufferedDeleteTerms.Count)
						{
							// Simple case: newSegment may or may not have
							// been added to the end of our segment infos,
							// so just check & remove if so:
							if (newSegment != null && segmentInfos.Count > 0 && segmentInfos.Info(segmentInfos.Count - 1) == newSegment)
							{
								segmentInfos.RemoveAt(segmentInfos.Count - 1);
							}
						}
						else if (rollback != null)
						{
							// Rollback the individual SegmentInfo
							// instances, but keep original SegmentInfos
							// instance (so we don't try to write again the
							// same segments_N file -- write once):
							segmentInfos.Clear();
							segmentInfos.AddRange(rollback);
						}
						
						// Erase any pending files that we were going to delete:
						// i.e. old del files added by SegmentReader.doCommit()
						deleter.ClearPendingFiles();
						
						// Delete any partially created files:
						deleter.DeleteFile(nextSegmentsFileName);
						deleter.FindDeletableFiles();
						deleter.DeleteFiles();
					}
				}
			}
			finally
			{
				// close readers before we attempt to delete now-obsolete segments
				if (doMerge)
					merger.CloseReaders();
			}
			
			if (!inTransaction)
			{
				// Attempt to delete all files we just obsoleted:
				deleter.DeleteFile(segmentsInfosFileName); // delete old segments_N
				// file
				deleter.DeleteSegments(segmentsToDelete); // delete now-unused
				// segments
				// Includes the old del files
				deleter.CommitPendingFiles();
			}
			else
			{
				deleter.AddPendingFile(segmentsInfosFileName); // delete old
				// segments_N file
				deleter.DeleteSegments(segmentsToDelete, protectedSegments); // delete
				// now-unused
				// segments
			}
			
			if (useCompoundFile && doMerge)
			{
				
				segmentsInfosFileName = nextSegmentsFileName;
				nextSegmentsFileName = segmentInfos.GetNextSegmentFileName();
				
				System.Collections.ArrayList filesToDelete;
				
				bool success = false;
				
				try
				{
					
					filesToDelete = merger.CreateCompoundFile(mergedName + ".cfs");
					newSegment.SetUseCompoundFile(true);
					if (!inTransaction)
					{
						segmentInfos.Write(directory); // commit again so readers
						// know we've switched this
						// segment to a compound
						// file
					}
					success = true;
				}
				finally
				{
					if (!success && !inTransaction)
					{
						// Must rollback:
						newSegment.SetUseCompoundFile(false);
						deleter.DeleteFile(mergedName + ".cfs");
						deleter.DeleteFile(nextSegmentsFileName);
					}
				}
				
				if (!inTransaction)
				{
					deleter.DeleteFile(segmentsInfosFileName); // delete old
					// segments_N file
				}
				
				// We can delete these segments whether or not we are
				// in a transaction because we had just written them
				// above so they can't need protection by the
				// transaction:
				deleter.DeleteFiles(filesToDelete); // delete now-unused segments
			}
			
			return mergedDocCount;
		}
		
		// Called during flush to apply any buffered deletes. If
		// doMerge is true then a new segment was just created and
		// flushed from the ram segments.
		private void  MaybeApplyDeletes(bool doMerge)
		{
			
			if (bufferedDeleteTerms.Count > 0)
			{
				if (infoStream != null)
					infoStream.WriteLine("flush " + numBufferedDeleteTerms + " buffered deleted terms on " + segmentInfos.Count + " segments.");
				
				if (doMerge)
				{
					IndexReader reader = null;
					try
					{
						reader = SegmentReader.Get(segmentInfos.Info(segmentInfos.Count - 1));
						reader.SetDeleter(deleter);
						
						// Apply delete terms to the segment just flushed from ram
						// apply appropriately so that a delete term is only applied
						// to
						// the documents buffered before it, not those buffered
						// after it.
						ApplyDeletesSelectively(bufferedDeleteTerms, reader);
					}
					finally
					{
						if (reader != null)
							reader.Close();
					}
				}
				
				int infosEnd = segmentInfos.Count;
				if (doMerge)
				{
					infosEnd--;
				}
				
				for (int i = 0; i < infosEnd; i++)
				{
					IndexReader reader = null;
					try
					{
						reader = SegmentReader.Get(segmentInfos.Info(i));
						reader.SetDeleter(deleter);
						
						// Apply delete terms to disk segments
						// except the one just flushed from ram.
						ApplyDeletes(bufferedDeleteTerms, reader);
					}
					finally
					{
						if (reader != null)
							reader.Close();
					}
				}
				
				// Clean up bufferedDeleteTerms.
				bufferedDeleteTerms.Clear();
				numBufferedDeleteTerms = 0;
			}
		}
		
		private bool CheckNonDecreasingLevels(int start)
		{
			int lowerBound = - 1;
			int upperBound = minMergeDocs;
			
			for (int i = segmentInfos.Count - 1; i >= start; i--)
			{
				int docCount = segmentInfos.Info(i).docCount;
				if (docCount <= lowerBound)
				{
					return false;
				}
				
				while (docCount > upperBound)
				{
					lowerBound = upperBound;
					upperBound *= mergeFactor;
				}
			}
			return true;
		}
		
		// For test purposes.
		public int GetBufferedDeleteTermsSize()
		{
			lock (this)
			{
				return bufferedDeleteTerms.Count;
			}
		}
		
		// For test purposes.
		public int GetNumBufferedDeleteTerms()
		{
			lock (this)
			{
				return numBufferedDeleteTerms;
			}
		}
		
		// Number of ram segments a delete term applies to.
		private class Num
		{
			private void  InitBlock(IndexWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private IndexWriter enclosingInstance;
			public IndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private int num;
			
			internal Num(IndexWriter enclosingInstance, int num)
			{
				InitBlock(enclosingInstance);
				this.num = num;
			}
			
			internal virtual int GetNum()
			{
				return num;
			}
			
			internal virtual void  SetNum(int num)
			{
				this.num = num;
			}
		}
		
		// Buffer a term in bufferedDeleteTerms, which records the
		// current number of documents buffered in ram so that the
		// delete term will be applied to those ram segments as
		// well as the disk segments.
		private void  BufferDeleteTerm(Term term)
		{
			Num num = (Num) bufferedDeleteTerms[term];
			if (num == null)
			{
				bufferedDeleteTerms[term] = new Num(this, ramSegmentInfos.Count);
			}
			else
			{
				num.SetNum(ramSegmentInfos.Count);
			}
			numBufferedDeleteTerms++;
		}
		
		// Apply buffered delete terms to the segment just flushed from ram
		// apply appropriately so that a delete term is only applied to
		// the documents buffered before it, not those buffered after it.
		private void  ApplyDeletesSelectively(System.Collections.Hashtable deleteTerms, IndexReader reader)
		{
			System.Collections.IEnumerator iter = new System.Collections.Hashtable(deleteTerms).GetEnumerator();
			while (iter.MoveNext())
			{
				System.Collections.DictionaryEntry entry = (System.Collections.DictionaryEntry) iter.Current;
				Term term = (Term) entry.Key;
				
				TermDocs docs = reader.TermDocs(term);
				if (docs != null)
				{
					int num = ((Num) entry.Value).GetNum();
					try
					{
						while (docs.Next())
						{
							int doc = docs.Doc();
							if (doc >= num)
							{
								break;
							}
							reader.DeleteDocument(doc);
						}
					}
					finally
					{
						docs.Close();
					}
				}
			}
		}
		
		// Apply buffered delete terms to this reader.
		private void  ApplyDeletes(System.Collections.Hashtable deleteTerms, IndexReader reader)
		{
			System.Collections.IEnumerator iter = new System.Collections.Hashtable(deleteTerms).GetEnumerator();
			while (iter.MoveNext())
			{
				System.Collections.DictionaryEntry entry = (System.Collections.DictionaryEntry) iter.Current;
				reader.DeleteDocuments((Term) entry.Key);
			}
		}
	}
}