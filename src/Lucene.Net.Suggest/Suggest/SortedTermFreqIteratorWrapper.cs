using Lucene.Net.Search.Spell;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

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
    public class SortedTermFreqEnumeratorWrapper : ITermFreqEnumerator
    {
        private readonly ITermFreqEnumerator source;
        // LUCENENET specific - since these tempInput and tempSorted are only used in the Sort() method, they were moved there.
        private readonly OfflineSorter.ByteSequencesReader reader;
        private readonly IComparer<BytesRef> comparer;
        private bool done = false;

        private long weight;
        private readonly BytesRef scratch = new BytesRef();
        private BytesRef current;

        /// <summary>
        /// Creates a new sorted wrapper, using <see cref="BytesRef.UTF8SortedAsUnicodeComparer"/>
        /// for sorting. 
        /// </summary>
        public SortedTermFreqEnumeratorWrapper(ITermFreqEnumerator source)
            : this(source, BytesRef.UTF8SortedAsUnicodeComparer)
        { }

        /// <summary>
        /// Creates a new sorted wrapper, sorting by BytesRef
        /// (ascending) then cost (ascending).
        /// </summary>
        public SortedTermFreqEnumeratorWrapper(ITermFreqEnumerator source, IComparer<BytesRef> comparer)
        {
            this.source = source;
            this.comparer = comparer;
            this.reader = Sort();
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
                int cmp = comparer.Compare(leftScratch, rightScratch);
                if (cmp != 0)
                {
                    return cmp;
                }
                return leftCost.CompareTo(rightCost);
            });
        }

        public virtual IComparer<BytesRef> Comparer => comparer;

        public virtual BytesRef Current => current;

        public virtual bool MoveNext()
        {
            if (done) return false;
            bool success = false;
            try
            {
                var input = new ByteArrayDataInput();
                if (reader.Read(scratch))
                {
                    weight = Decode(scratch, input);
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

        /// <summary>
        /// Sortes by BytesRef (ascending) then cost (ascending).
        /// </summary>
        private readonly IComparer<BytesRef> tieBreakByCostComparer;

        private OfflineSorter.ByteSequencesReader Sort()
        {
            string prefix = this.GetType().Name;
            var directory = OfflineSorter.DefaultTempDir;
            FileStream tempInput = FileSupport.CreateTempFileAsStream(prefix, ".input", directory);
            FileStream tempSorted = FileSupport.CreateTempFileAsStream(prefix, ".sorted", directory);

            var writer = new OfflineSorter.ByteSequencesWriter(tempInput);
            bool success = false;
            try
            {
                byte[] buffer = Arrays.Empty<byte>();
                ByteArrayDataOutput output = new ByteArrayDataOutput(buffer);

                while (source.MoveNext())
                {
                    Encode(writer, output, buffer, source.Current, source.Weight);
                }
                // LUCENENET: Reset the position to the beginning of the stream so we don't have to reopen the file
                tempInput.Position = 0;
                (new OfflineSorter(tieBreakByCostComparer)).Sort(tempInput, tempSorted);
                writer.Dispose(); // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.
                OfflineSorter.ByteSequencesReader reader = new OfflineSorter.ByteSequencesReader(tempSorted);
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
        /// encodes an entry (bytes+weight) to the provided writer
        /// </summary>
        protected internal virtual void Encode(OfflineSorter.ByteSequencesWriter writer,
            ByteArrayDataOutput output, byte[] buffer, BytesRef spare, long weight)
        {
            if (spare.Length + 8 >= buffer.Length)
            {
                buffer = ArrayUtil.Grow(buffer, spare.Length + 8);
            }
            output.Reset(buffer);
            output.WriteBytes(spare.Bytes, spare.Offset, spare.Length);
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
    }
}