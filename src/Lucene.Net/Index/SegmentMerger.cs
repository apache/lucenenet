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
using Directory = Lucene.Net.Store.Directory;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;

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
		private void  InitBlock()
		{
			termIndexInterval = IndexWriter.DEFAULT_TERM_INDEX_INTERVAL;
		}
		private Directory directory;
		private System.String segment;
		private int termIndexInterval;
		
		private System.Collections.ArrayList readers = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
		private FieldInfos fieldInfos;
		
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
		
		internal SegmentMerger(IndexWriter writer, System.String name)
		{
			InitBlock();
			directory = writer.GetDirectory();
			segment = name;
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
		/// <throws>  IOException </throws>
		public /*internal*/ int Merge()
		{
			int value_Renamed;
			
			value_Renamed = MergeFields();
			MergeTerms();
			MergeNorms();
			
			if (fieldInfos.HasVectors())
				MergeVectors();
			
			return value_Renamed;
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
		
		public System.Collections.ArrayList CreateCompoundFile(System.String fileName)
		{
			CompoundFileWriter cfsWriter = new CompoundFileWriter(directory, fileName);
			
			System.Collections.ArrayList files = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(IndexFileNames.COMPOUND_EXTENSIONS.Length + fieldInfos.Size()));
			
			// Basic files
			for (int i = 0; i < IndexFileNames.COMPOUND_EXTENSIONS.Length; i++)
			{
				files.Add(segment + "." + IndexFileNames.COMPOUND_EXTENSIONS[i]);
			}
			
			// Field norm files
			for (int i = 0; i < fieldInfos.Size(); i++)
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				if (fi.isIndexed && !fi.omitNorms)
				{
					files.Add(segment + ".f" + i);
				}
			}
			
			// Vector files
			if (fieldInfos.HasVectors())
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
		
		private void  AddIndexed(IndexReader reader, FieldInfos fieldInfos, System.Collections.ICollection names, bool storeTermVectors, bool storePositionWithTermVector, bool storeOffsetWithTermVector)
		{
			System.Collections.IEnumerator i = names.GetEnumerator();
			while (i.MoveNext())
			{
                System.Collections.DictionaryEntry e = (System.Collections.DictionaryEntry) i.Current;
                System.String field = (System.String) e.Key;
				fieldInfos.Add(field, true, storeTermVectors, storePositionWithTermVector, storeOffsetWithTermVector, !reader.HasNorms(field));
			}
		}
		
		/// <summary> </summary>
		/// <returns> The number of documents in all of the readers
		/// </returns>
		/// <throws>  IOException </throws>
		private int MergeFields()
		{
			fieldInfos = new FieldInfos(); // merge field names
			int docCount = 0;
			for (int i = 0; i < readers.Count; i++)
			{
				IndexReader reader = (IndexReader) readers[i];
				AddIndexed(reader, fieldInfos, reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR_WITH_POSITION_OFFSET), true, true, true);
				AddIndexed(reader, fieldInfos, reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR_WITH_POSITION), true, true, false);
				AddIndexed(reader, fieldInfos, reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR_WITH_OFFSET), true, false, true);
				AddIndexed(reader, fieldInfos, reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR), true, false, false);
				AddIndexed(reader, fieldInfos, reader.GetFieldNames(IndexReader.FieldOption.INDEXED), false, false, false);
				fieldInfos.Add(reader.GetFieldNames(IndexReader.FieldOption.UNINDEXED), false);
			}
			fieldInfos.Write(directory, segment + ".fnm");
			
			FieldsWriter fieldsWriter = new FieldsWriter(directory, segment, fieldInfos);
			try
			{
				for (int i = 0; i < readers.Count; i++)
				{
					IndexReader reader = (IndexReader) readers[i];
					int maxDoc = reader.MaxDoc();
					for (int j = 0; j < maxDoc; j++)
						if (!reader.IsDeleted(j))
						{
							// skip deleted docs
							fieldsWriter.AddDocument(reader.Document(j));
							docCount++;
						}
				}
			}
			finally
			{
				fieldsWriter.Close();
			}
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
					}
				}
			}
			finally
			{
				termVectorsWriter.Close();
			}
		}
		
		private IndexOutput freqOutput = null;
		private IndexOutput proxOutput = null;
		private TermInfosWriter termInfosWriter = null;
		private int skipInterval;
		private SegmentMergeQueue queue = null;
		
		private void  MergeTerms()
		{
			try
			{
				freqOutput = directory.CreateOutput(segment + ".frq");
				proxOutput = directory.CreateOutput(segment + ".prx");
				termInfosWriter = new TermInfosWriter(directory, segment, fieldInfos, termIndexInterval);
				skipInterval = termInfosWriter.skipInterval;
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
				
				MergeTermInfo(match, matchSize); // add new TermInfo
				
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
		private void  MergeTermInfo(SegmentMergeInfo[] smis, int n)
		{
			long freqPointer = freqOutput.GetFilePointer();
			long proxPointer = proxOutput.GetFilePointer();
			
			int df = AppendPostings(smis, n); // append posting data
			
			long skipPointer = WriteSkip();
			
			if (df > 0)
			{
				// add an entry to the dictionary with pointers to prox and freq files
				termInfo.Set(df, freqPointer, proxPointer, (int) (skipPointer - freqPointer));
				termInfosWriter.Add(smis[0].term, termInfo);
			}
		}
		
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
		private int AppendPostings(SegmentMergeInfo[] smis, int n)
		{
			int lastDoc = 0;
			int df = 0; // number of docs w/ term
			ResetSkip();
			for (int i = 0; i < n; i++)
			{
				SegmentMergeInfo smi = smis[i];
				TermPositions postings = smi.GetPositions();
				int base_Renamed = smi.base_Renamed;
				int[] docMap = smi.GetDocMap();
				postings.Seek(smi.termEnum);
				while (postings.Next())
				{
					int doc = postings.Doc();
					if (docMap != null)
						doc = docMap[doc]; // map around deletions
					doc += base_Renamed; // convert to merged space
					
					if (doc < lastDoc)
						throw new System.SystemException("docs out of order");
					
					df++;
					
					if ((df % skipInterval) == 0)
					{
						BufferSkip(lastDoc);
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
					
					int lastPosition = 0; // write position deltas
					for (int j = 0; j < freq; j++)
					{
						int position = postings.NextPosition();
						proxOutput.WriteVInt(position - lastPosition);
						lastPosition = position;
					}
				}
			}
			return df;
		}
		
		private RAMOutputStream skipBuffer = new RAMOutputStream();
		private int lastSkipDoc;
		private long lastSkipFreqPointer;
		private long lastSkipProxPointer;
		
		private void  ResetSkip()
		{
			skipBuffer.Reset();
			lastSkipDoc = 0;
			lastSkipFreqPointer = freqOutput.GetFilePointer();
			lastSkipProxPointer = proxOutput.GetFilePointer();
		}
		
		private void  BufferSkip(int doc)
		{
			long freqPointer = freqOutput.GetFilePointer();
			long proxPointer = proxOutput.GetFilePointer();
			
			skipBuffer.WriteVInt(doc - lastSkipDoc);
			skipBuffer.WriteVInt((int) (freqPointer - lastSkipFreqPointer));
			skipBuffer.WriteVInt((int) (proxPointer - lastSkipProxPointer));
			
			lastSkipDoc = doc;
			lastSkipFreqPointer = freqPointer;
			lastSkipProxPointer = proxPointer;
		}
		
		private long WriteSkip()
		{
			long skipPointer = freqOutput.GetFilePointer();
			skipBuffer.WriteTo(freqOutput);
			return skipPointer;
		}
		
		private void  MergeNorms()
		{
			for (int i = 0; i < fieldInfos.Size(); i++)
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				if (fi.isIndexed && !fi.omitNorms)
				{
					IndexOutput output = directory.CreateOutput(segment + ".f" + i);
					try
					{
						for (int j = 0; j < readers.Count; j++)
						{
							IndexReader reader = (IndexReader) readers[j];
							int maxDoc = reader.MaxDoc();
							byte[] input = new byte[maxDoc];
							reader.Norms(fi.name, input, 0);
							for (int k = 0; k < maxDoc; k++)
							{
								if (!reader.IsDeleted(k))
								{
									output.WriteByte(input[k]);
								}
							}
						}
					}
					finally
					{
						output.Close();
					}
				}
			}
		}
	}
}