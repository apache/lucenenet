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
        #region Messages

        const string RANDOM_SEED_PARAMS_MSG =
            "RandomSeed parameter must be a valid int value or the word 'random'.";

        #endregion

        public void InitializeRandomSeed(TestFixture fixture)
        {
            if (fixture.TypeInfo.IsDefined<LuceneTestCase.RandomSeedAttribute>(inherit: true))
            {
                var randomSeedAttribute = fixture.TypeInfo.GetCustomAttributes<LuceneTestCase.RandomSeedAttribute>(inherit: true).First();
                Randomizer.InitialSeed = randomSeedAttribute.RandomSeed;
            }
            // HACK: NUnit3TestAdapter seems to be supplying the seed from nunit_random_seed.tmp whether
            // or not there is a RandomSeed set by the user. This is a workaorund until that is fixed.
            else if (NUnit.Framework.TestContext.Parameters.Exists("RandomSeed"))
            {
                if (NUnit.Framework.TestContext.Parameters["RandomSeed"].Equals("random", StringComparison.OrdinalIgnoreCase))
                    Randomizer.InitialSeed = new Random().Next();
                else if (int.TryParse(NUnit.Framework.TestContext.Parameters["RandomSeed"], out int initialSeed))
                    Randomizer.InitialSeed = initialSeed;
                else
                    fixture.MakeInvalid(RANDOM_SEED_PARAMS_MSG);
            }
            else
            {
                // For now, ignore anything NUnit3TestAdapter does, because it is messing up repeatable runs.
                Randomizer.InitialSeed = SystemProperties.GetPropertyAsInt32("tests:seed", new Random().Next());
            }

            StringHelper.goodFastHashSeed = Randomizer.InitialSeed * 31; // LUCENENET: Multiplying 31 to remove the possility of a collision with the test framework while still using a deterministic number.
        }
    }
}
