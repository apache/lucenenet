using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using Debug = Lucene.Net.Diagnostics.Debug; // LUCENENET NOTE: We cannot use System.Diagnostics.Debug because those calls will be optimized out of the release!

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

    using AfterEffect = Lucene.Net.Search.Similarities.AfterEffect;
    using AfterEffectB = Lucene.Net.Search.Similarities.AfterEffectB;
    using AfterEffectL = Lucene.Net.Search.Similarities.AfterEffectL;
    using BasicModel = Lucene.Net.Search.Similarities.BasicModel;
    using BasicModelG = Lucene.Net.Search.Similarities.BasicModelG;
    using BasicModelIF = Lucene.Net.Search.Similarities.BasicModelIF;
    using BasicModelIn = Lucene.Net.Search.Similarities.BasicModelIn;
    using BasicModelIne = Lucene.Net.Search.Similarities.BasicModelIne;
    using BM25Similarity = Lucene.Net.Search.Similarities.BM25Similarity;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using DFRSimilarity = Lucene.Net.Search.Similarities.DFRSimilarity;
    using Distribution = Lucene.Net.Search.Similarities.Distribution;
    using DistributionLL = Lucene.Net.Search.Similarities.DistributionLL;
    using DistributionSPL = Lucene.Net.Search.Similarities.DistributionSPL;
    using IBSimilarity = Lucene.Net.Search.Similarities.IBSimilarity;
    using Lambda = Lucene.Net.Search.Similarities.Lambda;
    using LambdaDF = Lucene.Net.Search.Similarities.LambdaDF;
    using LambdaTTF = Lucene.Net.Search.Similarities.LambdaTTF;
    using LMJelinekMercerSimilarity = Lucene.Net.Search.Similarities.LMJelinekMercerSimilarity;
    using Normalization = Lucene.Net.Search.Similarities.Normalization;
    using NormalizationH1 = Lucene.Net.Search.Similarities.NormalizationH1;
    using NormalizationH2 = Lucene.Net.Search.Similarities.NormalizationH2;
    using NormalizationH3 = Lucene.Net.Search.Similarities.NormalizationH3;
    using NormalizationZ = Lucene.Net.Search.Similarities.NormalizationZ;
    using PerFieldSimilarityWrapper = Lucene.Net.Search.Similarities.PerFieldSimilarityWrapper;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;

    /// <summary>
    /// Similarity implementation that randomizes Similarity implementations
    /// per-field.
    /// <para/>
    /// The choices are 'sticky', so the selected algorithm is always used
    /// for the same field.
    /// </summary>
    public class RandomSimilarityProvider : PerFieldSimilarityWrapper
    {
        internal readonly DefaultSimilarity defaultSim = new DefaultSimilarity();
        internal readonly IList<Similarity> knownSims;
        internal IDictionary<string, Similarity> previousMappings = new Dictionary<string, Similarity>();
        internal readonly int perFieldSeed;
        internal readonly int coordType; // 0 = no coord, 1 = coord, 2 = crazy coord
        internal readonly bool shouldQueryNorm;

        public RandomSimilarityProvider(Random random)
        {
            perFieldSeed = random.Next();
            coordType = random.Next(3);
            shouldQueryNorm = random.NextBoolean();
            knownSims = new List<Similarity>(allSims);
            Collections.Shuffle(knownSims, random);
        }

        public override float Coord(int overlap, int maxOverlap)
        {
            if (coordType == 0)
            {
                return 1.0f;
            }
            else if (coordType == 1)
            {
                return defaultSim.Coord(overlap, maxOverlap);
            }
            else
            {
                return overlap / ((float)maxOverlap + 1);
            }
        }

        public override float QueryNorm(float sumOfSquaredWeights)
        {
            if (shouldQueryNorm)
            {
                return defaultSim.QueryNorm(sumOfSquaredWeights);
            }
            else
            {
                return 1.0f;
            }
        }

        public override Similarity Get(string field)
        {
            lock (this)
            {
                Debug.Assert(field != null);
                Similarity sim;
                if (!previousMappings.TryGetValue(field, out sim) || sim == null)
                {
                    sim = knownSims[Math.Max(0, Math.Abs(perFieldSeed ^ field.GetHashCode())) % knownSims.Count];
                    previousMappings[field] = sim;
                }
                return sim;
            }
        }

        // all the similarities that we rotate through
        /// <summary>
        /// The DFR basic models to test. </summary>
        internal static BasicModel[] BASIC_MODELS = new BasicModel[] { new BasicModelG(), new BasicModelIF(), new BasicModelIn(), new BasicModelIne() };

        /// <summary>
        /// The DFR aftereffects to test. </summary>
        internal static AfterEffect[] AFTER_EFFECTS = new AfterEffect[] { new AfterEffectB(), new AfterEffectL(), new AfterEffect.NoAfterEffect() };

        /// <summary>
        /// The DFR normalizations to test. </summary>
        internal static Normalization[] NORMALIZATIONS = new Normalization[] { new NormalizationH1(), new NormalizationH2(), new NormalizationH3(), new NormalizationZ() };

        /// <summary>
        /// The distributions for IB. </summary>
        internal static Distribution[] DISTRIBUTIONS = new Distribution[] { new DistributionLL(), new DistributionSPL() };

        /// <summary>
        /// Lambdas for IB. </summary>
        internal static Lambda[] LAMBDAS = new Lambda[] { new LambdaDF(), new LambdaTTF() };

        internal static IList<Similarity> allSims;

        static RandomSimilarityProvider()
        {
            allSims = new List<Similarity>();
            allSims.Add(new DefaultSimilarity());
            allSims.Add(new BM25Similarity());
            foreach (BasicModel basicModel in BASIC_MODELS)
            {
                foreach (AfterEffect afterEffect in AFTER_EFFECTS)
                {
                    foreach (Normalization normalization in NORMALIZATIONS)
                    {
                        allSims.Add(new DFRSimilarity(basicModel, afterEffect, normalization));
                    }
                }
            }
            foreach (Distribution distribution in DISTRIBUTIONS)
            {
                foreach (Lambda lambda in LAMBDAS)
                {
                    foreach (Normalization normalization in NORMALIZATIONS)
                    {
                        allSims.Add(new IBSimilarity(distribution, lambda, normalization));
                    }
                }
            }
            /* TODO: enable Dirichlet
            allSims.Add(new LMDirichletSimilarity()); */
            allSims.Add(new LMJelinekMercerSimilarity(0.1f));
            allSims.Add(new LMJelinekMercerSimilarity(0.7f));
        }

        public override string ToString()
        {
            lock (this)
            {
                string coordMethod;
                if (coordType == 0)
                {
                    coordMethod = "no";
                }
                else if (coordType == 1)
                {
                    coordMethod = "yes";
                }
                else
                {
                    coordMethod = "crazy";
                }
                return "RandomSimilarityProvider(queryNorm=" + shouldQueryNorm + ",coord=" + coordMethod + "): " + Arrays.ToString(previousMappings);
            }
        }
    }
}