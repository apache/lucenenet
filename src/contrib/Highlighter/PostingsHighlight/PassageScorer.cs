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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.PostingsHighlight
{
    public class PassageScorer
    {
        readonly float k1;
        readonly float b;
        readonly float pivot;

        public PassageScorer()
            : this(1.2F, 0.75F, 87F)
        {
        }

        public PassageScorer(float k1, float b, float pivot)
        {
            this.k1 = k1;
            this.b = b;
            this.pivot = pivot;
        }

        public virtual float Weight(int contentLength, int totalTermFreq)
        {
            float numDocs = 1 + contentLength / pivot;
            return (k1 + 1) * (float)Math.Log(1 + (numDocs + 0.5) / (totalTermFreq + 0.5));
        }

        public virtual float Tf(int freq, int passageLen)
        {
            float norm = k1 * ((1 - b) + b * (passageLen / pivot));
            return freq / (freq + norm);
        }

        public virtual float Norm(int passageStart)
        {
            return 1 + 1 / (float)Math.Log(pivot + passageStart);
        }
    }
}
