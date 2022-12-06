using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;

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
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using TermVectorsWriter = Lucene.Net.Codecs.TermVectorsWriter;

    internal sealed class TermVectorsConsumerPerField : TermsHashConsumerPerField
    {
        internal readonly TermsHashPerField termsHashPerField;
        internal readonly TermVectorsConsumer termsWriter;
        internal readonly FieldInfo fieldInfo;
        internal readonly DocumentsWriterPerThread.DocState docState;
        internal readonly FieldInvertState fieldState;

        internal bool doVectors;
        internal bool doVectorPositions;
        internal bool doVectorOffsets;
        internal bool doVectorPayloads;

        internal int maxNumPostings;
        internal IOffsetAttribute offsetAttribute;
        internal IPayloadAttribute payloadAttribute;
        internal bool hasPayloads; // if enabled, and we actually saw any for this field

        public TermVectorsConsumerPerField(TermsHashPerField termsHashPerField, TermVectorsConsumer termsWriter, FieldInfo fieldInfo)
        {
            this.termsHashPerField = termsHashPerField;
            this.termsWriter = termsWriter;
            this.fieldInfo = fieldInfo;
            docState = termsHashPerField.docState;
            fieldState = termsHashPerField.fieldState;
        }

        internal override int StreamCount => 2;

        internal override bool Start(IIndexableField[] fields, int count)
        {
            doVectors = false;
            doVectorPositions = false;
            doVectorOffsets = false;
            doVectorPayloads = false;
            hasPayloads = false;

            for (int i = 0; i < count; i++)
            {
                IIndexableField field = fields[i];
                if (field.IndexableFieldType.IsIndexed)
                {
                    if (field.IndexableFieldType.StoreTermVectors)
                    {
                        doVectors = true;
                        doVectorPositions |= field.IndexableFieldType.StoreTermVectorPositions;
                        doVectorOffsets |= field.IndexableFieldType.StoreTermVectorOffsets;
                        if (doVectorPositions)
                        {
                            doVectorPayloads |= field.IndexableFieldType.StoreTermVectorPayloads;
                        }
                        else if (field.IndexableFieldType.StoreTermVectorPayloads)
                        {
                            // TODO: move this check somewhere else, and impl the other missing ones
                            throw new ArgumentException("cannot index term vector payloads without term vector positions (field=\"" + field.Name + "\")");
                        }
                    }
                    else
                    {
                        if (field.IndexableFieldType.StoreTermVectorOffsets)
                        {
                            throw new ArgumentException("cannot index term vector offsets when term vectors are not indexed (field=\"" + field.Name + "\")");
                        }
                        if (field.IndexableFieldType.StoreTermVectorPositions)
                        {
                            throw new ArgumentException("cannot index term vector positions when term vectors are not indexed (field=\"" + field.Name + "\")");
                        }
                        if (field.IndexableFieldType.StoreTermVectorPayloads)
                        {
                            throw new ArgumentException("cannot index term vector payloads when term vectors are not indexed (field=\"" + field.Name + "\")");
                        }
                    }
                }
                else
                {
                    if (field.IndexableFieldType.StoreTermVectors)
                    {
                        throw new ArgumentException("cannot index term vectors when field is not indexed (field=\"" + field.Name + "\")");
                    }
                    if (field.IndexableFieldType.StoreTermVectorOffsets)
                    {
                        throw new ArgumentException("cannot index term vector offsets when field is not indexed (field=\"" + field.Name + "\")");
                    }
                    if (field.IndexableFieldType.StoreTermVectorPositions)
                    {
                        throw new ArgumentException("cannot index term vector positions when field is not indexed (field=\"" + field.Name + "\")");
                    }
                    if (field.IndexableFieldType.StoreTermVectorPayloads)
                    {
                        throw new ArgumentException("cannot index term vector payloads when field is not indexed (field=\"" + field.Name + "\")");
                    }
                }
            }

            if (doVectors)
            {
                termsWriter.hasVectors = true;
                if (termsHashPerField.bytesHash.Count != 0)
                {
                    // Only necessary if previous doc hit a
                    // non-aborting exception while writing vectors in
                    // this field:
                    termsHashPerField.Reset();
                }
            }

            // TODO: only if needed for performance
            //perThread.postingsCount = 0;

            return doVectors;
        }

        // LUCENENET: Removed Abort() method because it is not in use.

        /// <summary>
        /// Called once per field per document if term vectors
        /// are enabled, to write the vectors to
        /// RAMOutputStream, which is then quickly flushed to
        /// the real term vectors files in the Directory. 	  
        /// </summary>
        internal override void Finish()
        {
            if (!doVectors || termsHashPerField.bytesHash.Count == 0)
            {
                return;
            }

            termsWriter.AddFieldToFlush(this);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void FinishDocument()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(docState.TestPoint("TermVectorsTermsWriterPerField.finish start"));

            int numPostings = termsHashPerField.bytesHash.Count;

            BytesRef flushTerm = termsWriter.flushTerm;

            if (Debugging.AssertsEnabled) Debugging.Assert(numPostings >= 0);

            if (numPostings > maxNumPostings)
            {
                maxNumPostings = numPostings;
            }

            // this is called once, after inverting all occurrences
            // of a given field in the doc.  At this point we flush
            // our hash into the DocWriter.

            if (Debugging.AssertsEnabled) Debugging.Assert(termsWriter.VectorFieldsInOrder(fieldInfo));

            TermVectorsPostingsArray postings = (TermVectorsPostingsArray)termsHashPerField.postingsArray;
            TermVectorsWriter tv = termsWriter.writer;

            int[] termIDs = termsHashPerField.SortPostings(tv.Comparer);

            tv.StartField(fieldInfo, numPostings, doVectorPositions, doVectorOffsets, hasPayloads);

            ByteSliceReader posReader = doVectorPositions ? termsWriter.vectorSliceReaderPos : null;
            ByteSliceReader offReader = doVectorOffsets ? termsWriter.vectorSliceReaderOff : null;

            ByteBlockPool termBytePool = termsHashPerField.termBytePool;

            for (int j = 0; j < numPostings; j++)
            {
                int termID = termIDs[j];
                int freq = postings.freqs[termID];

                // Get BytesRef
                termBytePool.SetBytesRef(flushTerm, postings.textStarts[termID]);
                tv.StartTerm(flushTerm, freq);

                if (doVectorPositions || doVectorOffsets)
                {
                    if (posReader != null)
                    {
                        termsHashPerField.InitReader(posReader, termID, 0);
                    }
                    if (offReader != null)
                    {
                        termsHashPerField.InitReader(offReader, termID, 1);
                    }
                    tv.AddProx(freq, posReader, offReader);
                }
                tv.FinishTerm();
            }
            tv.FinishField();

            termsHashPerField.Reset();

            fieldInfo.SetStoreTermVectors();
        }

        internal void ShrinkHash()
        {
            termsHashPerField.ShrinkHash(/* maxNumPostings // LUCENENET: Not used */);
            maxNumPostings = 0;
        }

        internal override void Start(IIndexableField f)
        {
            if (doVectorOffsets)
            {
                offsetAttribute = fieldState.AttributeSource.AddAttribute<IOffsetAttribute>();
            }
            else
            {
                offsetAttribute = null;
            }
            if (doVectorPayloads && fieldState.AttributeSource.HasAttribute<IPayloadAttribute>())
            {
                payloadAttribute = fieldState.AttributeSource.GetAttribute<IPayloadAttribute>();
            }
            else
            {
                payloadAttribute = null;
            }
        }

        internal void WriteProx(TermVectorsPostingsArray postings, int termID)
        {
            if (doVectorOffsets)
            {
                int startOffset = fieldState.Offset + offsetAttribute.StartOffset;
                int endOffset = fieldState.Offset + offsetAttribute.EndOffset;

                termsHashPerField.WriteVInt32(1, startOffset - postings.lastOffsets[termID]);
                termsHashPerField.WriteVInt32(1, endOffset - startOffset);
                postings.lastOffsets[termID] = endOffset;
            }

            if (doVectorPositions)
            {
                BytesRef payload;
                if (payloadAttribute is null)
                {
                    payload = null;
                }
                else
                {
                    payload = payloadAttribute.Payload;
                }

                int pos = fieldState.Position - postings.lastPositions[termID];
                if (payload != null && payload.Length > 0)
                {
                    termsHashPerField.WriteVInt32(0, (pos << 1) | 1);
                    termsHashPerField.WriteVInt32(0, payload.Length);
                    termsHashPerField.WriteBytes(0, payload.Bytes, payload.Offset, payload.Length);
                    hasPayloads = true;
                }
                else
                {
                    termsHashPerField.WriteVInt32(0, pos << 1);
                }
                postings.lastPositions[termID] = fieldState.Position;
            }
        }

        internal override void NewTerm(int termID)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(docState.TestPoint("TermVectorsTermsWriterPerField.newTerm start"));
            TermVectorsPostingsArray postings = (TermVectorsPostingsArray)termsHashPerField.postingsArray;

            postings.freqs[termID] = 1;
            postings.lastOffsets[termID] = 0;
            postings.lastPositions[termID] = 0;

            WriteProx(postings, termID);
        }

        internal override void AddTerm(int termID)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(docState.TestPoint("TermVectorsTermsWriterPerField.addTerm start"));
            TermVectorsPostingsArray postings = (TermVectorsPostingsArray)termsHashPerField.postingsArray;

            postings.freqs[termID]++;

            WriteProx(postings, termID);
        }

        [ExceptionToNetNumericConvention]
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
                freqs = new int[size];
                lastOffsets = new int[size];
                lastPositions = new int[size];
            }

            internal int[] freqs; // How many times this term occurred in the current doc
            internal int[] lastOffsets; // Last offset we saw
            internal int[] lastPositions; // Last position where this term occurred

            internal override ParallelPostingsArray NewInstance(int size)
            {
                return new TermVectorsPostingsArray(size);
            }

            internal override void CopyTo(ParallelPostingsArray toArray, int numToCopy)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(toArray is TermVectorsPostingsArray);
                TermVectorsPostingsArray to = (TermVectorsPostingsArray)toArray;

                base.CopyTo(toArray, numToCopy);

                Arrays.Copy(freqs, 0, to.freqs, 0, size);
                Arrays.Copy(lastOffsets, 0, to.lastOffsets, 0, size);
                Arrays.Copy(lastPositions, 0, to.lastPositions, 0, size);
            }

            internal override int BytesPerPosting()
            {
                return base.BytesPerPosting() + 3 * RamUsageEstimator.NUM_BYTES_INT32;
            }
        }
    }
}