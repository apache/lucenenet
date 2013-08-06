using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class BaseCompositeReader<R> : CompositeReader
        where R : IndexReader
    {
        private readonly R[] subReaders;
        private readonly int[] starts;
        private readonly int maxDoc;
        private readonly int numDocs;

        private readonly IList<R> subReadersList;

        protected BaseCompositeReader(R[] subReaders)
        {
            this.subReaders = subReaders;
            this.subReadersList = subReaders; // .NET port: array inherits from IList<T> so we can just assign reference here
            starts = new int[subReaders.Length + 1];    // build starts array
            int maxDoc = 0, numDocs = 0;
            for (int i = 0; i < subReaders.Length; i++)
            {
                starts[i] = maxDoc;
                IndexReader r = subReaders[i];
                maxDoc += r.MaxDoc;      // compute maxDocs
                if (maxDoc < 0 /* overflow */)
                {
                    throw new ArgumentException("Too many documents, composite IndexReaders cannot exceed " + int.MaxValue);
                }
                numDocs += r.NumDocs;    // compute numDocs
                r.RegisterParentReader(this);
            }
            starts[subReaders.Length] = maxDoc;
            this.maxDoc = maxDoc;
            this.numDocs = numDocs;
        }

        public override Fields GetTermVectors(int docID)
        {
            EnsureOpen();
            int i = ReaderIndex(docID);
            return subReaders[i].GetTermVectors(docID - starts[i]);
        }

        public override int NumDocs
        {
            get
            {
                // Don't call ensureOpen() here (it could affect performance)
                return numDocs;
            }
        }

        public override int MaxDoc
        {
            get
            {
                // Don't call ensureOpen() here (it could affect performance)
                return maxDoc;
            }
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            EnsureOpen();
            int i = ReaderIndex(docID);                          // find subreader num
            subReaders[i].Document(docID - starts[i], visitor);    // dispatch to subreader
        }

        public override int DocFreq(Term term)
        {
            EnsureOpen();
            int total = 0;          // sum freqs in subreaders
            for (int i = 0; i < subReaders.Length; i++)
            {
                total += subReaders[i].DocFreq(term);
            }
            return total;
        }

        public override long TotalTermFreq(Term term)
        {
            EnsureOpen();
            long total = 0;        // sum freqs in subreaders
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

        public override long GetSumDocFreq(string field)
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

        public override int GetDocCount(string field)
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

        public override long GetSumTotalTermFreq(string field)
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

        protected int ReaderIndex(int docID)
        {
            if (docID < 0 || docID >= maxDoc)
            {
                throw new ArgumentException("docID must be >= 0 and < maxDoc=" + maxDoc + " (got docID=" + docID + ")");
            }
            return ReaderUtil.SubIndex(docID, this.starts);
        }

        protected int ReaderBase(int readerIndex)
        {
            if (readerIndex < 0 || readerIndex >= subReaders.Length)
            {
                throw new ArgumentException("readerIndex must be >= 0 and < getSequentialSubReaders().size()");
            }
            return this.starts[readerIndex];
        }

        protected internal override IList<IndexReader> GetSequentialSubReaders()
        {
            // TODO: .NET Port: does the new instance here cause problems?
            return subReadersList.Cast<IndexReader>().ToList();
        }

        protected internal override abstract void DoClose();
    }
}
