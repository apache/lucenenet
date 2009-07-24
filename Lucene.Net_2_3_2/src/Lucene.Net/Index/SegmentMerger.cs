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

using FieldSelector = Lucene.Net.Documents.FieldSelector;
using FieldSelectorResult = Lucene.Net.Documents.FieldSelectorResult;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;

namespace Lucene.Net.Index
{
	
	/// <summary> The SegmentMerger class combines two or more Segments, represented by an IndexReader ({@link #add},
	/// into a single Segment.  After adding the appropriate readers, call the merge method to combine the 
	/// segments.
	/// <P> 
	/// If the compoundFile flag is set, then the segments will be merged into a compound file.
	/// 
	/// 
	/// </summary>
	/// <seealso cref="merge">
	/// </seealso>
	/// <seealso cref="add">
	/// </seealso>
	public sealed class SegmentMerger
	{
		[Serializable]
		private class AnonymousClassFieldSelector : FieldSelector
		{
			public AnonymousClassFieldSelector(SegmentMerger enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(SegmentMerger enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private SegmentMerger enclosingInstance;
			public SegmentMerger Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public FieldSelectorResult Accept(System.String fieldName)
			{
				return FieldSelectorResult.LOAD_FOR_MERGE;
			}
		}
		private void  InitBlock()
		{
			termIndexInterval = IndexWriter.DEFAULT_TERM_INDEX_INTERVAL;
		}
		
		/// <summary>norms header placeholder </summary>
		internal static readonly byte[] NORMS_HEADER = new byte[]{(byte) 'N', (byte) 'R', (byte) 'M', unchecked((byte) -1)};
		
		private Directory directory;
		private System.String segment;
		private int termIndexInterval;
		
		private System.Collections.ArrayList readers = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
		private FieldInfos fieldInfos;
		
		private int mergedDocs;
		
		private CheckAbort checkAbort;
		
		// Whether we should merge doc stores (stored fields and
		// vectors files).  When all segments we are merging
		// already share the same doc store files, we don't need
		// to merge the doc stores.
		private bool mergeDocStores;
		
		/// <summary>Maximum number of contiguous documents to bulk-copy
		/// when merging stored fields 
		/// </summary>
		private const int MAX_RAW_MERGE_DOCS = 4192;
		
		/// <summary>This ctor used only by test code.
		/// 
		/// </summary>
		/// <param name="dir">The Directory to merge the other segments into
		/// </param>
		/// <param name="name">The name of the new segment
		/// </param>
		public /*internal*/ SegmentMerger(Directory dir, System.String name)
		{
			InitBlock();
			directory = dir;
			segment = name;
		}
		
		internal SegmentMerger(IndexWriter writer, System.String name, MergePolicy.OneMerge merge)
		{
			InitBlock();
			directory = writer.GetDirectory();
			segment = name;
			if (merge != null)
				checkAbort = new CheckAbort(merge, directory);
			termIndexInterval = writer.GetTermIndexInterval();
		}
		
		/// <summary> Add an IndexReader to the collection of readers that are to be merged</summary>
		/// <param name="reader">
		/// </param>
		public /*internal*/ void  Add(IndexReader reader)
		{
			readers.Add(reader);
		}
		
		/// <summary> </summary>
		/// <param name="i">The index of the reader to return
		/// </param>
		/// <returns> The ith reader to be merged
		/// </returns>
		internal IndexReader SegmentReader(int i)
		{
			return (IndexReader) readers[i];
		}
		
		/// <summary> Merges the readers specified by the {@link #add} method into the directory passed to the constructor</summary>
		/// <returns> The number of documents that were merged
		/// </returns>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public /*internal*/ int Merge()
		{
			return Merge(true);
		}
		
		/// <summary> Merges the readers specified by the {@link #add} method
		/// into the directory passed to the constructor.
		/// </summary>
		/// <param name="mergeDocStores">if false, we will not merge the
		/// stored fields nor vectors files
		/// </param>
		/// <returns> The number of documents that were merged
		/// </returns>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		internal int Merge(bool mergeDocStores)
		{
			
			this.mergeDocStores = mergeDocStores;
			
			// NOTE: it's important to add calls to
			// checkAbort.work(...) if you make any changes to this
			// method that will spend alot of time.  The frequency
			// of this check impacts how long
			// IndexWriter.close(false) takes to actually stop the
			// threads.
			
			mergedDocs = MergeFields();
			MergeTerms();
			MergeNorms();
			
			if (mergeDocStores && fieldInfos.HasVectors())
				MergeVectors();
			
			return mergedDocs;
		}
		
		/// <summary> close all IndexReaders that have been added.
		/// Should not be called before merge().
		/// </summary>
		/// <throws>  IOException </throws>
		public /*internal*/ void  CloseReaders()
		{
			for (int i = 0; i < readers.Count; i++)
			{
				// close readers
				IndexReader reader = (IndexReader) readers[i];
				reader.Close();
			}
		}
		
		public /*internal*/ System.Collections.ArrayList CreateCompoundFile(System.String fileName)
		{
			CompoundFileWriter cfsWriter = new CompoundFileWriter(directory, fileName, checkAbort);
			
			System.Collections.ArrayList files = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(IndexFileNames.COMPOUND_EXTENSIONS.Length + 1));
			
			// Basic files
			for (int i = 0; i < IndexFileNames.COMPOUND_EXTENSIONS.Length; i++)
			{
				System.String ext = IndexFileNames.COMPOUND_EXTENSIONS[i];
				if (mergeDocStores || (!ext.Equals(IndexFileNames.FIELDS_EXTENSION) && !ext.Equals(IndexFileNames.FIELDS_INDEX_EXTENSION)))
					files.Add(segment + "." + ext);
			}
			
			// Fieldable norm files
			for (int i = 0; i < fieldInfos.Size(); i++)
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				if (fi.isIndexed && !fi.omitNorms)
				{
					files.Add(segment + "." + IndexFileNames.NORMS_EXTENSION);
					break;
				}
			}
			
			// Vector files
			if (fieldInfos.HasVectors() && mergeDocStores)
			{
				for (int i = 0; i < IndexFileNames.VECTOR_EXTENSIONS.Length; i++)
				{
					files.Add(segment + "." + IndexFileNames.VECTOR_EXTENSIONS[i]);
				}
			}
			
			// Now merge all added files
			System.Collections.IEnumerator it = files.GetEnumerator();
			while (it.MoveNext())
			{
				cfsWriter.AddFile((System.String) it.Current);
			}
			
			// Perform the merge
			cfsWriter.Close();
			
			return files;
		}
		
		private void  AddIndexed(IndexReader reader, FieldInfos fieldInfos, System.Collections.ICollection names, bool storeTermVectors, bool storePositionWithTermVector, bool storeOffsetWithTermVector, bool storePayloads)
		{
			System.Collections.IEnumerator i = names.GetEnumerator();
			while (i.MoveNext())
			{
				System.String field = (System.String) i.Current;
				fieldInfos.Add(field, true, storeTermVectors, storePositionWithTermVector, storeOffsetWithTermVector, !reader.HasNorms(field), storePayloads);
			}
		}
		
		/// <summary> </summary>
		/// <returns> The number of documents in all of the readers
		/// </returns>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		private int MergeFields()
		{
			
			if (!mergeDocStores)
			{
				// When we are not merging by doc stores, that means
				// all segments were written as part of a single
				// autoCommit=false IndexWriter session, so their field
				// name -> number mapping are the same.  So, we start
				// with the fieldInfos of the last segment in this
				// case, to keep that numbering.
				SegmentReader sr = (SegmentReader) readers[readers.Count - 1];
				fieldInfos = (FieldInfos) sr.fieldInfos.Clone();
			}
			else
			{
				fieldInfos = new FieldInfos(); // merge field names
			}
			
			for (int i = 0; i < readers.Count; i++)
			{
				IndexReader reader = (IndexReader) readers[i];
				if (reader is SegmentReader)
				{
					SegmentReader segmentReader = (SegmentReader) reader;
					for (int j = 0; j < segmentReader.GetFieldInfos().Size(); j++)
					{
						FieldInfo fi = segmentReader.GetFieldInfos().FieldInfo(j);
						fieldInfos.Add(fi.name, fi.isIndexed, fi.storeTermVector, fi.storePositionWithTermVector, fi.storeOffsetWithTermVector, !reader.HasNorms(fi.name), fi.storePayloads);
					}
				}
				else
				{
					AddIndexed(reader, fieldInfos, reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR_WITH_POSITION_OFFSET), true, true, true, false);
					AddIndexed(reader, fieldInfos, reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR_WITH_POSITION), true, true, false, false);
					AddIndexed(reader, fieldInfos, reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR_WITH_OFFSET), true, false, true, false);
					AddIndexed(reader, fieldInfos, reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR), true, false, false, false);
					AddIndexed(reader, fieldInfos, reader.GetFieldNames(IndexReader.FieldOption.STORES_PAYLOADS), false, false, false, true);
					AddIndexed(reader, fieldInfos, reader.GetFieldNames(IndexReader.FieldOption.INDEXED), false, false, false, false);
					fieldInfos.Add(reader.GetFieldNames(IndexReader.FieldOption.UNINDEXED), false);
				}
			}
			fieldInfos.Write(directory, segment + ".fnm");
			
			int docCount = 0;
			
			if (mergeDocStores)
			{
				
				// If the i'th reader is a SegmentReader and has
				// identical fieldName -> number mapping, then this
				// array will be non-null at position i:
				SegmentReader[] matchingSegmentReaders = new SegmentReader[readers.Count];
				
				// If this reader is a SegmentReader, and all of its
				// field name -> number mappings match the "merged"
				// FieldInfos, then we can do a bulk copy of the
				// stored fields:
				for (int i = 0; i < readers.Count; i++)
				{
					IndexReader reader = (IndexReader) readers[i];
					if (reader is SegmentReader)
					{
						SegmentReader segmentReader = (SegmentReader) reader;
						bool same = true;
						FieldInfos segmentFieldInfos = segmentReader.GetFieldInfos();
						for (int j = 0; same && j < segmentFieldInfos.Size(); j++)
							same = fieldInfos.FieldName(j).Equals(segmentFieldInfos.FieldName(j));
						if (same)
						{
							matchingSegmentReaders[i] = segmentReader;
						}
					}
				}
				
				// Used for bulk-reading raw bytes for stored fields
				int[] rawDocLengths = new int[MAX_RAW_MERGE_DOCS];
				
				// for merging we don't want to compress/uncompress the data, so to tell the FieldsReader that we're
				// in  merge mode, we use this FieldSelector
				FieldSelector fieldSelectorMerge = new AnonymousClassFieldSelector(this);
				
				// merge field values
				FieldsWriter fieldsWriter = new FieldsWriter(directory, segment, fieldInfos);
				
				try
				{
					for (int i = 0; i < readers.Count; i++)
					{
						IndexReader reader = (IndexReader) readers[i];
						SegmentReader matchingSegmentReader = matchingSegmentReaders[i];
						FieldsReader matchingFieldsReader;
						if (matchingSegmentReader != null)
							matchingFieldsReader = matchingSegmentReader.GetFieldsReader();
						else
							matchingFieldsReader = null;
						int maxDoc = reader.MaxDoc();
						for (int j = 0; j < maxDoc; )
						{
							if (!reader.IsDeleted(j))
							{
								// skip deleted docs
								if (matchingSegmentReader != null)
								{
									// We can optimize this case (doing a bulk
									// byte copy) since the field numbers are
									// identical
									int start = j;
									int numDocs = 0;
									do 
									{
										j++;
										numDocs++;
									}
									while (j < maxDoc && !matchingSegmentReader.IsDeleted(j) && numDocs < MAX_RAW_MERGE_DOCS);
									
									IndexInput stream = matchingFieldsReader.RawDocs(rawDocLengths, start, numDocs);
									fieldsWriter.AddRawDocuments(stream, rawDocLengths, numDocs);
									docCount += numDocs;
									if (checkAbort != null)
										checkAbort.Work(300 * numDocs);
								}
								else
								{
									fieldsWriter.AddDocument(reader.Document(j, fieldSelectorMerge));
									j++;
									docCount++;
									if (checkAbort != null)
										checkAbort.Work(300);
								}
							}
							else
								j++;
						}
					}
				}
				finally
				{
					fieldsWriter.Close();
				}

                System.Diagnostics.Debug.Assert(docCount*8 == directory.FileLength(segment + "." + IndexFileNames.FIELDS_INDEX_EXTENSION),
                    "after MergeFields: fdx size mismatch: " + docCount + " docs vs " + 
                    directory.FileLength(segment + "." + IndexFileNames.FIELDS_INDEX_EXTENSION) +
                    " length in bytes of " + segment + "." + IndexFileNames.FIELDS_INDEX_EXTENSION); 
			}
			// If we are skipping the doc stores, that means there
			// are no deletions in any of these segments, so we
			// just sum numDocs() of each segment to get total docCount
			else
				for (int i = 0; i < readers.Count; i++)
					docCount += ((IndexReader) readers[i]).NumDocs();
			
			return docCount;
		}
		
		/// <summary> Merge the TermVectors from each of the segments into the new one.</summary>
		/// <throws>  IOException </throws>
		private void  MergeVectors()
		{
			TermVectorsWriter termVectorsWriter = new TermVectorsWriter(directory, segment, fieldInfos);
			
			try
			{
				for (int r = 0; r < readers.Count; r++)
				{
					IndexReader reader = (IndexReader) readers[r];
					int maxDoc = reader.MaxDoc();
					for (int docNum = 0; docNum < maxDoc; docNum++)
					{
						// skip deleted docs
						if (reader.IsDeleted(docNum))
							continue;
						termVectorsWriter.AddAllDocVectors(reader.GetTermFreqVectors(docNum));
						if (checkAbort != null)
							checkAbort.Work(300);
					}
				}
			}
			finally
			{
				termVectorsWriter.Close();
			}

            System.Diagnostics.Debug.Assert(4 + mergedDocs * 8 == directory.FileLength(segment + "." + IndexFileNames.VECTORS_INDEX_EXTENSION),
                "after MergeVectors: tvx size mismatch: " + mergedDocs + " docs vs " +
                directory.FileLength(segment + "." + IndexFileNames.VECTORS_INDEX_EXTENSION) +
                " length in bytes of " + segment + "." + IndexFileNames.VECTORS_INDEX_EXTENSION);
		}
		
		private IndexOutput freqOutput = null;
		private IndexOutput proxOutput = null;
		private TermInfosWriter termInfosWriter = null;
		private int skipInterval;
		private int maxSkipLevels;
		private SegmentMergeQueue queue = null;
		private DefaultSkipListWriter skipListWriter = null;
		
		private void  MergeTerms()
		{
			try
			{
				freqOutput = directory.CreateOutput(segment + ".frq");
				proxOutput = directory.CreateOutput(segment + ".prx");
				termInfosWriter = new TermInfosWriter(directory, segment, fieldInfos, termIndexInterval);
				skipInterval = termInfosWriter.skipInterval;
				maxSkipLevels = termInfosWriter.maxSkipLevels;
				skipListWriter = new DefaultSkipListWriter(skipInterval, maxSkipLevels, mergedDocs, freqOutput, proxOutput);
				queue = new SegmentMergeQueue(readers.Count);
				
				MergeTermInfos();
			}
			finally
			{
				if (freqOutput != null)
					freqOutput.Close();
				if (proxOutput != null)
					proxOutput.Close();
				if (termInfosWriter != null)
					termInfosWriter.Close();
				if (queue != null)
					queue.Close();
			}
		}
		
		private void  MergeTermInfos()
		{
			int base_Renamed = 0;
			for (int i = 0; i < readers.Count; i++)
			{
				IndexReader reader = (IndexReader) readers[i];
				TermEnum termEnum = reader.Terms();
				SegmentMergeInfo smi = new SegmentMergeInfo(base_Renamed, termEnum, reader);
				base_Renamed += reader.NumDocs();
				if (smi.Next())
					queue.Put(smi);
				// initialize queue
				else
					smi.Close();
			}
			
			SegmentMergeInfo[] match = new SegmentMergeInfo[readers.Count];
			
			while (queue.Size() > 0)
			{
				int matchSize = 0; // pop matching terms
				match[matchSize++] = (SegmentMergeInfo) queue.Pop();
				Term term = match[0].term;
				SegmentMergeInfo top = (SegmentMergeInfo) queue.Top();
				
				while (top != null && term.CompareTo(top.term) == 0)
				{
					match[matchSize++] = (SegmentMergeInfo) queue.Pop();
					top = (SegmentMergeInfo) queue.Top();
				}
				
				int df = MergeTermInfo(match, matchSize); // add new TermInfo
				
				if (checkAbort != null)
					checkAbort.Work(df / 3.0);
				
				while (matchSize > 0)
				{
					SegmentMergeInfo smi = match[--matchSize];
					if (smi.Next())
						queue.Put(smi);
					// restore queue
					else
						smi.Close(); // done with a segment
				}
			}
		}
		
		private TermInfo termInfo = new TermInfo(); // minimize consing
		
		/// <summary>Merge one term found in one or more segments. The array <code>smis</code>
		/// contains segments that are positioned at the same term. <code>N</code>
		/// is the number of cells in the array actually occupied.
		/// 
		/// </summary>
		/// <param name="smis">array of segments
		/// </param>
		/// <param name="n">number of cells in the array actually occupied
		/// </param>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		private int MergeTermInfo(SegmentMergeInfo[] smis, int n)
		{
			long freqPointer = freqOutput.GetFilePointer();
			long proxPointer = proxOutput.GetFilePointer();
			
			int df = AppendPostings(smis, n); // append posting data
			
			long skipPointer = skipListWriter.WriteSkip(freqOutput);
			
			if (df > 0)
			{
				// add an entry to the dictionary with pointers to prox and freq files
				termInfo.Set(df, freqPointer, proxPointer, (int) (skipPointer - freqPointer));
				termInfosWriter.Add(smis[0].term, termInfo);
			}
			
			return df;
		}
		
		private byte[] payloadBuffer = null;
		
		/// <summary>Process postings from multiple segments all positioned on the
		/// same term. Writes out merged entries into freqOutput and
		/// the proxOutput streams.
		/// 
		/// </summary>
		/// <param name="smis">array of segments
		/// </param>
		/// <param name="n">number of cells in the array actually occupied
		/// </param>
		/// <returns> number of documents across all segments where this term was found
		/// </returns>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		private int AppendPostings(SegmentMergeInfo[] smis, int n)
		{
			int lastDoc = 0;
			int df = 0; // number of docs w/ term
			skipListWriter.ResetSkip();
			bool storePayloads = fieldInfos.FieldInfo(smis[0].term.field).storePayloads;
			int lastPayloadLength = - 1; // ensures that we write the first length
			for (int i = 0; i < n; i++)
			{
				SegmentMergeInfo smi = smis[i];
				TermPositions postings = smi.GetPositions();
				System.Diagnostics.Debug.Assert(postings != null);
				int base_Renamed = smi.base_Renamed;
				int[] docMap = smi.GetDocMap();
				postings.Seek(smi.termEnum);
				while (postings.Next())
				{
					int doc = postings.Doc();
					if (docMap != null)
						doc = docMap[doc]; // map around deletions
					doc += base_Renamed; // convert to merged space
					
					if (doc < 0 || (df > 0 && doc <= lastDoc))
						throw new CorruptIndexException("docs out of order (" + doc + " <= " + lastDoc + " )");
					
					df++;
					
					if ((df % skipInterval) == 0)
					{
						skipListWriter.SetSkipData(lastDoc, storePayloads, lastPayloadLength);
						skipListWriter.BufferSkip(df);
					}
					
					int docCode = (doc - lastDoc) << 1; // use low bit to flag freq=1
					lastDoc = doc;
					
					int freq = postings.Freq();
					if (freq == 1)
					{
						freqOutput.WriteVInt(docCode | 1); // write doc & freq=1
					}
					else
					{
						freqOutput.WriteVInt(docCode); // write doc
						freqOutput.WriteVInt(freq); // write frequency in doc
					}
					
					/** See {@link DocumentWriter#writePostings(Posting[], String) for 
					*  documentation about the encoding of positions and payloads
					*/
					int lastPosition = 0; // write position deltas
					for (int j = 0; j < freq; j++)
					{
						int position = postings.NextPosition();
						int delta = position - lastPosition;
						if (storePayloads)
						{
							int payloadLength = postings.GetPayloadLength();
							if (payloadLength == lastPayloadLength)
							{
								proxOutput.WriteVInt(delta * 2);
							}
							else
							{
								proxOutput.WriteVInt(delta * 2 + 1);
								proxOutput.WriteVInt(payloadLength);
								lastPayloadLength = payloadLength;
							}
							if (payloadLength > 0)
							{
								if (payloadBuffer == null || payloadBuffer.Length < payloadLength)
								{
									payloadBuffer = new byte[payloadLength];
								}
								postings.GetPayload(payloadBuffer, 0);
								proxOutput.WriteBytes(payloadBuffer, 0, payloadLength);
							}
						}
						else
						{
							proxOutput.WriteVInt(delta);
						}
						lastPosition = position;
					}
				}
			}
			return df;
		}
		
		private void  MergeNorms()
		{
			byte[] normBuffer = null;
			IndexOutput output = null;
			try
			{
				for (int i = 0; i < fieldInfos.Size(); i++)
				{
					FieldInfo fi = fieldInfos.FieldInfo(i);
					if (fi.isIndexed && !fi.omitNorms)
					{
						if (output == null)
						{
							output = directory.CreateOutput(segment + "." + IndexFileNames.NORMS_EXTENSION);
							output.WriteBytes(NORMS_HEADER, NORMS_HEADER.Length);
						}
						for (int j = 0; j < readers.Count; j++)
						{
							IndexReader reader = (IndexReader) readers[j];
							int maxDoc = reader.MaxDoc();
							if (normBuffer == null || normBuffer.Length < maxDoc)
							{
								// the buffer is too small for the current segment
								normBuffer = new byte[maxDoc];
							}
							reader.Norms(fi.name, normBuffer, 0);
							if (!reader.HasDeletions())
							{
								//optimized case for segments without deleted docs
								output.WriteBytes(normBuffer, maxDoc);
							}
							else
							{
								// this segment has deleted docs, so we have to
								// check for every doc if it is deleted or not
								for (int k = 0; k < maxDoc; k++)
								{
									if (!reader.IsDeleted(k))
									{
										output.WriteByte(normBuffer[k]);
									}
								}
							}
							if (checkAbort != null)
								checkAbort.Work(maxDoc);
						}
					}
				}
			}
			finally
			{
				if (output != null)
				{
					output.Close();
				}
			}
		}
		
		internal sealed class CheckAbort
		{
			private double workCount;
			private MergePolicy.OneMerge merge;
			private Directory dir;
			public CheckAbort(MergePolicy.OneMerge merge, Directory dir)
			{
				this.merge = merge;
				this.dir = dir;
			}
			
			/// <summary> Records the fact that roughly units amount of work
			/// have been done since this method was last called.
			/// When adding time-consuming code into SegmentMerger,
			/// you should test different values for units to ensure
			/// that the time in between calls to merge.checkAborted
			/// is up to ~ 1 second.
			/// </summary>
			public void  Work(double units)
			{
				workCount += units;
				if (workCount >= 10000.0)
				{
					merge.CheckAborted(dir);
					workCount = 0;
				}
			}
		}
	}
}