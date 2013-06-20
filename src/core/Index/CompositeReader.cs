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

        public override CompositeReaderContext Context
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

        public abstract Fields GetTermVectors(int docID);

        public abstract int NumDocs { get; }

        public abstract int MaxDoc { get; }

        public abstract void Document(int docID, StoredFieldVisitor visitor);

        protected abstract void DoClose();

        public abstract int DocFreq(Term term);

        public abstract long TotalTermFreq(Term term);

        public abstract long GetSumDocFreq(string field);

        public abstract int GetDocCount(string field);

        public abstract long GetSumTotalTermFreq(string field);
    }

}
