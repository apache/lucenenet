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
                for (int i = 1, c = subReaders.Length; i < c; ++i)
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
        protected abstract List<T> GetSequentialSubReaders<T>() where T : IndexReader;

        public override CompositeReaderContext getContext()
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

}
