using System;

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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    internal class ParallelPostingsArray
    {
        internal static readonly int BYTES_PER_POSTING = 3 * RamUsageEstimator.NUM_BYTES_INT;

        internal readonly int Size;
        internal readonly int[] TextStarts;
        internal readonly int[] IntStarts;
        internal readonly int[] ByteStarts;

        internal ParallelPostingsArray(int size)
        {
            this.Size = size;
            TextStarts = new int[size];
            IntStarts = new int[size];
            ByteStarts = new int[size];
        }

        internal virtual int BytesPerPosting() // LUCENENET TODO: Make property
        {
            return BYTES_PER_POSTING;
        }

        internal virtual ParallelPostingsArray NewInstance(int size)
        {
            return new ParallelPostingsArray(size);
        }

        internal ParallelPostingsArray Grow()
        {
            int newSize = ArrayUtil.Oversize(Size + 1, BytesPerPosting());
            ParallelPostingsArray newArray = NewInstance(newSize);
            CopyTo(newArray, Size);
            return newArray;
        }

        internal virtual void CopyTo(ParallelPostingsArray toArray, int numToCopy)
        {
            Array.Copy(TextStarts, 0, toArray.TextStarts, 0, numToCopy);
            Array.Copy(IntStarts, 0, toArray.IntStarts, 0, numToCopy);
            Array.Copy(ByteStarts, 0, toArray.ByteStarts, 0, numToCopy);
        }
    }
}