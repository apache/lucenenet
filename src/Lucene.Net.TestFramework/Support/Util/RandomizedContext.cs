using J2N;
using NUnit.Framework.Internal;
using System;
using System.Reflection;
using System.Threading;

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

    /// <summary>
    /// Extensions to NUnit's test context for randomized testing.
    /// </summary>
    internal class RandomizedContext
    {
        // LUCENENET NOTE: Using an underscore to prefix the name hides it from "traits" in Test Explorer
        internal const string RandomizedContextPropertyName = "_RandomizedContext";

        private readonly ThreadLocal<Random> randomGenerator;
        private readonly Test currentTest;
        private readonly Assembly currentTestAssembly;
        private readonly long randomSeed;
        private readonly string randomSeedAsHex;
        private readonly long testSeed;

        /// <summary>
        /// Initializes the randomized context.
        /// </summary>
        /// <param name="currentTest">The test or test fixture for the context.</param>
        /// <param name="currentTestAssembly">The test assembly. This can be used in <see cref="LuceneTestFrameworkInitializer"/> to scan the test assembly for attributes.</param>
        /// <param name="randomSeed">The initial random seed that can be used to regenerate this context.</param>
        /// <param name="testSeed">The individual test's seed to regenerate the test.</param>
        public RandomizedContext(Test currentTest, Assembly currentTestAssembly, long randomSeed, long testSeed)
        {
            this.currentTest = currentTest ?? throw new ArgumentNullException(nameof(currentTest));
            this.currentTestAssembly = currentTestAssembly ?? throw new ArgumentNullException(nameof(currentTestAssembly));
            this.randomSeed = randomSeed;
            this.randomSeedAsHex = SeedUtils.FormatSeed(randomSeed);
            this.testSeed = testSeed;
            this.randomGenerator = new ThreadLocal<Random>(() => new J2N.Randomizer(this.testSeed));
        }

        /// <summary>
        /// Gets the initial seed that was used to initialize the current context.
        /// </summary>
        public long RandomSeed => randomSeed;

        /// <summary>
        /// Gets the initial seed as a hexadecimal string for display/configuration purposes.
        /// </summary>
        public string RandomSeedAsHex => randomSeedAsHex;

        /// <summary>
        /// The current test for this context.
        /// </summary>
        public Test CurrentTest => currentTest;

        /// <summary>
        /// The current test assembly, which may be used to scan the assembly for custom attributes.
        /// </summary>
        public Assembly CurrentTestAssembly => currentTestAssembly;

        /// <summary>
        /// The random seed for this test's <see cref="RandomGenerator"/>.
        /// </summary>
        public long TestSeed => testSeed;

        /// <summary>
        /// Gets the RandomGenerator specific to this Test and thread. This random generator implementatation
        /// is not platform specific, so random numbers generated on one operating system will work on another.
        /// <para/>
        /// NOTE: NUnit doesn't currently set the <see cref="Test.Seed"/> property for the test fixtures
        /// when using their built-in <see cref="NUnit.Framework.TestFixtureAttribute"/> or
        /// <see cref="NUnit.Framework.SetUpFixtureAttribute"/>. This means the seed will always be 0
        /// when using this property from <see cref="NUnit.Framework.OneTimeSetUpAttribute"/> or
        /// <see cref="NUnit.Framework.OneTimeTearDownAttribute"/>, so it cannot be relied upon to create
        /// random test data in these cases. Using the <see cref="LuceneTestCase.TestFixtureAttribute"/>
        /// will set the seed properly and make it possible to repeat the result.
        /// </summary>
        public Random RandomGenerator => randomGenerator.Value;

        /// <summary>
        /// Gets the randomized context for the current test or test fixture.
        /// </summary>
        public static RandomizedContext CurrentContext
        {
            get
            {
                var currentTest = TestExecutionContext.CurrentContext.CurrentTest;

                if (currentTest.Properties.ContainsKey(RandomizedContextPropertyName))
                    return (RandomizedContext)currentTest.Properties.Get(RandomizedContextPropertyName);

                return null; // We are out of random context and cannot respond with results that are repeatable.
            }
        }
    }
}
