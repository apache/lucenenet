using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class TermsConsumer
    {
        protected TermsConsumer()
        {
        }

        public abstract PostingsConsumer StartTerm(BytesRef text);

        public abstract void FinishTerm(BytesRef text, TermStats stats);

        public abstract void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount);

        public abstract IComparer<BytesRef> Comparator { get; }

        private MappingMultiDocsEnum docsEnum;
        private MappingMultiDocsEnum docsAndFreqsEnum;
        private MappingMultiDocsAndPositionsEnum postingsEnum;

        public virtual void Merge(MergeState mergeState, FieldInfo.IndexOptions indexOptions, TermsEnum termsEnum)
        {

            BytesRef term;
            //assert termsEnum != null;
            long sumTotalTermFreq = 0;
            long sumDocFreq = 0;
            long sumDFsinceLastAbortCheck = 0;
            FixedBitSet visitedDocs = new FixedBitSet(mergeState.segmentInfo.DocCount);

            if (indexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
            {
                if (docsEnum == null)
                {
                    docsEnum = new MappingMultiDocsEnum();
                }
                docsEnum.MergeState = mergeState;

                MultiDocsEnum docsEnumIn = null;

                while ((term = termsEnum.Next()) != null)
                {
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    docsEnumIn = (MultiDocsEnum)termsEnum.Docs(null, docsEnumIn, DocsEnum.FLAG_NONE);
                    if (docsEnumIn != null)
                    {
                        docsEnum.Reset(docsEnumIn);
                        PostingsConsumer postingsConsumer = StartTerm(term);
                        TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, docsEnum, visitedDocs);
                        if (stats.docFreq > 0)
                        {
                            FinishTerm(term, stats);
                            sumTotalTermFreq += stats.docFreq;
                            sumDFsinceLastAbortCheck += stats.docFreq;
                            sumDocFreq += stats.docFreq;
                            if (sumDFsinceLastAbortCheck > 60000)
                            {
                                mergeState.checkAbort.Work(sumDFsinceLastAbortCheck / 5.0);
                                sumDFsinceLastAbortCheck = 0;
                            }
                        }
                    }
                }
            }
            else if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS)
            {
                if (docsAndFreqsEnum == null)
                {
                    docsAndFreqsEnum = new MappingMultiDocsEnum();
                }
                docsAndFreqsEnum.MergeState = mergeState;

                MultiDocsEnum docsAndFreqsEnumIn = null;

                while ((term = termsEnum.Next()) != null)
                {
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    docsAndFreqsEnumIn = (MultiDocsEnum)termsEnum.Docs(null, docsAndFreqsEnumIn);
                    //assert docsAndFreqsEnumIn != null;
                    docsAndFreqsEnum.Reset(docsAndFreqsEnumIn);
                    PostingsConsumer postingsConsumer = StartTerm(term);
                    TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, docsAndFreqsEnum, visitedDocs);
                    if (stats.docFreq > 0)
                    {
                        FinishTerm(term, stats);
                        sumTotalTermFreq += stats.totalTermFreq;
                        sumDFsinceLastAbortCheck += stats.docFreq;
                        sumDocFreq += stats.docFreq;
                        if (sumDFsinceLastAbortCheck > 60000)
                        {
                            mergeState.checkAbort.Work(sumDFsinceLastAbortCheck / 5.0);
                            sumDFsinceLastAbortCheck = 0;
                        }
                    }
                }
            }
            else if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                if (postingsEnum == null)
                {
                    postingsEnum = new MappingMultiDocsAndPositionsEnum();
                }
                postingsEnum.MergeState = mergeState;
                MultiDocsAndPositionsEnum postingsEnumIn = null;
                while ((term = termsEnum.Next()) != null)
                {
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    postingsEnumIn = (MultiDocsAndPositionsEnum)termsEnum.DocsAndPositions(null, postingsEnumIn, DocsAndPositionsEnum.FLAG_PAYLOADS);
                    //assert postingsEnumIn != null;
                    postingsEnum.Reset(postingsEnumIn);

                    PostingsConsumer postingsConsumer = StartTerm(term);
                    TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, postingsEnum, visitedDocs);
                    if (stats.docFreq > 0)
                    {
                        FinishTerm(term, stats);
                        sumTotalTermFreq += stats.totalTermFreq;
                        sumDFsinceLastAbortCheck += stats.docFreq;
                        sumDocFreq += stats.docFreq;
                        if (sumDFsinceLastAbortCheck > 60000)
                        {
                            mergeState.checkAbort.Work(sumDFsinceLastAbortCheck / 5.0);
                            sumDFsinceLastAbortCheck = 0;
                        }
                    }
                }
            }
            else
            {
                //assert indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                if (postingsEnum == null)
                {
                    postingsEnum = new MappingMultiDocsAndPositionsEnum();
                }
                postingsEnum.MergeState = mergeState;
                MultiDocsAndPositionsEnum postingsEnumIn = null;
                while ((term = termsEnum.Next()) != null)
                {
                    // We can pass null for liveDocs, because the
                    // mapping enum will skip the non-live docs:
                    postingsEnumIn = (MultiDocsAndPositionsEnum)termsEnum.DocsAndPositions(null, postingsEnumIn);
                    //assert postingsEnumIn != null;
                    postingsEnum.Reset(postingsEnumIn);

                    PostingsConsumer postingsConsumer = StartTerm(term);
                    TermStats stats = postingsConsumer.Merge(mergeState, indexOptions, postingsEnum, visitedDocs);
                    if (stats.docFreq > 0)
                    {
                        FinishTerm(term, stats);
                        sumTotalTermFreq += stats.totalTermFreq;
                        sumDFsinceLastAbortCheck += stats.docFreq;
                        sumDocFreq += stats.docFreq;
                        if (sumDFsinceLastAbortCheck > 60000)
                        {
                            mergeState.checkAbort.Work(sumDFsinceLastAbortCheck / 5.0);
                            sumDFsinceLastAbortCheck = 0;
                        }
                    }
                }
            }

            Finish(indexOptions == FieldInfo.IndexOptions.DOCS_ONLY ? -1L : sumTotalTermFreq, sumDocFreq, visitedDocs.Cardinality());
        }
    }
}
