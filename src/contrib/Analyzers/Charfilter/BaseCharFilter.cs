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

using System.Diagnostics;
using System.IO;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Charfilter
{
    public abstract class BaseCharFilter : CharFilter
    {
        private int[] offsets;
        private int[] diffs;
        private int size;

        protected BaseCharFilter(StreamReader input)
            : base(input)
        {
        }

        protected override int Correct(int currentOff)
        {
            if (offsets == null || currentOff < offsets[0])
            {
                return currentOff;
            }

            var hi = size - 1;
            if (currentOff >= offsets[hi])
            {
                return currentOff + diffs[hi];
            }

            var lo = 0;
            var mid = -1;

            while (hi >= lo)
            {
                mid = Number.URShift((lo + hi), 1);
                if (currentOff < offsets[mid])
                    hi = mid - 1;
                else if (currentOff > offsets[mid])
                    lo = mid + 1;
                else
                    return currentOff + diffs[mid];
            }

            if (currentOff < offsets[mid])
                return mid == 0 ? currentOff : currentOff + diffs[mid - 1];
            else
                return currentOff + diffs[mid];
        }

        protected int LastCumulativeDiff
        {
            get { return offsets == null ? 0 : diffs[size - 1]; }
        }

        protected void AddOffCorrectMap(int off, int cumulativeDiff)
        {
            if (offsets == null)
            {
                offsets = new int[64];
                diffs = new int[64];
            }
            else if (size == offsets.Length)
            {
                offsets = ArrayUtil.Grow(offsets);
                diffs = ArrayUtil.Grow(diffs);
            }

            Debug.Assert(size == 0 || off >= offsets[size - 1],
                         string.Format("Offset #{0}({1}) is less than the last recorded offset {2}\n{3}\n{4}",
                         size, off, offsets[size - 1], Arrays.ToString(offsets), Arrays.ToString(diffs)));

            if (size == 0 || off != offsets[size - 1])
            {
                offsets[size] = off;
                diffs[size++] = cumulativeDiff;
            }
            else
            {
                diffs[size - 1] = cumulativeDiff;
            }
        }
    }
}
