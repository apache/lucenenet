namespace Lucene.Net.Search
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

    using Lucene.Net.Util;

    internal sealed class PhraseQueue : PriorityQueue<PhrasePositions>
    {
        internal PhraseQueue(int size)
            : base(size)
        {
        }

        protected internal override sealed bool LessThan(PhrasePositions pp1, PhrasePositions pp2)
        {
            if (pp1.Doc == pp2.Doc)
            {
                if (pp1.Position == pp2.Position)
                // same doc and pp.position, so decide by actual term positions.
                // rely on: pp.position == tp.position - offset.
                {
                    if (pp1.Offset == pp2.Offset)
                    {
                        return pp1.Ord < pp2.Ord;
                    }
                    else
                    {
                        return pp1.Offset < pp2.Offset;
                    }
                }
                else
                {
                    return pp1.Position < pp2.Position;
                }
            }
            else
            {
                return pp1.Doc < pp2.Doc;
            }
        }
    }
}