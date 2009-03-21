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
using Fieldable = Lucene.Net.Documents.Fieldable;
using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using Token = Lucene.Net.Analysis.Token;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using Similarity = Lucene.Net.Search.Similarity;

namespace Lucene.Net.Index
{
	
	/// <summary> This class accepts multiple added documents and directly
	/// writes a single segment file.  It does this more
	/// efficiently than creating a single segment per document
	/// (with DocumentWriter) and doing standard merges on those
	/// segments.
	/// 
	/// When a document is added, its stored fields (if any) and
	/// term vectors (if any) are immediately written to the
	/// Directory (ie these do not consume RAM).  The freq/prox
	/// postings are accumulated into a Postings hash table keyed
	/// by term.  Each entry in this hash table holds a separate
	/// byte stream (allocated as incrementally growing slices
	/// into large shared byte[] arrays) for freq and prox, that
	/// contains the postings data for multiple documents.  If
	/// vectors are enabled, each unique term for each document
	/// also allocates a PostingVector instance to similarly
	/// track the offsets & positions byte stream.
	/// 
	/// Once the Postings hash is full (ie is consuming the
	/// allowed RAM) or the number of added docs is large enough
	/// (in the case we are flushing by doc count instead of RAM
	/// usage), we create a real segment and flush it to disk and
	/// reset the Postings hash.
	/// 
	/// In adding a document we first organize all of its fields
	/// by field name.  We then process field by field, and
	/// record the Posting hash per-field.  After each field we
	/// flush its term vectors.  When it's time to flush the full
	/// segment we first sort the fields by name, and then go
	/// field by field and sorts its postings.
	/// 
	/// 
	/// Threads:
	/// 
	/// Multiple threads are allowed into addDocument at once.
	/// There is an initial synchronized call to getThreadState
	/// which allocates a ThreadState for this thread.  The same
	/// thread will get the same ThreadState over time (thread
	/// affinity) so that if there are consistent patterns (for
	/// example each thread is indexing a different content
	/// source) then we make better use of RAM.  Then
	/// processDocument is called on that ThreadState without
	/// synchronization (most of the "heavy lifting" is in this
	/// call).  Finally the synchronized "finishDocument" is
	/// called to flush changes to the directory.
	/// 
	/// Each ThreadState instance has its own Posting hash. Once
	/// we're using too much RAM, we flush all Posting hashes to
	/// a segment by merging the docIDs in the posting lists for
	/// the same term across multiple thread states (see
	/// writeSegment and appendPostings).
	/// 
	/// When flush is called by IndexWriter, or, we flush
	/// internally when autoCommit=false, we forcefully idle all
	/// threads and flush only once they are all idle.  This
	/// means you can call flush with a given thread even while
	/// other threads are actively adding/deleting documents.
	/// 
	/// 
	/// Exceptions:
	/// 
	/// Because this class directly updates in-memory posting
	/// lists, and flushes stored fields and term vectors
	/// directly to files in the directory, there are certain
	/// limited times when an exception can corrupt this state.
	/// For example, a disk full while flushing stored fields
	/// leaves this file in a corrupt state.  Or, an OOM
	/// exception while appending to the in-memory posting lists
	/// can corrupt that posting list.  We call such exceptions
	/// "aborting exceptions".  In these cases we must call
	/// abort() to discard all docs added since the last flush.
	/// 
	/// All other exceptions ("non-aborting exceptions") can
	/// still partially update the index structures.  These
	/// updates are consistent, but, they represent only a part
	/// of the document seen up until the exception was hit.
	/// When this happens, we immediately mark the document as
	/// deleted so that the document is always atomically ("all
	/// or none") added to the index.
	/// </summary>
	
	public sealed class DocumentsWriter
	{
		private void  InitBlock()
		{
			threadStates = new ThreadState[0];
			waitingThreadStates = new ThreadState[MAX_THREAD_STATE];
			maxBufferedDeleteTerms = IndexWriter.DEFAULT_MAX_BUFFERED_DELETE_TERMS;
			ramBufferSize = (long) (IndexWriter.DEFAULT_RAM_BUFFER_SIZE_MB * 1024 * 1024);  // {{Aroush-2.3.1}} should 'ramBufferSize'
			maxBufferedDocs = IndexWriter.DEFAULT_MAX_BUFFERED_DOCS;
			norms = new BufferedNorms[0];
		}
		
		private IndexWriter writer;
		private Directory directory;
		
		private FieldInfos fieldInfos = new FieldInfos(); // All fields we've seen
		private IndexOutput tvx, tvf, tvd; // To write term vectors
		private FieldsWriter fieldsWriter; // To write stored fields
		
		private System.String segment; // Current segment we are working on
		private System.String docStoreSegment; // Current doc-store segment we are writing
		private int docStoreOffset; // Current starting doc-store offset of current segment
		
		private int nextDocID; // Next docID to be added
		private int numDocsInRAM; // # docs buffered in RAM
		private int numDocsInStore; // # docs written to doc stores
		private int nextWriteDocID; // Next docID to be written
		
		// Max # ThreadState instances; if there are more threads
		// than this they share ThreadStates
		private const int MAX_THREAD_STATE = 5;
		private ThreadState[] threadStates;
		private System.Collections.Hashtable threadBindings = new System.Collections.Hashtable();
		private int numWaiting;
		private ThreadState[] waitingThreadStates;
		private int pauseThreads; // Non-zero when we need all threads to
		// pause (eg to flush)
		private bool flushPending; // True when a thread has decided to flush
		private bool bufferIsFull; // True when it's time to write segment
		private int abortCount; // Non-zero while abort is pending or running
		
		private System.IO.TextWriter infoStream;
		
		// This Hashmap buffers delete terms in ram before they
		// are applied.  The key is delete term; the value is
		// number of buffered documents the term applies to.
		private System.Collections.Hashtable bufferedDeleteTerms = new System.Collections.Hashtable();
		private int numBufferedDeleteTerms = 0;
		
		// Currently used only for deleting a doc on hitting an non-aborting exception
		private System.Collections.IList bufferedDeleteDocIDs = new System.Collections.ArrayList();
		
		// The max number of delete terms that can be buffered before
		// they must be flushed to disk.
		private int maxBufferedDeleteTerms;
		
		// How much RAM we can use before flushing.  This is 0 if
		// we are flushing by doc count instead.
		private long ramBufferSize;
		
		// Flush @ this number of docs.  If rarmBufferSize is
		// non-zero we will flush by RAM usage instead.
		private int maxBufferedDocs;
		
		private bool closed;
		
		// Coarse estimates used to measure RAM usage of buffered deletes
		private static int OBJECT_HEADER_BYTES = 8;
		private static int OBJECT_POINTER_BYTES = 4; // TODO: should be 8 on 64-bit platform
		private static int BYTES_PER_CHAR = 2;
		private static int BYTES_PER_INT = 4;
		
		private BufferedNorms[] norms; // Holds norms until we flush
		
		internal DocumentsWriter(Directory directory, IndexWriter writer)
		{
			InitBlock();
			this.directory = directory;
			this.writer = writer;
			
			postingsFreeList = new Posting[0];
		}
		
		/// <summary>If non-null, various details of indexing are printed
		/// here. 
		/// </summary>
		internal void  SetInfoStream(System.IO.TextWriter infoStream)
		{
			this.infoStream = infoStream;
		}
		
		/// <summary>Set how much RAM we can use before flushing. </summary>
		internal void  SetRAMBufferSizeMB(double mb)
		{
			if (mb == IndexWriter.DISABLE_AUTO_FLUSH)
			{
				ramBufferSize = IndexWriter.DISABLE_AUTO_FLUSH;
			}
			else
			{
				ramBufferSize = (long) (mb * 1024 * 1024);
			}
		}
		
		internal double GetRAMBufferSizeMB()
		{
			if (ramBufferSize == IndexWriter.DISABLE_AUTO_FLUSH)
			{
				return ramBufferSize;
			}
			else
			{
				return ramBufferSize / 1024.0 / 1024.0;
			}
		}
		
		/// <summary>Set max buffered docs, which means we will flush by
		/// doc count instead of by RAM usage. 
		/// </summary>
		internal void  SetMaxBufferedDocs(int count)
		{
			maxBufferedDocs = count;
		}
		
		internal int GetMaxBufferedDocs()
		{
			return maxBufferedDocs;
		}
		
		/// <summary>Get current segment name we are writing. </summary>
		internal System.String GetSegment()
		{
			return segment;
		}
		
		/// <summary>Returns how many docs are currently buffered in RAM. </summary>
		internal int GetNumDocsInRAM()
		{
			return numDocsInRAM;
		}
		
		/// <summary>Returns the current doc store segment we are writing
		/// to.  This will be the same as segment when autoCommit
		/// * is true. 
		/// </summary>
		internal System.String GetDocStoreSegment()
		{
			return docStoreSegment;
		}
		
		/// <summary>Returns the doc offset into the shared doc store for
		/// the current buffered docs. 
		/// </summary>
		internal int GetDocStoreOffset()
		{
			return docStoreOffset;
		}
		
		/// <summary>Closes the current open doc stores an returns the doc
		/// store segment name.  This returns null if there are *
		/// no buffered documents. 
		/// </summary>
		internal System.String CloseDocStore()
		{
			
			System.Diagnostics.Debug.Assert(AllThreadsIdle());
			
			System.Collections.IList flushedFiles = Files();
			
			if (infoStream != null)
				infoStream.WriteLine("\ncloseDocStore: " + flushedFiles.Count + " files to flush to segment " + docStoreSegment + " numDocs=" + numDocsInStore);
			
			if (flushedFiles.Count > 0)
			{
				files = null;
				
				if (tvx != null)
				{
					// At least one doc in this run had term vectors enabled
					System.Diagnostics.Debug.Assert(docStoreSegment != null);
					tvx.Close();
					tvf.Close();
					tvd.Close();
					tvx = null;
                    System.Diagnostics.Debug.Assert(4 + numDocsInStore * 8 == directory.FileLength(docStoreSegment + "." + IndexFileNames.VECTORS_INDEX_EXTENSION),
                        "after flush: tvx size mismatch: " + numDocsInStore + " docs vs " + directory.FileLength(docStoreSegment + "." + IndexFileNames.VECTORS_INDEX_EXTENSION) +
                        " length in bytes of " + docStoreSegment + "." + IndexFileNames.VECTORS_INDEX_EXTENSION);
				}
				
				if (fieldsWriter != null)
				{
					System.Diagnostics.Debug.Assert(docStoreSegment != null);
					fieldsWriter.Close();
					fieldsWriter = null;
                    System.Diagnostics.Debug.Assert(numDocsInStore * 8 == directory.FileLength(docStoreSegment + "." + IndexFileNames.FIELDS_INDEX_EXTENSION),
                        "after flush: fdx size mismatch: " + numDocsInStore + " docs vs " + directory.FileLength(docStoreSegment + "." + IndexFileNames.FIELDS_INDEX_EXTENSION) +
                        " length in bytes of " + docStoreSegment + "." + IndexFileNames.FIELDS_INDEX_EXTENSION);
                }
				
				System.String s = docStoreSegment;
				docStoreSegment = null;
				docStoreOffset = 0;
				numDocsInStore = 0;
				return s;
			}
			else
			{
				return null;
			}
		}
		
		private System.Collections.IList files = null; // Cached list of files we've created
		private System.Collections.IList abortedFiles = null; // List of files that were written before last abort()
		
		internal System.Collections.IList AbortedFiles()
		{
			return abortedFiles;
		}
		
		/* Returns list of files in use by this instance,
		* including any flushed segments. */
		internal System.Collections.IList Files()
		{
			lock (this)
			{
				
				if (files != null)
					return files;
				
				files = new System.Collections.ArrayList();
				
				// Stored fields:
				if (fieldsWriter != null)
				{
					System.Diagnostics.Debug.Assert(docStoreSegment != null);
					files.Add(docStoreSegment + "." + IndexFileNames.FIELDS_EXTENSION);
					files.Add(docStoreSegment + "." + IndexFileNames.FIELDS_INDEX_EXTENSION);
				}
				
				// Vectors:
				if (tvx != null)
				{
					System.Diagnostics.Debug.Assert(docStoreSegment != null);
					files.Add(docStoreSegment + "." + IndexFileNames.VECTORS_INDEX_EXTENSION);
					files.Add(docStoreSegment + "." + IndexFileNames.VECTORS_FIELDS_EXTENSION);
					files.Add(docStoreSegment + "." + IndexFileNames.VECTORS_DOCUMENTS_EXTENSION);
				}
				
				return files;
			}
		}
		
		internal void  SetAborting()
		{
			lock (this)
			{
				abortCount++;
			}
		}
		
		/// <summary>Called if we hit an exception when adding docs,
		/// flushing, etc.  This resets our state, discarding any
		/// docs added since last flush.  If ae is non-null, it
		/// contains the root cause exception (which we re-throw
		/// after we are done aborting). 
		/// </summary>
		internal void  Abort(AbortException ae)
		{
			lock (this)
			{
				
				// Anywhere that throws an AbortException must first
				// mark aborting to make sure while the exception is
				// unwinding the un-synchronized stack, no thread grabs
				// the corrupt ThreadState that hit the aborting
				// exception:
				System.Diagnostics.Debug.Assert(ae == null || abortCount > 0);
				
				try
				{
					
					if (infoStream != null)
						infoStream.WriteLine("docWriter: now abort");
					
					// Forcefully remove waiting ThreadStates from line
					for (int i = 0; i < numWaiting; i++)
						waitingThreadStates[i].isIdle = true;
					numWaiting = 0;
					
					// Wait for all other threads to finish with DocumentsWriter:
					PauseAllThreads();
					
					System.Diagnostics.Debug.Assert(0 == numWaiting);
					
					try
					{
						
						bufferedDeleteTerms.Clear();
						bufferedDeleteDocIDs.Clear();
						numBufferedDeleteTerms = 0;
						
						try
						{
							abortedFiles = Files();
						}
						catch (System.Exception)
						{
							abortedFiles = null;
						}
						
						docStoreSegment = null;
						numDocsInStore = 0;
						docStoreOffset = 0;
						files = null;
						
						// Clear vectors & fields from ThreadStates
						for (int i = 0; i < threadStates.Length; i++)
						{
							ThreadState state = threadStates[i];
							state.tvfLocal.Reset();
							state.fdtLocal.Reset();
							if (state.localFieldsWriter != null)
							{
								try
								{
									state.localFieldsWriter.Close();
								}
								catch (System.Exception)
								{
								}
								state.localFieldsWriter = null;
							}
						}
						
						// Reset vectors writer
						if (tvx != null)
						{
							try
							{
								tvx.Close();
							}
							catch (System.Exception)
							{
							}
							tvx = null;
						}
						if (tvd != null)
						{
							try
							{
								tvd.Close();
							}
							catch (System.Exception)
							{
							}
							tvd = null;
						}
						if (tvf != null)
						{
							try
							{
								tvf.Close();
							}
							catch (System.Exception)
							{
							}
							tvf = null;
						}
						
						// Reset fields writer
						if (fieldsWriter != null)
						{
							try
							{
								fieldsWriter.Close();
							}
							catch (System.Exception)
							{
							}
							fieldsWriter = null;
						}
						
						// Discard pending norms:
						int numField = fieldInfos.Size();
						for (int i = 0; i < numField; i++)
						{
							FieldInfo fi = fieldInfos.FieldInfo(i);
							if (fi.isIndexed && !fi.omitNorms)
							{
								BufferedNorms n = norms[i];
								if (n != null)
									try
									{
										n.Reset();
									}
									catch (System.Exception)
									{
									}
							}
						}
						
						// Reset all postings data
						ResetPostingsData();
					}
					finally
					{
						ResumeAllThreads();
					}
					
					// If we have a root cause exception, re-throw it now:
					if (ae != null)
					{
						System.Exception t = ae.InnerException;
						if (t is System.IO.IOException)
							throw (System.IO.IOException) t;
						else if (t is System.SystemException)
							throw (System.SystemException) t;
						else if (t is System.ApplicationException)
							throw (System.ApplicationException) t;
						else
							// Should not get here
							System.Diagnostics.Debug.Assert(false, "unknown exception: " + t);
					}
				}
				finally
				{
					if (ae != null)
						abortCount--;
					System.Threading.Monitor.PulseAll(this);
				}
			}
		}
		
		/// <summary>Reset after a flush </summary>
		private void  ResetPostingsData()
		{
			// All ThreadStates should be idle when we are called
			System.Diagnostics.Debug.Assert(AllThreadsIdle());
			threadBindings.Clear();
			segment = null;
			numDocsInRAM = 0;
			nextDocID = 0;
			nextWriteDocID = 0;
			files = null;
			BalanceRAM();
			bufferIsFull = false;
			flushPending = false;
			for (int i = 0; i < threadStates.Length; i++)
			{
				threadStates[i].numThreads = 0;
				threadStates[i].ResetPostings();
			}
			numBytesUsed = 0;
		}
		
		// Returns true if an abort is in progress
		internal bool PauseAllThreads()
		{
			lock (this)
			{
				pauseThreads++;
				while (!AllThreadsIdle())
				{
					try
					{
						System.Threading.Monitor.Wait(this);
					}
					catch (System.Threading.ThreadInterruptedException)
					{
						SupportClass.ThreadClass.Current().Interrupt();
					}
				}
				return abortCount > 0;
			}
		}
		
		internal void  ResumeAllThreads()
		{
			lock (this)
			{
				pauseThreads--;
				System.Diagnostics.Debug.Assert(pauseThreads >= 0);
				if (0 == pauseThreads)
					System.Threading.Monitor.PulseAll(this);
			}
		}
		
		private bool AllThreadsIdle()
		{
			lock (this)
			{
				for (int i = 0; i < threadStates.Length; i++)
					if (!threadStates[i].isIdle)
						return false;
				return true;
			}
		}
		
		private bool hasNorms; // Whether any norms were seen since last flush
		
		internal System.Collections.IList newFiles;
		
		/// <summary>Flush all pending docs to a new segment </summary>
		internal int Flush(bool closeDocStore)
		{
			lock (this)
			{
				
				System.Diagnostics.Debug.Assert(AllThreadsIdle());
				
				if (segment == null)
				// In case we are asked to flush an empty segment
					segment = writer.NewSegmentName();
				
				newFiles = new System.Collections.ArrayList();
				
				docStoreOffset = numDocsInStore;
				
				int docCount;
				
				System.Diagnostics.Debug.Assert(numDocsInRAM > 0);
				
				if (infoStream != null)
					infoStream.WriteLine("\nflush postings as segment " + segment + " numDocs=" + numDocsInRAM);
				
				bool success = false;
				
				try
				{
					System.Collections.IEnumerator e;

					if (closeDocStore)
					{
						System.Diagnostics.Debug.Assert(docStoreSegment != null);
						System.Diagnostics.Debug.Assert(docStoreSegment.Equals(segment));
						e = Files().GetEnumerator();
						while (e.MoveNext())
							newFiles.Add(e.Current);
						CloseDocStore();
					}
					
					fieldInfos.Write(directory, segment + ".fnm");
					
					docCount = numDocsInRAM;

					e = WriteSegment().GetEnumerator();
					while (e.MoveNext())
						newFiles.Add(e.Current);
					
					success = true;
				}
				finally
				{
					if (!success)
						Abort(null);
				}
				
				return docCount;
			}
		}
		
		/// <summary>Build compound file for the segment we just flushed </summary>
		internal void  CreateCompoundFile(System.String segment)
		{
			CompoundFileWriter cfsWriter = new CompoundFileWriter(directory, segment + "." + IndexFileNames.COMPOUND_FILE_EXTENSION);
			int size = newFiles.Count;
			for (int i = 0; i < size; i++)
				cfsWriter.AddFile((System.String) newFiles[i]);
			
			// Perform the merge
			cfsWriter.Close();
		}
		
		/// <summary>Set flushPending if it is not already set and returns
		/// whether it was set. This is used by IndexWriter to *
		/// trigger a single flush even when multiple threads are
		/// * trying to do so. 
		/// </summary>
		internal bool SetFlushPending()
		{
			lock (this)
			{
				if (flushPending)
					return false;
				else
				{
					flushPending = true;
					return true;
				}
			}
		}
		
		internal void  ClearFlushPending()
		{
			lock (this)
			{
				flushPending = false;
			}
		}
		
		/// <summary>Per-thread state.  We keep a separate Posting hash and
		/// other state for each thread and then merge postings *
		/// hashes from all threads when writing the segment. 
		/// </summary>
		sealed internal class ThreadState
		{
			private void  InitBlock(DocumentsWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
				allFieldDataArray = new FieldData[10];
				postingsPool = new ByteBlockPool(true, enclosingInstance);
				vectorsPool = new ByteBlockPool(false, enclosingInstance);
				charPool = new CharBlockPool(enclosingInstance);
			}
			private DocumentsWriter enclosingInstance;
			public DocumentsWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			internal Posting[] postingsFreeList; // Free Posting instances
			internal int postingsFreeCount;
			
			internal RAMOutputStream tvfLocal = new RAMOutputStream(); // Term vectors for one doc
			internal RAMOutputStream fdtLocal = new RAMOutputStream(); // Stored fields for one doc
			internal FieldsWriter localFieldsWriter; // Fields for one doc
			
			internal long[] vectorFieldPointers;
			internal int[] vectorFieldNumbers;
			
			internal bool isIdle = true; // Whether we are in use
			internal int numThreads = 1; // Number of threads that use this instance
			
			internal int docID; // docID we are now working on
			internal int numStoredFields; // How many stored fields in current doc
			internal float docBoost; // Boost for current doc
			
			internal FieldData[] fieldDataArray; // Fields touched by current doc
			internal int numFieldData; // How many fields in current doc
			internal int numVectorFields; // How many vector fields in current doc
			
			internal FieldData[] allFieldDataArray; // All FieldData instances
			internal int numAllFieldData;
			internal FieldData[] fieldDataHash; // Hash FieldData instances by field name
			internal int fieldDataHashMask;
			internal System.String maxTermPrefix; // Non-null prefix of a too-large term if this
			// doc has one
			
			internal bool doFlushAfter;
			
			public ThreadState(DocumentsWriter enclosingInstance)
			{
				InitBlock(enclosingInstance);
				fieldDataArray = new FieldData[8];
				
				fieldDataHash = new FieldData[16];
				fieldDataHashMask = 15;
				
				vectorFieldPointers = new long[10];
				vectorFieldNumbers = new int[10];
				postingsFreeList = new Posting[256];
				postingsFreeCount = 0;
			}
			
			/// <summary>Clear the postings hash and return objects back to
			/// shared pool 
			/// </summary>
			public void  ResetPostings()
			{
				fieldGen = 0;
				maxPostingsVectors = 0;
				doFlushAfter = false;
				if (localFieldsWriter != null)
				{
					localFieldsWriter.Close();
					localFieldsWriter = null;
				}
				postingsPool.Reset();
				charPool.Reset();
				Enclosing_Instance.RecyclePostings(postingsFreeList, postingsFreeCount);
				postingsFreeCount = 0;
				for (int i = 0; i < numAllFieldData; i++)
				{
					FieldData fp = allFieldDataArray[i];
					fp.lastGen = - 1;
					if (fp.numPostings > 0)
						fp.ResetPostingArrays();
				}
			}
			
			/// <summary>Move all per-document state that was accumulated in
			/// the ThreadState into the "real" stores. 
			/// </summary>
			public void  WriteDocument()
			{
				
				// If we hit an exception while appending to the
				// stored fields or term vectors files, we have to
				// abort all documents since we last flushed because
				// it means those files are possibly inconsistent.
				try
				{
					
					Enclosing_Instance.numDocsInStore++;
					
					// Append stored fields to the real FieldsWriter:
					Enclosing_Instance.fieldsWriter.FlushDocument(numStoredFields, fdtLocal);
					fdtLocal.Reset();
					
					// Append term vectors to the real outputs:
					if (Enclosing_Instance.tvx != null)
					{
						Enclosing_Instance.tvx.WriteLong(Enclosing_Instance.tvd.GetFilePointer());
						Enclosing_Instance.tvd.WriteVInt(numVectorFields);
						if (numVectorFields > 0)
						{
							for (int i = 0; i < numVectorFields; i++)
								Enclosing_Instance.tvd.WriteVInt(vectorFieldNumbers[i]);
							System.Diagnostics.Debug.Assert(0 == vectorFieldPointers [0]);
							Enclosing_Instance.tvd.WriteVLong(Enclosing_Instance.tvf.GetFilePointer());
							long lastPos = vectorFieldPointers[0];
							for (int i = 1; i < numVectorFields; i++)
							{
								long pos = vectorFieldPointers[i];
								Enclosing_Instance.tvd.WriteVLong(pos - lastPos);
								lastPos = pos;
							}
							tvfLocal.WriteTo(Enclosing_Instance.tvf);
							tvfLocal.Reset();
						}
					}
					
					// Append norms for the fields we saw:
					for (int i = 0; i < numFieldData; i++)
					{
						FieldData fp = fieldDataArray[i];
						if (fp.doNorms)
						{
							BufferedNorms bn = Enclosing_Instance.norms[fp.fieldInfo.number];
							System.Diagnostics.Debug.Assert(bn != null);
							System.Diagnostics.Debug.Assert(bn.upto <= docID);
							bn.Fill(docID);
							float norm = fp.boost * Enclosing_Instance.writer.GetSimilarity().LengthNorm(fp.fieldInfo.name, fp.length);
							bn.Add(norm);
						}
					}
				}
				catch (System.Exception t)
				{
					// Forcefully idle this threadstate -- its state will
					// be reset by abort()
					isIdle = true;
					throw new AbortException(t, Enclosing_Instance);
				}
				
				if (Enclosing_Instance.bufferIsFull && !Enclosing_Instance.flushPending)
				{
					Enclosing_Instance.flushPending = true;
					doFlushAfter = true;
				}
			}
			
			internal int fieldGen;
			
			/// <summary>Initializes shared state for this new document </summary>
			internal void  Init(Document doc, int docID)
			{

                System.Diagnostics.Debug.Assert(!isIdle);
                System.Diagnostics.Debug.Assert(Enclosing_Instance.writer.TestPoint("DocumentsWriter.ThreadState.init start"));
				
				this.docID = docID;
				docBoost = doc.GetBoost();
				numStoredFields = 0;
				numFieldData = 0;
				numVectorFields = 0;
				maxTermPrefix = null;
				
				System.Diagnostics.Debug.Assert(0 == fdtLocal.Length());
				System.Diagnostics.Debug.Assert(0 == fdtLocal.GetFilePointer());
				System.Diagnostics.Debug.Assert(0 == tvfLocal.Length());
				System.Diagnostics.Debug.Assert(0 == tvfLocal.GetFilePointer());
				int thisFieldGen = fieldGen++;
				
				System.Collections.IList docFields = doc.GetFields();
				int numDocFields = docFields.Count;
				bool docHasVectors = false;
				
				// Absorb any new fields first seen in this document.
				// Also absorb any changes to fields we had already
				// seen before (eg suddenly turning on norms or
				// vectors, etc.):
				
				for (int i = 0; i < numDocFields; i++)
				{
					Fieldable field = (Fieldable) docFields[i];
					
					FieldInfo fi = Enclosing_Instance.fieldInfos.Add(field.Name(), field.IsIndexed(), field.IsTermVectorStored(), field.IsStorePositionWithTermVector(), field.IsStoreOffsetWithTermVector(), field.GetOmitNorms(), false);
					if (fi.isIndexed && !fi.omitNorms)
					{
						// Maybe grow our buffered norms
						if (Enclosing_Instance.norms.Length <= fi.number)
						{
							int newSize = (int) ((1 + fi.number) * 1.25);
							BufferedNorms[] newNorms = new BufferedNorms[newSize];
							Array.Copy(Enclosing_Instance.norms, 0, newNorms, 0, Enclosing_Instance.norms.Length);
							Enclosing_Instance.norms = newNorms;
						}
						
						if (Enclosing_Instance.norms[fi.number] == null)
							Enclosing_Instance.norms[fi.number] = new BufferedNorms();
						
						Enclosing_Instance.hasNorms = true;
					}
					
					// Make sure we have a FieldData allocated
					int hashPos = fi.name.GetHashCode() & fieldDataHashMask;
					FieldData fp = fieldDataHash[hashPos];
					while (fp != null && !fp.fieldInfo.name.Equals(fi.name))
						fp = fp.next;
					
					if (fp == null)
					{
						
						fp = new FieldData(this, fi);
						fp.next = fieldDataHash[hashPos];
						fieldDataHash[hashPos] = fp;
						
						if (numAllFieldData == allFieldDataArray.Length)
						{
							int newSize = (int) (allFieldDataArray.Length * 1.5);
							int newHashSize = fieldDataHash.Length * 2;
							
							FieldData[] newArray = new FieldData[newSize];
							FieldData[] newHashArray = new FieldData[newHashSize];
							Array.Copy(allFieldDataArray, 0, newArray, 0, numAllFieldData);
							
							// Rehash
							fieldDataHashMask = newSize - 1;
							for (int j = 0; j < fieldDataHash.Length; j++)
							{
								FieldData fp0 = fieldDataHash[j];
								while (fp0 != null)
								{
									hashPos = fp0.fieldInfo.name.GetHashCode() & fieldDataHashMask;
									FieldData nextFP0 = fp0.next;
									fp0.next = newHashArray[hashPos];
									newHashArray[hashPos] = fp0;
									fp0 = nextFP0;
								}
							}
							
							allFieldDataArray = newArray;
							fieldDataHash = newHashArray;
						}
						allFieldDataArray[numAllFieldData++] = fp;
					}
					else
					{
						System.Diagnostics.Debug.Assert(fp.fieldInfo == fi);
					}
					
					if (thisFieldGen != fp.lastGen)
					{
						
						// First time we're seeing this field for this doc
						fp.lastGen = thisFieldGen;
						fp.fieldCount = 0;
						fp.doVectors = fp.doVectorPositions = fp.doVectorOffsets = false;
						fp.doNorms = fi.isIndexed && !fi.omitNorms;
						
						if (numFieldData == fieldDataArray.Length)
						{
							int newSize = fieldDataArray.Length * 2;
							FieldData[] newArray = new FieldData[newSize];
							Array.Copy(fieldDataArray, 0, newArray, 0, numFieldData);
							fieldDataArray = newArray;
						}
						fieldDataArray[numFieldData++] = fp;
					}
					
					if (field.IsTermVectorStored())
					{
						if (!fp.doVectors && numVectorFields++ == vectorFieldPointers.Length)
						{
							int newSize = (int) (numVectorFields * 1.5);
							vectorFieldPointers = new long[newSize];
							vectorFieldNumbers = new int[newSize];
						}
						fp.doVectors = true;
						docHasVectors = true;
						
						fp.doVectorPositions |= field.IsStorePositionWithTermVector();
						fp.doVectorOffsets |= field.IsStoreOffsetWithTermVector();
					}
					
					if (fp.fieldCount == fp.docFields.Length)
					{
						Fieldable[] newArray = new Fieldable[fp.docFields.Length * 2];
						Array.Copy(fp.docFields, 0, newArray, 0, fp.docFields.Length);
						fp.docFields = newArray;
					}
					
					// Lazily allocate arrays for postings:
					if (field.IsIndexed() && fp.postingsHash == null)
						fp.InitPostingArrays();
					
					fp.docFields[fp.fieldCount++] = field;
				}
				
				// Maybe init the local & global fieldsWriter
				if (localFieldsWriter == null)
				{
					if (Enclosing_Instance.fieldsWriter == null)
					{
						System.Diagnostics.Debug.Assert(Enclosing_Instance.docStoreSegment == null);
						System.Diagnostics.Debug.Assert(Enclosing_Instance.segment != null);
						Enclosing_Instance.docStoreSegment = Enclosing_Instance.segment;
						// If we hit an exception while init'ing the
						// fieldsWriter, we must abort this segment
						// because those files will be in an unknown
						// state:
						try
						{
							Enclosing_Instance.fieldsWriter = new FieldsWriter(Enclosing_Instance.directory, Enclosing_Instance.docStoreSegment, Enclosing_Instance.fieldInfos);
						}
						catch (System.Exception t)
						{
							throw new AbortException(t, Enclosing_Instance);
						}
						Enclosing_Instance.files = null;
					}
					localFieldsWriter = new FieldsWriter(null, fdtLocal, Enclosing_Instance.fieldInfos);
				}
				
				// First time we see a doc that has field(s) with
				// stored vectors, we init our tvx writer
				if (docHasVectors)
				{
					if (Enclosing_Instance.tvx == null)
					{
						System.Diagnostics.Debug.Assert(Enclosing_Instance.docStoreSegment != null);
						// If we hit an exception while init'ing the term
						// vector output files, we must abort this segment
						// because those files will be in an unknown
						// state:
						try
						{
							Enclosing_Instance.tvx = Enclosing_Instance.directory.CreateOutput(Enclosing_Instance.docStoreSegment + "." + IndexFileNames.VECTORS_INDEX_EXTENSION);
							Enclosing_Instance.tvx.WriteInt(TermVectorsReader.FORMAT_VERSION);
							Enclosing_Instance.tvd = Enclosing_Instance.directory.CreateOutput(Enclosing_Instance.docStoreSegment + "." + IndexFileNames.VECTORS_DOCUMENTS_EXTENSION);
							Enclosing_Instance.tvd.WriteInt(TermVectorsReader.FORMAT_VERSION);
							Enclosing_Instance.tvf = Enclosing_Instance.directory.CreateOutput(Enclosing_Instance.docStoreSegment + "." + IndexFileNames.VECTORS_FIELDS_EXTENSION);
							Enclosing_Instance.tvf.WriteInt(TermVectorsReader.FORMAT_VERSION);
							
							// We must "catch up" for all docs before us
							// that had no vectors:
							for (int i = 0; i < Enclosing_Instance.numDocsInStore; i++)
							{
								Enclosing_Instance.tvx.WriteLong(Enclosing_Instance.tvd.GetFilePointer());
								Enclosing_Instance.tvd.WriteVInt(0);
							}
						}
						catch (System.Exception t)
						{
							throw new AbortException(t, Enclosing_Instance);
						}
						Enclosing_Instance.files = null;
					}
					
					numVectorFields = 0;
				}
			}
			
			/// <summary>Do in-place sort of Posting array </summary>
			internal void  DoPostingSort(Posting[] postings, int numPosting)
			{
				QuickSort(postings, 0, numPosting - 1);
			}
			
			internal void  QuickSort(Posting[] postings, int lo, int hi)
			{
				if (lo >= hi)
					return ;
				
				int mid = SupportClass.Number.URShift((lo + hi), 1);
				
				if (ComparePostings(postings[lo], postings[mid]) > 0)
				{
					Posting tmp = postings[lo];
					postings[lo] = postings[mid];
					postings[mid] = tmp;
				}
				
				if (ComparePostings(postings[mid], postings[hi]) > 0)
				{
					Posting tmp = postings[mid];
					postings[mid] = postings[hi];
					postings[hi] = tmp;
					
					if (ComparePostings(postings[lo], postings[mid]) > 0)
					{
						Posting tmp2 = postings[lo];
						postings[lo] = postings[mid];
						postings[mid] = tmp2;
					}
				}
				
				int left = lo + 1;
				int right = hi - 1;
				
				if (left >= right)
					return ;
				
				Posting partition = postings[mid];
				
				for (; ; )
				{
					while (ComparePostings(postings[right], partition) > 0)
						--right;
					
					while (left < right && ComparePostings(postings[left], partition) <= 0)
						++left;
					
					if (left < right)
					{
						Posting tmp = postings[left];
						postings[left] = postings[right];
						postings[right] = tmp;
						--right;
					}
					else
					{
						break;
					}
				}
				
				QuickSort(postings, lo, left);
				QuickSort(postings, left + 1, hi);
			}
			
			/// <summary>Do in-place sort of PostingVector array </summary>
			internal void  DoVectorSort(PostingVector[] postings, int numPosting)
			{
				QuickSort(postings, 0, numPosting - 1);
			}
			
			internal void  QuickSort(PostingVector[] postings, int lo, int hi)
			{
				if (lo >= hi)
					return ;
				
				int mid = SupportClass.Number.URShift((lo + hi), 1);
				
				if (ComparePostings(postings[lo].p, postings[mid].p) > 0)
				{
					PostingVector tmp = postings[lo];
					postings[lo] = postings[mid];
					postings[mid] = tmp;
				}
				
				if (ComparePostings(postings[mid].p, postings[hi].p) > 0)
				{
					PostingVector tmp = postings[mid];
					postings[mid] = postings[hi];
					postings[hi] = tmp;
					
					if (ComparePostings(postings[lo].p, postings[mid].p) > 0)
					{
						PostingVector tmp2 = postings[lo];
						postings[lo] = postings[mid];
						postings[mid] = tmp2;
					}
				}
				
				int left = lo + 1;
				int right = hi - 1;
				
				if (left >= right)
					return ;
				
				PostingVector partition = postings[mid];
				
				for (; ; )
				{
					while (ComparePostings(postings[right].p, partition.p) > 0)
						--right;
					
					while (left < right && ComparePostings(postings[left].p, partition.p) <= 0)
						++left;
					
					if (left < right)
					{
						PostingVector tmp = postings[left];
						postings[left] = postings[right];
						postings[right] = tmp;
						--right;
					}
					else
					{
						break;
					}
				}
				
				QuickSort(postings, lo, left);
				QuickSort(postings, left + 1, hi);
			}
			
			/// <summary>If there are fields we've seen but did not see again
			/// in the last run, then free them up.  Also reduce
			/// postings hash size. 
			/// </summary>
			internal void  TrimFields()
			{
				
				int upto = 0;
				for (int i = 0; i < numAllFieldData; i++)
				{
					FieldData fp = allFieldDataArray[i];
					if (fp.lastGen == - 1)
					{
						// This field was not seen since the previous
						// flush, so, free up its resources now
						
						// Unhash
						int hashPos = fp.fieldInfo.name.GetHashCode() & fieldDataHashMask;
						FieldData last = null;
						FieldData fp0 = fieldDataHash[hashPos];
						while (fp0 != fp)
						{
							last = fp0;
							fp0 = fp0.next;
						}
						System.Diagnostics.Debug.Assert(fp0 != null);
						
						if (last == null)
							fieldDataHash[hashPos] = fp.next;
						else
							last.next = fp.next;
						
						if (Enclosing_Instance.infoStream != null)
							Enclosing_Instance.infoStream.WriteLine("  remove field=" + fp.fieldInfo.name);
					}
					else
					{
						// Reset
						fp.lastGen = - 1;
						allFieldDataArray[upto++] = fp;
						
						if (fp.numPostings > 0 && ((float) fp.numPostings) / fp.postingsHashSize < 0.2)
						{
							int hashSize = fp.postingsHashSize;
							
							// Reduce hash so it's between 25-50% full
							while (fp.numPostings < (hashSize >> 1) && hashSize >= 2)
								hashSize >>= 1;
							hashSize <<= 1;
							
							if (hashSize != fp.postingsHash.Length)
								fp.RehashPostings(hashSize);
						}
					}
				}
				
				// If we didn't see any norms for this field since
				// last flush, free it
				for (int i = 0; i < Enclosing_Instance.norms.Length; i++)
				{
					BufferedNorms n = Enclosing_Instance.norms[i];
					if (n != null && n.upto == 0)
						Enclosing_Instance.norms[i] = null;
				}
				
				numAllFieldData = upto;
				
				// Also pare back PostingsVectors if it's excessively
				// large
				if (maxPostingsVectors * 1.5 < postingsVectors.Length)
				{
					int newSize;
					if (0 == maxPostingsVectors)
						newSize = 1;
					else
					{
						newSize = (int) (1.5 * maxPostingsVectors);
					}
					PostingVector[] newArray = new PostingVector[newSize];
					Array.Copy(postingsVectors, 0, newArray, 0, newSize);
					postingsVectors = newArray;
				}
			}
			
			/// <summary>Tokenizes the fields of a document into Postings </summary>
			internal void  ProcessDocument(Analyzer analyzer)
			{
				
				int numFields = numFieldData;
				
				System.Diagnostics.Debug.Assert(0 == fdtLocal.Length());
				
				if (Enclosing_Instance.tvx != null)
				// If we are writing vectors then we must visit
				// fields in sorted order so they are written in
				// sorted order.  TODO: we actually only need to
				// sort the subset of fields that have vectors
				// enabled; we could save [small amount of] CPU
				// here.
					System.Array.Sort(fieldDataArray, 0, numFields - 0);
				
				// We process the document one field at a time
				for (int i = 0; i < numFields; i++)
					fieldDataArray[i].ProcessField(analyzer);
				
				if (maxTermPrefix != null && Enclosing_Instance.infoStream != null)
					Enclosing_Instance.infoStream.WriteLine("WARNING: document contains at least one immense term (longer than the max length " + Lucene.Net.Index.DocumentsWriter.MAX_TERM_LENGTH + "), all of which were skipped.  Please correct the analyzer to not produce such terms.  The prefix of the first immense term is: '" + maxTermPrefix + "...'");
				
				if (Enclosing_Instance.ramBufferSize != IndexWriter.DISABLE_AUTO_FLUSH && Enclosing_Instance.numBytesUsed > 0.95 * Enclosing_Instance.ramBufferSize)
					Enclosing_Instance.BalanceRAM();
			}
			
			internal ByteBlockPool postingsPool;
			internal ByteBlockPool vectorsPool;
			internal CharBlockPool charPool;
			
			// Current posting we are working on
			internal Posting p;
			internal PostingVector vector;
			
			// USE ONLY FOR DEBUGGING!
			/*
			public String getPostingText() {
			char[] text = charPool.buffers[p.textStart >> CHAR_BLOCK_SHIFT];
			int upto = p.textStart & CHAR_BLOCK_MASK;
			while(text[upto] != 0xffff)
			upto++;
			return new String(text, p.textStart, upto-(p.textStart & BYTE_BLOCK_MASK));
			}
			*/
			
			/// <summary>Test whether the text for current Posting p equals
			/// current tokenText. 
			/// </summary>
			internal bool PostingEquals(char[] tokenText, int tokenTextLen)
			{
				
				char[] text = charPool.buffers[p.textStart >> Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SHIFT];
				System.Diagnostics.Debug.Assert(text != null);
				int pos = p.textStart & Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_MASK;
				
				int tokenPos = 0;
				for (; tokenPos < tokenTextLen; pos++, tokenPos++)
					if (tokenText[tokenPos] != text[pos])
						return false;
				return 0xffff == text[pos];
			}
			
			/// <summary>Compares term text for two Posting instance and
			/// returns -1 if p1 < p2; 1 if p1 > p2; else 0.
			/// </summary>
			internal int ComparePostings(Posting p1, Posting p2)
			{
				char[] text1 = charPool.buffers[p1.textStart >> Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SHIFT];
				int pos1 = p1.textStart & Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_MASK;
				char[] text2 = charPool.buffers[p2.textStart >> Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SHIFT];
				int pos2 = p2.textStart & Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_MASK;
				while (true)
				{
					char c1 = text1[pos1++];
					char c2 = text2[pos2++];
					if (c1 < c2)
						if (0xffff == c2)
							return 1;
						else
							return - 1;
					else if (c2 < c1)
						if (0xffff == c1)
							return - 1;
						else
							return 1;
					else if (0xffff == c1)
						return 0;
				}
			}
			
			/// <summary>Write vInt into freq stream of current Posting </summary>
			public void  WriteFreqVInt(int i)
			{
				while ((i & ~ 0x7F) != 0)
				{
					WriteFreqByte((byte) ((i & 0x7f) | 0x80));
					i = SupportClass.Number.URShift(i, 7);
				}
				WriteFreqByte((byte) i);
			}
			
			/// <summary>Write vInt into prox stream of current Posting </summary>
			public void  WriteProxVInt(int i)
			{
				while ((i & ~ 0x7F) != 0)
				{
					WriteProxByte((byte) ((i & 0x7f) | 0x80));
					i = SupportClass.Number.URShift(i, 7);
				}
				WriteProxByte((byte) i);
			}
			
			/// <summary>Write byte into freq stream of current Posting </summary>
			internal byte[] freq;
			internal int freqUpto;
			public void  WriteFreqByte(byte b)
			{
				System.Diagnostics.Debug.Assert(freq != null);
				if (freq[freqUpto] != 0)
				{
					freqUpto = postingsPool.AllocSlice(freq, freqUpto);
					freq = postingsPool.buffer;
					p.freqUpto = postingsPool.byteOffset;
				}
				freq[freqUpto++] = b;
			}
			
			/// <summary>Write byte into prox stream of current Posting </summary>
			internal byte[] prox;
			internal int proxUpto;
			public void  WriteProxByte(byte b)
			{
				System.Diagnostics.Debug.Assert(prox != null);
				if (prox[proxUpto] != 0)
				{
					proxUpto = postingsPool.AllocSlice(prox, proxUpto);
					prox = postingsPool.buffer;
					p.proxUpto = postingsPool.byteOffset;
					System.Diagnostics.Debug.Assert(prox != null);
				}
				prox[proxUpto++] = b;
				System.Diagnostics.Debug.Assert(proxUpto != prox.Length);
			}
			
			/// <summary>Currently only used to copy a payload into the prox
			/// stream. 
			/// </summary>
			public void  WriteProxBytes(byte[] b, int offset, int len)
			{
				int offsetEnd = offset + len;
				while (offset < offsetEnd)
				{
					if (prox[proxUpto] != 0)
					{
						// End marker
						proxUpto = postingsPool.AllocSlice(prox, proxUpto);
						prox = postingsPool.buffer;
						p.proxUpto = postingsPool.byteOffset;
					}
					
					prox[proxUpto++] = b[offset++];
					System.Diagnostics.Debug.Assert(proxUpto != prox.Length);
				}
			}
			
			/// <summary>Write vInt into offsets stream of current
			/// PostingVector 
			/// </summary>
			public void  WriteOffsetVInt(int i)
			{
				while ((i & ~ 0x7F) != 0)
				{
					WriteOffsetByte((byte) ((i & 0x7f) | 0x80));
					i = SupportClass.Number.URShift(i, 7);
				}
				WriteOffsetByte((byte) i);
			}
			
			internal byte[] offsets;
			internal int offsetUpto;
			
			/// <summary>Write byte into offsets stream of current
			/// PostingVector 
			/// </summary>
			public void  WriteOffsetByte(byte b)
			{
				System.Diagnostics.Debug.Assert(offsets != null);
				if (offsets[offsetUpto] != 0)
				{
					offsetUpto = vectorsPool.AllocSlice(offsets, offsetUpto);
					offsets = vectorsPool.buffer;
					vector.offsetUpto = vectorsPool.byteOffset;
				}
				offsets[offsetUpto++] = b;
			}
			
			/// <summary>Write vInt into pos stream of current
			/// PostingVector 
			/// </summary>
			public void  WritePosVInt(int i)
			{
				while ((i & ~ 0x7F) != 0)
				{
					WritePosByte((byte) ((i & 0x7f) | 0x80));
					i = SupportClass.Number.URShift(i, 7);
				}
				WritePosByte((byte) i);
			}
			
			internal byte[] pos;
			internal int posUpto;
			
			/// <summary>Write byte into pos stream of current
			/// PostingVector 
			/// </summary>
			public void  WritePosByte(byte b)
			{
				System.Diagnostics.Debug.Assert(pos != null);
				if (pos[posUpto] != 0)
				{
					posUpto = vectorsPool.AllocSlice(pos, posUpto);
					pos = vectorsPool.buffer;
					vector.posUpto = vectorsPool.byteOffset;
				}
				pos[posUpto++] = b;
			}
			
			internal PostingVector[] postingsVectors = new PostingVector[1];
			internal int maxPostingsVectors;
			
			// Used to read a string value for a field
			internal ReusableStringReader stringReader = new ReusableStringReader();
			
			/// <summary>Holds data associated with a single field, including
			/// the Postings hash.  A document may have many *
			/// occurrences for a given field name; we gather all *
			/// such occurrences here (in docFields) so that we can
			/// * process the entire field at once. 
			/// </summary>
			sealed internal class FieldData : System.IComparable
			{
				private void  InitBlock(ThreadState enclosingInstance)
				{
					this.enclosingInstance = enclosingInstance;
				}
				private ThreadState enclosingInstance;
				public ThreadState Enclosing_Instance
				{
					get
					{
						return enclosingInstance;
					}
					
				}
				
				internal ThreadState threadState;
				internal FieldInfo fieldInfo;
				
				internal int fieldCount;
				internal Fieldable[] docFields = new Fieldable[1];
				
				internal int lastGen = - 1;
				internal FieldData next;
				
				internal bool doNorms;
				internal bool doVectors;
				internal bool doVectorPositions;
				internal bool doVectorOffsets;
				internal bool postingsCompacted;
				
				internal int numPostings;
				
				internal Posting[] postingsHash;
				internal int postingsHashSize;
				internal int postingsHashHalfSize;
				internal int postingsHashMask;
				
				internal int position;
				internal int length;
				internal int offset;
				internal float boost;
				internal int postingsVectorsUpto;
				
				public FieldData(ThreadState enclosingInstance, FieldInfo fieldInfo)
				{
					InitBlock(enclosingInstance);
					this.fieldInfo = fieldInfo;
					threadState = Enclosing_Instance;
				}
				
				internal void  ResetPostingArrays()
				{
					if (!postingsCompacted)
						CompactPostings();
					Enclosing_Instance.Enclosing_Instance.RecyclePostings(this.postingsHash, numPostings);
					Array.Clear(postingsHash, 0, postingsHash.Length);
					postingsCompacted = false;
					numPostings = 0;
				}
				
				internal void  InitPostingArrays()
				{
					// Target hash fill factor of <= 50%
					// NOTE: must be a power of two for hash collision
					// strategy to work correctly
					postingsHashSize = 4;
					postingsHashHalfSize = 2;
					postingsHashMask = postingsHashSize - 1;
					postingsHash = new Posting[postingsHashSize];
				}
				
				/// <summary>So Arrays.sort can sort us. </summary>
				public int CompareTo(System.Object o)
				{
					return String.CompareOrdinal(fieldInfo.name, ((FieldData) o).fieldInfo.name);
				}
				
				private void  CompactPostings()
				{
					int upto = 0;
					for (int i = 0; i < postingsHashSize; i++)
						if (postingsHash[i] != null)
							postingsHash[upto++] = postingsHash[i];
					
					System.Diagnostics.Debug.Assert(upto == numPostings);
					postingsCompacted = true;
				}
				
				/// <summary>Collapse the hash table & sort in-place. </summary>
				public Posting[] SortPostings()
				{
					CompactPostings();
					Enclosing_Instance.DoPostingSort(postingsHash, numPostings);
					return postingsHash;
				}
				
				/// <summary>Process all occurrences of one field in the document. </summary>
				public void  ProcessField(Analyzer analyzer)
				{
					length = 0;
					position = 0;
					offset = 0;
					boost = Enclosing_Instance.docBoost;
					
					int maxFieldLength = Enclosing_Instance.Enclosing_Instance.writer.GetMaxFieldLength();
					
					int limit = fieldCount;
					Fieldable[] docFieldsFinal = docFields;
					
					bool doWriteVectors = true;
					
					// Walk through all occurrences in this doc for this
					// field:
					try
					{
						for (int j = 0; j < limit; j++)
						{
							Fieldable field = docFieldsFinal[j];
							
							if (field.IsIndexed())
								InvertField(field, analyzer, maxFieldLength);
							
							if (field.IsStored())
							{
								Enclosing_Instance.numStoredFields++;
								bool success = false;
								try
								{
									Enclosing_Instance.localFieldsWriter.WriteField(fieldInfo, field);
									success = true;
								}
								finally
								{
									// If we hit an exception inside
									// localFieldsWriter.writeField, the
									// contents of fdtLocal can be corrupt, so
									// we must discard all stored fields for
									// this document:
									if (!success)
										Enclosing_Instance.fdtLocal.Reset();
								}
							}
							
							docFieldsFinal[j] = null;
						}
					}
					catch (AbortException ae)
					{
						doWriteVectors = false;
						throw ae;
					}
					finally
					{
						if (postingsVectorsUpto > 0)
						{
							try
							{
								if (doWriteVectors)
								{
									// Add term vectors for this field
									bool success = false;
									try
									{
										WriteVectors(fieldInfo);
										success = true;
									}
									finally
									{
										if (!success)
										{
											// If we hit an exception inside
											// writeVectors, the contents of tvfLocal
											// can be corrupt, so we must discard all
											// term vectors for this document:
											Enclosing_Instance.numVectorFields = 0;
											Enclosing_Instance.tvfLocal.Reset();
										}
									}
								}
							}
							finally
							{
								if (postingsVectorsUpto > Enclosing_Instance.maxPostingsVectors)
									Enclosing_Instance.maxPostingsVectors = postingsVectorsUpto;
								postingsVectorsUpto = 0;
								Enclosing_Instance.vectorsPool.Reset();
							}
						}
					}
				}
				
				internal int offsetEnd;
				internal Token localToken = new Token();
				
				/* Invert one occurrence of one field in the document */
				public void  InvertField(Fieldable field, Analyzer analyzer, int maxFieldLength)
				{
					
					if (length > 0)
						position += analyzer.GetPositionIncrementGap(fieldInfo.name);
					
					if (!field.IsTokenized())
					{
						// un-tokenized field
						System.String stringValue = field.StringValue();
						int valueLength = stringValue.Length;
						Token token = localToken;
						token.Clear();
						char[] termBuffer = token.TermBuffer();
						if (termBuffer.Length < valueLength)
							termBuffer = token.ResizeTermBuffer(valueLength);
						DocumentsWriter.GetCharsFromString(stringValue, 0, valueLength, termBuffer, 0);
						token.SetTermLength(valueLength);
						token.SetStartOffset(offset);
						token.SetEndOffset(offset + stringValue.Length);
						AddPosition(token);
						offset += stringValue.Length;
						length++;
					}
					else
					{
						// tokenized field
						TokenStream stream;
						TokenStream streamValue = field.TokenStreamValue();
						
						if (streamValue != null)
							stream = streamValue;
						else
						{
							// the field does not have a TokenStream,
							// so we have to obtain one from the analyzer
							System.IO.TextReader reader; // find or make Reader
							System.IO.TextReader readerValue = field.ReaderValue();
							
							if (readerValue != null)
								reader = readerValue;
							else
							{
								System.String stringValue = field.StringValue();
								if (stringValue == null)
									throw new System.ArgumentException("field must have either TokenStream, String or Reader value");
								Enclosing_Instance.stringReader.Init(stringValue);
								reader = Enclosing_Instance.stringReader;
							}
							
							// Tokenize field and add to postingTable
							stream = analyzer.ReusableTokenStream(fieldInfo.name, reader);
						}
						
						// reset the TokenStream to the first token
						stream.Reset();
						
						try
						{
							offsetEnd = offset - 1;
							for (; ; )
							{
								Token token = stream.Next(localToken);
								if (token == null)
									break;
								position += (token.GetPositionIncrement() - 1);
								AddPosition(token);
								if (++length >= maxFieldLength)
								{
									if (Enclosing_Instance.Enclosing_Instance.infoStream != null)
										Enclosing_Instance.Enclosing_Instance.infoStream.WriteLine("maxFieldLength " + maxFieldLength + " reached for field " + fieldInfo.name + ", ignoring following tokens");
									break;
								}
							}
							offset = offsetEnd + 1;
						}
						finally
						{
							stream.Close();
						}
					}
					
					boost *= field.GetBoost();
				}
				
				/// <summary>Only called when term vectors are enabled.  This
				/// is called the first time we see a given term for
				/// each * document, to allocate a PostingVector
				/// instance that * is used to record data needed to
				/// write the posting * vectors. 
				/// </summary>
				private PostingVector AddNewVector()
				{
					
					if (postingsVectorsUpto == Enclosing_Instance.postingsVectors.Length)
					{
						int newSize;
						if (Enclosing_Instance.postingsVectors.Length < 2)
							newSize = 2;
						else
						{
							newSize = (int) (1.5 * Enclosing_Instance.postingsVectors.Length);
						}
						PostingVector[] newArray = new PostingVector[newSize];
						Array.Copy(Enclosing_Instance.postingsVectors, 0, newArray, 0, Enclosing_Instance.postingsVectors.Length);
						Enclosing_Instance.postingsVectors = newArray;
					}
					
					Enclosing_Instance.p.vector = Enclosing_Instance.postingsVectors[postingsVectorsUpto];
					if (Enclosing_Instance.p.vector == null)
						Enclosing_Instance.p.vector = Enclosing_Instance.postingsVectors[postingsVectorsUpto] = new PostingVector();
					
					postingsVectorsUpto++;
					
					PostingVector v = Enclosing_Instance.p.vector;
					v.p = Enclosing_Instance.p;
					
					int firstSize = Lucene.Net.Index.DocumentsWriter.levelSizeArray[0];
					
					if (doVectorPositions)
					{
						int upto = Enclosing_Instance.vectorsPool.NewSlice(firstSize);
						v.posStart = v.posUpto = Enclosing_Instance.vectorsPool.byteOffset + upto;
					}
					
					if (doVectorOffsets)
					{
						int upto = Enclosing_Instance.vectorsPool.NewSlice(firstSize);
						v.offsetStart = v.offsetUpto = Enclosing_Instance.vectorsPool.byteOffset + upto;
					}
					
					return v;
				}
				
				internal int offsetStartCode;
				internal int offsetStart;
				
				/// <summary>This is the hotspot of indexing: it's called once
				/// for every term of every document.  Its job is to *
				/// update the postings byte stream (Postings hash) *
				/// based on the occurence of a single term. 
				/// </summary>
				private void  AddPosition(Token token)
				{
					
					Payload payload = token.GetPayload();
					
					// Get the text of this term.  Term can either
					// provide a String token or offset into a char[]
					// array
					char[] tokenText = token.TermBuffer();
					int tokenTextLen = token.TermLength();
					
					int code = 0;
					
					// Compute hashcode
					int downto = tokenTextLen;
					while (downto > 0)
						code = (code * 31) + tokenText[--downto];
					
					// System.out.println("  addPosition: buffer=" + new String(tokenText, 0, tokenTextLen) + " pos=" + position + " offsetStart=" + (offset+token.startOffset()) + " offsetEnd=" + (offset + token.endOffset()) + " docID=" + docID + " doPos=" + doVectorPositions + " doOffset=" + doVectorOffsets);
					
					int hashPos = code & postingsHashMask;
					
					System.Diagnostics.Debug.Assert(!postingsCompacted);
					
					// Locate Posting in hash
					Enclosing_Instance.p = postingsHash[hashPos];
					
					if (Enclosing_Instance.p != null && !Enclosing_Instance.PostingEquals(tokenText, tokenTextLen))
					{
						// Conflict: keep searching different locations in
						// the hash table.
						int inc = ((code >> 8) + code) | 1;
						do 
						{
							code += inc;
							hashPos = code & postingsHashMask;
							Enclosing_Instance.p = postingsHash[hashPos];
						}
						while (Enclosing_Instance.p != null && !Enclosing_Instance.PostingEquals(tokenText, tokenTextLen));
					}
					
					int proxCode;
					
					// If we hit an exception below, it's possible the
					// posting list or term vectors data will be
					// partially written and thus inconsistent if
					// flushed, so we have to abort all documents
					// since the last flush:
					
					try
					{
						
						if (Enclosing_Instance.p != null)
						{
							// term seen since last flush
							
							if (Enclosing_Instance.docID != Enclosing_Instance.p.lastDocID)
							{
								// term not yet seen in this doc
								
								// System.out.println("    seen before (new docID=" + docID + ") freqUpto=" + p.freqUpto +" proxUpto=" + p.proxUpto);

								System.Diagnostics.Debug.Assert(Enclosing_Instance.p.docFreq > 0);
								
								// Now that we know doc freq for previous doc,
								// write it & lastDocCode
								Enclosing_Instance.freqUpto = Enclosing_Instance.p.freqUpto & Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_MASK;
								Enclosing_Instance.freq = Enclosing_Instance.postingsPool.buffers[Enclosing_Instance.p.freqUpto >> Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SHIFT];
								if (1 == Enclosing_Instance.p.docFreq)
									Enclosing_Instance.WriteFreqVInt(Enclosing_Instance.p.lastDocCode | 1);
								else
								{
									Enclosing_Instance.WriteFreqVInt(Enclosing_Instance.p.lastDocCode);
									Enclosing_Instance.WriteFreqVInt(Enclosing_Instance.p.docFreq);
								}
								Enclosing_Instance.p.freqUpto = Enclosing_Instance.freqUpto + (Enclosing_Instance.p.freqUpto & Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_NOT_MASK);
								
								if (doVectors)
								{
									Enclosing_Instance.vector = AddNewVector();
									if (doVectorOffsets)
									{
										offsetStartCode = offsetStart = offset + token.StartOffset();
										offsetEnd = offset + token.EndOffset();
									}
								}
								
								proxCode = position;
								
								Enclosing_Instance.p.docFreq = 1;
								
								// Store code so we can write this after we're
								// done with this new doc
								Enclosing_Instance.p.lastDocCode = (Enclosing_Instance.docID - Enclosing_Instance.p.lastDocID) << 1;
								Enclosing_Instance.p.lastDocID = Enclosing_Instance.docID;
							}
							else
							{
								// term already seen in this doc
								// System.out.println("    seen before (same docID=" + docID + ") proxUpto=" + p.proxUpto);
								Enclosing_Instance.p.docFreq++;
								
								proxCode = position - Enclosing_Instance.p.lastPosition;
								
								if (doVectors)
								{
									Enclosing_Instance.vector = Enclosing_Instance.p.vector;
									if (Enclosing_Instance.vector == null)
										Enclosing_Instance.vector = AddNewVector();
									if (doVectorOffsets)
									{
										offsetStart = offset + token.StartOffset();
										offsetEnd = offset + token.EndOffset();
										offsetStartCode = offsetStart - Enclosing_Instance.vector.lastOffset;
									}
								}
							}
						}
						else
						{
							// term not seen before
							// System.out.println("    never seen docID=" + docID);
							
							// Refill?
							if (0 == Enclosing_Instance.postingsFreeCount)
							{
								Enclosing_Instance.Enclosing_Instance.GetPostings(Enclosing_Instance.postingsFreeList);
								Enclosing_Instance.postingsFreeCount = Enclosing_Instance.postingsFreeList.Length;
							}
							
							int textLen1 = 1 + tokenTextLen;
							if (textLen1 + Enclosing_Instance.charPool.byteUpto > Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SIZE)
							{
								if (textLen1 > Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SIZE)
								{
									// Just skip this term, to remain as robust as
									// possible during indexing.  A TokenFilter
									// can be inserted into the analyzer chain if
									// other behavior is wanted (pruning the term
									// to a prefix, throwing an exception, etc).
									if (Enclosing_Instance.maxTermPrefix == null)
										Enclosing_Instance.maxTermPrefix = new System.String(tokenText, 0, 30);
									
									// Still increment position:
									position++;
									return ;
								}
								Enclosing_Instance.charPool.NextBuffer();
							}
							char[] text = Enclosing_Instance.charPool.buffer;
							int textUpto = Enclosing_Instance.charPool.byteUpto;
							
							// Pull next free Posting from free list
							Enclosing_Instance.p = Enclosing_Instance.postingsFreeList[--Enclosing_Instance.postingsFreeCount];
							
							Enclosing_Instance.p.textStart = textUpto + Enclosing_Instance.charPool.byteOffset;
							Enclosing_Instance.charPool.byteUpto += textLen1;
							
							Array.Copy(tokenText, 0, text, textUpto, tokenTextLen);
							
							text[textUpto + tokenTextLen] = (char) (0xffff);
							
							System.Diagnostics.Debug.Assert(postingsHash [hashPos] == null);
							
							postingsHash[hashPos] = Enclosing_Instance.p;
							numPostings++;
							
							if (numPostings == postingsHashHalfSize)
								RehashPostings(2 * postingsHashSize);
							
							// Init first slice for freq & prox streams
							int firstSize = Lucene.Net.Index.DocumentsWriter.levelSizeArray[0];
							
							int upto1 = Enclosing_Instance.postingsPool.NewSlice(firstSize);
							Enclosing_Instance.p.freqStart = Enclosing_Instance.p.freqUpto = Enclosing_Instance.postingsPool.byteOffset + upto1;
							
							int upto2 = Enclosing_Instance.postingsPool.NewSlice(firstSize);
							Enclosing_Instance.p.proxStart = Enclosing_Instance.p.proxUpto = Enclosing_Instance.postingsPool.byteOffset + upto2;
							
							Enclosing_Instance.p.lastDocCode = Enclosing_Instance.docID << 1;
							Enclosing_Instance.p.lastDocID = Enclosing_Instance.docID;
							Enclosing_Instance.p.docFreq = 1;
							
							if (doVectors)
							{
								Enclosing_Instance.vector = AddNewVector();
								if (doVectorOffsets)
								{
									offsetStart = offsetStartCode = offset + token.StartOffset();
									offsetEnd = offset + token.EndOffset();
								}
							}
							
							proxCode = position;
						}
						
						Enclosing_Instance.proxUpto = Enclosing_Instance.p.proxUpto & Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_MASK;
						Enclosing_Instance.prox = Enclosing_Instance.postingsPool.buffers[Enclosing_Instance.p.proxUpto >> Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SHIFT];
						System.Diagnostics.Debug.Assert(Enclosing_Instance.prox != null);
						
						if (payload != null && payload.length > 0)
						{
							Enclosing_Instance.WriteProxVInt((proxCode << 1) | 1);
							Enclosing_Instance.WriteProxVInt(payload.length);
							Enclosing_Instance.WriteProxBytes(payload.data, payload.offset, payload.length);
							fieldInfo.storePayloads = true;
						}
						else
							Enclosing_Instance.WriteProxVInt(proxCode << 1);
						
						Enclosing_Instance.p.proxUpto = Enclosing_Instance.proxUpto + (Enclosing_Instance.p.proxUpto & Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_NOT_MASK);
						
						Enclosing_Instance.p.lastPosition = position++;
						
						if (doVectorPositions)
						{
							Enclosing_Instance.posUpto = Enclosing_Instance.vector.posUpto & Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_MASK;
							Enclosing_Instance.pos = Enclosing_Instance.vectorsPool.buffers[Enclosing_Instance.vector.posUpto >> Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SHIFT];
							Enclosing_Instance.WritePosVInt(proxCode);
							Enclosing_Instance.vector.posUpto = Enclosing_Instance.posUpto + (Enclosing_Instance.vector.posUpto & Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_NOT_MASK);
						}
						
						if (doVectorOffsets)
						{
							Enclosing_Instance.offsetUpto = Enclosing_Instance.vector.offsetUpto & Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_MASK;
							Enclosing_Instance.offsets = Enclosing_Instance.vectorsPool.buffers[Enclosing_Instance.vector.offsetUpto >> Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SHIFT];
							Enclosing_Instance.WriteOffsetVInt(offsetStartCode);
							Enclosing_Instance.WriteOffsetVInt(offsetEnd - offsetStart);
							Enclosing_Instance.vector.lastOffset = offsetEnd;
							Enclosing_Instance.vector.offsetUpto = Enclosing_Instance.offsetUpto + (Enclosing_Instance.vector.offsetUpto & Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_NOT_MASK);
						}
					}
					catch (System.Exception t)
					{
						throw new AbortException(t, Enclosing_Instance.Enclosing_Instance);
					}
				}
				
				/// <summary>Called when postings hash is too small (> 50%
				/// occupied) or too large (< 20% occupied). 
				/// </summary>
				internal void  RehashPostings(int newSize)
				{
					
					int newMask = newSize - 1;
					
					Posting[] newHash = new Posting[newSize];
					for (int i = 0; i < postingsHashSize; i++)
					{
						Posting p0 = postingsHash[i];
						if (p0 != null)
						{
							int start = p0.textStart & Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_MASK;
							char[] text = Enclosing_Instance.charPool.buffers[p0.textStart >> Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SHIFT];
							int pos = start;
							while (text[pos] != 0xffff)
								pos++;
							int code = 0;
							while (pos > start)
								code = (code * 31) + text[--pos];
							
							int hashPos = code & newMask;
							System.Diagnostics.Debug.Assert(hashPos >= 0);
							if (newHash[hashPos] != null)
							{
								int inc = ((code >> 8) + code) | 1;
								do 
								{
									code += inc;
									hashPos = code & newMask;
								}
								while (newHash[hashPos] != null);
							}
							newHash[hashPos] = p0;
						}
					}
					
					postingsHashMask = newMask;
					postingsHash = newHash;
					postingsHashSize = newSize;
					postingsHashHalfSize = newSize >> 1;
				}
				
				internal ByteSliceReader vectorSliceReader = new ByteSliceReader();
				
				/// <summary>Called once per field per document if term vectors
				/// are enabled, to write the vectors to *
				/// RAMOutputStream, which is then quickly flushed to
				/// * the real term vectors files in the Directory. 
				/// </summary>
				internal void  WriteVectors(FieldInfo fieldInfo)
				{
					
					System.Diagnostics.Debug.Assert(fieldInfo.storeTermVector);
					
					Enclosing_Instance.vectorFieldNumbers[Enclosing_Instance.numVectorFields] = fieldInfo.number;
					Enclosing_Instance.vectorFieldPointers[Enclosing_Instance.numVectorFields] = Enclosing_Instance.tvfLocal.GetFilePointer();
					Enclosing_Instance.numVectorFields++;
					
					int numPostingsVectors = postingsVectorsUpto;
					
					Enclosing_Instance.tvfLocal.WriteVInt(numPostingsVectors);
					byte bits = (byte) (0x0);
					if (doVectorPositions)
						bits |= TermVectorsReader.STORE_POSITIONS_WITH_TERMVECTOR;
					if (doVectorOffsets)
						bits |= TermVectorsReader.STORE_OFFSET_WITH_TERMVECTOR;
					Enclosing_Instance.tvfLocal.WriteByte(bits);
					
					Enclosing_Instance.DoVectorSort(Enclosing_Instance.postingsVectors, numPostingsVectors);
					
					Posting lastPosting = null;
					
					ByteSliceReader reader = vectorSliceReader;
					
					for (int j = 0; j < numPostingsVectors; j++)
					{
						PostingVector vector = Enclosing_Instance.postingsVectors[j];
						Posting posting = vector.p;
						int freq = posting.docFreq;
						
						int prefix;
						char[] text2 = Enclosing_Instance.charPool.buffers[posting.textStart >> Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SHIFT];
						int start2 = posting.textStart & Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_MASK;
						int pos2 = start2;
						
						// Compute common prefix between last term and
						// this term
						if (lastPosting == null)
							prefix = 0;
						else
						{
							char[] text1 = Enclosing_Instance.charPool.buffers[lastPosting.textStart >> Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SHIFT];
							int start1 = lastPosting.textStart & Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_MASK;
							int pos1 = start1;
							while (true)
							{
								char c1 = text1[pos1];
								char c2 = text2[pos2];
								if (c1 != c2 || c1 == 0xffff)
								{
									prefix = pos1 - start1;
									break;
								}
								pos1++;
								pos2++;
							}
						}
						lastPosting = posting;
						
						// Compute length
						while (text2[pos2] != 0xffff)
							pos2++;
						
						int suffix = pos2 - start2 - prefix;
						Enclosing_Instance.tvfLocal.WriteVInt(prefix);
						Enclosing_Instance.tvfLocal.WriteVInt(suffix);
						Enclosing_Instance.tvfLocal.WriteChars(text2, start2 + prefix, suffix);
						Enclosing_Instance.tvfLocal.WriteVInt(freq);
						
						if (doVectorPositions)
						{
							reader.Init(Enclosing_Instance.vectorsPool, vector.posStart, vector.posUpto);
							reader.WriteTo(Enclosing_Instance.tvfLocal);
						}
						
						if (doVectorOffsets)
						{
							reader.Init(Enclosing_Instance.vectorsPool, vector.offsetStart, vector.offsetUpto);
							reader.WriteTo(Enclosing_Instance.tvfLocal);
						}
					}
				}
			}
		}
		
		private static readonly byte defaultNorm;
		
		/// <summary>Write norms in the "true" segment format.  This is
		/// called only during commit, to create the .nrm file. 
		/// </summary>
		internal void  WriteNorms(System.String segmentName, int totalNumDoc)
		{
			
			IndexOutput normsOut = directory.CreateOutput(segmentName + "." + IndexFileNames.NORMS_EXTENSION);
			
			try
			{
				normsOut.WriteBytes(SegmentMerger.NORMS_HEADER, 0, SegmentMerger.NORMS_HEADER.Length);
				
				int numField = fieldInfos.Size();
				
				for (int fieldIdx = 0; fieldIdx < numField; fieldIdx++)
				{
					FieldInfo fi = fieldInfos.FieldInfo(fieldIdx);
					if (fi.isIndexed && !fi.omitNorms)
					{
						BufferedNorms n = norms[fieldIdx];
						long v;
						if (n == null)
							v = 0;
						else
						{
							v = n.out_Renamed.GetFilePointer();
							n.out_Renamed.WriteTo(normsOut);
							n.Reset();
						}
						if (v < totalNumDoc)
							FillBytes(normsOut, defaultNorm, (int) (totalNumDoc - v));
					}
				}
			}
			finally
			{
				normsOut.Close();
			}
		}
		
		private DefaultSkipListWriter skipListWriter = null;
		
		private bool currentFieldStorePayloads;
		
		/// <summary>Creates a segment from all Postings in the Postings
		/// hashes across all ThreadStates & FieldDatas. 
		/// </summary>
		private System.Collections.IList WriteSegment()
		{
			
			System.Diagnostics.Debug.Assert(AllThreadsIdle());
			
			System.Diagnostics.Debug.Assert(nextDocID == numDocsInRAM);
			
			System.String segmentName;
			
			segmentName = segment;
			
			TermInfosWriter termsOut = new TermInfosWriter(directory, segmentName, fieldInfos, writer.GetTermIndexInterval());
			
			IndexOutput freqOut = directory.CreateOutput(segmentName + ".frq");
			IndexOutput proxOut = directory.CreateOutput(segmentName + ".prx");
			
			// Gather all FieldData's that have postings, across all
			// ThreadStates
			System.Collections.ArrayList allFields = new System.Collections.ArrayList();
			System.Diagnostics.Debug.Assert(AllThreadsIdle());
			for (int i = 0; i < threadStates.Length; i++)
			{
				ThreadState state = threadStates[i];
				state.TrimFields();
				int numFields = state.numAllFieldData;
				for (int j = 0; j < numFields; j++)
				{
					ThreadState.FieldData fp = state.allFieldDataArray[j];
					if (fp.numPostings > 0)
						allFields.Add(fp);
				}
			}
			
			// Sort by field name
			allFields.Sort();
			int numAllFields = allFields.Count;
			
			skipListWriter = new DefaultSkipListWriter(termsOut.skipInterval, termsOut.maxSkipLevels, numDocsInRAM, freqOut, proxOut);
			
			int start = 0;
			while (start < numAllFields)
			{
				
				System.String fieldName = ((ThreadState.FieldData) allFields[start]).fieldInfo.name;
				
				int end = start + 1;
				while (end < numAllFields && ((ThreadState.FieldData) allFields[end]).fieldInfo.name.Equals(fieldName))
					end++;
				
				ThreadState.FieldData[] fields = new ThreadState.FieldData[end - start];
				for (int i = start; i < end; i++)
					fields[i - start] = (ThreadState.FieldData) allFields[i];
				
				// If this field has postings then add them to the
				// segment
				AppendPostings(fields, termsOut, freqOut, proxOut);
				
				for (int i = 0; i < fields.Length; i++)
					fields[i].ResetPostingArrays();
				
				start = end;
			}
			
			freqOut.Close();
			proxOut.Close();
			termsOut.Close();
			
			// Record all files we have flushed
			System.Collections.IList flushedFiles = new System.Collections.ArrayList();
			flushedFiles.Add(SegmentFileName(IndexFileNames.FIELD_INFOS_EXTENSION));
			flushedFiles.Add(SegmentFileName(IndexFileNames.FREQ_EXTENSION));
			flushedFiles.Add(SegmentFileName(IndexFileNames.PROX_EXTENSION));
			flushedFiles.Add(SegmentFileName(IndexFileNames.TERMS_EXTENSION));
			flushedFiles.Add(SegmentFileName(IndexFileNames.TERMS_INDEX_EXTENSION));
			
			if (hasNorms)
			{
				WriteNorms(segmentName, numDocsInRAM);
				flushedFiles.Add(SegmentFileName(IndexFileNames.NORMS_EXTENSION));
			}
			
			if (infoStream != null)
			{
				long newSegmentSize = SegmentSize(segmentName);
				System.String message = String.Format(nf, "  oldRAMSize={0:d} newFlushedSize={1:d} docs/MB={2:f} new/old={3:%}",
					new Object[] { numBytesUsed, newSegmentSize, (numDocsInRAM / (newSegmentSize / 1024.0 / 1024.0)), (newSegmentSize / numBytesUsed) });
				infoStream.WriteLine(message);
			}
			
			ResetPostingsData();
			
			nextDocID = 0;
			nextWriteDocID = 0;
			numDocsInRAM = 0;
			files = null;
			
			// Maybe downsize postingsFreeList array
			if (postingsFreeList.Length > 1.5 * postingsFreeCount)
			{
				int newSize = postingsFreeList.Length;
				while (newSize > 1.25 * postingsFreeCount)
				{
					newSize = (int) (newSize * 0.8);
				}
				Posting[] newArray = new Posting[newSize];
				Array.Copy(postingsFreeList, 0, newArray, 0, postingsFreeCount);
				postingsFreeList = newArray;
			}
			
			return flushedFiles;
		}
		
		/// <summary>Returns the name of the file with this extension, on
		/// the current segment we are working on. 
		/// </summary>
		private System.String SegmentFileName(System.String extension)
		{
			return segment + "." + extension;
		}
		
		private TermInfo termInfo = new TermInfo(); // minimize consing
		
		/// <summary>Used to merge the postings from multiple ThreadStates
		/// when creating a segment 
		/// </summary>
		internal sealed class FieldMergeState
		{
			
			internal ThreadState.FieldData field;
			
			internal Posting[] postings;
			
			private Posting p;
			internal char[] text;
			internal int textOffset;
			
			private int postingUpto = - 1;
			
			private ByteSliceReader freq = new ByteSliceReader();
			internal ByteSliceReader prox = new ByteSliceReader();

			internal int docID;
			internal int termFreq;
			
			internal bool NextTerm()
			{
				postingUpto++;
				if (postingUpto == field.numPostings)
					return false;
				
				p = postings[postingUpto];
				docID = 0;
				
				text = field.threadState.charPool.buffers[p.textStart >> Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SHIFT];
				textOffset = p.textStart & Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_MASK;
				
				if (p.freqUpto > p.freqStart)
					freq.Init(field.threadState.postingsPool, p.freqStart, p.freqUpto);
				else
					freq.bufferOffset = freq.upto = freq.endIndex = 0;
				
				prox.Init(field.threadState.postingsPool, p.proxStart, p.proxUpto);
				
				// Should always be true
				bool result = NextDoc();
				System.Diagnostics.Debug.Assert(result);
				
				return true;
			}
			
			public bool NextDoc()
			{
				if (freq.bufferOffset + freq.upto == freq.endIndex)
				{
					if (p.lastDocCode != - 1)
					{
						// Return last doc
						docID = p.lastDocID;
						termFreq = p.docFreq;
						p.lastDocCode = - 1;
						return true;
					}
					// EOF
					else
						return false;
				}
				
				int code = freq.ReadVInt();
				docID += SupportClass.Number.URShift(code, 1);
				if ((code & 1) != 0)
					termFreq = 1;
				else
					termFreq = freq.ReadVInt();
				
				return true;
			}
		}
		
		internal int CompareText(char[] text1, int pos1, char[] text2, int pos2)
		{
			while (true)
			{
				char c1 = text1[pos1++];
				char c2 = text2[pos2++];
				if (c1 < c2)
					if (0xffff == c2)
						return 1;
					else
						return - 1;
				else if (c2 < c1)
					if (0xffff == c1)
						return - 1;
					else
						return 1;
				else if (0xffff == c1)
					return 0;
			}
		}
		
		/* Walk through all unique text tokens (Posting
		* instances) found in this field and serialize them
		* into a single RAM segment. */
		internal void  AppendPostings(ThreadState.FieldData[] fields, TermInfosWriter termsOut, IndexOutput freqOut, IndexOutput proxOut)
		{
			
			int fieldNumber = fields[0].fieldInfo.number;
			int numFields = fields.Length;
			
			FieldMergeState[] mergeStates = new FieldMergeState[numFields];
			
			for (int i = 0; i < numFields; i++)
			{
				FieldMergeState fms = mergeStates[i] = new FieldMergeState();
				fms.field = fields[i];
				fms.postings = fms.field.SortPostings();
				
				System.Diagnostics.Debug.Assert(fms.field.fieldInfo == fields [0].fieldInfo);
				
				// Should always be true
				bool result = fms.NextTerm();
				System.Diagnostics.Debug.Assert(result);
			}
			
			int skipInterval = termsOut.skipInterval;
			currentFieldStorePayloads = fields[0].fieldInfo.storePayloads;
			
			FieldMergeState[] termStates = new FieldMergeState[numFields];
			
			while (numFields > 0)
			{
				
				// Get the next term to merge
				termStates[0] = mergeStates[0];
				int numToMerge = 1;
				
				for (int i = 1; i < numFields; i++)
				{
					char[] text = mergeStates[i].text;
					int textOffset = mergeStates[i].textOffset;
					int cmp = CompareText(text, textOffset, termStates[0].text, termStates[0].textOffset);
					
					if (cmp < 0)
					{
						termStates[0] = mergeStates[i];
						numToMerge = 1;
					}
					else if (cmp == 0)
						termStates[numToMerge++] = mergeStates[i];
				}
				
				int df = 0;
				int lastPayloadLength = - 1;
				
				int lastDoc = 0;
				
				char[] text2 = termStates[0].text;
				int start = termStates[0].textOffset;
				int pos = start;
				while (text2[pos] != 0xffff)
					pos++;
				
				long freqPointer = freqOut.GetFilePointer();
				long proxPointer = proxOut.GetFilePointer();
				
				skipListWriter.ResetSkip();
				
				// Now termStates has numToMerge FieldMergeStates
				// which all share the same term.  Now we must
				// interleave the docID streams.
				while (numToMerge > 0)
				{
					
					if ((++df % skipInterval) == 0)
					{
						skipListWriter.SetSkipData(lastDoc, currentFieldStorePayloads, lastPayloadLength);
						skipListWriter.BufferSkip(df);
					}
					
					FieldMergeState minState = termStates[0];
					for (int i = 1; i < numToMerge; i++)
						if (termStates[i].docID < minState.docID)
							minState = termStates[i];
					
					int doc = minState.docID;
					int termDocFreq = minState.termFreq;
					
					System.Diagnostics.Debug.Assert(doc < numDocsInRAM);
					System.Diagnostics.Debug.Assert(doc > lastDoc || df == 1);
					
					int newDocCode = (doc - lastDoc) << 1;
					lastDoc = doc;
					
					ByteSliceReader prox = minState.prox;
					
					// Carefully copy over the prox + payload info,
					// changing the format to match Lucene's segment
					// format.
					for (int j = 0; j < termDocFreq; j++)
					{
						int code = prox.ReadVInt();
						if (currentFieldStorePayloads)
						{
							int payloadLength;
							if ((code & 1) != 0)
							{
								// This position has a payload
								payloadLength = prox.ReadVInt();
							}
							else
								payloadLength = 0;
							if (payloadLength != lastPayloadLength)
							{
								proxOut.WriteVInt(code | 1);
								proxOut.WriteVInt(payloadLength);
								lastPayloadLength = payloadLength;
							}
							else
								proxOut.WriteVInt(code & (~ 1));
							if (payloadLength > 0)
								CopyBytes(prox, proxOut, payloadLength);
						}
						else
						{
							System.Diagnostics.Debug.Assert(0 ==(code & 1));
							proxOut.WriteVInt(code >> 1);
						}
					}
					
					if (1 == termDocFreq)
					{
						freqOut.WriteVInt(newDocCode | 1);
					}
					else
					{
						freqOut.WriteVInt(newDocCode);
						freqOut.WriteVInt(termDocFreq);
					}
					
					if (!minState.NextDoc())
					{
						
						// Remove from termStates
						int upto = 0;
						for (int i = 0; i < numToMerge; i++)
							if (termStates[i] != minState)
								termStates[upto++] = termStates[i];
						numToMerge--;
						System.Diagnostics.Debug.Assert(upto == numToMerge);
						
						// Advance this state to the next term
						
						if (!minState.NextTerm())
						{
							// OK, no more terms, so remove from mergeStates
							// as well
							upto = 0;
							for (int i = 0; i < numFields; i++)
								if (mergeStates[i] != minState)
									mergeStates[upto++] = mergeStates[i];
							numFields--;
							System.Diagnostics.Debug.Assert(upto == numFields);
						}
					}
				}
				
				System.Diagnostics.Debug.Assert(df > 0);
				
				// Done merging this term
				
				long skipPointer = skipListWriter.WriteSkip(freqOut);
				
				// Write term
				termInfo.Set(df, freqPointer, proxPointer, (int) (skipPointer - freqPointer));
				termsOut.Add(fieldNumber, text2, start, pos - start, termInfo);
			}
		}
		
		internal void  Close()
		{
			lock (this)
			{
				closed = true;
				System.Threading.Monitor.PulseAll(this);
			}
		}
		
		/// <summary>Returns a free (idle) ThreadState that may be used for
		/// indexing this one document.  This call also pauses if a
		/// flush is pending.  If delTerm is non-null then we
		/// buffer this deleted term after the thread state has
		/// been acquired. 
		/// </summary>
		internal ThreadState GetThreadState(Document doc, Term delTerm)
		{
			lock (this)
			{
				
				// First, find a thread state.  If this thread already
				// has affinity to a specific ThreadState, use that one
				// again.
				ThreadState state = (ThreadState) threadBindings[SupportClass.ThreadClass.Current()];
				if (state == null)
				{
					// First time this thread has called us since last flush
					ThreadState minThreadState = null;
					for (int i = 0; i < threadStates.Length; i++)
					{
						ThreadState ts = threadStates[i];
						if (minThreadState == null || ts.numThreads < minThreadState.numThreads)
							minThreadState = ts;
					}
					if (minThreadState != null && (minThreadState.numThreads == 0 || threadStates.Length == MAX_THREAD_STATE))
					{
						state = minThreadState;
						state.numThreads++;
					}
					else
					{
						// Just create a new "private" thread state
						ThreadState[] newArray = new ThreadState[1 + threadStates.Length];
						if (threadStates.Length > 0)
							Array.Copy(threadStates, 0, newArray, 0, threadStates.Length);
						state = newArray[threadStates.Length] = new ThreadState(this);
						threadStates = newArray;
					}
					threadBindings[SupportClass.ThreadClass.Current()] = state;
				}
				
				// Next, wait until my thread state is idle (in case
				// it's shared with other threads) and for threads to
				// not be paused nor a flush pending:
				while (!closed && (!state.isIdle || pauseThreads != 0 || flushPending || abortCount > 0))
					try
					{
						System.Threading.Monitor.Wait(this);
					}
					catch (System.Threading.ThreadInterruptedException)
					{
						SupportClass.ThreadClass.Current().Interrupt();
					}
				
				if (closed)
					throw new AlreadyClosedException("this IndexWriter is closed");
				
				if (segment == null)
					segment = writer.NewSegmentName();
				
				state.isIdle = false;
				
				try
				{
					bool success = false;
					try
					{
						state.Init(doc, nextDocID);
						if (delTerm != null)
						{
							AddDeleteTerm(delTerm, state.docID);
							state.doFlushAfter = TimeToFlushDeletes();
						}
						// Only increment nextDocID and numDocsInRAM on successful init
						nextDocID++;
                        numDocsInRAM++;

                        // We must at this point commit to flushing to ensure we
                        // always get N docs when we flush by doc count, even if
                        // > 1 thread is adding documents:
                        if (!flushPending && maxBufferedDocs != IndexWriter.DISABLE_AUTO_FLUSH && numDocsInRAM >= maxBufferedDocs)
                        {
                            flushPending = true;
                            state.doFlushAfter = true;
                        }

                        success = true;
					}
					finally
					{
						if (!success)
						{
							// Forcefully idle this ThreadState:
							state.isIdle = true;
							System.Threading.Monitor.PulseAll(this);
							if (state.doFlushAfter)
							{
								state.doFlushAfter = false;
								flushPending = false;
							}
						}
					}
				}
				catch (AbortException ae)
				{
					Abort(ae);
				}
				
				return state;
			}
		}
		
		/// <summary>Returns true if the caller (IndexWriter) should now
		/// flush. 
		/// </summary>
		internal bool AddDocument(Document doc, Analyzer analyzer)
		{
			return UpdateDocument(doc, analyzer, null);
		}
		
		internal bool UpdateDocument(Term t, Document doc, Analyzer analyzer)
		{
			return UpdateDocument(doc, analyzer, t);
		}
		
		internal bool UpdateDocument(Document doc, Analyzer analyzer, Term delTerm)
		{
			
			// This call is synchronized but fast
			ThreadState state = GetThreadState(doc, delTerm);
			try
			{
				bool success = false;
				try
				{
					try
					{
						// This call is not synchronized and does all the work
						state.ProcessDocument(analyzer);
					}
					finally
					{
						// This call is synchronized but fast
						FinishDocument(state);
					}
					success = true;
				}
				finally
				{
					if (!success)
					{
						lock (this)
						{
                            // If this thread state had decided to flush, we
                            // must clear is so another thread can flush
                            if (state.doFlushAfter)
                            {
                                state.doFlushAfter = false;
                                flushPending = false;
                                System.Threading.Monitor.PulseAll(this);
                            }

							// Immediately mark this document as deleted
							// since likely it was partially added.  This
							// keeps indexing as "all or none" (atomic) when
							// adding a document:
							AddDeleteDocID(state.docID);
						}
					}
				}
			}
			catch (AbortException ae)
			{
				Abort(ae);
			}
			
			return state.doFlushAfter || TimeToFlushDeletes();
		}
		
		internal int GetNumBufferedDeleteTerms()
		{
			lock (this)
			{
				return numBufferedDeleteTerms;
			}
		}
		
		internal System.Collections.Hashtable GetBufferedDeleteTerms()
		{
			lock (this)
			{
				return bufferedDeleteTerms;
			}
		}
		
		internal System.Collections.IList GetBufferedDeleteDocIDs()
		{
			lock (this)
			{
				return bufferedDeleteDocIDs;
			}
		}
		
		// Reset buffered deletes.
		internal void  ClearBufferedDeletes()
		{
			lock (this)
			{
				bufferedDeleteTerms.Clear();
				bufferedDeleteDocIDs.Clear();
				numBufferedDeleteTerms = 0;
				if (numBytesUsed > 0)
					ResetPostingsData();
			}
		}
		
		internal bool BufferDeleteTerms(Term[] terms)
		{
			lock (this)
			{
				while (pauseThreads != 0 || flushPending)
					try
					{
						System.Threading.Monitor.Wait(this);
					}
					catch (System.Threading.ThreadInterruptedException)
					{
						SupportClass.ThreadClass.Current().Interrupt();
					}
				for (int i = 0; i < terms.Length; i++)
					AddDeleteTerm(terms[i], numDocsInRAM);
				return TimeToFlushDeletes();
			}
		}
		
		internal bool BufferDeleteTerm(Term term)
		{
			lock (this)
			{
				while (pauseThreads != 0 || flushPending)
					try
					{
						System.Threading.Monitor.Wait(this);
					}
					catch (System.Threading.ThreadInterruptedException)
					{
						SupportClass.ThreadClass.Current().Interrupt();
					}
				AddDeleteTerm(term, numDocsInRAM);
				return TimeToFlushDeletes();
			}
		}
		
		private bool TimeToFlushDeletes()
		{
			lock (this)
			{
				return (bufferIsFull || (maxBufferedDeleteTerms != IndexWriter.DISABLE_AUTO_FLUSH && numBufferedDeleteTerms >= maxBufferedDeleteTerms)) && SetFlushPending();
			}
		}
		
		internal void  SetMaxBufferedDeleteTerms(int maxBufferedDeleteTerms)
		{
			this.maxBufferedDeleteTerms = maxBufferedDeleteTerms;
		}
		
		internal int GetMaxBufferedDeleteTerms()
		{
			return maxBufferedDeleteTerms;
		}
		
		internal bool HasDeletes()
		{
			lock (this)
			{
				return bufferedDeleteTerms.Count > 0 || bufferedDeleteDocIDs.Count > 0;
			}
		}
		
		// Number of documents a delete term applies to.
		internal class Num
		{
			private int num;
			
			internal Num(int num)
			{
				this.num = num;
			}
			
			internal int GetNum()
			{
				return num;
			}
			
			internal void  SetNum(int num)
			{
				// Only record the new number if it's greater than the
				// current one.  This is important because if multiple
				// threads are replacing the same doc at nearly the
				// same time, it's possible that one thread that got a
				// higher docID is scheduled before the other
				// threads.
				if (num > this.num)
					this.num = num;
			}
		}
		
		// Buffer a term in bufferedDeleteTerms, which records the
		// current number of documents buffered in ram so that the
		// delete term will be applied to those documents as well
		// as the disk segments.
		private void  AddDeleteTerm(Term term, int docCount)
		{
			lock (this)
			{
				Num num = (Num) bufferedDeleteTerms[term];
				if (num == null)
				{
					bufferedDeleteTerms[term] = new Num(docCount);
					// This is coarse approximation of actual bytes used:
					numBytesUsed += (term.Field().Length + term.Text().Length) * BYTES_PER_CHAR + 4 + 5 * OBJECT_HEADER_BYTES + 5 * OBJECT_POINTER_BYTES;
					if (ramBufferSize != IndexWriter.DISABLE_AUTO_FLUSH && numBytesUsed > ramBufferSize)
					{
						bufferIsFull = true;
					}
				}
				else
				{
					num.SetNum(docCount);
				}
				numBufferedDeleteTerms++;
			}
		}
		
		// Buffer a specific docID for deletion.  Currently only
		// used when we hit a exception when adding a document
		private void  AddDeleteDocID(int docId)
		{
			lock (this)
			{
				bufferedDeleteDocIDs.Add((System.Int32) docId);
				numBytesUsed += OBJECT_HEADER_BYTES + BYTES_PER_INT + OBJECT_POINTER_BYTES;
			}
		}
		
		/// <summary>Does the synchronized work to finish/flush the
		/// inverted document. 
		/// </summary>
		private void  FinishDocument(ThreadState state)
		{
			lock (this)
			{
				if (abortCount > 0)
				{
					// Forcefully idle this threadstate -- its state will
					// be reset by abort()
					state.isIdle = true;
					System.Threading.Monitor.PulseAll(this);
					return ;
				}
				
				// Now write the indexed document to the real files.
				if (nextWriteDocID == state.docID)
				{
					// It's my turn, so write everything now:
					nextWriteDocID++;
					state.WriteDocument();
					state.isIdle = true;
					System.Threading.Monitor.PulseAll(this);
					
					// If any states were waiting on me, sweep through and
					// flush those that are enabled by my write.
					if (numWaiting > 0)
					{
						bool any = true;
						while (any)
						{
							any = false;
							for (int i = 0; i < numWaiting; )
							{
								ThreadState s = waitingThreadStates[i];
								if (s.docID == nextWriteDocID)
								{
									s.WriteDocument();
									s.isIdle = true;
									nextWriteDocID++;
									any = true;
									if (numWaiting > i + 1)
									// Swap in the last waiting state to fill in
									// the hole we just created.  It's important
									// to do this as-we-go and not at the end of
									// the loop, because if we hit an aborting
									// exception in one of the s.writeDocument
									// calls (above), it leaves this array in an
									// inconsistent state:
										waitingThreadStates[i] = waitingThreadStates[numWaiting - 1];
									numWaiting--;
								}
								else
								{
									System.Diagnostics.Debug.Assert(!s.isIdle);
									i++;
								}
							}
						}
					}
				}
				else
				{
					// Another thread got a docID before me, but, it
					// hasn't finished its processing.  So add myself to
					// the line but don't hold up this thread.
					waitingThreadStates[numWaiting++] = state;
				}
			}
		}
		
		internal long GetRAMUsed()
		{
			return numBytesUsed;
		}
		
		internal long numBytesAlloc;
		internal long numBytesUsed;

		internal System.Globalization.NumberFormatInfo nf = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;
		
		/* Used only when writing norms to fill in default norm
		* value into the holes in docID stream for those docs
		* that didn't have this field. */
		internal static void  FillBytes(IndexOutput out_Renamed, byte b, int numBytes)
		{
			for (int i = 0; i < numBytes; i++)
				out_Renamed.WriteByte(b);
		}
		
		internal byte[] copyByteBuffer = new byte[4096];
		
		/// <summary>Copy numBytes from srcIn to destIn </summary>
		internal void  CopyBytes(IndexInput srcIn, IndexOutput destIn, long numBytes)
		{
			// TODO: we could do this more efficiently (save a copy)
			// because it's always from a ByteSliceReader ->
			// IndexOutput
			while (numBytes > 0)
			{
				int chunk;
				if (numBytes > 4096)
					chunk = 4096;
				else
					chunk = (int) numBytes;
				srcIn.ReadBytes(copyByteBuffer, 0, chunk);
				destIn.WriteBytes(copyByteBuffer, 0, chunk);
				numBytes -= chunk;
			}
		}
		
		/* Stores norms, buffered in RAM, until they are flushed
		* to a partial segment. */
		private class BufferedNorms
		{
			
			internal RAMOutputStream out_Renamed;
			internal int upto;
			
			internal BufferedNorms()
			{
				out_Renamed = new RAMOutputStream();
			}
			
			internal void  Add(float norm)
			{
				byte b = Similarity.EncodeNorm(norm);
				out_Renamed.WriteByte(b);
				upto++;
			}
			
			internal void  Reset()
			{
				out_Renamed.Reset();
				upto = 0;
			}
			
			internal void  Fill(int docID)
			{
				// Must now fill in docs that didn't have this
				// field.  Note that this is how norms can consume
				// tremendous storage when the docs have widely
				// varying different fields, because we are not
				// storing the norms sparsely (see LUCENE-830)
				if (upto < docID)
				{
					Lucene.Net.Index.DocumentsWriter.FillBytes(out_Renamed, Lucene.Net.Index.DocumentsWriter.defaultNorm, docID - upto);
					upto = docID;
				}
			}
		}
		
		/* Simple StringReader that can be reset to a new string;
		* we use this when tokenizing the string value from a
		* Field. */
		sealed internal class ReusableStringReader : System.IO.StringReader
		{
			internal ReusableStringReader() : base("")
			{
			}
			
			internal int upto;
			internal int left;
			internal System.String s;
			internal void  Init(System.String s)
			{
				this.s = s;
				left = s.Length;
				this.upto = 0;
			}
			public int Read(char[] c)
			{
				return Read(c, 0, c.Length);
			}
			public  override int Read(System.Char[] c, int off, int len)
			{
				if (left > len)
				{
					DocumentsWriter.GetCharsFromString(s, upto, upto + len, c, off);
					upto += len;
					left -= len;
					return len;
				}
				else if (0 == left)
				{
					return - 1;
				}
				else
				{
					DocumentsWriter.GetCharsFromString(s, upto, upto + left, c, off);
					int r = left;
					left = 0;
					upto = s.Length;
					return r;
				}
			}
			public override void  Close()
			{
			}
            public override string ReadToEnd()
            {
                if (left == 0) return null;
                left = 0;
                return s;
            }
		}
		
		/* IndexInput that knows how to read the byte slices written
		* by Posting and PostingVector.  We read the bytes in
		* each slice until we hit the end of that slice at which
		* point we read the forwarding address of the next slice
		* and then jump to it.*/
		sealed internal class ByteSliceReader:IndexInput
		{
			internal ByteBlockPool pool;
			internal int bufferUpto;
			internal byte[] buffer;
			public int upto;
			internal int limit;
			internal int level;
			public int bufferOffset;
			
			public int endIndex;
			
			public void  Init(ByteBlockPool pool, int startIndex, int endIndex)
			{
				
				System.Diagnostics.Debug.Assert(endIndex - startIndex > 0);
				
				this.pool = pool;
				this.endIndex = endIndex;
				
				level = 0;
				bufferUpto = startIndex / Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SIZE;
				bufferOffset = bufferUpto * Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SIZE;
				buffer = pool.buffers[bufferUpto];
				upto = startIndex & Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_MASK;
				
				int firstSize = Lucene.Net.Index.DocumentsWriter.levelSizeArray[0];
				
				if (startIndex + firstSize >= endIndex)
				{
					// There is only this one slice to read
					limit = endIndex & Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_MASK;
				}
				else
					limit = upto + firstSize - 4;
			}
			
			public override byte ReadByte()
			{
				// Assert that we are not @ EOF
				System.Diagnostics.Debug.Assert(upto + bufferOffset < endIndex);
				if (upto == limit)
					NextSlice();
				return buffer[upto++];
			}
			
			public long WriteTo(IndexOutput out_Renamed)
			{
				long size = 0;
				while (true)
				{
					if (limit + bufferOffset == endIndex)
					{
						System.Diagnostics.Debug.Assert(endIndex - bufferOffset >= upto);
						out_Renamed.WriteBytes(buffer, upto, limit - upto);
						size += limit - upto;
						break;
					}
					else
					{
						out_Renamed.WriteBytes(buffer, upto, limit - upto);
						size += limit - upto;
						NextSlice();
					}
				}
				
				return size;
			}
			
			public void  NextSlice()
			{
				
				// Skip to our next slice
				int nextIndex = ((buffer[limit] & 0xff) << 24) + ((buffer[1 + limit] & 0xff) << 16) + ((buffer[2 + limit] & 0xff) << 8) + (buffer[3 + limit] & 0xff);
				
				level = Lucene.Net.Index.DocumentsWriter.nextLevelArray[level];
				int newSize = Lucene.Net.Index.DocumentsWriter.levelSizeArray[level];
				
				bufferUpto = nextIndex / Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SIZE;
				bufferOffset = bufferUpto * Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SIZE;
				
				buffer = pool.buffers[bufferUpto];
				upto = nextIndex & Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_MASK;
				
				if (nextIndex + newSize >= endIndex)
				{
					// We are advancing to the final slice
					System.Diagnostics.Debug.Assert(endIndex - nextIndex > 0);
					limit = endIndex - bufferOffset;
				}
				else
				{
					// This is not the final slice (subtract 4 for the
					// forwarding address at the end of this new slice)
					limit = upto + newSize - 4;
				}
			}
			
			public override void  ReadBytes(byte[] b, int offset, int len)
			{
				while (len > 0)
				{
					int numLeft = limit - upto;
					if (numLeft < len)
					{
						// Read entire slice
						Array.Copy(buffer, upto, b, offset, numLeft);
						offset += numLeft;
						len -= numLeft;
						NextSlice();
					}
					else
					{
						// This slice is the last one
						Array.Copy(buffer, upto, b, offset, len);
						upto += len;
						break;
					}
				}
			}
			
			public override long GetFilePointer()
			{
				throw new System.SystemException("not implemented");
			}
			public override long Length()
			{
				throw new System.SystemException("not implemented");
			}
			public override void  Seek(long pos)
			{
				throw new System.SystemException("not implemented");
			}
			public override void  Close()
			{
				throw new System.SystemException("not implemented");
			}
			override public System.Object Clone()
			{
				return null;
			}
		}
		
		// Size of each slice.  These arrays should be at most 16
		// elements.  First array is just a compact way to encode
		// X+1 with a max.  Second array is the length of each
		// slice, ie first slice is 5 bytes, next slice is 14
		// bytes, etc.
		internal static readonly int[] nextLevelArray = new int[]{1, 2, 3, 4, 5, 6, 7, 8, 9, 9};
		internal static readonly int[] levelSizeArray = new int[]{5, 14, 20, 30, 40, 40, 80, 80, 120, 200};
		
		/* Class that Posting and PostingVector use to write byte
		* streams into shared fixed-size byte[] arrays.  The idea
		* is to allocate slices of increasing lengths For
		* example, the first slice is 5 bytes, the next slice is
		* 14, etc.  We start by writing our bytes into the first
		* 5 bytes.  When we hit the end of the slice, we allocate
		* the next slice and then write the address of the new
		* slice into the last 4 bytes of the previous slice (the
		* "forwarding address").
		*
		* Each slice is filled with 0's initially, and we mark
		* the end with a non-zero byte.  This way the methods
		* that are writing into the slice don't need to record
		* its length and instead allocate a new slice once they
		* hit a non-zero byte. */
		sealed internal class ByteBlockPool
		{
            private bool trackAllocations;

			public ByteBlockPool(bool trackAllocations, DocumentsWriter enclosingInstance)
			{
                trackAllocations = trackAllocations;
				InitBlock(enclosingInstance);
			}

			private void  InitBlock(DocumentsWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
				byteUpto = Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SIZE;
			}
			private DocumentsWriter enclosingInstance;
			public DocumentsWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			public byte[][] buffers = new byte[10][];
			
			internal int bufferUpto = - 1; // Which buffer we are upto
			public int byteUpto; // Where we are in head buffer
			
			public byte[] buffer; // Current head buffer
			public int byteOffset = - Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SIZE; // Current head offset
			
			public void  Reset()
			{
				if (bufferUpto != - 1)
				{
					// We allocated at least one buffer
					
					for (int i = 0; i < bufferUpto; i++)
						// Fully zero fill buffers that we fully used
						Array.Clear(buffers[i], 0, buffers[i].Length);
					
					// Partial zero fill the final buffer
					Array.Clear(buffers[bufferUpto], 0, byteUpto);
					
					if (bufferUpto > 0)
					// Recycle all but the first buffer
						Enclosing_Instance.RecycleByteBlocks(buffers, 1, 1 + bufferUpto);
					
					// Re-use the first buffer
					bufferUpto = 0;
					byteUpto = 0;
					byteOffset = 0;
					buffer = buffers[0];
				}
			}
			
			public void  NextBuffer()
			{
				if (1 + bufferUpto == buffers.GetLength(0))
				{
					byte[][] newBuffers = new byte[(int) (buffers.GetLength(0) * 1.5)][];
					Array.Copy(buffers, 0, newBuffers, 0, buffers.GetLength(0));
					buffers = newBuffers;
				}
				buffer = buffers[1 + bufferUpto] = Enclosing_Instance.GetByteBlock(trackAllocations);
				bufferUpto++;
				
				byteUpto = 0;
				byteOffset += Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SIZE;
			}
			
			public int NewSlice(int size)
			{
				if (byteUpto > Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SIZE - size)
					NextBuffer();
				int upto = byteUpto;
				byteUpto += size;
				buffer[byteUpto - 1] = 16;
				return upto;
			}
			
			public int AllocSlice(byte[] slice, int upto)
			{
				
				int level = slice[upto] & 15;
				int newLevel = Lucene.Net.Index.DocumentsWriter.nextLevelArray[level];
				int newSize = Lucene.Net.Index.DocumentsWriter.levelSizeArray[newLevel];
				
				// Maybe allocate another block
				if (byteUpto > Lucene.Net.Index.DocumentsWriter.BYTE_BLOCK_SIZE - newSize)
					NextBuffer();
				
				int newUpto = byteUpto;
				int offset = newUpto + byteOffset;
				byteUpto += newSize;
				
				// Copy forward the past 3 bytes (which we are about
				// to overwrite with the forwarding address):
				buffer[newUpto] = slice[upto - 3];
				buffer[newUpto + 1] = slice[upto - 2];
				buffer[newUpto + 2] = slice[upto - 1];
				
				// Write forwarding address at end of last slice:
				slice[upto - 3] = (byte) (SupportClass.Number.URShift(offset, 24));
				slice[upto - 2] = (byte) (SupportClass.Number.URShift(offset, 16));
				slice[upto - 1] = (byte) (SupportClass.Number.URShift(offset, 8));
				slice[upto] = (byte) offset;
				
				// Write new level:
				buffer[byteUpto - 1] = (byte) (16 | newLevel);
				
				return newUpto + 3;
			}
		}
		
		sealed internal class CharBlockPool
		{
			public CharBlockPool(DocumentsWriter enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(DocumentsWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
				byteUpto = Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SIZE;
			}
			private DocumentsWriter enclosingInstance;
			public DocumentsWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			public char[][] buffers = new char[10][];
			
			internal int bufferUpto = - 1; // Which buffer we are upto
			public int byteUpto; // Where we are in head buffer
			
			public char[] buffer; // Current head buffer
			public int byteOffset = - Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SIZE; // Current head offset
			
			public void  Reset()
			{
				Enclosing_Instance.RecycleCharBlocks(buffers, 1 + bufferUpto);
				bufferUpto = - 1;
				byteUpto = Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SIZE;
				byteOffset = - Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SIZE;
			}
			
			public void  NextBuffer()
			{
				if (1 + bufferUpto == buffers.GetLength(0))
				{
					char[][] newBuffers = new char[(int) (buffers.GetLength(0) * 1.5)][];
					Array.Copy(buffers, 0, newBuffers, 0, buffers.GetLength(0));
					buffers = newBuffers;
				}
				buffer = buffers[1 + bufferUpto] = Enclosing_Instance.GetCharBlock();
				bufferUpto++;
				
				byteUpto = 0;
				byteOffset += Lucene.Net.Index.DocumentsWriter.CHAR_BLOCK_SIZE;
			}
		}
		
		// Used only when infoStream != null
		private long SegmentSize(System.String segmentName)
		{
			System.Diagnostics.Debug.Assert(infoStream != null);
			
			long size = directory.FileLength(segmentName + ".tii") + directory.FileLength(segmentName + ".tis") + directory.FileLength(segmentName + ".frq") + directory.FileLength(segmentName + ".prx");
			
			System.String normFileName = segmentName + ".nrm";
			if (directory.FileExists(normFileName))
				size += directory.FileLength(normFileName);
			
			return size;
		}
		
		private const int POINTER_NUM_BYTE = 4;
		private const int INT_NUM_BYTE = 4;
		private const int CHAR_NUM_BYTE = 2;
		
		// Why + 5*POINTER_NUM_BYTE below?
		//   1: Posting has "vector" field which is a pointer
		//   2: Posting is referenced by postingsFreeList array
		//   3,4,5: Posting is referenced by postings hash, which
		//          targets 25-50% fill factor; approximate this
		//          as 3X # pointers
		internal static readonly int POSTING_NUM_BYTE = OBJECT_HEADER_BYTES + 9 * INT_NUM_BYTE + 5 * POINTER_NUM_BYTE;
		
		// Holds free pool of Posting instances
		private Posting[] postingsFreeList;
		private int postingsFreeCount;
		private int postingsAllocCount;
		
		/* Allocate more Postings from shared pool */
		internal void  GetPostings(Posting[] postings)
		{
			lock (this)
			{
				numBytesUsed += postings.Length * POSTING_NUM_BYTE;
				int numToCopy;
				if (postingsFreeCount < postings.Length)
					numToCopy = postingsFreeCount;
				else
					numToCopy = postings.Length;
				int start = postingsFreeCount - numToCopy;
				Array.Copy(postingsFreeList, start, postings, 0, numToCopy);
				postingsFreeCount -= numToCopy;
				
				// Directly allocate the remainder if any
				if (numToCopy < postings.Length)
				{
					int extra = postings.Length - numToCopy;
					int newPostingsAllocCount = postingsAllocCount + extra;
					if (newPostingsAllocCount > postingsFreeList.Length)
					{
						postingsFreeList = new Posting[(int) (1.25 * newPostingsAllocCount)];
					}
					
					BalanceRAM();
					for (int i = numToCopy; i < postings.Length; i++)
					{
						postings[i] = new Posting();
						numBytesAlloc += POSTING_NUM_BYTE;
						postingsAllocCount++;
					}
				}
			}
		}
		
		internal void  RecyclePostings(Posting[] postings, int numPostings)
		{
			lock (this)
			{
				// Move all Postings from this ThreadState back to our
				// free list.  We pre-allocated this array while we were
				// creating Postings to make sure it's large enough
				System.Diagnostics.Debug.Assert(postingsFreeCount + numPostings <= postingsFreeList.Length);
				Array.Copy(postings, 0, postingsFreeList, postingsFreeCount, numPostings);
				postingsFreeCount += numPostings;
			}
		}
		
		/* Initial chunks size of the shared byte[] blocks used to
		store postings data */
		internal const int BYTE_BLOCK_SHIFT = 15;
		internal static readonly int BYTE_BLOCK_SIZE = (int) System.Math.Pow(2.0, BYTE_BLOCK_SHIFT);
		internal static readonly int BYTE_BLOCK_MASK = BYTE_BLOCK_SIZE - 1;
		internal static readonly int BYTE_BLOCK_NOT_MASK = ~ BYTE_BLOCK_MASK;
		
		private System.Collections.ArrayList freeByteBlocks = new System.Collections.ArrayList();
		
		/* Allocate another byte[] from the shared pool */
		internal byte[] GetByteBlock(bool trackAllocations)
		{
			lock (this)
			{
				int size = freeByteBlocks.Count;
				byte[] b;
				if (0 == size)
				{
					numBytesAlloc += BYTE_BLOCK_SIZE;
					BalanceRAM();
					b = new byte[BYTE_BLOCK_SIZE];
				}
				else
				{
					System.Object tempObject;
					tempObject = freeByteBlocks[size - 1];
					freeByteBlocks.RemoveAt(size - 1);
					b = (byte[]) tempObject;
				}
                if (trackAllocations)
				    numBytesUsed += BYTE_BLOCK_SIZE;
				return b;
			}
		}
		
		/* Return a byte[] to the pool */
		internal void  RecycleByteBlocks(byte[][] blocks, int start, int end)
		{
			lock (this)
			{
				for (int i = start; i < end; i++)
					freeByteBlocks.Add(blocks[i]);
			}
		}
		
		/* Initial chunk size of the shared char[] blocks used to
		store term text */
		internal const int CHAR_BLOCK_SHIFT = 14;
		internal static readonly int CHAR_BLOCK_SIZE = (int) System.Math.Pow(2.0, CHAR_BLOCK_SHIFT);
		internal static readonly int CHAR_BLOCK_MASK = CHAR_BLOCK_SIZE - 1;
		
		internal static readonly int MAX_TERM_LENGTH = CHAR_BLOCK_SIZE - 1;
		
		private System.Collections.ArrayList freeCharBlocks = new System.Collections.ArrayList();
		
		/* Allocate another char[] from the shared pool */
		internal char[] GetCharBlock()
		{
			lock (this)
			{
				int size = freeCharBlocks.Count;
				char[] c;
				if (0 == size)
				{
					numBytesAlloc += CHAR_BLOCK_SIZE * CHAR_NUM_BYTE;
					BalanceRAM();
					c = new char[CHAR_BLOCK_SIZE];
				}
				else
				{
					System.Object tempObject;
					tempObject = freeCharBlocks[size - 1];
					freeCharBlocks.RemoveAt(size - 1);
					c = (char[]) tempObject;
				}
				numBytesUsed += CHAR_BLOCK_SIZE * CHAR_NUM_BYTE;
				return c;
			}
		}
		
		/* Return a char[] to the pool */
		internal void  RecycleCharBlocks(char[][] blocks, int numBlocks)
		{
			lock (this)
			{
				for (int i = 0; i < numBlocks; i++)
					freeCharBlocks.Add(blocks[i]);
			}
		}
		
		internal System.String ToMB(long v)
		{
			return String.Format(nf, "{0:f}", new Object[] { (v / 1024.0 / 1024.0) });
		}
		
		/* We have three pools of RAM: Postings, byte blocks
		* (holds freq/prox posting data) and char blocks (holds
		* characters in the term).  Different docs require
		* varying amount of storage from these three classes.
		* For example, docs with many unique single-occurrence
		* short terms will use up the Postings RAM and hardly any
		* of the other two.  Whereas docs with very large terms
		* will use alot of char blocks RAM and relatively less of
		* the other two.  This method just frees allocations from
		* the pools once we are over-budget, which balances the
		* pools to match the current docs. */
		private void  BalanceRAM()
		{
			lock (this)
			{
				
				if (ramBufferSize == IndexWriter.DISABLE_AUTO_FLUSH || bufferIsFull)
					return ;
				
				// We free our allocations if we've allocated 5% over
				// our allowed RAM buffer
				long freeTrigger = (long) (1.05 * ramBufferSize);
				long freeLevel = (long) (0.95 * ramBufferSize);
				
				// We flush when we've used our target usage
				long flushTrigger = (long) ramBufferSize;
				
				if (numBytesAlloc > freeTrigger)
				{
					if (infoStream != null)
						infoStream.WriteLine("  RAM: now balance allocations: usedMB=" + ToMB(numBytesUsed) + " vs trigger=" + ToMB(flushTrigger) + " allocMB=" + ToMB(numBytesAlloc) + " vs trigger=" + ToMB(freeTrigger) + " postingsFree=" + ToMB(postingsFreeCount * POSTING_NUM_BYTE) + " byteBlockFree=" + ToMB(freeByteBlocks.Count * BYTE_BLOCK_SIZE) + " charBlockFree=" + ToMB(freeCharBlocks.Count * CHAR_BLOCK_SIZE * CHAR_NUM_BYTE));
					
					// When we've crossed 100% of our target Postings
					// RAM usage, try to free up until we're back down
					// to 95%
					long startBytesAlloc = numBytesAlloc;
					
					int postingsFreeChunk = (int) (BYTE_BLOCK_SIZE / POSTING_NUM_BYTE);
					
					int iter = 0;
					
					// We free equally from each pool in 64 KB
					// chunks until we are below our threshold
					// (freeLevel)
					
					while (numBytesAlloc > freeLevel)
					{
						if (0 == freeByteBlocks.Count && 0 == freeCharBlocks.Count && 0 == postingsFreeCount)
						{
							// Nothing else to free -- must flush now.
							bufferIsFull = true;
							if (infoStream != null)
								infoStream.WriteLine("    nothing to free; now set bufferIsFull");
							break;
						}
						
						if ((0 == iter % 3) && freeByteBlocks.Count > 0)
						{
							freeByteBlocks.RemoveAt(freeByteBlocks.Count - 1);
							numBytesAlloc -= BYTE_BLOCK_SIZE;
						}
						
						if ((1 == iter % 3) && freeCharBlocks.Count > 0)
						{
							freeCharBlocks.RemoveAt(freeCharBlocks.Count - 1);
							numBytesAlloc -= CHAR_BLOCK_SIZE * CHAR_NUM_BYTE;
						}
						
						if ((2 == iter % 3) && postingsFreeCount > 0)
						{
							int numToFree;
							if (postingsFreeCount >= postingsFreeChunk)
								numToFree = postingsFreeChunk;
							else
								numToFree = postingsFreeCount;
							Array.Clear(postingsFreeList, postingsFreeCount - numToFree, numToFree);
							postingsFreeCount -= numToFree;
							postingsAllocCount -= numToFree;
							numBytesAlloc -= numToFree * POSTING_NUM_BYTE;
						}
						
						iter++;
					}
					
					if (infoStream != null)
						infoStream.WriteLine(String.Format("    after free: freedMB={0:f} usedMB={1:f} allocMB={2:f}",
							new Object[] { ((startBytesAlloc - numBytesAlloc) / 1024.0 / 1024.0), (numBytesUsed / 1024.0 / 1024.0), (numBytesAlloc / 1024.0 / 1024.0) }));
				}
				else
				{
					// If we have not crossed the 100% mark, but have
					// crossed the 95% mark of RAM we are actually
					// using, go ahead and flush.  This prevents
					// over-allocating and then freeing, with every
					// flush.
					if (numBytesUsed > flushTrigger)
					{
						if (infoStream != null)
							infoStream.WriteLine(String.Format(nf, "  RAM: now flush @ usedMB={0:f} allocMB={1:f} triggerMB={2:f}",
								new Object[] { (numBytesUsed / 1024.0 / 1024.0), (numBytesAlloc / 1024.0 / 1024.0), (flushTrigger / 1024.0 / 1024.0) }));
						
						bufferIsFull = true;
					}
				}
			}
		}
		
		/* Used to track postings for a single term.  One of these
		* exists per unique term seen since the last flush. */
		sealed internal class Posting
		{
			internal int textStart; // Address into char[] blocks where our text is stored
			internal int docFreq; // # times this term occurs in the current doc
			internal int freqStart; // Address of first byte[] slice for freq
			internal int freqUpto; // Next write address for freq
			internal int proxStart; // Address of first byte[] slice
			internal int proxUpto; // Next write address for prox
			internal int lastDocID; // Last docID where this term occurred
			internal int lastDocCode; // Code for prior doc
			internal int lastPosition; // Last position where this term occurred
			internal PostingVector vector; // Corresponding PostingVector instance
		}
		
		/* Used to track data for term vectors.  One of these
		* exists per unique term seen in each field in the
		* document. */
		sealed internal class PostingVector
		{
			internal Posting p; // Corresponding Posting instance for this term
			internal int lastOffset; // Last offset we saw
			internal int offsetStart; // Address of first slice for offsets
			internal int offsetUpto; // Next write address for offsets
			internal int posStart; // Address of first slice for positions
			internal int posUpto; // Next write address for positions
		}
		static DocumentsWriter()
		{
			defaultNorm = Similarity.EncodeNorm(1.0f);
		}
		
		/// <summary>
		/// Copies an array of chars obtained from a String into a specified array of chars
		/// </summary>
		/// <param name="sourceString">The String to get the chars from</param>
		/// <param name="sourceStart">Position of the String to start getting the chars</param>
		/// <param name="sourceEnd">Position of the String to end getting the chars</param>
		/// <param name="destinationArray">Array to return the chars</param>
		/// <param name="destinationStart">Position of the destination array of chars to start storing the chars</param>
		/// <returns>An array of chars</returns>
		static internal void GetCharsFromString(System.String sourceString, int sourceStart, int sourceEnd, char[] destinationArray, int destinationStart)
		{
			int sourceCounter;
			int destinationCounter;
			sourceCounter = sourceStart;
			destinationCounter = destinationStart;
			while (sourceCounter < sourceEnd)
			{
				destinationArray[destinationCounter] = (char)sourceString[sourceCounter];
				sourceCounter++;
				destinationCounter++;
			}
		}
	}
	
	// Used only internally to DW to call abort "up the stack"
	[Serializable]
	class AbortException : System.IO.IOException
	{
		public AbortException(System.Exception cause, DocumentsWriter docWriter) : base("", cause)
		{
			docWriter.SetAborting();
		}
	}
}
