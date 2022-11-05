using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using System.Diagnostics;
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
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using MergeState = Lucene.Net.Index.MergeState;

    /// <summary>
    /// Abstract API that consumes postings for an individual term.
    /// <para/>
    /// The lifecycle is:
    /// <list type="number">
    ///    <item><description>PostingsConsumer is returned for each term by
    ///        <see cref="TermsConsumer.StartTerm(BytesRef)"/>.</description></item>
    ///    <item><description><see cref="StartDoc(int, int)"/> is called for each
    ///        document where the term occurs, specifying id
    ///        and term frequency for that document.</description></item>
    ///    <item><description>If positions are enabled for the field, then
    ///        <see cref="AddPosition(int, BytesRef, int, int)"/>
    ///        will be called for each occurrence in the
    ///        document.</description></item>
    ///    <item><description><see cref="FinishDoc()"/> is called when the producer
    ///        is done adding positions to the document.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class PostingsConsumer
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected PostingsConsumer()
        {
        }

        /// <summary>
        /// Adds a new doc in this term.
        /// <paramref name="freq"/> will be -1 when term frequencies are omitted
        /// for the field.
        /// </summary>
        public abstract void StartDoc(int docId, int freq);

        /// <summary>
        /// Add a new position &amp; payload, and start/end offset.  A
        /// <c>null</c> <paramref name="payload"/> means no payload; a non-<c>null</c> <paramref name="payload"/> with
        /// zero length also means no payload.  Caller may reuse
        /// the <see cref="BytesRef"/> for the <paramref name="payload"/> between calls
        /// (method must fully consume the payload). <paramref name="startOffset"/>
        /// and <paramref name="endOffset"/> will be -1 when offsets are not indexed.
        /// </summary>
        public abstract void AddPosition(int position, BytesRef payload, int startOffset, int endOffset);

        /// <summary>
        /// Called when we are done adding positions &amp; payloads
        /// for each doc.
        /// </summary>
        public abstract void FinishDoc();

        /// <summary>
        /// Default merge impl: append documents, mapping around
        /// deletes.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual TermStats Merge(MergeState mergeState, IndexOptions indexOptions, DocsEnum postings, FixedBitSet visitedDocs)
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
                var postingsEnum = (DocsAndPositionsEnum)postings;
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
                        BytesRef payload = postingsEnum.GetPayload();
                        this.AddPosition(position, payload, -1, -1);
                    }
                    this.FinishDoc();
                    df++;
                }
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
                var postingsEnum = (DocsAndPositionsEnum)postings;
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
                        BytesRef payload = postingsEnum.GetPayload();
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