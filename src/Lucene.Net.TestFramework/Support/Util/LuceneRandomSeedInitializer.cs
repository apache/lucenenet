using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Linq;

namespace Lucene.Net.Util
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

    internal class LuceneRandomSeedInitializer
    {
        const string RandomSeed = "RandomSeed";

        #region Messages

        const string RANDOM_SEED_PARAMS_MSG =
            "RandomSeed parameter must be a valid int value or the word 'random'.";

        #endregion

        private Random random;

        private bool TryGetRandomSeedFromContext(Test test, out int seed)
        {
            var randomSeedAttribute = (RandomSeedAttribute)test.TypeInfo.Assembly
                .GetCustomAttributes(typeof(RandomSeedAttribute), inherit: false)
                .FirstOrDefault();
            if (randomSeedAttribute != null)
            {
                seed = randomSeedAttribute.RandomSeed;
            }
            // HACK: NUnit3TestAdapter seems to be supplying the seed from nunit_random_seed.tmp whether
            // or not there is a RandomSeed set by the user. This is a workaorund until that is fixed.
            else if (NUnit.Framework.TestContext.Parameters.Exists(RandomSeed))
            {
                if (NUnit.Framework.TestContext.Parameters[RandomSeed].Equals("random", StringComparison.OrdinalIgnoreCase))
                    seed = -1;
                else if (int.TryParse(NUnit.Framework.TestContext.Parameters[RandomSeed], out int initialSeed))
                    seed = initialSeed;
                else
                {
                    seed = -1;
                    test.MakeInvalid(RANDOM_SEED_PARAMS_MSG);
                }
            }
            else
            {
                // For now, ignore anything NUnit3TestAdapter does, because it is messing up repeatable runs.
                seed = SystemProperties.GetPropertyAsInt32("tests:seed", -1);
            }

            if (seed == -1)
            {
                seed = new Random().Next();
                return false;
            }
            return true;
        }

        public void EnsureInitialized(TestFixture fixture)
        {
            TryGetRandomSeedFromContext(fixture, out int seed);
            random = new Random(seed);

            // Set a prpoperty on the test fixture. This is where we will later
            // report the seed in the TearDown method (which sets the TestResult message).
            fixture.Properties.Set(RandomSeed, seed);

            int goodFastHashSeed = seed * 31; // LUCENENET: Multiplying 31 to remove the possility of a collision with the test framework while still using a deterministic number.
            if (StringHelper.goodFastHashSeed != goodFastHashSeed)
                StringHelper.goodFastHashSeed = goodFastHashSeed;

            // Now we need to generate the first seed for our test fixture
            // which will be used during OneTimeSetUp and OneTimeTearDown.
            fixture.Seed = random.Next();
        }

        public void GenerateRandomSeeds(Test test)
        {
            SetRandomSeeds(test);
        }

        private void SetRandomSeeds(Test test)
        {
            if (test is null)
                return;

            test.Seed = random.Next();

            if (test.HasChildren)
            {
                foreach (ITest child in test.Tests)
                    if (child is Test testChild)
                        SetRandomSeeds(testChild);
            }
        }
    }
}
