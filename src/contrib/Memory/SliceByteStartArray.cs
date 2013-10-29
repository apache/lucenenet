/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index.Memory
{
    public partial class MemoryIndex
    {
        internal class SliceByteStartArray : BytesRefHash.DirectBytesStartArray
        {
            internal int[] start; // the start offset in the IntBlockPool per term
            internal int[] end; // the end pointer in the IntBlockPool for the postings slice per term
            internal int[] freq; // the term frequency

            public SliceByteStartArray(int initSize)
                : base(initSize)
            {
            }

            public override int[] Init()
            {
                int[] ord = base.Init();
                start = new int[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT)];
                end = new int[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT)];
                freq = new int[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_INT)];
                //assert start.length >= ord.length;
                //assert end.length >= ord.length;
                //assert freq.length >= ord.length;
                return ord;
            }

            public override int[] Grow()
            {
                int[] ord = base.Grow();
                if (start.Length < ord.Length)
                {
                    start = ArrayUtil.Grow(start, ord.Length);
                    end = ArrayUtil.Grow(end, ord.Length);
                    freq = ArrayUtil.Grow(freq, ord.Length);
                }
                //assert start.length >= ord.length;
                //assert end.length >= ord.length;
                //assert freq.length >= ord.length;
                return ord;
            }

            public override int[] Clear()
            {
                start = end = null;
                return base.Clear();
            }
        }
    }
}
