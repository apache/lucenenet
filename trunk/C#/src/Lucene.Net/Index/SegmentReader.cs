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

using BitVector = Lucene.Net.Util.BitVector;
using BufferedIndexInput = Lucene.Net.Store.BufferedIndexInput;
using CloseableThreadLocal = Lucene.Net.Util.CloseableThreadLocal;
using DefaultSimilarity = Lucene.Net.Search.DefaultSimilarity;
using Directory = Lucene.Net.Store.Directory;
using Document = Lucene.Net.Documents.Document;
using FieldSelector = Lucene.Net.Documents.FieldSelector;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;

namespace Lucene.Net.Index
{
	public class SegmentReader : DirectoryIndexReader
	{
		private System.String segment;
		private SegmentInfo si;
		private int readBufferSize;
		
		internal FieldInfos fieldInfos;
		private FieldsReader fieldsReader;
		
		internal TermInfosReader tis;
		internal TermVectorsReader termVectorsReaderOrig = null;
        internal CloseableThreadLocal termVectorsLocal = new CloseableThreadLocal();
		
		internal BitVector deletedDocs = null;
		private bool deletedDocsDirty = false;
		private bool normsDirty = false;
		private bool undeleteAll = false;
        private int pendingDeleteCount;
		
		private bool rollbackDeletedDocsDirty = false;
		private bool rollbackNormsDirty = false;
		private bool rollbackUndeleteAll = false;
        private int rollbackPendingDeleteCount;
        new private bool readOnly;
		
		internal IndexInput freqStream;
		internal IndexInput proxStream;

        // for testing
        public IndexInput ProxStream_ForNUnitTest
        {
            get { return proxStream; }
            set { proxStream = value; }
        }
		
		// optionally used for the .nrm file shared by multiple norms
		private IndexInput singleNormStream;
		
		// Compound File Reader when based on a compound file segment
		internal CompoundFileReader cfsReader = null;
		internal CompoundFileReader storeCFSReader = null;
		
		// indicates the SegmentReader with which the resources are being shared,
		// in case this is a re-opened reader
		private SegmentReader referencedSegmentReader = null;
		
		private class Norm
		{
			private void  InitBlock(SegmentReader enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private SegmentReader enclosingInstance;
			public SegmentReader Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal volatile int refCount;
			internal bool useSingleNormStream;
			
			public virtual void  IncRef()
			{
				lock (this)
				{
					System.Diagnostics.Debug.Assert(refCount > 0);
					refCount++;
				}
			}
			
			public virtual void  DecRef()
			{
				lock (this)
				{
					System.Diagnostics.Debug.Assert(refCount > 0);
					if (refCount == 1)
					{
						Close();
					}
					refCount--;
				}
			}
			
			public Norm(SegmentReader enclosingInstance, IndexInput in_Renamed, bool useSingleNormStream, int number, long normSeek)
			{
				InitBlock(enclosingInstance);
				refCount = 1;
				this.in_Renamed = in_Renamed;
				this.number = number;
				this.normSeek = normSeek;
				this.useSingleNormStream = useSingleNormStream;
			}
			
			internal IndexInput in_Renamed;
			internal byte[] bytes;
			internal bool dirty;
			internal int number;
			internal long normSeek;
			internal bool rollbackDirty;
			
			internal void  ReWrite(SegmentInfo si)
			{
				// NOTE: norms are re-written in regular directory, not cfs
				si.AdvanceNormGen(this.number);
				IndexOutput out_Renamed = Enclosing_Instance.Directory().CreateOutput(si.GetNormFileName(this.number));
				try
				{
					out_Renamed.WriteBytes(bytes, Enclosing_Instance.MaxDoc());
				}
				finally
				{
					out_Renamed.Close();
				}
				this.dirty = false;
			}
			
			/// <summary>Closes the underlying IndexInput for this norm.
			/// It is still valid to access all other norm properties after close is called.
			/// </summary>
			/// <throws>  IOException </throws>
			internal void  Close()
			{
				lock (this)
				{
					if (in_Renamed != null && !useSingleNormStream)
					{
						in_Renamed.Close();
					}
					in_Renamed = null;
				}
			}
		}
		
		/// <summary> Increments the RC of this reader, as well as
		/// of all norms this reader is using
		/// </summary>
		public override void IncRef()
		{
			lock (this)
			{
				base.IncRef();
				System.Collections.IEnumerator it = norms.Values.GetEnumerator();
				while (it.MoveNext())
				{
					Norm norm = (Norm) it.Current;
					norm.IncRef();
				}
			}
		}
		
		/// <summary> only increments the RC of this reader, not tof 
		/// he norms. This is important whenever a reopen()
		/// creates a new SegmentReader that doesn't share
		/// the norms with this one 
		/// </summary>
		private void  IncRefReaderNotNorms()
		{
			lock (this)
			{
				base.IncRef();
			}
		}
		
		public override void DecRef()
		{
			lock (this)
			{
				base.DecRef();
				System.Collections.IEnumerator it = norms.Values.GetEnumerator();
				while (it.MoveNext())
				{
					Norm norm = (Norm) it.Current;
				  	norm.DecRef();
				}
			}
		}
		
		private void  DecRefReaderNotNorms()
		{
			lock (this)
			{
				base.DecRef();
			}
		}
		
		internal System.Collections.IDictionary norms = new System.Collections.Hashtable();
		
		/// <summary>The class which implements SegmentReader. </summary>
		private static System.Type IMPL;
		
        private static System.Type READONLY_IMPL;

		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static SegmentReader Get(SegmentInfo si)
		{
			return Get(READ_ONLY_DEFAULT, si.dir, si, null, false, false, BufferedIndexInput.BUFFER_SIZE, true);
		}
		
        public static SegmentReader Get(bool readOnly, SegmentInfo si)
        {
            return Get(readOnly, si.dir, si, null, false, false, BufferedIndexInput.BUFFER_SIZE, true);
        }

		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		internal static SegmentReader Get(SegmentInfo si, bool doOpenStores)
		{
            return Get(READ_ONLY_DEFAULT, si.dir, si, null, false, false, BufferedIndexInput.BUFFER_SIZE, doOpenStores);
		}
		
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static SegmentReader Get(SegmentInfo si, int readBufferSize)
		{
            return Get(READ_ONLY_DEFAULT, si.dir, si, null, false, false, readBufferSize, true);
		}
		
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		internal static SegmentReader Get(SegmentInfo si, int readBufferSize, bool doOpenStores)
		{
            return Get(READ_ONLY_DEFAULT, si.dir, si, null, false, false, readBufferSize, doOpenStores);
		}

        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  IOException if there is a low-level IO error </throws>
        internal static SegmentReader Get(bool readOnly, SegmentInfo si, int readBufferSize, bool doOpenStores)
        {
            return Get(readOnly, si.dir, si, null, false, false, readBufferSize, doOpenStores);
        }

        /// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static SegmentReader Get(bool readOnly, SegmentInfos sis, SegmentInfo si, bool closeDir)
		{
			return Get(readOnly, si.dir, si, sis, closeDir, true, BufferedIndexInput.BUFFER_SIZE, true);
		}
		
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static SegmentReader Get(Directory dir, SegmentInfo si, SegmentInfos sis, bool closeDir, bool ownDir, int readBufferSize)
		{
            return Get(READ_ONLY_DEFAULT, dir, si, sis, closeDir, ownDir, readBufferSize, true);
		}
		
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static SegmentReader Get(bool readOnly, Directory dir, SegmentInfo si, SegmentInfos sis, bool closeDir, bool ownDir, int readBufferSize, bool doOpenStores)
		{
			SegmentReader instance;
			try
			{
                if (readOnly)
                    instance = (SegmentReader)System.Activator.CreateInstance(READONLY_IMPL);
                else
                    instance = (SegmentReader)System.Activator.CreateInstance(IMPL);
            }
			catch (System.Exception e)
			{
				throw new System.Exception("cannot load SegmentReader class: " + e, e);
			}
			instance.Init(dir, sis, closeDir, readOnly);
			instance.Initialize(si, readBufferSize, doOpenStores);
			return instance;
		}
		
		private void  Initialize(SegmentInfo si, int readBufferSize, bool doOpenStores)
		{
			segment = si.name;
			this.si = si;
			this.readBufferSize = readBufferSize;
			
			bool success = false;
			
			try
			{
				// Use compound file directory for some files, if it exists
				Directory cfsDir = Directory();
				if (si.GetUseCompoundFile())
				{
					cfsReader = new CompoundFileReader(Directory(), segment + "." + IndexFileNames.COMPOUND_FILE_EXTENSION, readBufferSize);
					cfsDir = cfsReader;
				}
				
				Directory storeDir;
				
				if (doOpenStores)
				{
					if (si.GetDocStoreOffset() != - 1)
					{
						if (si.GetDocStoreIsCompoundFile())
						{
							storeCFSReader = new CompoundFileReader(Directory(), si.GetDocStoreSegment() + "." + IndexFileNames.COMPOUND_FILE_STORE_EXTENSION, readBufferSize);
							storeDir = storeCFSReader;
						}
						else
						{
							storeDir = Directory();
						}
					}
					else
					{
						storeDir = cfsDir;
					}
				}
				else
					storeDir = null;
				
				fieldInfos = new FieldInfos(cfsDir, segment + ".fnm");

                bool anyProx = false;
                int numFields = fieldInfos.Size();
                for (int i = 0; !anyProx && i < numFields; i++)
                    if (!fieldInfos.FieldInfo(i).omitTf)
                        anyProx = true;

				System.String fieldsSegment;
				
				if (si.GetDocStoreOffset() != - 1)
					fieldsSegment = si.GetDocStoreSegment();
				else
					fieldsSegment = segment;
				
				if (doOpenStores)
				{
					fieldsReader = new FieldsReader(storeDir, fieldsSegment, fieldInfos, readBufferSize, si.GetDocStoreOffset(), si.docCount);
					
					// Verify two sources of "maxDoc" agree:
					if (si.GetDocStoreOffset() == - 1 && fieldsReader.Size() != si.docCount)
					{
						throw new CorruptIndexException("doc counts differ for segment " + si.name + ": fieldsReader shows " + fieldsReader.Size() + " but segmentInfo shows " + si.docCount);
					}
				}
				
				tis = new TermInfosReader(cfsDir, segment, fieldInfos, readBufferSize);
				
				LoadDeletedDocs();
				
				// make sure that all index files have been read or are kept open
				// so that if an index update removes them we'll still have them
				freqStream = cfsDir.OpenInput(segment + ".frq", readBufferSize);
                if (anyProx)
                    proxStream = cfsDir.OpenInput(segment + ".prx", readBufferSize);
                OpenNorms(cfsDir, readBufferSize);
				
				if (doOpenStores && fieldInfos.HasVectors())
				{
					// open term vector files only as needed
					System.String vectorsSegment;
					if (si.GetDocStoreOffset() != - 1)
						vectorsSegment = si.GetDocStoreSegment();
					else
						vectorsSegment = segment;
					termVectorsReaderOrig = new TermVectorsReader(storeDir, vectorsSegment, fieldInfos, readBufferSize, si.GetDocStoreOffset(), si.docCount);
				}
				success = true;
			}
			finally
			{
				
				// With lock-less commits, it's entirely possible (and
				// fine) to hit a FileNotFound exception above.  In
				// this case, we want to explicitly close any subset
				// of things that were opened so that we don't have to
				// wait for a GC to do so.
				if (!success)
				{
					DoClose();
				}
			}
		}

        private void LoadDeletedDocs()
        {
            // NOTE: the bitvector is stored using the regular directory, not cfs
            if (HasDeletions(si))
            {
                deletedDocs = new BitVector(Directory(), si.GetDelFileName());

                System.Diagnostics.Debug.Assert(si.GetDelCount() == deletedDocs.Count(),
                    "delete count mismatch: info=" + si.GetDelCount() + " vs BitVector=" + deletedDocs.Count());

                // Verify # deletes does not exceed maxDoc for this segment:
                System.Diagnostics.Debug.Assert(si.GetDelCount() <= MaxDoc(),
                  "delete count mismatch: " + deletedDocs.Count() + ") exceeds max doc (" + MaxDoc() + ") for segment " + si.name);
            }
            else
                System.Diagnostics.Debug.Assert(si.GetDelCount() == 0);
        }
		
		protected internal override DirectoryIndexReader DoReopen(SegmentInfos infos)
		{
			lock (this)
			{
				DirectoryIndexReader newReader;
				
				if (infos.Count == 1)
				{
					SegmentInfo si = infos.Info(0);
					if (segment.Equals(si.name) && si.GetUseCompoundFile() == this.si.GetUseCompoundFile())
					{
						newReader = ReopenSegment(si);
					}
					else
					{
						// segment not referenced anymore, reopen not possible
						// or segment format changed
						newReader = SegmentReader.Get(readOnly, infos, infos.Info(0), false);
					}
				}
				else
				{
                    if (readOnly)
                        return new ReadOnlyMultiSegmentReader(directory, infos, closeDirectory, new SegmentReader[] { this }, null, null);
                    else
                        return new MultiSegmentReader(directory, infos, closeDirectory, new SegmentReader[] { this }, null, null, false);
                }
				
				return newReader;
			}
		}
		
		internal virtual SegmentReader ReopenSegment(SegmentInfo si)
		{
			lock (this)
			{
				bool deletionsUpToDate = (this.si.HasDeletions() == si.HasDeletions()) && (!si.HasDeletions() || this.si.GetDelFileName().Equals(si.GetDelFileName()));
				bool normsUpToDate = true;
				
				
				bool[] fieldNormsChanged = new bool[fieldInfos.Size()];
				if (normsUpToDate)
				{
					for (int i = 0; i < fieldInfos.Size(); i++)
					{
						if (!this.si.GetNormFileName(i).Equals(si.GetNormFileName(i)))
						{
							normsUpToDate = false;
							fieldNormsChanged[i] = true;
						}
					}
				}
				
				if (normsUpToDate && deletionsUpToDate)
				{
					return this;
				}
				
				
				// clone reader
				SegmentReader clone;
                if (readOnly)
                    clone = new ReadOnlySegmentReader();
                else
                    clone = new SegmentReader();

                bool success = false;
				try
				{
                    clone.readOnly = readOnly;
					clone.directory = directory;
					clone.si = si;
					clone.segment = segment;
					clone.readBufferSize = readBufferSize;
					clone.cfsReader = cfsReader;
					clone.storeCFSReader = storeCFSReader;
					
					clone.fieldInfos = fieldInfos;
					clone.tis = tis;
					clone.freqStream = freqStream;
					clone.proxStream = proxStream;
					clone.termVectorsReaderOrig = termVectorsReaderOrig;
					
					
					// we have to open a new FieldsReader, because it is not thread-safe
					// and can thus not be shared among multiple SegmentReaders
					// TODO: Change this in case FieldsReader becomes thread-safe in the future
					System.String fieldsSegment;
					
					Directory storeDir = Directory();
					
					if (si.GetDocStoreOffset() != - 1)
					{
						fieldsSegment = si.GetDocStoreSegment();
						if (storeCFSReader != null)
						{
							storeDir = storeCFSReader;
						}
					}
					else
					{
						fieldsSegment = segment;
						if (cfsReader != null)
						{
							storeDir = cfsReader;
						}
					}
					
					if (fieldsReader != null)
					{
						clone.fieldsReader = new FieldsReader(storeDir, fieldsSegment, fieldInfos, readBufferSize, si.GetDocStoreOffset(), si.docCount);
					}
					
					
					if (!deletionsUpToDate)
					{
						// load deleted docs
						clone.deletedDocs = null;
						clone.LoadDeletedDocs();
					}
					else
					{
						clone.deletedDocs = this.deletedDocs;
					}
					
					clone.norms = new System.Collections.Hashtable();
					if (!normsUpToDate)
					{
						// load norms
						for (int i = 0; i < fieldNormsChanged.Length; i++)
						{
							// copy unchanged norms to the cloned reader and incRef those norms
							if (!fieldNormsChanged[i])
							{
								System.String curField = fieldInfos.FieldInfo(i).name;
								Norm norm = (Norm) this.norms[curField];
								norm.IncRef();
								clone.norms[curField] = norm;
							}
						}
						
						clone.OpenNorms(si.GetUseCompoundFile() ? cfsReader : Directory(), readBufferSize);
					}
					else
					{
						System.Collections.IEnumerator it = norms.Keys.GetEnumerator();
						while (it.MoveNext())
						{
							System.String field = (System.String) it.Current;
							Norm norm = (Norm) norms[field];
							norm.IncRef();
							clone.norms[field] = norm;
						}
					}
					
					if (clone.singleNormStream == null)
					{
						for (int i = 0; i < fieldInfos.Size(); i++)
						{
							FieldInfo fi = fieldInfos.FieldInfo(i);
							if (fi.isIndexed && !fi.omitNorms)
							{
								Directory d = si.GetUseCompoundFile() ? cfsReader : Directory();
								System.String fileName = si.GetNormFileName(fi.number);
								if (si.HasSeparateNorms(fi.number))
								{
									continue;
								}
								
								if (fileName.EndsWith("." + IndexFileNames.NORMS_EXTENSION))
								{
									clone.singleNormStream = d.OpenInput(fileName, readBufferSize);
									break;
								}
							}
						}
					}
					
					success = true;
				}
				finally
				{
					if (this.referencedSegmentReader != null)
					{
						// this reader shares resources with another SegmentReader,
						// so we increment the other readers refCount. We don't
						// increment the refCount of the norms because we did
						// that already for the shared norms
						clone.referencedSegmentReader = this.referencedSegmentReader;
						referencedSegmentReader.IncRefReaderNotNorms();
					}
					else
					{
						// this reader wasn't reopened, so we increment this
						// readers refCount
						clone.referencedSegmentReader = this;
						IncRefReaderNotNorms();
					}
					
					if (!success)
					{
						// An exception occured during reopen, we have to decRef the norms
						// that we incRef'ed already and close singleNormsStream and FieldsReader
						clone.DecRef();
					}
				}
				
				return clone;
			}
		}
		
		protected internal override void  CommitChanges()
		{
			if (deletedDocsDirty)
			{
				// re-write deleted
				si.AdvanceDelGen();
				
				// We can write directly to the actual name (vs to a
				// .tmp & renaming it) because the file is not live
				// until segments file is written:
				deletedDocs.Write(Directory(), si.GetDelFileName());

                si.SetDelCount(si.GetDelCount() + pendingDeleteCount);
			}
			if (undeleteAll && si.HasDeletions())
			{
				si.ClearDelGen();
                si.SetDelCount(0);
            }
			if (normsDirty)
			{
				// re-write norms
				si.SetNumFields(fieldInfos.Size());
				System.Collections.IEnumerator it = norms.Values.GetEnumerator();
				while (it.MoveNext())
				{
					Norm norm = (Norm) it.Current;
					if (norm.dirty)
					{
						norm.ReWrite(si);
					}
				}
			}
			deletedDocsDirty = false;
			normsDirty = false;
			undeleteAll = false;
		}
		
		internal virtual FieldsReader GetFieldsReader()
		{
			return fieldsReader;
		}

        public void foo()
        { 
            //termVectorsLocal.Clear();
            termVectorsLocal.Close(); 
        }

		protected internal override void  DoClose()
		{
            bool hasReferencedReader = (referencedSegmentReader != null);

            termVectorsLocal.Close();
            //termVectorsLocal.Clear();

            if (hasReferencedReader)
            {
                referencedSegmentReader.DecRefReaderNotNorms();
                referencedSegmentReader = null;
            }

            deletedDocs = null;

            // close the single norms stream
            if (singleNormStream != null)
            {
                // we can close this stream, even if the norms
                // are shared, because every reader has it's own 
                // singleNormStream
                singleNormStream.Close();
                singleNormStream = null;
            }

            // re-opened SegmentReaders have their own instance of FieldsReader
            if (fieldsReader != null)
            {
                fieldsReader.Close();
            }

            if (!hasReferencedReader)
            {
                // close everything, nothing is shared anymore with other readers
                if (tis != null)
                {
                    tis.Close();
                }

                if (freqStream != null)
                    freqStream.Close();
                if (proxStream != null)
                    proxStream.Close();

                if (termVectorsReaderOrig != null)
                    termVectorsReaderOrig.Close();

                if (cfsReader != null)
                    cfsReader.Close();

                if (storeCFSReader != null)
                    storeCFSReader.Close();

                // maybe close directory
                base.DoClose();
            }
		}
		
		internal static bool HasDeletions(SegmentInfo si)
		{
			// Don't call ensureOpen() here (it could affect performance)
			return si.HasDeletions();
		}
		
		public override bool HasDeletions()
		{
			// Don't call ensureOpen() here (it could affect performance)
			return deletedDocs != null;
		}
		
		internal static bool UsesCompoundFile(SegmentInfo si)
		{
			return si.GetUseCompoundFile();
		}
		
		internal static bool HasSeparateNorms(SegmentInfo si)
		{
			return si.HasSeparateNorms();
		}
		
		protected internal override void  DoDelete(int docNum)
		{
			if (deletedDocs == null)
				deletedDocs = new BitVector(MaxDoc());
			deletedDocsDirty = true;
			undeleteAll = false;
			if (!deletedDocs.GetAndSet(docNum))
                pendingDeleteCount++;
		}
		
		protected internal override void  DoUndeleteAll()
		{
			deletedDocs = null;
			deletedDocsDirty = false;
			undeleteAll = true;
		}
		
		internal virtual List<string> Files()
		{
            List<string> copy = new List<string>(si.Files().Count);
            copy.AddRange(si.Files());
            return copy;
        }

        public override TermEnum Terms()
		{
			EnsureOpen();
			return tis.Terms();
		}
		
		public override TermEnum Terms(Term t)
		{
			EnsureOpen();
			return tis.Terms(t);
		}
		
		internal virtual FieldInfos GetFieldInfos()
		{
			return fieldInfos;
		}
		
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public override Document Document(int n, FieldSelector fieldSelector)
		{
			lock (this)
			{
				EnsureOpen();
				if (IsDeleted(n))
					throw new System.ArgumentException("attempt to access a deleted document");
				return fieldsReader.Doc(n, fieldSelector);
			}
		}
		
		public override bool IsDeleted(int n)
		{
			lock (this)
			{
				return (deletedDocs != null && deletedDocs.Get(n));
			}
		}
		
		public override TermDocs TermDocs()
		{
			EnsureOpen();
			return new SegmentTermDocs(this);
		}
		
		public override TermPositions TermPositions()
		{
			EnsureOpen();
			return new SegmentTermPositions(this);
		}
		
		public override int DocFreq(Term t)
		{
			EnsureOpen();
			TermInfo ti = tis.Get(t);
			if (ti != null)
				return ti.docFreq;
			else
				return 0;
		}
		
		public override int NumDocs()
		{
			// Don't call ensureOpen() here (it could affect performance)
			int n = MaxDoc();
			if (deletedDocs != null)
				n -= deletedDocs.Count();
			return n;
		}
		
		public override int MaxDoc()
		{
			// Don't call ensureOpen() here (it could affect performance)
			return si.docCount;
		}
		
		public override void  SetTermInfosIndexDivisor(int indexDivisor)
		{
			tis.SetIndexDivisor(indexDivisor);
		}
		
		public override int GetTermInfosIndexDivisor()
		{
			return tis.GetIndexDivisor();
		}
		
		/// <seealso cref="IndexReader.GetFieldNames(IndexReader.FieldOption fldOption)">
		/// </seealso>
		public override System.Collections.Generic.ICollection<string> GetFieldNames(IndexReader.FieldOption fieldOption)
		{
			EnsureOpen();

            System.Collections.Generic.Dictionary<string, string> fieldSet = new System.Collections.Generic.Dictionary<string, string>();
			for (int i = 0; i < fieldInfos.Size(); i++)
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				if (fieldOption == IndexReader.FieldOption.ALL)
				{
                    fieldSet[fi.name] = fi.name;
				}
				else if (!fi.isIndexed && fieldOption == IndexReader.FieldOption.UNINDEXED)
				{
                    fieldSet[fi.name] = fi.name;
				}
                else if (fi.omitTf && fieldOption == IndexReader.FieldOption.OMIT_TF)
                {
                    fieldSet[fi.name] = fi.name;
                }
                else if (fi.storePayloads && fieldOption == IndexReader.FieldOption.STORES_PAYLOADS)
                {
                    fieldSet[fi.name] = fi.name;
                }
                else if (fi.isIndexed && fieldOption == IndexReader.FieldOption.INDEXED)
                {
                    fieldSet[fi.name] = fi.name;
                }
                else if (fi.isIndexed && fi.storeTermVector == false && fieldOption == IndexReader.FieldOption.INDEXED_NO_TERMVECTOR)
                {
                    fieldSet[fi.name] = fi.name;
                }
                else if (fi.storeTermVector == true && fi.storePositionWithTermVector == false && fi.storeOffsetWithTermVector == false && fieldOption == IndexReader.FieldOption.TERMVECTOR)
                {
                    fieldSet[fi.name] = fi.name;
                }
                else if (fi.isIndexed && fi.storeTermVector && fieldOption == IndexReader.FieldOption.INDEXED_WITH_TERMVECTOR)
                {
                    fieldSet[fi.name] = fi.name;
                }
                else if (fi.storePositionWithTermVector && fi.storeOffsetWithTermVector == false && fieldOption == IndexReader.FieldOption.TERMVECTOR_WITH_POSITION)
                {
                    fieldSet[fi.name] = fi.name;
                }
                else if (fi.storeOffsetWithTermVector && fi.storePositionWithTermVector == false && fieldOption == IndexReader.FieldOption.TERMVECTOR_WITH_OFFSET)
                {
                    fieldSet[fi.name] = fi.name;
                }
                else if ((fi.storeOffsetWithTermVector && fi.storePositionWithTermVector) && fieldOption == IndexReader.FieldOption.TERMVECTOR_WITH_POSITION_OFFSET)
                {
                    fieldSet[fi.name] = fi.name;
                }
			}
			return fieldSet.Keys;
		}
		
		
		public override bool HasNorms(System.String field)
		{
			lock (this)
			{
				EnsureOpen();
				return norms.Contains(field);
			}
		}
		
		internal static byte[] CreateFakeNorms(int size)
		{
			byte[] ones = new byte[size];
			byte val = DefaultSimilarity.EncodeNorm(1.0f);
			for (int index = 0; index < size; index++)
				ones[index] = val;
			return ones;
		}
		
		private byte[] ones;
		private byte[] FakeNorms()
		{
			if (ones == null)
				ones = CreateFakeNorms(MaxDoc());
			return ones;
		}
		
		// can return null if norms aren't stored
		protected internal virtual byte[] GetNorms(System.String field)
		{
			lock (this)
			{
				Norm norm = (Norm) norms[field];
				if (norm == null)
					return null; // not indexed, or norms not stored
				lock (norm)
				{
					if (norm.bytes == null)
					{
						// value not yet read
						byte[] bytes = new byte[MaxDoc()];
						Norms(field, bytes, 0);
						norm.bytes = bytes; // cache it
						// it's OK to close the underlying IndexInput as we have cached the
						// norms and will never read them again.
						norm.Close();
					}
					return norm.bytes;
				}
			}
		}
		
		// returns fake norms if norms aren't available
		public override byte[] Norms(System.String field)
		{
			lock (this)
			{
				EnsureOpen();
				byte[] bytes = GetNorms(field);

//for (int i = 0; i < bytes.Length; i++)
//    if (bytes[i] == 0)
//        System.Console.Error.WriteLine("in directory " + directory + " norm for " + field + " = 0 for doc " + i);
//    System.Diagnostics.Debug.Assert(bytes[i] > 0, "norm for " + field + " = 0 for doc " + i);

                if (bytes == null)
					bytes = FakeNorms();
				return bytes;
			}
		}
		
		protected internal override void  DoSetNorm(int doc, System.String field, byte value_Renamed)
		{
			Norm norm = (Norm) norms[field];
			if (norm == null)
			// not an indexed field
				return ;
			
			norm.dirty = true; // mark it dirty
			normsDirty = true;
			
			Norms(field)[doc] = value_Renamed; // set the value
		}
		
		/// <summary>Read norms into a pre-allocated array. </summary>
		public override void  Norms(System.String field, byte[] bytes, int offset)
		{
			lock (this)
			{
				
				EnsureOpen();
				Norm norm = (Norm) norms[field];
				if (norm == null)
				{
					System.Array.Copy(FakeNorms(), 0, bytes, offset, MaxDoc());
					return ;
				}
				
				lock (norm)
				{
					if (norm.bytes != null)
					{
						// can copy from cache
						System.Array.Copy(norm.bytes, 0, bytes, offset, MaxDoc());
						return ;
					}
					
					// Read from disk.  norm.in may be shared across  multiple norms and
					// should only be used in a synchronized context.
					IndexInput normStream;
					if (norm.useSingleNormStream)
					{
						normStream = singleNormStream;
					}
					else
					{
						normStream = norm.in_Renamed;
					}
					normStream.Seek(norm.normSeek);
					normStream.ReadBytes(bytes, offset, MaxDoc());
				}
			}
		}
		
		
		private void  OpenNorms(Directory cfsDir, int readBufferSize)
		{
			long nextNormSeek = SegmentMerger.NORMS_HEADER.Length; //skip header (header unused for now)
			int maxDoc = MaxDoc();
			for (int i = 0; i < fieldInfos.Size(); i++)
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				if (norms.Contains(fi.name))
				{
					// in case this SegmentReader is being re-opened, we might be able to
					// reuse some norm instances and skip loading them here
					continue;
				}
				if (fi.isIndexed && !fi.omitNorms)
				{
					Directory d = Directory();
					System.String fileName = si.GetNormFileName(fi.number);
					if (!si.HasSeparateNorms(fi.number))
					{
						d = cfsDir;
					}
					
					// singleNormFile means multiple norms share this file
					bool singleNormFile = fileName.EndsWith("." + IndexFileNames.NORMS_EXTENSION);
					IndexInput normInput = null;
					long normSeek;
					
					if (singleNormFile)
					{
						normSeek = nextNormSeek;
						if (singleNormStream == null)
						{
							singleNormStream = d.OpenInput(fileName, readBufferSize);
						}
						// All norms in the .nrm file can share a single IndexInput since
						// they are only used in a synchronized context.
						// If this were to change in the future, a clone could be done here.
						normInput = singleNormStream;
					}
					else
					{
						normSeek = 0;
						normInput = d.OpenInput(fileName);
					}
					
					norms[fi.name] = new Norm(this, normInput, singleNormFile, fi.number, normSeek);
					nextNormSeek += maxDoc; // increment also if some norms are separate
				}
			}
		}
		
		// for testing only
		public /*internal*/ virtual bool NormsClosed()
		{
			if (singleNormStream != null)
			{
				return false;
			}
			System.Collections.IEnumerator it = norms.Values.GetEnumerator();
			while (it.MoveNext())
			{
				Norm norm = (Norm) it.Current;
				if (norm.refCount > 0)
				{
					return false;
				}
			}
			return true;
		}
		
		// for testing only
		public /*internal*/ virtual bool NormsClosed(System.String field)
		{
			Norm norm = (Norm) norms[field];
			return norm.refCount == 0;
		}
		
		/// <summary> Create a clone from the initial TermVectorsReader and store it in the ThreadLocal.</summary>
		/// <returns> TermVectorsReader
		/// </returns>
		private TermVectorsReader GetTermVectorsReader()
		{
            System.Diagnostics.Debug.Assert(termVectorsReaderOrig != null);
            TermVectorsReader tvReader = (TermVectorsReader)termVectorsLocal.Get();
            if (tvReader == null)
            {
                try
                {
                    tvReader = (TermVectorsReader)termVectorsReaderOrig.Clone();
                }
                catch (System.Exception)
                {
                    return null;
                }
                termVectorsLocal.Set(tvReader);
            }
			return tvReader;
		}
		
		/// <summary>Return a term frequency vector for the specified document and field. The
		/// vector returned contains term numbers and frequencies for all terms in
		/// the specified field of this document, if the field had storeTermVector
		/// flag set.  If the flag was not set, the method returns null.
		/// </summary>
		/// <throws>  IOException </throws>
		public override TermFreqVector GetTermFreqVector(int docNumber, System.String field)
		{
			// Check if this field is invalid or has no stored term vector
			EnsureOpen();
			FieldInfo fi = fieldInfos.FieldInfo(field);
			if (fi == null || !fi.storeTermVector || termVectorsReaderOrig == null)
				return null;
			
			TermVectorsReader termVectorsReader = GetTermVectorsReader();
			if (termVectorsReader == null)
				return null;
			
			return termVectorsReader.Get(docNumber, field);
		}
		
		public override void  GetTermFreqVector(int docNumber, System.String field, TermVectorMapper mapper)
		{
			EnsureOpen();
			FieldInfo fi = fieldInfos.FieldInfo(field);
			if (fi == null || !fi.storeTermVector || termVectorsReaderOrig == null)
				return ;
			
			TermVectorsReader termVectorsReader = GetTermVectorsReader();
			if (termVectorsReader == null)
			{
				return ;
			}
			
			
			termVectorsReader.Get(docNumber, field, mapper);
		}
		
		
		public override void  GetTermFreqVector(int docNumber, TermVectorMapper mapper)
		{
			EnsureOpen();
			if (termVectorsReaderOrig == null)
				return ;
			
			TermVectorsReader termVectorsReader = GetTermVectorsReader();
			if (termVectorsReader == null)
				return ;
			
			termVectorsReader.Get(docNumber, mapper);
		}
		
		/// <summary>Return an array of term frequency vectors for the specified document.
		/// The array contains a vector for each vectorized field in the document.
		/// Each vector vector contains term numbers and frequencies for all terms
		/// in a given vectorized field.
		/// If no such fields existed, the method returns null.
		/// </summary>
		/// <throws>  IOException </throws>
		public override TermFreqVector[] GetTermFreqVectors(int docNumber)
		{
			EnsureOpen();
			if (termVectorsReaderOrig == null)
				return null;
			
			TermVectorsReader termVectorsReader = GetTermVectorsReader();
			if (termVectorsReader == null)
				return null;
			
			return termVectorsReader.Get(docNumber);
		}
		
		/// <summary>Returns the field infos of this segment </summary>
		public /*internal*/ virtual FieldInfos FieldInfos()
		{
			return fieldInfos;
		}
		
		/// <summary> Return the name of the segment this reader is reading.</summary>
		internal virtual System.String GetSegmentName()
		{
			return segment;
		}
		
		/// <summary> Return the SegmentInfo of the segment this reader is reading.</summary>
		internal virtual SegmentInfo GetSegmentInfo()
		{
			return si;
		}
		
		internal virtual void  SetSegmentInfo(SegmentInfo info)
		{
			si = info;
		}
		
		internal override void  StartCommit()
		{
			base.StartCommit();
			rollbackDeletedDocsDirty = deletedDocsDirty;
			rollbackNormsDirty = normsDirty;
			rollbackUndeleteAll = undeleteAll;
            rollbackPendingDeleteCount = pendingDeleteCount;
			System.Collections.IEnumerator it = norms.Values.GetEnumerator();
			while (it.MoveNext())
			{
				Norm norm = (Norm) it.Current;
				norm.rollbackDirty = norm.dirty;
			}
		}
		
		internal override void  RollbackCommit()
		{
			base.RollbackCommit();
			deletedDocsDirty = rollbackDeletedDocsDirty;
			normsDirty = rollbackNormsDirty;
			undeleteAll = rollbackUndeleteAll;
            pendingDeleteCount = rollbackPendingDeleteCount;
			System.Collections.IEnumerator it = norms.Values.GetEnumerator();
			while (it.MoveNext())
			{
				Norm norm = (Norm) it.Current;
				norm.dirty = norm.rollbackDirty;
			}
		}
		static SegmentReader()
		{
            // IMPL type
            try
            {
                string name = SupportClass.AppSettings.Get("Lucene.Net.SegmentReader.class", typeof(SegmentReader).FullName);
                IMPL = System.Type.GetType(name);
            }
            catch (System.Security.SecurityException)
            {
                try
                {
                    IMPL = System.Type.GetType(typeof(SegmentReader).FullName);
                }
                catch (System.Exception e)
                {
                    throw new System.Exception("cannot load default SegmentReader class: " + e, e);
                }
            }
            catch (System.Exception e) { throw new System.Exception("cannot load SegmentReader class: " + e, e); }
            // READONLY_IMPL type
            try
            {
                string name = SupportClass.AppSettings.Get("Lucene.Net.ReadOnlySegmentReader.class", typeof(ReadOnlySegmentReader).FullName);
                READONLY_IMPL = System.Type.GetType(name);
            }
            catch (System.Security.SecurityException)
            {
                try
                {
                    READONLY_IMPL = System.Type.GetType(typeof(ReadOnlySegmentReader).FullName);
                }
                catch (System.Exception e)
                {
                    throw new System.Exception("cannot load default ReadOnlySegmentReader class: " + e, e);
                }
            }
            catch (System.Exception e) { throw new System.Exception("cannot load ReadOnlSegmentReader class: " + e, e); }
        }
    }
}