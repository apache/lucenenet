using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Diagnostics;

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

    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using OffsetAttribute = Lucene.Net.Analysis.TokenAttributes.OffsetAttribute;
    using PayloadAttribute = Lucene.Net.Analysis.TokenAttributes.PayloadAttribute;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using TermVectorsWriter = Lucene.Net.Codecs.TermVectorsWriter;

    internal sealed class TermVectorsConsumerPerField : TermsHashConsumerPerField
    {
        internal readonly TermsHashPerField TermsHashPerField;
        internal readonly TermVectorsConsumer TermsWriter;
        internal readonly FieldInfo FieldInfo;
        internal readonly DocumentsWriterPerThread.DocState DocState;
        internal readonly FieldInvertState FieldState;

        internal bool DoVectors;
        internal bool DoVectorPositions;
        internal bool DoVectorOffsets;
        internal bool DoVectorPayloads;

        internal int MaxNumPostings;
        internal IOffsetAttribute OffsetAttribute;
        internal IPayloadAttribute PayloadAttribute;
        internal bool HasPayloads; // if enabled, and we actually saw any for this field

        public TermVectorsConsumerPerField(TermsHashPerField termsHashPerField, TermVectorsConsumer termsWriter, FieldInfo fieldInfo)
        {
            this.TermsHashPerField = termsHashPerField;
            this.TermsWriter = termsWriter;
            this.FieldInfo = fieldInfo;
            DocState = termsHashPerField.docState;
            FieldState = termsHashPerField.fieldState;
        }

        internal override int StreamCount
        {
            get
            {
                return 2;
            }
        }

        internal override bool Start(IIndexableField[] fields, int count)
        {
            DoVectors = false;
            DoVectorPositions = false;
            DoVectorOffsets = false;
            DoVectorPayloads = false;
            HasPayloads = false;

            for (int i = 0; i < count; i++)
            {
                IIndexableField field = fields[i];
                if (field.FieldType.IsIndexed)
                {
                    if (field.FieldType.StoreTermVectors)
                    {
                        DoVectors = true;
                        DoVectorPositions |= field.FieldType.StoreTermVectorPositions;
                        DoVectorOffsets |= field.FieldType.StoreTermVectorOffsets;
                        if (DoVectorPositions)
                        {
                            DoVectorPayloads |= field.FieldType.StoreTermVectorPayloads;
                        }
                        else if (field.FieldType.StoreTermVectorPayloads)
                        {
                            // TODO: move this check somewhere else, and impl the other missing ones
                            throw new System.ArgumentException("cannot index term vector payloads without term vector positions (field=\"" + field.Name + "\")");
                        }
                    }
                    else
                    {
                        if (field.FieldType.StoreTermVectorOffsets)
                        {
                            throw new System.ArgumentException("cannot index term vector offsets when term vectors are not indexed (field=\"" + field.Name + "\")");
                        }
                        if (field.FieldType.StoreTermVectorPositions)
                        {
                            throw new System.ArgumentException("cannot index term vector positions when term vectors are not indexed (field=\"" + field.Name + "\")");
                        }
                        if (field.FieldType.StoreTermVectorPayloads)
                        {
                            throw new System.ArgumentException("cannot index term vector payloads when term vectors are not indexed (field=\"" + field.Name + "\")");
                        }
                    }
                }
                else
                {
                    if (field.FieldType.StoreTermVectors)
                    {
                        throw new System.ArgumentException("cannot index term vectors when field is not indexed (field=\"" + field.Name + "\")");
                    }
                    if (field.FieldType.StoreTermVectorOffsets)
                    {
                        throw new System.ArgumentException("cannot index term vector offsets when field is not indexed (field=\"" + field.Name + "\")");
                    }
                    if (field.FieldType.StoreTermVectorPositions)
                    {
                        throw new System.ArgumentException("cannot index term vector positions when field is not indexed (field=\"" + field.Name + "\")");
                    }
                    if (field.FieldType.StoreTermVectorPayloads)
                    {
                        throw new System.ArgumentException("cannot index term vector payloads when field is not indexed (field=\"" + field.Name + "\")");
                    }
                }
            }

            if (DoVectors)
            {
                TermsWriter.HasVectors = true;
                if (TermsHashPerField.bytesHash.Size != 0)
                {
                    // Only necessary if previous doc hit a
                    // non-aborting exception while writing vectors in
                    // this field:
                    TermsHashPerField.Reset();
                }
            }

            // TODO: only if needed for performance
            //perThread.postingsCount = 0;

            return DoVectors;
        }

        public void Abort()
        {
        }

        /// <summary>
        /// Called once per field per document if term vectors
        ///  are enabled, to write the vectors to
        ///  RAMOutputStream, which is then quickly flushed to
        ///  the real term vectors files in the Directory. 	  /// </summary>
        internal override void Finish()
        {
            if (!DoVectors || TermsHashPerField.bytesHash.Size == 0)
            {
                return;
            }

            TermsWriter.AddFieldToFlush(this);
        }

        internal void FinishDocument()
        {
            Debug.Assert(DocState.TestPoint("TermVectorsTermsWriterPerField.finish start"));

            int numPostings = TermsHashPerField.bytesHash.Size;

            BytesRef flushTerm = TermsWriter.FlushTerm;

            Debug.Assert(numPostings >= 0);

            if (numPostings > MaxNumPostings)
            {
                MaxNumPostings = numPostings;
            }

            // this is called once, after inverting all occurrences
            // of a given field in the doc.  At this point we flush
            // our hash into the DocWriter.

            Debug.Assert(TermsWriter.VectorFieldsInOrder(FieldInfo));

            TermVectorsPostingsArray postings = (TermVectorsPostingsArray)TermsHashPerField.postingsArray;
            TermVectorsWriter tv = TermsWriter.Writer;

            int[] termIDs = TermsHashPerField.SortPostings(tv.Comparator);

            tv.StartField(FieldInfo, numPostings, DoVectorPositions, DoVectorOffsets, HasPayloads);

            ByteSliceReader posReader = DoVectorPositions ? TermsWriter.VectorSliceReaderPos : null;
            ByteSliceReader offReader = DoVectorOffsets ? TermsWriter.VectorSliceReaderOff : null;

            ByteBlockPool termBytePool = TermsHashPerField.termBytePool;

            for (int j = 0; j < numPostings; j++)
            {
                int termID = termIDs[j];
                int freq = postings.Freqs[termID];

                // Get BytesRef
                termBytePool.SetBytesRef(flushTerm, postings.textStarts[termID]);
                tv.StartTerm(flushTerm, freq);

                if (DoVectorPositions || DoVectorOffsets)
                {
                    if (posReader != null)
                    {
                        TermsHashPerField.InitReader(posReader, termID, 0);
                    }
                    if (offReader != null)
                    {
                        TermsHashPerField.InitReader(offReader, termID, 1);
                    }
                    tv.AddProx(freq, posReader, offReader);
                }
                tv.FinishTerm();
            }
            tv.FinishField();

            TermsHashPerField.Reset();

            FieldInfo.SetStoreTermVectors();
        }

        internal void ShrinkHash()
        {
            TermsHashPerField.ShrinkHash(MaxNumPostings);
            MaxNumPostings = 0;
        }

        internal override void Start(IIndexableField f)
        {
            if (DoVectorOffsets)
            {
                OffsetAttribute = FieldState.AttributeSource.AddAttribute<IOffsetAttribute>();
            }
            else
            {
                OffsetAttribute = null;
            }
            if (DoVectorPayloads && FieldState.AttributeSource.HasAttribute<IPayloadAttribute>())
            {
                PayloadAttribute = FieldState.AttributeSource.GetAttribute<IPayloadAttribute>();
            }
            else
            {
                PayloadAttribute = null;
            }
        }

        internal void WriteProx(TermVectorsPostingsArray postings, int termID)
        {
            if (DoVectorOffsets)
            {
                int startOffset = FieldState.Offset + OffsetAttribute.StartOffset;
                int endOffset = FieldState.Offset + OffsetAttribute.EndOffset;

                TermsHashPerField.WriteVInt(1, startOffset - postings.LastOffsets[termID]);
                TermsHashPerField.WriteVInt(1, endOffset - startOffset);
                postings.LastOffsets[termID] = endOffset;
            }

            if (DoVectorPositions)
            {
                BytesRef payload;
                if (PayloadAttribute == null)
                {
                    payload = null;
                }
                else
                {
                    payload = PayloadAttribute.Payload;
                }

                int pos = FieldState.Position - postings.LastPositions[termID];
                if (payload != null && payload.Length > 0)
                {
                    TermsHashPerField.WriteVInt(0, (pos << 1) | 1);
                    TermsHashPerField.WriteVInt(0, payload.Length);
                    TermsHashPerField.WriteBytes(0, payload.Bytes, payload.Offset, payload.Length);
                    HasPayloads = true;
                }
                else
                {
                    TermsHashPerField.WriteVInt(0, pos << 1);
                }
                postings.LastPositions[termID] = FieldState.Position;
            }
        }

        internal override void NewTerm(int termID)
        {
            Debug.Assert(DocState.TestPoint("TermVectorsTermsWriterPerField.newTerm start"));
            TermVectorsPostingsArray postings = (TermVectorsPostingsArray)TermsHashPerField.postingsArray;

            postings.Freqs[termID] = 1;
            postings.LastOffsets[termID] = 0;
            postings.LastPositions[termID] = 0;

            WriteProx(postings, termID);
        }

        internal override void AddTerm(int termID)
        {
            Debug.Assert(DocState.TestPoint("TermVectorsTermsWriterPerField.addTerm start"));
            TermVectorsPostingsArray postings = (TermVectorsPostingsArray)TermsHashPerField.postingsArray;

            postings.Freqs[termID]++;

            WriteProx(postings, termID);
        }

        internal override void SkippingLongTerm()
        {
        }

        internal override ParallelPostingsArray CreatePostingsArray(int size)
        {
            return new TermVectorsPostingsArray(size);
        }

        internal sealed class TermVectorsPostingsArray : ParallelPostingsArray
        {
            public TermVectorsPostingsArray(int size)
                : base(size)
            {
                Freqs = new int[size];
                LastOffsets = new int[size];
                LastPositions = new int[size];
            }

            internal int[] Freqs; // How many times this term occurred in the current doc
            internal int[] LastOffsets; // Last offset we saw
            internal int[] LastPositions; // Last position where this term occurred

            internal override ParallelPostingsArray NewInstance(int size)
            {
                return new TermVectorsPostingsArray(size);
            }

            internal override void CopyTo(ParallelPostingsArray toArray, int numToCopy)
            {
                Debug.Assert(toArray is TermVectorsPostingsArray);
                TermVectorsPostingsArray to = (TermVectorsPostingsArray)toArray;

                base.CopyTo(toArray, numToCopy);

                Array.Copy(Freqs, 0, to.Freqs, 0, size);
                Array.Copy(LastOffsets, 0, to.LastOffsets, 0, size);
                Array.Copy(LastPositions, 0, to.LastPositions, 0, size);
            }

            internal override int BytesPerPosting()
            {
                return base.BytesPerPosting() + 3 * RamUsageEstimator.NUM_BYTES_INT;
            }
        }
    }
}