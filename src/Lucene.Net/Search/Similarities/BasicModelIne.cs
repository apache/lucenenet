﻿using System;
using static Lucene.Net.Search.Similarities.SimilarityBase;

namespace Lucene.Net.Search.Similarities
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
    /// Tf-idf model of randomness, based on a mixture of Poisson and inverse
    /// document frequency.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class BasicModelIne : BasicModel
    {
        /// <summary>
        /// Sole constructor: parameter-free </summary>
        public BasicModelIne()
        {
        }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            long N = stats.NumberOfDocuments;
            long F = stats.TotalTermFreq;
            double ne = N * (1 - Math.Pow((N - 1) / (double)N, F));
            return tfn * (float)(Log2((N + 1) / (ne + 0.5)));
        }

        public override string ToString()
        {
            return "I(ne)";
        }
    }
}