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

using System.Collections.Generic;

using Document = Lucene.Net.Documents.Document;
using FieldSelector = Lucene.Net.Documents.FieldSelector;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
{
	
	/// <summary> An IndexReader which reads indexes with multiple segments.</summary>
	public class MultiSegmentReader : DirectoryIndexReader
	{
		protected internal SegmentReader[] subReaders;
		private int[] starts; // 1st docno for each segment
        private Dictionary<string, byte[]> normsCache = new Dictionary<string, byte[]>();
		private int maxDoc = 0;
		private int numDocs = - 1;
		private bool hasDeletions = false;
		
		/// <summary>Construct reading the named set of readers. </summary>
		internal MultiSegmentReader(Directory directory, SegmentInfos sis, bool closeDirectory, bool readOnly):base(directory, sis, closeDirectory, readOnly)
		{
			// To reduce the chance of hitting FileNotFound
			// (and having to retry), we open segments in
			// reverse because IndexWriter merges & deletes
			// the newest segments first.
			
			SegmentReader[] readers = new SegmentReader[sis.Count];
			for (int i = sis.Count - 1; i >= 0; i--)
			{
				try
				{
					readers[i] = SegmentReader.Get(readOnly, sis.Info(i));
				}
				catch (System.IO.IOException e)
				{
					// Close all readers we had opened:
					for (i++; i < sis.Count; i++)
					{
						try
						{
							readers[i].Close();
						}
						catch (System.IO.IOException)
						{
							// keep going - we want to clean up as much as possible
						}
					}
					throw e;
				}
			}
			
			Initialize(readers);
		}
		
		/// <summary>This contructor is only used for {@link #Reopen()} </summary>
		internal MultiSegmentReader(Directory directory, SegmentInfos infos, bool closeDirectory, SegmentReader[] oldReaders, int[] oldStarts, Dictionary<string, byte[]> oldNormsCache, bool readOnly)
            : base(directory, infos, closeDirectory, readOnly)
		{
			
			// we put the old SegmentReaders in a map, that allows us
			// to lookup a reader using its segment name
            Dictionary<string, int> segmentReaders = new Dictionary<string, int>();
			
			if (oldReaders != null)
			{
				// create a Map SegmentName->SegmentReader
				for (int i = 0; i < oldReaders.Length; i++)
				{
					segmentReaders[oldReaders[i].GetSegmentName()] = i;
				}
			}
			
			SegmentReader[] newReaders = new SegmentReader[infos.Count];
			
			// remember which readers are shared between the old and the re-opened
			// MultiSegmentReader - we have to incRef those readers
			bool[] readerShared = new bool[infos.Count];
			
			for (int i = infos.Count - 1; i >= 0; i--)
			{
				// find SegmentReader for this segment
                int oldReaderIndex;
                if (!segmentReaders.ContainsKey(infos.Info(i).name))
				{
					// this is a new segment, no old SegmentReader can be reused
					newReaders[i] = null;
				}
				else
				{
                    oldReaderIndex = segmentReaders[infos.Info(i).name];
                    // there is an old reader for this segment - we'll try to reopen it
					newReaders[i] = oldReaders[oldReaderIndex];
				}
				
				bool success = false;
				try
				{
					SegmentReader newReader;
					if (newReaders[i] == null || infos.Info(i).GetUseCompoundFile() != newReaders[i].GetSegmentInfo().GetUseCompoundFile())
					{
						// this is a new reader; in case we hit an exception we can close it safely
						newReader = SegmentReader.Get(readOnly, infos.Info(i));
					}
					else
					{
						newReader = (SegmentReader) newReaders[i].ReopenSegment(infos.Info(i));
					}
					if (newReader == newReaders[i])
					{
						// this reader will be shared between the old and the new one,
						// so we must incRef it
						readerShared[i] = true;
						newReader.IncRef();
					}
					else
					{
						readerShared[i] = false;
						newReaders[i] = newReader;
					}
					success = true;
				}
				finally
				{
					if (!success)
					{
						for (i++; i < infos.Count; i++)
						{
							if (newReaders[i] != null)
							{
								try
								{
									if (!readerShared[i])
									{
										// this is a new subReader that is not used by the old one,
										// we can close it
										newReaders[i].Close();
									}
									else
									{
										// this subReader is also used by the old reader, so instead
										// closing we must decRef it
										newReaders[i].DecRef();
									}
								}
								catch (System.IO.IOException)
								{
									// keep going - we want to clean up as much as possible
								}
							}
						}
					}
				}
			}
			
			// initialize the readers to calculate maxDoc before we try to reuse the old normsCache
			Initialize(newReaders);
			
			// try to copy unchanged norms from the old normsCache to the new one
			if (oldNormsCache != null)
			{
				IEnumerator<KeyValuePair<string, byte[]>> it = oldNormsCache.GetEnumerator();
				while (it.MoveNext())
				{
                    KeyValuePair<string, byte[]> entry = it.Current;
					string field = entry.Key;
					if (!HasNorms(field))
					{
						continue;
					}
					
					byte[] oldBytes = entry.Value;
					byte[] bytes = new byte[MaxDoc()];
					
					for (int i = 0; i < subReaders.Length; i++)
					{
                        if (segmentReaders.ContainsKey(subReaders[i].GetSegmentName()))
                        {
                            int oldReaderIndex = segmentReaders[subReaders[i].GetSegmentName()];
                            // this SegmentReader was not re-opened, we can copy all of its norms 
                            if (oldReaders[oldReaderIndex] == subReaders[i] || oldReaders[oldReaderIndex].norms[field] == subReaders[i].norms[field])
                            {
                                // we don't have to synchronize here: either this constructor is called from a SegmentReader,
                                // in which case no old norms cache is present, or it is called from MultiReader.reopen(),
                                // which is synchronized
                                System.Array.Copy(oldBytes, oldStarts[oldReaderIndex], bytes, starts[i], starts[i + 1] - starts[i]);
                            }
                            else
                            {
                                subReaders[i].Norms(field, bytes, starts[i]);
                            }
                        }
                        else
                        {
                            subReaders[i].Norms(field, bytes, starts[i]);
                        }
                    }
					
					normsCache[field] = bytes; // update cache
                }
			}
		}
		
		private void  Initialize(SegmentReader[] subReaders)
		{
			this.subReaders = subReaders;
			starts = new int[subReaders.Length + 1]; // build starts array
			for (int i = 0; i < subReaders.Length; i++)
			{
				starts[i] = maxDoc;
				maxDoc += subReaders[i].MaxDoc(); // compute maxDocs
				
				if (subReaders[i].HasDeletions())
					hasDeletions = true;
			}
			starts[subReaders.Length] = maxDoc;
		}
		
		protected internal override DirectoryIndexReader DoReopen(SegmentInfos infos)
		{
			lock (this)
			{
				if (infos.Count == 1)
				{
					// The index has only one segment now, so we can't refresh the MultiSegmentReader.
					// Return a new [ReadOnly]SegmentReader instead
					return SegmentReader.Get(readOnly, infos, infos.Info(0), false);
				}
                else if (readOnly)
                {
                    return new ReadOnlyMultiSegmentReader(directory, infos, closeDirectory, subReaders, starts, normsCache);
                }
                else
                {
                    return new MultiSegmentReader(directory, infos, closeDirectory, subReaders, starts, normsCache, false);
                }
			}
		}
		
		public override TermFreqVector[] GetTermFreqVectors(int n)
		{
			EnsureOpen();
			int i = ReaderIndex(n); // find segment num
			return subReaders[i].GetTermFreqVectors(n - starts[i]); // dispatch to segment
		}
		
		public override TermFreqVector GetTermFreqVector(int n, string field)
		{
			EnsureOpen();
			int i = ReaderIndex(n); // find segment num
			return subReaders[i].GetTermFreqVector(n - starts[i], field);
		}
		
		
		public override void  GetTermFreqVector(int docNumber, string field, TermVectorMapper mapper)
		{
			EnsureOpen();
			int i = ReaderIndex(docNumber); // find segment num
			subReaders[i].GetTermFreqVector(docNumber - starts[i], field, mapper);
		}
		
		public override void  GetTermFreqVector(int docNumber, TermVectorMapper mapper)
		{
			EnsureOpen();
			int i = ReaderIndex(docNumber); // find segment num
			subReaders[i].GetTermFreqVector(docNumber - starts[i], mapper);
		}
		
		public override bool IsOptimized()
		{
			return false;
		}
		
		public override int NumDocs()
		{
			lock (this)
			{
				// Don't call ensureOpen() here (it could affect performance)
				if (numDocs == - 1)
				{
					// check cache
					int n = 0; // cache miss--recompute
					for (int i = 0; i < subReaders.Length; i++)
						n += subReaders[i].NumDocs(); // sum from readers
					numDocs = n;
				}
				return numDocs;
			}
		}
		
		public override int MaxDoc()
		{
			// Don't call ensureOpen() here (it could affect performance)
			return maxDoc;
		}
		
		// inherit javadoc
		public override Document Document(int n, FieldSelector fieldSelector)
		{
			EnsureOpen();
			int i = ReaderIndex(n); // find segment num
			return subReaders[i].Document(n - starts[i], fieldSelector); // dispatch to segment reader
		}
		
		public override bool IsDeleted(int n)
		{
			// Don't call ensureOpen() here (it could affect performance)
			int i = ReaderIndex(n); // find segment num
			return subReaders[i].IsDeleted(n - starts[i]); // dispatch to segment reader
		}
		
		public override bool HasDeletions()
		{
			// Don't call ensureOpen() here (it could affect performance)
			return hasDeletions;
		}
		
		protected internal override void  DoDelete(int n)
		{
			numDocs = - 1; // invalidate cache
			int i = ReaderIndex(n); // find segment num
			subReaders[i].DeleteDocument(n - starts[i]); // dispatch to segment reader
			hasDeletions = true;
		}
		
		protected internal override void  DoUndeleteAll()
		{
			for (int i = 0; i < subReaders.Length; i++)
				subReaders[i].UndeleteAll();
			
			hasDeletions = false;
			numDocs = - 1; // invalidate cache
		}
		
		private int ReaderIndex(int n)
		{
			// find reader for doc n:
			return ReaderIndex(n, this.starts, this.subReaders.Length);
		}
		
		internal static int ReaderIndex(int n, int[] starts, int numSubReaders)
		{
			// find reader for doc n:
			int lo = 0; // search starts array
			int hi = numSubReaders - 1; // for first element less
			
			while (hi >= lo)
			{
				int mid = (lo + hi) >> 1;
				int midValue = starts[mid];
				if (n < midValue)
					hi = mid - 1;
				else if (n > midValue)
					lo = mid + 1;
				else
				{
					// found a match
					while (mid + 1 < numSubReaders && starts[mid + 1] == midValue)
					{
						mid++; // scan to last match
					}
					return mid;
				}
			}
			return hi;
		}
		
		public override bool HasNorms(string field)
		{
			EnsureOpen();
			for (int i = 0; i < subReaders.Length; i++)
			{
				if (subReaders[i].HasNorms(field))
					return true;
			}
			return false;
		}
		
		private byte[] ones;
		private byte[] fakeNorms()
		{
			if (ones == null)
				ones = SegmentReader.CreateFakeNorms(MaxDoc());
			return ones;
		}
		
		public override byte[] Norms(string field)
		{
			lock (this)
			{
				EnsureOpen();
                byte[] bytes = normsCache.ContainsKey(field) ? normsCache[field] : null;

                if (bytes != null)
                    return bytes; // cache hit

                if (!HasNorms(field))
                    return fakeNorms();

				bytes = new byte[MaxDoc()];
				for (int i = 0; i < subReaders.Length; i++)
					subReaders[i].Norms(field, bytes, starts[i]);
                normsCache[field] = bytes; // update cache

                return bytes;
			}
		}
		
		public override void  Norms(string field, byte[] result, int offset)
		{
			lock (this)
			{
				EnsureOpen();
				byte[] bytes = normsCache.ContainsKey(field)? normsCache[field] : null;
				if (bytes == null && !HasNorms(field))
					bytes = fakeNorms();
				if (bytes != null)
				// cache hit
					System.Array.Copy(bytes, 0, result, offset, MaxDoc());
				
				for (int i = 0; i < subReaders.Length; i++)
				// read from segments
					subReaders[i].Norms(field, result, offset + starts[i]);
			}
		}
		
		protected internal override void  DoSetNorm(int n, string field, byte value_Renamed)
		{
            lock (normsCache)
            {
                normsCache.Remove(field); // clear cache
            }
			int i = ReaderIndex(n); // find segment num
			subReaders[i].SetNorm(n - starts[i], field, value_Renamed); // dispatch
		}
		
		public override TermEnum Terms()
		{
			EnsureOpen();
			return new MultiTermEnum(subReaders, starts, null);
		}
		
		public override TermEnum Terms(Term term)
		{
			EnsureOpen();
			return new MultiTermEnum(subReaders, starts, term);
		}
		
		public override int DocFreq(Term t)
		{
			EnsureOpen();
			int total = 0; // sum freqs in segments
			for (int i = 0; i < subReaders.Length; i++)
				total += subReaders[i].DocFreq(t);
			return total;
		}
		
		public override TermDocs TermDocs()
		{
			EnsureOpen();
			return new MultiTermDocs(subReaders, starts);
		}
		
		public override TermPositions TermPositions()
		{
			EnsureOpen();
			return new MultiTermPositions(subReaders, starts);
		}
		
		protected internal override void  CommitChanges()
		{
			for (int i = 0; i < subReaders.Length; i++)
				subReaders[i].Commit();
		}
		
		internal override void  StartCommit()
		{
			base.StartCommit();
			for (int i = 0; i < subReaders.Length; i++)
			{
				subReaders[i].StartCommit();
			}
		}
		
		internal override void  RollbackCommit()
		{
			base.RollbackCommit();
			for (int i = 0; i < subReaders.Length; i++)
			{
				subReaders[i].RollbackCommit();
			}
		}
		
		protected internal override void  DoClose()
		{
			lock (this)
			{
				for (int i = 0; i < subReaders.Length; i++)
					subReaders[i].DecRef();
				
				// maybe close directory
				base.DoClose();
			}
		}
		
		public override System.Collections.Generic.ICollection<string> GetFieldNames(IndexReader.FieldOption fieldNames)
		{
			EnsureOpen();
			return GetFieldNames(fieldNames, this.subReaders);
		}
		
		internal static System.Collections.Generic.ICollection<string> GetFieldNames(IndexReader.FieldOption fieldNames, IndexReader[] subReaders)
		{
			// maintain a unique set of field names
            System.Collections.Generic.Dictionary<string, string> fieldSet = new System.Collections.Generic.Dictionary<string, string>();
			for (int i = 0; i < subReaders.Length; i++)
			{
				IndexReader reader = subReaders[i];
                System.Collections.Generic.IEnumerator<string> names = reader.GetFieldNames(fieldNames).GetEnumerator();
                while (names.MoveNext())
				{
                    fieldSet[names.Current] = names.Current;
				}
			}
			return fieldSet.Keys;
		}
		
		// for testing
		public /*internal*/ virtual SegmentReader[] GetSubReaders()
		{
			return subReaders;
		}
		
		public override void  SetTermInfosIndexDivisor(int indexDivisor)
		{
			for (int i = 0; i < subReaders.Length; i++)
				subReaders[i].SetTermInfosIndexDivisor(indexDivisor);
		}
		
		public override int GetTermInfosIndexDivisor()
		{
			if (subReaders.Length > 0)
				return subReaders[0].GetTermInfosIndexDivisor();
			else
				throw new System.SystemException("no readers");
		}
		
		internal class MultiTermEnum:TermEnum
		{
			private SegmentMergeQueue queue;
			
			private Term term;
			private int docFreq;
			
			public MultiTermEnum(IndexReader[] readers, int[] starts, Term t)
			{
				queue = new SegmentMergeQueue(readers.Length);
				for (int i = 0; i < readers.Length; i++)
				{
					IndexReader reader = readers[i];
					TermEnum termEnum;
					
					if (t != null)
					{
						termEnum = reader.Terms(t);
					}
					else
						termEnum = reader.Terms();
					
					SegmentMergeInfo smi = new SegmentMergeInfo(starts[i], termEnum, reader);
					if (t == null ? smi.Next() : termEnum.Term() != null)
						queue.Put(smi);
					// initialize queue
					else
						smi.Close();
				}
				
				if (t != null && queue.Size() > 0)
				{
					Next();
				}
			}
			
			public override bool Next()
			{
				SegmentMergeInfo top = (SegmentMergeInfo) queue.Top();
				if (top == null)
				{
					term = null;
					return false;
				}
				
				term = top.term;
				docFreq = 0;
				
				while (top != null && term.CompareTo(top.term) == 0)
				{
					queue.Pop();
					docFreq += top.termEnum.DocFreq(); // increment freq
					if (top.Next())
						queue.Put(top);
					// restore queue
					else
						top.Close(); // done with a segment
					top = (SegmentMergeInfo) queue.Top();
				}
				return true;
			}
			
			public override Term Term()
			{
				return term;
			}
			
			public override int DocFreq()
			{
				return docFreq;
			}
			
			public override void  Close()
			{
				queue.Close();
			}
		}
		
		internal class MultiTermDocs : TermDocs
		{
			protected internal IndexReader[] readers;
			protected internal int[] starts;
			protected internal Term term;
			
			protected internal int base_Renamed = 0;
			protected internal int pointer = 0;
			
			private TermDocs[] readerTermDocs;
			protected internal TermDocs current; // == readerTermDocs[pointer]
			
			public MultiTermDocs(IndexReader[] r, int[] s)
			{
				readers = r;
				starts = s;
				
				readerTermDocs = new TermDocs[r.Length];
			}
			
			public virtual int Doc()
			{
				return base_Renamed + current.Doc();
			}
			public virtual int Freq()
			{
				return current.Freq();
			}
			
			public virtual void  Seek(Term term)
			{
				this.term = term;
				this.base_Renamed = 0;
				this.pointer = 0;
				this.current = null;
			}
			
			public virtual void  Seek(TermEnum termEnum)
			{
				Seek(termEnum.Term());
			}
			
			public virtual bool Next()
			{
				for (; ; )
				{
					if (current != null && current.Next())
					{
						return true;
					}
					else if (pointer < readers.Length)
					{
						base_Renamed = starts[pointer];
						current = TermDocs(pointer++);
					}
					else
					{
						return false;
					}
				}
			}
			
			/// <summary>Optimized implementation. </summary>
			public virtual int Read(int[] docs, int[] freqs)
			{
				while (true)
				{
					while (current == null)
					{
						if (pointer < readers.Length)
						{
							// try next segment
							base_Renamed = starts[pointer];
							current = TermDocs(pointer++);
						}
						else
						{
							return 0;
						}
					}
					int end = current.Read(docs, freqs);
					if (end == 0)
					{
						// none left in segment
						current = null;
					}
					else
					{
						// got some
						int b = base_Renamed; // adjust doc numbers
						for (int i = 0; i < end; i++)
							docs[i] += b;
						return end;
					}
				}
			}
			
			/* A Possible future optimization could skip entire segments */
			public virtual bool SkipTo(int target)
			{
				for (; ; )
				{
					if (current != null && current.SkipTo(target - base_Renamed))
					{
						return true;
					}
					else if (pointer < readers.Length)
					{
						base_Renamed = starts[pointer];
						current = TermDocs(pointer++);
					}
					else
						return false;
				}
			}
			
			private TermDocs TermDocs(int i)
			{
				if (term == null)
					return null;
				TermDocs result = readerTermDocs[i];
				if (result == null)
					result = readerTermDocs[i] = TermDocs(readers[i]);
				result.Seek(term);
				return result;
			}
			
			protected internal virtual TermDocs TermDocs(IndexReader reader)
			{
				return reader.TermDocs();
			}
			
			public virtual void  Close()
			{
				for (int i = 0; i < readerTermDocs.Length; i++)
				{
					if (readerTermDocs[i] != null)
						readerTermDocs[i].Close();
				}
			}
		}
		
		internal class MultiTermPositions:MultiTermDocs, TermPositions
		{
			public MultiTermPositions(IndexReader[] r, int[] s):base(r, s)
			{
			}
			
			protected internal override TermDocs TermDocs(IndexReader reader)
			{
				return (TermDocs) reader.TermPositions();
			}
			
			public virtual int NextPosition()
			{
				return ((TermPositions) current).NextPosition();
			}
			
			public virtual int GetPayloadLength()
			{
				return ((TermPositions) current).GetPayloadLength();
			}
			
			public virtual byte[] GetPayload(byte[] data, int offset)
			{
				return ((TermPositions) current).GetPayload(data, offset);
			}
			
			
			// TODO: Remove warning after API has been finalized
			public virtual bool IsPayloadAvailable()
			{
				return ((TermPositions) current).IsPayloadAvailable();
			}
		}
	}
}
