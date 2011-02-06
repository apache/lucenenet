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
using Field = Lucene.Net.Documents.Field;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
{
	
	/// <summary>An IndexReader which reads multiple indexes, appending their content.
	/// 
	/// </summary>
	/// <version>  $Id: MultiReader.java 355181 2005-12-08 19:53:06Z cutting $
	/// </version>
	public class MultiReader : IndexReader
	{
		private IndexReader[] subReaders;
		private int[] starts; // 1st docno for each segment
		private System.Collections.Hashtable normsCache = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());
		private int maxDoc = 0;
		private int numDocs = - 1;
		private bool hasDeletions = false;
		
		/// <summary> <p>Construct a MultiReader aggregating the named set of (sub)readers.
		/// Directory locking for delete, undeleteAll, and setNorm operations is
		/// left to the subreaders. </p>
		/// <p>Note that all subreaders are closed if this Multireader is closed.</p>
		/// </summary>
		/// <param name="subReaders">set of (sub)readers
		/// </param>
		/// <throws>  IOException </throws>
		public MultiReader(IndexReader[] subReaders):base(subReaders.Length == 0?null:subReaders[0].Directory())
		{
			Initialize(subReaders);
		}
		
		/// <summary>Construct reading the named set of readers. </summary>
		public /*internal*/ MultiReader(Directory directory, SegmentInfos sis, bool closeDirectory, IndexReader[] subReaders) : base(directory, sis, closeDirectory)
		{
			Initialize(subReaders);
		}
		
		private void  Initialize(IndexReader[] subReaders)
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
		
		
		/// <summary>Return an array of term frequency vectors for the specified document.
		/// The array contains a vector for each vectorized field in the document.
		/// Each vector vector contains term numbers and frequencies for all terms
		/// in a given vectorized field.
		/// If no such fields existed, the method returns null.
		/// </summary>
		public override TermFreqVector[] GetTermFreqVectors(int n)
		{
			int i = ReaderIndex(n); // find segment num
			return subReaders[i].GetTermFreqVectors(n - starts[i]); // dispatch to segment
		}
		
		public override TermFreqVector GetTermFreqVector(int n, System.String field)
		{
			int i = ReaderIndex(n); // find segment num
			return subReaders[i].GetTermFreqVector(n - starts[i], field);
		}
		
		public override int NumDocs()
		{
			lock (this)
			{
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
			return maxDoc;
		}
		
		public override Document Document(int n)
		{
			int i = ReaderIndex(n); // find segment num
			return subReaders[i].Document(n - starts[i]); // dispatch to segment reader
		}
		
		public override bool IsDeleted(int n)
		{
			int i = ReaderIndex(n); // find segment num
			return subReaders[i].IsDeleted(n - starts[i]); // dispatch to segment reader
		}
		
		public override bool HasDeletions()
		{
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
			int lo = 0; // search starts array
			int hi = subReaders.Length - 1; // for first element less
			
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
					while (mid + 1 < subReaders.Length && starts[mid + 1] == midValue)
					{
						mid++; // scan to last match
					}
					return mid;
				}
			}
			return hi;
		}
		
		public override bool HasNorms(System.String field)
		{
			for (int i = 0; i < subReaders.Length; i++)
			{
				if (subReaders[i].HasNorms(field))
					return true;
			}
			return false;
		}
		
		private byte[] ones;
		private byte[] FakeNorms()
		{
			if (ones == null)
				ones = SegmentReader.CreateFakeNorms(MaxDoc());
			return ones;
		}
		
		public override byte[] Norms(System.String field)
		{
			lock (this)
			{
				byte[] bytes = (byte[]) normsCache[field];
				if (bytes != null)
					return bytes; // cache hit
				if (!HasNorms(field))
					return FakeNorms();
				
				bytes = new byte[MaxDoc()];
				for (int i = 0; i < subReaders.Length; i++)
					subReaders[i].Norms(field, bytes, starts[i]);
				normsCache[field] = bytes; // update cache
				return bytes;
			}
		}
		
		public override void  Norms(System.String field, byte[] result, int offset)
		{
			lock (this)
			{
				byte[] bytes = (byte[]) normsCache[field];
				if (bytes == null && !HasNorms(field))
					bytes = FakeNorms();
				if (bytes != null)
				    // cache hit
					Array.Copy(bytes, 0, result, offset, MaxDoc());
				
				for (int i = 0; i < subReaders.Length; i++)
				    // read from segments
					subReaders[i].Norms(field, result, offset + starts[i]);
			}
		}
		
		protected internal override void  DoSetNorm(int n, System.String field, byte value_Renamed)
		{
			normsCache.Remove(field); // clear cache
			int i = ReaderIndex(n); // find segment num
			subReaders[i].SetNorm(n - starts[i], field, value_Renamed); // dispatch
		}
		
		public override TermEnum Terms()
		{
			return new MultiTermEnum(subReaders, starts, null);
		}
		
		public override TermEnum Terms(Term term)
		{
			return new MultiTermEnum(subReaders, starts, term);
		}
		
		public override int DocFreq(Term t)
		{
			int total = 0; // sum freqs in segments
			for (int i = 0; i < subReaders.Length; i++)
				total += subReaders[i].DocFreq(t);
			return total;
		}
		
		public override TermDocs TermDocs()
		{
			return new MultiTermDocs(subReaders, starts);
		}
		
		public override TermPositions TermPositions()
		{
			return new MultiTermPositions(subReaders, starts);
		}
		
		protected internal override void  DoCommit()
		{
			for (int i = 0; i < subReaders.Length; i++)
				subReaders[i].Commit();
		}
		
		protected internal override void  DoClose()
		{
			lock (this)
			{
				for (int i = 0; i < subReaders.Length; i++)
					subReaders[i].Close();
			}
		}
		
		/// <seealso cref="IndexReader.GetFieldNames(IndexReader.FieldOption)">
		/// </seealso>
		public override System.Collections.ICollection GetFieldNames(IndexReader.FieldOption fieldNames)
		{
			// maintain a unique set of field names
			System.Collections.Hashtable fieldSet = new System.Collections.Hashtable();
			for (int i = 0; i < subReaders.Length; i++)
			{
				IndexReader reader = subReaders[i];
				System.Collections.ICollection names = reader.GetFieldNames(fieldNames);
                for (System.Collections.IEnumerator iterator = names.GetEnumerator(); iterator.MoveNext(); )
                {
                    System.Collections.DictionaryEntry fi = (System.Collections.DictionaryEntry) iterator.Current;
                    System.String s = fi.Key.ToString();
                    if (fieldSet.ContainsKey(s) == false)
                    {
                        fieldSet.Add(s, s);
                    }
                }
            }
			return fieldSet;
		}
	}
	
	class MultiTermEnum : TermEnum
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
				if (t == null?smi.Next():termEnum.Term() != null)
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
	
	class MultiTermDocs : TermDocs
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
			if (current != null && current.Next())
			{
				return true;
			}
			else if (pointer < readers.Length)
			{
				base_Renamed = starts[pointer];
				current = TermDocs(pointer++);
				return Next();
			}
			else
				return false;
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
		
		/// <summary>As yet unoptimized implementation. </summary>
		public virtual bool SkipTo(int target)
		{
			do 
			{
				if (!Next())
					return false;
			}
			while (target > Doc());
			return true;
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
	
	class MultiTermPositions : MultiTermDocs, TermPositions
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
	}
}