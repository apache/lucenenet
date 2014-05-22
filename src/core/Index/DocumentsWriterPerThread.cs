using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Index
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements. See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License. You may obtain a copy of the License at
	 *
	 * http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Util.ByteBlockPool.BYTE_BLOCK_MASK;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Util.ByteBlockPool.BYTE_BLOCK_SIZE;


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using Codec = Lucene.Net.Codecs.Codec;
	using DeleteSlice = Lucene.Net.Index.DocumentsWriterDeleteQueue.DeleteSlice;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using Directory = Lucene.Net.Store.Directory;
	using FlushInfo = Lucene.Net.Store.FlushInfo;
	using IOContext = Lucene.Net.Store.IOContext;
	using TrackingDirectoryWrapper = Lucene.Net.Store.TrackingDirectoryWrapper;
	using Allocator = Lucene.Net.Util.ByteBlockPool.Allocator;
	using DirectTrackingAllocator = Lucene.Net.Util.ByteBlockPool.DirectTrackingAllocator;
	using Constants = Lucene.Net.Util.Constants;
	using Counter = Lucene.Net.Util.Counter;
	using InfoStream = Lucene.Net.Util.InfoStream;
	using IntBlockPool = Lucene.Net.Util.IntBlockPool;
	using MutableBits = Lucene.Net.Util.MutableBits;
	using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

	internal class DocumentsWriterPerThread
	{

	  /// <summary>
	  /// The IndexingChain must define the <seealso cref="#getChain(DocumentsWriterPerThread)"/> method
	  /// which returns the DocConsumer that the DocumentsWriter calls to process the
	  /// documents.
	  /// </summary>
	  internal abstract class IndexingChain
	  {
		internal abstract DocConsumer GetChain(DocumentsWriterPerThread documentsWriterPerThread);
	  }


	  internal static readonly IndexingChain defaultIndexingChain = new IndexingChainAnonymousInnerClassHelper();

	  private class IndexingChainAnonymousInnerClassHelper : IndexingChain
	  {
		  public IndexingChainAnonymousInnerClassHelper()
		  {
		  }


		  internal override DocConsumer GetChain(DocumentsWriterPerThread documentsWriterPerThread)
		  {
			/*
			this is the current indexing chain:
	  
			DocConsumer / DocConsumerPerThread
			  --> code: DocFieldProcessor
			    --> DocFieldConsumer / DocFieldConsumerPerField
			      --> code: DocFieldConsumers / DocFieldConsumersPerField
			        --> code: DocInverter / DocInverterPerField
			          --> InvertedDocConsumer / InvertedDocConsumerPerField
			            --> code: TermsHash / TermsHashPerField
			              --> TermsHashConsumer / TermsHashConsumerPerField
			                --> code: FreqProxTermsWriter / FreqProxTermsWriterPerField
			                --> code: TermVectorsTermsWriter / TermVectorsTermsWriterPerField
			          --> InvertedDocEndConsumer / InvertedDocConsumerPerField
			            --> code: NormsConsumer / NormsConsumerPerField
			    --> StoredFieldsConsumer
			      --> TwoStoredFieldConsumers
			        -> code: StoredFieldsProcessor
			        -> code: DocValuesProcessor
		  */

		  // Build up indexing chain:

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TermsHashConsumer termVectorsWriter = new TermVectorsConsumer(documentsWriterPerThread);
			TermsHashConsumer termVectorsWriter = new TermVectorsConsumer(documentsWriterPerThread);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TermsHashConsumer freqProxWriter = new FreqProxTermsWriter();
			TermsHashConsumer freqProxWriter = new FreqProxTermsWriter();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final InvertedDocConsumer termsHash = new TermsHash(documentsWriterPerThread, freqProxWriter, true, new TermsHash(documentsWriterPerThread, termVectorsWriter, false, null));
			InvertedDocConsumer termsHash = new TermsHash(documentsWriterPerThread, freqProxWriter, true, new TermsHash(documentsWriterPerThread, termVectorsWriter, false, null));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final NormsConsumer normsWriter = new NormsConsumer();
			NormsConsumer normsWriter = new NormsConsumer();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final DocInverter docInverter = new DocInverter(documentsWriterPerThread.docState, termsHash, normsWriter);
			DocInverter docInverter = new DocInverter(documentsWriterPerThread.DocState, termsHash, normsWriter);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final StoredFieldsConsumer storedFields = new TwoStoredFieldsConsumers(new StoredFieldsProcessor(documentsWriterPerThread), new DocValuesProcessor(documentsWriterPerThread.bytesUsed));
			StoredFieldsConsumer storedFields = new TwoStoredFieldsConsumers(new StoredFieldsProcessor(documentsWriterPerThread), new DocValuesProcessor(documentsWriterPerThread.BytesUsed_Renamed));
			return new DocFieldProcessor(documentsWriterPerThread, docInverter, storedFields);
		  }
	  }

	  internal class DocState
	  {
		internal readonly DocumentsWriterPerThread DocWriter;
		internal Analyzer Analyzer;
		internal InfoStream InfoStream;
		internal Similarity Similarity;
		internal int DocID;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: Iterable<? extends IndexableField> doc;
		internal IEnumerable<?> Doc;
		internal string MaxTermPrefix;

		internal DocState(DocumentsWriterPerThread docWriter, InfoStream infoStream)
		{
		  this.DocWriter = docWriter;
		  this.InfoStream = infoStream;
		}

		// Only called by asserts
		public virtual bool TestPoint(string name)
		{
		  return DocWriter.TestPoint(name);
		}

		public virtual void Clear()
		{
		  // don't hold onto doc nor analyzer, in case it is
		  // largish:
		  Doc = null;
		  Analyzer = null;
		}
	  }

	  internal class FlushedSegment
	  {
		internal readonly SegmentCommitInfo SegmentInfo;
		internal readonly FieldInfos FieldInfos;
		internal readonly FrozenBufferedUpdates SegmentUpdates;
		internal readonly MutableBits LiveDocs;
		internal readonly int DelCount;

		internal FlushedSegment(SegmentCommitInfo segmentInfo, FieldInfos fieldInfos, BufferedUpdates segmentUpdates, MutableBits liveDocs, int delCount)
		{
		  this.SegmentInfo = segmentInfo;
		  this.FieldInfos = fieldInfos;
		  this.SegmentUpdates = segmentUpdates != null && segmentUpdates.Any() ? new FrozenBufferedUpdates(segmentUpdates, true) : null;
		  this.LiveDocs = liveDocs;
		  this.DelCount = delCount;
		}
	  }

	  /// <summary>
	  /// Called if we hit an exception at a bad time (when
	  ///  updating the index files) and must discard all
	  ///  currently buffered docs.  this resets our state,
	  ///  discarding any docs added since last flush. 
	  /// </summary>
	  internal virtual void Abort(Set<string> createdFiles)
	  {
		//System.out.println(Thread.currentThread().getName() + ": now abort seg=" + segmentInfo.name);
		HasAborted = Aborting = true;
		try
		{
		  if (InfoStream.IsEnabled("DWPT"))
		  {
			InfoStream.Message("DWPT", "now abort");
		  }
		  try
		  {
			Consumer.Abort();
		  }
		  catch (Exception t)
		  {
		  }

		  PendingUpdates.Clear();
		  createdFiles.addAll(Directory.CreatedFiles);
		}
		finally
		{
		  Aborting = false;
		  if (InfoStream.IsEnabled("DWPT"))
		  {
			InfoStream.Message("DWPT", "done abort");
		  }
		}
	  }
	  private const bool INFO_VERBOSE = false;
	  internal readonly Codec Codec;
	  internal readonly TrackingDirectoryWrapper Directory;
	  internal readonly Directory DirectoryOrig;
	  internal readonly DocState DocState;
	  internal readonly DocConsumer Consumer;
	  internal readonly Counter BytesUsed_Renamed;

	  internal SegmentWriteState FlushState;
	  // Updates for our still-in-RAM (to be flushed next) segment
	  internal readonly BufferedUpdates PendingUpdates;
	  private readonly SegmentInfo SegmentInfo_Renamed; // Current segment we are working on
	  internal bool Aborting = false; // True if an abort is pending
	  internal bool HasAborted = false; // True if the last exception throws by #updateDocument was aborting

	  private FieldInfos.Builder FieldInfos;
	  private readonly InfoStream InfoStream;
	  private int NumDocsInRAM_Renamed;
	  internal readonly DocumentsWriterDeleteQueue DeleteQueue;
	  private readonly DeleteSlice DeleteSlice;
	  private readonly NumberFormat Nf = NumberFormat.getInstance(Locale.ROOT);
	  internal readonly Allocator ByteBlockAllocator;
	  internal readonly IntBlockPool.Allocator IntBlockAllocator;
	  private readonly LiveIndexWriterConfig IndexWriterConfig;


	  public DocumentsWriterPerThread(string segmentName, Directory directory, LiveIndexWriterConfig indexWriterConfig, InfoStream infoStream, DocumentsWriterDeleteQueue deleteQueue, FieldInfos.Builder fieldInfos)
	  {
		this.DirectoryOrig = directory;
		this.Directory = new TrackingDirectoryWrapper(directory);
		this.FieldInfos = fieldInfos;
		this.IndexWriterConfig = indexWriterConfig;
		this.InfoStream = infoStream;
		this.Codec = indexWriterConfig.Codec;
		this.DocState = new DocState(this, infoStream);
		this.DocState.Similarity = indexWriterConfig.Similarity;
		BytesUsed_Renamed = Counter.NewCounter();
		ByteBlockAllocator = new DirectTrackingAllocator(BytesUsed_Renamed);
		PendingUpdates = new BufferedUpdates();
		IntBlockAllocator = new IntBlockAllocator(BytesUsed_Renamed);
		this.DeleteQueue = deleteQueue;
		Debug.Assert(NumDocsInRAM_Renamed == 0, "num docs " + NumDocsInRAM_Renamed);
		PendingUpdates.Clear();
		DeleteSlice = deleteQueue.NewSlice();

		SegmentInfo_Renamed = new SegmentInfo(DirectoryOrig, Constants.LUCENE_MAIN_VERSION, segmentName, -1, false, Codec, null);
		Debug.Assert(NumDocsInRAM_Renamed == 0);
		if (INFO_VERBOSE && infoStream.IsEnabled("DWPT"))
		{
		  infoStream.Message("DWPT", Thread.CurrentThread.Name + " init seg=" + segmentName + " delQueue=" + deleteQueue);
		}
		// this should be the last call in the ctor 
		// it really sucks that we need to pull this within the ctor and pass this ref to the chain!
		Consumer = indexWriterConfig.IndexingChain.GetChain(this);

	  }

	  internal virtual void SetAborting()
	  {
		Aborting = true;
	  }

	  internal virtual bool CheckAndResetHasAborted()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean retval = hasAborted;
		bool retval = HasAborted;
		HasAborted = false;
		return retval;
	  }

	  internal bool TestPoint(string message)
	  {
		if (InfoStream.IsEnabled("TP"))
		{
		  InfoStream.Message("TP", message);
		}
		return true;
	  }

	  public virtual void updateDocument<T1>(IEnumerable<T1> doc, Analyzer analyzer, Term delTerm) where T1 : IndexableField
	  {
		Debug.Assert(TestPoint("DocumentsWriterPerThread addDocument start"));
		Debug.Assert(DeleteQueue != null);
		DocState.Doc = doc;
		DocState.Analyzer = analyzer;
		DocState.DocID = NumDocsInRAM_Renamed;
		if (INFO_VERBOSE && InfoStream.IsEnabled("DWPT"))
		{
		  InfoStream.Message("DWPT", Thread.CurrentThread.Name + " update delTerm=" + delTerm + " docID=" + DocState.DocID + " seg=" + SegmentInfo_Renamed.Name);
		}
		bool success = false;
		try
		{
		  try
		  {
			Consumer.ProcessDocument(FieldInfos);
		  }
		  finally
		  {
			DocState.Clear();
		  }
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			if (!Aborting)
			{
			  // mark document as deleted
			  DeleteDocID(DocState.DocID);
			  NumDocsInRAM_Renamed++;
			}
			else
			{
			  Abort(FilesToDelete);
			}
		  }
		}
		success = false;
		try
		{
		  Consumer.FinishDocument();
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			Abort(FilesToDelete);
		  }
		}
		FinishDocument(delTerm);
	  }

	  public virtual int updateDocuments<T1>(IEnumerable<T1> docs, Analyzer analyzer, Term delTerm) where T1 : Iterable<T1 extends IndexableField>
	  {
		Debug.Assert(TestPoint("DocumentsWriterPerThread addDocuments start"));
		Debug.Assert(DeleteQueue != null);
		DocState.Analyzer = analyzer;
		if (INFO_VERBOSE && InfoStream.IsEnabled("DWPT"))
		{
		  InfoStream.Message("DWPT", Thread.CurrentThread.Name + " update delTerm=" + delTerm + " docID=" + DocState.DocID + " seg=" + SegmentInfo_Renamed.Name);
		}
		int docCount = 0;
		bool allDocsIndexed = false;
		try
		{
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: for(Iterable<? extends IndexableField> doc : docs)
		  foreach (IEnumerable<?> doc in docs)
		  {
			DocState.Doc = doc;
			DocState.DocID = NumDocsInRAM_Renamed;
			docCount++;

			bool success = false;
			try
			{
			  Consumer.ProcessDocument(FieldInfos);
			  success = true;
			}
			finally
			{
			  if (!success)
			  {
				// An exc is being thrown...
				if (!Aborting)
				{
				  // Incr here because finishDocument will not
				  // be called (because an exc is being thrown):
				  NumDocsInRAM_Renamed++;
				}
				else
				{
				  Abort(FilesToDelete);
				}
			  }
			}
			success = false;
			try
			{
			  Consumer.FinishDocument();
			  success = true;
			}
			finally
			{
			  if (!success)
			  {
				Abort(FilesToDelete);
			  }
			}

			FinishDocument(null);
		  }
		  allDocsIndexed = true;

		  // Apply delTerm only after all indexing has
		  // succeeded, but apply it only to docs prior to when
		  // this batch started:
		  if (delTerm != null)
		  {
			DeleteQueue.Add(delTerm, DeleteSlice);
			Debug.Assert(DeleteSlice.IsTailItem(delTerm), "expected the delete term as the tail item");
			DeleteSlice.Apply(PendingUpdates, NumDocsInRAM_Renamed - docCount);
		  }

		}
		finally
		{
		  if (!allDocsIndexed && !Aborting)
		  {
			// the iterator threw an exception that is not aborting 
			// go and mark all docs from this block as deleted
			int docID = NumDocsInRAM_Renamed - 1;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endDocID = docID - docCount;
			int endDocID = docID - docCount;
			while (docID > endDocID)
			{
			  DeleteDocID(docID);
			  docID--;
			}
		  }
		  DocState.Clear();
		}

		return docCount;
	  }

	  private void FinishDocument(Term delTerm)
	  {
		/*
		 * here we actually finish the document in two steps 1. push the delete into
		 * the queue and update our slice. 2. increment the DWPT private document
		 * id.
		 * 
		 * the updated slice we get from 1. holds all the deletes that have occurred
		 * since we updated the slice the last time.
		 */
		bool applySlice = NumDocsInRAM_Renamed != 0;
		if (delTerm != null)
		{
		  DeleteQueue.Add(delTerm, DeleteSlice);
		  Debug.Assert(DeleteSlice.IsTailItem(delTerm), "expected the delete term as the tail item");
		}
		else
		{
		  applySlice &= DeleteQueue.UpdateSlice(DeleteSlice);
		}

		if (applySlice)
		{
		  DeleteSlice.Apply(PendingUpdates, NumDocsInRAM_Renamed);
		} // if we don't need to apply we must reset!
		else
		{
		  DeleteSlice.Reset();
		}
		++NumDocsInRAM_Renamed;
	  }

	  // Buffer a specific docID for deletion. Currently only
	  // used when we hit an exception when adding a document
	  internal virtual void DeleteDocID(int docIDUpto)
	  {
		PendingUpdates.AddDocID(docIDUpto);
		// NOTE: we do not trigger flush here.  this is
		// potentially a RAM leak, if you have an app that tries
		// to add docs but every single doc always hits a
		// non-aborting exception.  Allowing a flush here gets
		// very messy because we are only invoked when handling
		// exceptions so to do this properly, while handling an
		// exception we'd have to go off and flush new deletes
		// which is risky (likely would hit some other
		// confounding exception).
	  }

	  /// <summary>
	  /// Returns the number of delete terms in this <seealso cref="DocumentsWriterPerThread"/>
	  /// </summary>
	  public virtual int NumDeleteTerms()
	  {
		// public for FlushPolicy
		return PendingUpdates.NumTermDeletes.get();
	  }

	  /// <summary>
	  /// Returns the number of RAM resident documents in this <seealso cref="DocumentsWriterPerThread"/>
	  /// </summary>
	  public virtual int NumDocsInRAM
	  {
		  get
		  {
			// public for FlushPolicy
			return NumDocsInRAM_Renamed;
		  }
	  }


	  /// <summary>
	  /// Prepares this DWPT for flushing. this method will freeze and return the
	  /// <seealso cref="DocumentsWriterDeleteQueue"/>s global buffer and apply all pending
	  /// deletes to this DWPT.
	  /// </summary>
	  internal virtual FrozenBufferedUpdates PrepareFlush()
	  {
		Debug.Assert(NumDocsInRAM_Renamed > 0);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FrozenBufferedUpdates globalUpdates = deleteQueue.freezeGlobalBuffer(deleteSlice);
		FrozenBufferedUpdates globalUpdates = DeleteQueue.FreezeGlobalBuffer(DeleteSlice);
		/* deleteSlice can possibly be null if we have hit non-aborting exceptions during indexing and never succeeded 
		adding a document. */
		if (DeleteSlice != null)
		{
		  // apply all deletes before we flush and release the delete slice
		  DeleteSlice.Apply(PendingUpdates, NumDocsInRAM_Renamed);
		  Debug.Assert(DeleteSlice.Empty);
		  DeleteSlice.Reset();
		}
		return globalUpdates;
	  }

	  /// <summary>
	  /// Flush all pending docs to a new segment </summary>
	  internal virtual FlushedSegment Flush()
	  {
		Debug.Assert(NumDocsInRAM_Renamed > 0);
		Debug.Assert(DeleteSlice.Empty, "all deletes must be applied in prepareFlush");
		SegmentInfo_Renamed.DocCount = NumDocsInRAM_Renamed;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SegmentWriteState flushState = new SegmentWriteState(infoStream, directory, segmentInfo, fieldInfos.finish(), indexWriterConfig.getTermIndexInterval(), pendingUpdates, new Lucene.Net.Store.IOContext(new Lucene.Net.Store.FlushInfo(numDocsInRAM, bytesUsed())));
		SegmentWriteState flushState = new SegmentWriteState(InfoStream, Directory, SegmentInfo_Renamed, FieldInfos.Finish(), IndexWriterConfig.TermIndexInterval, PendingUpdates, new IOContext(new FlushInfo(NumDocsInRAM_Renamed, BytesUsed())));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double startMBUsed = bytesUsed() / 1024.0 / 1024.0;
		double startMBUsed = BytesUsed() / 1024.0 / 1024.0;

		// Apply delete-by-docID now (delete-byDocID only
		// happens when an exception is hit processing that
		// doc, eg if analyzer has some problem w/ the text):
		if (PendingUpdates.DocIDs.Count > 0)
		{
		  flushState.LiveDocs = Codec.LiveDocsFormat().newLiveDocs(NumDocsInRAM_Renamed);
		  foreach (int delDocID in PendingUpdates.DocIDs)
		  {
			flushState.LiveDocs.clear(delDocID);
		  }
		  flushState.DelCountOnFlush = PendingUpdates.DocIDs.Count;
		  PendingUpdates.BytesUsed.addAndGet(-PendingUpdates.DocIDs.Count * BufferedUpdates.BYTES_PER_DEL_DOCID);
		  PendingUpdates.DocIDs.Clear();
		}

		if (Aborting)
		{
		  if (InfoStream.IsEnabled("DWPT"))
		  {
			InfoStream.Message("DWPT", "flush: skip because aborting is set");
		  }
		  return null;
		}

		if (InfoStream.IsEnabled("DWPT"))
		{
		  InfoStream.Message("DWPT", "flush postings as segment " + flushState.SegmentInfo.name + " numDocs=" + NumDocsInRAM_Renamed);
		}

		bool success = false;

		try
		{
		  Consumer.Flush(flushState);
		  PendingUpdates.Terms.Clear();
		  SegmentInfo_Renamed.Files = new HashSet<>(Directory.CreatedFiles);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SegmentCommitInfo segmentInfoPerCommit = new SegmentCommitInfo(segmentInfo, 0, -1L, -1L);
		  SegmentCommitInfo segmentInfoPerCommit = new SegmentCommitInfo(SegmentInfo_Renamed, 0, -1L, -1L);
		  if (InfoStream.IsEnabled("DWPT"))
		  {
			InfoStream.Message("DWPT", "new segment has " + (flushState.LiveDocs == null ? 0 : (flushState.SegmentInfo.DocCount - flushState.DelCountOnFlush)) + " deleted docs");
			InfoStream.Message("DWPT", "new segment has " + (flushState.FieldInfos.hasVectors() ? "vectors" : "no vectors") + "; " + (flushState.FieldInfos.hasNorms() ? "norms" : "no norms") + "; " + (flushState.FieldInfos.hasDocValues() ? "docValues" : "no docValues") + "; " + (flushState.FieldInfos.hasProx() ? "prox" : "no prox") + "; " + (flushState.FieldInfos.hasFreq() ? "freqs" : "no freqs"));
			InfoStream.Message("DWPT", "flushedFiles=" + segmentInfoPerCommit.Files());
			InfoStream.Message("DWPT", "flushed codec=" + Codec);
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final BufferedUpdates segmentDeletes;
		  BufferedUpdates segmentDeletes;
		  if (PendingUpdates.Queries.Count == 0 && PendingUpdates.NumericUpdates.Count == 0 && PendingUpdates.BinaryUpdates.Count == 0)
		  {
			PendingUpdates.Clear();
			segmentDeletes = null;
		  }
		  else
		  {
			segmentDeletes = PendingUpdates;
		  }

		  if (InfoStream.IsEnabled("DWPT"))
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double newSegmentSize = segmentInfoPerCommit.sizeInBytes()/1024.0/1024.0;
			double newSegmentSize = segmentInfoPerCommit.SizeInBytes() / 1024.0 / 1024.0;
			InfoStream.Message("DWPT", "flushed: segment=" + SegmentInfo_Renamed.Name + " ramUsed=" + Nf.format(startMBUsed) + " MB" + " newFlushedSize(includes docstores)=" + Nf.format(newSegmentSize) + " MB" + " docs/MB=" + Nf.format(flushState.SegmentInfo.DocCount / newSegmentSize));
		  }

		  Debug.Assert(SegmentInfo_Renamed != null);

		  FlushedSegment fs = new FlushedSegment(segmentInfoPerCommit, flushState.FieldInfos, segmentDeletes, flushState.LiveDocs, flushState.DelCountOnFlush);
		  SealFlushedSegment(fs);
		  success = true;

		  return fs;
		}
		finally
		{
		  if (!success)
		  {
			Abort(FilesToDelete);
		  }
		}
	  }

	  private readonly Set<string> FilesToDelete = new HashSet<string>();

	  public virtual Set<string> PendingFilesToDelete()
	  {
		return FilesToDelete;
	  }
	  /// <summary>
	  /// Seals the <seealso cref="SegmentInfo"/> for the new flushed segment and persists
	  /// the deleted documents <seealso cref="MutableBits"/>.
	  /// </summary>
	  internal virtual void SealFlushedSegment(FlushedSegment flushedSegment)
	  {
		Debug.Assert(flushedSegment != null);

		SegmentCommitInfo newSegment = flushedSegment.SegmentInfo;

		IndexWriter.SetDiagnostics(newSegment.Info, IndexWriter.SOURCE_FLUSH);

		IOContext context = new IOContext(new FlushInfo(newSegment.Info.DocCount, newSegment.SizeInBytes()));

		bool success = false;
		try
		{

		  if (IndexWriterConfig.UseCompoundFile)
		  {
			FilesToDelete.addAll(IndexWriter.CreateCompoundFile(InfoStream, Directory, MergeState.CheckAbort.NONE, newSegment.Info, context));
			newSegment.Info.UseCompoundFile = true;
		  }

		  // Have codec write SegmentInfo.  Must do this after
		  // creating CFS so that 1) .si isn't slurped into CFS,
		  // and 2) .si reflects useCompoundFile=true change
		  // above:
		  Codec.SegmentInfoFormat().SegmentInfoWriter.write(Directory, newSegment.Info, flushedSegment.FieldInfos, context);

		  // TODO: ideally we would freeze newSegment here!!
		  // because any changes after writing the .si will be
		  // lost... 

		  // Must write deleted docs after the CFS so we don't
		  // slurp the del file into CFS:
		  if (flushedSegment.LiveDocs != null)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int delCount = flushedSegment.delCount;
			int delCount = flushedSegment.DelCount;
			Debug.Assert(delCount > 0);
			if (InfoStream.IsEnabled("DWPT"))
			{
			  InfoStream.Message("DWPT", "flush: write " + delCount + " deletes gen=" + flushedSegment.SegmentInfo.DelGen);
			}

			// TODO: we should prune the segment if it's 100%
			// deleted... but merge will also catch it.

			// TODO: in the NRT case it'd be better to hand
			// this del vector over to the
			// shortly-to-be-opened SegmentReader and let it
			// carry the changes; there's no reason to use
			// filesystem as intermediary here.

			SegmentCommitInfo info = flushedSegment.SegmentInfo;
			Codec codec = info.Info.Codec;
			codec.LiveDocsFormat().writeLiveDocs(flushedSegment.LiveDocs, Directory, info, delCount, context);
			newSegment.DelCount = delCount;
			newSegment.AdvanceDelGen();
		  }

		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			if (InfoStream.IsEnabled("DWPT"))
			{
			  InfoStream.Message("DWPT", "hit exception creating compound file for newly flushed segment " + newSegment.Info.name);
			}
		  }
		}
	  }

	  /// <summary>
	  /// Get current segment info we are writing. </summary>
	  internal virtual SegmentInfo SegmentInfo
	  {
		  get
		  {
			return SegmentInfo_Renamed;
		  }
	  }

	  internal virtual long BytesUsed()
	  {
		return BytesUsed_Renamed.Get() + PendingUpdates.BytesUsed.get();
	  }

	  /* Initial chunks size of the shared byte[] blocks used to
	     store postings data */
	  internal static readonly int BYTE_BLOCK_NOT_MASK = ~BYTE_BLOCK_MASK;

	  /* if you increase this, you must fix field cache impl for
	   * getTerms/getTermsIndex requires <= 32768 */
	  internal static readonly int MAX_TERM_LENGTH_UTF8 = BYTE_BLOCK_SIZE-2;


	  private class IntBlockAllocator : IntBlockPool.Allocator
	  {
		internal readonly Counter BytesUsed;

		public IntBlockAllocator(Counter bytesUsed) : base(IntBlockPool.INT_BLOCK_SIZE)
		{
		  this.BytesUsed = bytesUsed;
		}

		/* Allocate another int[] from the shared pool */
		public override int[] IntBlock
		{
			get
			{
			  int[] b = new int[IntBlockPool.INT_BLOCK_SIZE];
			  BytesUsed.AddAndGet(IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT);
			  return b;
			}
		}

		public override void RecycleIntBlocks(int[][] blocks, int offset, int length)
		{
		  BytesUsed.AddAndGet(-(length * (IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT)));
		}

	  }

	  public override string ToString()
	  {
		return "DocumentsWriterPerThread [pendingDeletes=" + PendingUpdates + ", segment=" + (SegmentInfo_Renamed != null ? SegmentInfo_Renamed.Name : "null") + ", aborting=" + Aborting + ", numDocsInRAM=" + NumDocsInRAM_Renamed + ", deleteQueue=" + DeleteQueue + "]";
	  }

	}

}