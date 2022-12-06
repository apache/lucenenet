using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

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
    /// This wrapper buffers incoming elements and makes sure they are sorted based on given comparer.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class SortedInputEnumerator : IInputEnumerator
    {
        private readonly IInputEnumerator source;
        // LUCENENET specific - since these tempInput and tempSorted are only used in the Sort() method, they were moved there.
        private readonly OfflineSorter.ByteSequencesReader reader;
        private readonly IComparer<BytesRef> comparer;
        private readonly bool hasPayloads;
        private readonly bool hasContexts;
        private bool done = false;

        private long weight;
        private readonly BytesRef scratch = new BytesRef();
        private BytesRef payload = new BytesRef();
        private ISet<BytesRef> contexts = null;
        private BytesRef current;

        /// <summary>
        /// Creates a new sorted wrapper, using <see cref="BytesRef.UTF8SortedAsUnicodeComparer"/>
        /// for sorting. 
        /// </summary>
        public SortedInputEnumerator(IInputEnumerator source)
            : this(source, BytesRef.UTF8SortedAsUnicodeComparer)
        {
        }

        /// <summary>
        /// Creates a new sorted wrapper, sorting by BytesRef
        /// (ascending) then cost (ascending).
        /// </summary>
        public SortedInputEnumerator(IInputEnumerator source, IComparer<BytesRef> comparer)
        {
            this.tieBreakByCostComparer = Comparer<BytesRef>.Create((left, right) =>
            {
                BytesRef leftScratch = new BytesRef();
                BytesRef rightScratch = new BytesRef();
                ByteArrayDataInput input = new ByteArrayDataInput();
                // Make shallow copy in case decode changes the BytesRef:
                leftScratch.Bytes = left.Bytes;
                leftScratch.Offset = left.Offset;
                leftScratch.Length = left.Length;
                rightScratch.Bytes = right.Bytes;
                rightScratch.Offset = right.Offset;
                rightScratch.Length = right.Length;
                long leftCost = Decode(leftScratch, input);
                long rightCost = Decode(rightScratch, input);
                if (HasPayloads)
                {
                    DecodePayload(leftScratch, input);
                    DecodePayload(rightScratch, input);
                }
                if (HasContexts)
                {
                    DecodeContexts(leftScratch, input);
                    DecodeContexts(rightScratch, input);
                }
                // LUCENENET NOTE: outerInstance.Comparer != outerInstance.comparer!!
                int cmp = this.comparer.Compare(leftScratch, rightScratch);
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
            });

            this.hasPayloads = source.HasPayloads;
            this.hasContexts = source.HasContexts;
            this.source = source;
            this.comparer = comparer;
            this.reader = Sort();
        }

        public virtual BytesRef Current => current;

        public virtual bool MoveNext()
        {
            if (done)
                return false;

            bool success = false;
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
                    current = scratch;
                    return true;
                }
                Close();
                success = done = true;
                current = null;
                return false;
            }
            finally
            {
                if (!success)
                {
                    done = true;
                    Close();
                }
            }
        }

        public virtual long Weight => weight;

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

        public virtual bool HasPayloads => hasPayloads;

        public virtual ICollection<BytesRef> Contexts => contexts;

        public virtual IComparer<BytesRef> Comparer => tieBreakByCostComparer;

        public virtual bool HasContexts => hasContexts;

        /// <summary>
        /// Sortes by BytesRef (ascending) then cost (ascending). </summary>
        private readonly IComparer<BytesRef> tieBreakByCostComparer;

        private OfflineSorter.ByteSequencesReader Sort()
        {
            string prefix = this.GetType().Name;
            var directory = OfflineSorter.DefaultTempDir;
            // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.
            FileStream tempInput = FileSupport.CreateTempFileAsStream(prefix, ".input", directory);
            FileStream tempSorted = FileSupport.CreateTempFileAsStream(prefix, ".sorted", directory);

            var writer = new OfflineSorter.ByteSequencesWriter(tempInput);
            bool success = false;
            try
            {
                byte[] buffer = Arrays.Empty<byte>();
                var output = new ByteArrayDataOutput(buffer);

                while (source.MoveNext())
                {
                    Encode(writer, output, buffer, source.Current, source.Payload, source.Contexts, source.Weight);
                }
                tempInput.Position = 0;
                (new OfflineSorter(tieBreakByCostComparer)).Sort(tempInput, tempSorted);

                // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.
                writer.Dispose();
                var reader = new OfflineSorter.ByteSequencesReader(tempSorted);
                success = true;
                return reader;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(writer);
                }
                else
                {
                    try
                    {
                        IOUtils.DisposeWhileHandlingException(writer, tempSorted);
                    }
                    finally
                    {
                        Close();
                    }
                }
            }
        }

        private void Close()
        {
            IOUtils.Dispose(reader);
            // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.
        }

        /// <summary>
        /// encodes an entry (bytes+(contexts)+(payload)+weight) to the provided writer
        /// </summary>
        protected internal virtual void Encode(OfflineSorter.ByteSequencesWriter writer,
            ByteArrayDataOutput output, byte[] buffer, BytesRef spare, BytesRef payload,
            ICollection<BytesRef> contexts, long weight)
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
                    output.WriteInt16((short)ctx.Length);
                }
                output.WriteInt16((short)contexts.Count);
            }
            if (hasPayloads)
            {
                output.WriteBytes(payload.Bytes, payload.Offset, payload.Length);
                output.WriteInt16((short)payload.Length);
            }
            output.WriteInt64(weight);
            writer.Write(buffer, 0, output.Position);
        }

        /// <summary>
        /// decodes the weight at the current position </summary>
        protected internal virtual long Decode(BytesRef scratch, ByteArrayDataInput tmpInput)
        {
            tmpInput.Reset(scratch.Bytes);
            tmpInput.SkipBytes(scratch.Length - 8); // suggestion
            scratch.Length -= 8; // long
            return tmpInput.ReadInt64();
        }

        /// <summary>
        /// decodes the contexts at the current position </summary>
        protected internal virtual ISet<BytesRef> DecodeContexts(BytesRef scratch, ByteArrayDataInput tmpInput)
        {
            tmpInput.Reset(scratch.Bytes);
            tmpInput.SkipBytes(scratch.Length - 2); //skip to context set size
            ushort ctxSetSize = (ushort)tmpInput.ReadInt16();
            scratch.Length -= 2;
            var contextSet = new JCG.HashSet<BytesRef>();
            for (ushort i = 0; i < ctxSetSize; i++)
            {
                tmpInput.Position = scratch.Length - 2;
                ushort curContextLength = (ushort)tmpInput.ReadInt16();
                scratch.Length -= 2;
                tmpInput.Position = scratch.Length - curContextLength;
                BytesRef contextSpare = new BytesRef(curContextLength);
                tmpInput.ReadBytes(contextSpare.Bytes, 0, curContextLength);
                contextSpare.Length = curContextLength;
                contextSet.Add(contextSpare);
                scratch.Length -= curContextLength;
            }
            // LUCENENET NOTE: The result was at one point reversed because of test failures, but since we are
            // using JCG.HashSet<T> now (whose Equals() implementation respects set equality),
            // we have reverted back to the original implementation.
            return contextSet;
        }

        /// <summary>
        /// decodes the payload at the current position
        /// </summary>
        protected internal virtual BytesRef DecodePayload(BytesRef scratch, ByteArrayDataInput tmpInput)
        {
            tmpInput.Reset(scratch.Bytes);
            tmpInput.SkipBytes(scratch.Length - 2); // skip to payload size
            ushort payloadLength = (ushort)tmpInput.ReadInt16(); // read payload size
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