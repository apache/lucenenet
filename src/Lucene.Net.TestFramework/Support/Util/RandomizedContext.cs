using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
#nullable enable

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
        private Lazy<ConcurrentQueue<IDisposable>>? toDisposeAtEnd = null;

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
        public Random RandomGenerator => randomGenerator.Value!;

        /// <summary>
        /// Gets the randomized context for the current test or test fixture.
        /// <para/>
        /// If <c>null</c>, the call is being made out of context and the random test behavior
        /// will not be repeatable.
        /// </summary>
        public static RandomizedContext? CurrentContext
            => TestExecutionContext.CurrentContext.CurrentTest.GetRandomizedContext();

        /// <summary>
        /// Gets a lazily-initialized concurrent queue to use for resources that will be disposed at the end of the test or suite.
        /// </summary>
        internal ConcurrentQueue<IDisposable> ToDisposeAtEnd
            => (toDisposeAtEnd ??= new Lazy<ConcurrentQueue<IDisposable>>(() => new ConcurrentQueue<IDisposable>())).Value;

        /// <summary>
        /// Registers the given <paramref name="resource"/> at the end of a given
        /// lifecycle <paramref name="scope"/>.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="IDisposable"/>.</typeparam>
        /// <param name="resource">A resource to dispose.</param>
        /// <param name="scope">The scope to dispose the resource in.</param>
        /// <returns>The <paramref name="resource"/> (for chaining).</returns>
        /// <remarks>
        /// Due to limitations of NUnit, any exceptions or assertions raised
        /// from the <paramref name="resource"/> will not be respected. However, if
        /// you want to detect a failure, do note that the message from either one
        /// will be printed to StdOut.
        /// </remarks>
        public T DisposeAtEnd<T>(T resource, LifecycleScope scope) where T : IDisposable
        {
            if (currentTest.IsTest())
            {
                if (scope == LifecycleScope.TEST)
                {
                    AddDisposableAtEnd(resource);
                }
                else // LifecycleScope.SUITE
                {
                    var context = FindClassLevelTest(currentTest).GetRandomizedContext();
                    if (context is null)
                        throw new InvalidOperationException($"The provided {LifecycleScope.TEST} has no conceptual {LifecycleScope.SUITE} associated with it.");
                    context.AddDisposableAtEnd(resource);
                }
            }
            else if (currentTest.IsTestClass())
            {
                AddDisposableAtEnd(resource);
            }
            else
            {
                throw new NotSupportedException("Only runnable tests and test classes are supported.");
            }

            return resource;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddDisposableAtEnd(IDisposable resource)
        {
            // LUCENENET: ConcurrentQueue handles thread-safety internally, so no explicit locking is needed.
            // Note that if we port more of randomizedtesting later, we may need to change this to a List<T> and
            // a lock, but for now it will suit our needs without locking.
            ToDisposeAtEnd.Enqueue(resource);
        }

        private Test? FindClassLevelTest(Test test)
        {
            ITest? current = test;

            while (current != null)
            {
                // Check if this test is at the class level
                if (current.IsTestClass() && current is Test t)
                {
                    return t;
                }

                current = current.Parent;
            }

            return null;
        }

        internal void DisposeResources()
        {
            Lazy<ConcurrentQueue<IDisposable>>? toDispose = Interlocked.Exchange(ref toDisposeAtEnd, null);
            if (toDispose?.IsValueCreated == true) // Does null check on toDispose
            {
                IOUtils.Dispose(toDispose.Value);
            }
        } // toDispose goes out of scope here - no need to Clear().
    }
}
