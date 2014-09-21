using System.Collections.Generic;
using Lucene.Net.Search.Spell;
using Lucene.Net.Store;
using Lucene.Net.Util;

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
    public class SortedTermFreqIteratorWrapper : TermFreqIterator
    {

        private readonly TermFreqIterator source;
        private File tempInput;
        private File tempSorted;
        private readonly OfflineSorter.ByteSequencesReader reader;
        private readonly IComparer<BytesRef> comparator;
        private bool done = false;

        private long weight;
        private readonly BytesRef scratch = new BytesRef();

        /// <summary>
        /// Creates a new sorted wrapper, using {@link
        /// BytesRef#getUTF8SortedAsUnicodeComparator} for
        /// sorting. 
        /// </summary>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public SortedTermFreqIteratorWrapper(org.apache.lucene.search.spell.TermFreqIterator source) throws java.io.IOException
        public SortedTermFreqIteratorWrapper(TermFreqIterator source)
            : this(source, BytesRef.UTF8SortedAsUnicodeComparator)
        {
        }

        /// <summary>
        /// Creates a new sorted wrapper, sorting by BytesRef
        /// (ascending) then cost (ascending).
        /// </summary>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public SortedTermFreqIteratorWrapper(org.apache.lucene.search.spell.TermFreqIterator source, java.util.Comparator<org.apache.lucene.util.BytesRef> comparator) throws java.io.IOException
        public SortedTermFreqIteratorWrapper(TermFreqIterator source, IComparer<BytesRef> comparator)
        {
            this.source = source;
            this.comparator = comparator;
            this.reader = Sort();
        }

        public IComparer<BytesRef> Comparator
        {
            get
            {
                return comparator;
            }
        }

        public BytesRef Next()
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
                    success = true;
                    return scratch;
                }
                close();
                success = done = true;
                return null;
            }
            finally
            {
                if (!success)
                {
                    done = true;
                    close();
                }
            }
        }

        public virtual long Weight
        {
            get { return weight; }
        }

        /// <summary>
        /// Sortes by BytesRef (ascending) then cost (ascending).
        /// </summary>
        private readonly IComparer<BytesRef> tieBreakByCostComparator = new ComparatorAnonymousInnerClassHelper();

        private class ComparatorAnonymousInnerClassHelper : IComparer<BytesRef>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }


            private readonly BytesRef leftScratch = new BytesRef();
            private readonly BytesRef rightScratch = new BytesRef();
            private readonly ByteArrayDataInput input = new ByteArrayDataInput();

            public virtual int Compare(BytesRef left, BytesRef right)
            {
                // Make shallow copy in case decode changes the BytesRef:
                leftScratch.bytes = left.bytes;
                leftScratch.offset = left.offset;
                leftScratch.length = left.length;
                rightScratch.bytes = right.bytes;
                rightScratch.offset = right.offset;
                rightScratch.length = right.length;
                long leftCost = outerInstance.decode(leftScratch, input);
                long rightCost = outerInstance.decode(rightScratch, input);
                int cmp = outerInstance.comparator.Compare(leftScratch, rightScratch);
                if (cmp != 0)
                {
                    return cmp;
                }
                return long.Compare(leftCost, rightCost);
            }
        }

        private OfflineSorter.ByteSequencesReader Sort()
        {
            string prefix = this.GetType().Name;
            File directory = OfflineSorter.DefaultTempDir();
            tempInput = File.createTempFile(prefix, ".input", directory);
            tempSorted = File.createTempFile(prefix, ".sorted", directory);

            var writer = new OfflineSorter.ByteSequencesWriter(tempInput);
            bool success = false;
            try
            {
                BytesRef spare;
                sbyte[] buffer = new sbyte[0];
                ByteArrayDataOutput output = new ByteArrayDataOutput(buffer);

                while ((spare = source.Next()) != null)
                {
                    Encode(writer, output, buffer, spare, source.Weight);
                }
                writer.Dispose();
                (new OfflineSorter(tieBreakByCostComparator)).Sort(tempInput, tempSorted);
                OfflineSorter.ByteSequencesReader reader = new OfflineSorter.ByteSequencesReader(tempSorted);
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
                        Close();
                    }
                }
            }
        }

        private void Close()
        {
            IOUtils.Close(reader);
            if (tempInput != null)
            {
                tempInput.delete();
            }
            if (tempSorted != null)
            {
                tempSorted.delete();
            }
        }

        /// <summary>
        /// encodes an entry (bytes+weight) to the provided writer
        /// </summary>
        protected internal virtual void Encode(OfflineSorter.ByteSequencesWriter writer, ByteArrayDataOutput output, sbyte[] buffer, BytesRef spare, long weight)
        {
            if (spare.Length + 8 >= buffer.Length)
            {
                buffer = ArrayUtil.Grow(buffer, spare.Length + 8);
            }
            output.Reset(buffer);
            output.WriteBytes(spare.Bytes, spare.Offset, spare.Length);
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
    }
}