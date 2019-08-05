/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Index;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Misc
{
    /// <summary>
    /// Test of the SweetSpotSimilarity
    /// </summary>
    public class SweetSpotSimilarityTest : LuceneTestCase
    {
        public static float ComputeAndDecodeNorm(SweetSpotSimilarity decode, Similarity encode, FieldInvertState state)
        {
            return decode.DecodeNormValue(ComputeAndGetNorm(encode, state));
        }

        public static byte ComputeAndGetNorm(Similarity s, FieldInvertState state)
        {
            return (byte)s.ComputeNorm(state);
        }

        [Test]
        public void TestSweetSpotComputeNorm()
        {

            SweetSpotSimilarity ss = new SweetSpotSimilarity();
            ss.SetLengthNormFactors(1, 1, 0.5f, true);

            Similarity d = new DefaultSimilarity();
            Similarity s = ss;


            // base case, should degrade
            FieldInvertState invertState = new FieldInvertState("bogus");
            invertState.Boost = 1.0f;
            for (int i = 1; i < 1000; i++)
            {
                invertState.Length = i;
                assertEquals("base case: i=" + i,
                             ComputeAndGetNorm(d, invertState),
                             ComputeAndGetNorm(s, invertState),
                             0.0f);
            }

            // make a sweet spot

            ss.SetLengthNormFactors(3, 10, 0.5f, true);

            for (int i = 3; i <= 10; i++)
            {
                invertState.Length = i;
                assertEquals("3,10: spot i=" + i,
                             1.0f,
                             ComputeAndDecodeNorm(ss, ss, invertState),
                             0.0f);
            }

            for (int i = 10; i < 1000; i++)
            {
                invertState.Length = (i - 9);
                byte normD = ComputeAndGetNorm(d, invertState);
                invertState.Length = i;
                byte normS = ComputeAndGetNorm(s, invertState);
                assertEquals("3,10: 10<x : i=" + i,
                             normD,
                             normS,
                             0.0f);
            }


            // separate sweet spot for certain fields

            SweetSpotSimilarity ssBar = new SweetSpotSimilarity();
            ssBar.SetLengthNormFactors(8, 13, 0.5f, false);
            SweetSpotSimilarity ssYak = new SweetSpotSimilarity();
            ssYak.SetLengthNormFactors(6, 9, 0.5f, false);
            SweetSpotSimilarity ssA = new SweetSpotSimilarity();
            ssA.SetLengthNormFactors(5, 8, 0.5f, false);
            SweetSpotSimilarity ssB = new SweetSpotSimilarity();
            ssB.SetLengthNormFactors(5, 8, 0.1f, false);

            Similarity sp = new PerFieldSimilarityWrapperHelper(ssBar, ssYak, ssA, ssB, ss);

            invertState = new FieldInvertState("foo");
            invertState.Boost = 1.0f;
            for (int i = 3; i <= 10; i++)
            {
                invertState.Length = i;
                assertEquals("f: 3,10: spot i=" + i,
                             1.0f,
                             ComputeAndDecodeNorm(ss, sp, invertState),
                             0.0f);
            }

            for (int i = 10; i < 1000; i++)
            {
                invertState.Length = (i - 9);
                byte normD = ComputeAndGetNorm(d, invertState);
                invertState.Length = (i);
                byte normS = ComputeAndGetNorm(sp, invertState);
                assertEquals("f: 3,10: 10<x : i=" + i,
                             normD,
                             normS,
                             0.0f);
            }

            invertState = new FieldInvertState("bar");
            invertState.Boost = (1.0f);
            for (int i = 8; i <= 13; i++)
            {
                invertState.Length = (i);
                assertEquals("f: 8,13: spot i=" + i,
                             1.0f,
                             ComputeAndDecodeNorm(ss, sp, invertState),
                             0.0f);
            }

            invertState = new FieldInvertState("yak");
            invertState.Boost = (1.0f);
            for (int i = 6; i <= 9; i++)
            {
                invertState.Length = (i);
                assertEquals("f: 6,9: spot i=" + i,
                             1.0f,
                             ComputeAndDecodeNorm(ss, sp, invertState),
                             0.0f);
            }

            invertState = new FieldInvertState("bar");
            invertState.Boost = (1.0f);
            for (int i = 13; i < 1000; i++)
            {
                invertState.Length = (i - 12);
                byte normD = ComputeAndGetNorm(d, invertState);
                invertState.Length = (i);
                byte normS = ComputeAndGetNorm(sp, invertState);
                assertEquals("f: 8,13: 13<x : i=" + i,
                             normD,
                             normS,
                             0.0f);
            }

            invertState = new FieldInvertState("yak");
            invertState.Boost = (1.0f);
            for (int i = 9; i < 1000; i++)
            {
                invertState.Length = (i - 8);
                byte normD = ComputeAndGetNorm(d, invertState);
                invertState.Length = (i);
                byte normS = ComputeAndGetNorm(sp, invertState);
                assertEquals("f: 6,9: 9<x : i=" + i,
                             normD,
                             normS,
                             0.0f);
            }


            // steepness

            for (int i = 9; i < 1000; i++)
            {
                invertState = new FieldInvertState("a");
                invertState.Boost = (1.0f);
                invertState.Length = (i);
                byte normSS = ComputeAndGetNorm(sp, invertState);
                invertState = new FieldInvertState("b");
                invertState.Boost = (1.0f);
                invertState.Length = (i);
                byte normS = ComputeAndGetNorm(sp, invertState);
                assertTrue("s: i=" + i + " : a=" + normSS +
                           " < b=" + normS,
                           normSS < normS);
            }

        }

        internal class PerFieldSimilarityWrapperHelper : PerFieldSimilarityWrapper
        {
            private readonly Similarity ssBar;
            private readonly Similarity ssYak;
            private readonly Similarity ssA;
            private readonly Similarity ssB;
            private readonly Similarity ss;

            public PerFieldSimilarityWrapperHelper(Similarity ssBar, Similarity ssYak, Similarity ssA, Similarity ssB, Similarity ss)
            {
                this.ssBar = ssBar;
                this.ssYak = ssYak;
                this.ssA = ssA;
                this.ssB = ssB;
                this.ss = ss;
            }

            public override Similarity Get(string field)
            {
                if (field.Equals("bar", StringComparison.Ordinal))
                    return ssBar;
                else if (field.Equals("yak", StringComparison.Ordinal))
                    return ssYak;
                else if (field.Equals("a", StringComparison.Ordinal))
                    return ssA;
                else if (field.Equals("b", StringComparison.Ordinal))
                    return ssB;
                else
                    return ss;
            }
        }

        [Test]
        public void TestSweetSpotTf()
        {
            SweetSpotSimilarity ss = new SweetSpotSimilarity();

            TFIDFSimilarity d = new DefaultSimilarity();
            TFIDFSimilarity s = ss;

            // tf equal

            ss.SetBaselineTfFactors(0.0f, 0.0f);

            for (int i = 1; i < 1000; i++)
            {
                assertEquals("tf: i=" + i,
                             d.Tf(i), s.Tf(i), 0.0f);
            }

            // tf higher

            ss.SetBaselineTfFactors(1.0f, 0.0f);

            for (int i = 1; i < 1000; i++)
            {
                assertTrue("tf: i=" + i + " : d=" + d.Tf(i) +
                           " < s=" + s.Tf(i),
                           d.Tf(i) < s.Tf(i));
            }

            // tf flat

            ss.SetBaselineTfFactors(1.0f, 6.0f);
            for (int i = 1; i <= 6; i++)
            {
                assertEquals("tf flat1: i=" + i, 1.0f, s.Tf(i), 0.0f);
            }
            ss.SetBaselineTfFactors(2.0f, 6.0f);
            for (int i = 1; i <= 6; i++)
            {
                assertEquals("tf flat2: i=" + i, 2.0f, s.Tf(i), 0.0f);
            }
            for (int i = 6; i <= 1000; i++)
            {
                assertTrue("tf: i=" + i + " : s=" + s.Tf(i) +
                           " < d=" + d.Tf(i),
                           s.Tf(i) < d.Tf(i));
            }

            // stupidity
            assertEquals("tf zero", 0.0f, s.Tf(0), 0.0f);
        }

        [Test]
        public void TestHyperbolicSweetSpot()
        {
            SweetSpotSimilarity ss = new HyperbolicSweetSpotSimilarityHelper();

            ss.SetHyperbolicTfFactors(3.3f, 7.7f, Math.E, 5.0f);

            TFIDFSimilarity s = ss;

            for (int i = 1; i <= 1000; i++)
            {
                assertTrue("MIN tf: i=" + i + " : s=" + s.Tf(i),
                           3.3f <= s.Tf(i));
                assertTrue("MAX tf: i=" + i + " : s=" + s.Tf(i),
                           s.Tf(i) <= 7.7f);
            }
            assertEquals("MID tf", 3.3f + (7.7f - 3.3f) / 2.0f, s.Tf(5), 0.00001f);

            // stupidity
            assertEquals("tf zero", 0.0f, s.Tf(0), 0.0f);
        }

        internal class HyperbolicSweetSpotSimilarityHelper : SweetSpotSimilarity
        {
            public override float Tf(float freq)
            {
                return HyperbolicTf(freq);
            }
        }
    }
}
