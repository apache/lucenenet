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
        private class MemoryDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private int posUpto; // for assert
            private bool hasNext;
            private IBits liveDocs;
            private int doc = -1;
            private IntBlockPool.SliceReader sliceReader;
            private int freq;
            private int startOffset;
            private int endOffset;

            private readonly MemoryIndex index;

            public MemoryDocsAndPositionsEnum(MemoryIndex index)
            {
                this.index = index; // .NET: needed for storeOffsets access
                this.sliceReader = new IntBlockPool.SliceReader(index.intBlockPool);
            }

            public DocsAndPositionsEnum Reset(IBits liveDocs, int start, int end, int freq)
            {
                this.liveDocs = liveDocs;
                this.sliceReader.Reset(start, end);
                posUpto = 0; // for assert
                hasNext = true;
                doc = -1;
                this.freq = freq;
                return this;
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                if (hasNext && (liveDocs == null || liveDocs[0]))
                {
                    hasNext = false;
                    return doc = 0;
                }
                else
                {
                    return doc = NO_MORE_DOCS;
                }
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            public override int Freq
            {
                get { return freq; }
            }

            public override int NextPosition()
            {
                //assert posUpto++ < freq;
                //assert !sliceReader.endOfSlice() : " stores offsets : " + startOffset;
                if (index.storeOffsets)
                {
                    int pos = sliceReader.ReadInt();
                    startOffset = sliceReader.ReadInt();
                    endOffset = sliceReader.ReadInt();
                    return pos;
                }
                else
                {
                    return sliceReader.ReadInt();
                }
            }

            public override int StartOffset
            {
                get { return startOffset; }
            }

            public override int EndOffset
            {
                get { return endOffset; }
            }

            public override BytesRef Payload
            {
                get { return null; }
            }

            public override long Cost
            {
                get { return 1; }
            }
        }
    }
}
