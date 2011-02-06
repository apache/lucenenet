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
using MultiTermDocs = Lucene.Net.Index.MultiSegmentReader.MultiTermDocs;
using MultiTermEnum = Lucene.Net.Index.MultiSegmentReader.MultiTermEnum;
using MultiTermPositions = Lucene.Net.Index.MultiSegmentReader.MultiTermPositions;

namespace Lucene.Net.Index
{
	
	/// <summary>An IndexReader which reads multiple indexes, appending their content.
	/// 
	/// </summary>
	/// <version>  $Id: MultiReader.java 596004 2007-11-17 21:34:23Z buschmi $
	/// </version>
	public class MultiReader : IndexReader
	{
		protected internal IndexReader[] subReaders;
		private int[] starts; // 1st docno for each segment
		private bool[] decrefOnClose; // remember which subreaders to decRef on close
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
		public MultiReader(IndexReader[] subReaders)
		{
			Initialize(subReaders, true);
		}
		
		/// <summary> <p>Construct a MultiReader aggregating the named set of (sub)readers.
		/// Directory locking for delete, undeleteAll, and setNorm operations is
		/// left to the subreaders. </p>
		/// </summary>
		/// <param name="closeSubReaders">indicates whether the subreaders should be closed
		/// when this MultiReader is closed
		/// </param>
		/// <param name="subReaders">set of (sub)readers
		/// </param>
		/// <throws>  IOException </throws>
		public MultiReader(IndexReader[] subReaders, bool closeSubReaders)
		{
			Initialize(subReaders, closeSubReaders);
		}
		
		private void  Initialize(IndexReader[] subReaders, bool closeSubReaders)
		{
			this.subReaders = subReaders;
			starts = new int[subReaders.Length + 1]; // build starts array
			decrefOnClose = new bool[subReaders.Length];
			for (int i = 0; i < subReaders.Length; i++)
			{
				starts[i] = maxDoc;
				maxDoc += subReaders[i].MaxDoc(); // compute maxDocs
				
				if (!closeSubReaders)
				{
					subReaders[i].IncRef();
					decrefOnClose[i] = true;
				}
				else
				{
					decrefOnClose[i] = false;
				}
				
				if (subReaders[i].HasDeletions())
					hasDeletions = true;
			}
			starts[subReaders.Length] = maxDoc;
		}
		
		/// <summary> Tries to reopen the subreaders.
		/// <br>
		/// If one or more subreaders could be re-opened (i. e. subReader.reopen() 
		/// returned a new instance != subReader), then a new MultiReader instance 
		/// is returned, otherwise this instance is returned.
		/// <p>
		/// A re-opened instance might share one or more subreaders with the old 
		/// instance. Index modification operations result in undefined behavior
		/// when performed before the old instance is closed.
		/// (see {@link IndexReader#Reopen()}).
		/// <p>
		/// If subreaders are shared, then the reference count of those
		/// readers is increased to ensure that the subreaders remain open
		/// until the last referring reader is closed.
		/// 
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error  </throws>
		public override IndexReader Reopen()
		{
			EnsureOpen();
			
			bool reopened = false;
			IndexReader[] newSubReaders = new IndexReader[subReaders.Length];
			bool[] newDecrefOnClose = new bool[subReaders.Length];
			
			bool success = false;
			try
			{
				for (int i = 0; i < subReaders.Length; i++)
				{
					newSubReaders[i] = subReaders[i].Reopen();
					// if at least one of the subreaders was updated we remember that
					// and return a new MultiReader
					if (newSubReaders[i] != subReaders[i])
					{
						reopened = true;
						// this is a new subreader instance, so on close() we don't
						// decRef but close it 
						newDecrefOnClose[i] = false;
					}
				}
				
				if (reopened)
				{
					for (int i = 0; i < subReaders.Length; i++)
					{
						if (newSubReaders[i] == subReaders[i])
						{
							newSubReaders[i].IncRef();
							newDecrefOnClose[i] = true;
						}
					}
					
					MultiReader mr = new MultiReader(newSubReaders);
					mr.decrefOnClose = newDecrefOnClose;
					success = true;
					return mr;
				}
				else
				{
					success = true;
					return this;
				}
			}
			finally
			{
				if (!success && reopened)
				{
					for (int i = 0; i < newSubReaders.Length; i++)
					{
						if (newSubReaders[i] != null)
						{
							try
							{
								if (newDecrefOnClose[i])
								{
									newSubReaders[i].DecRef();
								}
								else
								{
									newSubReaders[i].Close();
								}
							}
							catch (System.IO.IOException ignore)
							{
								// keep going - we want to clean up as much as possible
							}
						}
					}
				}
			}
		}
		
		public override TermFreqVector[] GetTermFreqVectors(int n)
		{
			EnsureOpen();
			int i = ReaderIndex(n); // find segment num
			return subReaders[i].GetTermFreqVectors(n - starts[i]); // dispatch to segment
		}
		
		public override TermFreqVector GetTermFreqVector(int n, System.String field)
		{
			EnsureOpen();
			int i = ReaderIndex(n); // find segment num
			return subReaders[i].GetTermFreqVector(n - starts[i], field);
		}
		
		
		public override void  GetTermFreqVector(int docNumber, System.String field, TermVectorMapper mapper)
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
			return MultiSegmentReader.ReaderIndex(n, this.starts, this.subReaders.Length);
		}
		
		public override bool HasNorms(System.String field)
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
				EnsureOpen();
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
				EnsureOpen();
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
				{
					if (decrefOnClose[i])
					{
						subReaders[i].DecRef();
					}
					else
					{
						subReaders[i].Close();
					}
				}
			}
		}
		
		public override System.Collections.ICollection GetFieldNames(IndexReader.FieldOption fieldNames)
		{
			EnsureOpen();
			return MultiSegmentReader.GetFieldNames(fieldNames, this.subReaders);
		}
		
		/// <summary> Checks recursively if all subreaders are up to date. </summary>
		public override bool IsCurrent()
		{
			for (int i = 0; i < subReaders.Length; i++)
			{
				if (!subReaders[i].IsCurrent())
				{
					return false;
				}
			}
			
			// all subreaders are up to date
			return true;
		}
		
		/// <summary>Not implemented.</summary>
		/// <throws>  UnsupportedOperationException </throws>
		public override long GetVersion()
		{
			throw new System.NotSupportedException("MultiReader does not support this method.");
		}
		
		// for testing
		public /*internal*/ virtual IndexReader[] GetSubReaders()
		{
			return subReaders;
		}
	}
}