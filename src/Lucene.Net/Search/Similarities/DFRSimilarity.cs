using System;

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
    /// Implements the <em>divergence from randomness (DFR)</em> framework
    /// introduced in Gianni Amati and Cornelis Joost Van Rijsbergen. 2002.
    /// Probabilistic models of information retrieval based on measuring the
    /// divergence from randomness. ACM Trans. Inf. Syst. 20, 4 (October 2002),
    /// 357-389.
    /// <para>The DFR scoring formula is composed of three separate components: the
    /// <em>basic model</em>, the <em>aftereffect</em> and an additional
    /// <em>normalization</em> component, represented by the classes
    /// <see cref="Similarities.BasicModel"/>, <see cref="Similarities.AfterEffect"/> and <see cref="Similarities.Normalization"/>,
    /// respectively. The names of these classes were chosen to match the names of
    /// their counterparts in the Terrier IR engine.</para>
    /// <para>To construct a <see cref="DFRSimilarity"/>, you must specify the implementations for
    /// all three components of DFR:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Component</term>
    ///         <term>Implementations</term>
    ///     </listheader>
    ///     <item>
    ///         <term><see cref="Similarities.BasicModel"/>: Basic model of information content:</term>
    ///         <term>
    ///             <list type="bullet">
    ///                 <item><description><see cref="BasicModelBE"/>: Limiting form of Bose-Einstein</description></item>
    ///                 <item><description><see cref="BasicModelG"/>: Geometric approximation of Bose-Einstein</description></item>
    ///                 <item><description><see cref="BasicModelP"/>: Poisson approximation of the Binomial</description></item>
    ///                 <item><description><see cref="BasicModelD"/>: Divergence approximation of the Binomial</description></item>
    ///                 <item><description><see cref="BasicModelIn"/>: Inverse document frequency</description></item>
    ///                 <item><description><see cref="BasicModelIne"/>: Inverse expected document frequency [mixture of Poisson and IDF]</description></item>
    ///                 <item><description><see cref="BasicModelIF"/>: Inverse term frequency [approximation of I(ne)]</description></item>
    ///             </list>
    ///         </term>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Similarities.AfterEffect"/>: First normalization of information gain:</term>
    ///         <term>
    ///             <list type="bullet">
    ///                 <item><description><see cref="AfterEffectL"/>: Laplace's law of succession</description></item>
    ///                 <item><description><see cref="AfterEffectB"/>: Ratio of two Bernoulli processes</description></item>
    ///                 <item><description><see cref="AfterEffect.NoAfterEffect"/>: no first normalization</description></item>
    ///             </list>
    ///         </term>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Similarities.Normalization"/>: Second (length) normalization:</term>
    ///         <term>
    ///             <list type="bullet">
    ///                 <item><description><see cref="NormalizationH1"/>: Uniform distribution of term frequency</description></item>
    ///                 <item><description><see cref="NormalizationH2"/>: term frequency density inversely related to length</description></item>
    ///                 <item><description><see cref="NormalizationH3"/>: term frequency normalization provided by Dirichlet prior</description></item>
    ///                 <item><description><see cref="NormalizationZ"/>: term frequency normalization provided by a Zipfian relation</description></item>
    ///                 <item><description><see cref="Normalization.NoNormalization"/>: no second normalization</description></item>
    ///             </list>
    ///         </term>
    ///     </item>
    /// </list>
    /// 
    /// </para>
    /// <para>Note that <em>qtf</em>, the multiplicity of term-occurrence in the query,
    /// is not handled by this implementation.
    /// </para> 
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="Similarities.BasicModel"/>
    /// <seealso cref="Similarities.AfterEffect"/>
    /// <seealso cref="Similarities.Normalization"/>
    public class DFRSimilarity : SimilarityBase
    {
        /// <summary>
        /// The basic model for information content. </summary>
        protected internal readonly BasicModel m_basicModel;

        /// <summary>
        /// The first normalization of the information content. </summary>
        protected internal readonly AfterEffect m_afterEffect;

        /// <summary>
        /// The term frequency normalization. </summary>
        protected internal readonly Normalization m_normalization;

        /// <summary>
        /// Creates DFRSimilarity from the three components.
        /// <para/>
        /// Note that <c>null</c> values are not allowed:
        /// if you want no normalization or after-effect, instead pass
        /// <see cref="Normalization.NoNormalization"/> or <see cref="AfterEffect.NoAfterEffect"/> respectively. </summary>
        /// <param name="basicModel"> Basic model of information content </param>
        /// <param name="afterEffect"> First normalization of information gain </param>
        /// <param name="normalization"> Second (length) normalization </param>
        /// <exception cref="ArgumentNullException"><paramref name="basicModel"/>, <paramref name="afterEffect"/>,
        /// or <paramref name="normalization"/> is <c>null</c>.</exception>
        public DFRSimilarity(BasicModel basicModel, AfterEffect afterEffect, Normalization normalization)
        {
            // LUCENENET: Changed guard clauses from NullPointerException to ArgumentNullException
            this.m_basicModel = basicModel ?? throw new ArgumentNullException(nameof(basicModel));
            this.m_afterEffect = afterEffect ?? throw new ArgumentNullException(nameof(afterEffect));
            this.m_normalization = normalization ?? throw new ArgumentNullException(nameof(normalization));
        }

        public override float Score(BasicStats stats, float freq, float docLen)
        {
            float tfn = m_normalization.Tfn(stats, freq, docLen);
            return stats.TotalBoost * m_basicModel.Score(stats, tfn) * m_afterEffect.Score(stats, tfn);
        }

        protected internal override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            if (stats.TotalBoost != 1.0f)
            {
                expl.AddDetail(new Explanation(stats.TotalBoost, "boost"));
            }

            Explanation normExpl = m_normalization.Explain(stats, freq, docLen);
            float tfn = normExpl.Value;
            expl.AddDetail(normExpl);
            expl.AddDetail(m_basicModel.Explain(stats, tfn));
            expl.AddDetail(m_afterEffect.Explain(stats, tfn));
        }

        public override string ToString()
        {
            return "DFR " + m_basicModel.ToString() + m_afterEffect.ToString() + m_normalization.ToString();
        }

        /// <summary>
        /// Returns the basic model of information content
        /// </summary>
        public virtual BasicModel BasicModel => m_basicModel;

        /// <summary>
        /// Returns the first normalization
        /// </summary>
        public virtual AfterEffect AfterEffect => m_afterEffect;

        /// <summary>
        /// Returns the second normalization
        /// </summary>
        public virtual Normalization Normalization => m_normalization;
    }
}