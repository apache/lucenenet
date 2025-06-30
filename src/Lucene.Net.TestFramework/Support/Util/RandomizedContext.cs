// Rougly similar to: https://github.com/randomizedtesting/randomizedtesting/blob/release/2.7.8/randomized-runner/src/main/java/com/carrotsearch/randomizedtesting/RandomizedContext.java

using Lucene.Net.Support.Threading;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
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
        internal const string RandomizedContextScopeKeyName = "_RandomizedContext_Scope";
        internal const string RandomizedContextThreadNameKeyName = "_RandomizedContext_ThreadName";
        internal const string RandomizedContextStackTraceKeyName = "_RandomizedContext_StackTrace";

        private readonly ThreadLocal<J2N.Randomizer> randomGenerator;
        private readonly Test currentTest;
        private readonly Assembly currentTestAssembly;
        private readonly long randomSeed;
        private volatile string? randomSeedAsString;
        private long testSeed;

        /// <summary>
        /// Disposable resources.
        /// </summary>
        private List<DisposableResourceInfo>? disposableResources = null;

        /// <summary>
        /// Coordination at context level.
        /// </summary>
        private readonly object contextLock = new object();

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
            this.testSeed = testSeed;
            this.randomGenerator = new ThreadLocal<J2N.Randomizer>(() => new J2N.Randomizer(this.testSeed));
        }

        /// <summary>
        /// Gets the initial seed that was used to initialize the current context.
        /// </summary>
        public long RandomSeed => randomSeed;

        /// <summary>
        /// Gets the initial seed as a hexadecimal string for display/configuration purposes.
        /// </summary>
        public string RandomSeedAsString => randomSeedAsString ??= SeedUtils.FormatSeed(randomSeed, testSeed);

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
        public long TestSeed => Interlocked.Read(ref this.testSeed);

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
        public Random RandomGenerator
        {
            get
            {
                var random = randomGenerator.Value!;
                UninterruptableMonitor.Enter(random.SyncRoot);
                try
                {
                    // Ensure the current thread is using the latest test seed.
                    if (random.Seed != testSeed)
                        random.Seed = testSeed;
                }
                finally
                {
                    UninterruptableMonitor.Exit(random.SyncRoot);
                }
                return random;
            }
        }

        /// <summary>
        /// Gets the randomized context for the current test or test fixture.
        /// <para/>
        /// If <c>null</c>, the call is being made out of context and the random test behavior
        /// will not be repeatable.
        /// </summary>
        public static RandomizedContext? CurrentContext
            => TestExecutionContext.CurrentContext.CurrentTest.GetRandomizedContext();

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
                    AddDisposableAtEnd(resource, scope);
                }
                else // LifecycleScope.SUITE
                {
                    var context = FindClassLevelTest(currentTest).GetRandomizedContext();
                    if (context is null)
                        throw new InvalidOperationException($"The provided {LifecycleScope.TEST} has no conceptual {LifecycleScope.SUITE} associated with it.");
                    context.AddDisposableAtEnd(resource, scope);
                }
            }
            else if (currentTest.IsTestClass())
            {
                AddDisposableAtEnd(resource, scope);
            }
            else
            {
                throw new NotSupportedException("Only runnable tests and test classes are supported.");
            }

            return resource;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddDisposableAtEnd(IDisposable resource, LifecycleScope scope)
        {
            UninterruptableMonitor.Enter(contextLock);
            try
            {
                disposableResources ??= new List<DisposableResourceInfo>();
                disposableResources.Add(new DisposableResourceInfo(resource, scope, Thread.CurrentThread.Name, new StackTrace(skipFrames: 3)));
            }
            finally
            {
                UninterruptableMonitor.Exit(contextLock);
            }
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
            List<DisposableResourceInfo>? resources;
            UninterruptableMonitor.Enter(contextLock);
            try
            {
                resources = disposableResources; // Set the resources to a local variable
                disposableResources = null; // Set disposableResources field to null so our local list will go out of scope when we are done
            }
            finally
            {
                UninterruptableMonitor.Exit(contextLock);
            }

            if (resources is not null)
            {
                Exception? th = null;

                foreach (DisposableResourceInfo disposable in resources)
                {
                    try
                    {
                        disposable.Resource.Dispose();
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        // Add details about the source of the exception, so they can be printed out later.
                        t.Data[RandomizedContextScopeKeyName]      = disposable.Scope.ToString(); // string
                        t.Data[RandomizedContextThreadNameKeyName] = disposable.ThreadName;       // string
                        t.Data[RandomizedContextStackTraceKeyName] = disposable.StackTrace;       // System.Diagnostics.StackTrace

                        if (th is not null)
                        {
                            th.AddSuppressed(t);
                        }
                        else
                        {
                            th = t;
                        }
                    }
                }

                if (th is not null)
                {
                    ExceptionDispatchInfo.Capture(th).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                }
            }
        } // resources goes out of scope here - no need to Clear().

        /// <summary>
        /// Prints a stack trace of the <paramref name="exception"/> to the destination <see cref="TextWriter"/>.
        /// The message will show additional stack details relevant to <see cref="DisposeAtEnd{T}(T, LifecycleScope)"/>
        /// to identify the calling method, <see cref="LifecycleScope"/>, and name of the calling thread.
        /// </summary>
        /// <param name="exception">The exception to print. This may contain additional details in <see cref="Exception.Data"/>.</param>
        /// <param name="destination">A <see cref="TextWriter"/> to write the output to.</param>
        internal static void PrintStackTrace(Exception exception, TextWriter destination)
        {
            destination.WriteLine(FormatStackTrace(exception));
        }

        private static string FormatStackTrace(Exception exception)
        {
            StringBuilder sb = new StringBuilder(256);
            FormatException(exception, sb);

            foreach (var suppressedException in exception.GetSuppressedAsList())
            {
                sb.AppendLine("Suppressed: ");
                FormatException(suppressedException, sb);
            }

            return sb.ToString();
        }

        private static void FormatException(Exception exception, StringBuilder destination)
        {
            destination.AppendLine(exception.ToString());

            string? scope = (string?)exception.Data[RandomizedContextScopeKeyName];
            string? threadName = (string?)exception.Data[RandomizedContextThreadNameKeyName];
            StackTrace? stackTrace = (StackTrace?)exception.Data[RandomizedContextStackTraceKeyName];

            bool hasData = scope != null || threadName != null || stackTrace != null;
            if (!hasData)
            {
                return;
            }

            destination.AppendLine("Caller Details:");
            if (scope != null)
            {
                destination.Append("Scope: ");
                destination.AppendLine(scope);
            }
            if (threadName != null)
            {
                destination.Append("Thread Name: ");
                destination.AppendLine(threadName);
            }
            if (stackTrace != null)
            {
                destination.Append("Stack Trace:");
                destination.AppendLine(stackTrace.ToString());
            }
        }

        /// <summary>
        /// Resets the local <see cref="testSeed"/> field so it can be
        /// used to update each thread that uses <see cref="randomGenerator"/> if
        /// the seed doesn't match.
        /// <para/>
        /// Although calling this method is threadsafe, it should not be
        /// called in the middle of a test. Instead, it should be called
        /// between test runs to prevent non-repeatable random conditions
        /// from occurring during a test.
        /// </summary>
        /// <param name="testSeed">The new test seed. This value will be
        /// used to initialize the random generator for the test run.</param>
        internal void ResetSeed(long testSeed)
        {
            var random = this.randomGenerator.Value!;
            UninterruptableMonitor.Enter(random.SyncRoot);
            try
            {
                this.randomSeedAsString = null;
                this.testSeed = testSeed;
                // Note that this resets the current thread only.
                // That is why we have to check in the RandomGenerator
                // property getter that the Seed is up-to-date.
                random.Seed = testSeed;
            }
            finally
            {
                UninterruptableMonitor.Exit(random.SyncRoot);
            }
        }
    }
}
