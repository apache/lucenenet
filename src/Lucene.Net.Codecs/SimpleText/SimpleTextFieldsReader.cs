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

using System.Linq;

namespace Lucene.Net.Codecs.SimpleText
{

    using System;
    using System.Diagnostics;
    using System.Collections.Generic;

    using DocsAndPositionsEnum = Index.DocsAndPositionsEnum;
    using DocsEnum = Index.DocsEnum;
    using FieldInfo = Index.FieldInfo;
    using IndexOptions = Index.FieldInfo.IndexOptions;
    using FieldInfos = Index.FieldInfos;
    using SegmentReadState = Index.SegmentReadState;
    using Terms = Index.Terms;
    using TermsEnum = Index.TermsEnum;
    using BufferedChecksumIndexInput = Store.BufferedChecksumIndexInput;
    using ChecksumIndexInput = Store.ChecksumIndexInput;
    using IndexInput = Store.IndexInput;
    using ArrayUtil = Util.ArrayUtil;
    using Bits = Util.Bits;
    using BytesRef = Util.BytesRef;
    using CharsRef = Util.CharsRef;
    using FixedBitSet = Util.FixedBitSet;
    using IOUtils = Util.IOUtils;
    using IntsRef = Util.IntsRef;
    using StringHelper = Util.StringHelper;
    using UnicodeUtil = Util.UnicodeUtil;
    using Builder = Util.Fst.Builder;
    using BytesRefFSTEnum = Util.Fst.BytesRefFSTEnum;
    using FST = Util.Fst.FST;
    using PairOutputs = Util.Fst.PairOutputs;
    using PositiveIntOutputs = Util.Fst.PositiveIntOutputs;
    using Util = Util.Fst.Util;

////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextFieldsWriter.END;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextFieldsWriter.FIELD;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextFieldsWriter.TERM;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextFieldsWriter.DOC;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextFieldsWriter.FREQ;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextFieldsWriter.POS;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextFieldsWriter.START_OFFSET;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextFieldsWriter.END_OFFSET;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextFieldsWriter.PAYLOAD;

    internal class SimpleTextFieldsReader : FieldsProducer
    {
        private readonly SortedDictionary<string, long?> fields;
        private readonly IndexInput _input;
        private readonly FieldInfos fieldInfos;
        private readonly int maxDoc;
        private readonly IDictionary<string, SimpleTextTerms> _termsCache = new Dictionary<string, SimpleTextTerms>();

        public SimpleTextFieldsReader(SegmentReadState state)
        {
            this.maxDoc = state.SegmentInfo.DocCount;
            fieldInfos = state.FieldInfos;
            _input =
                state.Directory.OpenInput(
                    SimpleTextPostingsFormat.GetPostingsFileName(state.SegmentInfo.Name, state.SegmentSuffix),
                    state.Context);
            bool success = false;
            try
            {
                fields = readFields((IndexInput)_input.Clone());
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(this);
                }
            }
        }

        private SortedDictionary<string, long?> ReadFields(IndexInput @in)
        {
            ChecksumIndexInput input = new BufferedChecksumIndexInput(@in);
            BytesRef scratch = new BytesRef(10);
            SortedDictionary<string, long?> fields = new SortedDictionary<string, long?>();

            while (true)
            {
                SimpleTextUtil.ReadLine(input, scratch);
                if (scratch.Equals(END))
                {
                    SimpleTextUtil.CheckFooter(input);
                    return fields;
                }
                else if (StringHelper.StartsWith(scratch, FIELD))
                {
                    string fieldName = new string(scratch.Bytes, scratch.Offset + FIELD.length,
                        scratch.Length - FIELD.length, StandardCharsets.UTF_8);
                    fields[fieldName] = input.FilePointer;
                }
            }
        }

        private class SimpleTextTermsEnum : TermsEnum
        {
            private readonly SimpleTextFieldsReader outerInstance;

            internal readonly FieldInfo.IndexOptions indexOptions;
            internal int docFreq_Renamed;
            internal long totalTermFreq_Renamed;
            internal long docsStart;
            internal bool ended;
            internal readonly BytesRefFSTEnum<PairOutputs.Pair<long?, PairOutputs.Pair<long?, long?>>> fstEnum;

            public SimpleTextTermsEnum(SimpleTextFieldsReader outerInstance,
                FST<PairOutputs.Pair<long?, PairOutputs.Pair<long?, long?>>> fst, FieldInfo.IndexOptions indexOptions)
            {
                this.outerInstance = outerInstance;
                this.indexOptions = indexOptions;
                fstEnum = new BytesRefFSTEnum<>(fst);
            }

            public override bool SeekExact(BytesRef text)
            {

                BytesRefFSTEnum.InputOutput<PairOutputs.Pair<long?, PairOutputs.Pair<long?, long?>>> result =
                    fstEnum.SeekExact(text);
                if (result != null)
                {
                    PairOutputs.Pair<long?, PairOutputs.Pair<long?, long?>> pair1 = result.output;
                    PairOutputs.Pair<long?, long?> pair2 = pair1.output2;
                    docsStart = pair1.Output1;
                    docFreq_Renamed = (int) pair2.Output1;
                    totalTermFreq_Renamed = pair2.Output2;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override SeekStatus SeekCeil(BytesRef text)
            {

                BytesRefFSTEnum.InputOutput<PairOutputs.Pair<long?, PairOutputs.Pair<long?, long?>>> result =
                    fstEnum.SeekCeil(text);
                if (result == null)
                {
                    //System.out.println("  end");
                    return SeekStatus.END;
                }
                else
                {
                    //System.out.println("  got text=" + term.utf8ToString());
                    PairOutputs.Pair<long?, PairOutputs.Pair<long?, long?>> pair1 = result.output;
                    PairOutputs.Pair<long?, long?> pair2 = pair1.output2;
                    docsStart = pair1.output1;
                    docFreq_Renamed = (int) pair2.output1;
                    totalTermFreq_Renamed = pair2.output2;

                    if (result.input.Equals(text))
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
            public override BytesRef Next()
            {
                Debug.Assert(!ended);
                BytesRefFSTEnum.InputOutput<PairOutputs.Pair<long?, PairOutputs.Pair<long?, long?>>> result =
                    fstEnum.Next();
                if (result != null)
                {
                    PairOutputs.Pair<long?, PairOutputs.Pair<long?, long?>> pair1 = result.output;
                    PairOutputs.Pair<long?, long?> pair2 = pair1.output2;
                    docsStart = pair1.output1;
                    docFreq_Renamed = (int) pair2.output1;
                    totalTermFreq_Renamed = pair2.output2;
                    return result.input;
                }
                else
                {
                    return null;
                }
            }

            public override BytesRef Term()
            {
                return fstEnum.Current().Input;
            }

            public override long Ord()
            {
                throw new NotSupportedException();
            }

            public override void SeekExact(long ord)
            {
                throw new NotSupportedException();
            }

            public override int DocFreq()
            {
                return docFreq_Renamed;
            }

            public override long TotalTermFreq()
            {
                return indexOptions == IndexOptions.DOCS_ONLY ? - 1 : totalTermFreq_Renamed;
            }

            public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
            {
                SimpleTextDocsEnum docsEnum;
                if (reuse != null && reuse is SimpleTextDocsEnum &&
                    ((SimpleTextDocsEnum) reuse).CanReuse(outerInstance._input))
                {
                    docsEnum = (SimpleTextDocsEnum) reuse;
                }
                else
                {
                    docsEnum = new SimpleTextDocsEnum(outerInstance);
                }
                return docsEnum.Reset(docsStart, liveDocs, indexOptions == IndexOptions.DOCS_ONLY,
                    docFreq_Renamed);
            }

            public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {

                if (indexOptions < IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    // Positions were not indexed
                    return null;
                }

                SimpleTextDocsAndPositionsEnum docsAndPositionsEnum;
                if (reuse != null && reuse is SimpleTextDocsAndPositionsEnum &&
                    ((SimpleTextDocsAndPositionsEnum) reuse).canReuse(outerInstance._input))
                {
                    docsAndPositionsEnum = (SimpleTextDocsAndPositionsEnum) reuse;
                }
                else
                {
                    docsAndPositionsEnum = new SimpleTextDocsAndPositionsEnum(outerInstance);
                }
                return docsAndPositionsEnum.reset(docsStart, liveDocs, indexOptions, docFreq_Renamed);
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }
        }

        private class SimpleTextDocsEnum : DocsEnum
        {
            private readonly SimpleTextFieldsReader outerInstance;

            internal readonly IndexInput inStart;
            internal readonly IndexInput @in;
            internal bool omitTF;
            internal int docID_Renamed = -1;
            internal int tf;
            internal Bits liveDocs;
            internal readonly BytesRef scratch = new BytesRef(10);
            internal readonly CharsRef scratchUTF16 = new CharsRef(10);
            internal int cost_Renamed;

            public SimpleTextDocsEnum(SimpleTextFieldsReader outerInstance)
            {
                this.outerInstance = outerInstance;
                inStart = outerInstance._input;
                @in = (IndexInput) inStart.Clone();
            }

            public virtual bool CanReuse(IndexInput @in)
            {
                return @in == inStart;
            }

            public virtual SimpleTextDocsEnum Reset(long fp, Bits liveDocs, bool omitTF, int docFreq)
            {
                this.liveDocs = liveDocs;
                @in.Seek(fp);
                this.omitTF = omitTF;
                docID_Renamed = -1;
                tf = 1;
                cost_Renamed = docFreq;
                return this;
            }

            public override int DocID()
            {
                return docID_Renamed;
            }

            public override int Freq()
            {
                return tf;
            }

            public override int NextDoc()
            {
                if (docID_Renamed == NO_MORE_DOCS)
                {
                    return docID_Renamed;
                }
                bool first = true;
                int termFreq = 0;
                while (true)
                {
                    long lineStart = @in.FilePointer;
                    SimpleTextUtil.ReadLine(@in, scratch);
                    if (StringHelper.StartsWith(scratch, DOC))
                    {
                        if (!first && (liveDocs == null || liveDocs.Get(docID_Renamed)))
                        {
                            @in.Seek(lineStart);
                            if (!omitTF)
                            {
                                tf = termFreq;
                            }
                            return docID_Renamed;
                        }
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + DOC.length, scratch.Length - DOC.length,
                            scratchUTF16);
                        docID_Renamed = ArrayUtil.ParseInt(scratchUTF16.Chars, 0, scratchUTF16.length);
                        termFreq = 0;
                        first = false;
                    }
                    else if (StringHelper.StartsWith(scratch, FREQ))
                    {
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + FREQ.length,
                            scratch.Length - FREQ.length, scratchUTF16);
                        termFreq = ArrayUtil.ParseInt(scratchUTF16.Chars, 0, scratchUTF16.length);
                    }
                    else if (StringHelper.StartsWith(scratch, POS))
                    {
                        // skip termFreq++;
                    }
                    else if (StringHelper.StartsWith(scratch, START_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(scratch, END_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(scratch, PAYLOAD))
                    {
                        // skip
                    }
                    else
                    {
                        Debug.Assert(
                            StringHelper.StartsWith(scratch, TERM) || StringHelper.StartsWith(scratch, FIELD) ||
                            StringHelper.StartsWith(scratch, END), "scratch=" + scratch.Utf8ToString());
                        if (!first && (liveDocs == null || liveDocs.Get(docID_Renamed)))
                        {
                            @in.Seek(lineStart);
                            if (!omitTF)
                            {
                                tf = termFreq;
                            }
                            return docID_Renamed;
                        }
                        return docID_Renamed = NO_MORE_DOCS;
                    }
                }
            }

            public override int Advance(int target)
            {
                // Naive -- better to index skip data
                return SlowAdvance(target);
            }

            public override long Cost()
            {
                return cost_Renamed;
            }
        }

        private class SimpleTextDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly SimpleTextFieldsReader outerInstance;

            internal readonly IndexInput inStart;
            internal readonly IndexInput @in;
            internal int docID_Renamed = -1;
            internal int tf;
            internal Bits liveDocs;
            internal readonly BytesRef scratch = new BytesRef(10);
            internal readonly BytesRef scratch2 = new BytesRef(10);
            internal readonly CharsRef scratchUTF16 = new CharsRef(10);
            internal readonly CharsRef scratchUTF16_2 = new CharsRef(10);
            internal BytesRef payload;
            internal long nextDocStart;
            internal bool readOffsets;
            internal bool readPositions;
            internal int startOffset_Renamed;
            internal int endOffset_Renamed;
            internal int cost_Renamed;

            public SimpleTextDocsAndPositionsEnum(SimpleTextFieldsReader outerInstance)
            {
                this.outerInstance = outerInstance;
                this.inStart = outerInstance._input;
                this.@in = (IndexInput) inStart.Clone();
            }

            public virtual bool canReuse(IndexInput @in)
            {
                return @in == inStart;
            }

            public virtual SimpleTextDocsAndPositionsEnum reset(long fp, Bits liveDocs,
                FieldInfo.IndexOptions indexOptions, int docFreq)
            {
                this.liveDocs = liveDocs;
                nextDocStart = fp;
                docID_Renamed = -1;
                readPositions = indexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                readOffsets = indexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;

                if (!readOffsets)
                {
                    startOffset_Renamed = -1;
                    endOffset_Renamed = -1;
                }
                cost_Renamed = docFreq;
                return this;
            }

            public override int DocID()
            {
                return docID_Renamed;
            }

            public override int Freq()
            {
                return tf;
            }

            public override int NextDoc()
            {
                bool first = true;
                @in.Seek(nextDocStart);
                long posStart = 0;
                while (true)
                {
                    long lineStart = @in.FilePointer;
                    SimpleTextUtil.ReadLine(@in, scratch);
                    //System.out.println("NEXT DOC: " + scratch.utf8ToString());
                    if (StringHelper.StartsWith(scratch, DOC))
                    {
                        if (!first && (liveDocs == null || liveDocs.Get(docID_Renamed)))
                        {
                            nextDocStart = lineStart;
                            @in.Seek(posStart);
                            return docID_Renamed;
                        }
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + DOC.length, scratch.Length - DOC.length,
                            scratchUTF16);
                        docID_Renamed = ArrayUtil.ParseInt(scratchUTF16.Chars, 0, scratchUTF16.length);
                        tf = 0;
                        first = false;
                    }
                    else if (StringHelper.StartsWith(scratch, FREQ))
                    {
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + FREQ.length,
                            scratch.Length - FREQ.length, scratchUTF16);
                        tf = ArrayUtil.ParseInt(scratchUTF16.Chars, 0, scratchUTF16.length);
                        posStart = @in.FilePointer;
                    }
                    else if (StringHelper.StartsWith(scratch, POS))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(scratch, START_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(scratch, END_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(scratch, PAYLOAD))
                    {
                        // skip
                    }
                    else
                    {
                        Debug.Assert(StringHelper.StartsWith(scratch, TERM) || StringHelper.StartsWith(scratch, FIELD) ||
                                     StringHelper.StartsWith(scratch, END));

                        if (!first && (liveDocs == null || liveDocs.Get(docID_Renamed)))
                        {
                            nextDocStart = lineStart;
                            @in.Seek(posStart);
                            return docID_Renamed;
                        }
                        return docID_Renamed = NO_MORE_DOCS;
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
                    SimpleTextUtil.ReadLine(@in, scratch);
                    Debug.Assert(StringHelper.StartsWith(scratch, POS), "got line=" + scratch.Utf8ToString());
                    UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.Offset + POS.length, scratch.Length - POS.length,
                        scratchUTF16_2);
                    pos = ArrayUtil.ParseInt(scratchUTF16_2.Chars, 0, scratchUTF16_2.length);
                }
                else
                {
                    pos = -1;
                }

                if (readOffsets)
                {
                    SimpleTextUtil.ReadLine(@in, scratch);
                    Debug.Assert(StringHelper.StartsWith(scratch, START_OFFSET), "got line=" + scratch.Utf8ToString());
                    UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.Offset + START_OFFSET.length,
                        scratch.Length - START_OFFSET.length, scratchUTF16_2);
                    startOffset_Renamed = ArrayUtil.ParseInt(scratchUTF16_2.chars, 0, scratchUTF16_2.length);
                    SimpleTextUtil.ReadLine(@in, scratch);
                    Debug.Assert(StringHelper.StartsWith(scratch, END_OFFSET), "got line=" + scratch.Utf8ToString());
                    UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + END_OFFSET.length,
                        scratch.Length - END_OFFSET.length, scratchUTF16_2);
                    endOffset_Renamed = ArrayUtil.ParseInt(scratchUTF16_2.Chars, 0, scratchUTF16_2.length);
                }

                long fp = @in.FilePointer;
                SimpleTextUtil.ReadLine(@in, scratch);
                if (StringHelper.StartsWith(scratch, PAYLOAD))
                {
                    int len = scratch.Length - PAYLOAD.length;
                    if (scratch2.Bytes.Length < len)
                    {
                        scratch2.Grow(len);
                    }
                    Array.Copy(scratch.Bytes, PAYLOAD.length, scratch2.Bytes, 0, len);
                    scratch2.Length = len;
                    payload = scratch2;
                }
                else
                {
                    payload = null;
                    @in.Seek(fp);
                }
                return pos;
            }

            public override int StartOffset()
            {
                return startOffset_Renamed;
            }

            public override int EndOffset()
            {
                return endOffset_Renamed;
            }

            public override BytesRef Payload
            {
                get { return payload; }
            }

            public override long Cost()
            {
                return cost_Renamed;
            }
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

            internal readonly long termsStart;
            internal readonly FieldInfo fieldInfo;
            internal readonly int maxDoc;
            internal long sumTotalTermFreq;
            internal long sumDocFreq;
            internal int docCount;
            internal FST<PairOutputs.Pair<long?, PairOutputs.Pair<long?, long?>>> fst;
            internal int termCount;
            internal readonly BytesRef scratch = new BytesRef(10);
            internal readonly CharsRef scratchUTF16 = new CharsRef(10);

            public SimpleTextTerms(SimpleTextFieldsReader outerInstance, string field, long termsStart, int maxDoc)
            {
                this.outerInstance = outerInstance;
                this.maxDoc = maxDoc;
                this.termsStart = termsStart;
                fieldInfo = outerInstance.fieldInfos.FieldInfo(field);
                LoadTerms();
            }

            internal virtual void LoadTerms()
            {
                PositiveIntOutputs posIntOutputs = PositiveIntOutputs.Singleton;
                Builder<PairOutputs.Pair<long?, PairOutputs.Pair<long?, long?>>> b;
                PairOutputs<long?, long?> outputsInner = new PairOutputs<long?, long?>(posIntOutputs, posIntOutputs);
                PairOutputs<long?, PairOutputs.Pair<long?, long?>> outputs =
                    new PairOutputs<long?, PairOutputs.Pair<long?, long?>>(posIntOutputs, outputsInner);
                b = new Builder<>(FST.INPUT_TYPE.BYTE1, outputs);
                IndexInput @in = (IndexInput) outerInstance._input.Clone();
                @in.Seek(termsStart);

                BytesRef lastTerm = new BytesRef(10);
                long lastDocsStart = -1;
                int docFreq = 0;
                long totalTermFreq = 0;
                FixedBitSet visitedDocs = new FixedBitSet(maxDoc);

                IntsRef scratchIntsRef = new IntsRef();
                while (true)
                {
                    SimpleTextUtil.ReadLine(@in, scratch);
                    if (scratch.Equals(END) || StringHelper.StartsWith(scratch, FIELD))
                    {
                        if (lastDocsStart != -1)
                        {
                            b.Add(Util.ToIntsRef(lastTerm, scratchIntsRef),
                                outputs.NewPair(lastDocsStart, outputsInner.NewPair((long) docFreq, totalTermFreq)));
                            sumTotalTermFreq += totalTermFreq;
                        }
                        break;
                    }
                    else if (StringHelper.StartsWith(scratch, DOC))
                    {
                        docFreq++;
                        sumDocFreq++;
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + DOC.length, scratch.Length - DOC.length,
                            scratchUTF16);
                        int docID = ArrayUtil.ParseInt(scratchUTF16.Chars, 0, scratchUTF16.length);
                        visitedDocs.Set(docID);
                    }
                    else if (StringHelper.StartsWith(scratch, FREQ))
                    {
                        UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + FREQ.length,
                            scratch.Length - FREQ.length, scratchUTF16);
                        totalTermFreq += ArrayUtil.ParseInt(scratchUTF16.Chars, 0, scratchUTF16.length);
                    }
                    else if (StringHelper.StartsWith(scratch, TERM))
                    {
                        if (lastDocsStart != -1)
                        {
                            b.Add(Util.ToIntsRef(lastTerm, scratchIntsRef),
                                outputs.NewPair(lastDocsStart, outputsInner.NewPair((long) docFreq, totalTermFreq)));
                        }
                        lastDocsStart = @in.FilePointer;
                        int len = scratch.Length - TERM.length;
                        if (len > lastTerm.Length)
                        {
                            lastTerm.Grow(len);
                        }
                        Array.Copy(scratch.Bytes, TERM.length, lastTerm.Bytes, 0, len);
                        lastTerm.Length = len;
                        docFreq = 0;
                        sumTotalTermFreq += totalTermFreq;
                        totalTermFreq = 0;
                        termCount++;
                    }
                }
                docCount = visitedDocs.Cardinality();
                fst = b.Finish();
            
            }

            /// <summary>Returns approximate RAM bytes used</summary>
            public virtual long RamBytesUsed()
            {
                return (fst != null) ? fst.SizeInBytes : 0;
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                return fst != null ? new SimpleTextTermsEnum(outerInstance, fst, fieldInfo.FieldIndexOptions.Value) : TermsEnum.EMPTY;
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }

            public override long Size()
            {
                return termCount;
            }

            public override long SumTotalTermFreq
            {
                get { return fieldInfo.FieldIndexOptions == IndexOptions.DOCS_ONLY ? - 1 : sumTotalTermFreq; }
            }

            public override long SumDocFreq
            {
                get { return sumDocFreq; }
            }

            public override int DocCount
            {
                get { return docCount; }
            }

            public override bool HasFreqs()
            {
                return fieldInfo.FieldIndexOptions >= IndexOptions.DOCS_AND_FREQS;
            }

            public override bool HasOffsets()
            {
                return
                    fieldInfo.FieldIndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            }

            public override bool HasPositions()
            {
                return fieldInfo.FieldIndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            }

            public override bool HasPayloads()
            {
                return fieldInfo.HasPayloads();
            }
        }

        public override IEnumerator<string> Iterator()
        {
        }

        public override Terms Terms(string field)
        {
            lock (this)
            {
                Terms terms = _termsCache[field];
                if (terms == null)
                {
                    long? fp = fields[field];
                    if (fp == null)
                    {
                        return null;
                    }
                    else
                    {
                        terms = new SimpleTextTerms(this, field, fp.Value, maxDoc);
                        _termsCache[field] = (SimpleTextTerms) terms;
                    }
                }
                return terms;
            }
        }

        public override int Size()
        {
            return -1;
        }

        public override void Dispose()
        {
            _input.Dispose();
        }

        public override long RamBytesUsed()
        {
            return _termsCache.Values.Sum(simpleTextTerms => (simpleTextTerms != null) ? simpleTextTerms.RamBytesUsed() : 0);
        }

        public override void CheckIntegrity()
        {
        }
    }

}