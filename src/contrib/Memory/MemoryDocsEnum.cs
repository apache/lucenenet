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
        private class MemoryDocsEnum : DocsEnum
        {
            private bool hasNext;
            private IBits liveDocs;
            private int doc = -1;
            private int freq;

            public DocsEnum Reset(IBits liveDocs, int freq)
            {
                this.liveDocs = liveDocs;
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

            public override long Cost
            {
                get { return 1; }
            }
        }
    }
}
