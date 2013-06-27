using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal class TermInfo
    {
        /** The number of documents which contain the term. */
        public int docFreq = 0;

        public long freqPointer = 0;
        public long proxPointer = 0;
        public int skipOffset;

        public TermInfo() { }

        public TermInfo(int df, long fp, long pp)
        {
            docFreq = df;
            freqPointer = fp;
            proxPointer = pp;
        }

        public TermInfo(TermInfo ti)
        {
            docFreq = ti.docFreq;
            freqPointer = ti.freqPointer;
            proxPointer = ti.proxPointer;
            skipOffset = ti.skipOffset;
        }

        public void Set(int docFreq, long freqPointer, long proxPointer, int skipOffset)
        {
            this.docFreq = docFreq;
            this.freqPointer = freqPointer;
            this.proxPointer = proxPointer;
            this.skipOffset = skipOffset;
        }

        public void Set(TermInfo ti)
        {
            docFreq = ti.docFreq;
            freqPointer = ti.freqPointer;
            proxPointer = ti.proxPointer;
            skipOffset = ti.skipOffset;
        }
    }
}
