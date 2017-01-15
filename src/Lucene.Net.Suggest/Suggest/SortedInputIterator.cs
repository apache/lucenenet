using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lucene.Net.Search.Suggest
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

    /// <summary>
    /// This wrapper buffers incoming elements and makes sure they are sorted based on given comparator.
    /// @lucene.experimental
    /// </summary>
    public class SortedInputIterator : IInputIterator
    {

        private readonly IInputIterator source;
        private FileInfo tempInput;
        private FileInfo tempSorted;
        private readonly OfflineSorter.ByteSequencesReader reader;
        private readonly IComparer<BytesRef> comparator;
        private readonly bool hasPayloads;
        private readonly bool hasContexts;
        private bool done = false;

        private long weight;
        private readonly BytesRef scratch = new BytesRef();
        private BytesRef payload = new BytesRef();
        private ISet<BytesRef> contexts = null;

        /// <summary>
        /// Creates a new sorted wrapper, using <see cref="BytesRef.UTF8SortedAsUnicodeComparer"/>
        /// for sorting. 
        /// </summary>
        public SortedInputIterator(IInputIterator source)
            : this(source, BytesRef.UTF8SortedAsUnicodeComparer)
        {
        }

        /// <summary>
        /// Creates a new sorted wrapper, sorting by BytesRef
        /// (ascending) then cost (ascending).
        /// </summary>
        public SortedInputIterator(IInputIterator source, IComparer<BytesRef> comparator)
        {
            this.tieBreakByCostComparer = new ComparerAnonymousInnerClassHelper(this);
            this.hasPayloads = source.HasPayloads;
            this.hasContexts = source.HasContexts;
            this.source = source;
            this.comparator = comparator;
            this.reader = Sort();
        }

        public virtual BytesRef Next()
        {
            bool success = false;
            if (done)
            {
                return null;
            }
            try
            {
                var input = new ByteArrayDataInput();
                if (reader.Read(scratch))
                {
                    weight = Decode(scratch, input);
                    if (hasPayloads)
                    {
                        payload = DecodePayload(scratch, input);
                    }
                    if (hasContexts)
                    {
                        contexts = DecodeContexts(scratch, input);
                    }
                    success = true;
                    return scratch;
                }
                Dispose();
                success = done = true;
                return null;
            }
            finally
            {
                if (!success)
                {
                    done = true;
                    Dispose();
                }
            }
        }

        public virtual long Weight
        {
            get { return weight; }
        }

        public virtual BytesRef Payload
        {
            get
            {
                if (hasPayloads)
                {
                    return payload;
                }
                return null;
            }
        }

        public virtual bool HasPayloads
        {
            get { return hasPayloads; }
        }

        public virtual IEnumerable<BytesRef> Contexts
        {
            get { return contexts; }
        }

        public virtual IComparer<BytesRef> Comparer
        {
            get
            {
                return tieBreakByCostComparer;
            }
        }

        public virtual bool HasContexts
        {
            get { return hasContexts; }
        }

        /// <summary>
        /// Sortes by BytesRef (ascending) then cost (ascending). </summary>
        private readonly IComparer<BytesRef> tieBreakByCostComparer;

        private class ComparerAnonymousInnerClassHelper : IComparer<BytesRef>
        {
            private readonly SortedInputIterator outerInstance;
            public ComparerAnonymousInnerClassHelper(SortedInputIterator outerInstance)
            {
                this.outerInstance = outerInstance;
            }


            private readonly BytesRef leftScratch = new BytesRef();
            private readonly BytesRef rightScratch = new BytesRef();
            private readonly ByteArrayDataInput input = new ByteArrayDataInput();

            public virtual int Compare(BytesRef left, BytesRef right)
            {
                // Make shallow copy in case decode changes the BytesRef:
                leftScratch.Bytes = left.Bytes;
                leftScratch.Offset = left.Offset;
                leftScratch.Length = left.Length;
                rightScratch.Bytes = right.Bytes;
                rightScratch.Offset = right.Offset;
                rightScratch.Length = right.Length;
                long leftCost = outerInstance.Decode(leftScratch, input);
                long rightCost = outerInstance.Decode(rightScratch, input);
                if (outerInstance.HasPayloads)
                {
                    outerInstance.DecodePayload(leftScratch, input);
                    outerInstance.DecodePayload(rightScratch, input);
                }
                if (outerInstance.HasContexts)
                {
                    outerInstance.DecodeContexts(leftScratch, input);
                    outerInstance.DecodeContexts(rightScratch, input);
                }
                // LUCENENET NOTE: outerInstance.Comparer != outerInstance.comparator!!
                int cmp = outerInstance.comparator.Compare(leftScratch, rightScratch);
                if (cmp != 0)
                {
                    return cmp;
                }
                if (leftCost < rightCost)
                {
                    return -1;
                }
                else if (leftCost > rightCost)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }

            }
        }

        private OfflineSorter.ByteSequencesReader Sort()
        {
            string prefix = this.GetType().Name;
            DirectoryInfo directory = OfflineSorter.DefaultTempDir();
            tempInput = FileSupport.CreateTempFile(prefix, ".input", directory);
            tempSorted = FileSupport.CreateTempFile(prefix, ".sorted", directory);

            var writer = new OfflineSorter.ByteSequencesWriter(tempInput);
            bool success = false;
            try
            {
                BytesRef spare;
                byte[] buffer = new byte[0];
                var output = new ByteArrayDataOutput(buffer);

                while ((spare = source.Next()) != null)
                {
                    Encode(writer, output, buffer, spare, source.Payload, source.Contexts, source.Weight);
                }
                writer.Dispose();
                (new OfflineSorter(tieBreakByCostComparer)).Sort(tempInput, tempSorted);
                var reader = new OfflineSorter.ByteSequencesReader(tempSorted);
                success = true;
                return reader;

            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(writer);
                }
                else
                {
                    try
                    {
                        IOUtils.CloseWhileHandlingException(writer);
                    }
                    finally
                    {
                        Dispose();
                    }
                }
            }
        }

        private void Dispose()
        {
            IOUtils.Close(reader);
            if (tempInput != null)
            {
                tempInput.Delete();
            }
            if (tempSorted != null)
            {
                tempSorted.Delete();
            }
        }

        /// <summary>
        /// encodes an entry (bytes+(contexts)+(payload)+weight) to the provided writer
        /// </summary>
        protected internal virtual void Encode(OfflineSorter.ByteSequencesWriter writer, 
            ByteArrayDataOutput output, byte[] buffer, BytesRef spare, BytesRef payload, 
            IEnumerable<BytesRef> contexts, long weight)
        {
            int requiredLength = spare.Length + 8 + ((hasPayloads) ? 2 + payload.Length : 0);
            if (hasContexts)
            {
                foreach (BytesRef ctx in contexts)
                {
                    requiredLength += 2 + ctx.Length;
                }
                requiredLength += 2; // for length of contexts
            }
            if (requiredLength >= buffer.Length)
            {
                buffer = ArrayUtil.Grow(buffer, requiredLength);
            }
            output.Reset(buffer);
            output.WriteBytes(spare.Bytes, spare.Offset, spare.Length);
            if (hasContexts)
            {
                foreach (BytesRef ctx in contexts)
                {
                    output.WriteBytes(ctx.Bytes, ctx.Offset, ctx.Length);
                    output.WriteShort((short)ctx.Length);
                }
                output.WriteShort((short)contexts.Count());
            }
            if (hasPayloads)
            {
                output.WriteBytes(payload.Bytes, payload.Offset, payload.Length);
                output.WriteShort((short)payload.Length);
            }
            output.WriteLong(weight);
            writer.Write(buffer, 0, output.Position);
        }

        /// <summary>
        /// decodes the weight at the current position </summary>
        protected internal virtual long Decode(BytesRef scratch, ByteArrayDataInput tmpInput)
        {
            tmpInput.Reset(scratch.Bytes);
            tmpInput.SkipBytes(scratch.Length - 8); // suggestion
            scratch.Length -= 8; // long
            return tmpInput.ReadLong();
        }

        /// <summary>
        /// decodes the contexts at the current position </summary>
        protected internal virtual ISet<BytesRef> DecodeContexts(BytesRef scratch, ByteArrayDataInput tmpInput)
        {
            tmpInput.Reset(scratch.Bytes);
            tmpInput.SkipBytes(scratch.Length - 2); //skip to context set size
            ushort ctxSetSize = (ushort)tmpInput.ReadShort();
            scratch.Length -= 2;

            var contextSet = new HashSet<BytesRef>();
            for (ushort i = 0; i < ctxSetSize; i++)
            {
                tmpInput.Position = scratch.Length - 2;
                ushort curContextLength = (ushort)tmpInput.ReadShort();
                scratch.Length -= 2;
                tmpInput.Position = scratch.Length - curContextLength;
                BytesRef contextSpare = new BytesRef(curContextLength);
                tmpInput.ReadBytes(contextSpare.Bytes, 0, curContextLength);
                contextSpare.Length = curContextLength;
                contextSet.Add(contextSpare); 
                scratch.Length -= curContextLength;
            }

            // LUCENENET TODO: We are writing the data forward.
            // Not sure exactly why, but when we read it back it
            // is reversed. So, we need to fix that before returning the result.
            // If the underlying problem is found and fixed, then this line can just be
            // return contextSet;
            return new HashSet<BytesRef>(contextSet.Reverse());
        }

        /// <summary>
        /// decodes the payload at the current position
        /// </summary>
        protected internal virtual BytesRef DecodePayload(BytesRef scratch, ByteArrayDataInput tmpInput)
        {
            tmpInput.Reset(scratch.Bytes);
            tmpInput.SkipBytes(scratch.Length - 2); // skip to payload size
            ushort payloadLength = (ushort)tmpInput.ReadShort(); // read payload size
            tmpInput.Position = scratch.Length - 2 - payloadLength; // setPosition to start of payload
            BytesRef payloadScratch = new BytesRef(payloadLength);
            tmpInput.ReadBytes(payloadScratch.Bytes, 0, payloadLength); // read payload
            payloadScratch.Length = payloadLength;
            scratch.Length -= 2; // payload length info (short)
            scratch.Length -= payloadLength; // payload
            return payloadScratch;
        }
    }
}