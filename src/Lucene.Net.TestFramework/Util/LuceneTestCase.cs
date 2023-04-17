using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util.Automaton;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using RandomizedTesting.Generators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using static Lucene.Net.Search.FieldCache;
using static Lucene.Net.Util.FieldCacheSanityChecker;
using After = NUnit.Framework.TearDownAttribute;
using Assert = Lucene.Net.TestFramework.Assert;
using AssumptionViolatedException = NUnit.Framework.InconclusiveException;
using Before = NUnit.Framework.SetUpAttribute;
using Console = Lucene.Net.Util.SystemConsole;
using Directory = Lucene.Net.Store.Directory;
using FieldInfo = Lucene.Net.Index.FieldInfo;
using JCG = J2N.Collections.Generic;
using OneTimeSetUp = NUnit.Framework.OneTimeSetUpAttribute;
using OneTimeTearDown = NUnit.Framework.OneTimeTearDownAttribute;
using Test = NUnit.Framework.TestAttribute;

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
    /// Base class for all Lucene.Net unit tests.
    ///
    /// <h3>Class and instance setup.</h3>
    ///
    /// <para>
    /// The preferred way to specify class (suite-level) setup/cleanup is to use
    /// static methods annotated with <see cref="OneTimeSetUp"/> and <see cref="OneTimeTearDown"/>. Any
    /// code in these methods is executed within the test framework's control and
    /// ensure proper setup has been made. <b>Try not to use static initializers
    /// (including complex readonly field initializers).</b> Static initializers are
    /// executed before any setup rules are fired and may cause you (or somebody
    /// else) headaches.
    /// </para>
    ///
    /// <para>
    /// For instance-level setup, use <see cref="Before"/> and <see cref="After"/> annotated
    /// methods. If you override either <see cref="SetUp()"/> or <see cref="TearDown()"/> in
    /// your subclass, make sure you call <c>base.SetUp()</c> and
    /// <c>base.TearDown()</c>. This is detected and enforced.
    /// </para>
    ///
    /// <h3>Specifying test cases</h3>
    ///
    /// <para>
    /// Any test method annotated with <see cref="Test"/> is considered a test case.
    /// </para>
    ///
    /// <h3>Randomized execution and test facilities</h3>
    ///
    /// <para>
    /// <see cref="LuceneTestCase"/> uses a custom <see cref="TestFixtureAttribute"/> to execute test cases.
    /// The custom <see cref="TestFixtureAttribute"/> has built-in support for test randomization
    /// including access to a repeatable <see cref="Random"/> instance. See
    /// <see cref="Random"/> property. Any test using <see cref="Random"/> acquired from
    /// <see cref="Random"/> should be fully reproducible (assuming no race conditions
    /// between threads etc.). The initial seed for a test case is reported in the failure
    /// test message.
    /// </para>
    /// <para>
    /// The seed can be configured with a RunSettings file, a <c>lucene.testSettings.config</c> JSON file,
    /// an environment variable, or using <see cref="RandomSeedAttribute"/> at the assembly level.
    /// It is recommended to configure the culture also, since they are randomly picked from a list
    /// of cultures installed on a given machine, so the culture will vary from one machine to the next.
    /// </para>
    /// 
    /// <h4><i>.runsettings</i> File Configuration Example</h4>
    /// 
    /// <code>
    /// &lt;RunSettings&gt;
    ///   &lt;TestRunParameters&gt;
    ///     &lt;Parameter name="tests:seed" value="0x1ffa1d067056b0e6" /&gt;
    ///     &lt;Parameter name="tests:culture" value="sw-TZ" /&gt;
    ///   &lt;/TestRunParameters&gt;
    /// &lt;/RunSettings&gt;
    /// </code>
    /// <para>
    /// See the <i>.runsettings</i> documentation at: <a href="https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file">
    /// https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file</a>.
    /// </para>
    /// 
    /// <h4>Attribute Configuration Example</h4>
    /// 
    /// <code>
    /// [assembly: Lucene.Net.Util.RandomSeed("0x1ffa1d067056b0e6")]
    /// [assembly: NUnit.Framework.SetCulture("sw-TZ")]
    /// </code>
    ///
    /// <h4><i>lucene.testSettings.config</i> File Configuration Example</h4>
    ///
    /// <para>
    /// Add a file named <i>lucene.testSettings.config</i> to the executable directory or
    /// any directory between the executable and the root of the drive with the following contents.
    /// </para>
    /// 
    /// <code>
    /// {
    ///	  "tests": {
    ///     "seed": "0x1ffa1d067056b0e6",
    ///     "culture": "sw-TZ"
    ///	  }
    /// }
    /// </code>
    ///
    /// <h4>Environment Variable Configuration Example</h4>
    ///
    /// <list type="table">
    ///     <listheader>
    ///         <term>Variable</term>
    ///         <term>Value</term>
    ///     </listheader>
    ///     <item>
    ///         <term>lucene:tests:seed</term>
    ///         <term>0x1ffa1d067056b0e6</term>
    ///     </item>
    ///     <item>
    ///         <term>lucene:tests:culture</term>
    ///         <term>sw-TZ</term>
    ///     </item>
    /// </list>
    /// 
    /// </summary>
    [TestFixture]
    public abstract partial class LuceneTestCase //: Assert // Wait long for leaked threads to complete before failure. zk needs this. -  See LUCENE-3995 for rationale.
    {
        // --------------------------------------------------------------------
        // Test groups, system properties and other annotations modifying tests
        // --------------------------------------------------------------------

        internal const string SYSPROP_NIGHTLY = "tests:nightly";
        internal const string SYSPROP_WEEKLY = "tests:weekly";
        internal const string SYSPROP_AWAITSFIX = "tests:awaitsfix";
        internal const string SYSPROP_SLOW = "tests:slow";
        internal const string SYSPROP_BADAPPLES = "tests:badapples"; // LUCENENET specific - made internal, because not fully implemented

        ///// <seealso cref="IgnoreAfterMaxFailures"/>
        internal const string SYSPROP_MAXFAILURES = "tests:maxfailures"; // LUCENENET specific - made internal, because not fully implemented

        ///// <seealso cref="IgnoreAfterMaxFailures"/>
        internal const string SYSPROP_FAILFAST = "tests:failfast"; // LUCENENET specific - made internal, because not fully implemented

        private class LuceneDelegatingTestCommand : DelegatingTestCommand
        {
            private readonly Func<bool> shouldSkip;
            public LuceneDelegatingTestCommand(TestCommand command, Func<bool> shouldSkip, string skipReason) : base(command)
            {
                this.shouldSkip = shouldSkip ?? throw new ArgumentNullException(nameof(shouldSkip));
                SkipReason = skipReason ?? throw new ArgumentNullException(nameof(skipReason));
            }

            public RunState RunState { get; set; } = RunState.Skipped;
            public string SkipReason { get; }

            public override TestResult Execute(TestExecutionContext context)
            {
                var test = context.CurrentTest;
                // The test framework setting overrides the NUnit category setting, if false
                bool skip = shouldSkip();
                if (skip)
                {
                    test.RunState = RunState;
                    test.Properties.Set(PropertyNames.SkipReason, SkipReason);
                }

                context.CurrentResult = test.MakeTestResult();

                if (!skip)
                {
                    try
                    {
                        context.CurrentResult = innerCommand.Execute(context);
                    }
                    catch (Exception ex)
                    {
                        context.CurrentResult.RecordException(ex);
                    }
                }

                return context.CurrentResult;
            }
        }

        /// <summary>
        /// Attribute for tests that should only be run during nightly builds.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "API looks better with this nested.")]
        public sealed class NightlyAttribute : System.Attribute
            , IApplyToTest, IApplyToContext, IWrapTestMethod
        {
            private const string SKIP_REASON = "This is a nightly test.";

            void IApplyToTest.ApplyToTest(NUnit.Framework.Internal.Test test)
            {
                // This method is called before initialization. The only thing
                // we can do here is set the category.
                test.Properties.Add(PropertyNames.Category, "Nightly");
            }

            void IApplyToContext.ApplyToContext(TestExecutionContext context)
            {
                // Cover the case where this attribute is applied to the whole test fixture
                var currentTest = context.CurrentTest;
                if (!TestNightly && currentTest is NUnit.Framework.Internal.TestFixture fixture)
                {
                    foreach (var testInterface in fixture.Tests)
                    {
                        var test = (NUnit.Framework.Internal.Test)testInterface;
                        test.RunState = RunState.Skipped;
                        test.Properties.Set(PropertyNames.SkipReason, SKIP_REASON);
                    }
                }
            }

            TestCommand ICommandWrapper.Wrap(TestCommand command)
            {
                // This is to cover the case where the test is decorated with the attribute
                // directly.
                return new LuceneDelegatingTestCommand(command, () => !TestNightly, SKIP_REASON);
            }
        }

        /// <summary>
        /// Attribute for tests that should only be run during weekly builds.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "API looks better with this nested.")]
        public sealed class WeeklyAttribute : System.Attribute
            , IApplyToTest, IApplyToContext, IWrapTestMethod
        {
            private const string SKIP_REASON = "This is a weekly test.";

            void IApplyToTest.ApplyToTest(NUnit.Framework.Internal.Test test)
            {
                // This method is called before initialization. The only thing
                // we can do here is set the category.
                test.Properties.Add(PropertyNames.Category, "Weekly");
            }

            void IApplyToContext.ApplyToContext(TestExecutionContext context)
            {
                // Cover the case where this attribute is applied to the whole test fixture
                var currentTest = context.CurrentTest;
                if (!TestWeekly && currentTest is NUnit.Framework.Internal.TestFixture fixture)
                {
                    foreach (var testInterface in fixture.Tests)
                    {
                        var test = (NUnit.Framework.Internal.Test)testInterface;
                        test.RunState = RunState.Skipped;
                        test.Properties.Set(PropertyNames.SkipReason, SKIP_REASON);
                    }
                }
            }

            TestCommand ICommandWrapper.Wrap(TestCommand command)
            {
                // This is to cover the case where the test is decorated with the attribute
                // directly.
                return new LuceneDelegatingTestCommand(command, () => !TestWeekly, SKIP_REASON);
            }
        }

        /// <summary>
        /// Attribute for tests which exhibit a known issue and are temporarily disabled.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "API looks better with this nested.")]
        public sealed class AwaitsFixAttribute : System.Attribute
            , IApplyToTest, IApplyToContext, IWrapTestMethod
        {
            void IApplyToTest.ApplyToTest(NUnit.Framework.Internal.Test test)
            {
                // This method is called before initialization. The only thing
                // we can do here is set the category.
                test.Properties.Add(PropertyNames.Category, "AwaitsFix");
            }

            void IApplyToContext.ApplyToContext(TestExecutionContext context)
            {
                // Cover the case where this attribute is applied to the whole test fixture
                var currentTest = context.CurrentTest;
                if (!TestAwaitsFix && currentTest is NUnit.Framework.Internal.TestFixture fixture)
                {
                    foreach (var testInterface in fixture.Tests)
                    {
                        var test = (NUnit.Framework.Internal.Test)testInterface;
                        test.RunState = RunState.Skipped;
                        test.Properties.Set(PropertyNames.SkipReason, BugUrl);
                    }
                }
            }

            TestCommand ICommandWrapper.Wrap(TestCommand command)
            {
                // This is to cover the case where the test is decorated with the attribute
                // directly.
                return new LuceneDelegatingTestCommand(command, () => !TestAwaitsFix, BugUrl) { RunState = RunState.Ignored };
            }

            /// <summary>
            /// Point to issue tracker entry. </summary>
            public string BugUrl { get; set; } = "A known bug is being investigated regarding this issue.";
        }

        /// <summary>
        /// Attribute for tests that are slow. Slow tests do run by default but can be
        /// disabled if a quick run is needed.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "API looks better with this nested.")]
        public sealed class SlowAttribute : System.Attribute
            , IApplyToTest, IApplyToContext, IWrapTestMethod
        {
            void IApplyToTest.ApplyToTest(NUnit.Framework.Internal.Test test)
            {
                // This method is called before initialization. The only thing
                // we can do here is set the category.
                test.Properties.Add(PropertyNames.Category, "Slow");
            }

            void IApplyToContext.ApplyToContext(TestExecutionContext context)
            {
                // Cover the case where this attribute is applied to the whole test fixture
                var currentTest = context.CurrentTest;
                if (!TestSlow && currentTest is NUnit.Framework.Internal.TestFixture fixture)
                {
                    foreach (var testInterface in fixture.Tests)
                    {
                        var test = (NUnit.Framework.Internal.Test)testInterface;
                        test.RunState = RunState.Skipped;
                        test.Properties.Set(PropertyNames.SkipReason, Message);
                    }
                }
            }

            TestCommand ICommandWrapper.Wrap(TestCommand command)
            {
                // This is to cover the case where the test is decorated with the attribute
                // directly.
                return new LuceneDelegatingTestCommand(command, () => !TestSlow, Message);
            }

            public string Message { get; set; } = "This test is slow.";
        }

        /////// <summary>
        /////// Attribute for tests that fail frequently and should
        /////// be moved to a <a href="https://builds.apache.org/job/Lucene-BadApples-trunk-java7/">"vault" plan in Jenkins</a>.
        ///////
        /////// Tests annotated with this will be turned off by default. If you want to enable
        /////// them, set:
        /////// <pre>
        /////// -Dtests.badapples=true
        /////// </pre>
        /////// </summary>
        ////[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
        ////[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "API looks better with this nested.")]
        ////public class BadAppleAttribute : System.Attribute
        ////{
        ////    /// <summary>
        ////    /// Point to issue tracker entry. </summary>
        ////    public virtual string BugUrl { get; set; }
        ////}

        /// <summary>
        /// Attribute for test classes that should avoid certain codec types
        /// (because they are expensive, for example).
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "API looks better with this nested.")]
        public sealed class SuppressCodecsAttribute : System.Attribute
        {
            private static readonly Regex WHITESPACE_REMOVAL = new Regex(@"\s*,\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

            /// <summary>
            /// Constructor for CLS compliance.
            /// </summary>
            /// <param name="codecs">A comma-deliminated set of codec names.</param>
            public SuppressCodecsAttribute(string codecs)
            {
                this.Value = WHITESPACE_REMOVAL.Split(codecs);
            }

            [CLSCompliant(false)]
            public SuppressCodecsAttribute(params string[] value)
            {
                this.Value = value;
            }

            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public string[] Value { get; private set; }
        }

        // LUCENENET TODO: Finish implementation
        /// <summary>
        /// Marks any suites which are known not to close all the temporary
        /// files. This may prevent temp files and folders from being cleaned
        /// up after the suite is completed.
        /// </summary>
        /// <seealso cref="LuceneTestCase.CreateTempDir()"/>
        /// <seealso cref="LuceneTestCase.CreateTempFile(string, string)"/>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "API looks better with this nested.")]
        public sealed class SuppressTempFileChecksAttribute : System.Attribute
        {
            /// <summary>
            /// Point to issue tracker entry. </summary>
            public string BugUrl { get; set; } = "None";
        }

        // -----------------------------------------------------------------
        // Truly immutable fields and constants, initialized once and valid
        // for all suites ever since.
        // -----------------------------------------------------------------

        // :Post-Release-Update-Version.LUCENE_XY:
        /// <summary>
        /// Use this constant when creating Analyzers and any other version-dependent stuff.
        /// <para/><b>NOTE:</b> Change this when development starts for new Lucene version:
        /// </summary>
        public const LuceneVersion TEST_VERSION_CURRENT = LuceneVersion.LUCENE_48;

        // LUCENENET specific - need to jump through a few hoops in order to
        // ensure the test configuration data is loaded before anything utilizes
        // it. We simply isolate this all in a SystemPropertyData class that is
        // lazily loaded the first time the SystemPropertyHolder property is called.
        private static SystemPropertyData testProperties;
        private static SystemPropertyData TestProperties
            => LazyInitializer.EnsureInitialized(ref testProperties, () => new SystemPropertyData());

        /// <summary>
        /// Used to defer static initialization of system properties
        /// until they are called explicitly.
        /// </summary>
        private class SystemPropertyData // LUCENENET specific
        {
            public SystemPropertyData()
            {
                Verbose = SystemProperties.GetPropertyAsBoolean("tests:verbose", // LUCENENET specific - reformatted with :
#if DEBUG
                    true
#else
                    false
#endif
                );
                UseInfoStream = SystemProperties.GetPropertyAsBoolean("tests:infostream", Verbose); // LUCENENET specific - reformatted with :
                RandomMultiplier = SystemProperties.GetPropertyAsInt32("tests:multiplier", 1); // LUCENENET specific - reformatted with :
                TestCodec = SystemProperties.GetProperty("tests:codec", "random");// LUCENENET specific - reformatted with :
                TestPostingsFormat = SystemProperties.GetProperty("tests:postingsformat", "random"); // LUCENENET specific - reformatted with :
                TestDocValuesFormat = SystemProperties.GetProperty("tests:docvaluesformat", "random"); // LUCENENET specific - reformatted with :
                TestDirectory = SystemProperties.GetProperty("tests:directory", "random"); // LUCENENET specific - reformatted with :
                TestLineDocsFile = SystemProperties.GetProperty("tests:linedocsfile", DEFAULT_LINE_DOCS_FILE); // LUCENENET specific - reformatted with :
                TestNightly = SystemProperties.GetPropertyAsBoolean(SYSPROP_NIGHTLY, false);
                TestWeekly = SystemProperties.GetPropertyAsBoolean(SYSPROP_WEEKLY, false);
                TestAwaitsFix = SystemProperties.GetPropertyAsBoolean(SYSPROP_AWAITSFIX, false);
                TestSlow = SystemProperties.GetPropertyAsBoolean(SYSPROP_SLOW, true); // LUCENENET specific - made default true, as per the docs
                TestThrottling = TestNightly ? Throttling.SOMETIMES : Throttling.NEVER;
                LeaveTemporary = LoadLeaveTemorary();
            }


            /// <summary>
            /// True if and only if tests are run in verbose mode. If this flag is false
            /// tests are not expected to print any messages.
            /// </summary>
            public bool Verbose { get; }

            /// <summary>
            /// TODO: javadoc? </summary>
            public bool UseInfoStream { get; } 

            /// <summary>
            /// A random multiplier which you should use when writing random tests:
            /// multiply it by the number of iterations to scale your tests (for nightly builds).
            /// </summary>
            public int RandomMultiplier { get; } // LUCENENET specific - changed from field to property, and renamed

            /// <summary>
            /// Gets the codec to run tests with. </summary>
            public string TestCodec { get; } // LUCENENET specific - changed from field to property, and renamed

            /// <summary>
            /// Gets the postingsFormat to run tests with. </summary>
            public string TestPostingsFormat { get; } // LUCENENET specific - changed from field to property, and renamed

            /// <summary>
            /// Gets the docValuesFormat to run tests with </summary>
            public string TestDocValuesFormat { get; } // LUCENENET specific - changed from field to property, and renamed

            /// <summary>
            /// Gets the directory to run tests with </summary>
            public string TestDirectory { get; } // LUCENENET specific - changed from field to property, and renamed

            /// <summary>
            /// The line file used by LineFileDocs </summary>
            public string TestLineDocsFile { get; set; } // LUCENENET specific - changed from field to property, and renamed

            /// <summary>
            /// Whether or not <see cref="NightlyAttribute"/> tests should run. </summary>
            public bool TestNightly { get; } // LUCENENET specific - changed from field to property, and renamed

            /// <summary>
            /// Whether or not <see cref="WeeklyAttribute"/> tests should run. </summary>
            public bool TestWeekly { get; } // LUCENENET specific - changed from field to property, and renamed

            /// <summary>
            /// Whether or not <see cref="AwaitsFixAttribute"/> tests should run. </summary>
            public bool TestAwaitsFix { get; } // LUCENENET specific - changed from field to property, and renamed

            /// <summary>
            /// Whether or not <see cref="SlowAttribute"/> tests should run. </summary>
            public bool TestSlow { get; } // LUCENENET specific - changed from field to property, and renamed

            /// <summary>
            /// Throttling, see <see cref="MockDirectoryWrapper.Throttling"/>. </summary>
            public Throttling TestThrottling { get; }

            /// <summary>
            /// Leave temporary files on disk, even on successful runs. </summary>
            public bool LeaveTemporary { get; }

            private static bool LoadLeaveTemorary()
            {
                bool defaultValue = false;
                // LUCENENET specific - reformatted with :
                foreach (string property in new string[] {
                    "tests:leaveTemporary" /* ANT tasks's (junit4) flag. */,
                    "tests:leavetemporary" /* lowercase */,
                    "tests:leavetmpdir" /* default */,
                    "solr:test:leavetmpdir" /* Solr's legacy */
                })
                {
                    defaultValue |= SystemProperties.GetPropertyAsBoolean(property, false);
                }
                return defaultValue;
            }
        }


        /// <summary>
        /// True if and only if tests are run in verbose mode. If this flag is false
        /// tests are not expected to print any messages.
        /// </summary>
        public static bool Verbose => TestProperties.Verbose;

        /// <summary>
        /// TODO: javadoc? </summary>
        public static bool UseInfoStream => TestProperties.UseInfoStream;

        /// <summary>
        /// A random multiplier which you should use when writing random tests:
        /// multiply it by the number of iterations to scale your tests (for nightly builds).
        /// </summary>
        public static int RandomMultiplier => TestProperties.RandomMultiplier; // LUCENENET specific - changed from field to property, and renamed

        /// <summary>
        /// TODO: javadoc? </summary>
        public const string DEFAULT_LINE_DOCS_FILE = "europarl.lines.txt.gz";

        /// <summary>
        /// TODO: javadoc? </summary>
        public const string JENKINS_LARGE_LINE_DOCS_FILE = "enwiki.random.lines.txt";

        /// <summary>
        /// Gets the codec to run tests with. </summary>
        public static string TestCodec => TestProperties.TestCodec; // LUCENENET specific - changed from field to property, and renamed

        /// <summary>
        /// Gets the postingsFormat to run tests with. </summary>
        public static string TestPostingsFormat => TestProperties.TestPostingsFormat; // LUCENENET specific - changed from field to property, and renamed

        /// <summary>
        /// Gets the docValuesFormat to run tests with </summary>
        public static string TestDocValuesFormat => TestProperties.TestDocValuesFormat; // LUCENENET specific - changed from field to property, and renamed

        /// <summary>
        /// Gets the directory to run tests with </summary>
        public static string TestDirectory => TestProperties.TestDirectory; // LUCENENET specific - changed from field to property, and renamed

        /// <summary>
        /// The line file used by <see cref="LineFileDocs"/> </summary>
        public static string TestLineDocsFile // LUCENENET specific - changed from field to property, and renamed
        {
            get => TestProperties.TestLineDocsFile;
            internal set => TestProperties.TestLineDocsFile = value;
        }

        /// <summary>
        /// Whether or not <see cref="NightlyAttribute"/> tests should run. </summary>
        public static bool TestNightly => TestProperties.TestNightly; // LUCENENET specific - changed from field to property, and renamed

        /// <summary>
        /// Whether or not <see cref="WeeklyAttribute"/> tests should run. </summary>
        public static bool TestWeekly => TestProperties.TestWeekly; // LUCENENET specific - changed from field to property, and renamed

        /// <summary>
        /// Whether or not <see cref="AwaitsFixAttribute"/> tests should run. </summary>
        public static bool TestAwaitsFix => TestProperties.TestAwaitsFix; // LUCENENET specific - changed from field to property, and renamed

        /// <summary>
        /// Whether or not <see cref="SlowAttribute"/> tests should run. </summary>
        public static bool TestSlow => TestProperties.TestSlow; // LUCENENET specific - changed from field to property, and renamed

        /// <summary>
        /// Throttling, see <see cref="MockDirectoryWrapper.Throttling"/>. </summary>
        internal static Throttling TestThrottling => TestProperties.TestThrottling; // LUCENENET specific - made internal, because not fully implemented

        /// <summary>
        /// Leave temporary files on disk, even on successful runs. </summary>
        public static bool LeaveTemporary => TestProperties.LeaveTemporary;


        // LUCENENET: Not Implemented
        /////// <summary>
        /////// These property keys will be ignored in verification of altered properties. </summary>
        /////// <seealso> cref= SystemPropertiesInvariantRule </seealso>
        /////// <seealso> cref= #ruleChain </seealso>
        /////// <seealso> cref= #classRules </seealso>
        ////private static readonly string[] IGNORED_INVARIANT_PROPERTIES = { "user.timezone", "java.rmi.server.randomIDs" };

        /// <summary>
        /// Filesystem-based <see cref="Directory"/> implementations. </summary>
        private static readonly IList<string> FS_DIRECTORIES = new string[] {
            "SimpleFSDirectory",
            "NIOFSDirectory",
            "MMapDirectory"
        };

        /// <summary>
        /// All <see cref="Directory"/> implementations. </summary>
        private static readonly IList<string> CORE_DIRECTORIES = LoadCoreDirectories();

        private static IList<string> LoadCoreDirectories()
        {
            return new JCG.List<string>(FS_DIRECTORIES)
            {
                "RAMDirectory"
            };
        }

        protected static ICollection<string> DoesntSupportOffsets { get; } = new JCG.HashSet<string>
        {
            "Lucene3x",
            "MockFixedIntBlock",
            "MockVariableIntBlock",
            "MockSep",
            "MockRandom"
        };

        // -----------------------------------------------------------------
        // Fields initialized in class or instance rules.
        // -----------------------------------------------------------------

        /// <summary>
        /// When <c>true</c>, Codecs for old Lucene version will support writing
        /// indexes in that format. Defaults to <c>false</c>, can be disabled by
        /// specific tests on demand.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public static bool OldFormatImpersonationIsActive { get; set; } = false; // LUCENENET specific - made into a property, since this is intended for end users to set

        // -----------------------------------------------------------------
        // Class level (suite) rules.
        // -----------------------------------------------------------------

        ///// <summary>
        ///// Stores the currently class under test.
        ///// </summary>
        //private static TestRuleStoreClassName ClassNameRule;

        /// <summary>
        /// Class environment setup rule.
        /// </summary>
        internal static TestRuleSetupAndRestoreClassEnv ClassEnvRule { get; } = new TestRuleSetupAndRestoreClassEnv();

        // LUCENENET TODO
        /// <summary>
        /// Suite failure marker (any error in the test or suite scope).
        /// </summary>
        public static readonly /*TestRuleMarkFailure*/ bool SuiteFailureMarker = true; // Means: was successful

        ///// <summary>
        ///// Ignore tests after hitting a designated number of initial failures. This
        ///// is truly a "static" global singleton since it needs to span the lifetime of all
        ///// test classes running inside this AppDomain (it cannot be part of a class rule).
        /////
        ///// <para/>This poses some problems for the test framework's tests because these sometimes
        ///// trigger intentional failures which add up to the global count. This field contains
        ///// a (possibly) changing reference to <see cref="TestRuleIgnoreAfterMaxFailures"/> and we
        ///// dispatch to its current value from the <see cref="#classRules"/> chain using <see cref="TestRuleDelegate"/>.
        ///// </summary>
        ////private static AtomicReference<TestRuleIgnoreAfterMaxFailures> IgnoreAfterMaxFailuresDelegate = LoadMaxFailuresDelegate();

        //////private static TestRule IgnoreAfterMaxFailures = LoadIgnoreAfterMaxFailures();

        ////private static AtomicReference<TestRuleIgnoreAfterMaxFailures> LoadMaxFailuresDelegate()
        ////{
        ////    int maxFailures = SystemProperties.GetPropertyAsInt32(SYSPROP_MAXFAILURES, int.MaxValue);
        ////    bool failFast = SystemProperties.GetPropertyAsBoolean(SYSPROP_FAILFAST, false);

        ////    if (failFast)
        ////    {
        ////        if (maxFailures == int.MaxValue)
        ////        {
        ////            maxFailures = 1;
        ////        }
        ////        else
        ////        {
        ////            Console.Out.Write(typeof(LuceneTestCase).Name + " WARNING: Property '" + SYSPROP_MAXFAILURES + "'=" + maxFailures + ", 'failfast' is" + " ignored.");
        ////        }
        ////    }

        ////    return new AtomicReference<TestRuleIgnoreAfterMaxFailures>(new TestRuleIgnoreAfterMaxFailures(maxFailures));
        ////}

        ////private static TestRule LoadIgnoreAfterMaxFailures()
        ////{
        ////    return TestRuleDelegate.Of(IgnoreAfterMaxFailuresDelegate);
        ////}

        /////// <summary>
        /////// Temporarily substitute the global <seealso cref="TestRuleIgnoreAfterMaxFailures"/>. See
        /////// <seealso cref="#ignoreAfterMaxFailuresDelegate"/> for some explanation why this method
        /////// is needed.
        /////// </summary>
        /////*public static TestRuleIgnoreAfterMaxFailures ReplaceMaxFailureRule(TestRuleIgnoreAfterMaxFailures newValue)
        ////{
        ////  return IgnoreAfterMaxFailuresDelegate.GetAndSet(newValue);
        ////}*/

        /////// <summary>
        /////// Max 10mb of static data stored in a test suite class after the suite is complete.
        /////// Prevents static data structures leaking and causing OOMs in subsequent tests.
        /////// </summary>
        //////private static readonly long STATIC_LEAK_THRESHOLD = 10 * 1024 * 1024;

        /////// <summary>
        /////// By-name list of ignored types like loggers etc. </summary>
        //////private static ISet<string> STATIC_LEAK_IGNORED_TYPES = new JCG.HashSet<string>(new string[] { "org.slf4j.Logger", "org.apache.solr.SolrLogFormatter", typeof(EnumSet).Name });

        /////// <summary>
        /////// this controls how suite-level rules are nested. It is important that _all_ rules declared
        /////// in <seealso cref="LuceneTestCase"/> are executed in proper order if they depend on each
        /////// other.
        /////// </summary>

        ////public static TestRule classRules = RuleChain
        ////  .outerRule(new TestRuleIgnoreTestSuites())
        ////  .around(IgnoreAfterMaxFailures)
        ////  .around(SuiteFailureMarker = new TestRuleMarkFailure())
        ////  .around(new TestRuleAssertionsRequired())
        ////  .around(new TemporaryFilesCleanupRule())
        ////  .around(new StaticFieldsInvariantRule(STATIC_LEAK_THRESHOLD, true)
        ////  {
        ////      @Override
        ////      protected bool accept(System.Reflection.FieldInfo field)
        ////      {
        ////          if (STATIC_LEAK_IGNORED_TYPES.contains(field.Type.Name))
        ////          {
        ////              return false;
        ////          }
        ////          if (field.DeclaringClass == typeof(LuceneTestCase))
        ////          {
        ////              return false;
        ////          }
        ////          return base.accept(field);
        ////      }})
        ////      .around(new NoClassHooksShadowingRule())
        ////      .around(new NoInstanceHooksOverridesRule()
        ////      {
        ////      @Override
        ////      protected bool verify(Method key)
        ////      {
        ////          string name = key.Name;
        ////          return !(name.Equals("SetUp", StringComparison.Ordinal) || name.Equals("TearDown", StringComparison.Ordinal));
        ////      }})
        ////      .around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES))
        ////      .around(ClassNameRule = new TestRuleStoreClassName())
        ////      .around(ClassEnvRule = new TestRuleSetupAndRestoreClassEnv());
        ////*/

        // Don't count known classes that consume memory once.
        // Don't count references from ourselves, we're top-level.

        // -----------------------------------------------------------------
        // Test level rules.
        // -----------------------------------------------------------------
        /////// <summary>
        /////// Enforces <seealso cref="#setUp()"/> and <seealso cref="#tearDown()"/> calls are chained. </summary>
        /////*private TestRuleSetupTeardownChained parentChainCallRule = new TestRuleSetupTeardownChained();

        /////// <summary>
        /////// Save test thread and name. </summary>
        ////private TestRuleThreadAndTestName threadAndTestNameRule = new TestRuleThreadAndTestName();

        /////// <summary>
        /////// Taint suite result with individual test failures. </summary>
        ////private TestRuleMarkFailure testFailureMarker = new TestRuleMarkFailure(SuiteFailureMarker);*/

        /////// <summary>
        /////// this controls how individual test rules are nested. It is important that
        /////// _all_ rules declared in <seealso cref="LuceneTestCase"/> are executed in proper order
        /////// if they depend on each other.
        /////// </summary>
        ////public TestRule ruleChain = RuleChain
        ////    .outerRule(TestFailureMarker)
        ////    .around(IgnoreAfterMaxFailures)
        ////    .around(ThreadAndTestNameRule)
        ////    .around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES))
        ////    .around(new TestRuleSetupAndRestoreInstanceEnv()).
        ////    around(new TestRuleFieldCacheSanity()).
        ////    around(ParentChainCallRule);
        ////*/

        // -----------------------------------------------------------------
        // Suite and test case setup/ cleanup.
        // -----------------------------------------------------------------

        // LUCENENET specific: Temporary storage for random selections so they
        // can be set once per OneTimeSetUp and reused multiple times in SetUp
        // where they are written to the output.
        private string codecType;
        private string similarityName;

        /// <summary>
        /// For subclasses to override. Overrides must call <c>base.SetUp()</c>.
        /// </summary>
        [Before]
        public virtual void SetUp()
        {
            // LUCENENET TODO: Not sure how to convert these
            //ParentChainCallRule.SetupCalled = true;

            // LUCENENET: Printing out randomized context regardless
            // of whether verbose is enabled (since we need it for debugging,
            // but the verbose output can crash tests).
            Console.Write("RandomSeed: ");
            Console.WriteLine(RandomizedContext.CurrentContext.RandomSeedAsHex);

            Console.Write("Culture: ");
            Console.WriteLine(ClassEnvRule.locale.Name);

            Console.Write("Time Zone: ");
            Console.WriteLine(ClassEnvRule.timeZone.DisplayName);

            Console.Write("Default Codec: ");
            Console.Write(ClassEnvRule.codec.Name);
            Console.Write(" (");
            Console.Write(codecType);
            Console.WriteLine(")");

            Console.Write("Default Similarity: ");
            Console.WriteLine(similarityName);

            Console.Write("Nightly: ");
            Console.WriteLine(TestNightly);

            Console.Write("Weekly: ");
            Console.WriteLine(TestWeekly);

            Console.Write("Slow: ");
            Console.WriteLine(TestSlow);

            Console.Write("Awaits Fix: ");
            Console.WriteLine(TestAwaitsFix);

            Console.Write("Directory: ");
            Console.WriteLine(TestDirectory);

            Console.Write("Verbose: ");
            Console.WriteLine(Verbose);

            Console.Write("Random Multiplier: ");
            Console.WriteLine(RandomMultiplier);
        }

        /// <summary>
        /// For subclasses to override. Overrides must call <c>base.TearDown()</c>.
        /// </summary>
        [After]
        public virtual void TearDown()
        {
            /* LUCENENET TODO: Not sure how to convert these
                ParentChainCallRule.TeardownCalled = true;
                */

            TestResult result = TestExecutionContext.CurrentContext.CurrentResult;
            string message;
            if (result.ResultState == ResultState.Failure || result.ResultState == ResultState.Error)
            {
                message = result.Message + $"\n\nTo reproduce this test result:\n\n" +
                    $"Option 1:\n\n" +
                    $" Apply the following assembly-level attributes:\n\n" +
                    $"[assembly: Lucene.Net.Util.RandomSeed(\"{RandomizedContext.CurrentContext.RandomSeedAsHex}\")]\n" +
                    $"[assembly: NUnit.Framework.SetCulture(\"{Thread.CurrentThread.CurrentCulture.Name}\")]\n\n" +
                    $"Option 2:\n\n" +
                    $" Use the following .runsettings file:\n\n" +
                    $"<RunSettings>\n" +
                    $"  <TestRunParameters>\n" +
                    $"    <Parameter name=\"tests:seed\" value=\"{RandomizedContext.CurrentContext.RandomSeedAsHex}\" />\n" +
                    $"    <Parameter name=\"tests:culture\" value=\"{Thread.CurrentThread.CurrentCulture.Name}\" />\n" +
                    $"  </TestRunParameters>\n" +
                    $"</RunSettings>\n\n" +
                    $"See the .runsettings documentation at: https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file.";
                result.SetResult(result.ResultState, message, result.StackTrace);
            }
        }

        /// <summary>
        /// Sets up dependency injection of codec factories for running the test class,
        /// and also picks random defaults for culture, time zone, similarity, and default codec.
        /// <para/>
        /// If you override this method, be sure to call <c>base.BeforeClass()</c> BEFORE setting
        /// up your test fixture.
        /// </summary>
        // LUCENENET specific method for setting up dependency injection of test classes.
        [OneTimeSetUp]
        public virtual void BeforeClass()
        {
            try
            {
                ClassEnvRule.Before();

                // LUCENENET: Generate the info once so it can be printed out for each test
                codecType = ClassEnvRule.codec.GetType().Name;
                similarityName = ClassEnvRule.similarity.ToString();
            }
            catch (Exception ex)
            {
                // Write the stack trace so we have something to go on if an error occurs here.
                throw new Exception($"An exception occurred during BeforeClass:\n{ex}", ex);
            }
        }

        /// <summary>
        /// Tears down random defaults and cleans up temporary files.
        /// <para/>
        /// If you override this method, be sure to call <c>base.AfterClass()</c> AFTER
        /// tearing down your test fixture.
        /// </summary>
        // LUCENENET specific method for setting up dependency injection of test classes.
        [OneTimeTearDown]
        public virtual void AfterClass()
        {
            try
            {
                ClassEnvRule.After();
                CleanupTemporaryFiles();
            }
            catch (Exception ex)
            {
                // Write the stack trace so we have something to go on if an error occurs here.
                throw new Exception($"An exception occurred during AfterClass:\n{ex}", ex);
            }
        }

        // -----------------------------------------------------------------
        // Test facilities and facades for subclasses.
        // -----------------------------------------------------------------

        /// <summary>
        /// Access to the current <see cref="System.Random"/> instance. It is safe to use
        /// this method from multiple threads, etc., but it should be called while within a runner's
        /// scope (so no static initializers). The returned <see cref="System.Random"/> instance will be
        /// <b>different</b> when this method is called inside a <see cref="BeforeClass()"/> hook (static
        /// suite scope) and within <see cref="Before"/>/ <see cref="After"/> hooks or test methods.
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
        [SuppressMessage("Style", "IDE0025:Use expression body for properties", Justification = "Multiple lines")]
        public static Random Random
        {
            get
            {
                var context = RandomizedContext.CurrentContext;
                if (context is null)
                    Assert.Fail("LuceneTestCase.Random may only be used within tests/setup/teardown context in subclasses of LuceneTestCase or LuceneTestFrameworkInitializer.");
                return context.RandomGenerator;
            }
        }

        /////// <summary>
        /////// Registers a <see cref="IDisposable"/> resource that should be closed after the test
        /////// completes.
        /////// </summary>
        /////// <returns> <c>resource</c> (for call chaining). </returns>
        /////*public static T CloseAfterTest<T>(T resource)
        ////{
        ////    return RandomizedContext.Current.CloseAtEnd(resource, LifecycleScope.TEST);
        ////}*/

        /////// <summary>
        /////// Registers a <see cref="IDisposable"/> resource that should be closed after the suite
        /////// completes.
        /////// </summary>
        /////// <returns> <c>resource</c> (for call chaining). </returns>
        /////*public static T CloseAfterSuite<T>(T resource)
        ////{
        ////    return RandomizedContext.Current.CloseAtEnd(resource, LifecycleScope.SUITE);
        ////}*/

        /// <summary>
        /// Gets the current type being tested.
        /// </summary>
        public static Type TestType
            => TestExecutionContext.CurrentContext.CurrentTest.Fixture?.GetType(); // LUCENENET specific - renamed from testClass()

        /// <summary>
        /// Return the name of the currently executing test case.
        /// </summary>
        public virtual string TestName => NUnit.Framework.TestContext.CurrentContext.Test.MethodName;

        /// <summary>
        /// Some tests expect the directory to contain a single segment, and want to
        /// do tests on that segment's reader. This is an utility method to help them.
        /// </summary>
        public static SegmentReader GetOnlySegmentReader(DirectoryReader reader)
        {
            IList<AtomicReaderContext> subReaders = reader.Leaves;
            if (subReaders.Count != 1)
            {
                throw new ArgumentException(reader + " has " + subReaders.Count + " segments instead of exactly one");
            }
            AtomicReader r = (AtomicReader)subReaders[0].Reader;
            Assert.IsTrue(r is SegmentReader);
            return (SegmentReader)r;
        }

        /// <summary>
        /// Returns true if and only if the calling thread is the primary thread
        /// executing the test case.
        /// <para/>
        /// LUCENENET: Not Implemented - always returns true
        /// </summary>
        /*Assert.IsNotNull(ThreadAndTestNameRule.TestCaseThread, "Test case thread not set?");
                return Thread.CurrentThread == ThreadAndTestNameRule.TestCaseThread;*/
        internal static bool IsTestThread => true; // LUCENENET specific - changed from public to internal since there is no way to support it

        /// <summary>
        /// Asserts that <see cref="FieldCacheSanityChecker"/> does not detect any
        /// problems with <see cref="FieldCache.DEFAULT"/>.
        /// <para>
        /// If any problems are found, they are logged to <see cref="Console.Error"/>
        /// (allong with the msg) when the Assertion is thrown.
        /// </para>
        /// <para>
        /// This method is called by <see cref="TearDown()"/> after every test method,
        /// however <see cref="IndexReader"/>s scoped inside test methods may be garbage
        /// collected prior to this method being called, causing errors to
        /// be overlooked. Tests are encouraged to keep their <see cref="IndexReader"/>s
        /// scoped at the class level, or to explicitly call this method
        /// directly in the same scope as the <see cref="IndexReader"/>.
        /// </para>
        /// </summary>
        /// <seealso cref="Lucene.Net.Util.FieldCacheSanityChecker"/>
        protected static void AssertSaneFieldCaches(string msg)
        {
            CacheEntry[] entries = FieldCache.DEFAULT.GetCacheEntries();
            Insanity[] insanity = null;
            try
            {
                try
                {
                    insanity = FieldCacheSanityChecker.CheckSanity(entries);
                }
                catch (Exception e) when (e.IsRuntimeException())
                {
                    DumpArray(msg + ": FieldCache", entries, Console.Error);
                    throw;  // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }

                Assert.AreEqual(0, insanity.Length, msg + ": Insane FieldCache usage(s) found");
                insanity = null;
            }
            finally
            {
                // report this in the event of any exception/failure
                // if no failure, then insanity will be null anyway
                if (null != insanity)
                {
                    DumpArray(msg + ": Insane FieldCache usage(s)", insanity, Console.Error);
                }
            }
        }

        /// <summary>
        /// Returns a number of at least <c>i</c>
        /// <para/>
        /// The actual number returned will be influenced by whether <see cref="TestNightly"/>
        /// is active and <see cref="RandomMultiplier"/>, but also with some random fudge.
        /// </summary>
        public static int AtLeast(Random random, int i)
        {
            //int min = (TestNightly ? 2 * i : i) * RandomMultiplier;
            // LUCENENET specific - reduced nightly factor to lower the
            // total test time in Nightly builds to get under the 1 hour time limit of Azure DevOps
            int min = (TestNightly ? (int)(1.5 * i) : i) * RandomMultiplier;
            int max = min + (min / 2);
            return TestUtil.NextInt32(random, min, max);
        }

        public static int AtLeast(int i)
        {
            return AtLeast(Random, i);
        }

        /// <summary>
        /// Returns true if something should happen rarely,
        /// <para/>
        /// The actual number returned will be influenced by whether <see cref="TestNightly"/>
        /// is active and <see cref="RandomMultiplier"/>.
        /// </summary>
        public static bool Rarely(Random random)
        {
            //int p = TestNightly ? 10 : 1;
            // LUCENENET specific - reduced nightly instance by 1/2 to lower the
            // total test time in Nightly builds to get under the 1 hour time limit of Azure DevOps
            int p = TestNightly ? 5 : 1;
            p += (int)(p * Math.Log(RandomMultiplier));
            int min = 100 - Math.Min(p, 50); // never more than 50
            return random.Next(100) >= min;
        }

        public static bool Rarely()
        {
            return Rarely(Random);
        }

        public static bool Usually(Random random)
        {
            return !Rarely(random);
        }

        public static bool Usually()
        {
            return Usually(Random);
        }

        /// <param name="msg">Message to be included in the exception's string.</param>
        /// <param name="condition">
        /// If <c>false</c> an <see cref="AssumptionViolatedException"/> is
        /// thrown by this method and the test case (should be) ignored (or
        /// rather technically, flagged as a failure not passing a certain
        /// assumption). Tests that are assumption-failures do not break
        /// builds (again: typically).
        /// </param>
        public static void AssumeTrue(string msg, bool condition) // LUCENENET: From RandomizedTest
        {
            NUnit.Framework.Assume.That(condition, msg);
        }

        /// <param name="msg">Message to be included in the exception's string.</param>
        /// <param name="condition">
        /// If <c>true</c> an <see cref="AssumptionViolatedException"/> is
        /// thrown by this method and the test case (should be) ignored (or
        /// rather technically, flagged as a failure not passing a certain
        /// assumption). Tests that are assumption-failures do not break
        /// builds (again: typically).
        /// </param>
        public static void AssumeFalse(string msg, bool condition) // LUCENENET: From RandomizedTest
        {
            NUnit.Framework.Assume.That(!condition, msg);
        }

        /// <summary>
        /// Assume <paramref name="e"/> is <c>null</c>.
        /// </summary>
        public static void AssumeNoException(string msg, Exception e) // LUCENENET: From RandomizedTest
        {
            if (e != null)
            {
                // This does chain the exception as the cause.
                throw new AssumptionViolatedException(msg, e);
            }
        }

        /// <summary>
        /// Return <paramref name="args"/> as a <see cref="ISet{Object}"/> instance. The order of elements is not
        /// preserved in enumerators.
        /// </summary>
        public static ISet<object> AsSet(params object[] args)
        {
            return new JCG.HashSet<object>(args);
        }

        /// <summary>
        /// Convenience method for logging an enumerator.
        /// </summary>
        /// <param name="label">  String logged before/after the items in the enumerator. </param>
        /// <param name="iter">   Each element is ToString()ed and logged on it's own line. If iter is null this is logged differnetly then an empty enumerator. </param>
        /// <param name="stream"> Stream to log messages to. </param>
        public static void DumpEnumerator(string label, IEnumerator iter, TextWriter stream) // LUCENENET specifc - renamed from DumpIterator
        {
            stream.WriteLine("*** BEGIN " + label + " ***");
            if (null == iter)
            {
                stream.WriteLine(" ... NULL ...");
            }
            else
            {
                while (iter.MoveNext())
                {
                    stream.WriteLine(Collections.ToString(iter.Current));
                }
            }
            stream.WriteLine("*** END " + label + " ***");
        }

        /// <summary>
        /// Convenience method for logging an array.  Wraps the array in an enumerator and delegates to <see cref="DumpEnumerator(string, IEnumerator, TextWriter)"/>
        /// </summary>
        /// <seealso cref="DumpEnumerator(string, IEnumerator, TextWriter)"/>
        public static void DumpArray(string label, object[] objs, TextWriter stream)
        {
            IEnumerator iter = (null == objs) ? (IEnumerator)null : objs.GetEnumerator();
            DumpEnumerator(label, iter, stream);
        }

        /// <summary>
        /// Create a new <see cref="IndexWriterConfig"/> with random defaults.
        /// </summary>
        public static IndexWriterConfig NewIndexWriterConfig(LuceneVersion v, Analyzer a)
        {
            return NewIndexWriterConfig(Random, v, a);
        }

        /// <summary>
        /// Create a new <see cref="IndexWriterConfig"/> with random defaults using the specified <paramref name="random"/>.
        /// </summary>
        /// <param name="random">A random instance (usually <see cref="LuceneTestCase.Random"/>).</param>
        /// <param name="v"></param>
        /// <param name="a"></param>
        public static IndexWriterConfig NewIndexWriterConfig(Random random, LuceneVersion v, Analyzer a)

        {
            IndexWriterConfig c = new IndexWriterConfig(v, a);
            c.SetSimilarity(ClassEnvRule.similarity);
            if (Verbose)
            {
                // Even though TestRuleSetupAndRestoreClassEnv calls
                // InfoStream.setDefault, we do it again here so that
                // the PrintStreamInfoStream.messageID increments so
                // that when there are separate instances of
                // IndexWriter created we see "IW 0", "IW 1", "IW 2",
                // ... instead of just always "IW 0":
                c.SetInfoStream(new TestRuleSetupAndRestoreClassEnv.ThreadNameFixingPrintStreamInfoStream(Console.Out));
            }

            if (random.NextBoolean())
            {
                c.SetMergeScheduler(new SerialMergeScheduler());
            }
            else if (Rarely(random))
            {
                int maxThreadCount = TestUtil.NextInt32(Random, 1, 4);
                int maxMergeCount = TestUtil.NextInt32(Random, maxThreadCount, maxThreadCount + 4);
                IConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
                cms.SetMaxMergesAndThreads(maxMergeCount, maxThreadCount);
                c.SetMergeScheduler(cms);
            }
            if (random.NextBoolean())
            {
                if (Rarely(random))
                {
                    // crazy value
                    c.SetMaxBufferedDocs(TestUtil.NextInt32(random, 2, 15));
                }
                else
                {
                    // reasonable value
                    c.SetMaxBufferedDocs(TestUtil.NextInt32(random, 16, 1000));
                }
            }
            if (random.NextBoolean())
            {
                if (Rarely(random))
                {
                    // crazy value
                    c.SetTermIndexInterval(random.NextBoolean() ? TestUtil.NextInt32(random, 1, 31) : TestUtil.NextInt32(random, 129, 1000));
                }
                else
                {
                    // reasonable value
                    c.SetTermIndexInterval(TestUtil.NextInt32(random, 32, 128));
                }
            }
            if (random.NextBoolean())
            {
                int maxNumThreadStates = Rarely(random) ? TestUtil.NextInt32(random, 5, 20) : TestUtil.NextInt32(random, 1, 4); // reasonable value -  crazy value

                // LUCENENET specific - Removed RandomDocumentsWriterPerThreadPool, as was done in Lucene 4.8.1 (see #208)
                c.SetMaxThreadStates(maxNumThreadStates);
            }
            c.SetMergePolicy(NewMergePolicy(random));
            if (Rarely(random))
            {
                c.SetMergedSegmentWarmer(new SimpleMergedSegmentWarmer(c.InfoStream));
            }
            c.SetUseCompoundFile(random.NextBoolean());
            c.SetReaderPooling(random.NextBoolean());
            c.SetReaderTermsIndexDivisor(TestUtil.NextInt32(random, 1, 4));
            c.SetCheckIntegrityAtMerge(random.NextBoolean());
            return c;
        }

        public static MergePolicy NewMergePolicy(Random r)
        {
            if (Rarely(r))
            {
                return new MockRandomMergePolicy(r);
            }
            else if (r.NextBoolean())
            {
                return NewTieredMergePolicy(r);
            }
            else if (r.Next(5) == 0)
            {
                return NewAlcoholicMergePolicy(r, ClassEnvRule.timeZone);
            }
            return NewLogMergePolicy(r);
        }

        public static MergePolicy NewMergePolicy()
        {
            return NewMergePolicy(Random);
        }

        public static LogMergePolicy NewLogMergePolicy()
        {
            return NewLogMergePolicy(Random);
        }

        public static TieredMergePolicy NewTieredMergePolicy()
        {
            return NewTieredMergePolicy(Random);
        }

        public static AlcoholicMergePolicy NewAlcoholicMergePolicy()
        {
            return NewAlcoholicMergePolicy(Random, ClassEnvRule.timeZone);
        }

        public static AlcoholicMergePolicy NewAlcoholicMergePolicy(Random random, TimeZoneInfo timeZone)
        {
            return new AlcoholicMergePolicy(timeZone, new J2N.Randomizer(random.NextInt64()));
        }

        public static LogMergePolicy NewLogMergePolicy(Random random)
        {
            LogMergePolicy logmp = random.NextBoolean() ? (LogMergePolicy)new LogDocMergePolicy() : new LogByteSizeMergePolicy();

            logmp.CalibrateSizeByDeletes = random.NextBoolean();
            if (Rarely(random))
            {
                logmp.MergeFactor = TestUtil.NextInt32(random, 2, 9);
            }
            else
            {
                logmp.MergeFactor = TestUtil.NextInt32(random, 10, 50);
            }
            ConfigureRandom(random, logmp);
            return logmp;
        }

        private static void ConfigureRandom(Random random, MergePolicy mergePolicy)
        {
            if (random.NextBoolean())
            {
                mergePolicy.NoCFSRatio = 0.1 + random.NextDouble() * 0.8;
            }
            else
            {
                mergePolicy.NoCFSRatio = random.NextBoolean() ? 1.0 : 0.0;
            }

            if (Rarely())
            {
                mergePolicy.MaxCFSSegmentSizeMB = 0.2 + random.NextDouble() * 2.0;
            }
            else
            {
                mergePolicy.MaxCFSSegmentSizeMB = double.PositiveInfinity;
            }
        }

        public static TieredMergePolicy NewTieredMergePolicy(Random random)
        {
            TieredMergePolicy tmp = new TieredMergePolicy();
            if (Rarely(random))
            {
                tmp.MaxMergeAtOnce = TestUtil.NextInt32(random, 2, 9);
                tmp.MaxMergeAtOnceExplicit = TestUtil.NextInt32(random, 2, 9);
            }
            else
            {
                tmp.MaxMergeAtOnce = TestUtil.NextInt32(random, 10, 50);
                tmp.MaxMergeAtOnceExplicit = TestUtil.NextInt32(random, 10, 50);
            }
            if (Rarely(random))
            {
                tmp.MaxMergedSegmentMB = 0.2 + random.NextDouble() * 2.0;
            }
            else
            {
                tmp.MaxMergedSegmentMB = random.NextDouble() * 100;
            }
            tmp.FloorSegmentMB = 0.2 + random.NextDouble() * 2.0;
            tmp.ForceMergeDeletesPctAllowed = 0.0 + random.NextDouble() * 30.0;
            if (Rarely(random))
            {
                tmp.SegmentsPerTier = TestUtil.NextInt32(random, 2, 20);
            }
            else
            {
                tmp.SegmentsPerTier = TestUtil.NextInt32(random, 10, 50);
            }
            ConfigureRandom(random, tmp);
            tmp.ReclaimDeletesWeight = random.NextDouble() * 4;
            return tmp;
        }

        public static MergePolicy NewLogMergePolicy(bool useCFS)
        {
            MergePolicy logmp = NewLogMergePolicy();
            logmp.NoCFSRatio = useCFS ? 1.0 : 0.0;
            return logmp;
        }

        public static MergePolicy NewLogMergePolicy(bool useCFS, int mergeFactor)
        {
            LogMergePolicy logmp = NewLogMergePolicy();
            logmp.NoCFSRatio = useCFS ? 1.0 : 0.0;
            logmp.MergeFactor = mergeFactor;
            return logmp;
        }

        public static MergePolicy NewLogMergePolicy(int mergeFactor)
        {
            LogMergePolicy logmp = NewLogMergePolicy();
            logmp.MergeFactor = mergeFactor;
            return logmp;
        }

        /// <summary>
        /// Returns a new <see cref="Directory"/> instance. Use this when the test does not
        /// care about the specific <see cref="Directory"/> implementation (most tests).
        /// <para/>
        /// The <see cref="Directory"/> is wrapped with <see cref="BaseDirectoryWrapper"/>.
        /// This means usually it will be picky, such as ensuring that you
        /// properly dispose it and close all open files in your test. It will emulate
        /// some features of Windows, such as not allowing open files to be
        /// overwritten.
        /// </summary>
        public static BaseDirectoryWrapper NewDirectory()
        {
            return NewDirectory(Random);
        }

        /// <summary>
        /// Returns a new <see cref="Directory"/> instance, using the specified <paramref name="random"/>.
        /// See <see cref="NewDirectory()"/> for more information.
        /// </summary>
        public static BaseDirectoryWrapper NewDirectory(Random random)
        {
            var newDir = NewDirectoryImpl(random, TestDirectory);

            return WrapDirectory(random, newDir, Rarely(random));
        }

        public static MockDirectoryWrapper NewMockDirectory()
        {
            return NewMockDirectory(Random);
        }

        public static MockDirectoryWrapper NewMockDirectory(Random random)
        {
            return (MockDirectoryWrapper)WrapDirectory(random, NewDirectoryImpl(random, TestDirectory), false);
        }

        public static MockDirectoryWrapper NewMockFSDirectory(DirectoryInfo d)
        {
            return (MockDirectoryWrapper)NewFSDirectory(d, null, false);
        }

        /// <summary>
        /// Returns a new <see cref="Directory"/> instance, with contents copied from the
        /// provided directory. See <see cref="NewDirectory()"/> for more
        /// information.
        /// </summary>
        public static BaseDirectoryWrapper NewDirectory(Directory d)
        {
            return NewDirectory(Random, d);
        }

        /// <summary>
        /// Returns a new <see cref="FSDirectory"/> instance over the given file, which must be a folder. </summary>
        public static BaseDirectoryWrapper NewFSDirectory(DirectoryInfo d)
        {
            return NewFSDirectory(d, null);
        }

        /// <summary>
        /// Returns a new <see cref="FSDirectory"/> instance over the given file, which must be a folder. </summary>
        public static BaseDirectoryWrapper NewFSDirectory(DirectoryInfo d, LockFactory lf)
        {
            return NewFSDirectory(d, lf, Rarely());
        }

        private static BaseDirectoryWrapper NewFSDirectory(DirectoryInfo d, LockFactory lf, bool bare)
        {
            string fsdirClass = TestDirectory;
            if (fsdirClass.Equals("random", StringComparison.Ordinal))
            {
                fsdirClass = RandomPicks.RandomFrom(Random, FS_DIRECTORIES);
            }

            // LUCENENET specific - .NET will not throw an exception if the
            // class does not inherit FSDirectory. We get a null if the name
            // cannot be resolved, not an exception.
            // We need to do an explicit check to determine if this type
            // is not a subclass of FSDirectory.
            Type clazz = CommandLineUtil.LoadFSDirectoryClass(fsdirClass);
            if (clazz is null || !(typeof(FSDirectory).IsAssignableFrom(clazz)))
            {
                // TEST_DIRECTORY is not a sub-class of FSDirectory, so draw one at random
                fsdirClass = RandomPicks.RandomFrom(Random, FS_DIRECTORIES);
                clazz = CommandLineUtil.LoadFSDirectoryClass(fsdirClass);
            }

            Directory fsdir = NewFSDirectoryImpl(clazz, d);
            BaseDirectoryWrapper wrapped = WrapDirectory(Random, fsdir, bare);
            if (lf != null)
            {
                wrapped.SetLockFactory(lf);
            }
            return wrapped;
        }

        /// <summary>
        /// Returns a new <see cref="Directory"/> instance, using the specified <paramref name="random"/>
        /// with contents copied from the provided <paramref name="directory"/>. See
        /// <see cref="NewDirectory()"/> for more information.
        /// </summary>
        public static BaseDirectoryWrapper NewDirectory(Random random, Directory directory)
        {
            Directory impl = NewDirectoryImpl(random, TestDirectory);
            foreach (string file in directory.ListAll())
            {
                directory.Copy(impl, file, file, NewIOContext(random));
            }
            return WrapDirectory(random, impl, Rarely(random));
        }

        private static BaseDirectoryWrapper WrapDirectory(Random random, Directory directory, bool bare)
        {
            if (Rarely(random))
            {
                directory = new NRTCachingDirectory(directory, random.NextDouble(), random.NextDouble());
            }

            if (Rarely(random))
            {
                double maxMBPerSec = 10 + 5 * (random.NextDouble() - 0.5);
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("LuceneTestCase: will rate limit output IndexOutput to " + maxMBPerSec + " MB/sec");
                }
                RateLimitedDirectoryWrapper rateLimitedDirectoryWrapper = new RateLimitedDirectoryWrapper(directory);
                switch (random.Next(10))
                {
                    case 3: // sometimes rate limit on flush
                        rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, IOContext.UsageContext.FLUSH);
                        break;

                    case 2: // sometimes rate limit flush & merge
                        rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, IOContext.UsageContext.FLUSH);
                        rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, IOContext.UsageContext.MERGE);
                        break;

                    default:
                        rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, IOContext.UsageContext.MERGE);
                        break;
                }
                directory = rateLimitedDirectoryWrapper;
            }

            if (bare)
            {
                BaseDirectoryWrapper @base = new BaseDirectoryWrapper(directory);
                // LUCENENET TODO: CloseAfterSuite(new DisposableDirectory(@base, SuiteFailureMarker));
                return @base;
            }
            else
            {
                MockDirectoryWrapper mock = new MockDirectoryWrapper(random, directory);

                mock.Throttling = TestThrottling;
                // LUCENENET TODO: CloseAfterSuite(new DisposableDirectory(mock, SuiteFailureMarker));
                return mock;
            }
        }


        public static Field NewStringField(string name, string value, Field.Store stored)
        {
            return NewField(Random, name, value, stored == Field.Store.YES ? StringField.TYPE_STORED : StringField.TYPE_NOT_STORED);
        }

        public static Field NewTextField(string name, string value, Field.Store stored)
        {
            return NewField(Random, name, value, stored == Field.Store.YES ? TextField.TYPE_STORED : TextField.TYPE_NOT_STORED);
        }

        public static Field NewStringField(Random random, string name, string value, Field.Store stored)
        {
            return NewField(random, name, value, stored == Field.Store.YES ? StringField.TYPE_STORED : StringField.TYPE_NOT_STORED);
        }

        public static Field NewTextField(Random random, string name, string value, Field.Store stored)
        {
            return NewField(random, name, value, stored == Field.Store.YES ? TextField.TYPE_STORED : TextField.TYPE_NOT_STORED);
        }

        public static Field NewField(string name, string value, FieldType type)
        {
            return NewField(Random, name, value, type);
        }

        public static Field NewField(Random random, string name, string value, FieldType type)
        {
            name = new string(name.ToCharArray());
            if (Usually(random) || !type.IsIndexed)
            {
                // most of the time, don't modify the params
                return new Field(name, value, type);
            }

            // TODO: once all core & test codecs can index
            // offsets, sometimes randomly turn on offsets if we are
            // already indexing positions...

            FieldType newType = new FieldType(type);
            if (!newType.IsStored && random.NextBoolean())
            {
                newType.IsStored = true; // randomly store it
            }

            if (!newType.StoreTermVectors && random.NextBoolean())
            {
                newType.StoreTermVectors = true;
                if (!newType.StoreTermVectorOffsets)
                {
                    newType.StoreTermVectorOffsets = random.NextBoolean();
                }
                if (!newType.StoreTermVectorPositions)
                {
                    newType.StoreTermVectorPositions = random.NextBoolean();

                    if (newType.StoreTermVectorPositions && !newType.StoreTermVectorPayloads && !OldFormatImpersonationIsActive)
                    {
                        newType.StoreTermVectorPayloads = random.NextBoolean();
                    }
                }
            }

            // TODO: we need to do this, but smarter, ie, most of
            // the time we set the same value for a given field but
            // sometimes (rarely) we change it up:
            /*
            if (newType.OmitNorms) {
                newType.setOmitNorms(random.NextBoolean());
            }
            */

            return new Field(name, value, newType);
        }

        /// <summary>
        /// Return a random <see cref="CultureInfo"/> from the available cultures on the system. 
        /// <para/>
        /// See <a href="https://issues.apache.org/jira/browse/LUCENE-4020">https://issues.apache.org/jira/browse/LUCENE-4020</a>.
        /// </summary>
        public static CultureInfo RandomCulture(Random random) // LUCENENET specific renamed from RandomLocale
        {
            return RandomPicks.RandomFrom(random, CultureInfo.GetCultures(CultureTypes.SpecificCultures | CultureTypes.NeutralCultures));
        }

        /// <summary>
        /// Return a random <see cref="TimeZoneInfo"/> from the available timezones on the system 
        /// <para/>
        /// See <a href="https://issues.apache.org/jira/browse/LUCENE-4020">https://issues.apache.org/jira/browse/LUCENE-4020</a>.
        /// </summary>
        public static TimeZoneInfo RandomTimeZone(Random random)
        {
            return RandomPicks.RandomFrom(random, TimeZoneInfo.GetSystemTimeZones());
        }

        /// <summary>
        /// return a <see cref="CultureInfo"/> object equivalent to its programmatic name. </summary>
        public static CultureInfo CultureForName(string localeName) // LUCENENET specific - renamed from LocaleForName
        {
            return new CultureInfo(localeName);
        }

        public static bool DefaultCodecSupportsDocValues => !Codec.Default.Name.Equals("Lucene3x", StringComparison.Ordinal);

        private static Directory NewFSDirectoryImpl(Type clazz, DirectoryInfo file)
        {
            return CommandLineUtil.NewFSDirectory(clazz, file);
            // LUCENENET: No sense in catching just to rethrow again as the same type
        }

        private static Directory NewDirectoryImpl(Random random, string clazzName)
        {
            if (clazzName.Equals("random", StringComparison.Ordinal))
            {
                if (Rarely(random))
                {
                    clazzName = RandomPicks.RandomFrom(random, CORE_DIRECTORIES);
                }
                else
                {
                    clazzName = "RAMDirectory";
                }
            }

            if (Verbose)
            {
                Trace.TraceInformation("Type of Directory is : {0}", clazzName);
            }

            Type clazz = CommandLineUtil.LoadDirectoryClass(clazzName);
            if (clazz is null)
                throw IllegalStateException.Create($"Type '{clazzName}' could not be instantiated."); // LUCENENET: We don't get an exception in this case, so throwing one for compatibility
            // If it is a FSDirectory type, try its ctor(File)
            if (typeof(FSDirectory).IsAssignableFrom(clazz))
            {
                DirectoryInfo dir = CreateTempDir("index-" + clazzName);
                dir.Create(); // ensure it's created so we 'have' it.
                return NewFSDirectoryImpl(clazz, dir);
            }

            // try empty ctor
            return (Directory)Activator.CreateInstance(clazz);
        }

        /// <summary>
        /// Sometimes wrap the <see cref="IndexReader"/> as slow, parallel or filter reader (or
        /// combinations of that)
        /// </summary>
        public static IndexReader MaybeWrapReader(IndexReader r)
        {
            Random random = Random;
            if (Rarely())
            {
                // TODO: remove this, and fix those tests to wrap before putting slow around:
                bool wasOriginallyAtomic = r is AtomicReader;
                for (int i = 0, c = random.Next(6) + 1; i < c; i++)
                {
                    switch (random.Next(5))
                    {
                        case 0:
                            r = SlowCompositeReaderWrapper.Wrap(r);
                            break;

                        case 1:
                            // will create no FC insanity in atomic case, as ParallelAtomicReader has own cache key:
                            r = (r is AtomicReader atomicReader) ? (IndexReader)new ParallelAtomicReader(atomicReader) : new ParallelCompositeReader((CompositeReader)r);
                            break;

                        case 2:
                            // Häckidy-Hick-Hack: a standard MultiReader will cause FC insanity, so we use
                            // QueryUtils' reader with a fake cache key, so insanity checker cannot walk
                            // along our reader:
                            r = new FCInvisibleMultiReader(r);
                            break;

                        case 3:
                            AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);
                            JCG.List<string> allFields = new JCG.List<string>();
                            foreach (FieldInfo fi in ar.FieldInfos)
                            {
                                allFields.Add(fi.Name);
                            }
                            allFields.Shuffle(Random);
                            int end = allFields.Count == 0 ? 0 : random.Next(allFields.Count);
                            ISet<string> fields = new JCG.HashSet<string>(allFields.GetView(0, end)); // LUCENENET: Checked length for correctness
                            // will create no FC insanity as ParallelAtomicReader has own cache key:
                            r = new ParallelAtomicReader(new FieldFilterAtomicReader(ar, fields, false), new FieldFilterAtomicReader(ar, fields, true));
                            break;

                        case 4:
                            // Häckidy-Hick-Hack: a standard Reader will cause FC insanity, so we use
                            // QueryUtils' reader with a fake cache key, so insanity checker cannot walk
                            // along our reader:
                            if (r is AtomicReader atomicReader2)
                            {
                                r = new AssertingAtomicReader(atomicReader2);
                            }
                            else if (r is DirectoryReader directoryReader)
                            {
                                r = new AssertingDirectoryReader(directoryReader);
                            }
                            break;

                        default:
                            Assert.Fail("should not get here");
                            break;
                    }
                }
                if (wasOriginallyAtomic)
                {
                    r = SlowCompositeReaderWrapper.Wrap(r);
                }
                else if ((r is CompositeReader) && !(r is FCInvisibleMultiReader))
                {
                    // prevent cache insanity caused by e.g. ParallelCompositeReader, to fix we wrap one more time:
                    r = new FCInvisibleMultiReader(r);
                }
                if (Verbose)
                {
                    Console.WriteLine("maybeWrapReader wrapped: " + r);
                }
            }
            return r;
        }

        /// <summary>
        /// TODO: javadoc </summary>
        public static IOContext NewIOContext(Random random)
        {
            return NewIOContext(random, IOContext.DEFAULT);
        }

        /// <summary>
        /// TODO: javadoc </summary>
        public static IOContext NewIOContext(Random random, IOContext oldContext)
        {
            int randomNumDocs = random.Next(4192);
            int size = random.Next(512) * randomNumDocs;
            if (oldContext.FlushInfo != null)
            {
                // Always return at least the estimatedSegmentSize of
                // the incoming IOContext:
                return new IOContext(new FlushInfo(randomNumDocs, (long)Math.Max(oldContext.FlushInfo.EstimatedSegmentSize, size)));
            }
            else if (oldContext.MergeInfo != null)
            {
                // Always return at least the estimatedMergeBytes of
                // the incoming IOContext:
                return new IOContext(new MergeInfo(randomNumDocs, Math.Max(oldContext.MergeInfo.EstimatedMergeBytes, size), random.NextBoolean(), TestUtil.NextInt32(random, 1, 100)));
            }
            else
            {
                // Make a totally random IOContext:
                IOContext context;
                switch (random.Next(5))
                {
                    case 0:
                        context = IOContext.DEFAULT;
                        break;

                    case 1:
                        context = IOContext.READ;
                        break;

                    case 2:
                        context = IOContext.READ_ONCE;
                        break;

                    case 3:
                        context = new IOContext(new MergeInfo(randomNumDocs, size, true, -1));
                        break;

                    case 4:
                        context = new IOContext(new FlushInfo(randomNumDocs, size));
                        break;

                    default:
                        context = IOContext.DEFAULT;
                        break;
                }
                return context;
            }
        }

        /// <summary>
        /// Create a new searcher over the reader. this searcher might randomly use
        /// threads.
        /// </summary>
        public static IndexSearcher NewSearcher(IndexReader r)
        {
            return NewSearcher(r, true);
        }

        /// <summary>
        /// Create a new searcher over the reader. This searcher might randomly use
        /// threads.
        /// </summary>
        public static IndexSearcher NewSearcher(IndexReader r, bool maybeWrap)
        {
            return NewSearcher(r, maybeWrap, true);
        }

        /// <summary>
        /// Create a new searcher over the reader. This searcher might randomly use
        /// threads. If <paramref name="maybeWrap"/> is true, this searcher might wrap the
        /// reader with one that returns null for <see cref="CompositeReader.GetSequentialSubReaders()"/>. If
        /// <paramref name="wrapWithAssertions"/> is true, this searcher might be an
        /// <see cref="AssertingIndexSearcher"/> instance.
        /// </summary>
        public static IndexSearcher NewSearcher(IndexReader r, bool maybeWrap, bool wrapWithAssertions)
        {
            Random random = Random;
            if (Usually())
            {
                if (maybeWrap)
                {
                    // LUCENENET: Rethrow.rethrow() call not needed here because it simply rethrows an exception as itself
                    r = MaybeWrapReader(r);
                }
                // TODO: this whole check is a coverage hack, we should move it to tests for various filterreaders.
                // ultimately whatever you do will be checkIndex'd at the end anyway.
                if (random.Next(500) == 0 && r is AtomicReader)
                {
                    // TODO: not useful to check DirectoryReader (redundant with checkindex)
                    // but maybe sometimes run this on the other crazy readers maybeWrapReader creates?

                    // LUCENENET: Rethrow.rethrow() call not needed here because it simply rethrows an exception as itself
                    TestUtil.CheckReader(r);
                }
                IndexSearcher ret;
                if (wrapWithAssertions)
                {
                    ret = random.NextBoolean() ? new AssertingIndexSearcher(random, r) : new AssertingIndexSearcher(random, r.Context);
                }
                else
                {
                    ret = random.NextBoolean() ? new IndexSearcher(r) : new IndexSearcher(r.Context);
                }
                ret.Similarity = ClassEnvRule.similarity;
                return ret;
            }
            else
            {
                int threads = 0;
                LimitedConcurrencyLevelTaskScheduler ex;
                if (random.NextBoolean())
                {
                    ex = null;
                }
                else
                {
                    threads = TestUtil.NextInt32(random, 1, 8);
                    ex = new LimitedConcurrencyLevelTaskScheduler(threads);
                    //ex = new ThreadPoolExecutor(threads, threads, 0L, TimeUnit.MILLISECONDS, new LinkedBlockingQueue<IThreadRunnable>(), new NamedThreadFactory("LuceneTestCase"));
                    // uncomment to intensify LUCENE-3840
                    // ex.prestartAllCoreThreads();
                }
                if (ex != null)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("NOTE: newSearcher using ExecutorService with " + threads + " threads");
                    }
                    r.AddReaderDisposedListener(new ReaderClosedListenerAnonymousClass(ex));
                }
                IndexSearcher ret;
                if (wrapWithAssertions)
                {
                    ret = random.NextBoolean() ? new AssertingIndexSearcher(random, r, ex) : new AssertingIndexSearcher(random, r.Context, ex);
                }
                else
                {
                    ret = random.NextBoolean() ? new IndexSearcher(r, ex) : new IndexSearcher(r.Context, ex);
                }
                ret.Similarity = ClassEnvRule.similarity;
                return ret;
            }
        }

        /// <summary>
        /// Gets a resource from the classpath as <see cref="Stream"/>. This method should only
        /// be used, if a real file is needed. To get a stream, code should prefer
        /// <see cref="J2N.AssemblyExtensions.FindAndGetManifestResourceStream(Assembly, Type, string)"/> using 
        /// <c>this.GetType().Assembly</c> and <c>this.GetType()</c>.
        /// </summary>
        protected virtual Stream GetDataFile(string name)
        {
            try
            {
                return this.GetType().getResourceAsStream(name);
            }
            catch (Exception e) when (e.IsException())
            {
                throw new IOException("Cannot find resource: " + name, e); // LUCENENET specific - wrapped inner exception
            }
        }

        /// <summary>
        /// Returns true if the default codec supports single valued docvalues with missing values </summary>
        public static bool DefaultCodecSupportsMissingDocValues
        {
            get
            {
                string name = Codec.Default.Name;
                if (name.Equals("Lucene3x", StringComparison.Ordinal)
                    || name.Equals("Lucene40", StringComparison.Ordinal)
                    || name.Equals("Appending", StringComparison.Ordinal)
                    || name.Equals("Lucene41", StringComparison.Ordinal)
                    || name.Equals("Lucene42", StringComparison.Ordinal))
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Returns true if the default codec supports SORTED_SET docvalues </summary>
        public static bool DefaultCodecSupportsSortedSet
        {
            get
            {
                if (!DefaultCodecSupportsDocValues)
                {
                    return false;
                }
                string name = Codec.Default.Name;
                if (name.Equals("Lucene40", StringComparison.Ordinal)
                    || name.Equals("Lucene41", StringComparison.Ordinal)
                    || name.Equals("Appending", StringComparison.Ordinal))
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Returns true if the codec "supports" docsWithField
        /// (other codecs return MatchAllBits, because you couldnt write missing values before)
        /// </summary>
        public static bool DefaultCodecSupportsDocsWithField
        {
            get
            {
                if (!DefaultCodecSupportsDocValues)
                {
                    return false;
                }
                string name = Codec.Default.Name;
                if (name.Equals("Appending", StringComparison.Ordinal)
                    || name.Equals("Lucene40", StringComparison.Ordinal)
                    || name.Equals("Lucene41", StringComparison.Ordinal)
                    || name.Equals("Lucene42", StringComparison.Ordinal))
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Returns true if the codec "supports" field updates. </summary>
        public static bool DefaultCodecSupportsFieldUpdates
        {
            get
            {
                string name = Codec.Default.Name;
                if (name.Equals("Lucene3x", StringComparison.Ordinal)
                    || name.Equals("Appending", StringComparison.Ordinal)
                    || name.Equals("Lucene40", StringComparison.Ordinal)
                    || name.Equals("Lucene41", StringComparison.Ordinal)
                    || name.Equals("Lucene42", StringComparison.Ordinal)
                    || name.Equals("Lucene45", StringComparison.Ordinal))
                {
                    return false;
                }
                return true;
            }
        }

        public virtual void AssertReaderEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            AssertReaderStatisticsEquals(info, leftReader, rightReader);
            AssertFieldsEquals(info, leftReader, MultiFields.GetFields(leftReader), MultiFields.GetFields(rightReader), true);
            AssertNormsEquals(info, leftReader, rightReader);
            AssertStoredFieldsEquals(info, leftReader, rightReader);
            AssertTermVectorsEquals(info, leftReader, rightReader);
            AssertDocValuesEquals(info, leftReader, rightReader);
            AssertDeletedDocsEquals(info, leftReader, rightReader);
            AssertFieldInfosEquals(info, leftReader, rightReader);
        }

        /// <summary>
        /// Checks that reader-level statistics are the same.
        /// </summary>
        public virtual void AssertReaderStatisticsEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            // Somewhat redundant: we never delete docs
            Assert.AreEqual(leftReader.MaxDoc, rightReader.MaxDoc, info);
            Assert.AreEqual(leftReader.NumDocs, rightReader.NumDocs, info);
            Assert.AreEqual(leftReader.NumDeletedDocs, rightReader.NumDeletedDocs, info);
            Assert.AreEqual(leftReader.HasDeletions, rightReader.HasDeletions, info);
        }

        /// <summary>
        /// Fields API equivalency.
        /// </summary>
        public virtual void AssertFieldsEquals(string info, IndexReader leftReader, Fields leftFields, Fields rightFields, bool deep)
        {
            // Fields could be null if there are no postings,
            // but then it must be null for both
            if (leftFields is null || rightFields is null)
            {
                Assert.IsNull(leftFields, info);
                Assert.IsNull(rightFields, info);
                return;
            }
            AssertFieldStatisticsEquals(info, leftFields, rightFields);

            using IEnumerator<string> leftEnum = leftFields.GetEnumerator();
            using IEnumerator<string> rightEnum = rightFields.GetEnumerator();
            while (leftEnum.MoveNext())
            {
                string field = leftEnum.Current;
                rightEnum.MoveNext();
                Assert.AreEqual(field, rightEnum.Current, info);
                AssertTermsEquals(info, leftReader, leftFields.GetTerms(field), rightFields.GetTerms(field), deep);
            }
            Assert.IsFalse(rightEnum.MoveNext());
        }

        /// <summary>
        /// Checks that top-level statistics on <see cref="Fields"/> are the same.
        /// </summary>
        public virtual void AssertFieldStatisticsEquals(string info, Fields leftFields, Fields rightFields)
        {
            if (leftFields.Count != -1 && rightFields.Count != -1)
            {
                Assert.AreEqual(leftFields.Count, rightFields.Count, info);
            }
        }

        /// <summary>
        /// <see cref="Terms"/> API equivalency.
        /// </summary>
        public virtual void AssertTermsEquals(string info, IndexReader leftReader, Terms leftTerms, Terms rightTerms, bool deep)
        {
            if (leftTerms is null || rightTerms is null)
            {
                Assert.IsNull(leftTerms, info);
                Assert.IsNull(rightTerms, info);
                return;
            }
            AssertTermsStatisticsEquals(info, leftTerms, rightTerms);
            Assert.AreEqual(leftTerms.HasOffsets, rightTerms.HasOffsets);
            Assert.AreEqual(leftTerms.HasPositions, rightTerms.HasPositions);
            Assert.AreEqual(leftTerms.HasPayloads, rightTerms.HasPayloads);

            TermsEnum leftTermsEnum = leftTerms.GetEnumerator();
            TermsEnum rightTermsEnum = rightTerms.GetEnumerator();
            AssertTermsEnumEquals(info, leftReader, leftTermsEnum, rightTermsEnum, true);

            AssertTermsSeekingEquals(info, leftTerms, rightTerms);

            if (deep)
            {
                int numIntersections = AtLeast(3);
                for (int i = 0; i < numIntersections; i++)
                {
                    string re = AutomatonTestUtil.RandomRegexp(Random);
                    CompiledAutomaton automaton = new CompiledAutomaton((new RegExp(re, RegExpSyntax.NONE)).ToAutomaton());
                    if (automaton.Type == CompiledAutomaton.AUTOMATON_TYPE.NORMAL)
                    {
                        // TODO: test start term too
                        TermsEnum leftIntersection = leftTerms.Intersect(automaton, null);
                        TermsEnum rightIntersection = rightTerms.Intersect(automaton, null);
                        AssertTermsEnumEquals(info, leftReader, leftIntersection, rightIntersection, Rarely());
                    }
                }
            }
        }

        /// <summary>
        /// Checks collection-level statistics on <see cref="Terms"/>.
        /// </summary>
        public virtual void AssertTermsStatisticsEquals(string info, Terms leftTerms, Terms rightTerms)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(leftTerms.Comparer == rightTerms.Comparer);
            if (leftTerms.DocCount != -1 && rightTerms.DocCount != -1)
            {
                Assert.AreEqual(leftTerms.DocCount, rightTerms.DocCount, info);
            }
            if (leftTerms.SumDocFreq != -1 && rightTerms.SumDocFreq != -1)
            {
                Assert.AreEqual(leftTerms.SumDocFreq, rightTerms.SumDocFreq, info);
            }
            if (leftTerms.SumTotalTermFreq != -1 && rightTerms.SumTotalTermFreq != -1)
            {
                Assert.AreEqual(leftTerms.SumTotalTermFreq, rightTerms.SumTotalTermFreq, info);
            }
            if (leftTerms.Count != -1 && rightTerms.Count != -1)
            {
                Assert.AreEqual(leftTerms.Count, rightTerms.Count, info);
            }
        }

        internal class RandomBits : IBits
        {
            private static FixedBitSet bits;

            internal RandomBits(int maxDoc, double pctLive, Random random)
            {
                bits = new FixedBitSet(maxDoc);
                for (int i = 0; i < maxDoc; i++)
                {
                    if (random.NextDouble() <= pctLive)
                    {
                        bits.Set(i);
                    }
                }
            }

            public bool Get(int index)
                => bits.Get(index);

            public int Length => bits.Length;
        }

        /// <summary>
        /// Checks the terms enum sequentially.
        /// If <paramref name="deep"/> is false, it does a 'shallow' test that doesnt go down to the docsenums.
        /// </summary>
        public virtual void AssertTermsEnumEquals(string info, IndexReader leftReader, TermsEnum leftTermsEnum, TermsEnum rightTermsEnum, bool deep)
        {
            BytesRef term;
            IBits randomBits = new RandomBits(leftReader.MaxDoc, Random.NextDouble(), Random);
            DocsAndPositionsEnum leftPositions = null;
            DocsAndPositionsEnum rightPositions = null;
            DocsEnum leftDocs = null;
            DocsEnum rightDocs = null;

            while (leftTermsEnum.MoveNext())
            {
                term = leftTermsEnum.Term;
                rightTermsEnum.MoveNext();
                Assert.AreEqual(term, rightTermsEnum.Term, info);
                AssertTermStatsEquals(info, leftTermsEnum, rightTermsEnum);
                if (deep)
                {
                    AssertDocsAndPositionsEnumEquals(info, leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions));
                    AssertDocsAndPositionsEnumEquals(info, leftPositions = leftTermsEnum.DocsAndPositions(randomBits, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions));

                    AssertPositionsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq, leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions));
                    AssertPositionsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq, leftPositions = leftTermsEnum.DocsAndPositions(randomBits, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions));

                    // with freqs:
                    AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(null, leftDocs), rightDocs = rightTermsEnum.Docs(null, rightDocs), true);
                    AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(randomBits, leftDocs), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs), true);

                    // w/o freqs:
                    AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(null, leftDocs, DocsFlags.NONE), rightDocs = rightTermsEnum.Docs(null, rightDocs, DocsFlags.NONE), false);
                    AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(randomBits, leftDocs, DocsFlags.NONE), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs, DocsFlags.NONE), false);

                    // with freqs:
                    AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq, leftDocs = leftTermsEnum.Docs(null, leftDocs), rightDocs = rightTermsEnum.Docs(null, rightDocs), true);
                    AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq, leftDocs = leftTermsEnum.Docs(randomBits, leftDocs), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs), true);

                    // w/o freqs:
                    AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq, leftDocs = leftTermsEnum.Docs(null, leftDocs, DocsFlags.NONE), rightDocs = rightTermsEnum.Docs(null, rightDocs, DocsFlags.NONE), false);
                    AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq, leftDocs = leftTermsEnum.Docs(randomBits, leftDocs, DocsFlags.NONE), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs, DocsFlags.NONE), false);
                }
            }
            Assert.IsFalse(rightTermsEnum.MoveNext(), info);
        }

        /// <summary>
        /// Checks docs + freqs + positions + payloads, sequentially.
        /// </summary>
        public virtual void AssertDocsAndPositionsEnumEquals(string info, DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
        {
            if (leftDocs is null || rightDocs is null)
            {
                Assert.IsNull(leftDocs);
                Assert.IsNull(rightDocs);
                return;
            }
            Assert.AreEqual(-1, leftDocs.DocID, info);
            Assert.AreEqual(-1, rightDocs.DocID, info);
            int docid;
            while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                Assert.AreEqual(docid, rightDocs.NextDoc(), info);
                int freq = leftDocs.Freq;
                Assert.AreEqual(freq, rightDocs.Freq, info);
                for (int i = 0; i < freq; i++)
                {
                    Assert.AreEqual(leftDocs.NextPosition(), rightDocs.NextPosition(), info);
                    Assert.AreEqual(leftDocs.GetPayload(), rightDocs.GetPayload(), info);
                    Assert.AreEqual(leftDocs.StartOffset, rightDocs.StartOffset, info);
                    Assert.AreEqual(leftDocs.EndOffset, rightDocs.EndOffset, info);
                }
            }
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc(), info);
        }

        /// <summary>
        /// Checks docs + freqs, sequentially.
        /// </summary>
        public virtual void AssertDocsEnumEquals(string info, DocsEnum leftDocs, DocsEnum rightDocs, bool hasFreqs)
        {
            if (leftDocs is null)
            {
                Assert.IsNull(rightDocs);
                return;
            }
            Assert.AreEqual(-1, leftDocs.DocID, info);
            Assert.AreEqual(-1, rightDocs.DocID, info);
            int docid;
            while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                Assert.AreEqual(docid, rightDocs.NextDoc(), info);
                if (hasFreqs)
                {
                    Assert.AreEqual(leftDocs.Freq, rightDocs.Freq, info);
                }
            }
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc(), info);
        }

        /// <summary>
        /// Checks advancing docs.
        /// </summary>
        public virtual void AssertDocsSkippingEquals(string info, IndexReader leftReader, int docFreq, DocsEnum leftDocs, DocsEnum rightDocs, bool hasFreqs)
        {
            if (leftDocs is null)
            {
                Assert.IsNull(rightDocs);
                return;
            }
            int docid = -1;
            int averageGap = leftReader.MaxDoc / (1 + docFreq);
            int skipInterval = 16;

            while (true)
            {
                if (Random.NextBoolean())
                {
                    // nextDoc()
                    docid = leftDocs.NextDoc();
                    Assert.AreEqual(docid, rightDocs.NextDoc(), info);
                }
                else
                {
                    // advance()
                    int skip = docid + (int)Math.Ceiling(Math.Abs(skipInterval + Random.NextGaussian() * averageGap));
                    docid = leftDocs.Advance(skip);
                    Assert.AreEqual(docid, rightDocs.Advance(skip), info);
                }

                if (docid == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return;
                }
                if (hasFreqs)
                {
                    Assert.AreEqual(leftDocs.Freq, rightDocs.Freq, info);
                }
            }
        }

        /// <summary>
        /// Checks advancing docs + positions.
        /// </summary>
        public virtual void AssertPositionsSkippingEquals(string info, IndexReader leftReader, int docFreq, DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
        {
            if (leftDocs is null || rightDocs is null)
            {
                Assert.IsNull(leftDocs);
                Assert.IsNull(rightDocs);
                return;
            }

            int docid = -1;
            int averageGap = leftReader.MaxDoc / (1 + docFreq);
            int skipInterval = 16;

            while (true)
            {
                if (Random.NextBoolean())
                {
                    // nextDoc()
                    docid = leftDocs.NextDoc();
                    Assert.AreEqual(docid, rightDocs.NextDoc(), info);
                }
                else
                {
                    // advance()
                    int skip = docid + (int)Math.Ceiling(Math.Abs(skipInterval + Random.NextGaussian() * averageGap));
                    docid = leftDocs.Advance(skip);
                    Assert.AreEqual(docid, rightDocs.Advance(skip), info);
                }

                if (docid == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return;
                }
                int freq = leftDocs.Freq;
                Assert.AreEqual(freq, rightDocs.Freq, info);
                for (int i = 0; i < freq; i++)
                {
                    Assert.AreEqual(leftDocs.NextPosition(), rightDocs.NextPosition(), info);
                    Assert.AreEqual(leftDocs.GetPayload(), rightDocs.GetPayload(), info);
                }
            }
        }

        private void AssertTermsSeekingEquals(string info, Terms leftTerms, Terms rightTerms)
        {
            TermsEnum leftEnum = null;
            TermsEnum rightEnum = null;

            // just an upper bound
            int numTests = AtLeast(20);
            Random random = Random;

            // collect this number of terms from the left side
            ISet<BytesRef> tests = new JCG.HashSet<BytesRef>();
            int numPasses = 0;
            while (numPasses < 10 && tests.Count < numTests)
            {
                leftEnum = leftTerms.GetEnumerator(leftEnum);
                BytesRef term;
                while (leftEnum.MoveNext())
                {
                    term = leftEnum.Term;
                    int code = random.Next(10);
                    if (code == 0)
                    {
                        // the term
                        tests.Add(BytesRef.DeepCopyOf(term));
                    }
                    else if (code == 1)
                    {
                        // truncated subsequence of term
                        term = BytesRef.DeepCopyOf(term);
                        if (term.Length > 0)
                        {
                            // truncate it
                            term.Length = random.Next(term.Length);
                        }
                    }
                    else if (code == 2)
                    {
                        // term, but ensure a non-zero offset
                        var newbytes = new byte[term.Length + 5];
                        Arrays.Copy(term.Bytes, term.Offset, newbytes, 5, term.Length);
                        tests.Add(new BytesRef(newbytes, 5, term.Length));
                    }
                    else if (code == 3)
                    {
                        switch (LuceneTestCase.Random.Next(3))
                        {
                            case 0:
                                tests.Add(new BytesRef()); // before the first term
                                break;

                            case 1:
                                tests.Add(new BytesRef(new byte[] { unchecked((byte)0xFF), unchecked((byte)0xFF) })); // past the last term
                                break;

                            case 2:
                                tests.Add(new BytesRef(TestUtil.RandomSimpleString(LuceneTestCase.Random))); // random term
                                break;

                            default:
                                throw AssertionError.Create();
                        }
                    }
                }
                numPasses++;
            }

            rightEnum = rightTerms.GetEnumerator(rightEnum);

            IList<BytesRef> shuffledTests = new JCG.List<BytesRef>(tests);
            shuffledTests.Shuffle(Random);

            foreach (BytesRef b in shuffledTests)
            {
                if (Rarely())
                {
                    // reuse the enums
                    leftEnum = leftTerms.GetEnumerator(leftEnum);
                    rightEnum = rightTerms.GetEnumerator(rightEnum);
                }

                bool seekExact = LuceneTestCase.Random.NextBoolean();

                if (seekExact)
                {
                    Assert.AreEqual(leftEnum.SeekExact(b), rightEnum.SeekExact(b), info);
                }
                else
                {
                    TermsEnum.SeekStatus leftStatus = leftEnum.SeekCeil(b);
                    TermsEnum.SeekStatus rightStatus = rightEnum.SeekCeil(b);
                    Assert.AreEqual(leftStatus, rightStatus, info);
                    if (leftStatus != TermsEnum.SeekStatus.END)
                    {
                        Assert.AreEqual(leftEnum.Term, rightEnum.Term, info);
                        AssertTermStatsEquals(info, leftEnum, rightEnum);
                    }
                }
            }
        }

        /// <summary>
        /// Checks term-level statistics.
        /// </summary>
        public virtual void AssertTermStatsEquals(string info, TermsEnum leftTermsEnum, TermsEnum rightTermsEnum)
        {
            Assert.AreEqual(leftTermsEnum.DocFreq, rightTermsEnum.DocFreq, info);
            if (leftTermsEnum.TotalTermFreq != -1 && rightTermsEnum.TotalTermFreq != -1)
            {
                Assert.AreEqual(leftTermsEnum.TotalTermFreq, rightTermsEnum.TotalTermFreq, info);
            }
        }

        /// <summary>
        /// Checks that norms are the same across all fields.
        /// </summary>
        public virtual void AssertNormsEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            Fields leftFields = MultiFields.GetFields(leftReader);
            Fields rightFields = MultiFields.GetFields(rightReader);
            // Fields could be null if there are no postings,
            // but then it must be null for both
            if (leftFields is null || rightFields is null)
            {
                Assert.IsNull(leftFields, info);
                Assert.IsNull(rightFields, info);
                return;
            }

            foreach (string field in leftFields)
            {
                NumericDocValues leftNorms = MultiDocValues.GetNormValues(leftReader, field);
                NumericDocValues rightNorms = MultiDocValues.GetNormValues(rightReader, field);
                if (leftNorms != null && rightNorms != null)
                {
                    AssertDocValuesEquals(info, leftReader.MaxDoc, leftNorms, rightNorms);
                }
                else
                {
                    Assert.IsNull(leftNorms, info);
                    Assert.IsNull(rightNorms, info);
                }
            }
        }

        /// <summary>
        /// Checks that stored fields of all documents are the same.
        /// </summary>
        public virtual void AssertStoredFieldsEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(leftReader.MaxDoc == rightReader.MaxDoc);
            for (int i = 0; i < leftReader.MaxDoc; i++)
            {
                Document leftDoc = leftReader.Document(i);
                Document rightDoc = rightReader.Document(i);

                // TODO: I think this is bogus because we don't document what the order should be
                // from these iterators, etc. I think the codec/IndexReader should be free to order this stuff
                // in whatever way it wants (e.g. maybe it packs related fields together or something)
                // To fix this, we sort the fields in both documents by name, but
                // we still assume that all instances with same name are in order:
                var comp = Comparer<IIndexableField>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                leftDoc.Fields.Sort(comp);
                rightDoc.Fields.Sort(comp);

                using var leftIterator = leftDoc.GetEnumerator();
                using var rightIterator = rightDoc.GetEnumerator();
                while (leftIterator.MoveNext())
                {
                    Assert.IsTrue(rightIterator.MoveNext(), info);
                    AssertStoredFieldEquals(info, leftIterator.Current, rightIterator.Current);
                }
                Assert.IsFalse(rightIterator.MoveNext(), info);
            }
        }

        /// <summary>
        /// Checks that two stored fields are equivalent.
        /// </summary>
        public virtual void AssertStoredFieldEquals(string info, IIndexableField leftField, IIndexableField rightField)
        {
            Assert.AreEqual(leftField.Name, rightField.Name, info);
            Assert.AreEqual(leftField.GetBinaryValue(), rightField.GetBinaryValue(), info);
            Assert.AreEqual(leftField.GetStringValue(), rightField.GetStringValue(), info);
#pragma warning disable 612, 618
            Assert.AreEqual(leftField.GetNumericValue(), rightField.GetNumericValue(), info);
#pragma warning restore 612, 618
            // TODO: should we check the FT at all?
        }

        /// <summary>
        /// Checks that term vectors across all fields are equivalent.
        /// </summary>
        public virtual void AssertTermVectorsEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(leftReader.MaxDoc == rightReader.MaxDoc);
            for (int i = 0; i < leftReader.MaxDoc; i++)
            {
                Fields leftFields = leftReader.GetTermVectors(i);
                Fields rightFields = rightReader.GetTermVectors(i);
                AssertFieldsEquals(info, leftReader, leftFields, rightFields, Rarely());
            }
        }

        private static ISet<string> GetDVFields(IndexReader reader)
        {
            ISet<string> fields = new JCG.HashSet<string>();
            foreach (FieldInfo fi in MultiFields.GetMergedFieldInfos(reader))
            {
                if (fi.HasDocValues)
                {
                    fields.Add(fi.Name);
                }
            }

            return fields;
        }

        /// <summary>
        /// Checks that docvalues across all fields are equivalent.
        /// </summary>
        public virtual void AssertDocValuesEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            ISet<string> leftFields = GetDVFields(leftReader);
            ISet<string> rightFields = GetDVFields(rightReader);
            Assert.AreEqual(leftFields, rightFields, info);

            foreach (string field in leftFields)
            {
                // TODO: clean this up... very messy
                {
                    NumericDocValues leftValues = MultiDocValues.GetNumericValues(leftReader, field);
                    NumericDocValues rightValues = MultiDocValues.GetNumericValues(rightReader, field);
                    if (leftValues != null && rightValues != null)
                    {
                        AssertDocValuesEquals(info, leftReader.MaxDoc, leftValues, rightValues);
                    }
                    else
                    {
                        Assert.IsNull(leftValues, info);
                        Assert.IsNull(rightValues, info);
                    }
                }

                {
                    BinaryDocValues leftValues = MultiDocValues.GetBinaryValues(leftReader, field);
                    BinaryDocValues rightValues = MultiDocValues.GetBinaryValues(rightReader, field);
                    if (leftValues != null && rightValues != null)
                    {
                        BytesRef scratchLeft = new BytesRef();
                        BytesRef scratchRight = new BytesRef();
                        for (int docID = 0; docID < leftReader.MaxDoc; docID++)
                        {
                            leftValues.Get(docID, scratchLeft);
                            rightValues.Get(docID, scratchRight);
                            Assert.AreEqual(scratchLeft, scratchRight, info);
                        }
                    }
                    else
                    {
                        Assert.IsNull(leftValues, info);
                        Assert.IsNull(rightValues, info);
                    }
                }

                {
                    SortedDocValues leftValues = MultiDocValues.GetSortedValues(leftReader, field);
                    SortedDocValues rightValues = MultiDocValues.GetSortedValues(rightReader, field);
                    if (leftValues != null && rightValues != null)
                    {
                        // numOrds
                        Assert.AreEqual(leftValues.ValueCount, rightValues.ValueCount, info);
                        // ords
                        BytesRef scratchLeft = new BytesRef();
                        BytesRef scratchRight = new BytesRef();
                        for (int i = 0; i < leftValues.ValueCount; i++)
                        {
                            leftValues.LookupOrd(i, scratchLeft);
                            rightValues.LookupOrd(i, scratchRight);
                            Assert.AreEqual(scratchLeft, scratchRight, info);
                        }
                        // bytes
                        for (int docID = 0; docID < leftReader.MaxDoc; docID++)
                        {
                            leftValues.Get(docID, scratchLeft);
                            rightValues.Get(docID, scratchRight);
                            Assert.AreEqual(scratchLeft, scratchRight, info);
                        }
                    }
                    else
                    {
                        Assert.IsNull(leftValues, info);
                        Assert.IsNull(rightValues, info);
                    }
                }

                {
                    SortedSetDocValues leftValues = MultiDocValues.GetSortedSetValues(leftReader, field);
                    SortedSetDocValues rightValues = MultiDocValues.GetSortedSetValues(rightReader, field);
                    if (leftValues != null && rightValues != null)
                    {
                        // numOrds
                        Assert.AreEqual(leftValues.ValueCount, rightValues.ValueCount, info);
                        // ords
                        BytesRef scratchLeft = new BytesRef();
                        BytesRef scratchRight = new BytesRef();
                        for (int i = 0; i < leftValues.ValueCount; i++)
                        {
                            leftValues.LookupOrd(i, scratchLeft);
                            rightValues.LookupOrd(i, scratchRight);
                            Assert.AreEqual(scratchLeft, scratchRight, info);
                        }
                        // ord lists
                        for (int docID = 0; docID < leftReader.MaxDoc; docID++)
                        {
                            leftValues.SetDocument(docID);
                            rightValues.SetDocument(docID);
                            long ord;
                            while ((ord = leftValues.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                            {
                                Assert.AreEqual(ord, rightValues.NextOrd(), info);
                            }
                            Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, rightValues.NextOrd(), info);
                        }
                    }
                    else
                    {
                        Assert.IsNull(leftValues, info);
                        Assert.IsNull(rightValues, info);
                    }
                }

                {
                    IBits leftBits = MultiDocValues.GetDocsWithField(leftReader, field);
                    IBits rightBits = MultiDocValues.GetDocsWithField(rightReader, field);
                    if (leftBits != null && rightBits != null)
                    {
                        Assert.AreEqual(leftBits.Length, rightBits.Length, info);
                        for (int i = 0; i < leftBits.Length; i++)
                        {
                            Assert.AreEqual(leftBits.Get(i), rightBits.Get(i), info);
                        }
                    }
                    else
                    {
                        Assert.IsNull(leftBits, info);
                        Assert.IsNull(rightBits, info);
                    }
                }
            }
        }

        public virtual void AssertDocValuesEquals(string info, int num, NumericDocValues leftDocValues, NumericDocValues rightDocValues)
        {
            Assert.IsNotNull(leftDocValues, info);
            Assert.IsNotNull(rightDocValues, info);
            for (int docID = 0; docID < num; docID++)
            {
                Assert.AreEqual(leftDocValues.Get(docID), rightDocValues.Get(docID));
            }
        }

        // TODO: this is kinda stupid, we don't delete documents in the test.
        public virtual void AssertDeletedDocsEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(leftReader.NumDeletedDocs == rightReader.NumDeletedDocs);
            IBits leftBits = MultiFields.GetLiveDocs(leftReader);
            IBits rightBits = MultiFields.GetLiveDocs(rightReader);

            if (leftBits is null || rightBits is null)
            {
                Assert.IsNull(leftBits, info);
                Assert.IsNull(rightBits, info);
                return;
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(leftReader.MaxDoc == rightReader.MaxDoc);
            Assert.AreEqual(leftBits.Length, rightBits.Length, info);
            for (int i = 0; i < leftReader.MaxDoc; i++)
            {
                Assert.AreEqual(leftBits.Get(i), rightBits.Get(i), info);
            }
        }

        public virtual void AssertFieldInfosEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            FieldInfos leftInfos = MultiFields.GetMergedFieldInfos(leftReader);
            FieldInfos rightInfos = MultiFields.GetMergedFieldInfos(rightReader);

            // TODO: would be great to verify more than just the names of the fields!
            JCG.SortedSet<string> left = new JCG.SortedSet<string>(StringComparer.Ordinal);
            JCG.SortedSet<string> right = new JCG.SortedSet<string>(StringComparer.Ordinal);

            foreach (FieldInfo fi in leftInfos)
            {
                left.Add(fi.Name);
            }

            foreach (FieldInfo fi in rightInfos)
            {
                right.Add(fi.Name);
            }

            Assert.AreEqual(left, right, aggressive: false, info);
        }

        /// <summary>
        /// Returns true if the file exists (can be opened), false
        /// if it cannot be opened, and (unlike .NET's 
        /// <see cref="System.IO.File.Exists(string)"/>) if there's some
        /// unexpected error, returns <c>false</c>.
        /// </summary>
        public static bool SlowFileExists(Directory dir, string fileName)
        {
            try
            {
                dir.OpenInput(fileName, IOContext.DEFAULT).Dispose();
                return true;
            }
            catch (Exception e) when (e.IsNoSuchFileExceptionOrFileNotFoundException())
            {
                return false;
            }
        }

        /// <summary>
        /// A base location for temporary files of a given test. Helps in figuring out
        /// which tests left which files and where.
        /// </summary>
        private static DirectoryInfo tempDirBase;

        /// <summary>
        /// Retry to create temporary file name this many times.
        /// </summary>
        private const int TEMP_NAME_RETRY_THRESHOLD = 9999;

        // LUCENENET specific - omitted GetBaseTempDirForTestClass() method, since it was already deprecated and not recommended

        /// <summary>
        /// Creates an empty, temporary folder (when the name of the folder is of no importance).
        /// </summary>
        /// <seealso cref="CreateTempDir(string)"/>
        public static DirectoryInfo CreateTempDir()
        {
            return CreateTempDir("tempDir");
        }

        /// <summary>
        /// Creates an empty, temporary folder with the given name <paramref name="prefix"/> under the
        /// system's <see cref="Path.GetTempPath()"/> or if supplied, the <c>tempDir</c> system property.
        /// 
        /// <para/>The folder will be automatically removed after the
        /// test class completes successfully. The test should close any file handles that would prevent
        /// the folder from being removed.
        /// </summary>
        public static DirectoryInfo CreateTempDir(string prefix)
        {
            //DirectoryInfo @base = BaseTempDirForTestClass();
            string @base = SystemProperties.GetProperty("tempDir", System.IO.Path.GetTempPath());

            int attempt = 0;
            DirectoryInfo f;
            bool iterate = true;
            do
            {
                if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
                {
                    throw RuntimeException.Create("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: " + System.IO.Path.GetTempPath());
                }
                // LUCENENET specific - need to use a random file name instead of a sequential one or two threads may attempt to do 
                // two operations on a file at the same time.
                //f = new DirectoryInfo(Path.Combine(System.IO.Path.GetTempPath(), "LuceneTemp", prefix + "-" + attempt));
                f = new DirectoryInfo(Path.Combine(@base, "LuceneTemp", prefix + "-" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));

                try
                {
                    if (!System.IO.Directory.Exists(f.FullName))
                    {
                        f.Create();
                        iterate = false;
                    }
                }
                catch (Exception exc) when (exc.IsIOException())
                {
                    iterate = true;
                }
            } while (iterate);

            RegisterToRemoveAfterSuite(f);
            return f;
        }

        /// <summary>
        /// Creates an empty file with the given <paramref name="prefix"/> and <paramref name="suffix"/> under the
        /// system's <see cref="Path.GetTempPath()"/> or if supplied, the <c>tempDir</c> system property.
        ///
        /// <para/>The file will be automatically removed after the
        /// test class completes successfully. The test should close any file handles that would prevent
        /// the file from being removed.
        /// </summary>
        public static FileInfo CreateTempFile(string prefix, string suffix)
        {
            //DirectoryInfo @base = BaseTempDirForTestClass();
            string @base = SystemProperties.GetProperty("tempDir", System.IO.Path.GetTempPath());

            //int attempt = 0;
            FileInfo f = FileSupport.CreateTempFile(prefix, suffix, @base);
            //do
            //{
            //    if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
            //    {
            //        throw RuntimeException.Create("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: " + System.IO.Path.GetTempPath());
            //    }
            //    //f = new FileInfo(Path.Combine(System.IO.Path.GetTempPath(), prefix + "-" + string.Format(CultureInfo.InvariantCulture, "{0:D3}", attempt) + suffix));
            //    f = FileSupport.CreateTempFile(prefix, suffix, new DirectoryInfo(System.IO.Path.GetTempPath()));
            //} while (f.Create() is null);

            RegisterToRemoveAfterSuite(f);
            return f;
        }

        /// <summary>
        /// Creates an empty temporary file.
        ///
        /// <para/>The file will be automatically removed after the
        /// test class completes successfully. The test should close any file handles that would prevent
        /// the file from being removed.
        /// </summary>
        /// <seealso cref="CreateTempFile(string, string)"/>
        public static FileInfo CreateTempFile()
        {
            return CreateTempFile("tempFile", ".tmp");
        }

        /// <summary>
        /// A queue of temporary resources to be removed after the
        /// suite completes. </summary>
        /// <seealso cref="RegisterToRemoveAfterSuite(FileSystemInfo)"/>
        // LUCENENET specific - using a stack, since this is read in reverse order
        private static readonly Stack<FileSystemInfo> cleanupQueue = new Stack<FileSystemInfo>();
        private static readonly object cleanupQueueLock = new object();

        /// <summary>
        /// Register temporary folder for removal after the suite completes.
        /// </summary>
        private static void RegisterToRemoveAfterSuite(FileSystemInfo f)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(f != null);

            if (LuceneTestCase.LeaveTemporary)
            {
                Console.Error.WriteLine("INFO: Will leave temporary file: " + f.FullName);
                return;
            }
            UninterruptableMonitor.Enter(cleanupQueueLock);
            try
            {
                cleanupQueue.Push(f);
            }
            finally
            {
                UninterruptableMonitor.Exit(cleanupQueueLock);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected string GetFullMethodName([CallerMemberName] string memberName = "")
        {
            return string.Format("{0}+{1}", this.GetType().Name, memberName);
        }

        private static void CleanupTemporaryFiles()
        {
            // Drain cleanup queue and clear it.
            FileSystemInfo[] everything;
            string tempDirBasePath;

            UninterruptableMonitor.Enter(cleanupQueueLock);
            try
            {
                tempDirBasePath = tempDirBase?.FullName;
                tempDirBase = null;

                // LUCENENET: The stack order is alredy reversed, so no need to do that here as in Lucene
                everything = cleanupQueue.ToArray();
                cleanupQueue.Clear();
            }
            finally
            {
                UninterruptableMonitor.Exit(cleanupQueueLock);
            }

            // LUCENENET specific - If the everything array is empty, there is no reason
            // to continue.
            if (everything.Length == 0)
                return;

            // Only check and throw an IOException on un-removable files if the test
            // was successful. Otherwise just report the path of temporary files
            // and leave them there.
            if (LuceneTestCase.SuiteFailureMarker /*.WasSuccessful()*/)
            {
                try
                {
                    TestUtil.Rm(everything);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    //                    Type suiteClass = RandomizedContext.Current.GetTargetType;
                    //                    if (suiteClass.IsAnnotationPresent(typeof(SuppressTempFileChecks)))
                    //                    {
                    Console.Error.WriteLine("WARNING: Leftover undeleted temporary files " + e.Message);
                    return;
                    //                    }
                }
            }
            else
            {
                if (tempDirBasePath != null)
                {
                    Console.Error.WriteLine("NOTE: leaving temporary files on disk at: " + tempDirBasePath);
                }
            }
        }

        internal static void LogNativeFSFactoryDebugInfo()
        {
            // LUCENENET specific - log the current locking strategy used and HResult values
            // for assistance troubleshooting problems on Linux/macOS
            SystemConsole.WriteLine($"Locking Strategy: {NativeFSLockFactory.LockingStrategy}");
            SystemConsole.WriteLine($"Share Violation HResult: {(NativeFSLockFactory.HRESULT_FILE_SHARE_VIOLATION.HasValue ? NativeFSLockFactory.HRESULT_FILE_SHARE_VIOLATION.ToString() : "null")}");
            SystemConsole.WriteLine($"Lock Violation HResult: {(NativeFSLockFactory.HRESULT_FILE_LOCK_VIOLATION.HasValue ? NativeFSLockFactory.HRESULT_FILE_LOCK_VIOLATION.ToString() : "null")}");

            string fileName;
            try
            {
                // This could throw, but we don't care about this HResult value.
                fileName = Path.GetTempFileName();
            }
            catch (Exception e)
            {
                SystemConsole.WriteLine($"Error while creating temp file: {e}");
                return;
            }

            Stream lockStream = null;
            try
            {
                lockStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 1, FileOptions.None);
            }
            catch (Exception e)
            {
                SystemConsole.WriteLine($"Error while opening initial share stream: {e}");
                SystemConsole.WriteLine($"******* HResult: {e.HResult}");
                return;
            }
            finally
            {
                lockStream?.Dispose();
            }
            try
            {
                // Try to get an exclusive lock on the file - this should throw an IOException with the current platform's HResult value for FileShare violation
                using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.None, 1, FileOptions.None);
            }
            catch (IOException io) when (io.HResult != 0)
            {
                SystemConsole.WriteLine($"Successfully retrieved sharing violation.");
                SystemConsole.WriteLine($"******* HResult: {io.HResult}");
                SystemConsole.WriteLine($"Exception: {io}");
            }
            finally
            {
                lockStream?.Dispose();
            }
        }

        /// <summary>
        /// Returns the next pseudorandom, Gaussian ("normally") distributed
        /// <c>double</c> value with mean <c>0.0</c> and standard
        /// deviation <c>1.0</c> from this random number generator's sequence.
        /// <para/>
        /// The general contract of <see cref="RandomGaussian()"/> is that one
        /// <see cref="double"/> value, chosen from (approximately) the usual
        /// normal distribution with mean <c>0.0</c> and standard deviation
        /// <c>1.0</c>, is pseudorandomly generated and returned.
        /// 
        /// <para/>This uses the <i>polar method</i> of G. E. P. Box, M. E. Muller, and
        /// G. Marsaglia, as described by Donald E. Knuth in <i>The Art of
        /// Computer Programming</i>, Volume 3: <i>Seminumerical Algorithms</i>,
        /// section 3.4.1, subsection C, algorithm P. Note that it generates two
        /// independent values at the cost of only one call to <see cref="Math.Log(double)"/>
        /// and one call to <see cref="Math.Sqrt(double)"/>.
        /// </summary>
        /// <returns>The next pseudorandom, Gaussian ("normally") distributed
        /// <see cref="double"/> value with mean <c>0.0</c> and
        /// standard deviation <c>1.0</c> from this random number
        /// generator's sequence.</returns>
        // LUCENENET specific - moved this here so we can reference it more readily (similar to how Spatial does it).
        // However, this is also available as an extension method of the System.Random class in RandomizedTesting.Generators.
        // This method was originally in carrotsearch.randomizedtesting.RandomizedTest.
        public static double RandomGaussian() // LUCENENET: CA1822: Mark members as static
        {
            return Random.NextGaussian();
        }

        private sealed class ReaderClosedListenerAnonymousClass : IReaderDisposedListener
        {
            private readonly LimitedConcurrencyLevelTaskScheduler ex;

            public ReaderClosedListenerAnonymousClass(LimitedConcurrencyLevelTaskScheduler ex)
            {
                this.ex = ex;
            }

            public void OnDispose(IndexReader reader)
            {
                ex?.Shutdown();
                //TestUtil.ShutdownExecutorService(ex);
            }
        }
    }
}