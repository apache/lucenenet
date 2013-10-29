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

using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index.Memory
{
    public partial class MemoryIndex
    {
        private sealed partial class MemoryIndexReader
        {
            private class MemoryTermsEnum : TermsEnum
            {
                private readonly Info info;
                private readonly BytesRef br = new BytesRef();
                int termUpto = -1;

                private readonly MemoryIndex index;

                public MemoryTermsEnum(MemoryIndex index, Info info)
                {
                    this.index = index;
                    this.info = info;
                    info.SortTerms();
                }

                private int BinarySearch(BytesRef b, BytesRef bytesRef, int low,
                    int high, BytesRefHash hash, int[] ords, IComparer<BytesRef> comparator)
                {
                    int mid = 0;
                    while (low <= high)
                    {
                        mid = Number.URShift((low + high), 1);
                        hash.Get(ords[mid], bytesRef);
                        int cmp = comparator.Compare(bytesRef, b);
                        if (cmp < 0)
                        {
                            low = mid + 1;
                        }
                        else if (cmp > 0)
                        {
                            high = mid - 1;
                        }
                        else
                        {
                            return mid;
                        }
                    }
                    //assert comparator.compare(bytesRef, b) != 0;
                    return -(low + 1);
                }

                public override bool SeekExact(BytesRef text, bool useCache)
                {
                    termUpto = BinarySearch(text, br, 0, info.terms.Size - 1, info.terms, info.sortedTerms, BytesRef.UTF8SortedAsUnicodeComparer);
                    return termUpto >= 0;
                }

                public override SeekStatus SeekCeil(BytesRef text, bool useCache)
                {
                    termUpto = BinarySearch(text, br, 0, info.terms.Size - 1, info.terms, info.sortedTerms, BytesRef.UTF8SortedAsUnicodeComparer);
                    if (termUpto < 0)
                    { // not found; choose successor
                        termUpto = -termUpto - 1;
                        if (termUpto >= info.terms.Size)
                        {
                            return SeekStatus.END;
                        }
                        else
                        {
                            info.terms.Get(info.sortedTerms[termUpto], br);
                            return SeekStatus.NOT_FOUND;
                        }
                    }
                    else
                    {
                        return SeekStatus.FOUND;
                    }
                }

                public override void SeekExact(long ord)
                {
                    //assert ord < info.terms.size();
                    termUpto = (int)ord;
                }

                public override BytesRef Next()
                {
                    termUpto++;
                    if (termUpto >= info.terms.Size)
                    {
                        return null;
                    }
                    else
                    {
                        info.terms.Get(info.sortedTerms[termUpto], br);
                        return br;
                    }
                }

                public override BytesRef Term
                {
                    get
                    {
                        return br;
                    }
                }

                public override long Ord
                {
                    get { return termUpto; }
                }

                public override int DocFreq
                {
                    get { return 1; }
                }

                public override long TotalTermFreq
                {
                    get { return info.sliceArray.freq[info.sortedTerms[termUpto]]; }
                }

                public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
                {
                    if (reuse == null || !(reuse is MemoryDocsEnum))
                    {
                        reuse = new MemoryDocsEnum();
                    }
                    return ((MemoryDocsEnum)reuse).Reset(liveDocs, info.sliceArray.freq[info.sortedTerms[termUpto]]);
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
                {
                    if (reuse == null || !(reuse is MemoryDocsAndPositionsEnum))
                    {
                        reuse = new MemoryDocsAndPositionsEnum(index);
                    }
                    int ord = info.sortedTerms[termUpto];
                    return ((MemoryDocsAndPositionsEnum)reuse).Reset(liveDocs, info.sliceArray.start[ord], info.sliceArray.end[ord], info.sliceArray.freq[ord]);
                }

                public override IComparer<BytesRef> Comparator
                {
                    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
                }

                public override void SeekExact(BytesRef term, TermState state)
                {
                    //assert state != null;
                    this.SeekExact(((OrdTermState)state).ord);
                }

                public override TermState TermState
                {
                    get
                    {
                        OrdTermState ts = new OrdTermState();
                        ts.ord = termUpto;
                        return ts;
                    }
                }
            }
        }
    }
}
