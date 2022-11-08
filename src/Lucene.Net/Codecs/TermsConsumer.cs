using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs
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
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using FieldInfo = Lucene.Net.Index.FieldInfo; // javadocs
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using MergeState = Lucene.Net.Index.MergeState;
    using MultiDocsAndPositionsEnum = Lucene.Net.Index.MultiDocsAndPositionsEnum;
    using MultiDocsEnum = Lucene.Net.Index.MultiDocsEnum;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Abstract API that consumes terms for an individual field.
    /// <para/>
    /// The lifecycle is:
    /// <list type="number">
    ///   <item><description>TermsConsumer is returned for each field
    ///       by <see cref="FieldsConsumer.AddField(FieldInfo)"/>.</description></item>
    ///   <item><description>TermsConsumer returns a <see cref="PostingsConsumer"/> for
    ///       each term in <see cref="StartTerm(BytesRef)"/>.</description></item>
    ///   <item><description>When the producer (e.g. IndexWriter)
    ///       is done adding documents for the term, it calls
    ///       <see cref="FinishTerm(BytesRef, TermStats)"/>, passing in
    ///       the accumulated term statistics.</description></item>
    ///   <item><description>Producer calls <see cref="Finish(long, long, int)"/> with
    ///       the accumulated collection statistics when it is finished
    ///       adding terms to the field.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class TermsConsumer
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected TermsConsumer()
        {
        }

        /// <summary>
        /// Starts a new term in this field; this may be called
        /// with no corresponding call to finish if the term had
        /// no docs.
        /// </summary>
        public abstract PostingsConsumer StartTerm(BytesRef text);

        /// <summary>
        /// Finishes the current term; numDocs must be &gt; 0.
        /// <c>stats.TotalTermFreq</c> will be -1 when term
        /// frequencies are omitted for the field.
        /// </summary>
        public abstract void FinishTerm(BytesRef text, TermStats stats);

        /// <summary>
        /// Called when we are done adding terms to this field.
        /// <paramref name="sumTotalTermFreq"/> will be -1 when term
        /// frequencies are omitted for the field.
        /// </summary>
        public abstract void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount);

        /// <summary>
        /// Gets the <see cref="T:IComparer{BytesRef}"/> used to sort terms
        /// before feeding to this API.
        /// </summary>
        public abstract IComparer<BytesRef> Comparer { get; }

        private MappingMultiDocsEnum docsEnum;
        private MappingMultiDocsEnum docsAndFreqsEnum;
        private MappingMultiDocsAndPositionsEnum postingsEnum;

        /// <summary>
        /// Default merge impl. </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void Merge(MergeState mergeState, IndexOptions indexOptions, TermsEnum termsEnum)
        {
            BytesRef term;
            if (Debugging.AssertsEnabled) Debugging.Assert(termsEnum != null);
            long sumTotalTermFreq = 0;
            long sumDocFreq = 0;
            long sumDFsinceLastAbortCheck = 0;
            FixedBitSet visitedDocs = new FixedBitSet(mergeState.SegmentInfo.DocCount);

            if (indexOptions == IndexOptions.DOCS_ONLY)
            {
                if (docsEnum is null)
                {
                    docsEnum = new MappingMultiDocsEnum();
                }
                docsEnum.MergeState = mergeState;

                MultiDocsEnum docsEnumIn = null;

                while (termsEnum.MoveNext())
                {
                    term = termsEnum.Term;
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    docsEnumIn = (MultiDocsEnum)termsEnum.Docs(null, docsEnumIn, DocsFlags.NONE);
                    if (docsEnumIn != null)
                    {
                        docsEnum.Reset(docsEnumIn);
                        PostingsConsumer postingsConsumer = StartTerm(term);
                        TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, docsEnum, visitedDocs);
                        if (stats.DocFreq > 0)
                        {
                            FinishTerm(term, stats);
                            sumTotalTermFreq += stats.DocFreq;
                            sumDFsinceLastAbortCheck += stats.DocFreq;
                            sumDocFreq += stats.DocFreq;
                            if (sumDFsinceLastAbortCheck > 60000)
                            {
                                mergeState.CheckAbort.Work(sumDFsinceLastAbortCheck / 5.0);
                                sumDFsinceLastAbortCheck = 0;
                            }
                        }
                    }
                }
            }
            else if (indexOptions == IndexOptions.DOCS_AND_FREQS)
            {
                if (docsAndFreqsEnum is null)
                {
                    docsAndFreqsEnum = new MappingMultiDocsEnum();
                }
                docsAndFreqsEnum.MergeState = mergeState;

                MultiDocsEnum docsAndFreqsEnumIn = null;

                while (termsEnum.MoveNext())
                {
                    term = termsEnum.Term;
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    docsAndFreqsEnumIn = (MultiDocsEnum)termsEnum.Docs(null, docsAndFreqsEnumIn);
                    if (Debugging.AssertsEnabled) Debugging.Assert(docsAndFreqsEnumIn != null);
                    docsAndFreqsEnum.Reset(docsAndFreqsEnumIn);
                    PostingsConsumer postingsConsumer = StartTerm(term);
                    TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, docsAndFreqsEnum, visitedDocs);
                    if (stats.DocFreq > 0)
                    {
                        FinishTerm(term, stats);
                        sumTotalTermFreq += stats.TotalTermFreq;
                        sumDFsinceLastAbortCheck += stats.DocFreq;
                        sumDocFreq += stats.DocFreq;
                        if (sumDFsinceLastAbortCheck > 60000)
                        {
                            mergeState.CheckAbort.Work(sumDFsinceLastAbortCheck / 5.0);
                            sumDFsinceLastAbortCheck = 0;
                        }
                    }
                }
            }
            else if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                if (postingsEnum is null)
                {
                    postingsEnum = new MappingMultiDocsAndPositionsEnum();
                }
                postingsEnum.MergeState = mergeState;
                MultiDocsAndPositionsEnum postingsEnumIn = null;
                while (termsEnum.MoveNext())
                {
                    term = termsEnum.Term;
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    postingsEnumIn = (MultiDocsAndPositionsEnum)termsEnum.DocsAndPositions(null, postingsEnumIn, DocsAndPositionsFlags.PAYLOADS);
                    if (Debugging.AssertsEnabled) Debugging.Assert(postingsEnumIn != null);
                    postingsEnum.Reset(postingsEnumIn);

                    PostingsConsumer postingsConsumer = StartTerm(term);
                    TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, postingsEnum, visitedDocs);
                    if (stats.DocFreq > 0)
                    {
                        FinishTerm(term, stats);
                        sumTotalTermFreq += stats.TotalTermFreq;
                        sumDFsinceLastAbortCheck += stats.DocFreq;
                        sumDocFreq += stats.DocFreq;
                        if (sumDFsinceLastAbortCheck > 60000)
                        {
                            mergeState.CheckAbort.Work(sumDFsinceLastAbortCheck / 5.0);
                            sumDFsinceLastAbortCheck = 0;
                        }
                    }
                }
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
                if (postingsEnum is null)
                {
                    postingsEnum = new MappingMultiDocsAndPositionsEnum();
                }
                postingsEnum.MergeState = mergeState;
                MultiDocsAndPositionsEnum postingsEnumIn = null;
                while (termsEnum.MoveNext())
                {
                    term = termsEnum.Term;
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    postingsEnumIn = (MultiDocsAndPositionsEnum)termsEnum.DocsAndPositions(null, postingsEnumIn);
                    if (Debugging.AssertsEnabled) Debugging.Assert(postingsEnumIn != null);
                    postingsEnum.Reset(postingsEnumIn);

                    PostingsConsumer postingsConsumer = StartTerm(term);
                    TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, postingsEnum, visitedDocs);
                    if (stats.DocFreq > 0)
                    {
                        FinishTerm(term, stats);
                        sumTotalTermFreq += stats.TotalTermFreq;
                        sumDFsinceLastAbortCheck += stats.DocFreq;
                        sumDocFreq += stats.DocFreq;
                        if (sumDFsinceLastAbortCheck > 60000)
                        {
                            mergeState.CheckAbort.Work(sumDFsinceLastAbortCheck / 5.0);
                            sumDFsinceLastAbortCheck = 0;
                        }
                    }
                }
            }
            Finish(indexOptions == IndexOptions.DOCS_ONLY ? -1 : sumTotalTermFreq, sumDocFreq, visitedDocs.Cardinality);
        }
    }
}