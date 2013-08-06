using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class CompositeReader : IndexReader
    {
        private volatile CompositeReaderContext readerContext = null; // lazy init

        /** Sole constructor. (For invocation by subclass 
         *  constructors, typically implicit.) */
        protected CompositeReader()
            : base()
        {
        }

        public override String ToString()
        {
            StringBuilder buffer = new StringBuilder();
            // walk up through class hierarchy to get a non-empty simple name (anonymous classes have no name):
            for (Type clazz = GetType(); clazz != null; clazz = clazz.BaseType)
            {
                buffer.Append(clazz.Name);
                break;
            }
            buffer.Append('(');
            var subReaders = GetSequentialSubReaders();
            //assert subReaders != null;
            if (subReaders.Any())
            {
                buffer.Append(subReaders[0]);
                for (int i = 1, c = subReaders.Count; i < c; ++i)
                {
                    buffer.Append(" ").Append(subReaders[i]);
                }
            }
            buffer.Append(')');
            return buffer.ToString();
        }

        /** Expert: returns the sequential sub readers that this
         *  reader is logically composed of. This method may not
         *  return {@code null}.
         *  
         *  <p><b>NOTE:</b> In contrast to previous Lucene versions this method
         *  is no longer public, code that wants to get all {@link AtomicReader}s
         *  this composite is composed of should use {@link IndexReader#leaves()}.
         * @see IndexReader#leaves()
         */
        protected internal abstract IList<IndexReader> GetSequentialSubReaders();

        public override IndexReaderContext Context
        {
            get
            {
                EnsureOpen();
                // lazy init without thread safety for perf reasons: Building the readerContext twice does not hurt!
                if (readerContext == null)
                {
                    //assert getSequentialSubReaders() != null;
                    readerContext = CompositeReaderContext.Create(this);
                }
                return readerContext;
            }
        }

        public abstract override Fields GetTermVectors(int docID);

        public abstract override int NumDocs { get; }

        public abstract override int MaxDoc { get; }

        public abstract override void Document(int docID, StoredFieldVisitor visitor);

        protected internal abstract override void DoClose();

        public abstract override int DocFreq(Term term);

        public abstract override long TotalTermFreq(Term term);

        public abstract override long GetSumDocFreq(string field);

        public abstract override int GetDocCount(string field);

        public abstract override long GetSumTotalTermFreq(string field);
    }

}
