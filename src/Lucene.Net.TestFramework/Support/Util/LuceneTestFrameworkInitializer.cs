using Lucene.Net.Codecs;
using Lucene.Net.Configuration;
using NUnit.Framework;
using System;
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
    /// <see cref="LuceneTestFrameworkInitializer"/> may be subclassed by end users in order to
    /// use standard dependency injection techniques for classes under test. A subclass of <see cref="LuceneTestFrameworkInitializer"/>
    /// will be executed automatically by NUnit. To work properly with the Lucene.NET test framework, the subclass
    /// must be placed outside of all namespaces, which ensures it is only executed 1 time per test assembly.
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
    /// <b>Example:</b>
    /// <code>
    /// // IMPORTANT: Do not put the initializer in a namespace
    /// public class MyTestFrameworkInitializer : LuceneTestFrameworkInitializer
    /// {
    ///     protected override IConfigurationFactory LoadConfigurationFactory()
    ///     {
    ///         // Inject a custom configuration factory
    ///         return new MyConfigurationFactory();
    ///     }
    ///     
    ///     protected override void TestFrameworkSetUp()
    ///     {
    ///         // Inject a custom configuration factory
    ///         ConfigurationFactory = new MyConfigurationFactory();
    /// 
    ///         // Do any additional dependency injection setup here
    ///     }
    /// }
    /// </code>
    /// </summary>
    [SetUpFixture]
    public abstract class LuceneTestFrameworkInitializer
    {
        // Initialization target must be static to ensure the initializer only fires one
        // time per application run (even when the static EnsureInitialized() method is called).
        private static object initializationTarget;


        // LUCENENET specific factory methods to scan the test framework for codecs/docvaluesformats/postingsformats only once

        /// <summary>
        /// The <see cref="ICodecFactory"/> implementation to use to load codecs during testing.
        /// </summary>
        protected static ICodecFactory CodecFactory { get; set; } = new TestCodecFactory();

        /// <summary>
        /// The <see cref="IDocValuesFormatFactory"/> implementation to use to load doc values during testing.
        /// </summary>
        protected static IDocValuesFormatFactory DocValuesFormatFactory { get; set; } = new TestDocValuesFormatFactory();

        /// <summary>
        /// The <see cref="IPostingsFormatFactory"/> implementation to use to load postings formats during testing.
        /// </summary>
        protected static IPostingsFormatFactory PostingsFormatFactory { get; set; } = new TestPostingsFormatFactory();

        /// <summary>
        /// The <see cref="IConfigurationFactory"/> implementation to use to load configuration settings during testing.
        /// See: <a href="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/">
        /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/</a>.
        /// </summary>
        [CLSCompliant(false)]
        protected static IConfigurationFactory ConfigurationFactory { get; set; } = new TestConfigurationFactory();

        /// <summary>
        /// Overridden in a derived class, can be used to initialize factory instances that are
        /// injected into Lucene.NET during test operations, or perform other one-time initialization
        /// of the test framework setup.
        /// <para/>
        /// NOTE: To initialize 1 time for your entire test library, only create a single derived class
        /// and place it outside of any namespace.
        /// </summary>
        protected virtual void TestFrameworkSetUp()
        {
            // Runs only once per test framework run before all tests
        }


        /// <summary>
        /// Overridden in a derived class, can be used to tear down classes one-time
        /// for the test framework.
        /// </summary>
        protected virtual void TestFrameworkTearDown()
        {
            // Runs only once per test framework run after all tests
        }

        /// <summary>
        /// When this class is derived in a test assembly, this method is automatically called
        /// by NUnit one time before all tests run.
        /// </summary>
        [OneTimeSetUp]
        protected void OneTimeSetUpBeforeTests()
        {
            try
            {
                TestFrameworkSetUp();

                // Initialize of the test factories here, so that doesn't happen
                // when EnsureInitialized() is called.
                Initialize();
            }
            catch (Exception ex)
            {
                // Write the stack trace so we have something to go on if an error occurs here.
                throw new Exception($"An exception occurred during OneTimeSetUpBeforeTests:\n{ex.ToString()}", ex);
            }
        }

        /// <summary>
        /// When this class is derived in a test assembly, this method is automatically called
        /// by NUnit one time after all tests run.
        /// </summary>
        [OneTimeTearDown]
        protected void OneTimeTearDownAfterTests()
        {
            try
            {
                TestFrameworkTearDown();
            }
            catch (Exception ex)
            {
                // Write the stack trace so we have something to go on if an error occurs here.
                throw new Exception($"An exception occurred during OneTimeTearDownAfterTests:\n{ex.ToString()}", ex);
            }
        }

        /// <summary>
        /// The default implementation of the initializer that is simply used to initialize with the
        /// default test factory implementations.
        /// </summary>
        private class DefaultLuceneTestFrameworkInitializer : LuceneTestFrameworkInitializer { }

        /// <summary>
        /// Can be called by an external source to ensure that the test framework has been initialized.
        /// <see cref="Initialize()"/> only occurs if it hasn't already been called by NUnit in a subclass
        /// of this class.
        /// </summary>
        internal static void EnsureInitialized()
        {
            new DefaultLuceneTestFrameworkInitializer().Initialize();
        }

        private void Initialize()
        {
            LazyInitializer.EnsureInitialized(ref initializationTarget, () =>
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

                IncrementInitalizationCount(); // For testing

                return new object(); // Placeholder to indicate our initializer has been run already
            });
        }

        internal virtual void IncrementInitalizationCount()
        {
        }
    }
}
