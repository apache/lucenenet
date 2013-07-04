using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public class DocTermOrds
    {
        // Term ords are shifted by this, internally, to reserve
        // values 0 (end term) and 1 (index is a pointer into byte array)
        private const int TNUM_OFFSET = 2;

        /** Every 128th term is indexed, by default. */
        public const int DEFAULT_INDEX_INTERVAL_BITS = 7; // decrease to a low number like 2 for testing

        private int indexIntervalBits;
        private int indexIntervalMask;
        private int indexInterval;

        /** Don't uninvert terms that exceed this count. */
        protected readonly int maxTermDocFreq;

        /** Field we are uninverting. */
        protected readonly String field;

        /** Number of terms in the field. */
        protected int numTermsInField;

        /** Total number of references to term numbers. */
        protected long termInstances;
        private long memsz;

        /** Total time to uninvert the field. */
        protected int total_time;

        /** Time for phase1 of the uninvert process. */
        protected int phase1_time;

        /** Holds the per-document ords or a pointer to the ords. */
        protected int[] index;

        /** Holds term ords for documents. */
        protected byte[][] tnums = new byte[256][];

        /** Total bytes (sum of term lengths) for all indexed terms.*/
        protected long sizeOfIndexedStrings;

        /** Holds the indexed (by default every 128th) terms. */
        protected BytesRef[] indexedTermsArray;

        /** If non-null, only terms matching this prefix were
         *  indexed. */
        protected BytesRef prefix;

        /** Ordinal of the first term in the field, or 0 if the
         *  {@link PostingsFormat} does not implement {@link
         *  TermsEnum#ord}. */
        protected int ordBase;

        /** Used while uninverting. */
        protected DocsEnum docsEnum;

        public long RamUsedInBytes
        {
            get
            {
                // can cache the mem size since it shouldn't change
                if (memsz != 0) return memsz;
                long sz = 8 * 8 + 32; // local fields
                if (index != null) sz += index.Length * 4;
                if (tnums != null)
                {
                    foreach (byte[] arr in tnums)
                        if (arr != null) sz += arr.Length;
                }
                memsz = sz;
                return sz;
            }
        }

        public DocTermOrds(AtomicReader reader, IBits liveDocs, String field)
            : this(reader, liveDocs, field, null, int.MaxValue)
        {
        }

        public DocTermOrds(AtomicReader reader, IBits liveDocs, String field, BytesRef termPrefix)
            : this(reader, liveDocs, field, termPrefix, int.MaxValue)
        {
        }

        public DocTermOrds(AtomicReader reader, IBits liveDocs, String field, BytesRef termPrefix, int maxTermDocFreq)
            : this(reader, liveDocs, field, termPrefix, maxTermDocFreq, DEFAULT_INDEX_INTERVAL_BITS)
        {
        }

        public DocTermOrds(AtomicReader reader, IBits liveDocs, String field, BytesRef termPrefix, int maxTermDocFreq, int indexIntervalBits)
            : this(field, maxTermDocFreq, indexIntervalBits)
        {
            Uninvert(reader, liveDocs, termPrefix);
        }

        protected DocTermOrds(String field, int maxTermDocFreq, int indexIntervalBits)
        {
            //System.out.println("DTO init field=" + field + " maxTDFreq=" + maxTermDocFreq);
            this.field = field;
            this.maxTermDocFreq = maxTermDocFreq;
            this.indexIntervalBits = indexIntervalBits;
            indexIntervalMask = (int)Number.URShift(0xffffffff, (32 - indexIntervalBits));
            indexInterval = 1 << indexIntervalBits;
        }

        public virtual TermsEnum GetOrdTermsEnum(AtomicReader reader)
        {
            if (indexedTermsArray == null)
            {
                //System.out.println("GET normal enum");
                Fields fields = reader.Fields;
                if (fields == null)
                {
                    return null;
                }
                Terms terms = fields.Terms(field);
                if (terms == null)
                {
                    return null;
                }
                else
                {
                    return terms.Iterator(null);
                }
            }
            else
            {
                //System.out.println("GET wrapped enum ordBase=" + ordBase);
                return new OrdWrappedTermsEnum(this, reader);
            }
        }

        public virtual int NumTerms
        {
            get
            {
                return numTermsInField;
            }
        }

        public virtual bool IsEmpty
        {
            get
            {
                return index == null;
            }
        }

        protected virtual void VisitTerm(TermsEnum te, int termNum)
        {
        }

        protected virtual void SetActualDocFreq(int termNum, int df)
        {
        }

        protected virtual void Uninvert(AtomicReader reader, IBits liveDocs, BytesRef termPrefix)
        {
            FieldInfo info = reader.FieldInfos.FieldInfo(field);
            if (info != null && info.HasDocValues)
            {
                throw new InvalidOperationException("Type mismatch: " + field + " was indexed as " + info.DocValuesTypeValue.GetValueOrDefault());
            }
            //System.out.println("DTO uninvert field=" + field + " prefix=" + termPrefix);
            long startTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            prefix = termPrefix == null ? null : BytesRef.DeepCopyOf(termPrefix);

            int maxDoc = reader.MaxDoc;
            int[] index = new int[maxDoc];       // immediate term numbers, or the index into the byte[] representing the last number
            int[] lastTerm = new int[maxDoc];    // last term we saw for this document
            byte[][] bytes = new byte[maxDoc][]; // list of term numbers for the doc (delta encoded vInts)

            Fields fields = reader.Fields;
            if (fields == null)
            {
                // No terms
                return;
            }
            Terms terms = fields.Terms(field);
            if (terms == null)
            {
                // No terms
                return;
            }

            TermsEnum te = terms.Iterator(null);
            BytesRef seekStart = termPrefix != null ? termPrefix : new BytesRef();
            //System.out.println("seekStart=" + seekStart.utf8ToString());
            if (te.SeekCeil(seekStart) == TermsEnum.SeekStatus.END)
            {
                // No terms match
                return;
            }

            // If we need our "term index wrapper", these will be
            // init'd below:
            List<BytesRef> indexedTerms = null;
            PagedBytes indexedTermsBytes = null;

            bool testedOrd = false;

            // we need a minimum of 9 bytes, but round up to 12 since the space would
            // be wasted with most allocators anyway.
            byte[] tempArr = new byte[12];

            //
            // enumerate all terms, and build an intermediate form of the un-inverted field.
            //
            // During this intermediate form, every document has a (potential) byte[]
            // and the int[maxDoc()] array either contains the termNumber list directly
            // or the *end* offset of the termNumber list in it's byte array (for faster
            // appending and faster creation of the form).
            //
            // idea... if things are too large while building, we could do a range of docs
            // at a time (but it would be a fair amount slower to build)
            // could also do ranges in parallel to take advantage of multiple CPUs

            // OPTIONAL: remap the largest df terms to the lowest 128 (single byte)
            // values.  This requires going over the field first to find the most
            // frequent terms ahead of time.

            int termNum = 0;
            docsEnum = null;

            // Loop begins with te positioned to first term (we call
            // seek above):
            for (; ; )
            {
                BytesRef t = te.Term;
                if (t == null || (termPrefix != null && !StringHelper.StartsWith(t, termPrefix)))
                {
                    break;
                }
                //System.out.println("visit term=" + t.utf8ToString() + " " + t + " termNum=" + termNum);

                if (!testedOrd)
                {
                    try
                    {
                        ordBase = (int)te.Ord;
                        //System.out.println("got ordBase=" + ordBase);
                    }
                    catch (NotSupportedException)
                    {
                        // Reader cannot provide ord support, so we wrap
                        // our own support by creating our own terms index:
                        indexedTerms = new List<BytesRef>();
                        indexedTermsBytes = new PagedBytes(15);
                        //System.out.println("NO ORDS");
                    }
                    testedOrd = true;
                }

                VisitTerm(te, termNum);

                if (indexedTerms != null && (termNum & indexIntervalMask) == 0)
                {
                    // Index this term
                    sizeOfIndexedStrings += t.length;
                    BytesRef indexedTerm = new BytesRef();
                    indexedTermsBytes.Copy(t, indexedTerm);
                    // TODO: really should 1) strip off useless suffix,
                    // and 2) use FST not array/PagedBytes
                    indexedTerms.Add(indexedTerm);
                }

                int df = te.DocFreq;
                if (df <= maxTermDocFreq)
                {

                    docsEnum = te.Docs(liveDocs, docsEnum, DocsEnum.FLAG_NONE);

                    // dF, but takes deletions into account
                    int actualDF = 0;

                    for (; ; )
                    {
                        int doc = docsEnum.NextDoc();
                        if (doc == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            break;
                        }
                        //System.out.println("  chunk=" + chunk + " docs");

                        actualDF++;
                        termInstances++;

                        //System.out.println("    docID=" + doc);
                        // add TNUM_OFFSET to the term number to make room for special reserved values:
                        // 0 (end term) and 1 (index into byte array follows)
                        int delta = termNum - lastTerm[doc] + TNUM_OFFSET;
                        lastTerm[doc] = termNum;
                        int val = index[doc];

                        if ((val & 0xff) == 1)
                        {
                            // index into byte array (actually the end of
                            // the doc-specific byte[] when building)
                            int pos = Number.URShift(val, 8);
                            int ilen = VIntSize(delta);
                            byte[] arr = bytes[doc];
                            int newend = pos + ilen;
                            if (newend > arr.Length)
                            {
                                // We avoid a doubling strategy to lower memory usage.
                                // this faceting method isn't for docs with many terms.
                                // In hotspot, objects have 2 words of overhead, then fields, rounded up to a 64-bit boundary.
                                // TODO: figure out what array lengths we can round up to w/o actually using more memory
                                // (how much space does a byte[] take up?  Is data preceded by a 32 bit length only?
                                // It should be safe to round up to the nearest 32 bits in any case.
                                int newLen = (int)((newend + 3) & 0xfffffffc);  // 4 byte alignment
                                byte[] newarr = new byte[newLen];
                                Array.Copy(arr, 0, newarr, 0, pos);
                                arr = newarr;
                                bytes[doc] = newarr;
                            }
                            pos = WriteInt(delta, arr, pos);
                            index[doc] = (pos << 8) | 1;  // update pointer to end index in byte[]
                        }
                        else
                        {
                            // OK, this int has data in it... find the end (a zero starting byte - not
                            // part of another number, hence not following a byte with the high bit set).
                            int ipos;
                            if (val == 0)
                            {
                                ipos = 0;
                            }
                            else if ((val & 0x0000ff80) == 0)
                            {
                                ipos = 1;
                            }
                            else if ((val & 0x00ff8000) == 0)
                            {
                                ipos = 2;
                            }
                            else if ((val & 0xff800000) == 0)
                            {
                                ipos = 3;
                            }
                            else
                            {
                                ipos = 4;
                            }

                            //System.out.println("      ipos=" + ipos);

                            int endPos = WriteInt(delta, tempArr, ipos);
                            //System.out.println("      endpos=" + endPos);
                            if (endPos <= 4)
                            {
                                //System.out.println("      fits!");
                                // value will fit in the integer... move bytes back
                                for (int j = ipos; j < endPos; j++)
                                {
                                    val |= (tempArr[j] & 0xff) << (j << 3);
                                }
                                index[doc] = val;
                            }
                            else
                            {
                                // value won't fit... move integer into byte[]
                                for (int j = 0; j < ipos; j++)
                                {
                                    tempArr[j] = (byte)val;
                                    val = Number.URShift(val, 8);
                                }
                                // point at the end index in the byte[]
                                index[doc] = (endPos << 8) | 1;
                                bytes[doc] = tempArr;
                                tempArr = new byte[12];
                            }
                        }
                    }
                    SetActualDocFreq(termNum, actualDF);
                }

                termNum++;
                if (te.Next() == null)
                {
                    break;
                }
            }

            numTermsInField = termNum;

            long midPoint = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            if (termInstances == 0)
            {
                // we didn't invert anything
                // lower memory consumption.
                tnums = null;
            }
            else
            {

                this.index = index;

                //
                // transform intermediate form into the form, building a single byte[]
                // at a time, and releasing the intermediate byte[]s as we go to avoid
                // increasing the memory footprint.
                //

                for (int pass = 0; pass < 256; pass++)
                {
                    byte[] target = tnums[pass];
                    int pos = 0;  // end in target;
                    if (target != null)
                    {
                        pos = target.Length;
                    }
                    else
                    {
                        target = new byte[4096];
                    }

                    // loop over documents, 0x00ppxxxx, 0x01ppxxxx, 0x02ppxxxx
                    // where pp is the pass (which array we are building), and xx is all values.
                    // each pass shares the same byte[] for termNumber lists.
                    for (int docbase = pass << 16; docbase < maxDoc; docbase += (1 << 24))
                    {
                        int lim = Math.Min(docbase + (1 << 16), maxDoc);
                        for (int doc = docbase; doc < lim; doc++)
                        {
                            //System.out.println("  pass=" + pass + " process docID=" + doc);
                            int val = index[doc];
                            if ((val & 0xff) == 1)
                            {
                                int len = Number.URShift(val, 8);
                                //System.out.println("    ptr pos=" + pos);
                                index[doc] = (pos << 8) | 1; // change index to point to start of array
                                if ((pos & 0xff000000) != 0)
                                {
                                    // we only have 24 bits for the array index
                                    throw new InvalidOperationException("Too many values for UnInvertedField faceting on field " + field);
                                }
                                byte[] arr = bytes[doc];
                                /*
                                for(byte b : arr) {
                                  //System.out.println("      b=" + Integer.toHexString((int) b));
                                }
                                */
                                bytes[doc] = null;        // IMPORTANT: allow GC to avoid OOM
                                if (target.Length <= pos + len)
                                {
                                    int newlen = target.Length;
                                    /*** we don't have to worry about the array getting too large
                                     * since the "pos" param will overflow first (only 24 bits available)
                                    if ((newlen<<1) <= 0) {
                                      // overflow...
                                      newlen = Integer.MAX_VALUE;
                                      if (newlen <= pos + len) {
                                        throw new SolrException(400,"Too many terms to uninvert field!");
                                      }
                                    } else {
                                      while (newlen <= pos + len) newlen<<=1;  // doubling strategy
                                    }
                                    ****/
                                    while (newlen <= pos + len) newlen <<= 1;  // doubling strategy                 
                                    byte[] newtarget = new byte[newlen];
                                    Array.Copy(target, 0, newtarget, 0, pos);
                                    target = newtarget;
                                }
                                Array.Copy(arr, 0, target, pos, len);
                                pos += len + 1;  // skip single byte at end and leave it 0 for terminator
                            }
                        }
                    }

                    // shrink array
                    if (pos < target.Length)
                    {
                        byte[] newtarget = new byte[pos];
                        Array.Copy(target, 0, newtarget, 0, pos);
                        target = newtarget;
                    }

                    tnums[pass] = target;

                    if ((pass << 16) > maxDoc)
                        break;
                }

            }
            if (indexedTerms != null)
            {
                indexedTermsArray = indexedTerms.ToArray();
            }

            long endTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            total_time = (int)(endTime - startTime);
            phase1_time = (int)(midPoint - startTime);
        }

        private static int VIntSize(int x)
        {
            if ((x & (0xffffffff << (7 * 1))) == 0)
            {
                return 1;
            }
            if ((x & (0xffffffff << (7 * 2))) == 0)
            {
                return 2;
            }
            if ((x & (0xffffffff << (7 * 3))) == 0)
            {
                return 3;
            }
            if ((x & (0xffffffff << (7 * 4))) == 0)
            {
                return 4;
            }
            return 5;
        }

        private static int WriteInt(int x, byte[] arr, int pos)
        {
            int a;
            a = Number.URShift(x, (7 * 4));
            if (a != 0)
            {
                arr[pos++] = (byte)(a | 0x80);
            }
            a = Number.URShift(x, (7 * 3));
            if (a != 0)
            {
                arr[pos++] = (byte)(a | 0x80);
            }
            a = Number.URShift(x, (7 * 2));
            if (a != 0)
            {
                arr[pos++] = (byte)(a | 0x80);
            }
            a = Number.URShift(x, (7 * 1));
            if (a != 0)
            {
                arr[pos++] = (byte)(a | 0x80);
            }
            arr[pos++] = (byte)(x & 0x7f);
            return pos;
        }

        private sealed class OrdWrappedTermsEnum : TermsEnum
        {
            private readonly TermsEnum termsEnum;
            private BytesRef term;
            private long ord; //.NET Port: initialization moved to ctor

            private readonly DocTermOrds parent;

            public OrdWrappedTermsEnum(DocTermOrds parent, AtomicReader reader)
            {
                ord = -parent.indexInterval - 1;          // force "real" seek
                this.parent = parent;

                //assert indexedTermsArray != null;
                termsEnum = reader.Fields.Terms(parent.field).Iterator(null);
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return termsEnum.Comparator; }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                return termsEnum.Docs(liveDocs, reuse, flags);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                return termsEnum.DocsAndPositions(liveDocs, reuse, flags);
            }

            public override BytesRef Term
            {
                get { return term; }
            }

            public override BytesRef Next()
            {
                if (++ord < 0)
                {
                    ord = 0;
                }
                if (termsEnum.Next() == null)
                {
                    term = null;
                    return null;
                }
                return SetTerm();  // this is extra work if we know we are in bounds...
            }

            public override int DocFreq
            {
                get { return termsEnum.DocFreq; }
            }

            public override long TotalTermFreq
            {
                get { return termsEnum.TotalTermFreq; }
            }

            public override long Ord
            {
                get { return parent.ordBase + ord; }
            }

            public override SeekStatus SeekCeil(BytesRef target, bool useCache)
            {
                // already here
                if (term != null && term.Equals(target))
                {
                    return SeekStatus.FOUND;
                }

                int startIdx = Array.BinarySearch(parent.indexedTermsArray, target);

                if (startIdx >= 0)
                {
                    // we hit the term exactly... lucky us!
                    TermsEnum.SeekStatus seekStatus = termsEnum.SeekCeil(target);
                    //assert seekStatus == TermsEnum.SeekStatus.FOUND;
                    ord = startIdx << parent.indexIntervalBits;
                    SetTerm();
                    //assert term != null;
                    return SeekStatus.FOUND;
                }

                // we didn't hit the term exactly
                startIdx = -startIdx - 1;

                if (startIdx == 0)
                {
                    // our target occurs *before* the first term
                    TermsEnum.SeekStatus seekStatus = termsEnum.SeekCeil(target);
                    //assert seekStatus == TermsEnum.SeekStatus.NOT_FOUND;
                    ord = 0;
                    SetTerm();
                    //assert term != null;
                    return SeekStatus.NOT_FOUND;
                }

                // back up to the start of the block
                startIdx--;

                if ((ord >> parent.indexIntervalBits) == startIdx && term != null && term.CompareTo(target) <= 0)
                {
                    // we are already in the right block and the current term is before the term we want,
                    // so we don't need to seek.
                }
                else
                {
                    // seek to the right block
                    TermsEnum.SeekStatus seekStatus = termsEnum.SeekCeil(parent.indexedTermsArray[startIdx]);
                    //assert seekStatus == TermsEnum.SeekStatus.FOUND;
                    ord = startIdx << parent.indexIntervalBits;
                    SetTerm();
                    //assert term != null;  // should be non-null since it's in the index
                }

                while (term != null && term.CompareTo(target) < 0)
                {
                    Next();
                }

                if (term == null)
                {
                    return SeekStatus.END;
                }
                else if (term.CompareTo(target) == 0)
                {
                    return SeekStatus.FOUND;
                }
                else
                {
                    return SeekStatus.NOT_FOUND;
                }
            }

            public override void SeekExact(long targetOrd)
            {
                int delta = (int)(targetOrd - parent.ordBase - ord);
                //System.out.println("  seek(ord) targetOrd=" + targetOrd + " delta=" + delta + " ord=" + ord + " ii=" + indexInterval);
                if (delta < 0 || delta > parent.indexInterval)
                {
                    int idx = (int)Number.URShift(targetOrd, parent.indexIntervalBits);
                    BytesRef baseref = parent.indexedTermsArray[idx];
                    //System.out.println("  do seek term=" + base.utf8ToString());
                    ord = idx << parent.indexIntervalBits;
                    delta = (int)(targetOrd - ord);
                    TermsEnum.SeekStatus seekStatus = termsEnum.SeekCeil(baseref, true);
                    //assert seekStatus == TermsEnum.SeekStatus.FOUND;
                }
                else
                {
                    //System.out.println("seek w/in block");
                }

                while (--delta >= 0)
                {
                    BytesRef br = termsEnum.Next();
                    if (br == null)
                    {
                        //assert false;
                        return;
                    }
                    ord++;
                }

                SetTerm();
                //assert term != null;
            }

            private BytesRef SetTerm()
            {
                term = termsEnum.Term;
                //System.out.println("  setTerm() term=" + term.utf8ToString() + " vs prefix=" + (prefix == null ? "null" : prefix.utf8ToString()));
                if (parent.prefix != null && !StringHelper.StartsWith(term, parent.prefix))
                {
                    term = null;
                }
                return term;
            }
        }

        public virtual BytesRef LookupTerm(TermsEnum termsEnum, int ord)
        {
            termsEnum.SeekExact(ord);
            return termsEnum.Term;
        }

        public virtual SortedSetDocValues Iterator(AtomicReader reader)
        {
            if (IsEmpty)
            {
                return SortedSetDocValues.EMPTY;
            }
            else
            {
                return new DocTermOrdsIterator(this, reader);
            }
        }

        private class DocTermOrdsIterator : SortedSetDocValues
        {
            internal readonly AtomicReader reader;
            internal readonly TermsEnum te;  // used internally for lookupOrd() and lookupTerm()
            // currently we read 5 at a time (using the logic of the old iterator)
            internal readonly int[] buffer = new int[5];
            internal int bufferUpto;
            internal int bufferLength;

            private int tnum;
            private int upto;
            private byte[] arr;

            private readonly DocTermOrds parent;

            public DocTermOrdsIterator(DocTermOrds parent, AtomicReader reader)
            {
                this.parent = parent;
                this.reader = reader;
                this.te = TermsEnum;
            }

            public override long NextOrd()
            {
                while (bufferUpto == bufferLength)
                {
                    if (bufferLength < buffer.Length)
                    {
                        return NO_MORE_ORDS;
                    }
                    else
                    {
                        bufferLength = Read(buffer);
                        bufferUpto = 0;
                    }
                }
                return buffer[bufferUpto++];
            }

            internal int Read(int[] buffer)
            {
                int bufferUpto = 0;
                if (arr == null)
                {
                    // code is inlined into upto
                    //System.out.println("inlined");
                    int code = upto;
                    int delta = 0;
                    for (; ; )
                    {
                        delta = (delta << 7) | (code & 0x7f);
                        if ((code & 0x80) == 0)
                        {
                            if (delta == 0) break;
                            tnum += delta - TNUM_OFFSET;
                            buffer[bufferUpto++] = parent.ordBase + tnum;
                            //System.out.println("  tnum=" + tnum);
                            delta = 0;
                        }
                        code = Number.URShift(code, 8);
                    }
                }
                else
                {
                    // code is a pointer
                    for (; ; )
                    {
                        int delta = 0;
                        for (; ; )
                        {
                            byte b = arr[upto++];
                            delta = (delta << 7) | (b & 0x7f);
                            //System.out.println("    cycle: upto=" + upto + " delta=" + delta + " b=" + b);
                            if ((b & 0x80) == 0) break;
                        }
                        //System.out.println("  delta=" + delta);
                        if (delta == 0) break;
                        tnum += delta - TNUM_OFFSET;
                        //System.out.println("  tnum=" + tnum);
                        buffer[bufferUpto++] = parent.ordBase + tnum;
                        if (bufferUpto == buffer.Length)
                        {
                            break;
                        }
                    }
                }

                return bufferUpto;
            }

            public override void SetDocument(int docID)
            {
                tnum = 0;
                int code = parent.index[docID];
                if ((code & 0xff) == 1)
                {
                    // a pointer
                    upto = Number.URShift(code, 8);
                    //System.out.println("    pointer!  upto=" + upto);
                    int whichArray = Number.URShift(docID, 16) & 0xff;
                    arr = parent.tnums[whichArray];
                }
                else
                {
                    //System.out.println("    inline!");
                    arr = null;
                    upto = code;
                }
                bufferUpto = 0;
                bufferLength = Read(buffer);
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                BytesRef br = null;
                try
                {
                    br = parent.LookupTerm(te, (int)ord);
                }
                catch (System.IO.IOException)
                {
                    throw;
                }
                result.bytes = br.bytes;
                result.offset = br.offset;
                result.length = br.length;
            }

            public override long ValueCount
            {
                get { return parent.NumTerms; }
            }

            public override long LookupTerm(BytesRef key)
            {
                try
                {
                    if (te.SeekCeil(key) == TermsEnum.SeekStatus.FOUND)
                    {
                        return te.Ord;
                    }
                    else
                    {
                        return -te.Ord - 1;
                    }
                }
                catch (System.IO.IOException)
                {
                    throw;
                }
            }

            public override TermsEnum TermsEnum
            {
                get
                {
                    try
                    {
                        return parent.GetOrdTermsEnum(reader);
                    }
                    catch (System.IO.IOException)
                    {
                        throw new SystemException();
                    }
                }
            }
        }
    }
}
