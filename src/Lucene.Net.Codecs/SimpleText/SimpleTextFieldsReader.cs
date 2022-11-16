using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Codecs.SimpleText
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

    using ArrayUtil = Util.ArrayUtil;
    using BufferedChecksumIndexInput = Store.BufferedChecksumIndexInput;
    using BytesRef = Util.BytesRef;
    using CharsRef = Util.CharsRef;
    using ChecksumIndexInput = Store.ChecksumIndexInput;
    using DocsAndPositionsEnum = Index.DocsAndPositionsEnum;
    using DocsEnum = Index.DocsEnum;
    using FieldInfo = Index.FieldInfo;
    using FieldInfos = Index.FieldInfos;
    using FixedBitSet = Util.FixedBitSet;
    using FST = Util.Fst.FST;
    using IBits = Util.IBits;
    using IndexInput = Store.IndexInput;
    using IndexOptions = Index.IndexOptions;
    using Int32sRef = Util.Int32sRef;
    using IOUtils = Util.IOUtils;
    using PositiveInt32Outputs = Util.Fst.PositiveInt32Outputs;
    using SegmentReadState = Index.SegmentReadState;
    using StringHelper = Util.StringHelper;
    using Terms = Index.Terms;
    using TermsEnum = Index.TermsEnum;
    using UnicodeUtil = Util.UnicodeUtil;
    using Util = Util.Fst.Util;

    internal class SimpleTextFieldsReader : FieldsProducer
    {
        private readonly JCG.SortedDictionary<string, long> fields;
        private readonly IndexInput input;
        private readonly FieldInfos fieldInfos;
        private readonly int maxDoc;

        public SimpleTextFieldsReader(SegmentReadState state)
        {
            this.maxDoc = state.SegmentInfo.DocCount;
            fieldInfos = state.FieldInfos;
            input = state.Directory.OpenInput(SimpleTextPostingsFormat.GetPostingsFileName(state.SegmentInfo.Name, state.SegmentSuffix), state.Context);
            bool success = false;
            try
            {
                fields = ReadFields((IndexInput)input.Clone());
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(this);
                }
            }
        }

        private static JCG.SortedDictionary<string, long> ReadFields(IndexInput @in) // LUCENENET specific - marked static
        {
            ChecksumIndexInput input = new BufferedChecksumIndexInput(@in);
            BytesRef scratch = new BytesRef(10);
            // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
            var fields = new JCG.SortedDictionary<string, long>(StringComparer.Ordinal);

            while (true)
            {
                SimpleTextUtil.ReadLine(input, scratch);
                if (scratch.Equals(SimpleTextFieldsWriter.END))
                {
                    SimpleTextUtil.CheckFooter(input);
                    return fields;
                }
                else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.FIELD))
                {
                    string fieldName = Encoding.UTF8.GetString(scratch.Bytes, scratch.Offset + SimpleTextFieldsWriter.FIELD.Length, scratch.Length - SimpleTextFieldsWriter.FIELD.Length);
                    fields[fieldName] = input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }
            }
        }

        private class SimpleTextTermsEnum : TermsEnum
        {
            private readonly SimpleTextFieldsReader outerInstance;

            private readonly IndexOptions indexOptions;
            private int docFreq;
            private long totalTermFreq;
            private long docsStart;
            //private bool ended; // LUCENENET: Never read
            private readonly BytesRefFSTEnum<PairOutputs<Int64, PairOutputs<Int64, Int64>.Pair>.Pair> fstEnum;

            public SimpleTextTermsEnum(SimpleTextFieldsReader outerInstance, FST<PairOutputs<Int64, PairOutputs<Int64, Int64>.Pair>.Pair> fst, IndexOptions indexOptions)
            {
                this.outerInstance = outerInstance;
                this.indexOptions = indexOptions;
                fstEnum = new BytesRefFSTEnum<PairOutputs<Int64, PairOutputs<Int64, Int64>.Pair>.Pair>(fst);
            }

            public override bool SeekExact(BytesRef text)
            {
                var result = fstEnum.SeekExact(text);
                if (result != null)
                {
                    var pair1 = result.Output;
                    var pair2 = pair1.Output2;
                    docsStart = pair1.Output1;
                    docFreq = (int)pair2.Output1;
                    totalTermFreq = pair2.Output2;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override SeekStatus SeekCeil(BytesRef text)
            {
                //System.out.println("seek to text=" + text.utf8ToString());
                var result = fstEnum.SeekCeil(text);
                if (result is null)
                {
                    //System.out.println("  end");
                    return SeekStatus.END;
                }
                else
                {
                    //System.out.println("  got text=" + term.utf8ToString());
                    var pair1 = result.Output;
                    var pair2 = pair1.Output2;
                    docsStart = pair1.Output1;
                    docFreq = (int)pair2.Output1;
                    totalTermFreq = pair2.Output2;

                    if (result.Input.Equals(text))
                    {
                        //System.out.println("  match docsStart=" + docsStart);
                        return SeekStatus.FOUND;
                    }
                    else
                    {
                        //System.out.println("  not match docsStart=" + docsStart);
                        return SeekStatus.NOT_FOUND;
                    }
                }
            }

            public override bool MoveNext()
            {
                //if (Debugging.AssertsEnabled) Debugging.Assert(!ended); // LUCENENET: Ended field is never set, so this can never fail
                if (fstEnum.MoveNext())
                {
                    var pair1 = fstEnum.Current.Output;
                    var pair2 = pair1.Output2;
                    docsStart = pair1.Output1;
                    docFreq = (int)pair2.Output1;
                    totalTermFreq = pair2.Output2;
                    return fstEnum.Current.Input != null;
                }
                else
                {
                    return false;
                }
            }

            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                if (MoveNext())
                    return fstEnum.Current.Input;
                return null;
            }

            public override BytesRef Term => fstEnum.Current.Input;

            public override long Ord => throw UnsupportedOperationException.Create();

            public override void SeekExact(long ord) => throw UnsupportedOperationException.Create();

            public override int DocFreq => docFreq;

            public override long TotalTermFreq => indexOptions == IndexOptions.DOCS_ONLY ? -1 : totalTermFreq;

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                if (reuse is null || !(reuse is SimpleTextDocsEnum docsEnum) || !docsEnum.CanReuse(outerInstance.input))
                    docsEnum = new SimpleTextDocsEnum(outerInstance);

                return docsEnum.Reset(docsStart, liveDocs, indexOptions == IndexOptions.DOCS_ONLY, docFreq);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                if (IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
                {
                    // Positions were not indexed
                    return null;
                }

                if (reuse is null || !(reuse is SimpleTextDocsAndPositionsEnum docsAndPositionsEnum) || !docsAndPositionsEnum.CanReuse(outerInstance.input))
                    docsAndPositionsEnum = new SimpleTextDocsAndPositionsEnum(outerInstance);

                return docsAndPositionsEnum.Reset(docsStart, liveDocs, indexOptions, docFreq);
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;
        }

        private class SimpleTextDocsEnum : DocsEnum
        {
            private readonly IndexInput inStart;
            private readonly IndexInput input;
            private bool omitTF;
            private int docID = -1;
            private int tf;
            private IBits liveDocs;
            private readonly BytesRef scratch = new BytesRef(10);
            private readonly CharsRef scratchUTF16 = new CharsRef(10);
            private int cost;

            public SimpleTextDocsEnum(SimpleTextFieldsReader outerInstance)
            {
                this.inStart = outerInstance.input;
                this.input = (IndexInput)this.inStart.Clone();
            }

            public virtual bool CanReuse(IndexInput @in)
            {
                return @in == inStart;
            }

            public virtual SimpleTextDocsEnum Reset(long fp, IBits liveDocs, bool omitTF, int docFreq)
            {
                this.liveDocs = liveDocs;
                input.Seek(fp);
                this.omitTF = omitTF;
                docID = -1;
                tf = 1;
                cost = docFreq;
                return this;
            }

            public override int DocID => docID;

            public override int Freq => tf;

            public override int NextDoc()
            {
                if (docID == NO_MORE_DOCS)
                {
                    return docID;
                }
                bool first = true;
                int termFreq = 0;
                while (true)
                {
                    long lineStart = input.Position;
                    SimpleTextUtil.ReadLine(input, scratch);
                    if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.DOC))
                    {
                        if (!first && (liveDocs is null || liveDocs.Get(docID)))
                        {
                            input.Seek(lineStart);
                            if (!omitTF)
                            {
                                tf = termFreq;
                            }
                            return docID;
                        }
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + SimpleTextFieldsWriter.DOC.Length, scratch.Length - SimpleTextFieldsWriter.DOC.Length, scratchUTF16);
                        docID = ArrayUtil.ParseInt32(scratchUTF16.Chars, 0, scratchUTF16.Length);
                        termFreq = 0;
                        first = false;
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.FREQ))
                    {
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + SimpleTextFieldsWriter.FREQ.Length, scratch.Length - SimpleTextFieldsWriter.FREQ.Length, scratchUTF16);
                        termFreq = ArrayUtil.ParseInt32(scratchUTF16.Chars, 0, scratchUTF16.Length);
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.POS))
                    {
                        // skip termFreq++;
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.START_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.END_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.PAYLOAD))
                    {
                        // skip
                    }
                    else
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(
                            StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.TERM)
                            || StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.FIELD)
                            || StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.END),
                            "scratch={0}", new BytesRefFormatter(scratch, BytesRefFormat.UTF8));
                        if (!first && (liveDocs is null || liveDocs.Get(docID)))
                        {
                            input.Seek(lineStart);
                            if (!omitTF)
                            {
                                tf = termFreq;
                            }
                            return docID;
                        }
                        return docID = NO_MORE_DOCS;
                    }
                }
            }


            public override int Advance(int target)
            {
                // Naive -- better to index skip data
                return SlowAdvance(target);
            }

            public override long GetCost() => cost;
        }

        private class SimpleTextDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly IndexInput inStart;
            private readonly IndexInput input;
            private int docID = -1;
            private int tf;
            private IBits liveDocs;
            private readonly BytesRef scratch = new BytesRef(10);
            private readonly BytesRef scratch2 = new BytesRef(10);
            private readonly CharsRef scratchUTF16 = new CharsRef(10);
            private readonly CharsRef scratchUTF16_2 = new CharsRef(10);
            private BytesRef payload;
            private long nextDocStart;
            private bool readOffsets;
            private bool readPositions;
            private int startOffset;
            private int endOffset;
            private int cost;

            public SimpleTextDocsAndPositionsEnum(SimpleTextFieldsReader outerInstance)
            {
                this.inStart = outerInstance.input;
                this.input = (IndexInput)inStart.Clone();
            }

            public virtual bool CanReuse(IndexInput @in)
            {
                return @in == inStart;
            }

            public virtual SimpleTextDocsAndPositionsEnum Reset(long fp, IBits liveDocs, IndexOptions indexOptions, int docFreq)
            {
                this.liveDocs = liveDocs;
                nextDocStart = fp;
                docID = -1;
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                readPositions = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
                readOffsets = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
                if (!readOffsets)
                {
                    startOffset = -1;
                    endOffset = -1;
                }
                cost = docFreq;
                return this;
            }

            public override int DocID => docID;

            public override int Freq => tf;


            public override int NextDoc()
            {
                bool first = true;
                input.Seek(nextDocStart);
                long posStart = 0;
                while (true)
                {
                    long lineStart = input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    SimpleTextUtil.ReadLine(input, scratch);
                    //System.out.println("NEXT DOC: " + scratch.utf8ToString());
                    if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.DOC))
                    {
                        if (!first && (liveDocs is null || liveDocs.Get(docID)))
                        {
                            nextDocStart = lineStart;
                            input.Seek(posStart);
                            return docID;
                        }
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + SimpleTextFieldsWriter.DOC.Length, scratch.Length - SimpleTextFieldsWriter.DOC.Length, scratchUTF16);
                        docID = ArrayUtil.ParseInt32(scratchUTF16.Chars, 0, scratchUTF16.Length);
                        tf = 0;
                        first = false;
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.FREQ))
                    {
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + SimpleTextFieldsWriter.FREQ.Length, scratch.Length - SimpleTextFieldsWriter.FREQ.Length, scratchUTF16);
                        tf = ArrayUtil.ParseInt32(scratchUTF16.Chars, 0, scratchUTF16.Length);
                        posStart = input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.POS))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.START_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.END_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.PAYLOAD))
                    {
                        // skip
                    }
                    else
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(
                            StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.TERM)
                            || StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.FIELD)
                            || StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.END));
                        if (!first && (liveDocs is null || liveDocs.Get(docID)))
                        {
                            nextDocStart = lineStart;
                            input.Seek(posStart);
                            return docID;
                        }
                        return docID = NO_MORE_DOCS;
                    }
                }
            }

            public override int Advance(int target)
            {
                // Naive -- better to index skip data
                return SlowAdvance(target);
            }

            public override int NextPosition()
            {
                int pos;
                if (readPositions)
                {
                    SimpleTextUtil.ReadLine(input, scratch);
                    // LUCENENET specific - use wrapper BytesRefFormatter struct to defer building the string unless string.Format() is called
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.POS), "got line={0}", new BytesRefFormatter(scratch, BytesRefFormat.UTF8));
                    UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + SimpleTextFieldsWriter.POS.Length, scratch.Length - SimpleTextFieldsWriter.POS.Length, scratchUTF16_2);
                    pos = ArrayUtil.ParseInt32(scratchUTF16_2.Chars, 0, scratchUTF16_2.Length);
                }
                else
                {
                    pos = -1;
                }

                if (readOffsets)
                {
                    SimpleTextUtil.ReadLine(input, scratch);
                    // LUCENENET specific - use wrapper BytesRefFormatter struct to defer building the string unless string.Format() is called
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.START_OFFSET), "got line={0}", new BytesRefFormatter(scratch, BytesRefFormat.UTF8));
                    UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + SimpleTextFieldsWriter.START_OFFSET.Length, scratch.Length - SimpleTextFieldsWriter.START_OFFSET.Length, scratchUTF16_2);
                    startOffset = ArrayUtil.ParseInt32(scratchUTF16_2.Chars, 0, scratchUTF16_2.Length);
                    SimpleTextUtil.ReadLine(input, scratch);
                    // LUCENENET specific - use wrapper BytesRefFormatter struct to defer building the string unless string.Format() is called
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.END_OFFSET), "got line={0}", new BytesRefFormatter(scratch, BytesRefFormat.UTF8));
                    UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + SimpleTextFieldsWriter.END_OFFSET.Length, scratch.Length - SimpleTextFieldsWriter.END_OFFSET.Length, scratchUTF16_2);
                    endOffset = ArrayUtil.ParseInt32(scratchUTF16_2.Chars, 0, scratchUTF16_2.Length);
                }

                long fp = input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                SimpleTextUtil.ReadLine(input, scratch);
                if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.PAYLOAD))
                {
                    int len = scratch.Length - SimpleTextFieldsWriter.PAYLOAD.Length;
                    if (scratch2.Bytes.Length < len)
                    {
                        scratch2.Grow(len);
                    }
                    Arrays.Copy(scratch.Bytes, SimpleTextFieldsWriter.PAYLOAD.Length, scratch2.Bytes, 0, len);
                    scratch2.Length = len;
                    payload = scratch2;
                }
                else
                {
                    payload = null;
                    input.Seek(fp);
                }
                return pos;
            }

            public override int StartOffset => startOffset;

            public override int EndOffset => endOffset;

            public override BytesRef GetPayload() => payload;

            public override long GetCost() => cost;
        }

        internal class TermData
        {
            public long DocsStart { get; set; }
            public int DocFreq { get; set; }

            public TermData(long docsStart, int docFreq)
            {
                DocsStart = docsStart;
                DocFreq = docFreq;
            }
        }

        private class SimpleTextTerms : Terms
        {
            private readonly SimpleTextFieldsReader outerInstance;

            private readonly long termsStart;
            private readonly FieldInfo fieldInfo;
            private readonly int maxDoc;
            private long sumTotalTermFreq;
            private long sumDocFreq;
            private int docCount;
            private FST<PairOutputs<Int64, PairOutputs<Int64, Int64>.Pair>.Pair> fst;
            private int termCount;
            private readonly BytesRef scratch = new BytesRef(10);
            private readonly CharsRef scratchUTF16 = new CharsRef(10);

            public SimpleTextTerms(SimpleTextFieldsReader outerInstance, string field, long termsStart, int maxDoc)
            {
                this.outerInstance = outerInstance;
                this.maxDoc = maxDoc;
                this.termsStart = termsStart;
                fieldInfo = outerInstance.fieldInfos.FieldInfo(field);
                LoadTerms();
            }

            private void LoadTerms()
            {
                PositiveInt32Outputs posIntOutputs = PositiveInt32Outputs.Singleton;
                var outputsInner = new PairOutputs<Int64, Int64>(posIntOutputs, posIntOutputs);
                var outputs = new PairOutputs<Int64, PairOutputs<Int64, Int64>.Pair>(posIntOutputs,
                    outputsInner);
                var b = new Builder<PairOutputs<Int64, PairOutputs<Int64, Int64>.Pair>.Pair>(FST.INPUT_TYPE.BYTE1, outputs);
                IndexInput @in = (IndexInput)outerInstance.input.Clone();
                @in.Seek(termsStart);
                BytesRef lastTerm = new BytesRef(10);
                long lastDocsStart = -1;
                int docFreq = 0;
                long totalTermFreq = 0;
                FixedBitSet visitedDocs = new FixedBitSet(maxDoc);
                Int32sRef scratchIntsRef = new Int32sRef();
                while (true)
                {
                    SimpleTextUtil.ReadLine(@in, scratch);
                    if (scratch.Equals(SimpleTextFieldsWriter.END) || StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.FIELD))
                    {
                        if (lastDocsStart != -1)
                        {
                            b.Add(Util.ToInt32sRef(lastTerm, scratchIntsRef),
                                  outputs.NewPair(lastDocsStart,
                                                  outputsInner.NewPair((long)docFreq, totalTermFreq)));
                            sumTotalTermFreq += totalTermFreq;
                        }
                        break;
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.DOC))
                    {
                        docFreq++;
                        sumDocFreq++;
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + SimpleTextFieldsWriter.DOC.Length, scratch.Length - SimpleTextFieldsWriter.DOC.Length, scratchUTF16);
                        int docID = ArrayUtil.ParseInt32(scratchUTF16.Chars, 0, scratchUTF16.Length);
                        visitedDocs.Set(docID);
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.FREQ))
                    {
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + SimpleTextFieldsWriter.FREQ.Length, scratch.Length - SimpleTextFieldsWriter.FREQ.Length, scratchUTF16);
                        totalTermFreq += ArrayUtil.ParseInt32(scratchUTF16.Chars, 0, scratchUTF16.Length);
                    }
                    else if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.TERM))
                    {
                        if (lastDocsStart != -1)
                        {
                            b.Add(Util.ToInt32sRef(lastTerm, scratchIntsRef), outputs.NewPair(lastDocsStart,
                                                                                            outputsInner.NewPair((long)docFreq, totalTermFreq)));
                        }
                        lastDocsStart = @in.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        int len = scratch.Length - SimpleTextFieldsWriter.TERM.Length;
                        if (len > lastTerm.Length)
                        {
                            lastTerm.Grow(len);
                        }
                        Arrays.Copy(scratch.Bytes, SimpleTextFieldsWriter.TERM.Length, lastTerm.Bytes, 0, len);
                        lastTerm.Length = len;
                        docFreq = 0;
                        sumTotalTermFreq += totalTermFreq;
                        totalTermFreq = 0;
                        termCount++;
                    }
                }
                docCount = visitedDocs.Cardinality;
                fst = b.Finish();
                /*
                PrintStream ps = new PrintStream("out.dot");
                fst.toDot(ps);
                ps.close();
                System.out.println("SAVED out.dot");
                */
                //System.out.println("FST " + fst.sizeInBytes());
            }

            /// <summary>Returns approximate RAM bytes used.</summary>
            public virtual long RamBytesUsed()
            {
                return (fst != null) ? fst.GetSizeInBytes() : 0;
            }

            public override TermsEnum GetEnumerator()
            {
                if (fst != null)
                {
                    return new SimpleTextTermsEnum(outerInstance, fst, fieldInfo.IndexOptions);
                }
                else
                {
                    return TermsEnum.EMPTY;
                }
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            public override long Count => (long)termCount;

            public override long SumTotalTermFreq => fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY ? -1 : sumTotalTermFreq;

            public override long SumDocFreq => sumDocFreq;

            public override int DocCount => docCount;

            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            public override bool HasFreqs => IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS) >= 0;

            public override bool HasOffsets => IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;

            public override bool HasPositions => IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;

            public override bool HasPayloads => fieldInfo.HasPayloads;
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return fields.Keys.GetEnumerator();
        }

        private readonly IDictionary<string, SimpleTextTerms> termsCache = new Dictionary<string, SimpleTextTerms>();

        public override Terms GetTerms(string field)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!termsCache.TryGetValue(field, out SimpleTextTerms terms) || terms is null)
                {
                    if (!fields.TryGetValue(field, out long fp))
                    {
                        return null;
                    }
                    else
                    {
                        terms = new SimpleTextTerms(this, field, fp, maxDoc);
                        termsCache[field] = terms;
                    }
                }

                return terms;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override int Count => -1;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                input?.Dispose();
            }
        }

        public override long RamBytesUsed()
        {
            long sizeInBytes = 0;
            foreach (SimpleTextTerms simpleTextTerms in termsCache.Values)
            {
                sizeInBytes += (simpleTextTerms != null) ? simpleTextTerms.RamBytesUsed() : 0;
            }
            return sizeInBytes;
        }

        public override void CheckIntegrity()
        {
        }
    }
}