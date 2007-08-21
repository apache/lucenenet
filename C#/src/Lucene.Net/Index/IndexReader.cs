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
using Similarity = Lucene.Net.Search.Similarity;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using IndexInput = Lucene.Net.Store.IndexInput;
using Lock = Lucene.Net.Store.Lock;

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
	/// </summary>
	/// <author>  Doug Cutting
	/// </author>
	/// <version>  $Id: IndexReader.java 497612 2007-01-18 22:47:03Z mikemccand $
	/// </version>
	public abstract class IndexReader
	{
		private class AnonymousClassFindSegmentsFile : SegmentInfos.FindSegmentsFile
		{
			private void  InitBlock(bool closeDirectory)
			{
				this.closeDirectory = closeDirectory;
			}
			private bool closeDirectory;
			internal AnonymousClassFindSegmentsFile(bool closeDirectory, Lucene.Net.Store.Directory Param1) : base(Param1)
			{
				InitBlock(closeDirectory);
			}
			
			public override System.Object DoBody(System.String segmentFileName)
			{
				
				SegmentInfos infos = new SegmentInfos();
				infos.Read(directory, segmentFileName);
				
				if (infos.Count == 1)
				{
					// index is optimized
					return SegmentReader.Get(infos, infos.Info(0), closeDirectory);
				}
				else
				{
					
					// To reduce the chance of hitting FileNotFound
					// (and having to retry), we open segments in
					// reverse because IndexWriter merges & deletes
					// the newest segments first.
					
					IndexReader[] readers = new IndexReader[infos.Count];
					for (int i = infos.Count - 1; i >= 0; i--)
					{
						try
						{
							readers[i] = SegmentReader.Get(infos.Info(i));
						}
						catch (System.IO.IOException e)
						{
							// Close all readers we had opened:
							for (i++; i < infos.Count; i++)
							{
								readers[i].Close();
							}
							throw e;
						}
					}
					
					return new MultiReader(directory, infos, closeDirectory, readers);
				}
			}
		}
		private class AnonymousClassFindSegmentsFile1 : SegmentInfos.FindSegmentsFile
		{
			internal AnonymousClassFindSegmentsFile1(System.IO.FileInfo Param1):base(Param1)
			{
			}
			public override System.Object DoBody(System.String segmentFileName)
			{
				return (long) FSDirectory.FileModified(fileDirectory, segmentFileName);
			}
		}
		private class AnonymousClassFindSegmentsFile2 : SegmentInfos.FindSegmentsFile
		{
			private void  InitBlock(Lucene.Net.Store.Directory directory2)
			{
				this.directory2 = directory2;
			}
			private Lucene.Net.Store.Directory directory2;
			internal AnonymousClassFindSegmentsFile2(Lucene.Net.Store.Directory directory2, Lucene.Net.Store.Directory Param1):base(Param1)
			{
				InitBlock(directory2);
			}
			public override System.Object DoBody(System.String segmentFileName)
			{
				return (long) directory2.FileModified(segmentFileName);
			}
		}
		
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
			// all fields
			public static readonly FieldOption ALL = new FieldOption("ALL");
			// all indexed fields
			public static readonly FieldOption INDEXED = new FieldOption("INDEXED");
			// all fields which are not indexed
			public static readonly FieldOption UNINDEXED = new FieldOption("UNINDEXED");
			// all fields which are indexed with termvectors enables
			public static readonly FieldOption INDEXED_WITH_TERMVECTOR = new FieldOption("INDEXED_WITH_TERMVECTOR");
			// all fields which are indexed but don't have termvectors enabled
			public static readonly FieldOption INDEXED_NO_TERMVECTOR = new FieldOption("INDEXED_NO_TERMVECTOR");
			// all fields where termvectors are enabled. Please note that only standard termvector fields are returned
			public static readonly FieldOption TERMVECTOR = new FieldOption("TERMVECTOR");
			// all field with termvectors wiht positions enabled
			public static readonly FieldOption TERMVECTOR_WITH_POSITION = new FieldOption("TERMVECTOR_WITH_POSITION");
			// all fields where termvectors with offset position are set
			public static readonly FieldOption TERMVECTOR_WITH_OFFSET = new FieldOption("TERMVECTOR_WITH_OFFSET");
			// all fields where termvectors with offset and position values set
			public static readonly FieldOption TERMVECTOR_WITH_POSITION_OFFSET = new FieldOption("TERMVECTOR_WITH_POSITION_OFFSET");
		}
		
		/// <summary> Constructor used if IndexReader is not owner of its directory. 
		/// This is used for IndexReaders that are used within other IndexReaders that take care or locking directories.
		/// 
		/// </summary>
		/// <param name="directory">Directory where IndexReader files reside.
		/// </param>
		protected internal IndexReader(Directory directory)
		{
			this.directory = directory;
		}
		
		/// <summary> Constructor used if IndexReader is owner of its directory.
		/// If IndexReader is owner of its directory, it locks its directory in case of write operations.
		/// 
		/// </summary>
		/// <param name="directory">Directory where IndexReader files reside.
		/// </param>
		/// <param name="segmentInfos">Used for write-l
		/// </param>
		/// <param name="">closeDirectory
		/// </param>
		internal IndexReader(Directory directory, SegmentInfos segmentInfos, bool closeDirectory)
		{
			Init(directory, segmentInfos, closeDirectory, true);
		}
		
		internal virtual void  Init(Directory directory, SegmentInfos segmentInfos, bool closeDirectory, bool directoryOwner)
		{
			this.directory = directory;
			this.segmentInfos = segmentInfos;
			this.directoryOwner = directoryOwner;
			this.closeDirectory = closeDirectory;
		}
		
		private Directory directory;
		private bool directoryOwner;
		private bool closeDirectory;
		protected internal IndexFileDeleter deleter;
		
		private SegmentInfos segmentInfos;
		private Lock writeLock;
		private bool stale;
		private bool hasChanges;
		
		/// <summary>Used by commit() to record pre-commit state in case
		/// rollback is necessary 
		/// </summary>
		private bool rollbackHasChanges;
		private SegmentInfos rollbackSegmentInfos;
		
		/// <summary>Returns an IndexReader reading the index in an FSDirectory in the named
		/// path. 
		/// </summary>
		public static IndexReader Open(System.String path)
		{
			return Open(FSDirectory.GetDirectory(path), true);
		}
		
		/// <summary>Returns an IndexReader reading the index in an FSDirectory in the named
		/// path. 
		/// </summary>
		public static IndexReader Open(System.IO.FileInfo path)
		{
			return Open(FSDirectory.GetDirectory(path), true);
		}
		
		/// <summary>Returns an IndexReader reading the index in the given Directory. </summary>
		public static IndexReader Open(Directory directory)
		{
			return Open(directory, false);
		}
		
		private static IndexReader Open(Directory directory, bool closeDirectory)
		{
			
			return (IndexReader) new AnonymousClassFindSegmentsFile(closeDirectory, directory).run();
		}
		
		/// <summary>Returns the directory this index resides in. </summary>
		public virtual Directory Directory()
		{
			return directory;
		}
		
		/// <summary> Returns the time the index in the named directory was last modified.
		/// Do not use this to check whether the reader is still up-to-date, use
		/// {@link #IsCurrent()} instead. 
		/// </summary>
		public static long LastModified(System.String directory)
		{
			return LastModified(new System.IO.FileInfo(directory));
		}
		
		/// <summary> Returns the time the index in the named directory was last modified. 
		/// Do not use this to check whether the reader is still up-to-date, use
		/// {@link #IsCurrent()} instead. 
		/// </summary>
		public static long LastModified(System.IO.FileInfo fileDirectory)
		{
			return (long) ((System.Int64) new AnonymousClassFindSegmentsFile1(fileDirectory).run());
		}
		
		/// <summary> Returns the time the index in the named directory was last modified. 
		/// Do not use this to check whether the reader is still up-to-date, use
		/// {@link #IsCurrent()} instead. 
		/// </summary>
		public static long LastModified(Directory directory2)
		{
			return (long) ((System.Int64) new AnonymousClassFindSegmentsFile2(directory2, directory2).run());
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
		/// <throws>  IOException if segments file cannot be read </throws>
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
		/// <throws>  IOException if segments file cannot be read </throws>
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
		/// <throws>  IOException if segments file cannot be read. </throws>
		public static long GetCurrentVersion(Directory directory)
		{
			return SegmentInfos.ReadCurrentVersion(directory);
		}
		
		/// <summary> Version number when this IndexReader was opened.</summary>
		public virtual long GetVersion()
		{
			return segmentInfos.GetVersion();
		}
		
		/// <summary> Check whether this IndexReader still works on a current version of the index.
		/// If this is not the case you will need to re-open the IndexReader to
		/// make sure you see the latest changes made to the index.
		/// 
		/// </summary>
		/// <throws>  IOException </throws>
		public virtual bool IsCurrent()
		{
			return SegmentInfos.ReadCurrentVersion(directory) == segmentInfos.GetVersion();
		}
		
		/// <summary> Checks is the index is optimized (if it has a single segment and no deletions)</summary>
		/// <returns> <code>true</code> if the index is optimized; <code>false</code> otherwise
		/// </returns>
		public virtual bool IsOptimized()
		{
			return segmentInfos.Count == 1 && HasDeletions() == false;
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
                return SegmentInfos.GetCurrentSegmentGeneration(System.IO.Directory.GetFileSystemEntries(directory.FullName)) != - 1;
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
		public virtual Document Document(int n)
		{
			return Document(n, null);
		}
		
		/// <summary> Get the {@link Lucene.Net.Documents.Document} at the <code>n</code><sup>th</sup> position. The {@link Lucene.Net.Documents.FieldSelector}
		/// may be used to determine what {@link Lucene.Net.Documents.Field}s to load and how they should be loaded.
		/// 
		/// <b>NOTE:</b> If this Reader (more specifically, the underlying {@link FieldsReader} is closed before the lazy {@link Lucene.Net.Documents.Field} is
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
		/// <throws>  IOException If there is a problem reading this document </throws>
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
			return Norms(field) != null;
		}
		
		/// <summary>Returns the byte-encoded normalization factor for the named field of
		/// every document.  This is used by the search code to score documents.
		/// 
		/// </summary>
		/// <seealso cref="Lucene.Net.Documents.Field#SetBoost(float)">
		/// </seealso>
		public abstract byte[] Norms(System.String field);
		
		/// <summary>Reads the byte-encoded normalization factor for the named field of every
		/// document.  This is used by the search code to score documents.
		/// 
		/// </summary>
		/// <seealso cref="Lucene.Net.Documents.Field#SetBoost(float)">
		/// </seealso>
		public abstract void  Norms(System.String field, byte[] bytes, int offset);
		
		/// <summary>Expert: Resets the normalization factor for the named field of the named
		/// document.  The norm represents the product of the field's {@link
		/// Fieldable#SetBoost(float) boost} and its {@link Similarity#LengthNorm(String,
		/// int) length normalization}.  Thus, to preserve the length normalization
		/// values when resetting this, one should base the new value upon the old.
		/// 
		/// </summary>
		/// <seealso cref="#Norms(String)">
		/// </seealso>
		/// <seealso cref="Similarity#DecodeNorm(byte)">
		/// </seealso>
		public void  SetNorm(int doc, System.String field, byte value_Renamed)
		{
			lock (this)
			{
				if (directoryOwner)
					AquireWriteLock();
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
		/// <seealso cref="#Norms(String)">
		/// </seealso>
		/// <seealso cref="Similarity#DecodeNorm(byte)">
		/// </seealso>
		public virtual void  SetNorm(int doc, System.String field, float value_Renamed)
		{
			SetNorm(doc, field, Similarity.EncodeNorm(value_Renamed));
		}
		
		/// <summary>Returns an enumeration of all the terms in the index.
		/// The enumeration is ordered by Term.compareTo().  Each term
		/// is greater than all that precede it in the enumeration.
		/// </summary>
		public abstract TermEnum Terms();
		
		/// <summary>Returns an enumeration of all terms after a given term.
		/// The enumeration is ordered by Term.compareTo().  Each term
		/// is greater than all that precede it in the enumeration.
		/// </summary>
		public abstract TermEnum Terms(Term t);
		
		/// <summary>Returns the number of documents containing the term <code>t</code>. </summary>
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
		public virtual TermDocs TermDocs(Term term)
		{
			TermDocs termDocs = TermDocs();
			termDocs.Seek(term);
			return termDocs;
		}
		
		/// <summary>Returns an unpositioned {@link TermDocs} enumerator. </summary>
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
		/// <p> This positional information faciliates phrase and proximity searching.
		/// <p>The enumeration is ordered by document number.  Each document number is
		/// greater than all that precede it in the enumeration.
		/// </summary>
		public virtual TermPositions TermPositions(Term term)
		{
			TermPositions termPositions = TermPositions();
			termPositions.Seek(term);
			return termPositions;
		}
		
		/// <summary>Returns an unpositioned {@link TermPositions} enumerator. </summary>
		public abstract TermPositions TermPositions();
		
		/// <summary> Tries to acquire the WriteLock on this directory.
		/// this method is only valid if this IndexReader is directory owner.
		/// 
		/// </summary>
		/// <throws>  IOException If WriteLock cannot be acquired. </throws>
		private void  AquireWriteLock()
		{
			if (stale)
				throw new System.IO.IOException("IndexReader out of date and no longer valid for delete, undelete, or setNorm operations");
			
			if (this.writeLock == null)
			{
				Lock writeLock = directory.MakeLock(IndexWriter.WRITE_LOCK_NAME);
				if (!writeLock.Obtain(IndexWriter.WRITE_LOCK_TIMEOUT))
				// obtain write lock
				{
					throw new System.IO.IOException("Index locked for write: " + writeLock);
				}
				this.writeLock = writeLock;
				
				// we have to check whether index has changed since this reader was opened.
				// if so, this reader is no longer valid for deletion
				if (SegmentInfos.ReadCurrentVersion(directory) > segmentInfos.GetVersion())
				{
					stale = true;
					this.writeLock.Release();
					this.writeLock = null;
					throw new System.IO.IOException("IndexReader out of date and no longer valid for delete, undelete, or setNorm operations");
				}
			}
		}
		
		
		/// <summary>Deletes the document numbered <code>docNum</code>.  Once a document is
		/// deleted it will not appear in TermDocs or TermPostitions enumerations.
		/// Attempts to read its field with the {@link #document}
		/// method will result in an error.  The presence of this document may still be
		/// reflected in the {@link #docFreq} statistic, though
		/// this will be corrected eventually as the index is further modified.
		/// </summary>
		public void  DeleteDocument(int docNum)
		{
			lock (this)
			{
				if (directoryOwner)
					AquireWriteLock();
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
		/// </summary>
		/// <returns> the number of documents deleted
		/// </returns>
		public int DeleteDocuments(Term term)
		{
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
		
		/// <summary>Undeletes all documents currently marked as deleted in this index.</summary>
		public void  UndeleteAll()
		{
			lock (this)
			{
				if (directoryOwner)
					AquireWriteLock();
				hasChanges = true;
				DoUndeleteAll();
			}
		}
		
		/// <summary>Implements actual undeleteAll() in subclass. </summary>
		protected internal abstract void  DoUndeleteAll();
		
		/// <summary> Should internally checkpoint state that will change
		/// during commit so that we can rollback if necessary.
		/// </summary>
		internal virtual void  StartCommit()
		{
			if (directoryOwner)
			{
				rollbackSegmentInfos = (SegmentInfos) segmentInfos.Clone();
			}
			rollbackHasChanges = hasChanges;
		}
		
		/// <summary> Rolls back state to just before the commit (this is
		/// called by commit() if there is some exception while
		/// committing).
		/// </summary>
		internal virtual void  RollbackCommit()
		{
			if (directoryOwner)
			{
				for (int i = 0; i < segmentInfos.Count; i++)
				{
					// Rollback each segmentInfo.  Because the
					// SegmentReader holds a reference to the
					// SegmentInfo we can't [easily] just replace
					// segmentInfos, so we reset it in place instead:
					segmentInfos.Info(i).Reset(rollbackSegmentInfos.Info(i));
				}
				rollbackSegmentInfos = null;
			}
			
			hasChanges = rollbackHasChanges;
		}
		
		/// <summary> Commit changes resulting from delete, undeleteAll, or
		/// setNorm operations
		/// 
		/// If an exception is hit, then either no changes or all
		/// changes will have been committed to the index
		/// (transactional semantics).
		/// 
		/// </summary>
		/// <throws>  IOException </throws>
		public void  Commit()
		{
			lock (this)
			{
				if (hasChanges)
				{
					if (deleter == null)
					{
						// In the MultiReader case, we share this deleter
						// across all SegmentReaders:
						SetDeleter(new IndexFileDeleter(segmentInfos, directory));
					}
					if (directoryOwner)
					{
						
						// Should not be necessary: no prior commit should
						// have left pending files, so just defensive:
						deleter.ClearPendingFiles();
						
						System.String oldInfoFileName = segmentInfos.GetCurrentSegmentFileName();
						System.String nextSegmentsFileName = segmentInfos.GetNextSegmentFileName();
						
						// Checkpoint the state we are about to change, in
						// case we have to roll back:
						StartCommit();
						
						bool success = false;
						try
						{
							DoCommit();
							segmentInfos.Write(directory);
							success = true;
						}
						finally
						{
							
							if (!success)
							{
								
								// Rollback changes that were made to
								// SegmentInfos but failed to get [fully]
								// committed.  This way this reader instance
								// remains consistent (matched to what's
								// actually in the index):
								RollbackCommit();
								
								// Erase any pending files that we were going to delete:
								deleter.ClearPendingFiles();
								
								// Remove possibly partially written next
								// segments file:
								deleter.DeleteFile(nextSegmentsFileName);
								
								// Recompute deletable files & remove them (so
								// partially written .del files, etc, are
								// removed):
								deleter.FindDeletableFiles();
								deleter.DeleteFiles();
							}
						}
						
						// Attempt to delete all files we just obsoleted:
						deleter.DeleteFile(oldInfoFileName);
						deleter.CommitPendingFiles();
						
						if (writeLock != null)
						{
							writeLock.Release(); // release write lock
							writeLock = null;
						}
					}
					else
						DoCommit();
				}
				hasChanges = false;
			}
		}
		
		protected internal virtual void  SetDeleter(IndexFileDeleter deleter)
		{
			this.deleter = deleter;
		}
		protected internal virtual IndexFileDeleter GetDeleter()
		{
			return deleter;
		}
		
		/// <summary>Implements commit. </summary>
		protected internal abstract void  DoCommit();
		
		/// <summary> Closes files associated with this index.
		/// Also saves any new deletions to disk.
		/// No other methods should be called after this has been called.
		/// </summary>
		public void  Close()
		{
			lock (this)
			{
				Commit();
				DoClose();
				if (closeDirectory)
					directory.Close();
			}
		}
		
		/// <summary>Implements close. </summary>
		protected internal abstract void  DoClose();
		
		/// <summary>Release the write lock, if needed. </summary>
		~IndexReader()
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
		/// <throws>  IOException if there is a problem with accessing the index </throws>
		public static bool IsLocked(Directory directory)
		{
			return directory.MakeLock(IndexWriter.WRITE_LOCK_NAME).IsLocked();
		}
		
		/// <summary> Returns <code>true</code> iff the index in the named directory is
		/// currently locked.
		/// </summary>
		/// <param name="directory">the directory to check for a lock
		/// </param>
		/// <throws>  IOException if there is a problem with accessing the index </throws>
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

							byte[] byteArray = new byte[buffer.Length];
							for (int index=0; index < buffer.Length; index++)
								byteArray[index] = (byte) buffer[index];

							f.Write(byteArray, 0, bufLen);

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