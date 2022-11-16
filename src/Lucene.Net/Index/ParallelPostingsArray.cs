using System;
using Lucene.Net.Support;

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
        internal const int BYTES_PER_POSTING = 3 * RamUsageEstimator.NUM_BYTES_INT32;

        internal readonly int size;
        internal readonly int[] textStarts;
        internal readonly int[] intStarts;
        internal readonly int[] byteStarts;

        internal ParallelPostingsArray(int size)
        {
            this.size = size;
            textStarts = new int[size];
            intStarts = new int[size];
            byteStarts = new int[size];
        }

        internal virtual int BytesPerPosting()
        {
            return BYTES_PER_POSTING;
        }

        internal virtual ParallelPostingsArray NewInstance(int size)
        {
            return new ParallelPostingsArray(size);
        }

        internal ParallelPostingsArray Grow()
        {
            int newSize = ArrayUtil.Oversize(size + 1, BytesPerPosting());
            ParallelPostingsArray newArray = NewInstance(newSize);
            CopyTo(newArray, size);
            return newArray;
        }

        internal virtual void CopyTo(ParallelPostingsArray toArray, int numToCopy)
        {
            Arrays.Copy(textStarts, 0, toArray.textStarts, 0, numToCopy);
            Arrays.Copy(intStarts, 0, toArray.intStarts, 0, numToCopy);
            Arrays.Copy(byteStarts, 0, toArray.byteStarts, 0, numToCopy);
        }
    }
}