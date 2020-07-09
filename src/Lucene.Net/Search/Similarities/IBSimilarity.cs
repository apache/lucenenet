using Lucene.Net.Support;

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
    /// Provides a framework for the family of information-based models, as described
    /// in St&#201;phane Clinchant and Eric Gaussier. 2010. Information-based
    /// models for ad hoc IR. In Proceeding of the 33rd international ACM SIGIR
    /// conference on Research and development in information retrieval (SIGIR '10).
    /// ACM, New York, NY, USA, 234-241.
    /// <para>The retrieval function is of the form <em>RSV(q, d) = &#8721;
    /// -x<sup>q</sup><sub>w</sub> log Prob(X<sub>w</sub> &gt;=
    /// t<sup>d</sup><sub>w</sub> | &#955;<sub>w</sub>)</em>, where
    /// <list type="bullet">
    ///     <item><description><em>x<sup>q</sup><sub>w</sub></em> is the query boost;</description></item>
    ///     <item><description><em>X<sub>w</sub></em> is a random variable that counts the occurrences
    ///         of word <em>w</em>;</description></item>
    ///     <item><description><em>t<sup>d</sup><sub>w</sub></em> is the normalized term frequency;</description></item>
    ///     <item><description><em>&#955;<sub>w</sub></em> is a parameter.</description></item>
    /// </list>
    /// </para>
    /// <para>The framework described in the paper has many similarities to the DFR
    /// framework (see <see cref="DFRSimilarity"/>). It is possible that the two
    /// Similarities will be merged at one point.</para>
    /// <para>To construct an <see cref="IBSimilarity"/>, you must specify the implementations for
    /// all three components of the Information-Based model.
    /// <list type="table">
    ///     <listheader>
    ///         <term>Component</term>
    ///         <term>Implementations</term>
    ///     </listheader>
    ///     <item>
    ///         <term><see cref="Distribution"/>: Probabilistic distribution used to
    ///             model term occurrence</term>
    ///         <term>
    ///             <list type="bullet">
    ///                 <item><description><see cref="DistributionLL"/>: Log-logistic</description></item>
    ///                 <item><description><see cref="DistributionLL"/>: Smoothed power-law</description></item>
    ///             </list>
    ///         </term>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Lambda"/>: &#955;<sub>w</sub> parameter of the
    ///             probability distribution</term>
    ///         <term>
    ///             <list type="bullet">
    ///                 <item><description><see cref="LambdaDF"/>: <c>N<sub>w</sub>/N</c> or average
    ///                     number of documents where w occurs</description></item>
    ///                 <item><description><see cref="LambdaTTF"/>: <c>F<sub>w</sub>/N</c> or
    ///                     average number of occurrences of w in the collection</description></item>
    ///             </list>
    ///         </term>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Normalization"/>: Term frequency normalization</term>
    ///         <term>Any supported DFR normalization (listed in
    ///                      <see cref="DFRSimilarity"/>)
    ///         </term>
    ///     </item>
    /// </list>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="DFRSimilarity"/>
    [ExceptionToClassNameConvention]
    public class IBSimilarity : SimilarityBase
    {
        /// <summary>
        /// The probabilistic distribution used to model term occurrence. </summary>
        protected internal readonly Distribution m_distribution;

        /// <summary>
        /// The <em>lambda (&#955;<sub>w</sub>)</em> parameter. </summary>
        protected internal readonly Lambda m_lambda;

        /// <summary>
        /// The term frequency normalization. </summary>
        protected internal readonly Normalization m_normalization;

        /// <summary>
        /// Creates IBSimilarity from the three components.
        /// <para/>
        /// Note that <c>null</c> values are not allowed:
        /// if you want no normalization, instead pass
        /// <see cref="Normalization.NoNormalization"/>. </summary>
        /// <param name="distribution"> probabilistic distribution modeling term occurrence </param>
        /// <param name="lambda"> distribution's &#955;<sub>w</sub> parameter </param>
        /// <param name="normalization"> term frequency normalization </param>
        public IBSimilarity(Distribution distribution, Lambda lambda, Normalization normalization)
        {
            this.m_distribution = distribution;
            this.m_lambda = lambda;
            this.m_normalization = normalization;
        }

        public override float Score(BasicStats stats, float freq, float docLen)
        {
            return stats.TotalBoost * m_distribution.Score(stats, m_normalization.Tfn(stats, freq, docLen), m_lambda.CalculateLambda(stats));
        }

        protected internal override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            if (stats.TotalBoost != 1.0f)
            {
                expl.AddDetail(new Explanation(stats.TotalBoost, "boost"));
            }
            Explanation normExpl = m_normalization.Explain(stats, freq, docLen);
            Explanation lambdaExpl = m_lambda.Explain(stats);
            expl.AddDetail(normExpl);
            expl.AddDetail(lambdaExpl);
            expl.AddDetail(m_distribution.Explain(stats, normExpl.Value, lambdaExpl.Value));
        }

        /// <summary>
        /// The name of IB methods follow the pattern
        /// <c>IB &lt;distribution&gt; &lt;lambda&gt;&lt;normalization&gt;</c>. The name of the
        /// distribution is the same as in the original paper; for the names of lambda
        /// parameters, refer to the doc of the <see cref="Similarities.Lambda"/> classes.
        /// </summary>
        public override string ToString()
        {
            return "IB " + m_distribution.ToString() + "-" + m_lambda.ToString() + m_normalization.ToString();
        }

        /// <summary>
        /// Returns the distribution
        /// </summary>
        public virtual Distribution Distribution => m_distribution;

        /// <summary>
        /// Returns the distribution's lambda parameter
        /// </summary>
        public virtual Lambda Lambda => m_lambda;

        /// <summary>
        /// Returns the term frequency normalization
        /// </summary>
        public virtual Normalization Normalization => m_normalization;
    }
}