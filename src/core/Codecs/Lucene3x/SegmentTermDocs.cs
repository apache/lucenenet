using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal class SegmentTermDocs : IDisposable
    {
        // .NET Port: not Closeable in java version, but does have Close method so we're implementing IDisposable

        //protected SegmentReader parent;
        private readonly FieldInfos fieldInfos;
        private readonly TermInfosReader tis;
        protected IBits liveDocs;
        protected IndexInput freqStream;
        protected int count;
        protected internal int df;
        internal int doc = 0;
        internal int freq;

        private int skipInterval;
        private int maxSkipLevels;
        private Lucene3xSkipListReader skipListReader;

        private long freqBasePointer;
        private long proxBasePointer;

        private long skipPointer;
        private bool haveSkipped;

        protected bool currentFieldStoresPayloads;
        protected IndexOptions? indexOptions;

        public SegmentTermDocs(IndexInput freqStream, TermInfosReader tis, FieldInfos fieldInfos)
        {
            this.freqStream = (IndexInput)freqStream.Clone();
            this.tis = tis;
            this.fieldInfos = fieldInfos;
            skipInterval = tis.SkipInterval;
            maxSkipLevels = tis.MaxSkipLevels;
        }

        public virtual void Seek(Term term)
        {
            TermInfo ti = tis.Get(term);
            Seek(ti, term);
        }

        public virtual IBits LiveDocs
        {
            get { return liveDocs; }
            set
            {
                this.liveDocs = value;
            }
        }

        public virtual void Seek(SegmentTermEnum segmentTermEnum)
        {
            TermInfo ti;
            Term term;

            // use comparison of fieldinfos to verify that termEnum belongs to the same segment as this SegmentTermDocs
            if (segmentTermEnum.fieldInfos == fieldInfos)
            {        // optimized case
                term = segmentTermEnum.Term;
                ti = segmentTermEnum.TermInfo();
            }
            else
            {                                         // punt case
                term = segmentTermEnum.Term;
                ti = tis.Get(term);
            }

            Seek(ti, term);
        }

        internal virtual void Seek(TermInfo ti, Term term)
        {
            count = 0;
            FieldInfo fi = fieldInfos.FieldInfo(term.Field);
            this.indexOptions = (fi != null) ? fi.IndexOptionsValue : IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            currentFieldStoresPayloads = (fi != null) ? fi.HasPayloads : false;
            if (ti == null)
            {
                df = 0;
            }
            else
            {
                df = ti.docFreq;
                doc = 0;
                freqBasePointer = ti.freqPointer;
                proxBasePointer = ti.proxPointer;
                skipPointer = freqBasePointer + ti.skipOffset;
                freqStream.Seek(freqBasePointer);
                haveSkipped = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                freqStream.Dispose();
                if (skipListReader != null)
                    skipListReader.Dispose();
            }

            freqStream = null;
            skipListReader = null;
        }

        public int Doc
        {
            get { return doc; }
        }

        public int Freq
        {
            get { return freq; }
        }

        protected virtual void SkippingDoc()
        {
        }

        public virtual bool Next()
        {
            while (true)
            {
                if (count == df)
                    return false;
                int docCode = freqStream.ReadVInt();

                if (indexOptions == IndexOptions.DOCS_ONLY)
                {
                    doc += docCode;
                }
                else
                {
                    doc += Number.URShift(docCode, 1);       // shift off low bit
                    if ((docCode & 1) != 0)       // if low bit is set
                        freq = 1;         // freq is one
                    else
                    {
                        freq = freqStream.ReadVInt();     // else read freq
                        //assert freq != 1;
                    }
                }

                count++;

                if (liveDocs == null || liveDocs[doc])
                {
                    break;
                }
                SkippingDoc();
            }
            return true;
        }

        public virtual int Read(int[] docs, int[] freqs)
        {
            int length = docs.Length;
            if (indexOptions == IndexOptions.DOCS_ONLY)
            {
                return ReadNoTf(docs, freqs, length);
            }
            else
            {
                int i = 0;
                while (i < length && count < df)
                {
                    // manually inlined call to next() for speed
                    int docCode = freqStream.ReadVInt();
                    doc += Number.URShift(docCode, 1);       // shift off low bit
                    if ((docCode & 1) != 0)       // if low bit is set
                        freq = 1;         // freq is one
                    else
                        freq = freqStream.ReadVInt();     // else read freq
                    count++;

                    if (liveDocs == null || liveDocs[doc])
                    {
                        docs[i] = doc;
                        freqs[i] = freq;
                        ++i;
                    }
                }
                return i;
            }
        }

        private int ReadNoTf(int[] docs, int[] freqs, int length)
        {
            int i = 0;
            while (i < length && count < df)
            {
                // manually inlined call to next() for speed
                doc += freqStream.ReadVInt();
                count++;

                if (liveDocs == null || liveDocs[doc])
                {
                    docs[i] = doc;
                    // Hardware freq to 1 when term freqs were not
                    // stored in the index
                    freqs[i] = 1;
                    ++i;
                }
            }
            return i;
        }

        protected virtual void SkipProx(long proxPointer, int payloadLength)
        {
        }

        public virtual bool SkipTo(int target)
        {
            // don't skip if the target is close (within skipInterval docs away)
            if ((target - skipInterval) >= doc && df >= skipInterval)
            {                      // optimized case
                if (skipListReader == null)
                    skipListReader = new Lucene3xSkipListReader((IndexInput)freqStream.Clone(), maxSkipLevels, skipInterval); // lazily clone

                if (!haveSkipped)
                {                          // lazily initialize skip stream
                    skipListReader.Init(skipPointer, freqBasePointer, proxBasePointer, df, currentFieldStoresPayloads);
                    haveSkipped = true;
                }

                int newCount = skipListReader.SkipTo(target);
                if (newCount > count)
                {
                    freqStream.Seek(skipListReader.FreqPointer);
                    SkipProx(skipListReader.ProxPointer, skipListReader.PayloadLength);

                    doc = skipListReader.Doc;
                    count = newCount;
                }
            }

            // done skipping, now just scan
            do
            {
                if (!Next())
                    return false;
            } while (target > doc);
            return true;
        }
    }
}
