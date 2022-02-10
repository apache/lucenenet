using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Support.Threading;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

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
            knownSims = new JCG.List<Similarity>(allSims);
            knownSims.Shuffle(random);
        }

        public override float Coord(int overlap, int maxOverlap)
        {
            if (coordType == 0)
                return 1.0f;
            else if (coordType == 1)
                return defaultSim.Coord(overlap, maxOverlap);
            else
                return overlap / ((float)maxOverlap + 1);
        }

        public override float QueryNorm(float sumOfSquaredWeights)
        {
            if (shouldQueryNorm)
                return defaultSim.QueryNorm(sumOfSquaredWeights);
            else
                return 1.0f;
        }

        public override Similarity Get(string field)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(field != null);
                if (!previousMappings.TryGetValue(field, out Similarity sim) || sim is null)
                {
                    sim = knownSims[Math.Max(0, Math.Abs(perFieldSeed ^ field.GetHashCode())) % knownSims.Count];
                    previousMappings[field] = sim;
                }
                return sim;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
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

        internal static IList<Similarity> allSims = LoadAllSims();

        private static IList<Similarity> LoadAllSims() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            var allSims = new JCG.List<Similarity>();
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
            return allSims;
        }

        public override string ToString()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // LUCENENET: Use StringBuilder for better efficiency
                var sb = new StringBuilder();
                sb.Append(nameof(RandomSimilarityProvider));
                sb.Append("(queryNorm=");
                sb.Append(shouldQueryNorm);
                sb.Append(",coord=");
                if (coordType == 0)
                    sb.Append("no");
                else if (coordType == 1)
                    sb.Append("yes");
                else
                    sb.Append("crazy");
                sb.Append("): ");
                sb.AppendFormat(J2N.Text.StringFormatter.InvariantCulture, "{0}", previousMappings);
                return sb.ToString();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }
}