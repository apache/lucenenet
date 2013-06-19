using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;

namespace Lucene.Net.Codecs
{
    public abstract class PostingsConsumer
    {
        protected PostingsConsumer()
        {
        }

        public abstract void StartDoc(int docID, int freq);

        public abstract void AddPosition(int position, BytesRef payload, int startOffset, int endOffset);

        public abstract void FinishDoc();

        public TermStats Merge(MergeState mergeState, IndexOptions indexOptions, DocsEnum postings, FixedBitSet visitedDocs)
        {

            int df = 0;
            long totTF = 0;

            if (indexOptions == IndexOptions.DOCS_ONLY)
            {
                while (true)
                {
                    int doc = postings.NextDoc();
                    if (doc == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        break;
                    }
                    visitedDocs.Set(doc);
                    this.StartDoc(doc, -1);
                    this.FinishDoc();
                    df++;
                }
                totTF = -1;
            }
            else if (indexOptions == IndexOptions.DOCS_AND_FREQS)
            {
                while (true)
                {
                    int doc = postings.NextDoc();
                    if (doc == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        break;
                    }
                    visitedDocs.Set(doc);
                    int freq = postings.Freq;
                    this.StartDoc(doc, freq);
                    this.FinishDoc();
                    df++;
                    totTF += freq;
                }
            }
            else if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                DocsAndPositionsEnum postingsEnum = (DocsAndPositionsEnum)postings;
                while (true)
                {
                    int doc = postingsEnum.NextDoc();
                    if (doc == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        break;
                    }
                    visitedDocs.Set(doc);
                    int freq = postingsEnum.Freq;
                    this.StartDoc(doc, freq);
                    totTF += freq;
                    for (int i = 0; i < freq; i++)
                    {
                        int position = postingsEnum.NextPosition();
                        BytesRef payload = postingsEnum.Payload;
                        this.AddPosition(position, payload, -1, -1);
                    }
                    this.FinishDoc();
                    df++;
                }
            }
            else
            {
                //assert indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                DocsAndPositionsEnum postingsEnum = (DocsAndPositionsEnum)postings;
                while (true)
                {
                    int doc = postingsEnum.NextDoc();
                    if (doc == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        break;
                    }
                    visitedDocs.Set(doc);
                    int freq = postingsEnum.Freq;
                    this.StartDoc(doc, freq);
                    totTF += freq;
                    for (int i = 0; i < freq; i++)
                    {
                        int position = postingsEnum.NextPosition();
                        BytesRef payload = postingsEnum.Payload;
                        this.AddPosition(position, payload, postingsEnum.StartOffset, postingsEnum.EndOffset);
                    }
                    this.FinishDoc();
                    df++;
                }
            }
            return new TermStats(df, indexOptions == IndexOptions.DOCS_ONLY ? -1 : totTF);
        }
    }
}
