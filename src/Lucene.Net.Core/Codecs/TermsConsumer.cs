using System.Collections.Generic;
using System.Diagnostics;

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
    using FieldInfo = Lucene.Net.Index.FieldInfo; // javadocs
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using MergeState = Lucene.Net.Index.MergeState;
    using MultiDocsAndPositionsEnum = Lucene.Net.Index.MultiDocsAndPositionsEnum;
    using MultiDocsEnum = Lucene.Net.Index.MultiDocsEnum;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Abstract API that consumes terms for an individual field.
    /// <p>
    /// The lifecycle is:
    /// <ol>
    ///   <li>TermsConsumer is returned for each field
    ///       by <seealso cref="FieldsConsumer#addField(FieldInfo)"/>.
    ///   <li>TermsConsumer returns a <seealso cref="PostingsConsumer"/> for
    ///       each term in <seealso cref="#startTerm(BytesRef)"/>.
    ///   <li>When the producer (e.g. IndexWriter)
    ///       is done adding documents for the term, it calls
    ///       <seealso cref="#finishTerm(BytesRef, TermStats)"/>, passing in
    ///       the accumulated term statistics.
    ///   <li>Producer calls <seealso cref="#finish(long, long, int)"/> with
    ///       the accumulated collection statistics when it is finished
    ///       adding terms to the field.
    /// </ol>
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class TermsConsumer
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected internal TermsConsumer()
        {
        }

        /// <summary>
        /// Starts a new term in this field; this may be called
        ///  with no corresponding call to finish if the term had
        ///  no docs.
        /// </summary>
        public abstract PostingsConsumer StartTerm(BytesRef text);

        /// <summary>
        /// Finishes the current term; numDocs must be > 0.
        ///  <code>stats.totalTermFreq</code> will be -1 when term
        ///  frequencies are omitted for the field.
        /// </summary>
        public abstract void FinishTerm(BytesRef text, TermStats stats);

        /// <summary>
        /// Called when we are done adding terms to this field.
        ///  <code>sumTotalTermFreq</code> will be -1 when term
        ///  frequencies are omitted for the field.
        /// </summary>
        public abstract void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount);

        /// <summary>
        /// Return the BytesRef Comparator used to sort terms
        ///  before feeding to this API.
        /// </summary>
        public abstract IComparer<BytesRef> Comparator { get; } // LUCENENET TODO: Rename Comparer

        private MappingMultiDocsEnum DocsEnum;
        private MappingMultiDocsEnum DocsAndFreqsEnum;
        private MappingMultiDocsAndPositionsEnum PostingsEnum;

        /// <summary>
        /// Default merge impl </summary>
        public virtual void Merge(MergeState mergeState, FieldInfo.IndexOptions? indexOptions, TermsEnum termsEnum)
        {
            BytesRef term;
            Debug.Assert(termsEnum != null);
            long sumTotalTermFreq = 0;
            long sumDocFreq = 0;
            long sumDFsinceLastAbortCheck = 0;
            FixedBitSet visitedDocs = new FixedBitSet(mergeState.SegmentInfo.DocCount);

            if (indexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
            {
                if (DocsEnum == null)
                {
                    DocsEnum = new MappingMultiDocsEnum();
                }
                DocsEnum.MergeState = mergeState;

                MultiDocsEnum docsEnumIn = null;

                while ((term = termsEnum.Next()) != null)
                {
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    docsEnumIn = (MultiDocsEnum)termsEnum.Docs(null, docsEnumIn, Index.DocsEnum.FLAG_NONE);
                    if (docsEnumIn != null)
                    {
                        DocsEnum.Reset(docsEnumIn);
                        PostingsConsumer postingsConsumer = StartTerm(term);
                        TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, DocsEnum, visitedDocs);
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
            else if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS)
            {
                if (DocsAndFreqsEnum == null)
                {
                    DocsAndFreqsEnum = new MappingMultiDocsEnum();
                }
                DocsAndFreqsEnum.MergeState = mergeState;

                MultiDocsEnum docsAndFreqsEnumIn = null;

                while ((term = termsEnum.Next()) != null)
                {
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    docsAndFreqsEnumIn = (MultiDocsEnum)termsEnum.Docs(null, docsAndFreqsEnumIn);
                    Debug.Assert(docsAndFreqsEnumIn != null);
                    DocsAndFreqsEnum.Reset(docsAndFreqsEnumIn);
                    PostingsConsumer postingsConsumer = StartTerm(term);
                    TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, DocsAndFreqsEnum, visitedDocs);
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
            else if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                if (PostingsEnum == null)
                {
                    PostingsEnum = new MappingMultiDocsAndPositionsEnum();
                }
                PostingsEnum.MergeState = mergeState;
                MultiDocsAndPositionsEnum postingsEnumIn = null;
                while ((term = termsEnum.Next()) != null)
                {
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    postingsEnumIn = (MultiDocsAndPositionsEnum)termsEnum.DocsAndPositions(null, postingsEnumIn, DocsAndPositionsEnum.FLAG_PAYLOADS);
                    Debug.Assert(postingsEnumIn != null);
                    PostingsEnum.Reset(postingsEnumIn);

                    PostingsConsumer postingsConsumer = StartTerm(term);
                    TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, PostingsEnum, visitedDocs);
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
                Debug.Assert(indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
                if (PostingsEnum == null)
                {
                    PostingsEnum = new MappingMultiDocsAndPositionsEnum();
                }
                PostingsEnum.MergeState = mergeState;
                MultiDocsAndPositionsEnum postingsEnumIn = null;
                while ((term = termsEnum.Next()) != null)
                {
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    postingsEnumIn = (MultiDocsAndPositionsEnum)termsEnum.DocsAndPositions(null, postingsEnumIn);
                    Debug.Assert(postingsEnumIn != null);
                    PostingsEnum.Reset(postingsEnumIn);

                    PostingsConsumer postingsConsumer = StartTerm(term);
                    TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, PostingsEnum, visitedDocs);
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
            Finish(indexOptions == FieldInfo.IndexOptions.DOCS_ONLY ? -1 : sumTotalTermFreq, sumDocFreq, visitedDocs.Cardinality());
        }
    }
}