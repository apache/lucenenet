using J2N.Collections.Generic.Extensions;
using System;
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

    /// <summary>
    /// Base class for implementing <see cref="CompositeReader"/>s based on an array
    /// of sub-readers. The implementing class has to add code for
    /// correctly refcounting and closing the sub-readers.
    ///
    /// <para/>User code will most likely use <see cref="MultiReader"/> to build a
    /// composite reader on a set of sub-readers (like several
    /// <see cref="DirectoryReader"/>s).
    ///
    /// <para/> For efficiency, in this API documents are often referred to via
    /// <i>document numbers</i>, non-negative integers which each name a unique
    /// document in the index.  These document numbers are ephemeral -- they may change
    /// as documents are added to and deleted from an index.  Clients should thus not
    /// rely on a given document having the same number between sessions.
    ///
    /// <para/><b>NOTE</b>: 
    /// <see cref="IndexReader"/> instances are completely thread
    /// safe, meaning multiple threads can call any of its methods,
    /// concurrently.  If your application requires external
    /// synchronization, you should <b>not</b> synchronize on the
    /// <see cref="IndexReader"/> instance; use your own
    /// (non-Lucene) objects instead.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <seealso cref="MultiReader"/>
    public abstract class BaseCompositeReader<R> : CompositeReader
        where R : IndexReader
    {
        private readonly R[] subReaders;
        private readonly int[] starts; // 1st docno for each reader
        private readonly int maxDoc;
        private readonly int numDocs;

        /// <summary>
        /// List view solely for <see cref="GetSequentialSubReaders()"/>,
        /// for effectiveness the array is used internally.
        /// </summary>
        private readonly IList<IndexReader> subReadersList; // LUCENENET: Changed from IList<R> to IList<IndexReader> to eliminate casting

        /// <summary>
        /// Constructs a <see cref="BaseCompositeReader{R}"/> on the given <paramref name="subReaders"/>. </summary>
        /// <param name="subReaders"> the wrapped sub-readers. This array is returned by
        /// <see cref="GetSequentialSubReaders()"/> and used to resolve the correct
        /// subreader for docID-based methods. <b>Please note:</b> this array is <b>not</b>
        /// cloned and not protected for modification, the subclass is responsible
        /// to do this. </param>
        protected BaseCompositeReader(R[] subReaders)
        {
            this.subReaders = subReaders;
            this.subReadersList = ((IndexReader[])subReaders).AsReadOnly(); // LUCENENET: Work around generic casting from R to IndexWriter
            starts = new int[subReaders.Length + 1]; // build starts array
            int maxDoc = 0, numDocs = 0;
            for (int i = 0; i < subReaders.Length; i++)
            {
                starts[i] = maxDoc;
                IndexReader r = subReaders[i];
                maxDoc += r.MaxDoc; // compute maxDocs
                if (maxDoc < 0) // overflow
                {
                    throw new ArgumentException("Too many documents, composite IndexReaders cannot exceed " + int.MaxValue);
                }
                numDocs += r.NumDocs; // compute numDocs
                r.RegisterParentReader(this);
            }
            starts[subReaders.Length] = maxDoc;
            this.maxDoc = maxDoc;
            this.numDocs = numDocs;
        }

        public override sealed Fields GetTermVectors(int docID)
        {
            EnsureOpen();
            int i = ReaderIndex(docID); // find subreader num
            return subReaders[i].GetTermVectors(docID - starts[i]); // dispatch to subreader
        }

        public override sealed int NumDocs =>
            // Don't call ensureOpen() here (it could affect performance)
            numDocs;

        public override sealed int MaxDoc =>
            // Don't call ensureOpen() here (it could affect performance)
            maxDoc;

        public override sealed void Document(int docID, StoredFieldVisitor visitor)
        {
            EnsureOpen();
            int i = ReaderIndex(docID); // find subreader num
            subReaders[i].Document(docID - starts[i], visitor); // dispatch to subreader
        }

        public override sealed int DocFreq(Term term)
        {
            EnsureOpen();
            int total = 0; // sum freqs in subreaders
            for (int i = 0; i < subReaders.Length; i++)
            {
                total += subReaders[i].DocFreq(term);
            }
            return total;
        }

        public override sealed long TotalTermFreq(Term term)
        {
            EnsureOpen();
            long total = 0; // sum freqs in subreaders
            for (int i = 0; i < subReaders.Length; i++)
            {
                long sub = subReaders[i].TotalTermFreq(term);
                if (sub == -1)
                {
                    return -1;
                }
                total += sub;
            }
            return total;
        }

        public override sealed long GetSumDocFreq(string field)
        {
            EnsureOpen();
            long total = 0; // sum doc freqs in subreaders
            foreach (R reader in subReaders)
            {
                long sub = reader.GetSumDocFreq(field);
                if (sub == -1)
                {
                    return -1; // if any of the subs doesn't support it, return -1
                }
                total += sub;
            }
            return total;
        }

        public override sealed int GetDocCount(string field)
        {
            EnsureOpen();
            int total = 0; // sum doc counts in subreaders
            foreach (R reader in subReaders)
            {
                int sub = reader.GetDocCount(field);
                if (sub == -1)
                {
                    return -1; // if any of the subs doesn't support it, return -1
                }
                total += sub;
            }
            return total;
        }

        public override sealed long GetSumTotalTermFreq(string field)
        {
            EnsureOpen();
            long total = 0; // sum doc total term freqs in subreaders
            foreach (R reader in subReaders)
            {
                long sub = reader.GetSumTotalTermFreq(field);
                if (sub == -1)
                {
                    return -1; // if any of the subs doesn't support it, return -1
                }
                total += sub;
            }
            return total;
        }

        /// <summary>
        /// Helper method for subclasses to get the corresponding reader for a doc ID </summary>
        protected internal int ReaderIndex(int docID)
        {
            if (docID < 0 || docID >= maxDoc)
            {
                throw new ArgumentOutOfRangeException(nameof(docID), "docID must be >= 0 and < maxDoc=" + maxDoc + " (got docID=" + docID + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            return ReaderUtil.SubIndex(docID, this.starts);
        }

        /// <summary>
        /// Helper method for subclasses to get the docBase of the given sub-reader index. </summary>
        protected internal int ReaderBase(int readerIndex)
        {
            if (readerIndex < 0 || readerIndex >= subReaders.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(readerIndex), "readerIndex must be >= 0 and < GetSequentialSubReaders().Count"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            return this.starts[readerIndex];
        }

        protected internal override sealed IList<IndexReader> GetSequentialSubReaders()
        {
            return subReadersList;
        }
    }
}