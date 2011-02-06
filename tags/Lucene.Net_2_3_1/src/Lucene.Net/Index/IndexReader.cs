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

using Document = Lucene.Net.Documents.Document;
using FieldSelector = Lucene.Net.Documents.FieldSelector;
using Lucene.Net.Store;
using Similarity = Lucene.Net.Search.Similarity;

namespace Lucene.Net.Index
{
	
	/// <summary>IndexReader is an abstract class, providing an interface for accessing an
	/// index.  Search of an index is done entirely through this abstract interface,
	/// so that any subclass which implements it is searchable.
	/// <p> Concrete subclasses of IndexReader are usually constructed with a call to
	/// one of the static <code>open()</code> methods, e.g. {@link #Open(String)}.
	/// <p> For efficiency, in this API documents are often referred to via
	/// <i>document numbers</i>, non-negative integers which each name a unique
	/// document in the index.  These document numbers are ephemeral--they may change
	/// as documents are added to and deleted from an index.  Clients should thus not
	/// rely on a given document having the same number between sessions.
	/// <p> An IndexReader can be opened on a directory for which an IndexWriter is
	/// opened already, but it cannot be used to delete documents from the index then.
	/// <p>
	/// NOTE: for backwards API compatibility, several methods are not listed 
	/// as abstract, but have no useful implementations in this base class and 
	/// instead always throw UnsupportedOperationException.  Subclasses are 
	/// strongly encouraged to override these methods, but in many cases may not 
	/// need to.
	/// </p>
	/// </summary>
	/// <version>  $Id: IndexReader.java 598462 2007-11-26 23:31:39Z dnaber $
	/// </version>
	public abstract class IndexReader
	{
		private class AnonymousClassFindSegmentsFile:SegmentInfos.FindSegmentsFile
		{
			internal AnonymousClassFindSegmentsFile(System.IO.FileInfo Param1):base(Param1)
			{
			}
			protected internal override System.Object DoBody(System.String segmentFileName)
			{
				return (long) FSDirectory.FileModified(fileDirectory, segmentFileName);
			}
		}
		private class AnonymousClassFindSegmentsFile1:SegmentInfos.FindSegmentsFile
		{
			private void  InitBlock(Lucene.Net.Store.Directory directory2)
			{
				this.directory2 = directory2;
			}
			private Lucene.Net.Store.Directory directory2;
			internal AnonymousClassFindSegmentsFile1(Lucene.Net.Store.Directory directory2, Lucene.Net.Store.Directory Param1):base(Param1)
			{
				InitBlock(directory2);
			}
			protected internal override System.Object DoBody(System.String segmentFileName)
			{
				return (long) directory2.FileModified(segmentFileName);
			}
		}
		
		/// <summary> Constants describing field properties, for example used for
		/// {@link IndexReader#GetFieldNames(FieldOption)}.
		/// </summary>
		public sealed class FieldOption
		{
			private System.String option;
			internal FieldOption()
			{
			}
			internal FieldOption(System.String option)
			{
				this.option = option;
			}
			public override System.String ToString()
			{
				return this.option;
			}
			/// <summary>All fields </summary>
			public static readonly FieldOption ALL = new FieldOption("ALL");
			/// <summary>All indexed fields </summary>
			public static readonly FieldOption INDEXED = new FieldOption("INDEXED");
			/// <summary>All fields that store payloads </summary>
			public static readonly FieldOption STORES_PAYLOADS = new FieldOption("STORES_PAYLOADS");
			/// <summary>All fields which are not indexed </summary>
			public static readonly FieldOption UNINDEXED = new FieldOption("UNINDEXED");
			/// <summary>All fields which are indexed with termvectors enabled </summary>
			public static readonly FieldOption INDEXED_WITH_TERMVECTOR = new FieldOption("INDEXED_WITH_TERMVECTOR");
			/// <summary>All fields which are indexed but don't have termvectors enabled </summary>
			public static readonly FieldOption INDEXED_NO_TERMVECTOR = new FieldOption("INDEXED_NO_TERMVECTOR");
			/// <summary>All fields with termvectors enabled. Please note that only standard termvector fields are returned </summary>
			public static readonly FieldOption TERMVECTOR = new FieldOption("TERMVECTOR");
			/// <summary>All fields with termvectors with position values enabled </summary>
			public static readonly FieldOption TERMVECTOR_WITH_POSITION = new FieldOption("TERMVECTOR_WITH_POSITION");
			/// <summary>All fields with termvectors with offset values enabled </summary>
			public static readonly FieldOption TERMVECTOR_WITH_OFFSET = new FieldOption("TERMVECTOR_WITH_OFFSET");
			/// <summary>All fields with termvectors with offset values and position values enabled </summary>
			public static readonly FieldOption TERMVECTOR_WITH_POSITION_OFFSET = new FieldOption("TERMVECTOR_WITH_POSITION_OFFSET");
		}
		
		private bool closed;
		protected internal bool hasChanges;
		
		private volatile int refCount;
		
		// for testing
		public /*internal*/ virtual int GetRefCount()
		{
			lock (this)
			{
				return refCount;
			}
		}
		
		/// <summary> Increments the refCount of this IndexReader instance. RefCounts are used to determine
		/// when a reader can be closed safely, i. e. as soon as no other IndexReader is referencing
		/// it anymore.
		/// </summary>
		protected internal virtual void  IncRef()
		{
			lock (this)
			{
				System.Diagnostics.Debug.Assert(refCount > 0);
				refCount++;
			}
		}
		
		/// <summary> Decreases the refCount of this IndexReader instance. If the refCount drops
		/// to 0, then pending changes are committed to the index and this reader is closed.
		/// 
		/// </summary>
		/// <throws>  IOException in case an IOException occurs in commit() or doClose() </throws>
		protected internal virtual void  DecRef()
		{
			lock (this)
			{
				System.Diagnostics.Debug.Assert(refCount > 0);
				if (refCount == 1)
				{
					Commit();
					DoClose();
				}
				refCount--;
			}
		}
		
		/// <deprecated> will be deleted when IndexReader(Directory) is deleted
		/// </deprecated>
		/// <seealso cref="Directory()">
		/// </seealso>
		private Directory directory;
		
		/// <summary> Legacy Constructor for backwards compatibility.
		/// 
		/// <p>
		/// This Constructor should not be used, it exists for backwards 
		/// compatibility only to support legacy subclasses that did not "own" 
		/// a specific directory, but needed to specify something to be returned 
		/// by the directory() method.  Future subclasses should delegate to the 
		/// no arg constructor and implement the directory() method as appropriate.
		/// 
		/// </summary>
		/// <param name="directory">Directory to be returned by the directory() method
		/// </param>
		/// <seealso cref="Directory()">
		/// </seealso>
		/// <deprecated> - use IndexReader()
		/// </deprecated>
		protected internal IndexReader(Directory directory):this()
		{
			this.directory = directory;
		}
		
		protected internal IndexReader()
		{
			refCount = 1;
		}
		
		/// <throws>  AlreadyClosedException if this IndexReader is closed </throws>
		public /*protected internal*/ void  EnsureOpen()
		{
			if (refCount <= 0)
			{
				throw new AlreadyClosedException("this IndexReader is closed");
			}
		}
		
		/// <summary>Returns an IndexReader reading the index in an FSDirectory in the named
		/// path.
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		/// <param name="path">the path to the index directory 
		/// </param>
		public static IndexReader Open(System.String path)
		{
			return Open(FSDirectory.GetDirectory(path), true, null);
		}
		
		/// <summary>Returns an IndexReader reading the index in an FSDirectory in the named
		/// path.
		/// </summary>
		/// <param name="path">the path to the index directory
		/// </param>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static IndexReader Open(System.IO.FileInfo path)
		{
			return Open(FSDirectory.GetDirectory(path), true, null);
		}
		
		/// <summary>Returns an IndexReader reading the index in the given Directory.</summary>
		/// <param name="directory">the index directory
		/// </param>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static IndexReader Open(Directory directory)
		{
			return Open(directory, false, null);
		}
		
		/// <summary>Expert: returns an IndexReader reading the index in the given
		/// Directory, with a custom {@link IndexDeletionPolicy}.
		/// </summary>
		/// <param name="directory">the index directory
		/// </param>
		/// <param name="deletionPolicy">a custom deletion policy (only used
		/// if you use this reader to perform deletes or to set
		/// norms); see {@link IndexWriter} for details.
		/// </param>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static IndexReader Open(Directory directory, IndexDeletionPolicy deletionPolicy)
		{
			return Open(directory, false, deletionPolicy);
		}
		
		private static IndexReader Open(Directory directory, bool closeDirectory, IndexDeletionPolicy deletionPolicy)
		{
			return DirectoryIndexReader.Open(directory, closeDirectory, deletionPolicy);
		}
		
		/// <summary> Refreshes an IndexReader if the index has changed since this instance 
		/// was (re)opened. 
		/// <p>
		/// Opening an IndexReader is an expensive operation. This method can be used
		/// to refresh an existing IndexReader to reduce these costs. This method 
		/// tries to only load segments that have changed or were created after the 
		/// IndexReader was (re)opened.
		/// <p>
		/// If the index has not changed since this instance was (re)opened, then this
		/// call is a NOOP and returns this instance. Otherwise, a new instance is 
		/// returned. The old instance is <b>not</b> closed and remains usable.<br>
		/// <b>Note:</b> The re-opened reader instance and the old instance might share
		/// the same resources. For this reason no index modification operations 
		/// (e. g. {@link #DeleteDocument(int)}, {@link #SetNorm(int, String, byte)}) 
		/// should be performed using one of the readers until the old reader instance
		/// is closed. <b>Otherwise, the behavior of the readers is undefined.</b> 
		/// <p>   
		/// You can determine whether a reader was actually reopened by comparing the
		/// old instance with the instance returned by this method: 
		/// <pre>
		/// IndexReader reader = ... 
		/// ...
		/// IndexReader new = r.reopen();
		/// if (new != reader) {
		/// ...     // reader was reopened
		/// reader.close(); 
		/// }
		/// reader = new;
		/// ...
		/// </pre>
		/// 
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public virtual IndexReader Reopen()
		{
			lock (this)
			{
				throw new System.NotSupportedException("This reader does not support reopen().");
			}
		}
		
		/// <summary> Returns the directory associated with this index.  The Default 
		/// implementation returns the directory specified by subclasses when 
		/// delegating to the IndexReader(Directory) constructor, or throws an 
		/// UnsupportedOperationException if one was not specified.
		/// </summary>
		/// <throws>  UnsupportedOperationException if no directory </throws>
		public virtual Directory Directory()
		{
			EnsureOpen();
			if (null != directory)
			{
				return directory;
			}
			else
			{
				throw new System.NotSupportedException("This reader does not support this method.");
			}
		}
		
		/// <summary> Returns the time the index in the named directory was last modified.
		/// Do not use this to check whether the reader is still up-to-date, use
		/// {@link #IsCurrent()} instead. 
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static long LastModified(System.String directory)
		{
			return LastModified(new System.IO.FileInfo(directory));
		}
		
		/// <summary> Returns the time the index in the named directory was last modified. 
		/// Do not use this to check whether the reader is still up-to-date, use
		/// {@link #IsCurrent()} instead. 
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static long LastModified(System.IO.FileInfo fileDirectory)
		{
			return (long) ((System.Int64) new AnonymousClassFindSegmentsFile(fileDirectory).Run());
		}
		
		/// <summary> Returns the time the index in the named directory was last modified. 
		/// Do not use this to check whether the reader is still up-to-date, use
		/// {@link #IsCurrent()} instead. 
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static long LastModified(Directory directory2)
		{
			return (long) ((System.Int64) new AnonymousClassFindSegmentsFile1(directory2, directory2).Run());
		}
		
		/// <summary> Reads version number from segments files. The version number is
		/// initialized with a timestamp and then increased by one for each change of
		/// the index.
		/// 
		/// </summary>
		/// <param name="directory">where the index resides.
		/// </param>
		/// <returns> version number.
		/// </returns>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static long GetCurrentVersion(System.String directory)
		{
			return GetCurrentVersion(new System.IO.FileInfo(directory));
		}
		
		/// <summary> Reads version number from segments files. The version number is
		/// initialized with a timestamp and then increased by one for each change of
		/// the index.
		/// 
		/// </summary>
		/// <param name="directory">where the index resides.
		/// </param>
		/// <returns> version number.
		/// </returns>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static long GetCurrentVersion(System.IO.FileInfo directory)
		{
			Directory dir = FSDirectory.GetDirectory(directory);
			long version = GetCurrentVersion(dir);
			dir.Close();
			return version;
		}
		
		/// <summary> Reads version number from segments files. The version number is
		/// initialized with a timestamp and then increased by one for each change of
		/// the index.
		/// 
		/// </summary>
		/// <param name="directory">where the index resides.
		/// </param>
		/// <returns> version number.
		/// </returns>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static long GetCurrentVersion(Directory directory)
		{
			return SegmentInfos.ReadCurrentVersion(directory);
		}
		
		/// <summary> Version number when this IndexReader was opened. Not implemented in the IndexReader base class.</summary>
		/// <throws>  UnsupportedOperationException unless overridden in subclass </throws>
		public virtual long GetVersion()
		{
			throw new System.NotSupportedException("This reader does not support this method.");
		}
		
		/// <summary><p>For IndexReader implementations that use
		/// TermInfosReader to read terms, this sets the
		/// indexDivisor to subsample the number of indexed terms
		/// loaded into memory.  This has the same effect as {@link
		/// IndexWriter#setTermIndexInterval} except that setting
		/// must be done at indexing time while this setting can be
		/// set per reader.  When set to N, then one in every
		/// N*termIndexInterval terms in the index is loaded into
		/// memory.  By setting this to a value > 1 you can reduce
		/// memory usage, at the expense of higher latency when
		/// loading a TermInfo.  The default value is 1.</p>
		/// 
		/// <b>NOTE:</b> you must call this before the term
		/// index is loaded.  If the index is already loaded, 
		/// an IllegalStateException is thrown.
		/// </summary>
		/// <throws>  IllegalStateException if the term index has already been loaded into memory </throws>
		public virtual void  SetTermInfosIndexDivisor(int indexDivisor)
		{
			throw new System.NotSupportedException("This reader does not support this method.");
		}
		
		/// <summary><p>For IndexReader implementations that use
		/// TermInfosReader to read terms, this returns the
		/// current indexDivisor.
		/// </summary>
		/// <seealso cref="setTermInfosIndexDivisor">
		/// </seealso>
		public virtual int GetTermInfosIndexDivisor()
		{
			throw new System.NotSupportedException("This reader does not support this method.");
		}
		
		/// <summary> Check whether this IndexReader is still using the
		/// current (i.e., most recently committed) version of the
		/// index.  If a writer has committed any changes to the
		/// index since this reader was opened, this will return
		/// <code>false</code>, in which case you must open a new
		/// IndexReader in order to see the changes.  See the
		/// description of the <a href="IndexWriter.html#autoCommit"><code>autoCommit</code></a>
		/// flag which controls when the {@link IndexWriter}
		/// actually commits changes to the index.
		/// 
		/// <p>
		/// Not implemented in the IndexReader base class.
		/// </p>
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		/// <throws>  UnsupportedOperationException unless overridden in subclass </throws>
		public virtual bool IsCurrent()
		{
			throw new System.NotSupportedException("This reader does not support this method.");
		}
		
		/// <summary> Checks is the index is optimized (if it has a single segment and 
		/// no deletions).  Not implemented in the IndexReader base class.
		/// </summary>
		/// <returns> <code>true</code> if the index is optimized; <code>false</code> otherwise
		/// </returns>
		/// <throws>  UnsupportedOperationException unless overridden in subclass </throws>
		public virtual bool IsOptimized()
		{
			throw new System.NotSupportedException("This reader does not support this method.");
		}
		
		/// <summary>  Return an array of term frequency vectors for the specified document.
		/// The array contains a vector for each vectorized field in the document.
		/// Each vector contains terms and frequencies for all terms in a given vectorized field.
		/// If no such fields existed, the method returns null. The term vectors that are
		/// returned my either be of type TermFreqVector or of type TermPositionsVector if
		/// positions or offsets have been stored.
		/// 
		/// </summary>
		/// <param name="docNumber">document for which term frequency vectors are returned
		/// </param>
		/// <returns> array of term frequency vectors. May be null if no term vectors have been
		/// stored for the specified document.
		/// </returns>
		/// <throws>  IOException if index cannot be accessed </throws>
		/// <seealso cref="Lucene.Net.Documents.Field.TermVector">
		/// </seealso>
		abstract public TermFreqVector[] GetTermFreqVectors(int docNumber);
		
		
		/// <summary>  Return a term frequency vector for the specified document and field. The
		/// returned vector contains terms and frequencies for the terms in
		/// the specified field of this document, if the field had the storeTermVector
		/// flag set. If termvectors had been stored with positions or offsets, a 
		/// TermPositionsVector is returned.
		/// 
		/// </summary>
		/// <param name="docNumber">document for which the term frequency vector is returned
		/// </param>
		/// <param name="field">field for which the term frequency vector is returned.
		/// </param>
		/// <returns> term frequency vector May be null if field does not exist in the specified
		/// document or term vector was not stored.
		/// </returns>
		/// <throws>  IOException if index cannot be accessed </throws>
		/// <seealso cref="Lucene.Net.Documents.Field.TermVector">
		/// </seealso>
		abstract public TermFreqVector GetTermFreqVector(int docNumber, System.String field);
		
		/// <summary> Load the Term Vector into a user-defined data structure instead of relying on the parallel arrays of
		/// the {@link TermFreqVector}.
		/// </summary>
		/// <param name="docNumber">The number of the document to load the vector for
		/// </param>
		/// <param name="field">The name of the field to load
		/// </param>
		/// <param name="mapper">The {@link TermVectorMapper} to process the vector.  Must not be null
		/// </param>
		/// <throws>  IOException if term vectors cannot be accessed or if they do not exist on the field and doc. specified. </throws>
		/// <summary> 
		/// </summary>
		abstract public void  GetTermFreqVector(int docNumber, System.String field, TermVectorMapper mapper);
		
		/// <summary> Map all the term vectors for all fields in a Document</summary>
		/// <param name="docNumber">The number of the document to load the vector for
		/// </param>
		/// <param name="mapper">The {@link TermVectorMapper} to process the vector.  Must not be null
		/// </param>
		/// <throws>  IOException if term vectors cannot be accessed or if they do not exist on the field and doc. specified. </throws>
		abstract public void  GetTermFreqVector(int docNumber, TermVectorMapper mapper);
		
		/// <summary> Returns <code>true</code> if an index exists at the specified directory.
		/// If the directory does not exist or if there is no index in it.
		/// <code>false</code> is returned.
		/// </summary>
		/// <param name="directory">the directory to check for an index
		/// </param>
		/// <returns> <code>true</code> if an index exists; <code>false</code> otherwise
		/// </returns>
		public static bool IndexExists(System.String directory)
		{
			return IndexExists(new System.IO.FileInfo(directory));
		}
		
		/// <summary> Returns <code>true</code> if an index exists at the specified directory.
		/// If the directory does not exist or if there is no index in it.
		/// </summary>
		/// <param name="directory">the directory to check for an index
		/// </param>
		/// <returns> <code>true</code> if an index exists; <code>false</code> otherwise
		/// </returns>
		
		public static bool IndexExists(System.IO.FileInfo directory)
		{
			if (System.IO.Directory.Exists(directory.FullName))
			{
				return SegmentInfos.GetCurrentSegmentGeneration(System.IO.Directory.GetFileSystemEntries(directory.FullName)) != -1;
			}
			else
			{
				return false;
			}
		}
		
		/// <summary> Returns <code>true</code> if an index exists at the specified directory.
		/// If the directory does not exist or if there is no index in it.
		/// </summary>
		/// <param name="directory">the directory to check for an index
		/// </param>
		/// <returns> <code>true</code> if an index exists; <code>false</code> otherwise
		/// </returns>
		/// <throws>  IOException if there is a problem with accessing the index </throws>
		public static bool IndexExists(Directory directory)
		{
			return SegmentInfos.GetCurrentSegmentGeneration(directory) != - 1;
		}
		
		/// <summary>Returns the number of documents in this index. </summary>
		public abstract int NumDocs();
		
		/// <summary>Returns one greater than the largest possible document number.
		/// This may be used to, e.g., determine how big to allocate an array which
		/// will have an element for every document number in an index.
		/// </summary>
		public abstract int MaxDoc();
		
		/// <summary>Returns the stored fields of the <code>n</code><sup>th</sup>
		/// <code>Document</code> in this index.
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public virtual Document Document(int n)
		{
			EnsureOpen();
			return Document(n, null);
		}
		
		/// <summary> Get the {@link Lucene.Net.Documents.Document} at the <code>n</code><sup>th</sup> position. The {@link Lucene.Net.Documents.FieldSelector}
		/// may be used to determine what {@link Lucene.Net.Documents.Field}s to load and how they should be loaded.
		/// 
		/// <b>NOTE:</b> If this Reader (more specifically, the underlying <code>FieldsReader</code>) is closed before the lazy {@link Lucene.Net.Documents.Field} is
		/// loaded an exception may be thrown.  If you want the value of a lazy {@link Lucene.Net.Documents.Field} to be available after closing you must
		/// explicitly load it or fetch the Document again with a new loader.
		/// 
		/// 
		/// </summary>
		/// <param name="n">Get the document at the <code>n</code><sup>th</sup> position
		/// </param>
		/// <param name="fieldSelector">The {@link Lucene.Net.Documents.FieldSelector} to use to determine what Fields should be loaded on the Document.  May be null, in which case all Fields will be loaded.
		/// </param>
		/// <returns> The stored fields of the {@link Lucene.Net.Documents.Document} at the nth position
		/// </returns>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		/// <summary> 
		/// </summary>
		/// <seealso cref="Lucene.Net.Documents.Fieldable">
		/// </seealso>
		/// <seealso cref="Lucene.Net.Documents.FieldSelector">
		/// </seealso>
		/// <seealso cref="Lucene.Net.Documents.SetBasedFieldSelector">
		/// </seealso>
		/// <seealso cref="Lucene.Net.Documents.LoadFirstFieldSelector">
		/// </seealso>
		//When we convert to JDK 1.5 make this Set<String>
		public abstract Document Document(int n, FieldSelector fieldSelector);
		
		
		
		/// <summary>Returns true if document <i>n</i> has been deleted </summary>
		public abstract bool IsDeleted(int n);
		
		/// <summary>Returns true if any documents have been deleted </summary>
		public abstract bool HasDeletions();
		
		/// <summary>Returns true if there are norms stored for this field. </summary>
		public virtual bool HasNorms(System.String field)
		{
			// backward compatible implementation.
			// SegmentReader has an efficient implementation.
			EnsureOpen();
			return Norms(field) != null;
		}
		
		/// <summary>Returns the byte-encoded normalization factor for the named field of
		/// every document.  This is used by the search code to score documents.
		/// 
		/// </summary>
		/// <seealso cref="Lucene.Net.Documents.Field.SetBoost(float)">
		/// </seealso>
		public abstract byte[] Norms(System.String field);
		
		/// <summary>Reads the byte-encoded normalization factor for the named field of every
		/// document.  This is used by the search code to score documents.
		/// 
		/// </summary>
		/// <seealso cref="Lucene.Net.Documents.Field.SetBoost(float)">
		/// </seealso>
		public abstract void  Norms(System.String field, byte[] bytes, int offset);
		
		/// <summary>Expert: Resets the normalization factor for the named field of the named
		/// document.  The norm represents the product of the field's {@link
		/// Lucene.Net.Documents.Fieldable#SetBoost(float) boost} and its {@link Similarity#LengthNorm(String,
		/// int) length normalization}.  Thus, to preserve the length normalization
		/// values when resetting this, one should base the new value upon the old.
		/// 
		/// </summary>
		/// <seealso cref="Norms(String)">
		/// </seealso>
		/// <seealso cref="Similarity.DecodeNorm(byte)">
		/// </seealso>
		/// <throws>  StaleReaderException if the index has changed </throws>
		/// <summary>  since this reader was opened
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  LockObtainFailedException if another writer </throws>
		/// <summary>  has this index open (<code>write.lock</code> could not
		/// be obtained)
		/// </summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public void  SetNorm(int doc, System.String field, byte value_Renamed)
		{
			lock (this)
			{
				EnsureOpen();
				AcquireWriteLock();
				hasChanges = true;
				DoSetNorm(doc, field, value_Renamed);
			}
		}
		
		/// <summary>Implements setNorm in subclass.</summary>
		protected internal abstract void  DoSetNorm(int doc, System.String field, byte value_Renamed);
		
		/// <summary>Expert: Resets the normalization factor for the named field of the named
		/// document.
		/// 
		/// </summary>
		/// <seealso cref="Norms(String)">
		/// </seealso>
		/// <seealso cref="Similarity.DecodeNorm(byte)">
		/// 
		/// </seealso>
		/// <throws>  StaleReaderException if the index has changed </throws>
		/// <summary>  since this reader was opened
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  LockObtainFailedException if another writer </throws>
		/// <summary>  has this index open (<code>write.lock</code> could not
		/// be obtained)
		/// </summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public virtual void  SetNorm(int doc, System.String field, float value_Renamed)
		{
			EnsureOpen();
			SetNorm(doc, field, Similarity.EncodeNorm(value_Renamed));
		}
		
		/// <summary>Returns an enumeration of all the terms in the index. The
		/// enumeration is ordered by Term.compareTo(). Each term is greater
		/// than all that precede it in the enumeration. Note that after
		/// calling terms(), {@link TermEnum#Next()} must be called
		/// on the resulting enumeration before calling other methods such as
		/// {@link TermEnum#Term()}.
		/// </summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public abstract TermEnum Terms();
		
		/// <summary>Returns an enumeration of all terms starting at a given term. If
		/// the given term does not exist, the enumeration is positioned at the
		/// first term greater than the supplied therm. The enumeration is
		/// ordered by Term.compareTo(). Each term is greater than all that
		/// precede it in the enumeration.
		/// </summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public abstract TermEnum Terms(Term t);
		
		/// <summary>Returns the number of documents containing the term <code>t</code>.</summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public abstract int DocFreq(Term t);
		
		/// <summary>Returns an enumeration of all the documents which contain
		/// <code>term</code>. For each document, the document number, the frequency of
		/// the term in that document is also provided, for use in search scoring.
		/// Thus, this method implements the mapping:
		/// <p><ul>
		/// Term &nbsp;&nbsp; =&gt; &nbsp;&nbsp; &lt;docNum, freq&gt;<sup>*</sup>
		/// </ul>
		/// <p>The enumeration is ordered by document number.  Each document number
		/// is greater than all that precede it in the enumeration.
		/// </summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public virtual TermDocs TermDocs(Term term)
		{
			EnsureOpen();
			TermDocs termDocs = TermDocs();
			termDocs.Seek(term);
			return termDocs;
		}
		
		/// <summary>Returns an unpositioned {@link TermDocs} enumerator.</summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public abstract TermDocs TermDocs();
		
		/// <summary>Returns an enumeration of all the documents which contain
		/// <code>term</code>.  For each document, in addition to the document number
		/// and frequency of the term in that document, a list of all of the ordinal
		/// positions of the term in the document is available.  Thus, this method
		/// implements the mapping:
		/// 
		/// <p><ul>
		/// Term &nbsp;&nbsp; =&gt; &nbsp;&nbsp; &lt;docNum, freq,
		/// &lt;pos<sub>1</sub>, pos<sub>2</sub>, ...
		/// pos<sub>freq-1</sub>&gt;
		/// &gt;<sup>*</sup>
		/// </ul>
		/// <p> This positional information facilitates phrase and proximity searching.
		/// <p>The enumeration is ordered by document number.  Each document number is
		/// greater than all that precede it in the enumeration.
		/// </summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public virtual TermPositions TermPositions(Term term)
		{
			EnsureOpen();
			TermPositions termPositions = TermPositions();
			termPositions.Seek(term);
			return termPositions;
		}
		
		/// <summary>Returns an unpositioned {@link TermPositions} enumerator.</summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public abstract TermPositions TermPositions();
		
		
		
		/// <summary>Deletes the document numbered <code>docNum</code>.  Once a document is
		/// deleted it will not appear in TermDocs or TermPostitions enumerations.
		/// Attempts to read its field with the {@link #document}
		/// method will result in an error.  The presence of this document may still be
		/// reflected in the {@link #docFreq} statistic, though
		/// this will be corrected eventually as the index is further modified.
		/// 
		/// </summary>
		/// <throws>  StaleReaderException if the index has changed </throws>
		/// <summary> since this reader was opened
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  LockObtainFailedException if another writer </throws>
		/// <summary>  has this index open (<code>write.lock</code> could not
		/// be obtained)
		/// </summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public void  DeleteDocument(int docNum)
		{
			lock (this)
			{
				EnsureOpen();
				AcquireWriteLock();
				hasChanges = true;
				DoDelete(docNum);
			}
		}
		
		
		/// <summary>Implements deletion of the document numbered <code>docNum</code>.
		/// Applications should call {@link #DeleteDocument(int)} or {@link #DeleteDocuments(Term)}.
		/// </summary>
		protected internal abstract void  DoDelete(int docNum);
		
		
		/// <summary>Deletes all documents that have a given <code>term</code> indexed.
		/// This is useful if one uses a document field to hold a unique ID string for
		/// the document.  Then to delete such a document, one merely constructs a
		/// term with the appropriate field and the unique ID string as its text and
		/// passes it to this method.
		/// See {@link #DeleteDocument(int)} for information about when this deletion will 
		/// become effective.
		/// 
		/// </summary>
		/// <returns> the number of documents deleted
		/// </returns>
		/// <throws>  StaleReaderException if the index has changed </throws>
		/// <summary>  since this reader was opened
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  LockObtainFailedException if another writer </throws>
		/// <summary>  has this index open (<code>write.lock</code> could not
		/// be obtained)
		/// </summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public int DeleteDocuments(Term term)
		{
			EnsureOpen();
			TermDocs docs = TermDocs(term);
			if (docs == null)
				return 0;
			int n = 0;
			try
			{
				while (docs.Next())
				{
					DeleteDocument(docs.Doc());
					n++;
				}
			}
			finally
			{
				docs.Close();
			}
			return n;
		}
		
		/// <summary>Undeletes all documents currently marked as deleted in this index.
		/// 
		/// </summary>
		/// <throws>  StaleReaderException if the index has changed </throws>
		/// <summary>  since this reader was opened
		/// </summary>
		/// <throws>  LockObtainFailedException if another writer </throws>
		/// <summary>  has this index open (<code>write.lock</code> could not
		/// be obtained)
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public void  UndeleteAll()
		{
			lock (this)
			{
				EnsureOpen();
				AcquireWriteLock();
				hasChanges = true;
				DoUndeleteAll();
			}
		}
		
		/// <summary>Implements actual undeleteAll() in subclass. </summary>
		protected internal abstract void  DoUndeleteAll();
		
		/// <summary>Does nothing by default. Subclasses that require a write lock for
		/// index modifications must implement this method. 
		/// </summary>
		protected internal virtual void  AcquireWriteLock()
		{
			lock (this)
			{
				/* NOOP */
			}
		}
		
		/// <summary> </summary>
		/// <throws>  IOException </throws>
		public void  Flush()
		{
			lock (this)
			{
				EnsureOpen();
				Commit();
			}
		}
		
		/// <summary> Commit changes resulting from delete, undeleteAll, or
		/// setNorm operations
		/// 
		/// If an exception is hit, then either no changes or all
		/// changes will have been committed to the index
		/// (transactional semantics).
		/// </summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public /*protected internal*/ void  Commit()
		{
			lock (this)
			{
				if (hasChanges)
				{
					DoCommit();
				}
				hasChanges = false;
			}
		}
		
		/// <summary>Implements commit. </summary>
		protected internal abstract void  DoCommit();
		
		/// <summary> Closes files associated with this index.
		/// Also saves any new deletions to disk.
		/// No other methods should be called after this has been called.
		/// </summary>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public void  Close()
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
		
		/// <summary>Implements close. </summary>
		protected internal abstract void  DoClose();
		
		
		/// <summary> Get a list of unique field names that exist in this index and have the specified
		/// field option information.
		/// </summary>
		/// <param name="fldOption">specifies which field option should be available for the returned fields
		/// </param>
		/// <returns> Collection of Strings indicating the names of the fields.
		/// </returns>
		/// <seealso cref="IndexReader.FieldOption">
		/// </seealso>
		public abstract System.Collections.ICollection GetFieldNames(FieldOption fldOption);
		
		/// <summary> Returns <code>true</code> iff the index in the named directory is
		/// currently locked.
		/// </summary>
		/// <param name="directory">the directory to check for a lock
		/// </param>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static bool IsLocked(Directory directory)
		{
			return directory.MakeLock(IndexWriter.WRITE_LOCK_NAME).IsLocked();
		}
		
		/// <summary> Returns <code>true</code> iff the index in the named directory is
		/// currently locked.
		/// </summary>
		/// <param name="directory">the directory to check for a lock
		/// </param>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static bool IsLocked(System.String directory)
		{
			Directory dir = FSDirectory.GetDirectory(directory);
			bool result = IsLocked(dir);
			dir.Close();
			return result;
		}
		
		/// <summary> Forcibly unlocks the index in the named directory.
		/// <P>
		/// Caution: this should only be used by failure recovery code,
		/// when it is known that no other process nor thread is in fact
		/// currently accessing this index.
		/// </summary>
		public static void  Unlock(Directory directory)
		{
			directory.MakeLock(IndexWriter.WRITE_LOCK_NAME).Release();
		}
		
		/// <summary> Prints the filename and size of each file within a given compound file.
		/// Add the -extract flag to extract files to the current working directory.
		/// In order to make the extracted version of the index work, you have to copy
		/// the segments file from the compound index into the directory where the extracted files are stored.
		/// </summary>
		/// <param name="args">Usage: Lucene.Net.Index.IndexReader [-extract] &lt;cfsfile&gt;
		/// </param>
		[STAThread]
		public static void  Main(System.String[] args)
		{
			System.String filename = null;
			bool extract = false;
			
			for (int i = 0; i < args.Length; ++i)
			{
				if (args[i].Equals("-extract"))
				{
					extract = true;
				}
				else if (filename == null)
				{
					filename = args[i];
				}
			}
			
			if (filename == null)
			{
				System.Console.Out.WriteLine("Usage: Lucene.Net.Index.IndexReader [-extract] <cfsfile>");
				return ;
			}
			
			Directory dir = null;
			CompoundFileReader cfr = null;
			
			try
			{
				System.IO.FileInfo file = new System.IO.FileInfo(filename);
				System.String dirname = new System.IO.FileInfo(file.FullName).DirectoryName;
				filename = file.Name;
				dir = FSDirectory.GetDirectory(dirname);
				cfr = new CompoundFileReader(dir, filename);
				
				System.String[] files = cfr.List();
				System.Array.Sort(files); // sort the array of filename so that the output is more readable
				
				for (int i = 0; i < files.Length; ++i)
				{
					long len = cfr.FileLength(files[i]);
					
					if (extract)
					{
						System.Console.Out.WriteLine("extract " + files[i] + " with " + len + " bytes to local directory...");
						IndexInput ii = cfr.OpenInput(files[i]);
						
						System.IO.FileStream f = new System.IO.FileStream(files[i], System.IO.FileMode.Create);
						
						// read and write with a small buffer, which is more effectiv than reading byte by byte
						byte[] buffer = new byte[1024];
						int chunk = buffer.Length;
						while (len > 0)
						{
							int bufLen = (int) System.Math.Min(chunk, len);
							ii.ReadBytes(buffer, 0, bufLen);
							f.Write(buffer, 0, bufLen);
							len -= bufLen;
						}
						
						f.Close();
						ii.Close();
					}
					else
						System.Console.Out.WriteLine(files[i] + ": " + len + " bytes");
				}
			}
			catch (System.IO.IOException ioe)
			{
				System.Console.Error.WriteLine(ioe.StackTrace);
			}
			finally
			{
				try
				{
					if (dir != null)
						dir.Close();
					if (cfr != null)
						cfr.Close();
				}
				catch (System.IO.IOException ioe)
				{
					System.Console.Error.WriteLine(ioe.StackTrace);
				}
			}
		}
	}
}