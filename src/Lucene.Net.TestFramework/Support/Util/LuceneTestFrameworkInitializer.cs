using Lucene.Net.Codecs;
using Lucene.Net.Configuration;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

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
    /// <see cref="LuceneTestFrameworkInitializer"/> may be subclassed in order to
    /// use standard dependency injection techniques for classes under test. A subclass of <see cref="LuceneTestFrameworkInitializer"/>
    /// will be executed automatically.
    /// <para/>
    /// Only one subclass per assembly is allowed, and by convention these subclasses are usually named "Startup".
    /// <para/>
    /// The following abstract factories can be overridden for testing purposes:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Abstraction</term>
    ///         <term>Factory Method Name</term>
    ///         <term>Default</term>
    ///     </listheader>
    ///     <item>
    ///         <term><see cref="ICodecFactory"/></term>
    ///         <term><see cref="CodecFactory"/></term>
    ///         <term><see cref="TestCodecFactory"/></term>
    ///     </item>
    ///     <item>
    ///         <term><see cref="IDocValuesFormatFactory"/></term>
    ///         <term><see cref="DocValuesFormatFactory"/></term>
    ///         <term><see cref="TestDocValuesFormatFactory"/></term>
    ///     </item>
    ///     <item>
    ///         <term><see cref="IPostingsFormatFactory"/></term>
    ///         <term><see cref="PostingsFormatFactory"/></term>
    ///         <term><see cref="TestPostingsFormatFactory"/></term>
    ///     </item>
    ///     <item>
    ///         <term><see cref="IConfigurationFactory"/></term>
    ///         <term><see cref="ConfigurationFactory"/></term>
    ///         <term><see cref="TestConfigurationFactory"/></term>
    ///     </item>
    /// </list>
    /// <para/>
    /// Methods are executed one time per assembly, and are executed in the following order:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Method</term>
    ///         <term>Description</term>
    ///     </listheader>
    ///     <item>
    ///         <term><see cref="Initialize()"/></term>
    ///         <term>Used to set the factories in the above table. In general, dependency injection for the test assembly is
    ///         setup in this method. No randomized context is available.</term>
    ///     </item>
    ///     <item>
    ///         <term><see cref="TestFrameworkSetUp()"/></term>
    ///         <term>Used to set assembly-level test setup. Executed before all tests and class-level setup in the assembly.
    ///         Repeatable randomized content can be generated using the <see cref="Random"/> property.</term>
    ///     </item>
    ///     <item>
    ///         <term><see cref="TestFrameworkTearDown()"/></term>
    ///         <term>Used to tear down assembly-level test setup. Executed after all tests and class-level tear down in the assembly.
    ///         Repeatable randomized content can be generated using the <see cref="Random"/> property.</term>
    ///     </item>
    /// </list>
    /// <para/>
    /// <b>Example:</b>
    /// <code>
    /// using RandomizedTesting.Generators;
    /// 
    /// public class Startup : LuceneTestFrameworkInitializer
    /// {
    ///     // Run first
    ///     protected override void Initialize()
    ///     {
    ///         // Inject a custom configuration factory
    ///         ConfigurationFactory = new MyConfigurationFactory();
    /// 
    ///         // Do any additional dependency injection setup here
    ///     }
    ///
    ///     // Run before all tests in the assembly and before any class-level setup
    ///     protected overide void TestFrameworkSetUp()
    ///     {
    ///         // Get the random instance for the current context
    ///         var random = Random;
    ///
    ///         // Generate random content
    ///         string content = random.NextSimpleString();
    ///
    ///         // Use randomization from LuceneTestCase
    ///         int numberOfDocuments = LuceneTestCase.AtLeast(30);
    ///     }
    ///
    ///     // Run after all tests in the assembly and after any class-level setup
    ///     protected override void TestFrameworkTearDown()
    ///     {
    ///         // Tear down everything here
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class LuceneTestFrameworkInitializer
    {
        private bool isInitializing = false;
        private ICodecFactory codecFactory;
        private IDocValuesFormatFactory docValuesFormatFactory;
        private IPostingsFormatFactory postingsFormatFactory;
        private IConfigurationFactory configurationFactory;

        private readonly AbstractBeforeAfterRule useTempLineDocsFileRule;

        protected LuceneTestFrameworkInitializer()
        {
            codecFactory = new TestCodecFactory();
            docValuesFormatFactory = new TestDocValuesFormatFactory();
            postingsFormatFactory = new TestPostingsFormatFactory();
            configurationFactory = new TestConfigurationFactory();

            useTempLineDocsFileRule = new UseTempLineDocsFileRule();
        }

        // LUCENENET specific factory methods to scan the test framework for codecs/docvaluesformats/postingsformats only once

        /// <summary>
        /// The <see cref="ICodecFactory"/> implementation to use to load codecs during testing.
        /// </summary>
        protected ICodecFactory CodecFactory
        {
            get => codecFactory;
            set
            {
                if (!isInitializing)
                    throw new InvalidOperationException("CodecFactory must be set in the Initialize() method.");
                codecFactory = value ?? throw new ArgumentNullException(nameof(CodecFactory));
            }
        }

        /// <summary>
        /// The <see cref="IDocValuesFormatFactory"/> implementation to use to load doc values during testing.
        /// </summary>
        protected IDocValuesFormatFactory DocValuesFormatFactory
        {
            get => docValuesFormatFactory;
            set
            {
                if (!isInitializing)
                    throw new InvalidOperationException("DocValuesFormatFactory must be set in the Initialize() method.");
                docValuesFormatFactory = value ?? throw new ArgumentNullException(nameof(DocValuesFormatFactory));
            }
        }

        /// <summary>
        /// The <see cref="IPostingsFormatFactory"/> implementation to use to load postings formats during testing.
        /// </summary>
        protected IPostingsFormatFactory PostingsFormatFactory
        {
            get => postingsFormatFactory;
            set
            {
                if (!isInitializing)
                    throw new InvalidOperationException("PostingsFormatFactory must be set in the Initialize() method.");
                postingsFormatFactory = value ?? throw new ArgumentNullException(nameof(PostingsFormatFactory));
            }
        }

        /// <summary>
        /// The <see cref="IConfigurationFactory"/> implementation to use to load configuration settings during testing.
        /// See: <a href="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/">
        /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/</a>.
        /// </summary>
        [CLSCompliant(false)]
        protected IConfigurationFactory ConfigurationFactory
        {
            get => configurationFactory;
            set
            {
                if (!isInitializing)
                    throw new InvalidOperationException("ConfigurationFactory must be set in the Initialize() method.");
                configurationFactory = value ?? throw new ArgumentNullException(nameof(ConfigurationFactory));
            }
        }

        /// <summary>
        /// Called by <see cref="LuceneTestCase.SetUpFixture"/> to run the initialization phase for the test assembly.
        /// </summary>
        internal void DoInitialize()
        {
            try
            {
                isInitializing = true;
                Initialize();
                isInitializing = false;
                InitializeStaticState();
                AfterInitialization(); // Hook for our tests to check the result
            }
            catch (Exception ex)
            {
                // Write the stack trace so we have something to go on if an error occurs here.
                throw RuntimeException.Create($"An exception occurred during initialization:\n{ex}", ex);
            }
        }

        /// <summary>
        /// Called by <see cref="LuceneTestCase.SetUpFixture"/> to run the one time set up phase for the test assembly.
        /// </summary>
        internal void DoTestFrameworkSetUp()
        {
            useTempLineDocsFileRule.Before();

            try
            {
                TestFrameworkSetUp();
            }
            catch (Exception ex)
            {
                // Write the stack trace so we have something to go on if an error occurs here.
                throw RuntimeException.Create($"An exception occurred during TestFrameworkSetUp:\n{ex}", ex);
            }
        }


        /// <summary>
        /// Called by <see cref="LuceneTestCase.SetUpFixture"/> to run the one time tear down phase for the test assembly.
        /// </summary>
        internal void DoTestFrameworkTearDown()
        {
            try
            {
                TestFrameworkTearDown();
            }
            catch (Exception ex)
            {
                // Write the stack trace so we have something to go on if an error occurs here.
                throw RuntimeException.Create($"An exception occurred during TestFrameworkTearDown:\n{ex}", ex);
            }

            useTempLineDocsFileRule.After();
        }

        /// <summary>
        /// Access to the current <see cref="System.Random"/> instance. It is safe to use
        /// this method from multiple threads, etc., but it should be called while within a runner's
        /// scope (so no static initializers). The returned <see cref="System.Random"/> instance will be
        /// <b>different</b> when this method is called inside a <see cref="LuceneTestCase.BeforeClass()"/> hook (static
        /// suite scope) and within <see cref="OneTimeSetUpAttribute"/>/ <see cref="OneTimeTearDownAttribute"/> hooks or test methods.
        ///
        /// <para/>The returned instance must not be shared with other threads or cross a single scope's
        /// boundary. For example, a <see cref="System.Random"/> acquired within a test method shouldn't be reused
        /// for another test case.
        ///
        /// <para/>There is an overhead connected with getting the <see cref="System.Random"/> for a particular context
        /// and thread. It is better to cache the <see cref="System.Random"/> locally if tight loops with multiple
        /// invocations are present or create a derivative local <see cref="System.Random"/> for millions of calls
        /// like this:
        /// <code>
        /// Random random = new J2N.Randomizer(Random.NextInt64());
        /// // tight loop with many invocations.
        /// </code>
        /// </summary>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        protected Random Random => LuceneTestCase.Random;

        /// <summary>
        /// Overridden in a derived class, provides a way to set <see cref="CodecFactory"/>,
        /// <see cref="PostingsFormatFactory"/>, <see cref="DocValuesFormatFactory"/> and <see cref="ConfigurationFactory"/> as well as
        /// other dependency injection setup.
        /// <para/>
        /// This method is called only one time per assembly.
        /// <para/>
        /// Using the <see cref="Random"/> property here will result in a test failure. To build randomized global setup,
        /// use <see cref="TestFrameworkSetUp()"/> instead.
        /// </summary>
        protected virtual void Initialize()
        {
            // Runs only once per test framework before OneTimeSetUp on any tests, and before TestFrameworkSetUp
        }

        /// <summary>
        /// Overridden in a derived class, can be used to perform one-time initialization
        /// of the test framework setup.
        /// <para/>
        /// Repeatable random setup can be done by calling <see cref="Random"/> or by using methods of <see cref="LuceneTestCase"/>.
        /// <para/>
        /// It is not possible to set <see cref="CodecFactory"/>, <see cref="PostingsFormatFactory"/>, 
        /// <see cref="DocValuesFormatFactory"/> and <see cref="ConfigurationFactory"/>
        /// from this method. Those must be called in <see cref="Initialize()"/>.
        /// </summary>
        protected virtual void TestFrameworkSetUp()
        {
            // Runs only once per test framework run before all tests
        }

        /// <summary>
        /// Overridden in a derived class, can be used to perform one-time tear down
        /// of the test framework setup (whether the setup was done in <see cref="Initialize()"/>
        /// or <see cref="TestFrameworkSetUp()"/> doesn't matter).
        /// <para/>
        /// Repeatable random setup can be done by calling <see cref="Random"/> or by using methods of <see cref="LuceneTestCase"/>.
        /// </summary>
        protected virtual void TestFrameworkTearDown()
        {
            // Runs only once per test framework run after all tests
        }

        /// <summary>
        /// The default implementation of the initializer that is simply used to initialize with the
        /// default test factory implementations.
        /// </summary>
        internal class DefaultLuceneTestFrameworkInitializer : LuceneTestFrameworkInitializer { }

        /// <summary>
        /// Called after the factory properties have been set (possibly by using dependency injection) which allows customization
        /// of codecs and system properties during testing.
        /// </summary>
        internal void InitializeStaticState()
        {
            // Setup the factories
            ConfigurationSettings.SetConfigurationFactory(ConfigurationFactory);
            Codec.SetCodecFactory(CodecFactory);
            DocValuesFormat.SetDocValuesFormatFactory(DocValuesFormatFactory);
            PostingsFormat.SetPostingsFormatFactory(PostingsFormatFactory);

            // Enable "asserts" for tests. In Java, these were actual asserts,
            // but in .NET we simply mock this as a boolean static setting that can be
            // toggled on and off, even in release mode. Note this must be done after
            // the ConfigurationFactory is set.
            Lucene.Net.Diagnostics.Debugging.AssertsEnabled = SystemProperties.GetPropertyAsBoolean("assert", true);

            // Identify NUnit exceptions down in Lucene.Net so they can be ignored in catch blocks that
            // catch Java "Exception" types that do subclass Error (for the ExceptionExtensions.IsException() method).
            Lucene.ExceptionExtensions.NUnitResultStateExceptionType = typeof(NUnit.Framework.ResultStateException);
            Lucene.ExceptionExtensions.NUnitAssertionExceptionType = typeof(NUnit.Framework.AssertionException);
            Lucene.ExceptionExtensions.NUnitMultipleAssertExceptionType = typeof(NUnit.Framework.MultipleAssertException);
            Lucene.ExceptionExtensions.NUnitInconclusiveExceptionType = typeof(NUnit.Framework.InconclusiveException);
            Lucene.ExceptionExtensions.NUnitSuccessExceptionType = typeof(NUnit.Framework.SuccessException);
            Lucene.ExceptionExtensions.NUnitInvalidPlatformException = Type.GetType("NUnit.Framework.Internal.InvalidPlatformException, NUnit.Framework");

            // Identify the Debug.Assert() exception so it can be excluded from being swallowed by catch blocks.
            // These types are internal, so we can identify them using Reflection.
            Lucene.ExceptionExtensions.DebugAssertExceptionType =
                // .NET 5/.NET Core 3.x
                Type.GetType("System.Diagnostics.DebugProvider+DebugAssertException, System.Private.CoreLib")
                // .NET Core 2.x
                ?? Type.GetType("System.Diagnostics.Debug+DebugAssertException, System.Private.CoreLib");
                // .NET Framework doesn't throw in this case
        }

        /// <summary>
        /// Checkpoint to allow tests to check the results of <see cref="InitializeStaticState()"/>.
        /// </summary>
        internal virtual void AfterInitialization() // Called only by tests
        {
        }
    }
}
