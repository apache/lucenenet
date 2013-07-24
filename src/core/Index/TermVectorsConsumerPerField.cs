using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Codecs;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
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

        public override int StreamCount
        {
            get { return 2; }
        }

        public override bool Start(IIndexableField[] fields, int count)
        {
            doVectors = false;
            doVectorPositions = false;
            doVectorOffsets = false;
            doVectorPayloads = false;
            hasPayloads = false;

            for (int i = 0; i < count; i++)
            {
                IIndexableField field = fields[i];
                if (field.FieldTypeValue.Indexed)
                {
                    if (field.FieldTypeValue.StoreTermVectors)
                    {
                        doVectors = true;
                        doVectorPositions |= field.FieldTypeValue.StoreTermVectorPositions;
                        doVectorOffsets |= field.FieldTypeValue.StoreTermVectorOffsets;
                        if (doVectorPositions)
                        {
                            doVectorPayloads |= field.FieldTypeValue.StoreTermVectorPayloads;
                        }
                        else if (field.FieldTypeValue.StoreTermVectorPayloads)
                        {
                            // TODO: move this check somewhere else, and impl the other missing ones
                            throw new ArgumentException("cannot index term vector payloads for field: " + field + " without term vector positions");
                        }
                    }
                    else
                    {
                        if (field.FieldType.StoreTermVectorOffsets)
                        {
                            throw new ArgumentException("cannot index term vector offsets when term vectors are not indexed (field=\"" + field.name());
                        }
                        if (field.FieldType.StoreTermVectorPositions)
                        {
                            throw new ArgumentException("cannot index term vector positions when term vectors are not indexed (field=\"" + field.name());
                        }
                        if (field.FieldType.StoreTermVectorPayloads)
                        {
                            throw new ArgumentException("cannot index term vector payloads when term vectors are not indexed (field=\"" + field.name());
                        }
                    }
                }
                else
                {
                    if (field.FieldType.StoreTermVectors)
                    {
                        throw new ArgumentException("cannot index term vectors when field is not indexed (field=\"" + field.name());
                    }
                    if (field.FieldType.StoreTermVectorOffsets)
                    {
                        throw new ArgumentException("cannot index term vector offsets when field is not indexed (field=\"" + field.name());
                    }
                    if (field.FieldType.StoreTermVectorPositions)
                    {
                        throw new ArgumentException("cannot index term vector positions when field is not indexed (field=\"" + field.name());
                    }
                    if (field.FieldType.StoreTermVectorPayloads)
                    {
                        throw new ArgumentException("cannot index term vector payloads when field is not indexed (field=\"" + field.name());
                    }
                }
            }

            if (doVectors)
            {
                termsWriter.hasVectors = true;
                if (termsHashPerField.bytesHash.Size != 0)
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

        public void Abort() { }

        public override void Finish()
        {
            if (!doVectors || termsHashPerField.bytesHash.Size == 0)
            {
                return;
            }

            termsWriter.AddFieldToFlush(this);
        }

        internal void FinishDocument()
        {
            //assert docState.testPoint("TermVectorsTermsWriterPerField.finish start");

            int numPostings = termsHashPerField.bytesHash.Size;

            BytesRef flushTerm = termsWriter.flushTerm;

            //assert numPostings >= 0;

            if (numPostings > maxNumPostings)
                maxNumPostings = numPostings;

            // This is called once, after inverting all occurrences
            // of a given field in the doc.  At this point we flush
            // our hash into the DocWriter.

            //assert termsWriter.vectorFieldsInOrder(fieldInfo);

            TermVectorsPostingsArray postings = (TermVectorsPostingsArray)termsHashPerField.postingsArray;
            TermVectorsWriter tv = termsWriter.writer;

            int[] termIDs = termsHashPerField.SortPostings(tv.Comparator);

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
            termsHashPerField.ShrinkHash(maxNumPostings);
            maxNumPostings = 0;
        }

        public override void Start(IIndexableField field)
        {
            if (doVectorOffsets)
            {
                offsetAttribute = fieldState.attributeSource.AddAttribute<IOffsetAttribute>();
            }
            else
            {
                offsetAttribute = null;
            }
            if (doVectorPayloads && fieldState.attributeSource.HasAttribute<IPayloadAttribute>())
            {
                payloadAttribute = fieldState.attributeSource.GetAttribute<IPayloadAttribute>();
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
                int startOffset = fieldState.offset + offsetAttribute.StartOffset;
                int endOffset = fieldState.offset + offsetAttribute.EndOffset;

                termsHashPerField.WriteVInt(1, startOffset - postings.lastOffsets[termID]);
                termsHashPerField.WriteVInt(1, endOffset - startOffset);
                postings.lastOffsets[termID] = endOffset;
            }

            if (doVectorPositions)
            {
                BytesRef payload;
                if (payloadAttribute == null)
                {
                    payload = null;
                }
                else
                {
                    payload = payloadAttribute.Payload;
                }

                int pos = fieldState.position - postings.lastPositions[termID];
                if (payload != null && payload.length > 0)
                {
                    termsHashPerField.WriteVInt(0, (pos << 1) | 1);
                    termsHashPerField.WriteVInt(0, payload.length);
                    termsHashPerField.WriteBytes(0, (byte[])(Array)payload.bytes, payload.offset, payload.length);
                    hasPayloads = true;
                }
                else
                {
                    termsHashPerField.WriteVInt(0, pos << 1);
                }
                postings.lastPositions[termID] = fieldState.position;
            }
        }

        public override void NewTerm(int termID)
        {
            //assert docState.testPoint("TermVectorsTermsWriterPerField.newTerm start");
            TermVectorsPostingsArray postings = (TermVectorsPostingsArray)termsHashPerField.postingsArray;

            postings.freqs[termID] = 1;
            postings.lastOffsets[termID] = 0;
            postings.lastPositions[termID] = 0;

            WriteProx(postings, termID);
        }

        public override void AddTerm(int termID)
        {
            //assert docState.testPoint("TermVectorsTermsWriterPerField.addTerm start");
            TermVectorsPostingsArray postings = (TermVectorsPostingsArray)termsHashPerField.postingsArray;

            postings.freqs[termID]++;

            WriteProx(postings, termID);
        }

        public override void SkippingLongTerm()
        {
        }

        public override ParallelPostingsArray CreatePostingsArray(int size)
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

            internal int[] freqs;                                       // How many times this term occurred in the current doc
            internal int[] lastOffsets;                                 // Last offset we saw
            internal int[] lastPositions;                               // Last position where this term occurred

            internal override ParallelPostingsArray NewInstance(int size)
            {
                return new TermVectorsPostingsArray(size);
            }

            internal override void CopyTo(ParallelPostingsArray toArray, int numToCopy)
            {
                //assert toArray instanceof TermVectorsPostingsArray;
                TermVectorsPostingsArray to = (TermVectorsPostingsArray)toArray;

                base.CopyTo(toArray, numToCopy);

                Array.Copy(freqs, 0, to.freqs, 0, size);
                Array.Copy(lastOffsets, 0, to.lastOffsets, 0, size);
                Array.Copy(lastPositions, 0, to.lastPositions, 0, size);
            }

            internal override int BytesPerPosting
            {
                get
                {
                    return base.BytesPerPosting + 3 * RamUsageEstimator.NUM_BYTES_INT;
                }
            }
        }
    }
}
