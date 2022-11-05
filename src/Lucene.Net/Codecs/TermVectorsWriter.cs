using J2N.Numerics;
using J2N.Text;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DataInput = Lucene.Net.Store.DataInput;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using Fields = Lucene.Net.Index.Fields;
    using MergeState = Lucene.Net.Index.MergeState;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Codec API for writing term vectors:
    /// <para/>
    /// <list type="number">
    ///   <item><description>For every document, <see cref="StartDocument(int)"/> is called,
    ///       informing the <see cref="Codec"/> how many fields will be written.</description></item>
    ///   <item><description><see cref="StartField(FieldInfo, int, bool, bool, bool)"/> is called for
    ///       each field in the document, informing the codec how many terms
    ///       will be written for that field, and whether or not positions,
    ///       offsets, or payloads are enabled.</description></item>
    ///   <item><description>Within each field, <see cref="StartTerm(BytesRef, int)"/> is called
    ///       for each term.</description></item>
    ///   <item><description>If offsets and/or positions are enabled, then
    ///       <see cref="AddPosition(int, int, int, BytesRef)"/> will be called for each term
    ///       occurrence.</description></item>
    ///   <item><description>After all documents have been written, <see cref="Finish(FieldInfos, int)"/>
    ///       is called for verification/sanity-checks.</description></item>
    ///   <item><description>Finally the writer is disposed (<see cref="Dispose(bool)"/>)</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class TermVectorsWriter : IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected TermVectorsWriter()
        {
        }

        /// <summary>
        /// Called before writing the term vectors of the document.
        /// <see cref="StartField(FieldInfo, int, bool, bool, bool)"/> will
        /// be called <paramref name="numVectorFields"/> times. Note that if term
        /// vectors are enabled, this is called even if the document
        /// has no vector fields, in this case <paramref name="numVectorFields"/>
        /// will be zero.
        /// </summary>
        public abstract void StartDocument(int numVectorFields);

        /// <summary>
        /// Called after a doc and all its fields have been added. </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void FinishDocument()
        {
        }

        /// <summary>
        /// Called before writing the terms of the field.
        /// <see cref="StartTerm(BytesRef, int)"/> will be called <paramref name="numTerms"/> times.
        /// </summary>
        public abstract void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads);

        /// <summary>
        /// Called after a field and all its terms have been added. </summary>
        public virtual void FinishField()
        {
        }

        /// <summary>
        /// Adds a <paramref name="term"/> and its term frequency <paramref name="freq"/>.
        /// If this field has positions and/or offsets enabled, then
        /// <see cref="AddPosition(int, int, int, BytesRef)"/> will be called
        /// <paramref name="freq"/> times respectively.
        /// </summary>
        public abstract void StartTerm(BytesRef term, int freq);

        /// <summary>
        /// Called after a term and all its positions have been added. </summary>
        public virtual void FinishTerm()
        {
        }

        /// <summary>
        /// Adds a term <paramref name="position"/> and offsets. </summary>
        public abstract void AddPosition(int position, int startOffset, int endOffset, BytesRef payload);

        /// <summary>
        /// Aborts writing entirely, implementation should remove
        /// any partially-written files, etc.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public abstract void Abort();

        /// <summary>
        /// Called before <see cref="Dispose(bool)"/>, passing in the number
        /// of documents that were written. Note that this is
        /// intentionally redundant (equivalent to the number of
        /// calls to <see cref="StartDocument(int)"/>, but a <see cref="Codec"/> should
        /// check that this is the case to detect the bug described
        /// in LUCENE-1282.
        /// </summary>
        public abstract void Finish(FieldInfos fis, int numDocs);

        /// <summary>
        /// Called by <see cref="Index.IndexWriter"/> when writing new segments.
        /// <para/>
        /// This is an expert API that allows the codec to consume
        /// positions and offsets directly from the indexer.
        /// <para/>
        /// The default implementation calls <see cref="AddPosition(int, int, int, BytesRef)"/>,
        /// but subclasses can override this if they want to efficiently write
        /// all the positions, then all the offsets, for example.
        /// <para/>
        /// NOTE: this API is extremely expert and subject to change or removal!!!
        /// <para/>
        /// @lucene.internal
        /// </summary>
        // TODO: we should probably nuke this and make a more efficient 4.x format
        // PreFlex-RW could then be slow and buffer (its only used in tests...)
        public virtual void AddProx(int numProx, DataInput positions, DataInput offsets)
        {
            int position = 0;
            int lastOffset = 0;
            BytesRef payload = null;

            for (int i = 0; i < numProx; i++)
            {
                int startOffset;
                int endOffset;
                BytesRef thisPayload;

                if (positions is null)
                {
                    position = -1;
                    thisPayload = null;
                }
                else
                {
                    int code = positions.ReadVInt32();
                    position += code.TripleShift(1);
                    if ((code & 1) != 0)
                    {
                        // this position has a payload
                        int payloadLength = positions.ReadVInt32();

                        if (payload is null)
                        {
                            payload = new BytesRef();
                            payload.Bytes = new byte[payloadLength];
                        }
                        else if (payload.Bytes.Length < payloadLength)
                        {
                            payload.Grow(payloadLength);
                        }

                        positions.ReadBytes(payload.Bytes, 0, payloadLength);
                        payload.Length = payloadLength;
                        thisPayload = payload;
                    }
                    else
                    {
                        thisPayload = null;
                    }
                }

                if (offsets is null)
                {
                    startOffset = endOffset = -1;
                }
                else
                {
                    startOffset = lastOffset + offsets.ReadVInt32();
                    endOffset = startOffset + offsets.ReadVInt32();
                    lastOffset = endOffset;
                }
                AddPosition(position, startOffset, endOffset, thisPayload);
            }
        }

        /// <summary>
        /// Merges in the term vectors from the readers in
        /// <paramref name="mergeState"/>. The default implementation skips
        /// over deleted documents, and uses <see cref="StartDocument(int)"/>,
        /// <see cref="StartField(FieldInfo, int, bool, bool, bool)"/>,
        /// <see cref="StartTerm(BytesRef, int)"/>, <see cref="AddPosition(int, int, int, BytesRef)"/>,
        /// and <see cref="Finish(FieldInfos, int)"/>,
        /// returning the number of documents that were written.
        /// Implementations can override this method for more sophisticated
        /// merging (bulk-byte copying, etc).
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual int Merge(MergeState mergeState)
        {
            int docCount = 0;
            for (int i = 0; i < mergeState.Readers.Count; i++)
            {
                AtomicReader reader = mergeState.Readers[i];
                int maxDoc = reader.MaxDoc;
                IBits liveDocs = reader.LiveDocs;

                for (int docID = 0; docID < maxDoc; docID++)
                {
                    if (liveDocs != null && !liveDocs.Get(docID))
                    {
                        // skip deleted docs
                        continue;
                    }
                    // NOTE: it's very important to first assign to vectors then pass it to
                    // termVectorsWriter.addAllDocVectors; see LUCENE-1282
                    Fields vectors = reader.GetTermVectors(docID);
                    AddAllDocVectors(vectors, mergeState);
                    docCount++;
                    mergeState.CheckAbort.Work(300);
                }
            }
            Finish(mergeState.FieldInfos, docCount);
            return docCount;
        }

        /// <summary>
        /// Safe (but, slowish) default method to write every
        /// vector field in the document.
        /// </summary>
        protected void AddAllDocVectors(Fields vectors, MergeState mergeState)
        {
            if (vectors is null)
            {
                StartDocument(0);
                FinishDocument();
                return;
            }

            int numFields = vectors.Count;
            if (numFields == -1)
            {
                // count manually! TODO: Maybe enforce that Fields.size() returns something valid?
                numFields = 0;
                //for (IEnumerator<string> it = vectors.Iterator(); it.hasNext();)
                foreach (string it in vectors)
                {
                    numFields++;
                }
            }
            StartDocument(numFields);

            string lastFieldName = null;

            TermsEnum termsEnum = null;
            DocsAndPositionsEnum docsAndPositionsEnum = null;

            int fieldCount = 0;
            foreach (string fieldName in vectors)
            {
                fieldCount++;
                FieldInfo fieldInfo = mergeState.FieldInfos.FieldInfo(fieldName);

                if (Debugging.AssertsEnabled) Debugging.Assert(lastFieldName is null || fieldName.CompareToOrdinal(lastFieldName) > 0, "lastFieldName={0} fieldName={1}", lastFieldName, fieldName);
                lastFieldName = fieldName;

                Terms terms = vectors.GetTerms(fieldName);
                if (terms is null)
                {
                    // FieldsEnum shouldn't lie...
                    continue;
                }

                bool hasPositions = terms.HasPositions;
                bool hasOffsets = terms.HasOffsets;
                bool hasPayloads = terms.HasPayloads;
                if (Debugging.AssertsEnabled) Debugging.Assert(!hasPayloads || hasPositions);

                int numTerms = (int)terms.Count;
                if (numTerms == -1)
                {
                    // count manually. It is stupid, but needed, as Terms.size() is not a mandatory statistics function
                    numTerms = 0;
                    termsEnum = terms.GetEnumerator(termsEnum);
                    while (termsEnum.MoveNext())
                    {
                        numTerms++;
                    }
                }

                StartField(fieldInfo, numTerms, hasPositions, hasOffsets, hasPayloads);
                termsEnum = terms.GetEnumerator(termsEnum);

                int termCount = 0;
                while (termsEnum.MoveNext())
                {
                    termCount++;

                    int freq = (int)termsEnum.TotalTermFreq;

                    StartTerm(termsEnum.Term, freq);

                    if (hasPositions || hasOffsets)
                    {
                        docsAndPositionsEnum = termsEnum.DocsAndPositions(null, docsAndPositionsEnum);
                        if (Debugging.AssertsEnabled) Debugging.Assert(docsAndPositionsEnum != null);

                        int docID = docsAndPositionsEnum.NextDoc();
                        if (Debugging.AssertsEnabled)
                        {
                            Debugging.Assert(docID != DocIdSetIterator.NO_MORE_DOCS);
                            Debugging.Assert(docsAndPositionsEnum.Freq == freq);
                        }

                        for (int posUpto = 0; posUpto < freq; posUpto++)
                        {
                            int pos = docsAndPositionsEnum.NextPosition();
                            int startOffset = docsAndPositionsEnum.StartOffset;
                            int endOffset = docsAndPositionsEnum.EndOffset;

                            BytesRef payload = docsAndPositionsEnum.GetPayload();

                            if (Debugging.AssertsEnabled) Debugging.Assert(!hasPositions || pos >= 0);
                            AddPosition(pos, startOffset, endOffset, payload);
                        }
                    }
                    FinishTerm();
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(termCount == numTerms);
                FinishField();
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(fieldCount == numFields);
            FinishDocument();
        }

        /// <summary>
        /// Return the <see cref="T:IComparer{BytesRef}"/> used to sort terms
        /// before feeding to this API.
        /// </summary>
        public abstract IComparer<BytesRef> Comparer { get; }

        /// <summary>
        /// Disposes all resources used by this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implementations must override and should dispose all resources used by this instance.
        /// </summary>
        protected abstract void Dispose(bool disposing);
    }
}