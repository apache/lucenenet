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
using System.Linq;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Document = Lucene.Net.Documents.Document;
using BufferedIndexInput = Lucene.Net.Store.BufferedIndexInput;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using Lucene.Net.Store;
using Lucene.Net.Codecs;

namespace Lucene.Net.Index
{
    public sealed class SegmentReader : AtomicReader
    {
        private readonly SegmentInfoPerCommit si;
        private readonly IBits liveDocs;

        // Normally set to si.docCount - si.delDocCount, unless we
        // were created as an NRT reader from IW, in which case IW
        // tells us the docCount:
        private readonly int numDocs;

        internal readonly SegmentCoreReaders core;

        public SegmentReader(SegmentInfoPerCommit si, int termInfosIndexDivisor, IOContext context)
        {
            this.si = si;
            core = new SegmentCoreReaders(this, si.info.dir, si, context, termInfosIndexDivisor);
            bool success = false;
            try
            {
                if (si.HasDeletions)
                {
                    // NOTE: the bitvector is stored using the regular directory, not cfs
                    liveDocs = si.info.Codec.LiveDocsFormat.ReadLiveDocs(this.Directory, si, new IOContext(IOContext.READ, true));
                }
                else
                {
                    //assert si.getDelCount() == 0;
                    liveDocs = null;
                }
                numDocs = si.info.DocCount - si.DelCount;
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
                    core.DecRef();
                }
            }
        }

        internal SegmentReader(SegmentInfoPerCommit si, SegmentCoreReaders core, IOContext context)
            : this(si, core,
                 si.info.Codec.LiveDocsFormat.ReadLiveDocs(si.info.dir, si, context),
                 si.info.DocCount - si.DelCount)
        {
        }

        internal SegmentReader(SegmentInfoPerCommit si, SegmentCoreReaders core, IBits liveDocs, int numDocs)
        {
            this.si = si;
            this.core = core;
            core.IncRef();

            //assert liveDocs != null;
            this.liveDocs = liveDocs;

            this.numDocs = numDocs;
        }

        public override IBits LiveDocs
        {
            get
            {
                EnsureOpen();
                return liveDocs;
            }
        }

        protected override void DoClose()
        {
            core.DecRef();
        }

        public override FieldInfos FieldInfos
        {
            get
            {
                EnsureOpen();
                return core.fieldInfos;
            }
        }

        public StoredFieldsReader FieldsReader
        {
            get
            {
                EnsureOpen();
                return core.fieldsReaderLocal.Get();
            }
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            CheckBounds(docID);
            FieldsReader.VisitDocument(docID, visitor);
        }

        public override Fields Fields
        {
            get
            {
                EnsureOpen();
                return core.fields;
            }
        }

        public override int NumDocs
        {
            get { return numDocs; }
        }

        public override int MaxDoc
        {
            get { return si.info.DocCount; }
        }

        public TermVectorsReader TermVectorsReader
        {
            get
            {
                EnsureOpen();
                return core.termVectorsLocal.Get();
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            TermVectorsReader termVectorsReader = this.TermVectorsReader;
            if (termVectorsReader == null)
            {
                return null;
            }
            CheckBounds(docID);
            return termVectorsReader.Get(docID);
        }

        private void CheckBounds(int docID)
        {
            if (docID < 0 || docID >= MaxDoc)
            {
                throw new IndexOutOfRangeException("docID must be >= 0 and < maxDoc=" + MaxDoc + " (got docID=" + docID + ")");
            }
        }

        public override string ToString()
        {
            // SegmentInfo.toString takes dir and number of
            // *pending* deletions; so we reverse compute that here:
            return si.ToString(si.info.dir, si.info.DocCount - numDocs - si.DelCount);
        }

        public string SegmentName
        {
            get
            {
                return si.info.name;
            }
        }

        public SegmentInfoPerCommit SegmentInfo
        {
            get
            {
                return si;
            }
        }

        public Directory Directory
        {
            get
            {
                // Don't ensureOpen here -- in certain cases, when a
                // cloned/reopened reader needs to commit, it may call
                // this method on the closed original reader
                return si.info.dir;
            }
        }

        public override object CoreCacheKey
        {
            get
            {
                return core;
            }
        }

        public override object CombinedCoreAndDeletesKey
        {
            get
            {
                return this;
            }
        }

        public int TermInfosIndexDivisor
        {
            get
            {
                return core.termsIndexDivisor;
            }
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            EnsureOpen();
            return core.GetNumericDocValues(field);
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
            return core.GetBinaryDocValues(field);
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
            return core.GetSortedDocValues(field);
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
            return core.GetSortedSetDocValues(field);
        }

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
            return core.GetNormValues(field);
        }

        public interface ICoreClosedListener
        {
            /** Invoked when the shared core of the provided {@link
             *  SegmentReader} has closed. */
            void OnClose(SegmentReader owner);
        }

        public void AddCoreClosedListener(ICoreClosedListener listener)
        {
            EnsureOpen();
            core.AddCoreClosedListener(listener);
        }

        public void RemoveCoreClosedListener(ICoreClosedListener listener)
        {
            EnsureOpen();
            core.RemoveCoreClosedListener(listener);
        }
    }
}