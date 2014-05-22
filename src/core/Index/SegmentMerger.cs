using System.Diagnostics;
using System.Collections.Generic;

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


	using Codec = Lucene.Net.Codecs.Codec;
	using FieldInfosWriter = Lucene.Net.Codecs.FieldInfosWriter;
	using FieldsConsumer = Lucene.Net.Codecs.FieldsConsumer;
	using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
	using StoredFieldsWriter = Lucene.Net.Codecs.StoredFieldsWriter;
	using TermVectorsWriter = Lucene.Net.Codecs.TermVectorsWriter;
	using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using Bits = Lucene.Net.Util.Bits;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using InfoStream = Lucene.Net.Util.InfoStream;

	/// <summary>
	/// The SegmentMerger class combines two or more Segments, represented by an
	/// IndexReader, into a single Segment.  Call the merge method to combine the
	/// segments.
	/// </summary>
	/// <seealso cref= #merge </seealso>
	internal sealed class SegmentMerger
	{
	  private readonly Directory Directory;
	  private readonly int TermIndexInterval;

	  private readonly Codec Codec;

	  private readonly IOContext Context;

	  private readonly MergeState MergeState;
	  private readonly FieldInfos.Builder FieldInfosBuilder;

	  // note, just like in codec apis Directory 'dir' is NOT the same as segmentInfo.dir!!
	  internal SegmentMerger(IList<AtomicReader> readers, SegmentInfo segmentInfo, InfoStream infoStream, Directory dir, int termIndexInterval, MergeState.CheckAbort checkAbort, FieldInfos.FieldNumbers fieldNumbers, IOContext context, bool validate)
	  {
		// validate incoming readers
		if (validate)
		{
		  foreach (AtomicReader reader in readers)
		  {
			reader.CheckIntegrity();
		  }
		}
		MergeState = new MergeState(readers, segmentInfo, infoStream, checkAbort);
		Directory = dir;
		this.TermIndexInterval = termIndexInterval;
		this.Codec = segmentInfo.Codec;
		this.Context = context;
		this.FieldInfosBuilder = new FieldInfos.Builder(fieldNumbers);
		MergeState.SegmentInfo.DocCount = SetDocMaps();
	  }

	  /// <summary>
	  /// True if any merging should happen </summary>
	  internal bool ShouldMerge()
	  {
		return MergeState.SegmentInfo.DocCount > 0;
	  }

	  /// <summary>
	  /// Merges the readers into the directory passed to the constructor </summary>
	  /// <returns> The number of documents that were merged </returns>
	  /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
	  /// <exception cref="IOException"> if there is a low-level IO error </exception>
	  internal MergeState Merge()
	  {
		if (!ShouldMerge())
		{
		  throw new IllegalStateException("Merge would result in 0 document segment");
		}
		// NOTE: it's important to add calls to
		// checkAbort.work(...) if you make any changes to this
		// method that will spend alot of time.  The frequency
		// of this check impacts how long
		// IndexWriter.close(false) takes to actually stop the
		// threads.
		MergeFieldInfos();
		SetMatchingSegmentReaders();
		long t0 = 0;
		if (MergeState.InfoStream.isEnabled("SM"))
		{
		  t0 = System.nanoTime();
		}
		int numMerged = MergeFields();
		if (MergeState.InfoStream.isEnabled("SM"))
		{
		  long t1 = System.nanoTime();
		  MergeState.InfoStream.message("SM", ((t1 - t0) / 1000000) + " msec to merge stored fields [" + numMerged + " docs]");
		}
		Debug.Assert(numMerged == MergeState.SegmentInfo.DocCount);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SegmentWriteState segmentWriteState = new SegmentWriteState(mergeState.infoStream, directory, mergeState.segmentInfo, mergeState.fieldInfos, termIndexInterval, null, context);
		SegmentWriteState segmentWriteState = new SegmentWriteState(MergeState.InfoStream, Directory, MergeState.SegmentInfo, MergeState.FieldInfos, TermIndexInterval, null, Context);
		if (MergeState.InfoStream.isEnabled("SM"))
		{
		  t0 = System.nanoTime();
		}
		MergeTerms(segmentWriteState);
		if (MergeState.InfoStream.isEnabled("SM"))
		{
		  long t1 = System.nanoTime();
		  MergeState.InfoStream.message("SM", ((t1 - t0) / 1000000) + " msec to merge postings [" + numMerged + " docs]");
		}

		if (MergeState.InfoStream.isEnabled("SM"))
		{
		  t0 = System.nanoTime();
		}
		if (MergeState.FieldInfos.hasDocValues())
		{
		  MergeDocValues(segmentWriteState);
		}
		if (MergeState.InfoStream.isEnabled("SM"))
		{
		  long t1 = System.nanoTime();
		  MergeState.InfoStream.message("SM", ((t1 - t0) / 1000000) + " msec to merge doc values [" + numMerged + " docs]");
		}

		if (MergeState.FieldInfos.hasNorms())
		{
		  if (MergeState.InfoStream.isEnabled("SM"))
		  {
			t0 = System.nanoTime();
		  }
		  MergeNorms(segmentWriteState);
		  if (MergeState.InfoStream.isEnabled("SM"))
		  {
			long t1 = System.nanoTime();
			MergeState.InfoStream.message("SM", ((t1 - t0) / 1000000) + " msec to merge norms [" + numMerged + " docs]");
		  }
		}

		if (MergeState.FieldInfos.hasVectors())
		{
		  if (MergeState.InfoStream.isEnabled("SM"))
		  {
			t0 = System.nanoTime();
		  }
		  numMerged = MergeVectors();
		  if (MergeState.InfoStream.isEnabled("SM"))
		  {
			long t1 = System.nanoTime();
			MergeState.InfoStream.message("SM", ((t1 - t0) / 1000000) + " msec to merge vectors [" + numMerged + " docs]");
		  }
		  Debug.Assert(numMerged == MergeState.SegmentInfo.DocCount);
		}

		// write the merged infos
		FieldInfosWriter fieldInfosWriter = Codec.FieldInfosFormat().FieldInfosWriter;
		fieldInfosWriter.Write(Directory, MergeState.SegmentInfo.name, "", MergeState.FieldInfos, Context);

		return MergeState;
	  }

	  private void MergeDocValues(SegmentWriteState segmentWriteState)
	  {
		DocValuesConsumer consumer = Codec.DocValuesFormat().fieldsConsumer(segmentWriteState);
		bool success = false;
		try
		{
		  foreach (FieldInfo field in MergeState.FieldInfos)
		  {
			DocValuesType type = field.DocValuesType;
			if (type != null)
			{
			  if (type == DocValuesType.NUMERIC)
			  {
				IList<NumericDocValues> toMerge = new List<NumericDocValues>();
				IList<Bits> docsWithField = new List<Bits>();
				foreach (AtomicReader reader in MergeState.Readers)
				{
				  NumericDocValues values = reader.GetNumericDocValues(field.Name);
				  Bits bits = reader.GetDocsWithField(field.Name);
				  if (values == null)
				  {
					values = DocValues.EMPTY_NUMERIC;
					bits = new Lucene.Net.Util.Bits_MatchNoBits(reader.MaxDoc());
				  }
				  toMerge.Add(values);
				  docsWithField.Add(bits);
				}
				consumer.MergeNumericField(field, MergeState, toMerge, docsWithField);
			  }
			  else if (type == DocValuesType.BINARY)
			  {
				IList<BinaryDocValues> toMerge = new List<BinaryDocValues>();
				IList<Bits> docsWithField = new List<Bits>();
				foreach (AtomicReader reader in MergeState.Readers)
				{
				  BinaryDocValues values = reader.GetBinaryDocValues(field.Name);
				  Bits bits = reader.GetDocsWithField(field.Name);
				  if (values == null)
				  {
					values = DocValues.EMPTY_BINARY;
					bits = new Lucene.Net.Util.Bits_MatchNoBits(reader.MaxDoc());
				  }
				  toMerge.Add(values);
				  docsWithField.Add(bits);
				}
				consumer.MergeBinaryField(field, MergeState, toMerge, docsWithField);
			  }
			  else if (type == DocValuesType.SORTED)
			  {
				IList<SortedDocValues> toMerge = new List<SortedDocValues>();
				foreach (AtomicReader reader in MergeState.Readers)
				{
				  SortedDocValues values = reader.GetSortedDocValues(field.Name);
				  if (values == null)
				  {
					values = DocValues.EMPTY_SORTED;
				  }
				  toMerge.Add(values);
				}
				consumer.MergeSortedField(field, MergeState, toMerge);
			  }
			  else if (type == DocValuesType.SORTED_SET)
			  {
				IList<SortedSetDocValues> toMerge = new List<SortedSetDocValues>();
				foreach (AtomicReader reader in MergeState.Readers)
				{
				  SortedSetDocValues values = reader.GetSortedSetDocValues(field.Name);
				  if (values == null)
				  {
					values = DocValues.EMPTY_SORTED_SET;
				  }
				  toMerge.Add(values);
				}
				consumer.MergeSortedSetField(field, MergeState, toMerge);
			  }
			  else
			  {
				throw new AssertionError("type=" + type);
			  }
			}
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.Close(consumer);
		  }
		  else
		  {
			IOUtils.CloseWhileHandlingException(consumer);
		  }
		}
	  }

	  private void MergeNorms(SegmentWriteState segmentWriteState)
	  {
		DocValuesConsumer consumer = Codec.NormsFormat().normsConsumer(segmentWriteState);
		bool success = false;
		try
		{
		  foreach (FieldInfo field in MergeState.FieldInfos)
		  {
			if (field.HasNorms())
			{
			  IList<NumericDocValues> toMerge = new List<NumericDocValues>();
			  IList<Bits> docsWithField = new List<Bits>();
			  foreach (AtomicReader reader in MergeState.Readers)
			  {
				NumericDocValues norms = reader.GetNormValues(field.Name);
				if (norms == null)
				{
				  norms = DocValues.EMPTY_NUMERIC;
				}
				toMerge.Add(norms);
				docsWithField.Add(new Lucene.Net.Util.Bits_MatchAllBits(reader.MaxDoc()));
			  }
			  consumer.MergeNumericField(field, MergeState, toMerge, docsWithField);
			}
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.Close(consumer);
		  }
		  else
		  {
			IOUtils.CloseWhileHandlingException(consumer);
		  }
		}
	  }

	  private void SetMatchingSegmentReaders()
	  {
		// If the i'th reader is a SegmentReader and has
		// identical fieldName -> number mapping, then this
		// array will be non-null at position i:
		int numReaders = MergeState.Readers.Count;
		MergeState.MatchingSegmentReaders = new SegmentReader[numReaders];

		// If this reader is a SegmentReader, and all of its
		// field name -> number mappings match the "merged"
		// FieldInfos, then we can do a bulk copy of the
		// stored fields:
		for (int i = 0; i < numReaders; i++)
		{
		  AtomicReader reader = MergeState.Readers[i];
		  // TODO: we may be able to broaden this to
		  // non-SegmentReaders, since FieldInfos is now
		  // required?  But... this'd also require exposing
		  // bulk-copy (TVs and stored fields) API in foreign
		  // readers..
		  if (reader is SegmentReader)
		  {
			SegmentReader segmentReader = (SegmentReader) reader;
			bool same = true;
			FieldInfos segmentFieldInfos = segmentReader.FieldInfos;
			foreach (FieldInfo fi in segmentFieldInfos)
			{
			  FieldInfo other = MergeState.FieldInfos.fieldInfo(fi.Number);
			  if (other == null || !other.Name.Equals(fi.Name))
			  {
				same = false;
				break;
			  }
			}
			if (same)
			{
			  MergeState.MatchingSegmentReaders[i] = segmentReader;
			  MergeState.MatchedCount++;
			}
		  }
		}

		if (MergeState.InfoStream.isEnabled("SM"))
		{
		  MergeState.InfoStream.message("SM", "merge store matchedCount=" + MergeState.MatchedCount + " vs " + MergeState.Readers.Count);
		  if (MergeState.MatchedCount != MergeState.Readers.Count)
		  {
			MergeState.InfoStream.message("SM", "" + (MergeState.Readers.Count - MergeState.MatchedCount) + " non-bulk merges");
		  }
		}
	  }

	  public void MergeFieldInfos()
	  {
		foreach (AtomicReader reader in MergeState.Readers)
		{
		  FieldInfos readerFieldInfos = reader.FieldInfos;
		  foreach (FieldInfo fi in readerFieldInfos)
		  {
			FieldInfosBuilder.Add(fi);
		  }
		}
		MergeState.FieldInfos = FieldInfosBuilder.Finish();
	  }

	  /// 
	  /// <returns> The number of documents in all of the readers </returns>
	  /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
	  /// <exception cref="IOException"> if there is a low-level IO error </exception>
	  private int MergeFields()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Codecs.StoredFieldsWriter fieldsWriter = codec.storedFieldsFormat().fieldsWriter(directory, mergeState.segmentInfo, context);
		StoredFieldsWriter fieldsWriter = Codec.StoredFieldsFormat().fieldsWriter(Directory, MergeState.SegmentInfo, Context);

		try
		{
		  return fieldsWriter.Merge(MergeState);
		}
		finally
		{
		  fieldsWriter.Close();
		}
	  }

	  /// <summary>
	  /// Merge the TermVectors from each of the segments into the new one. </summary>
	  /// <exception cref="IOException"> if there is a low-level IO error </exception>
	  private int MergeVectors()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Codecs.TermVectorsWriter termVectorsWriter = codec.termVectorsFormat().vectorsWriter(directory, mergeState.segmentInfo, context);
		TermVectorsWriter termVectorsWriter = Codec.TermVectorsFormat().vectorsWriter(Directory, MergeState.SegmentInfo, Context);

		try
		{
		  return termVectorsWriter.Merge(MergeState);
		}
		finally
		{
		  termVectorsWriter.Close();
		}
	  }

	  // NOTE: removes any "all deleted" readers from mergeState.readers
	  private int SetDocMaps()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numReaders = mergeState.readers.size();
		int numReaders = MergeState.Readers.Count;

		// Remap docIDs
		MergeState.DocMaps = new MergeState.DocMap[numReaders];
		MergeState.DocBase = new int[numReaders];

		int docBase = 0;

		int i = 0;
		while (i < MergeState.Readers.Count)
		{

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final AtomicReader reader = mergeState.readers.get(i);
		  AtomicReader reader = MergeState.Readers[i];

		  MergeState.DocBase[i] = docBase;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final MergeState.DocMap docMap = MergeState.DocMap.build(reader);
		  MergeState.DocMap docMap = MergeState.DocMap.Build(reader);
		  MergeState.DocMaps[i] = docMap;
		  docBase += docMap.NumDocs();

		  i++;
		}

		return docBase;
	  }

	  private void MergeTerms(SegmentWriteState segmentWriteState)
	  {

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<Fields> fields = new java.util.ArrayList<>();
		IList<Fields> fields = new List<Fields>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<ReaderSlice> slices = new java.util.ArrayList<>();
		IList<ReaderSlice> slices = new List<ReaderSlice>();

		int docBase = 0;

		for (int readerIndex = 0;readerIndex < MergeState.Readers.Count;readerIndex++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final AtomicReader reader = mergeState.readers.get(readerIndex);
		  AtomicReader reader = MergeState.Readers[readerIndex];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Fields f = reader.fields();
		  Fields f = reader.Fields();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxDoc = reader.maxDoc();
		  int maxDoc = reader.MaxDoc();
		  if (f != null)
		  {
			slices.Add(new ReaderSlice(docBase, maxDoc, readerIndex));
			fields.Add(f);
		  }
		  docBase += maxDoc;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Codecs.FieldsConsumer consumer = codec.postingsFormat().fieldsConsumer(segmentWriteState);
		FieldsConsumer consumer = Codec.PostingsFormat().fieldsConsumer(segmentWriteState);
		bool success = false;
		try
		{
		  consumer.Merge(MergeState, new MultiFields(fields.toArray(Fields.EMPTY_ARRAY), slices.toArray(ReaderSlice.EMPTY_ARRAY)));
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.Close(consumer);
		  }
		  else
		  {
			IOUtils.CloseWhileHandlingException(consumer);
		  }
		}
	  }
	}

}