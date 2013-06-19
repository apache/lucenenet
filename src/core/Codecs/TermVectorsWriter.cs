using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class TermVectorsWriter : IDisposable
    {
        protected TermVectorsWriter()
        {
        }

        public abstract void StartDocument(int numVectorFields);

        public virtual void FinishDocument()
        {
        }

        public abstract void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads);

        public virtual void FinishField()
        {
        }

        public abstract void StartTerm(BytesRef term, int freq);

        public virtual void FinishTerm()
        {
        }

        public abstract void AddPosition(int position, int startOffset, int endOffset, BytesRef payload);

        public abstract void Abort();

        public abstract void Finish(FieldInfos fis, int numDocs);

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

                if (positions == null)
                {
                    position = -1;
                    thisPayload = null;
                }
                else
                {
                    int code = positions.ReadVInt();
                    position += Number.URShift(code, 1);
                    if ((code & 1) != 0)
                    {
                        // This position has a payload
                        int payloadLength = positions.ReadVInt();

                        if (payload == null)
                        {
                            payload = new BytesRef();
                            payload.bytes = new sbyte[payloadLength];
                        }
                        else if (payload.bytes.Length < payloadLength)
                        {
                            payload.Grow(payloadLength);
                        }

                        positions.ReadBytes(payload.bytes, 0, payloadLength);
                        payload.length = payloadLength;
                        thisPayload = payload;
                    }
                    else
                    {
                        thisPayload = null;
                    }
                }

                if (offsets == null)
                {
                    startOffset = endOffset = -1;
                }
                else
                {
                    startOffset = lastOffset + offsets.ReadVInt();
                    endOffset = startOffset + offsets.ReadVInt();
                    lastOffset = endOffset;
                }
                AddPosition(position, startOffset, endOffset, thisPayload);
            }
        }

        public virtual int Merge(MergeState mergeState)
        {
            int docCount = 0;
            for (int i = 0; i < mergeState.readers.Count; i++)
            {
                AtomicReader reader = mergeState.readers[i];
                int maxDoc = reader.MaxDoc;
                IBits liveDocs = reader.LiveDocs;

                for (int docID = 0; docID < maxDoc; docID++)
                {
                    if (liveDocs != null && !liveDocs[docID])
                    {
                        // skip deleted docs
                        continue;
                    }
                    // NOTE: it's very important to first assign to vectors then pass it to
                    // termVectorsWriter.addAllDocVectors; see LUCENE-1282
                    Fields vectors = reader.GetTermVectors(docID);
                    AddAllDocVectors(vectors, mergeState);
                    docCount++;
                    mergeState.checkAbort.Work(300);
                }
            }
            Finish(mergeState.fieldInfos, docCount);
            return docCount;
        }

        protected void AddAllDocVectors(Fields vectors, MergeState mergeState)
        {
            if (vectors == null)
            {
                StartDocument(0);
                FinishDocument();
                return;
            }

            int numFields = vectors.Size;
            if (numFields == -1)
            {
                // count manually! TODO: Maybe enforce that Fields.size() returns something valid?
                numFields = 0;
                foreach (string it in vectors.Iterator)
                {
                    numFields++;
                }
            }
            StartDocument(numFields);

            String lastFieldName = null;

            TermsEnum termsEnum = null;
            DocsAndPositionsEnum docsAndPositionsEnum = null;

            int fieldCount = 0;
            foreach (String fieldName in vectors.Iterator)
            {
                fieldCount++;
                FieldInfo fieldInfo = mergeState.fieldInfos.FieldInfo(fieldName);

                //assert lastFieldName == null || fieldName.compareTo(lastFieldName) > 0: "lastFieldName=" + lastFieldName + " fieldName=" + fieldName;
                lastFieldName = fieldName;

                Terms terms = vectors.Terms(fieldName);
                if (terms == null)
                {
                    // FieldsEnum shouldn't lie...
                    continue;
                }

                bool hasPositions = terms.HasPositions;
                bool hasOffsets = terms.HasOffsets;
                bool hasPayloads = terms.HasPayloads;
                //assert !hasPayloads || hasPositions;

                int numTerms = (int)terms.Size;
                if (numTerms == -1)
                {
                    // count manually. It is stupid, but needed, as Terms.size() is not a mandatory statistics function
                    numTerms = 0;
                    termsEnum = terms.Iterator(termsEnum);
                    while (termsEnum.Next() != null)
                    {
                        numTerms++;
                    }
                }

                StartField(fieldInfo, numTerms, hasPositions, hasOffsets, hasPayloads);
                termsEnum = terms.Iterator(termsEnum);

                int termCount = 0;
                while (termsEnum.Next() != null)
                {
                    termCount++;

                    int freq = (int)termsEnum.TotalTermFreq;

                    StartTerm(termsEnum.Term, freq);

                    if (hasPositions || hasOffsets)
                    {
                        docsAndPositionsEnum = termsEnum.DocsAndPositions(null, docsAndPositionsEnum);
                        //assert docsAndPositionsEnum != null;

                        int docID = docsAndPositionsEnum.NextDoc();
                        //assert docID != DocIdSetIterator.NO_MORE_DOCS;
                        //assert docsAndPositionsEnum.freq() == freq;

                        for (int posUpto = 0; posUpto < freq; posUpto++)
                        {
                            int pos = docsAndPositionsEnum.NextPosition();
                            int startOffset = docsAndPositionsEnum.StartOffset;
                            int endOffset = docsAndPositionsEnum.EndOffset;

                            BytesRef payload = docsAndPositionsEnum.Payload;

                            //assert !hasPositions || pos >= 0;
                            AddPosition(pos, startOffset, endOffset, payload);
                        }
                    }
                    FinishTerm();
                }
                //assert termCount == numTerms;
                FinishField();
            }
            //assert fieldCount == numFields;
            FinishDocument();
        }

        public abstract IComparer<BytesRef> Comparator { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}
