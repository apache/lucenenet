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
    /// <p>The DFR scoring formula is composed of three separate components: the
    /// <em>basic model</em>, the <em>aftereffect</em> and an additional
    /// <em>normalization</em> component, represented by the classes
    /// {@code BasicModel}, {@code AfterEffect} and {@code Normalization},
    /// respectively. The names of these classes were chosen to match the names of
    /// their counterparts in the Terrier IR engine.</p>
    /// <p>To construct a DFRSimilarity, you must specify the implementations for
    /// all three components of DFR:
    /// <ol>
    ///    <li><seealso cref="BasicModel"/>: Basic model of information content:
    ///        <ul>
    ///           <li><seealso cref="BasicModelBE"/>: Limiting form of Bose-Einstein
    ///           <li><seealso cref="BasicModelG"/>: Geometric approximation of Bose-Einstein
    ///           <li><seealso cref="BasicModelP"/>: Poisson approximation of the Binomial
    ///           <li><seealso cref="BasicModelD"/>: Divergence approximation of the Binomial
    ///           <li><seealso cref="BasicModelIn"/>: Inverse document frequency
    ///           <li><seealso cref="BasicModelIne"/>: Inverse expected document
    ///               frequency [mixture of Poisson and IDF]
    ///           <li><seealso cref="BasicModelIF"/>: Inverse term frequency
    ///               [approximation of I(ne)]
    ///        </ul>
    ///    <li><seealso cref="AfterEffect"/>: First normalization of information
    ///        gain:
    ///        <ul>
    ///           <li><seealso cref="AfterEffectL"/>: Laplace's law of succession
    ///           <li><seealso cref="AfterEffectB"/>: Ratio of two Bernoulli processes
    ///           <li><seealso cref="NoAfterEffect"/>: no first normalization
    ///        </ul>
    ///    <li><seealso cref="Normalization"/>: Second (length) normalization:
    ///        <ul>
    ///           <li><seealso cref="NormalizationH1"/>: Uniform distribution of term
    ///               frequency
    ///           <li><seealso cref="NormalizationH2"/>: term frequency density inversely
    ///               related to length
    ///           <li><seealso cref="NormalizationH3"/>: term frequency normalization
    ///               provided by Dirichlet prior
    ///           <li><seealso cref="NormalizationZ"/>: term frequency normalization provided
    ///                by a Zipfian relation
    ///           <li><seealso cref="NoNormalization"/>: no second normalization
    ///        </ul>
    /// </ol>
    /// <p>Note that <em>qtf</em>, the multiplicity of term-occurrence in the query,
    /// is not handled by this implementation.</p> </summary>
    /// <seealso cref= BasicModel </seealso>
    /// <seealso cref= AfterEffect </seealso>
    /// <seealso cref= Normalization
    /// @lucene.experimental </seealso>
    public class DFRSimilarity : SimilarityBase
    {
        /// <summary>
        /// The basic model for information content. </summary>
        protected internal readonly BasicModel BasicModel_Renamed; // LUCENENET TODO: Rename

        /// <summary>
        /// The first normalization of the information content. </summary>
        protected internal readonly AfterEffect AfterEffect_Renamed; // LUCENENET TODO: Rename

        /// <summary>
        /// The term frequency normalization. </summary>
        protected internal readonly Normalization Normalization_Renamed; // LUCENENET TODO: Rename

        /// <summary>
        /// Creates DFRSimilarity from the three components.
        /// <p>
        /// Note that <code>null</code> values are not allowed:
        /// if you want no normalization or after-effect, instead pass
        /// <seealso cref="NoNormalization"/> or <seealso cref="NoAfterEffect"/> respectively. </summary>
        /// <param name="basicModel"> Basic model of information content </param>
        /// <param name="afterEffect"> First normalization of information gain </param>
        /// <param name="normalization"> Second (length) normalization </param>
        public DFRSimilarity(BasicModel basicModel, AfterEffect afterEffect, Normalization normalization)
        {
            if (basicModel == null || afterEffect == null || normalization == null)
            {
                throw new System.NullReferenceException("null parameters not allowed.");
            }
            this.BasicModel_Renamed = basicModel;
            this.AfterEffect_Renamed = afterEffect;
            this.Normalization_Renamed = normalization;
        }

        public override float Score(BasicStats stats, float freq, float docLen)
        {
            float tfn = Normalization_Renamed.Tfn(stats, freq, docLen);
            return stats.TotalBoost * BasicModel_Renamed.Score(stats, tfn) * AfterEffect_Renamed.Score(stats, tfn);
        }

        protected internal override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            if (stats.TotalBoost != 1.0f)
            {
                expl.AddDetail(new Explanation(stats.TotalBoost, "boost"));
            }

            Explanation normExpl = Normalization_Renamed.Explain(stats, freq, docLen);
            float tfn = normExpl.Value;
            expl.AddDetail(normExpl);
            expl.AddDetail(BasicModel_Renamed.Explain(stats, tfn));
            expl.AddDetail(AfterEffect_Renamed.Explain(stats, tfn));
        }

        public override string ToString()
        {
            return "DFR " + BasicModel_Renamed.ToString() + AfterEffect_Renamed.ToString() + Normalization_Renamed.ToString();
        }

        /// <summary>
        /// Returns the basic model of information content
        /// </summary>
        public virtual BasicModel BasicModel
        {
            get
            {
                return BasicModel_Renamed;
            }
        }

        /// <summary>
        /// Returns the first normalization
        /// </summary>
        public virtual AfterEffect AfterEffect
        {
            get
            {
                return AfterEffect_Renamed;
            }
        }

        /// <summary>
        /// Returns the second normalization
        /// </summary>
        public virtual Normalization Normalization
        {
            get
            {
                return Normalization_Renamed;
            }
        }
    }
}