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
    /// in St&eacute;phane Clinchant and Eric Gaussier. 2010. Information-based
    /// models for ad hoc IR. In Proceeding of the 33rd international ACM SIGIR
    /// conference on Research and development in information retrieval (SIGIR '10).
    /// ACM, New York, NY, USA, 234-241.
    /// <p>The retrieval function is of the form <em>RSV(q, d) = &sum;
    /// -x<sup>q</sup><sub>w</sub> log Prob(X<sub>w</sub> &gt;=
    /// t<sup>d</sup><sub>w</sub> | &lambda;<sub>w</sub>)</em>, where
    /// <ul>
    ///   <li><em>x<sup>q</sup><sub>w</sub></em> is the query boost;</li>
    ///   <li><em>X<sub>w</sub></em> is a random variable that counts the occurrences
    ///   of word <em>w</em>;</li>
    ///   <li><em>t<sup>d</sup><sub>w</sub></em> is the normalized term frequency;</li>
    ///   <li><em>&lambda;<sub>w</sub></em> is a parameter.</li>
    /// </ul>
    /// </p>
    /// <p>The framework described in the paper has many similarities to the DFR
    /// framework (see <seealso cref="DFRSimilarity"/>). It is possible that the two
    /// Similarities will be merged at one point.</p>
    /// <p>To construct an IBSimilarity, you must specify the implementations for
    /// all three components of the Information-Based model.
    /// <ol>
    ///     <li><seealso cref="Distribution"/>: Probabilistic distribution used to
    ///         model term occurrence
    ///         <ul>
    ///             <li><seealso cref="DistributionLL"/>: Log-logistic</li>
    ///             <li><seealso cref="DistributionLL"/>: Smoothed power-law</li>
    ///         </ul>
    ///     </li>
    ///     <li><seealso cref="Lambda"/>: &lambda;<sub>w</sub> parameter of the
    ///         probability distribution
    ///         <ul>
    ///             <li><seealso cref="LambdaDF"/>: <code>N<sub>w</sub>/N</code> or average
    ///                 number of documents where w occurs</li>
    ///             <li><seealso cref="LambdaTTF"/>: <code>F<sub>w</sub>/N</code> or
    ///                 average number of occurrences of w in the collection</li>
    ///         </ul>
    ///     </li>
    ///     <li><seealso cref="Normalization"/>: Term frequency normalization
    ///         <blockquote>Any supported DFR normalization (listed in
    ///                      <seealso cref="DFRSimilarity"/>)</blockquote>
    ///     </li>
    /// </ol>
    /// <p> </summary>
    /// <seealso cref= DFRSimilarity
    /// @lucene.experimental  </seealso>
    public class IBSimilarity : SimilarityBase
    {
        /// <summary>
        /// The probabilistic distribution used to model term occurrence. </summary>
        protected internal readonly Distribution m_distribution;

        /// <summary>
        /// The <em>lambda (&lambda;<sub>w</sub>)</em> parameter. </summary>
        protected internal readonly Lambda m_lambda;

        /// <summary>
        /// The term frequency normalization. </summary>
        protected internal readonly Normalization m_normalization;

        /// <summary>
        /// Creates IBSimilarity from the three components.
        /// <p>
        /// Note that <code>null</code> values are not allowed:
        /// if you want no normalization, instead pass
        /// <seealso cref="NoNormalization"/>. </summary>
        /// <param name="distribution"> probabilistic distribution modeling term occurrence </param>
        /// <param name="lambda"> distribution's &lambda;<sub>w</sub> parameter </param>
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
        /// {@code IB <distribution> <lambda><normalization>}. The name of the
        /// distribution is the same as in the original paper; for the names of lambda
        /// parameters, refer to the javadoc of the <seealso cref="Lambda"/> classes.
        /// </summary>
        public override string ToString()
        {
            return "IB " + m_distribution.ToString() + "-" + m_lambda.ToString() + m_normalization.ToString();
        }

        /// <summary>
        /// Returns the distribution
        /// </summary>
        public virtual Distribution Distribution
        {
            get
            {
                return m_distribution;
            }
        }

        /// <summary>
        /// Returns the distribution's lambda parameter
        /// </summary>
        public virtual Lambda Lambda
        {
            get
            {
                return m_lambda;
            }
        }

        /// <summary>
        /// Returns the term frequency normalization
        /// </summary>
        public virtual Normalization Normalization
        {
            get
            {
                return m_normalization;
            }
        }
    }
}