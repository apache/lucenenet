using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JCG = J2N.Collections.Generic;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using IBits = Lucene.Net.Util.IBits;
    using PagedBytes = Lucene.Net.Util.PagedBytes;
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat; // javadocs
    using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
    using StringHelper = Lucene.Net.Util.StringHelper;

    /// <summary>
    /// This class enables fast access to multiple term ords for
    /// a specified field across all docIDs.
    /// <para/>
    /// Like <see cref="Search.IFieldCache"/>, it uninverts the index and holds a
    /// packed data structure in RAM to enable fast access.
    /// Unlike <see cref="Search.IFieldCache"/>, it can handle multi-valued fields,
    /// and, it does not hold the term bytes in RAM.  Rather, you
    /// must obtain a <see cref="TermsEnum"/> from the <see cref="GetOrdTermsEnum"/>
    /// method, and then seek-by-ord to get the term's bytes.
    /// <para/>
    /// While normally term ords are type <see cref="long"/>, in this API they are
    /// <see cref="int"/> as the internal representation here cannot address
    /// more than <see cref="BufferedUpdates.MAX_INT32"/> unique terms.  Also, typically this
    /// class is used on fields with relatively few unique terms
    /// vs the number of documents.  In addition, there is an
    /// internal limit (16 MB) on how many bytes each chunk of
    /// documents may consume.  If you trip this limit you'll hit
    /// an <see cref="InvalidOperationException"/>.
    /// <para/>
    /// Deleted documents are skipped during uninversion, and if
    /// you look them up you'll get 0 ords.
    /// <para/>
    /// The returned per-document ords do not retain their
    /// original order in the document.  Instead they are returned
    /// in sorted (by ord, ie term's <see cref="BytesRef"/> comparer) order.  They
    /// are also de-dup'd (ie if doc has same term more than once
    /// in this field, you'll only get that ord back once).
    /// <para/>
    /// This class tests whether the provided reader is able to
    /// retrieve terms by ord (ie, it's single segment, and it
    /// uses an ord-capable terms index).  If not, this class
    /// will create its own term index internally, allowing to
    /// create a wrapped <see cref="TermsEnum"/> that can handle ord.  The
    /// <see cref="GetOrdTermsEnum"/> method then provides this
    /// wrapped enum, if necessary.
    /// <para/>
    /// The RAM consumption of this class can be high!
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <remarks>
    /// Final form of the un-inverted field:
    /// <list type="bullet">
    ///     <item><description>Each document points to a list of term numbers that are contained in that document.</description></item>
    ///     <item><description>
    ///         Term numbers are in sorted order, and are encoded as variable-length deltas from the
    ///         previous term number.  Real term numbers start at 2 since 0 and 1 are reserved.  A
    ///         term number of 0 signals the end of the termNumber list.
    ///     </description></item>
    ///     <item><description>
    ///         There is a single int[maxDoc()] which either contains a pointer into a byte[] for
    ///         the termNumber lists, or directly contains the termNumber list if it fits in the 4
    ///         bytes of an integer.  If the first byte in the integer is 1, the next 3 bytes
    ///         are a pointer into a byte[] where the termNumber list starts.
    ///     </description></item>
    ///     <item><description>
    ///         There are actually 256 byte arrays, to compensate for the fact that the pointers
    ///         into the byte arrays are only 3 bytes long.  The correct byte array for a document
    ///         is a function of it's id.
    ///     </description></item>
    ///     <item><description>
    ///         To save space and speed up faceting, any term that matches enough documents will
    ///         not be un-inverted... it will be skipped while building the un-inverted field structure,
    ///         and will use a set intersection method during faceting.
    ///     </description></item>
    ///     <item><description>
    ///         To further save memory, the terms (the actual string values) are not all stored in
    ///         memory, but a TermIndex is used to convert term numbers to term values only
    ///         for the terms needed after faceting has completed.  Only every 128th term value
    ///         is stored, along with it's corresponding term number, and this is used as an
    ///         index to find the closest term and iterate until the desired number is hit (very
    ///         much like Lucene's own internal term index).
    ///     </description></item>
    /// </list>
    /// </remarks>
    public class DocTermOrds
    {
        /// <summary>
        /// Term ords are shifted by this, internally, to reserve
        /// values 0 (end term) and 1 (index is a pointer into byte array)
        /// </summary>
        private const int TNUM_OFFSET = 2;

        /// <summary>
        /// Every 128th term is indexed, by default. </summary>
        public const int DEFAULT_INDEX_INTERVAL_BITS = 7; // decrease to a low number like 2 for testing

        private readonly int indexIntervalBits; // LUCENENET: marked readonly
        private readonly int indexIntervalMask; // LUCENENET: marked readonly
        private readonly int indexInterval; // LUCENENET: marked readonly

        /// <summary>
        /// Don't uninvert terms that exceed this count. </summary>
        protected readonly int m_maxTermDocFreq; 

        /// <summary>
        /// Field we are uninverting. </summary>
        protected readonly string m_field; 

        /// <summary>
        /// Number of terms in the field. </summary>
        protected int m_numTermsInField; 

        /// <summary>
        /// Total number of references to term numbers. </summary>
        protected long m_termInstances;

        private long memsz;

        /// <summary>
        /// Total time to uninvert the field. </summary>
        protected int m_total_time; 

        /// <summary>
        /// Time for phase1 of the uninvert process. </summary>
        protected int m_phase1_time;

        /// <summary>
        /// Holds the per-document ords or a pointer to the ords. </summary>
        protected int[] m_index;

        /// <summary>
        /// Holds term ords for documents. </summary>
        [CLSCompliant(false)] // LUCENENET NOTE: Since jagged arrays are not CLS compliant anyway, this would need to be byte[,] to be CLS compliant - not sure it is worth the effort
        protected sbyte[][] m_tnums = new sbyte[256][];

        /// <summary>
        /// Total bytes (sum of term lengths) for all indexed terms. </summary>
        protected long m_sizeOfIndexedStrings;

        /// <summary>
        /// Holds the indexed (by default every 128th) terms. </summary>
        protected BytesRef[] m_indexedTermsArray;

        /// <summary>
        /// If non-null, only terms matching this prefix were
        /// indexed.
        /// </summary>
        protected BytesRef m_prefix;

        /// <summary>
        /// Ordinal of the first term in the field, or 0 if the
        /// <see cref="PostingsFormat"/> does not implement 
        /// <see cref="TermsEnum.Ord"/>.
        /// </summary>
        protected int m_ordBase;

        /// <summary>
        /// Used while uninverting. </summary>
        protected DocsEnum m_docsEnum;

        /// <summary>
        /// Returns total bytes used. </summary>
        public virtual long RamUsedInBytes()
        {
            // can cache the mem size since it shouldn't change
            if (memsz != 0)
            {
                return memsz;
            }
            long sz = 8 * 8 + 32; // local fields
            if (m_index != null)
            {
                sz += m_index.Length * 4;
            }
            if (m_tnums != null)
            {
                for (int i = 0; i < m_tnums.Length; i++)
                {
                    var arr = m_tnums[i];
                    if (arr != null)
                        sz += arr.Length;
                }
            }
            memsz = sz;
            return sz;
        }

        /// <summary>
        /// Inverts all terms </summary>
        public DocTermOrds(AtomicReader reader, IBits liveDocs, string field)
            : this(reader, liveDocs, field, null, int.MaxValue)
        {
        }

        /// <summary>
        /// Inverts only terms starting w/ prefix </summary>
        public DocTermOrds(AtomicReader reader, IBits liveDocs, string field, BytesRef termPrefix)
            : this(reader, liveDocs, field, termPrefix, int.MaxValue)
        {
        }

        /// <summary>
        /// Inverts only terms starting w/ prefix, and only terms
        /// whose docFreq (not taking deletions into account) is
        /// &lt;= <paramref name="maxTermDocFreq"/>
        /// </summary>
        public DocTermOrds(AtomicReader reader, IBits liveDocs, string field, BytesRef termPrefix, int maxTermDocFreq)
            : this(reader, liveDocs, field, termPrefix, maxTermDocFreq, DEFAULT_INDEX_INTERVAL_BITS)
        {
        }

        /// <summary>
        /// Inverts only terms starting w/ prefix, and only terms
        /// whose docFreq (not taking deletions into account) is
        /// &lt;=  <paramref name="maxTermDocFreq"/>, with a custom indexing interval
        /// (default is every 128nd term).
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "A workaround exists that involves using a protected constructor")]
        public DocTermOrds(AtomicReader reader, IBits liveDocs, string field, BytesRef termPrefix, int maxTermDocFreq, int indexIntervalBits)
            : this(field, maxTermDocFreq, indexIntervalBits)
        {
            Uninvert(reader, liveDocs, termPrefix);
        }

        /// <summary>
        /// Subclass inits w/ this, but be sure you then call
        /// uninvert, only once
        /// </summary>
        protected DocTermOrds(string field, int maxTermDocFreq, int indexIntervalBits)
        {
            //System.out.println("DTO init field=" + field + " maxTDFreq=" + maxTermDocFreq);
            this.m_field = field;
            this.m_maxTermDocFreq = maxTermDocFreq;
            this.indexIntervalBits = indexIntervalBits;
            indexIntervalMask = (int)(0xffffffff >> (32 - indexIntervalBits)); // LUCENENET: No need to cast to uint here for the triple shift because the literal already is uint
            indexInterval = 1 << indexIntervalBits;
        }

        /// <summary>
        /// Returns a <see cref="TermsEnum"/> that implements <see cref="TermsEnum.Ord"/>.  If the
        /// provided <paramref name="reader"/> supports <see cref="TermsEnum.Ord"/>, we just return its
        /// <see cref="TermsEnum"/>; if it does not, we build a "private" terms
        /// index internally (WARNING: consumes RAM) and use that
        /// index to implement <see cref="TermsEnum.Ord"/>.  This also enables <see cref="TermsEnum.Ord"/> on top
        /// of a composite reader.  The returned <see cref="TermsEnum"/> is
        /// unpositioned.  This returns <c>null</c> if there are no terms.
        ///
        /// <para/><b>NOTE</b>: you must pass the same reader that was
        /// used when creating this class
        /// </summary>
        public virtual TermsEnum GetOrdTermsEnum(AtomicReader reader)
        {
            if (m_indexedTermsArray is null)
            {
                //System.out.println("GET normal enum");
                Fields fields = reader.Fields;
                if (fields is null)
                {
                    return null;
                }
                Terms terms = fields.GetTerms(m_field);
                if (terms is null)
                {
                    return null;
                }
                else
                {
                    return terms.GetEnumerator();
                }
            }
            else
            {
                //System.out.println("GET wrapped enum ordBase=" + ordBase);
                return new OrdWrappedTermsEnum(this, reader);
            }
        }

        /// <summary>
        /// Returns the number of terms in this field
        /// </summary>
        public virtual int NumTerms => m_numTermsInField;

        /// <summary>
        /// Returns <c>true</c> if no terms were indexed.
        /// </summary>
        public virtual bool IsEmpty => m_index is null;

        /// <summary>
        /// Subclass can override this </summary>
        protected virtual void VisitTerm(TermsEnum te, int termNum)
        {
        }

        /// <summary>
        /// Invoked during <see cref="Uninvert(AtomicReader, IBits, BytesRef)"/>
        /// to record the document frequency for each uninverted
        /// term.
        /// </summary>
        protected virtual void SetActualDocFreq(int termNum, int df)
        {
        }

        /// <summary>
        /// Call this only once (if you subclass!) </summary>
        protected virtual void Uninvert(AtomicReader reader, IBits liveDocs, BytesRef termPrefix)
        {
            FieldInfo info = reader.FieldInfos.FieldInfo(m_field);
            if (info != null && info.HasDocValues)
            {
                throw IllegalStateException.Create("Type mismatch: " + m_field + " was indexed as " + info.DocValuesType);
            }
            //System.out.println("DTO uninvert field=" + field + " prefix=" + termPrefix);
            long startTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            m_prefix = termPrefix is null ? null : BytesRef.DeepCopyOf(termPrefix);

            int maxDoc = reader.MaxDoc;
            int[] index = new int[maxDoc]; // immediate term numbers, or the index into the byte[] representing the last number
            int[] lastTerm = new int[maxDoc]; // last term we saw for this document
            var bytes = new sbyte[maxDoc][]; // list of term numbers for the doc (delta encoded vInts)

            Fields fields = reader.Fields;
            if (fields is null)
            {
                // No terms
                return;
            }
            Terms terms = fields.GetTerms(m_field);
            if (terms is null)
            {
                // No terms
                return;
            }

            TermsEnum te = terms.GetEnumerator();
            BytesRef seekStart = termPrefix ?? new BytesRef();
            //System.out.println("seekStart=" + seekStart.utf8ToString());
            if (te.SeekCeil(seekStart) == TermsEnum.SeekStatus.END)
            {
                // No terms match
                return;
            }

            // If we need our "term index wrapper", these will be
            // init'd below:
            IList<BytesRef> indexedTerms = null;
            PagedBytes indexedTermsBytes = null;

            bool testedOrd = false;

            // we need a minimum of 9 bytes, but round up to 12 since the space would
            // be wasted with most allocators anyway.
            var tempArr = new sbyte[12];

            //
            // enumerate all terms, and build an intermediate form of the un-inverted field.
            //
            // During this intermediate form, every document has a (potential) byte[]
            // and the int[maxDoc()] array either contains the termNumber list directly
            // or the *end* offset of the termNumber list in it's byte array (for faster
            // appending and faster creation of the final form).
            //
            // idea... if things are too large while building, we could do a range of docs
            // at a time (but it would be a fair amount slower to build)
            // could also do ranges in parallel to take advantage of multiple CPUs

            // OPTIONAL: remap the largest df terms to the lowest 128 (single byte)
            // values.  this requires going over the field first to find the most
            // frequent terms ahead of time.

            int termNum = 0;
            m_docsEnum = null;

            // Loop begins with te positioned to first term (we call
            // seek above):
            for (; ; )
            {
                BytesRef t = te.Term;
                if (t is null || (termPrefix != null && !StringHelper.StartsWith(t, termPrefix)))
                {
                    break;
                }
                //System.out.println("visit term=" + t.utf8ToString() + " " + t + " termNum=" + termNum);

                if (!testedOrd)
                {
                    try
                    {
                        m_ordBase = (int)te.Ord;
                        //System.out.println("got ordBase=" + ordBase);
                    }
                    catch (Exception uoe) when (uoe.IsUnsupportedOperationException())
                    {
                        // Reader cannot provide ord support, so we wrap
                        // our own support by creating our own terms index:
                        indexedTerms = new JCG.List<BytesRef>();
                        indexedTermsBytes = new PagedBytes(15);
                        //System.out.println("NO ORDS");
                    }
                    testedOrd = true;
                }

                VisitTerm(te, termNum);

                if (indexedTerms != null && (termNum & indexIntervalMask) == 0)
                {
                    // Index this term
                    m_sizeOfIndexedStrings += t.Length;
                    BytesRef indexedTerm = new BytesRef();
                    indexedTermsBytes.Copy(t, indexedTerm);
                    // TODO: really should 1) strip off useless suffix,
                    // and 2) use FST not array/PagedBytes
                    indexedTerms.Add(indexedTerm);
                }

                int df = te.DocFreq;
                if (df <= m_maxTermDocFreq)
                {
                    m_docsEnum = te.Docs(liveDocs, m_docsEnum, DocsFlags.NONE);

                    // dF, but takes deletions into account
                    int actualDF = 0;

                    for (; ; )
                    {
                        int doc = m_docsEnum.NextDoc();
                        if (doc == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            break;
                        }
                        //System.out.println("  chunk=" + chunk + " docs");

                        actualDF++;
                        m_termInstances++;

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
                            int pos = val.TripleShift(8);
                            int ilen = VInt32Size(delta);
                            var arr = bytes[doc];
                            int newend = pos + ilen;
                            if (newend > arr.Length)
                            {
                                // We avoid a doubling strategy to lower memory usage.
                                // this faceting method isn't for docs with many terms.
                                // In hotspot, objects have 2 words of overhead, then fields, rounded up to a 64-bit boundary.
                                // TODO: figure out what array lengths we can round up to w/o actually using more memory
                                // (how much space does a byte[] take up?  Is data preceded by a 32 bit length only?
                                // It should be safe to round up to the nearest 32 bits in any case.
                                int newLen = (newend + 3) & unchecked((int)0xfffffffc); // 4 byte alignment
                                var newarr = new sbyte[newLen];
                                Arrays.Copy(arr, 0, newarr, 0, pos);
                                arr = newarr;
                                bytes[doc] = newarr;
                            }
                            pos = WriteInt32(delta, arr, pos);
                            index[doc] = (pos << 8) | 1; // update pointer to end index in byte[]
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

                            int endPos = WriteInt32(delta, tempArr, ipos);
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
                                    tempArr[j] = (sbyte)val;
                                    val = val.TripleShift(8);
                                }
                                // point at the end index in the byte[]
                                index[doc] = (endPos << 8) | 1;
                                bytes[doc] = tempArr;
                                tempArr = new sbyte[12];
                            }
                        }
                    }
                    SetActualDocFreq(termNum, actualDF);
                }

                termNum++;
                if (!te.MoveNext())
                {
                    break;
                }
            }

            m_numTermsInField = termNum;

            long midPoint = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            if (m_termInstances == 0)
            {
                // we didn't invert anything
                // lower memory consumption.
                m_tnums = null;
            }
            else
            {
                this.m_index = index;

                //
                // transform intermediate form into the final form, building a single byte[]
                // at a time, and releasing the intermediate byte[]s as we go to avoid
                // increasing the memory footprint.
                //

                for (int pass = 0; pass < 256; pass++)
                {
                    var target = m_tnums[pass];
                    var pos = 0; // end in target;
                    if (target != null)
                    {
                        pos = target.Length;
                    }
                    else
                    {
                        target = new sbyte[4096];
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
                                int len = val.TripleShift(8);
                                //System.out.println("    ptr pos=" + pos);
                                index[doc] = (pos << 8) | 1; // change index to point to start of array
                                if ((pos & 0xff000000) != 0)
                                {
                                    // we only have 24 bits for the array index
                                    throw IllegalStateException.Create("Too many values for UnInvertedField faceting on field " + m_field);
                                }
                                var arr = bytes[doc];
                                /*
                                for(byte b : arr) {
                                  //System.out.println("      b=" + Integer.toHexString((int) b));
                                }
                                */
                                bytes[doc] = null; // IMPORTANT: allow GC to avoid OOM
                                if (target.Length <= pos + len)
                                {
                                    int newlen = target.Length;

                                    //* we don't have to worry about the array getting too large
                                    // since the "pos" param will overflow first (only 24 bits available)
                                    // if ((newlen<<1) <= 0) {
                                    //  // overflow...
                                    //  newlen = Integer.MAX_VALUE;
                                    //  if (newlen <= pos + len) {
                                    //    throw new SolrException(400,"Too many terms to uninvert field!");
                                    //  }
                                    // } else {
                                    //  while (newlen <= pos + len) newlen<<=1;  // doubling strategy
                                    // }
                                    //
                                    while (newlen <= pos + len) // doubling strategy
                                    {
                                        newlen <<= 1;
                                    }
                                    var newtarget = new sbyte[newlen];
                                    Arrays.Copy(target, 0, newtarget, 0, pos);
                                    target = newtarget;
                                }
                                Arrays.Copy(arr, 0, target, pos, len);
                                pos += len + 1; // skip single byte at end and leave it 0 for terminator
                            }
                        }
                    }

                    // shrink array
                    if (pos < target.Length)
                    {
                        var newtarget = new sbyte[pos];
                        Arrays.Copy(target, 0, newtarget, 0, pos);
                        target = newtarget;
                    }

                    m_tnums[pass] = target;

                    if ((pass << 16) > maxDoc)
                    {
                        break;
                    }
                }
            }
            if (indexedTerms != null)
            {
                m_indexedTermsArray = new BytesRef[indexedTerms.Count];
                indexedTerms.CopyTo(m_indexedTermsArray, 0);
            }

            long endTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            m_total_time = (int)(endTime - startTime);
            m_phase1_time = (int)(midPoint - startTime);
        }

        /// <summary>
        /// Number of bytes to represent an unsigned int as a vint. 
        /// <para/>
        /// NOTE: This was vIntSize() in Lucene
        /// </summary>
        private static int VInt32Size(int x)
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

        // todo: if we know the size of the vInt already, we could do
        // a single switch on the size
        /// <summary>
        /// NOTE: This was writeInt() in Lucene
        /// </summary>
        private static int WriteInt32(int x, sbyte[] arr, int pos)
        {
            var a = x.TripleShift(7 * 4);
            if (a != 0)
            {
                arr[pos++] = (sbyte)(a | 0x80);
            }
            a = x.TripleShift(7 * 3);
            if (a != 0)
            {
                arr[pos++] = (sbyte)(a | 0x80);
            }
            a = x.TripleShift(7 * 2);
            if (a != 0)
            {
                arr[pos++] = (sbyte)(a | 0x80);
            }
            a = x.TripleShift(7 * 1);
            if (a != 0)
            {
                arr[pos++] = (sbyte)(a | 0x80);
            }
            arr[pos++] = (sbyte)(x & 0x7f);
            return pos;
        }

        /// <summary>
        /// Only used if original <see cref="IndexReader"/> doesn't implement
        /// <see cref="TermsEnum.Ord"/>; in this case we "wrap" our own terms index
        /// around it.
        /// </summary>
        private sealed class OrdWrappedTermsEnum : TermsEnum
        {
            private readonly DocTermOrds outerInstance;

            internal readonly TermsEnum termsEnum;
            internal BytesRef term;
            internal long ord; // force "real" seek

            public OrdWrappedTermsEnum(DocTermOrds outerInstance, AtomicReader reader)
            {
                this.outerInstance = outerInstance;

                ord = -outerInstance.indexInterval - 1;
                if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.m_indexedTermsArray != null);
                termsEnum = reader.Fields.GetTerms(outerInstance.m_field).GetEnumerator();
            }

            public override IComparer<BytesRef> Comparer => termsEnum.Comparer;

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                return termsEnum.Docs(liveDocs, reuse, flags);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                return termsEnum.DocsAndPositions(liveDocs, reuse, flags);
            }

            public override BytesRef Term => term;

            public override bool MoveNext()
            {
                if (++ord < 0)
                {
                    ord = 0;
                }
                if (!termsEnum.MoveNext())
                {
                    term = null;
                    return false;
                }
                return SetTerm() != null;   // this is extra work if we know we are in bounds...
            }

            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                if (MoveNext())
                    return term;
                return null;
            }

            public override int DocFreq => termsEnum.DocFreq;

            public override long TotalTermFreq => termsEnum.TotalTermFreq;

            public override long Ord => outerInstance.m_ordBase + ord;

            public override SeekStatus SeekCeil(BytesRef target)
            {
                // already here
                if (term != null && term.Equals(target))
                {
                    return SeekStatus.FOUND;
                }

                int startIdx = Array.BinarySearch(outerInstance.m_indexedTermsArray, target);

                if (startIdx >= 0)
                {
                    // we hit the term exactly... lucky us!
                    TermsEnum.SeekStatus seekStatus = termsEnum.SeekCeil(target);
                    if (Debugging.AssertsEnabled) Debugging.Assert(seekStatus == TermsEnum.SeekStatus.FOUND);
                    ord = startIdx << outerInstance.indexIntervalBits;
                    SetTerm();
                    if (Debugging.AssertsEnabled) Debugging.Assert(term != null);
                    return SeekStatus.FOUND;
                }

                // we didn't hit the term exactly
                startIdx = -startIdx - 1;

                if (startIdx == 0)
                {
                    // our target occurs *before* the first term
                    TermsEnum.SeekStatus seekStatus = termsEnum.SeekCeil(target);
                    if (Debugging.AssertsEnabled) Debugging.Assert(seekStatus == TermsEnum.SeekStatus.NOT_FOUND);
                    ord = 0;
                    SetTerm();
                    if (Debugging.AssertsEnabled) Debugging.Assert(term != null);
                    return SeekStatus.NOT_FOUND;
                }

                // back up to the start of the block
                startIdx--;

                if ((ord >> outerInstance.indexIntervalBits) == startIdx && term != null && term.CompareTo(target) <= 0)
                {
                    // we are already in the right block and the current term is before the term we want,
                    // so we don't need to seek.
                }
                else
                {
                    // seek to the right block
                    TermsEnum.SeekStatus seekStatus = termsEnum.SeekCeil(outerInstance.m_indexedTermsArray[startIdx]);
                    if (Debugging.AssertsEnabled) Debugging.Assert(seekStatus == TermsEnum.SeekStatus.FOUND);
                    ord = startIdx << outerInstance.indexIntervalBits;
                    SetTerm();
                    if (Debugging.AssertsEnabled) Debugging.Assert(term != null); // should be non-null since it's in the index
                }

                while (term != null && term.CompareTo(target) < 0)
                {
                    MoveNext();
                }

                if (term is null)
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
                int delta = (int)(targetOrd - outerInstance.m_ordBase - ord);
                //System.out.println("  seek(ord) targetOrd=" + targetOrd + " delta=" + delta + " ord=" + ord + " ii=" + indexInterval);
                if (delta < 0 || delta > outerInstance.indexInterval)
                {
                    int idx = (int)targetOrd.TripleShift(outerInstance.indexIntervalBits);
                    BytesRef @base = outerInstance.m_indexedTermsArray[idx];
                    //System.out.println("  do seek term=" + base.utf8ToString());
                    ord = idx << outerInstance.indexIntervalBits;
                    delta = (int)(targetOrd - ord);
                    TermsEnum.SeekStatus seekStatus = termsEnum.SeekCeil(@base);
                    if (Debugging.AssertsEnabled) Debugging.Assert(seekStatus == TermsEnum.SeekStatus.FOUND);
                }
                else
                {
                    //System.out.println("seek w/in block");
                }

                while (--delta >= 0)
                {
                    if (!termsEnum.MoveNext())
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(false);
                        return;
                    }
                    ord++;
                }

                SetTerm();
                if (Debugging.AssertsEnabled) Debugging.Assert(term != null);
            }

            private BytesRef SetTerm()
            {
                term = termsEnum.Term;
                //System.out.println("  setTerm() term=" + term.utf8ToString() + " vs prefix=" + (prefix is null ? "null" : prefix.utf8ToString()));
                if (outerInstance.m_prefix != null && !StringHelper.StartsWith(term, outerInstance.m_prefix))
                {
                    term = null;
                }
                return term;
            }
        }

        /// <summary>
        /// Returns the term (<see cref="BytesRef"/>) corresponding to
        /// the provided ordinal.
        /// </summary>
        public virtual BytesRef LookupTerm(TermsEnum termsEnum, int ord)
        {
            termsEnum.SeekExact(ord);
            return termsEnum.Term;
        }

        /// <summary>
        /// Returns a <see cref="SortedSetDocValues"/> view of this instance </summary>
        public virtual SortedSetDocValues GetIterator(AtomicReader reader)
        {
            if (IsEmpty)
            {
                return DocValues.EMPTY_SORTED_SET;
            }
            else
            {
                return new Iterator(this, reader);
            }
        }

        private class Iterator : SortedSetDocValues
        {
            private readonly DocTermOrds outerInstance;

            private readonly AtomicReader reader;
            private readonly TermsEnum te; // used internally for lookupOrd() and lookupTerm()

            // currently we read 5 at a time (using the logic of the old iterator)
            private readonly int[] buffer = new int[5];

            private int bufferUpto;
            private int bufferLength;

            private int tnum;
            private int upto;
            private sbyte[] arr;

            internal Iterator(DocTermOrds outerInstance, AtomicReader reader)
            {
                this.outerInstance = outerInstance;
                this.reader = reader;
                this.te = GetTermsEnum();
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

            /// <summary>
            /// Buffer must be at least 5 <see cref="int"/>s long.  Returns number
            /// of term ords placed into buffer; if this count is
            /// less than buffer.Length then that is the end.
            /// </summary>
            internal virtual int Read(int[] buffer)
            {
                int bufferUpto = 0;
                if (arr is null)
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
                            if (delta == 0)
                            {
                                break;
                            }
                            tnum += delta - TNUM_OFFSET;
                            buffer[bufferUpto++] = outerInstance.m_ordBase + tnum;
                            //System.out.println("  tnum=" + tnum);
                            delta = 0;
                        }
                        code = code.TripleShift(8);
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
                            sbyte b = arr[upto++];
                            delta = (delta << 7) | (b & 0x7f);
                            //System.out.println("    cycle: upto=" + upto + " delta=" + delta + " b=" + b);
                            if ((b & 0x80) == 0)
                            {
                                break;
                            }
                        }
                        //System.out.println("  delta=" + delta);
                        if (delta == 0)
                        {
                            break;
                        }
                        tnum += delta - TNUM_OFFSET;
                        //System.out.println("  tnum=" + tnum);
                        buffer[bufferUpto++] = outerInstance.m_ordBase + tnum;
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
                int code = outerInstance.m_index[docID];
                if ((code & 0xff) == 1)
                {
                    // a pointer
                    upto = code.TripleShift(8);
                    //System.out.println("    pointer!  upto=" + upto);
                    int whichArray = (docID.TripleShift(16)) & 0xff;
                    arr = outerInstance.m_tnums[whichArray];
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
                BytesRef @ref/* = null*/; // LUCENENET: IDE0059: Remove unnecessary value assignment
                try
                {
                    @ref = outerInstance.LookupTerm(te, (int)ord);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
                result.Bytes = @ref.Bytes;
                result.Offset = @ref.Offset;
                result.Length = @ref.Length;
            }

            public override long ValueCount => outerInstance.NumTerms;

            public override long LookupTerm(BytesRef key)
            {
                try
                {
                    if (te.SeekCeil(key) == SeekStatus.FOUND)
                    {
                        return te.Ord;
                    }
                    else
                    {
                        return -te.Ord - 1;
                    }
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
            }

            public override TermsEnum GetTermsEnum()
            {
                try
                {
                    return outerInstance.GetOrdTermsEnum(reader);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }
    }
}