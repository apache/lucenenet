/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
	
	
	/// <summary>An IndexWriter creates and maintains an index.
	/// The third argument to the 
	/// <a href="#IndexWriter(Lucene.Net.store.Directory, Lucene.Net.analysis.Analyzer, boolean)"><b>constructor</b></a>
	/// determines whether a new index is created, or whether an existing index is
	/// opened for the addition of new documents.
	/// In either case, documents are added with the <a
	/// href="#addDocument(Lucene.Net.document.Document)"><b>addDocument</b></a> method.  
	/// When finished adding documents, <a href="#close()"><b>close</b></a> should be called.
	/// <p>If an index will not have more documents added for a while and optimal search
	/// performance is desired, then the <a href="#optimize()"><b>optimize</b></a>
	/// method should be called before the index is closed.
	/// </summary>
	/// <summary><p>Opening an IndexWriter creates a lock file for the directory in use. Trying to open
	/// another IndexWriter on the same directory will lead to an IOException. The IOException
	/// is also thrown if an IndexReader on the same directory is used to delete documents
	/// from the index.
	/// </summary>
	/// <seealso cref="IndexModifier IndexModifier supports the important methods of IndexWriter plus deletion">
	/// </seealso>
	
	public class IndexWriter
	{
		private class AnonymousClassWith : Lock.With
		{
			private void  InitBlock(bool create, IndexWriter enclosingInstance)
			{
				this.create = create;
				this.enclosingInstance = enclosingInstance;
			}
			private bool create;
			private IndexWriter enclosingInstance;
			public IndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
            internal AnonymousClassWith(bool create, IndexWriter enclosingInstance, Lucene.Net.Store.Lock Param1, long Param2):base(Param1, Param2)
            {
				InitBlock(create, enclosingInstance);
			}
			public override System.Object DoBody()
			{
				if (create)
					Enclosing_Instance.segmentInfos.Write(Enclosing_Instance.directory);
				else
					Enclosing_Instance.segmentInfos.Read(Enclosing_Instance.directory);
				return null;
			}
		}
		private class AnonymousClassWith1 : Lock.With
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
            internal AnonymousClassWith1(IndexWriter enclosingInstance, Lucene.Net.Store.Lock Param1, long Param2):base(Param1, Param2)
            {
                InitBlock(enclosingInstance);
            }
			public override System.Object DoBody()
			{
				Enclosing_Instance.segmentInfos.Write(Enclosing_Instance.directory); // commit changes
				return null;
			}
		}
		private class AnonymousClassWith2 : Lock.With
		{
            private void  InitBlock(System.String mergedName, IndexWriter enclosingInstance)
            {
				this.mergedName = mergedName;
				this.enclosingInstance = enclosingInstance;
			}
			private System.String mergedName;
			private IndexWriter enclosingInstance;
			public IndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
            internal AnonymousClassWith2(System.String mergedName, IndexWriter enclosingInstance, Lucene.Net.Store.Lock Param1, long Param2):base(Param1, Param2)
            {
                InitBlock(mergedName, enclosingInstance);
            }
			public override System.Object DoBody()
			{
				// make compound file visible for SegmentReaders
				Enclosing_Instance.directory.RenameFile(mergedName + ".tmp", mergedName + ".cfs");
				return null;
			}
		}
		private class AnonymousClassWith3 : Lock.With
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
			internal AnonymousClassWith3(IndexWriter enclosingInstance, Lucene.Net.Store.Lock Param1, long Param2):base(Param1, Param2)
			{
				InitBlock(enclosingInstance);
			}
			public override System.Object DoBody()
			{
				Enclosing_Instance.segmentInfos.Write(Enclosing_Instance.directory); // commit before deleting
				return null;
			}
		}
		private class AnonymousClassWith4 : Lock.With
		{
            private void  InitBlock(System.String mergedName, IndexWriter enclosingInstance)
            {
				this.mergedName = mergedName;
				this.enclosingInstance = enclosingInstance;
			}
			private System.String mergedName;
			private IndexWriter enclosingInstance;
			public IndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
            internal AnonymousClassWith4(System.String mergedName, IndexWriter enclosingInstance, Lucene.Net.Store.Lock Param1, long Param2) : base(Param1, Param2)
            {
                InitBlock(mergedName, enclosingInstance);
            }
			public override System.Object DoBody()
			{
				// make compound file visible for SegmentReaders
				Enclosing_Instance.directory.RenameFile(mergedName + ".tmp", mergedName + ".cfs");
				return null;
			}
		}
		private void  InitBlock()
		{
			similarity = Similarity.GetDefault();
		}
		
        /// <summary> Default value for the write lock timeout (1,000).</summary>
        public const long WRITE_LOCK_TIMEOUT = 1000;
		
        private long writeLockTimeout = WRITE_LOCK_TIMEOUT;
		
        /// <summary> Default value for the commit lock timeout (10,000).</summary>
        public const long COMMIT_LOCK_TIMEOUT = 10000;
		
        private long commitLockTimeout = COMMIT_LOCK_TIMEOUT;
		
        public const System.String WRITE_LOCK_NAME = "write.lock";
		public const System.String COMMIT_LOCK_NAME = "commit.lock";
		
		/// <summary> Default value is 10. Change using {@link #SetMergeFactor(int)}.</summary>
		public const int DEFAULT_MERGE_FACTOR = 10;
		
		/// <summary> Default value is 10. Change using {@link #SetMaxBufferedDocs(int)}.</summary>
		public const int DEFAULT_MAX_BUFFERED_DOCS = 10;
		
		/// <summary> Default value is {@link Integer#MAX_VALUE}. Change using {@link #SetMaxMergeDocs(int)}.</summary>
		public static readonly int DEFAULT_MAX_MERGE_DOCS = System.Int32.MaxValue;
		
		/// <summary> Default value is 10,000. Change using {@link #SetMaxFieldLength(int)}.</summary>
		public const int DEFAULT_MAX_FIELD_LENGTH = 10000;
		
		/// <summary> Default value is 128. Change using {@link #SetTermIndexInterval(int)}.</summary>
		public const int DEFAULT_TERM_INDEX_INTERVAL = 128;
		
		private Directory directory; // where this index resides
		private Analyzer analyzer; // how to analyze text
		
		private Similarity similarity; // how to normalize
		
		private SegmentInfos segmentInfos = new SegmentInfos(); // the segments
		private Directory ramDirectory = new RAMDirectory(); // for temp segs
		
		private Lock writeLock;
		
		private int termIndexInterval = DEFAULT_TERM_INDEX_INTERVAL;
		
		/// <summary>Use compound file setting. Defaults to true, minimizing the number of
		/// files used.  Setting this to false may improve indexing performance, but
		/// may also cause file handle problems.
		/// </summary>
		private bool useCompoundFile = true;
		
		private bool closeDir;
		
		/// <summary>Get the current setting of whether to use the compound file format.
		/// Note that this just returns the value you set with setUseCompoundFile(boolean)
		/// or the default. You cannot use this to query the status of an existing index.
		/// </summary>
		/// <seealso cref="SetUseCompoundFile(boolean)">
		/// </seealso>
		public virtual bool GetUseCompoundFile()
		{
			return useCompoundFile;
		}
		
		/// <summary>Setting to turn on usage of a compound file. When on, multiple files
		/// for each segment are merged into a single file once the segment creation
		/// is finished. This is done regardless of what directory is in use.
		/// </summary>
		public virtual void  SetUseCompoundFile(bool value_Renamed)
		{
			useCompoundFile = value_Renamed;
		}
		
		/// <summary>Expert: Set the Similarity implementation used by this IndexWriter.
		/// 
		/// </summary>
		/// <seealso cref="Similarity.SetDefault(Similarity)">
		/// </seealso>
		public virtual void  SetSimilarity(Similarity similarity)
		{
			this.similarity = similarity;
		}
		
		/// <summary>Expert: Return the Similarity implementation used by this IndexWriter.
		/// 
		/// <p>This defaults to the current value of {@link Similarity#GetDefault()}.
		/// </summary>
		public virtual Similarity GetSimilarity()
		{
			return this.similarity;
		}
		
		/// <summary>Expert: Set the interval between indexed terms.  Large values cause less
		/// memory to be used by IndexReader, but slow random-access to terms.  Small
		/// values cause more memory to be used by an IndexReader, and speed
		/// random-access to terms.
		/// 
		/// This parameter determines the amount of computation required per query
		/// term, regardless of the number of documents that contain that term.  In
		/// particular, it is the maximum number of other terms that must be
		/// scanned before a term is located and its frequency and position information
		/// may be processed.  In a large index with user-entered query terms, query
		/// processing time is likely to be dominated not by term lookup but rather
		/// by the processing of frequency and positional data.  In a small index
		/// or when many uncommon query terms are generated (e.g., by wildcard
		/// queries) term lookup may become a dominant cost.
		/// 
		/// In particular, <code>numUniqueTerms/interval</code> terms are read into
		/// memory by an IndexReader, and, on average, <code>interval/2</code> terms
		/// must be scanned for each random term access.
		/// 
		/// </summary>
		/// <seealso cref="DEFAULT_TERM_INDEX_INTERVAL">
		/// </seealso>
		public virtual void  SetTermIndexInterval(int interval)
		{
			this.termIndexInterval = interval;
		}
		
		/// <summary>Expert: Return the interval between indexed terms.
		/// 
		/// </summary>
		/// <seealso cref="SetTermIndexInterval(int)">
		/// </seealso>
		public virtual int GetTermIndexInterval()
		{
			return termIndexInterval;
		}
		
		/// <summary> Constructs an IndexWriter for the index in <code>path</code>.
		/// Text will be analyzed with <code>a</code>.  If <code>create</code>
		/// is true, then a new, empty index will be created in
		/// <code>path</code>, replacing the index already there, if any.
		/// 
		/// </summary>
		/// <param name="path">the path to the index directory
		/// </param>
		/// <param name="a">the analyzer to use
		/// </param>
		/// <param name="create"><code>true</code> to create the index or overwrite
		/// the existing one; <code>false</code> to append to the existing
		/// index
		/// </param>
		/// <throws>  IOException if the directory cannot be read/written to, or </throws>
		/// <summary>  if it does not exist, and <code>create</code> is
		/// <code>false</code>
		/// </summary>
		public IndexWriter(System.String path, Analyzer a, bool create) : this(FSDirectory.GetDirectory(path, create), a, create, true)
		{
		}
		
		/// <summary> Constructs an IndexWriter for the index in <code>path</code>.
		/// Text will be analyzed with <code>a</code>.  If <code>create</code>
		/// is true, then a new, empty index will be created in
		/// <code>path</code>, replacing the index already there, if any.
		/// 
		/// </summary>
		/// <param name="path">the path to the index directory
		/// </param>
		/// <param name="a">the analyzer to use
		/// </param>
		/// <param name="create"><code>true</code> to create the index or overwrite
		/// the existing one; <code>false</code> to append to the existing
		/// index
		/// </param>
		/// <throws>  IOException if the directory cannot be read/written to, or </throws>
		/// <summary>  if it does not exist, and <code>create</code> is
		/// <code>false</code>
		/// </summary>
		public IndexWriter(System.IO.FileInfo path, Analyzer a, bool create) : this(FSDirectory.GetDirectory(path, create), a, create, true)
		{
		}
		
		/// <summary> Constructs an IndexWriter for the index in <code>d</code>.
		/// Text will be analyzed with <code>a</code>.  If <code>create</code>
		/// is true, then a new, empty index will be created in
		/// <code>d</code>, replacing the index already there, if any.
		/// 
		/// </summary>
		/// <param name="d">the index directory
		/// </param>
		/// <param name="a">the analyzer to use
		/// </param>
		/// <param name="create"><code>true</code> to create the index or overwrite
		/// the existing one; <code>false</code> to append to the existing
		/// index
		/// </param>
		/// <throws>  IOException if the directory cannot be read/written to, or </throws>
		/// <summary>  if it does not exist, and <code>create</code> is
		/// <code>false</code>
		/// </summary>
		public IndexWriter(Directory d, Analyzer a, bool create) : this(d, a, create, false)
		{
		}
		
		private IndexWriter(Directory d, Analyzer a, bool create, bool closeDir)
		{
			InitBlock();
			this.closeDir = closeDir;
			directory = d;
			analyzer = a;
			
			Lock writeLock = directory.MakeLock(IndexWriter.WRITE_LOCK_NAME);
			if (!writeLock.Obtain(writeLockTimeout))
			// obtain write lock
			{
				throw new System.IO.IOException("Index locked for write: " + writeLock);
			}
			this.writeLock = writeLock; // save it
			
			lock (directory)
			{
				// in- & inter-process sync
				new AnonymousClassWith(create, this, directory.MakeLock(IndexWriter.COMMIT_LOCK_NAME), commitLockTimeout).Run();
			}
		}
		
		/// <summary>Determines the largest number of documents ever merged by addDocument().
		/// Small values (e.g., less than 10,000) are best for interactive indexing,
		/// as this limits the length of pauses while indexing to a few seconds.
		/// Larger values are best for batched indexing and speedier searches.
		/// 
		/// <p>The default value is {@link Integer#MAX_VALUE}.
		/// </summary>
		public virtual void  SetMaxMergeDocs(int maxMergeDocs)
		{
			this.maxMergeDocs = maxMergeDocs;
		}
		
		/// <seealso cref="setMaxMergeDocs">
		/// </seealso>
		public virtual int GetMaxMergeDocs()
		{
			return maxMergeDocs;
		}
		
		/// <summary> The maximum number of terms that will be indexed for a single field in a
		/// document.  This limits the amount of memory required for indexing, so that
		/// collections with very large files will not crash the indexing process by
		/// running out of memory.<p/>
		/// Note that this effectively truncates large documents, excluding from the
		/// index terms that occur further in the document.  If you know your source
		/// documents are large, be sure to set this value high enough to accomodate
		/// the expected size.  If you set it to Integer.MAX_VALUE, then the only limit
		/// is your memory, but you should anticipate an OutOfMemoryError.<p/>
		/// By default, no more than 10,000 terms will be indexed for a field.
		/// </summary>
		public virtual void  SetMaxFieldLength(int maxFieldLength)
		{
			this.maxFieldLength = maxFieldLength;
		}
		
		/// <seealso cref="setMaxFieldLength">
		/// </seealso>
		public virtual int GetMaxFieldLength()
		{
			return maxFieldLength;
		}
		
		/// <summary>Determines the minimal number of documents required before the buffered
		/// in-memory documents are merging and a new Segment is created.
		/// Since Documents are merged in a {@link Lucene.Net.store.RAMDirectory},
		/// large value gives faster indexing.  At the same time, mergeFactor limits
		/// the number of files open in a FSDirectory.
		/// 
		/// <p> The default value is 10.
		/// 
		/// </summary>
		/// <throws>  IllegalArgumentException if maxBufferedDocs is smaller than 1  </throws>
		public virtual void  SetMaxBufferedDocs(int maxBufferedDocs)
		{
			if (maxBufferedDocs < 2)
				throw new System.ArgumentException("maxBufferedDocs must at least be 2");
			this.minMergeDocs = maxBufferedDocs;
		}
		
		/// <seealso cref="setMaxBufferedDocs">
		/// </seealso>
		public virtual int GetMaxBufferedDocs()
		{
			return minMergeDocs;
		}
		
		/// <summary>Determines how often segment indices are merged by addDocument().  With
		/// smaller values, less RAM is used while indexing, and searches on
		/// unoptimized indices are faster, but indexing speed is slower.  With larger
		/// values, more RAM is used during indexing, and while searches on unoptimized
		/// indices are slower, indexing is faster.  Thus larger values (> 10) are best
		/// for batch index creation, and smaller values (< 10) for indices that are
		/// interactively maintained.
		/// 
		/// <p>This must never be less than 2.  The default value is 10.
		/// </summary>
		public virtual void  SetMergeFactor(int mergeFactor)
		{
			if (mergeFactor < 2)
				throw new System.ArgumentException("mergeFactor cannot be less than 2");
			this.mergeFactor = mergeFactor;
		}
		
		/// <seealso cref="setMergeFactor">
		/// </seealso>
		public virtual int GetMergeFactor()
		{
			return mergeFactor;
		}
		
		/// <summary>If non-null, information about merges and a message when
		/// maxFieldLength is reached will be printed to this.
		/// </summary>
		public virtual void  SetInfoStream(System.IO.TextWriter infoStream)
		{
			this.infoStream = infoStream;
		}
		
		/// <seealso cref="setInfoStream">
		/// </seealso>
		public virtual System.IO.TextWriter GetInfoStream()
		{
			return infoStream;
		}
        
        /// <summary> Sets the maximum time to wait for a commit lock (in milliseconds).</summary>
        public virtual void SetCommitLockTimeout(long commitLockTimeout)
        {
            this.commitLockTimeout = commitLockTimeout;
        }
        
        /// <seealso cref="setCommitLockTimeout">
        /// </seealso>
        public virtual long GetCommitLockTimeout()
        {
            return commitLockTimeout;
        }
        
        /// <summary> Sets the maximum time to wait for a write lock (in milliseconds).</summary>
        public virtual void SetWriteLockTimeout(long writeLockTimeout)
        {
            this.writeLockTimeout = writeLockTimeout;
        }
		
        /// <seealso cref="#setWriteLockTimeout">
        /// </seealso>
        public virtual long GetWriteLockTimeout()
        {
            return writeLockTimeout;
        }
		
        /// <summary>Flushes all changes to an index and closes all associated files. </summary>
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
                System.GC.SuppressFinalize(this);
            }
		}
		
		/// <summary>Release the write lock, if needed. </summary>
		~IndexWriter()
		{
			if (writeLock != null)
			{
				writeLock.Release(); // release write lock
				writeLock = null;
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
				int count = 0;
				for (int i = 0; i < segmentInfos.Count; i++)
				{
					SegmentInfo si = segmentInfos.Info(i);
					count += si.docCount;
				}
				return count;
			}
		}
		
		/// <summary> The maximum number of terms that will be indexed for a single field in a
		/// document.  This limits the amount of memory required for indexing, so that
		/// collections with very large files will not crash the indexing process by
		/// running out of memory.<p/>
		/// Note that this effectively truncates large documents, excluding from the
		/// index terms that occur further in the document.  If you know your source
		/// documents are large, be sure to set this value high enough to accomodate
		/// the expected size.  If you set it to Integer.MAX_VALUE, then the only limit
		/// is your memory, but you should anticipate an OutOfMemoryError.<p/>
		/// By default, no more than 10,000 terms will be indexed for a field.
		/// 
		/// </summary>
		private int maxFieldLength = DEFAULT_MAX_FIELD_LENGTH;
		
		/// <summary> Adds a document to this index.  If the document contains more than
		/// {@link #SetMaxFieldLength(int)} terms for a given field, the remainder are
		/// discarded.
		/// </summary>
		public virtual void  AddDocument(Document doc)
		{
			AddDocument(doc, analyzer);
		}
		
		/// <summary> Adds a document to this index, using the provided analyzer instead of the
		/// value of {@link #GetAnalyzer()}.  If the document contains more than
		/// {@link #SetMaxFieldLength(int)} terms for a given field, the remainder are
		/// discarded.
		/// </summary>
		public virtual void  AddDocument(Document doc, Analyzer analyzer)
		{
			DocumentWriter dw = new DocumentWriter(ramDirectory, analyzer, this);
			dw.SetInfoStream(infoStream);
			System.String segmentName = NewSegmentName();
			dw.AddDocument(segmentName, doc);
			lock (this)
			{
				segmentInfos.Add(new SegmentInfo(segmentName, 1, ramDirectory));
				MaybeMergeSegments();
			}
		}
		
		internal int GetSegmentsCounter()
		{
			return segmentInfos.counter;
		}
		
		private System.String NewSegmentName()
		{
			lock (this)
			{
                return "_" + SupportClass.Number.ToString(segmentInfos.counter++, SupportClass.Number.MAX_RADIX);
			}
		}
		
		/// <summary>Determines how often segment indices are merged by addDocument().  With
		/// smaller values, less RAM is used while indexing, and searches on
		/// unoptimized indices are faster, but indexing speed is slower.  With larger
		/// values, more RAM is used during indexing, and while searches on unoptimized
		/// indices are slower, indexing is faster.  Thus larger values (> 10) are best
		/// for batch index creation, and smaller values (< 10) for indices that are
		/// interactively maintained.
		/// 
        /// <p>This must never be less than 2.  The default value is {@link #DEFAULT_MERGE_FACTOR}.
        /// </summary>
        private int mergeFactor = DEFAULT_MERGE_FACTOR;
		
		/// <summary>Determines the minimal number of documents required before the buffered
		/// in-memory documents are merging and a new Segment is created.
		/// Since Documents are merged in a {@link Lucene.Net.store.RAMDirectory},
		/// large value gives faster indexing.  At the same time, mergeFactor limits
		/// the number of files open in a FSDirectory.
        /// 
        /// <p> The default value is {@link #DEFAULT_MAX_BUFFERED_DOCS}.
        /// </summary>
        private int minMergeDocs = DEFAULT_MAX_BUFFERED_DOCS;
		
		
		/// <summary>Determines the largest number of documents ever merged by addDocument().
		/// Small values (e.g., less than 10,000) are best for interactive indexing,
		/// as this limits the length of pauses while indexing to a few seconds.
		/// Larger values are best for batched indexing and speedier searches.
        /// 
        /// <p>The default value is {@link #DEFAULT_MAX_MERGE_DOCS}.
        /// </summary>
        private int maxMergeDocs = DEFAULT_MAX_MERGE_DOCS;
		
        /// <summary>If non-null, information about merges will be printed to this.</summary>
		private System.IO.TextWriter infoStream = null;
		
		/// <summary>Merges all segments together into a single segment, optimizing an index
		/// for search. 
		/// </summary>
		public virtual void  Optimize()
		{
			lock (this)
			{
				FlushRamSegments();
				while (segmentInfos.Count > 1 || (segmentInfos.Count == 1 && (SegmentReader.HasDeletions(segmentInfos.Info(0)) || segmentInfos.Info(0).dir != directory || (useCompoundFile && (!SegmentReader.UsesCompoundFile(segmentInfos.Info(0)) || SegmentReader.HasSeparateNorms(segmentInfos.Info(0)))))))
				{
					int minSegment = segmentInfos.Count - mergeFactor;
					MergeSegments(minSegment < 0?0:minSegment);
				}
			}
		}
		
		/// <summary>Merges all segments from an array of indexes into this index.
		/// 
		/// <p>This may be used to parallelize batch indexing.  A large document
		/// collection can be broken into sub-collections.  Each sub-collection can be
		/// indexed in parallel, on a different thread, process or machine.  The
		/// complete index can then be created by merging sub-collection indexes
		/// with this method.
		/// 
		/// <p>After this completes, the index is optimized. 
		/// </summary>
		public virtual void  AddIndexes(Directory[] dirs)
		{
			lock (this)
			{
				Optimize(); // start with zero or 1 seg
				
				int start = segmentInfos.Count;
				
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
							MergeSegments(base_Renamed, end);
					}
				}
				
				Optimize(); // final cleanup
			}
		}
		
		/// <summary>Merges the provided indexes into this index.
		/// <p>After this completes, the index is optimized. </p>
		/// <p>The provided IndexReaders are not closed.</p>
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
					segmentsToDelete.Add(sReader); // queue segment for deletion
				}
				
				for (int i = 0; i < readers.Length; i++)
    				// add new indexes
					merger.Add(readers[i]);
				
				int docCount = merger.Merge(); // merge 'em
				
				segmentInfos.RemoveRange(0, segmentInfos.Count);  // pop old infos & add new
				segmentInfos.Add(new SegmentInfo(mergedName, docCount, directory));
				
				if (sReader != null)
					sReader.Close();
				
				lock (directory)
				{
					// in- & inter-process sync
                    new AnonymousClassWith1(this, directory.MakeLock(COMMIT_LOCK_NAME), commitLockTimeout).Run();
                }
				
                DeleteSegments(segmentsToDelete); // delete now-unused segments
				
                if (useCompoundFile)
				{
					System.Collections.ArrayList filesToDelete = merger.CreateCompoundFile(mergedName + ".tmp");
					lock (directory)
					{
						// in- & inter-process sync
                        new AnonymousClassWith2(mergedName, this, directory.MakeLock(COMMIT_LOCK_NAME), commitLockTimeout).Run();
                    }
					
                    // delete now unused files of segment 
                    DeleteFiles(filesToDelete);
                }
			}
		}
		
		/// <summary>Merges all RAM-resident segments. </summary>
		private void  FlushRamSegments()
		{
			int minSegment = segmentInfos.Count - 1;
			int docCount = 0;
			while (minSegment >= 0 && (segmentInfos.Info(minSegment)).dir == ramDirectory)
			{
				docCount += segmentInfos.Info(minSegment).docCount;
				minSegment--;
			}
			if (minSegment < 0 || (docCount + segmentInfos.Info(minSegment).docCount) > mergeFactor || !(segmentInfos.Info(segmentInfos.Count - 1).dir == ramDirectory))
				minSegment++;
			if (minSegment >= segmentInfos.Count)
				return ; // none to merge
			MergeSegments(minSegment);
		}
		
		/// <summary>Incremental segment merger.  </summary>
		private void  MaybeMergeSegments()
		{
			long targetMergeDocs = minMergeDocs;
			while (targetMergeDocs <= maxMergeDocs)
			{
				// find segments smaller than current target size
				int minSegment = segmentInfos.Count;
				int mergeDocs = 0;
				while (--minSegment >= 0)
				{
					SegmentInfo si = segmentInfos.Info(minSegment);
					if (si.docCount >= targetMergeDocs)
						break;
					mergeDocs += si.docCount;
				}
				
				if (mergeDocs >= targetMergeDocs)
				// found a merge to do
					MergeSegments(minSegment + 1);
				else
					break;
				
				targetMergeDocs *= mergeFactor; // increase target size
			}
		}
		
		/// <summary>Pops segments off of segmentInfos stack down to minSegment, merges them,
		/// and pushes the merged index onto the top of the segmentInfos stack. 
		/// </summary>
		private void  MergeSegments(int minSegment)
		{
			MergeSegments(minSegment, segmentInfos.Count);
		}
		
		/// <summary>Merges the named range of segments, replacing them in the stack with a
		/// single segment. 
		/// </summary>
		private void  MergeSegments(int minSegment, int end)
		{
			System.String mergedName = NewSegmentName();
			if (infoStream != null)
				infoStream.Write("merging segments");
			SegmentMerger merger = new SegmentMerger(this, mergedName);
			
			System.Collections.ArrayList segmentsToDelete = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			for (int i = minSegment; i < end; i++)
			{
				SegmentInfo si = segmentInfos.Info(i);
				if (infoStream != null)
					infoStream.Write(" " + si.name + " (" + si.docCount + " docs)");
				IndexReader reader = SegmentReader.Get(si);
				merger.Add(reader);
				if ((reader.Directory() == this.directory) || (reader.Directory() == this.ramDirectory))
					segmentsToDelete.Add(reader); // queue segment for deletion
			}
			
			int mergedDocCount = merger.Merge();
			
			if (infoStream != null)
			{
				infoStream.WriteLine(" into " + mergedName + " (" + mergedDocCount + " docs)");
			}
			
			for (int i = end - 1; i > minSegment; i--)
    			// remove old infos & add new
				segmentInfos.RemoveAt(i);
			segmentInfos[minSegment] = new SegmentInfo(mergedName, mergedDocCount, directory);
			
			// close readers before we attempt to delete now-obsolete segments
			merger.CloseReaders();
			
			lock (directory)
			{
				// in- & inter-process sync
                new AnonymousClassWith3(this, directory.MakeLock(COMMIT_LOCK_NAME), commitLockTimeout).Run();
            }
			
            DeleteSegments(segmentsToDelete); // delete now-unused segments
			
            if (useCompoundFile)
			{
				System.Collections.ArrayList filesToDelete = merger.CreateCompoundFile(mergedName + ".tmp");
				lock (directory)
				{
					// in- & inter-process sync
                    new AnonymousClassWith4(mergedName, this, directory.MakeLock(COMMIT_LOCK_NAME), commitLockTimeout).Run();
                }
				
                // delete now unused files of segment 
                DeleteFiles(filesToDelete);
            }
		}
		
		/*
		* Some operating systems (e.g. Windows) don't permit a file to be deleted
		* while it is opened for read (e.g. by another process or thread). So we
		* assume that when a delete fails it is because the file is open in another
		* process, and queue the file for subsequent deletion.
		*/
		
		private void  DeleteSegments(System.Collections.ArrayList segments)
		{
			System.Collections.ArrayList deletable = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			
			DeleteFiles(ReadDeleteableFiles(), deletable); // try to delete deleteable
			
			for (int i = 0; i < segments.Count; i++)
			{
				SegmentReader reader = (SegmentReader) segments[i];
				if (reader.Directory() == this.directory)
					DeleteFiles(reader.Files(), deletable);
				// try to delete our files
				else
					DeleteFiles(reader.Files(), reader.Directory()); // delete other files
			}
			
			WriteDeleteableFiles(deletable); // note files we can't delete
		}
		
		private void  DeleteFiles(System.Collections.ArrayList files)
		{
			System.Collections.ArrayList deletable = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			DeleteFiles(ReadDeleteableFiles(), deletable); // try to delete deleteable
			DeleteFiles(files, deletable); // try to delete our files
			WriteDeleteableFiles(deletable); // note files we can't delete
		}
		
		private void  DeleteFiles(System.Collections.ArrayList files, Directory directory)
		{
			for (int i = 0; i < files.Count; i++)
				directory.DeleteFile((System.String) files[i]);
		}
		
		private void  DeleteFiles(System.Collections.ArrayList files, System.Collections.ArrayList deletable)
		{
			for (int i = 0; i < files.Count; i++)
			{
				System.String file = (System.String) files[i];
				try
				{
					directory.DeleteFile(file); // try to delete each file
				}
				catch (System.IO.IOException e)
				{
					// if delete fails
					if (directory.FileExists(file))
					{
						if (infoStream != null)
						{
							infoStream.WriteLine(e.ToString() + "; Will re-try later.");
						}
						deletable.Add(file); // add to deletable
					}
				}
			}
		}
		
		private System.Collections.ArrayList ReadDeleteableFiles()
		{
			System.Collections.ArrayList result = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			if (!directory.FileExists(IndexFileNames.DELETABLE))
				return result;
			
			IndexInput input = directory.OpenInput(IndexFileNames.DELETABLE);
			try
			{
				for (int i = input.ReadInt(); i > 0; i--)
				// read file names
					result.Add(input.ReadString());
			}
			finally
			{
				input.Close();
			}
			return result;
		}
		
		private void  WriteDeleteableFiles(System.Collections.ArrayList files)
		{
			IndexOutput output = directory.CreateOutput("deleteable.new");
			try
			{
				output.WriteInt(files.Count);
				for (int i = 0; i < files.Count; i++)
					output.WriteString((System.String) files[i]);
			}
			finally
			{
				output.Close();
			}
			directory.RenameFile("deleteable.new", IndexFileNames.DELETABLE);
		}
	}
}