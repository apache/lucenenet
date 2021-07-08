using System;
using System.Globalization;
using Float = J2N.Numerics.Single;

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
    /// Language model based on the Jelinek-Mercer smoothing method. From Chengxiang
    /// Zhai and John Lafferty. 2001. A study of smoothing methods for language
    /// models applied to Ad Hoc information retrieval. In Proceedings of the 24th
    /// annual international ACM SIGIR conference on Research and development in
    /// information retrieval (SIGIR '01). ACM, New York, NY, USA, 334-342.
    /// <para>The model has a single parameter, &#955;. According to said paper, the
    /// optimal value depends on both the collection and the query. The optimal value
    /// is around <c>0.1</c> for title queries and <c>0.7</c> for long queries.</para>
    ///
    /// @lucene.experimental
    /// </summary>
    public class LMJelinekMercerSimilarity : LMSimilarity
    {
        /// <summary>
        /// The &#955; parameter. </summary>
        private readonly float lambda;

        /// <summary>
        /// Instantiates with the specified <paramref name="collectionModel"/> and &#955; parameter. </summary>
        public LMJelinekMercerSimilarity(ICollectionModel collectionModel, float lambda)
            : base(collectionModel)
        {
            this.lambda = lambda;
        }

        /// <summary>
        /// Instantiates with the specified &#955; parameter. </summary>
        public LMJelinekMercerSimilarity(float lambda)
        {
            this.lambda = lambda;
        }

        public override float Score(BasicStats stats, float freq, float docLen)
        {
            return stats.TotalBoost * (float)Math.Log(1 + ((1 - lambda) * freq / docLen) / (lambda * ((LMStats)stats).CollectionProbability));
        }

        protected internal override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            if (stats.TotalBoost != 1.0f)
            {
                expl.AddDetail(new Explanation(stats.TotalBoost, "boost"));
            }
            expl.AddDetail(new Explanation(lambda, "lambda"));
            base.Explain(expl, stats, doc, freq, docLen);
        }

        /// <summary>
        /// Returns the &#955; parameter. </summary>
        public virtual float Lambda => lambda;

        public override string GetName()
        {
            // LUCENENET: Intentionally using current culture
            return "Jelinek-Mercer(" + Float.ToString(Lambda) + ")";
        }
    }
}