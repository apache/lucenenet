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

using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using Lucene.Net.Support.Threading;
using Lucene.Net.TestFramework.Support;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Console = Lucene.Net.Support.SystemConsole;

// LUCENENET NOTE: These are primarily here because they are referred to
// in the XML documentation. Be sure to add a new option if a new test framework
// is being supported.
#if TESTFRAMEWORK_MSTEST

#elif TESTFRAMEWORK_XUNIT

#else // #elif TESTFRAMEWORK_NUNIT
using Before = NUnit.Framework.SetUpAttribute;
using After = NUnit.Framework.TearDownAttribute;
#endif


namespace Lucene.Net.Util
{
    using AlcoholicMergePolicy = Lucene.Net.Index.AlcoholicMergePolicy;
    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using AssertingAtomicReader = Lucene.Net.Index.AssertingAtomicReader;
    using AssertingDirectoryReader = Lucene.Net.Index.AssertingDirectoryReader;
    using AssertingIndexSearcher = Lucene.Net.Search.AssertingIndexSearcher;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using CacheEntry = Lucene.Net.Search.FieldCache.CacheEntry;
    using Codec = Lucene.Net.Codecs.Codec;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
    using CompositeReader = Lucene.Net.Index.CompositeReader;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using Document = Documents.Document;
    using FCInvisibleMultiReader = Lucene.Net.Search.FCInvisibleMultiReader;
    using Field = Field;
    using FieldFilterAtomicReader = Lucene.Net.Index.FieldFilterAtomicReader;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using Fields = Lucene.Net.Index.Fields;
    using FieldType = FieldType;
    using FlushInfo = Lucene.Net.Store.FlushInfo;
    using FSDirectory = Lucene.Net.Store.FSDirectory;
    using IIndexableField = Lucene.Net.Index.IIndexableField;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using Insanity = Lucene.Net.Util.FieldCacheSanityChecker.Insanity;
    using IOContext = Lucene.Net.Store.IOContext;
    //using Context = Lucene.Net.Store.IOContext.Context;
    using LockFactory = Lucene.Net.Store.LockFactory;
    using LogByteSizeMergePolicy = Lucene.Net.Index.LogByteSizeMergePolicy;
    using LogDocMergePolicy = Lucene.Net.Index.LogDocMergePolicy;
    using LogMergePolicy = Lucene.Net.Index.LogMergePolicy;
    using MergeInfo = Lucene.Net.Store.MergeInfo;
    using MergePolicy = Lucene.Net.Index.MergePolicy;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using MockRandomMergePolicy = Lucene.Net.Index.MockRandomMergePolicy;
    using MultiDocValues = Lucene.Net.Index.MultiDocValues;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using NRTCachingDirectory = Lucene.Net.Store.NRTCachingDirectory;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using ParallelAtomicReader = Lucene.Net.Index.ParallelAtomicReader;
    using ParallelCompositeReader = Lucene.Net.Index.ParallelCompositeReader;
    using RateLimitedDirectoryWrapper = Lucene.Net.Store.RateLimitedDirectoryWrapper;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using SegmentReader = Lucene.Net.Index.SegmentReader;
    using SerialMergeScheduler = Lucene.Net.Index.SerialMergeScheduler;
    using SimpleMergedSegmentWarmer = Lucene.Net.Index.SimpleMergedSegmentWarmer;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
    using StringField = StringField;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TextField = TextField;
    using TieredMergePolicy = Lucene.Net.Index.TieredMergePolicy;

    /*using After = org.junit.After;
    using AfterClass = org.junit.AfterClass;
    using Assert = org.junit.Assert;
    using Before = org.junit.Before;
    using BeforeClass = org.junit.BeforeClass;
    using ClassRule = org.junit.ClassRule;
    using Rule = org.junit.Rule;
    using Test = org.junit.Test;
    using RuleChain = org.junit.rules.RuleChain;
    using TestRule = org.junit.rules.TestRule;
    using RunWith = org.junit.runner.RunWith;

    using JUnit4MethodProvider = com.carrotsearch.randomizedtesting.JUnit4MethodProvider;
    using LifecycleScope = com.carrotsearch.randomizedtesting.LifecycleScope;
    using MixWithSuiteName = com.carrotsearch.randomizedtesting.MixWithSuiteName;
    using RandomizedContext = com.carrotsearch.randomizedtesting.RandomizedContext;
    using RandomizedRunner = com.carrotsearch.randomizedtesting.RandomizedRunner;
    using RandomizedTest = com.carrotsearch.randomizedtesting.RandomizedTest;
    using Listeners = com.carrotsearch.randomizedtesting.annotations.Listeners;
    using SeedDecorators = com.carrotsearch.randomizedtesting.annotations.SeedDecorators;
    using TestGroup = com.carrotsearch.randomizedtesting.annotations.TestGroup;
    using TestMethodProviders = com.carrotsearch.randomizedtesting.annotations.TestMethodProviders;
    using ThreadLeakAction = com.carrotsearch.randomizedtesting.annotations.ThreadLeakAction;
    using Action = com.carrotsearch.randomizedtesting.annotations.ThreadLeakAction.Action;
    using ThreadLeakFilters = com.carrotsearch.randomizedtesting.annotations.ThreadLeakFilters;
    using ThreadLeakGroup = com.carrotsearch.randomizedtesting.annotations.ThreadLeakGroup;
    using Group = com.carrotsearch.randomizedtesting.annotations.ThreadLeakGroup.Group;
    using ThreadLeakLingering = com.carrotsearch.randomizedtesting.annotations.ThreadLeakLingering;
    using ThreadLeakScope = com.carrotsearch.randomizedtesting.annotations.ThreadLeakScope;
    using Scope = com.carrotsearch.randomizedtesting.annotations.ThreadLeakScope.Scope;
    using ThreadLeakZombies = com.carrotsearch.randomizedtesting.annotations.ThreadLeakZombies;
    using Consequence = com.carrotsearch.randomizedtesting.annotations.ThreadLeakZombies.Consequence;
    using TimeoutSuite = com.carrotsearch.randomizedtesting.annotations.TimeoutSuite;
    using RandomPicks = com.carrotsearch.randomizedtesting.generators.RandomPicks;
    using NoClassHooksShadowingRule = com.carrotsearch.randomizedtesting.rules.NoClassHooksShadowingRule;
    using NoInstanceHooksOverridesRule = com.carrotsearch.randomizedtesting.rules.NoInstanceHooksOverridesRule;
    using StaticFieldsInvariantRule = com.carrotsearch.randomizedtesting.rules.StaticFieldsInvariantRule;
    using SystemPropertiesInvariantRule = com.carrotsearch.randomizedtesting.rules.SystemPropertiesInvariantRule;
    using TestRuleAdapter = com.carrotsearch.randomizedtesting.rules.TestRuleAdapter;
    */

    /// <summary>
    /// Base class for all Lucene.Net unit tests.
    ///
    /// <h3>Class and instance setup.</h3>
    ///
    /// <para>
    /// The preferred way to specify class (suite-level) setup/cleanup is to use
    /// static methods annotated with <see cref="BeforeClass"/> and <see cref="AfterClass"/>. Any
    /// code in these methods is executed within the test framework's control and
    /// ensure proper setup has been made. <b>Try not to use static initializers
    /// (including complex final field initializers).</b> Static initializers are
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
    /// Any test method annotated with <see cref="TestAttribute"/> is considered a test case.
    /// </para>
    ///
    /// <h3>Randomized execution and test facilities</h3>
    ///
    /// <para>
    /// <see cref="LuceneTestCase"/> uses <see cref="RandomizedRunner"/> to execute test cases.
    /// <see cref="RandomizedRunner"/> has built-in support for tests randomization
    /// including access to a repeatable <see cref="Random"/> instance. See
    /// <see cref="Random()"/> method. Any test using <see cref="Random"/> acquired from
    /// <see cref="Random()"/> should be fully reproducible (assuming no race conditions
    /// between threads etc.). The initial seed for a test case is reported in many
    /// ways:
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             as part of any exception thrown from its body (inserted as a dummy stack
    ///             trace entry),
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             as part of the main thread executing the test case (if your test hangs,
    ///             just dump the stack trace of all threads and you'll see the seed),
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             the master seed can also be accessed manually by getting the current
    ///             context (<see cref="RandomizedContext.Current"/>) and then calling
    ///             <see cref="RandomizedContext.RunnerSeed"/>.
    ///         </description>
    ///     </item>
    /// </list>
    /// </para>
    /// </summary>
    [TestFixture]
    public abstract partial class LuceneTestCase : Assert // Wait long for leaked threads to complete before failure. zk needs this. -  See LUCENE-3995 for rationale.
    {
        public LuceneTestCase()
        {
            ClassEnvRule = new TestRuleSetupAndRestoreClassEnv();
        }

        // --------------------------------------------------------------------
        // Test groups, system properties and other annotations modifying tests
        // --------------------------------------------------------------------

        internal const string SYSPROP_NIGHTLY = "tests.nightly"; // LUCENENET specific - made internal, because not fully implemented
        internal const string SYSPROP_WEEKLY = "tests.weekly"; // LUCENENET specific - made internal, because not fully implemented
        internal const string SYSPROP_AWAITSFIX = "tests.awaitsfix"; // LUCENENET specific - made internal, because not fully implemented
        internal const string SYSPROP_SLOW = "tests.slow"; // LUCENENET specific - made internal, because not fully implemented
        internal const string SYSPROP_BADAPPLES = "tests.badapples"; // LUCENENET specific - made internal, because not fully implemented

        ///// <seealso cref="IgnoreAfterMaxFailures"/>
        internal const string SYSPROP_MAXFAILURES = "tests.maxfailures"; // LUCENENET specific - made internal, because not fully implemented

        ///// <seealso cref="IgnoreAfterMaxFailures"/>
        internal const string SYSPROP_FAILFAST = "tests.failfast"; // // LUCENENET specific - made internal, because not fully implemented

        // LUCENENET: Not Implemented
        /////// <summary>
        /////// Annotation for tests that should only be run during nightly builds.
        /////// </summary>
        ////[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        ////public class NightlyAttribute : System.Attribute { } // LUCENENET: API looks better with this nested

        /////// <summary>
        /////// Annotation for tests that should only be run during weekly builds.
        /////// </summary>
        ////[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        ////public class WeeklyAttribute : System.Attribute { } // LUCENENET: API looks better with this nested

        /////// <summary>
        /////// Annotation for tests which exhibit a known issue and are temporarily disabled.
        /////// </summary>
        ////[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        ////public class AwaitsFix : System.Attribute // LUCENENET: API looks better with this nested
        ////{
        ////    /// <summary>
        ////    /// Point to issue tracker entry. </summary>
        ////    public virtual string BugUrl { get; set; }
        ////}

        /////// <summary>
        /////// Annotation for tests that are slow. Slow tests do run by default but can be
        /////// disabled if a quick run is needed.
        /////// </summary>
        ////public class SlowAttribute : System.Attribute { } // LUCENENET: API looks better with this nested

        /////// <summary>
        /////// Annotation for tests that fail frequently and should
        /////// be moved to a <a href="https://builds.apache.org/job/Lucene-BadApples-trunk-java7/">"vault" plan in Jenkins</a>.
        ///////
        /////// Tests annotated with this will be turned off by default. If you want to enable
        /////// them, set:
        /////// <pre>
        /////// -Dtests.badapples=true
        /////// </pre>
        /////// </summary>
        ////public class BadAppleAttribute : System.Attribute // LUCENENET: API looks better with this nested
        ////{
        ////    /// <summary>
        ////    /// Point to issue tracker entry. </summary>
        ////    public virtual string BugUrl { get; set;}
        ////}

        /// <summary>
        /// Annotation for test classes that should avoid certain codec types
        /// (because they are expensive, for example).
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        public class SuppressCodecsAttribute : System.Attribute // LUCENENET: API looks better with this nested
        {
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
        /// <seealso cref= LuceneTestCase.CreateTempFile(String, String)"/>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        public class SuppressTempFileChecksAttribute : System.Attribute // LUCENENET: API looks better with this nested
        {
            /// <summary>
            /// Point to issue tracker entry. </summary>
            public virtual string BugUrl { get; set; } = "None";
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
        public static readonly LuceneVersion TEST_VERSION_CURRENT = LuceneVersion.LUCENE_48;

        /// <summary>
        /// True if and only if tests are run in verbose mode. If this flag is false
        /// tests are not expected to print any messages.
        /// </summary>
        public static readonly bool VERBOSE = SystemProperties.GetPropertyAsBoolean("tests.verbose",
#if DEBUG
            true
#else
            false
#endif
);

        /// <summary>
        /// TODO: javadoc? </summary>
        public static readonly bool INFOSTREAM = SystemProperties.GetPropertyAsBoolean("tests.infostream", VERBOSE);

        /// <summary>
        /// A random multiplier which you should use when writing random tests:
        /// multiply it by the number of iterations to scale your tests (for nightly builds).
        /// </summary>
        public static readonly int RANDOM_MULTIPLIER = SystemProperties.GetPropertyAsInt32("tests.multiplier", 1);

        /// <summary>
        /// TODO: javadoc? </summary>
        public static readonly string DEFAULT_LINE_DOCS_FILE = "europarl.lines.txt.gz";

        /// <summary>
        /// TODO: javadoc? </summary>
        public static readonly string JENKINS_LARGE_LINE_DOCS_FILE = "enwiki.random.lines.txt";

        /// <summary>
        /// Gets the codec to run tests with. </summary>
        public static readonly string TEST_CODEC = SystemProperties.GetProperty("tests.codec", "random");

        /// <summary>
        /// Gets the postingsFormat to run tests with. </summary>
        public static readonly string TEST_POSTINGSFORMAT = SystemProperties.GetProperty("tests.postingsformat", "random");

        /// <summary>
        /// Gets the docValuesFormat to run tests with </summary>
        public static readonly string TEST_DOCVALUESFORMAT = SystemProperties.GetProperty("tests.docvaluesformat", "random");

        /// <summary>
        /// Gets the directory to run tests with </summary>
        public static readonly string TEST_DIRECTORY = SystemProperties.GetProperty("tests.directory", "random");

        /// <summary>
        /// The line file used by LineFileDocs </summary>
        public static readonly string TEST_LINE_DOCS_FILE = SystemProperties.GetProperty("tests.linedocsfile", DEFAULT_LINE_DOCS_FILE);

        /// <summary>
        /// Whether or not <see cref="Nightly"/> tests should run. </summary>
        public static readonly bool TEST_NIGHTLY = SystemProperties.GetPropertyAsBoolean(SYSPROP_NIGHTLY, false);

        /// <summary>
        /// Whether or not <see cref="Weekly"/> tests should run. </summary>
        public static readonly bool TEST_WEEKLY = SystemProperties.GetPropertyAsBoolean(SYSPROP_WEEKLY, false);

        /// <summary>
        /// Whether or not <see cref="AwaitsFix"/> tests should run. </summary>
        public static readonly bool TEST_AWAITSFIX = SystemProperties.GetPropertyAsBoolean(SYSPROP_AWAITSFIX, false);

        /// <summary>
        /// Whether or not <see cref="Slow"/> tests should run. </summary>
        public static readonly bool TEST_SLOW = SystemProperties.GetPropertyAsBoolean(SYSPROP_SLOW, false);

        /// <summary>
        /// Throttling, see <see cref="MockDirectoryWrapper.Throttling"/>. </summary>
        public static readonly Throttling TEST_THROTTLING = TEST_NIGHTLY ? Throttling.SOMETIMES : Throttling.NEVER;

        /// <summary>
        /// Leave temporary files on disk, even on successful runs. </summary>
        public static readonly bool LEAVE_TEMPORARY;

        static LuceneTestCase()
        {
            bool defaultValue = false;
            foreach (string property in Arrays.AsList("tests.leaveTemporary", "tests.leavetemporary", "tests.leavetmpdir", "solr.test.leavetmpdir")) // Solr's legacy -  default -  lowercase -  ANT tasks's (junit4) flag.
            {
                defaultValue |= SystemProperties.GetPropertyAsBoolean(property, false);
            }
            LEAVE_TEMPORARY = defaultValue;
            CORE_DIRECTORIES = new List<string>(FS_DIRECTORIES);
            CORE_DIRECTORIES.Add("RAMDirectory");
            int maxFailures = SystemProperties.GetPropertyAsInt32(SYSPROP_MAXFAILURES, int.MaxValue);
            bool failFast = SystemProperties.GetPropertyAsBoolean(SYSPROP_FAILFAST, false);

            if (failFast)
            {
                if (maxFailures == int.MaxValue)
                {
                    maxFailures = 1;
                }
                else
                {
                    Console.Out.Write(typeof(LuceneTestCase).Name + " WARNING: Property '" + SYSPROP_MAXFAILURES + "'=" + maxFailures + ", 'failfast' is" + " ignored.");
                }
            }

            //IgnoreAfterMaxFailuresDelegate = new AtomicReference<TestRuleIgnoreAfterMaxFailures>(new TestRuleIgnoreAfterMaxFailures(maxFailures));
            //IgnoreAfterMaxFailures = TestRuleDelegate.Of(IgnoreAfterMaxFailuresDelegate);
        }

        // LUCENENET: Not Implemented
        /////// <summary>
        /////// These property keys will be ignored in verification of altered properties. </summary>
        /////// <seealso> cref= SystemPropertiesInvariantRule </seealso>
        /////// <seealso> cref= #ruleChain </seealso>
        /////// <seealso> cref= #classRules </seealso>
        ////private static readonly string[] IGNORED_INVARIANT_PROPERTIES = { "user.timezone", "java.rmi.server.randomIDs" };

        /// <summary>
        /// Filesystem-based <see cref="Directory"/> implementations. </summary>
        private static readonly IList<string> FS_DIRECTORIES = Arrays.AsList(
            "SimpleFSDirectory", 
            "NIOFSDirectory", 
            "MMapDirectory"
        );

        /// <summary>
        /// All <see cref="Directory"/> implementations. </summary>
        private static readonly IList<string> CORE_DIRECTORIES;

        protected static readonly HashSet<string> m_doesntSupportOffsets = new HashSet<string>(Arrays.AsList(
            "Lucene3x", 
            "MockFixedIntBlock", 
            "MockVariableIntBlock", 
            "MockSep", 
            "MockRandom"
        ));

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
        public static bool OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;

        // -----------------------------------------------------------------
        // Class level (suite) rules.
        // -----------------------------------------------------------------

        /// <summary>
        /// Stores the currently class under test.
        /// </summary>
        //private static TestRuleStoreClassName ClassNameRule;

        /// <summary>
        /// Class environment setup rule.
        /// 
        /// LUCENENET specific
        /// Is non-static to remove inter-class dependencies on this variable
        /// </summary>
        internal TestRuleSetupAndRestoreClassEnv ClassEnvRule { get; private set; }

        /// <summary>
        /// Gets the Similarity from the Class Environment setup rule
        /// 
        /// LUCENENET specific
        /// Exposed because <see cref="TestRuleSetupAndRestoreClassEnv"/> is
        /// internal and this field is needed by other classes.
        /// </summary>
        public Similarity Similarity { get { return ClassEnvRule.similarity; } }

        /// <summary>
        /// Gets the Timezone from the Class Environment setup rule
        /// 
        /// LUCENENET specific
        /// Exposed because <see cref="TestRuleSetupAndRestoreClassEnv"/> is
        /// internal and this field is needed by other classes.
        /// </summary>
        public TimeZoneInfo TimeZone { get { return ClassEnvRule.timeZone; } }

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
        ////private static AtomicReference<TestRuleIgnoreAfterMaxFailures> IgnoreAfterMaxFailuresDelegate;

        //////private static TestRule IgnoreAfterMaxFailures;

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
        //////private static ISet<string> STATIC_LEAK_IGNORED_TYPES = new HashSet<string>(Arrays.AsList("org.slf4j.Logger", "org.apache.solr.SolrLogFormatter", typeof(EnumSet).Name));

        /////// <summary>
        /////// this controls how suite-level rules are nested. It is important that _all_ rules declared
        /////// in <seealso cref="LuceneTestCase"/> are executed in proper order if they depend on each
        /////// other.
        /////// </summary>

        ////public static TestRule ClassRules = RuleChain
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
        /////*private TestRuleSetupTeardownChained ParentChainCallRule = new TestRuleSetupTeardownChained();

        /////// <summary>
        /////// Save test thread and name. </summary>
        ////private TestRuleThreadAndTestName ThreadAndTestNameRule = new TestRuleThreadAndTestName();

        /////// <summary>
        /////// Taint suite result with individual test failures. </summary>
        ////private TestRuleMarkFailure TestFailureMarker = new TestRuleMarkFailure(SuiteFailureMarker);*/

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

        /// <summary>
        /// For subclasses to override. Overrides must call <c>base.SetUp()</c>.
        /// </summary>
        [SetUp]
        public virtual void SetUp()
        {
            // LUCENENET TODO: Not sure how to convert these
            //ParentChainCallRule.SetupCalled = true;

            // LUCENENET: Printing out randomized context regardless
            // of whether verbose is enabled (since we need it for debugging,
            // but the verbose output can crash tests).
            Console.Write("Culture: ");
            Console.WriteLine(this.ClassEnvRule.locale.Name);

            Console.Write("Time Zone: ");
            Console.WriteLine(this.ClassEnvRule.timeZone.DisplayName);

            Console.Write("Default Codec: ");
            Console.Write(this.ClassEnvRule.codec.Name);
            Console.Write(" (");
            Console.Write(this.ClassEnvRule.codec.GetType().ToString());
            Console.WriteLine(")");

            Console.Write("Default Similarity: ");
            Console.WriteLine(this.ClassEnvRule.similarity.ToString());
        }

        /// <summary>
        /// For subclasses to override. Overrides must call <c>base.TearDown()</c>.
        /// </summary>
        [TearDown]
        public virtual void TearDown()
        {
            /* LUCENENET TODO: Not sure how to convert these
                ParentChainCallRule.TeardownCalled = true;
                */
        }

        // LUCENENET specific constants to scan the test framework for codecs/docvaluesformats/postingsformats only once
        private static readonly TestCodecFactory TEST_CODEC_FACTORY = new TestCodecFactory();
        private static readonly TestDocValuesFormatFactory TEST_DOCVALUES_FORMAT_FACTORY = new TestDocValuesFormatFactory();
        private static readonly TestPostingsFormatFactory TEST_POSTINGS_FORMAT_FACTORY = new TestPostingsFormatFactory();


        // LUCENENET specific method for setting up dependency injection of test classes.
        [OneTimeSetUp]
        public virtual void BeforeClass()
        {
            
            try
            {
                // Setup the factories
                Codec.SetCodecFactory(TEST_CODEC_FACTORY);
                DocValuesFormat.SetDocValuesFormatFactory(TEST_DOCVALUES_FORMAT_FACTORY);
                PostingsFormat.SetPostingsFormatFactory(TEST_POSTINGS_FORMAT_FACTORY);

                ClassEnvRule.Before(this);
            }
            catch (Exception ex)
            {
                // Print the stack trace so we have something to go on if an error occurs here.
                Console.Write("An exception occurred during BeforeClass: ");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        [OneTimeTearDown]
        public virtual void AfterClass()
        {
            try
            {
                ClassEnvRule.After(this);
                CleanupTemporaryFiles();
            }
            catch (Exception ex)
            {
                // Print the stack trace so we have something to go on if an error occurs here.
                Console.Write("An exception occurred during AfterClass: ");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        // -----------------------------------------------------------------
        // Test facilities and facades for subclasses.
        // -----------------------------------------------------------------

        /// <summary>
        /// Access to the current <see cref="RandomizedContext"/>'s Random instance. It is safe to use
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
        /// Random random = new Random(Random.Next());
        /// // tight loop with many invocations.
        /// </code>
        /// </summary>
        public static Random Random
        {
            get
            {
                return _random ?? (_random = new Random(/* LUCENENET TODO seed */));
                //return RandomizedContext.Current.Random;
            }
        }

        [ThreadStatic]
        private static Random _random;

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
        /// Return the current class being tested.
        /// </summary>
        public static Type TestClass //LUCENENET TODO: Either implement, or change the doc to indicate it is hard coded
        {
            get
            {
                return typeof(LuceneTestCase); // LUCENENET TODO: return this.GetType();
            }
        }

        /// <summary>
        /// Return the name of the currently executing test case.
        /// </summary>
        public string TestName //LUCENENET TODO: Either implement, or change the doc to indicate it is hard coded
        {
            get
            {
                //return ThreadAndTestNameRule.TestMethodName;
                return "LuceneTestCase"; // LUCENENET TODO: return this.GetType().GetTypeInfo().Name
            }
        }

        /// <summary>
        /// Some tests expect the directory to contain a single segment, and want to
        /// do tests on that segment's reader. This is an utility method to help them.
        /// </summary>
        public static SegmentReader GetOnlySegmentReader(DirectoryReader reader)
        {
            IList<AtomicReaderContext> subReaders = reader.Leaves;
            if (subReaders.Count != 1)
            {
                throw new System.ArgumentException(reader + " has " + subReaders.Count + " segments instead of exactly one");
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
        public static bool IsTestThread
        {
            get
            {
                /*Assert.IsNotNull(ThreadAndTestNameRule.TestCaseThread, "Test case thread not set?");
                return Thread.CurrentThread == ThreadAndTestNameRule.TestCaseThread;*/
                return true;
            }
        }

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
                catch (Exception /*e*/)
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
        /// The actual number returned will be influenced by whether <see cref="TEST_NIGHTLY"/>
        /// is active and <see cref="RANDOM_MULTIPLIER"/>, but also with some random fudge.
        /// </summary>
        public static int AtLeast(Random random, int i)
        {
            int min = (TEST_NIGHTLY ? 2 * i : i) * RANDOM_MULTIPLIER;
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
        /// The actual number returned will be influenced by whether <see cref="TEST_NIGHTLY"/>
        /// is active and <see cref="RANDOM_MULTIPLIER"/>.
        /// </summary>
        public static bool Rarely(Random random)
        {
            int p = TEST_NIGHTLY ? 10 : 1;
            p += (int)(p * Math.Log(RANDOM_MULTIPLIER));
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

        public static void AssumeTrue(string msg, bool condition)
        {
            RandomizedTest.AssumeTrue(msg, condition);
        }

        public static void AssumeFalse(string msg, bool condition)
        {
            RandomizedTest.AssumeFalse(msg, condition);
        }

        public static void AssumeNoException(string msg, Exception e) // LUCENENET TODO: Either implement or eliminate
        {
            //RandomizedTest.AssumeNoException(msg, e);
        }

        /// <summary>
        /// Return <paramref name="args"/> as a <see cref="ISet{Object}"/> instance. The order of elements is not
        /// preserved in enumerators.
        /// </summary>
        public static ISet<object> AsSet(params object[] args)
        {
            return new HashSet<object>(Arrays.AsList(args));
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
                    stream.WriteLine(iter.Current.ToString()); // LUCENENET TODO: Call Collections.ToString()
                }
            }
            stream.WriteLine("*** END " + label + " ***");
        }

        /// <summary>
        /// Convenience method for logging an array.  Wraps the array in an enumerator and delegates to <see cref="DumpEnumerator(string, IEnumerator, TextWriter)"/>
        /// </summary>
        /// <seealso cref="DumpEnumerator(string, IEnumerator, TextWriter)"/>
        public static void DumpArray(string label, Object[] objs, TextWriter stream)
        {
            IEnumerator iter = (null == objs) ? (IEnumerator)null : Arrays.AsList(objs).GetEnumerator();
            DumpEnumerator(label, iter, stream);
        }

        /// <summary>
        /// Create a new index writer config with random defaults.
        /// 
        /// LUCENENET specific
        /// Non-static so that we do not depend on any hidden static dependencies
        /// </summary>
        public IndexWriterConfig NewIndexWriterConfig(LuceneVersion v, Analyzer a)
        {
            return NewIndexWriterConfig(Random, v, a);
        }

        /// <summary>
        /// LUCENENET specific
        /// Non-static so that we do not depend on any hidden static dependencies
        /// </summary>
        public IndexWriterConfig NewIndexWriterConfig(Random r, LuceneVersion v, Analyzer a)
        {
            return NewIndexWriterConfig(r, v, a, ClassEnvRule.similarity, ClassEnvRule.timeZone);
        }

        /// <summary>
        /// create a new index writer config with random defaults using the specified random
        /// 
        /// LUCENENET specific
        /// This is the only static ctor for IndexWriterConfig because it removes the dependency
        /// on ClassEnvRule by using parameters Similarity and TimeZone.
        /// </summary>
        public static IndexWriterConfig NewIndexWriterConfig(Random r, LuceneVersion v, Analyzer a, Similarity similarity, TimeZoneInfo timezone) // LUCENENET TODO: API - Investigate how to eliminate these extra parameters, similarity and timezone
        {
            IndexWriterConfig c = new IndexWriterConfig(v, a);
            c.SetSimilarity(similarity);
            if (VERBOSE)
            {
                // Even though TestRuleSetupAndRestoreClassEnv calls
                // InfoStream.setDefault, we do it again here so that
                // the PrintStreamInfoStream.messageID increments so
                // that when there are separate instances of
                // IndexWriter created we see "IW 0", "IW 1", "IW 2",
                // ... instead of just always "IW 0":
                c.SetInfoStream(new TestRuleSetupAndRestoreClassEnv.ThreadNameFixingPrintStreamInfoStream(Console.Out));
            }

            if (r.NextBoolean())
            {
                c.SetMergeScheduler(new SerialMergeScheduler());
            }
            else if (Rarely(r))
            {
                int maxThreadCount = TestUtil.NextInt32(Random, 1, 4);
                int maxMergeCount = TestUtil.NextInt32(Random, maxThreadCount, maxThreadCount + 4);
                IConcurrentMergeScheduler mergeScheduler;

#if !FEATURE_CONCURRENTMERGESCHEDULER
                mergeScheduler = new TaskMergeScheduler();
#else
                if (r.NextBoolean())
                {
                    mergeScheduler = new ConcurrentMergeScheduler();
                }
                else
                {
                    mergeScheduler = new TaskMergeScheduler();
                }
#endif
                mergeScheduler.SetMaxMergesAndThreads(maxMergeCount, maxThreadCount);
                c.SetMergeScheduler(mergeScheduler);
            }
            if (r.NextBoolean())
            {
                if (Rarely(r))
                {
                    // crazy value
                    c.SetMaxBufferedDocs(TestUtil.NextInt32(r, 2, 15));
                }
                else
                {
                    // reasonable value
                    c.SetMaxBufferedDocs(TestUtil.NextInt32(r, 16, 1000));
                }
            }
            if (r.NextBoolean())
            {
                if (Rarely(r))
                {
                    // crazy value
                    c.SetTermIndexInterval(r.NextBoolean() ? TestUtil.NextInt32(r, 1, 31) : TestUtil.NextInt32(r, 129, 1000));
                }
                else
                {
                    // reasonable value
                    c.SetTermIndexInterval(TestUtil.NextInt32(r, 32, 128));
                }
            }
            if (r.NextBoolean())
            {
                int maxNumThreadStates = Rarely(r) ? TestUtil.NextInt32(r, 5, 20) : TestUtil.NextInt32(r, 1, 4); // reasonable value -  crazy value

                if (Rarely(r))
                {
                    //// Retrieve the package-private setIndexerThreadPool
                    //// method:
                    ////MethodInfo setIndexerThreadPoolMethod = typeof(IndexWriterConfig).GetTypeInfo().GetMethod("SetIndexerThreadPool", new Type[] { typeof(DocumentsWriterPerThreadPool) });
                    //MethodInfo setIndexerThreadPoolMethod = typeof(IndexWriterConfig).GetTypeInfo().GetMethod(
                    //    "SetIndexerThreadPool", 
                    //    BindingFlags.NonPublic | BindingFlags.Instance, 
                    //    null, 
                    //    new Type[] { typeof(DocumentsWriterPerThreadPool) }, 
                    //    null);
                    ////setIndexerThreadPoolMethod.setAccessible(true);
                    //Type clazz = typeof(RandomDocumentsWriterPerThreadPool);
                    //ConstructorInfo ctor = clazz.GetTypeInfo().GetConstructor(new[] { typeof(int), typeof(Random) });
                    ////ctor.Accessible = true;
                    //// random thread pool
                    //setIndexerThreadPoolMethod.Invoke(c, new[] { ctor.Invoke(new object[] { maxNumThreadStates, r }) });

                    // LUCENENET specific: Since we are using InternalsVisibleTo, there is no need for Reflection
                    c.SetIndexerThreadPool(new RandomDocumentsWriterPerThreadPool(maxNumThreadStates, r));
                }
                else
                {
                    // random thread pool
                    c.SetMaxThreadStates(maxNumThreadStates);
                }
            }

            c.SetMergePolicy(NewMergePolicy(r, timezone));

            if (Rarely(r))
            {
                c.SetMergedSegmentWarmer(new SimpleMergedSegmentWarmer(c.InfoStream));
            }
            c.SetUseCompoundFile(r.NextBoolean());
            c.SetReaderPooling(r.NextBoolean());
            c.SetReaderTermsIndexDivisor(TestUtil.NextInt32(r, 1, 4));
            c.SetCheckIntegrityAtMerge(r.NextBoolean());
            return c;
        }

        /// <param name="timezone">
        /// LUCENENET specific
        /// Timezone added to remove dependency on the then-static <see cref="ClassEnvRule"/>
        /// </param>
        public static MergePolicy NewMergePolicy(Random r, TimeZoneInfo timezone)
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
                return NewAlcoholicMergePolicy(r);
            }
            return NewLogMergePolicy(r);
        }

        /// <param name="timezone">
        /// LUCENENET specific
        /// Timezone added to remove dependency on the then-static <see cref="ClassEnvRule"/>
        /// </param>
        public static MergePolicy NewMergePolicy(TimeZoneInfo timezone)
        {
            return NewMergePolicy(Random, timezone);
        }

        public static LogMergePolicy NewLogMergePolicy()
        {
            return NewLogMergePolicy(Random);
        }

        public static TieredMergePolicy NewTieredMergePolicy()
        {
            return NewTieredMergePolicy(Random);
        }

        public AlcoholicMergePolicy NewAlcoholicMergePolicy()
        {
            return NewAlcoholicMergePolicy(Random);
        }

        public static AlcoholicMergePolicy NewAlcoholicMergePolicy(Random r)
        {
            return new AlcoholicMergePolicy(new Random(r.Next()));
        }

        public static LogMergePolicy NewLogMergePolicy(Random r)
        {
            LogMergePolicy logmp = r.NextBoolean() ? (LogMergePolicy)new LogDocMergePolicy() : new LogByteSizeMergePolicy();

            logmp.CalibrateSizeByDeletes = r.NextBoolean();
            if (Rarely(r))
            {
                logmp.MergeFactor = TestUtil.NextInt32(r, 2, 9);
            }
            else
            {
                logmp.MergeFactor = TestUtil.NextInt32(r, 10, 50);
            }
            ConfigureRandom(r, logmp);
            return logmp;
        }

        private static void ConfigureRandom(Random r, MergePolicy mergePolicy)
        {
            if (r.NextBoolean())
            {
                mergePolicy.NoCFSRatio = 0.1 + r.NextDouble() * 0.8;
            }
            else
            {
                mergePolicy.NoCFSRatio = r.NextBoolean() ? 1.0 : 0.0;
            }

            if (Rarely())
            {
                mergePolicy.MaxCFSSegmentSizeMB = 0.2 + r.NextDouble() * 2.0;
            }
            else
            {
                mergePolicy.MaxCFSSegmentSizeMB = double.PositiveInfinity;
            }
        }

        public static TieredMergePolicy NewTieredMergePolicy(Random r)
        {
            TieredMergePolicy tmp = new TieredMergePolicy();
            if (Rarely(r))
            {
                tmp.MaxMergeAtOnce = TestUtil.NextInt32(r, 2, 9);
                tmp.MaxMergeAtOnceExplicit = TestUtil.NextInt32(r, 2, 9);
            }
            else
            {
                tmp.MaxMergeAtOnce = TestUtil.NextInt32(r, 10, 50);
                tmp.MaxMergeAtOnceExplicit = TestUtil.NextInt32(r, 10, 50);
            }
            if (Rarely(r))
            {
                tmp.MaxMergedSegmentMB = 0.2 + r.NextDouble() * 2.0;
            }
            else
            {
                tmp.MaxMergedSegmentMB = r.NextDouble() * 100;
            }
            tmp.FloorSegmentMB = 0.2 + r.NextDouble() * 2.0;
            tmp.ForceMergeDeletesPctAllowed = 0.0 + r.NextDouble() * 30.0;
            if (Rarely(r))
            {
                tmp.SegmentsPerTier = TestUtil.NextInt32(r, 2, 20);
            }
            else
            {
                tmp.SegmentsPerTier = TestUtil.NextInt32(r, 10, 50);
            }
            ConfigureRandom(r, tmp);
            tmp.ReclaimDeletesWeight = r.NextDouble() * 4;
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
            var newDir = NewDirectoryImpl(random, TEST_DIRECTORY);

            return WrapDirectory(random, newDir, Rarely(random));
        }

        public static MockDirectoryWrapper NewMockDirectory()
        {
            return NewMockDirectory(Random);
        }

        public static MockDirectoryWrapper NewMockDirectory(Random r)
        {
            return (MockDirectoryWrapper)WrapDirectory(r, NewDirectoryImpl(r, TEST_DIRECTORY), false);
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
            string fsdirClass = TEST_DIRECTORY;
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
            if (clazz == null || !(typeof(FSDirectory).IsAssignableFrom(clazz)))
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
            Directory impl = NewDirectoryImpl(random, TEST_DIRECTORY);
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
                if (LuceneTestCase.VERBOSE)
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
                // LUCENENET TODO CloseAfterSuite(new IDisposableDirectory(@base, SuiteFailureMarker));
                return @base;
            }
            else
            {
                MockDirectoryWrapper mock = new MockDirectoryWrapper(random, directory);

                mock.Throttling = TEST_THROTTLING;
                // LUCENENET TODO CloseAfterSuite(new IDisposableDirectory(mock, SuiteFailureMarker));
                return mock;
            }
        }


        // LUCENENET specific: non-static
        public Field NewStringField(string name, string value, Field.Store stored)
        {
            return NewField(Random, name, value, stored == Field.Store.YES ? StringField.TYPE_STORED : StringField.TYPE_NOT_STORED);
        }

        // LUCENENET specific: non-static
        public Field NewTextField(string name, string value, Field.Store stored)
        {
            return NewField(Random, name, value, stored == Field.Store.YES ? TextField.TYPE_STORED : TextField.TYPE_NOT_STORED);
        }

        // LUCENENET specific: non-static
        public Field NewStringField(Random random, string name, string value, Field.Store stored)
        {
            return NewField(random, name, value, stored == Field.Store.YES ? StringField.TYPE_STORED : StringField.TYPE_NOT_STORED);
        }

        // LUCENENET specific: non-static
        public Field NewTextField(Random random, string name, string value, Field.Store stored)
        {
            return NewField(random, name, value, stored == Field.Store.YES ? TextField.TYPE_STORED : TextField.TYPE_NOT_STORED);
        }

        // LUCENENET specific: non-static
        public Field NewField(string name, string value, FieldType type)
        {
            return NewField(Random, name, value, type);
        }

        // LUCENENET specific: non-static
        public Field NewField(Random random, string name, string value, FieldType type)
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

                    if (newType.StoreTermVectorPositions && !newType.StoreTermVectorPayloads && !OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
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
            return RandomPicks.RandomFrom(random, CultureInfoSupport.GetNeutralAndSpecificCultures());
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

            //string[] elements = Regex.Split(localeName, "\\_", RegexOptions.Compiled);
            //switch (elements.Length)
            //{
            //    case 4: // fallthrough for special cases
            //    case 3:
            //    return new Locale(elements[0], elements[1], elements[2]);

            //    case 2:
            //    return new Locale(elements[0], elements[1]);

            //    case 1:
            //    return new Locale(elements[0]);

            //    default:
            //    throw new System.ArgumentException("Invalid Locale: " + localeName);
            //}
        }

        public static bool DefaultCodecSupportsDocValues
        {
            get{ return !Codec.Default.Name.Equals("Lucene3x", StringComparison.Ordinal); }
        }

        private static Directory NewFSDirectoryImpl(Type clazz, DirectoryInfo file)
        {
            return CommandLineUtil.NewFSDirectory(clazz, file);
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

            if (VERBOSE)
            {
                Trace.TraceInformation("Type of Directory is : {0}", clazzName);
            }

            Type clazz = CommandLineUtil.LoadDirectoryClass(clazzName);
            if (clazz == null)
                throw new InvalidOperationException($"Type '{clazzName}' could not be instantiated.");
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
                            r = (r is AtomicReader) ? (IndexReader)new ParallelAtomicReader((AtomicReader)r) : new ParallelCompositeReader((CompositeReader)r);
                            break;

                        case 2:
                            // Häckidy-Hick-Hack: a standard MultiReader will cause FC insanity, so we use
                            // QueryUtils' reader with a fake cache key, so insanity checker cannot walk
                            // along our reader:
                            r = new FCInvisibleMultiReader(r);
                            break;

                        case 3:
                            AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);
                            IList<string> allFields = new List<string>();
                            foreach (FieldInfo fi in ar.FieldInfos)
                            {
                                allFields.Add(fi.Name);
                            }
                            Collections.Shuffle(allFields);
                            int end = allFields.Count == 0 ? 0 : random.Next(allFields.Count);
                            HashSet<string> fields = new HashSet<string>(allFields.SubList(0, end));
                            // will create no FC insanity as ParallelAtomicReader has own cache key:
                            r = new ParallelAtomicReader(new FieldFilterAtomicReader(ar, fields, false), new FieldFilterAtomicReader(ar, fields, true));
                            break;

                        case 4:
                            // Häckidy-Hick-Hack: a standard Reader will cause FC insanity, so we use
                            // QueryUtils' reader with a fake cache key, so insanity checker cannot walk
                            // along our reader:
                            if (r is AtomicReader)
                            {
                                r = new AssertingAtomicReader((AtomicReader)r);
                            }
                            else if (r is DirectoryReader)
                            {
                                r = new AssertingDirectoryReader((DirectoryReader)r);
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
                if (VERBOSE)
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
        /// 
        /// LUCENENET specific
        /// Is non-static because <see cref="ClassEnvRule"/> is now non-static.
        /// </summary>
        public IndexSearcher NewSearcher(IndexReader r)
        {
            return NewSearcher(r, ClassEnvRule.similarity);
        }

        /// <param name="similarity">
        /// LUCENENET specific
        /// Removes dependency on <see cref="LuceneTestCase.ClassEnv.Similarity"/>
        /// </param>
        public static IndexSearcher NewSearcher(IndexReader r, Similarity similarity)
        {
            return NewSearcher(r, true, similarity);
        }

        /// <summary>
        /// Create a new searcher over the reader. this searcher might randomly use
        /// threads.
        /// </summary>
        /// <param name="similarity">
        /// LUCENENET specific
        /// Removes dependency on <see cref="LuceneTestCase.ClassEnv.Similarity"/>
        /// </param>
        public static IndexSearcher NewSearcher(IndexReader r, bool maybeWrap, Similarity similarity)
        {
            return NewSearcher(r, maybeWrap, true, similarity);
        }

        /// <summary>
        /// Create a new searcher over the reader. This searcher might randomly use
        /// threads.
        /// </summary>
        public IndexSearcher NewSearcher(IndexReader r, bool maybeWrap)
        {
            return NewSearcher(r, maybeWrap, true);
        }

        public IndexSearcher NewSearcher(IndexReader r, bool maybeWrap, bool wrapWithAssertions)
        {
            return NewSearcher(r, maybeWrap, wrapWithAssertions, ClassEnvRule.similarity);
        }

        /// <summary>
        /// Create a new searcher over the reader. this searcher might randomly use
        /// threads. If <paramref name="maybeWrap"/> is true, this searcher might wrap the
        /// reader with one that returns null for <see cref="CompositeReader.GetSequentialSubReaders()"/>. If
        /// <paramref name="wrapWithAssertions"/> is true, this searcher might be an
        /// <see cref="AssertingIndexSearcher"/> instance.
        /// </summary>
        /// <param name="similarity">
        /// LUCENENET specific
        /// Removes dependency on <see cref="LuceneTestCase.ClassEnv.Similarity"/>
        /// </param>
        public static IndexSearcher NewSearcher(IndexReader r, bool maybeWrap, bool wrapWithAssertions, Similarity similarity)
        {
            Random random = Random;
            if (Usually())
            {
                if (maybeWrap)
                {
                    r = MaybeWrapReader(r);
                }
                // TODO: this whole check is a coverage hack, we should move it to tests for various filterreaders.
                // ultimately whatever you do will be checkIndex'd at the end anyway.
                if (random.Next(500) == 0 && r is AtomicReader)
                {
                    // TODO: not useful to check DirectoryReader (redundant with checkindex)
                    // but maybe sometimes run this on the other crazy readers maybeWrapReader creates?
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
                ret.Similarity = similarity;
                return ret;
            }
            else
            {
                int threads = 0;
                TaskScheduler ex;
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
                    if (VERBOSE)
                    {
                        Console.WriteLine("NOTE: newSearcher using ExecutorService with " + threads + " threads");
                    }
                    //r.AddReaderClosedListener(new ReaderClosedListenerAnonymousInnerClassHelper(ex)); // LUCENENET TODO: Implement event (see the commented ReaderClosedListenerAnonymousInnerClassHelper class near the bottom of this file)
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
                ret.Similarity = similarity;
                return ret;
            }
        }

        /// <summary>
        /// Gets a resource from the classpath as <see cref="Stream"/>. This method should only
        /// be used, if a real file is needed. To get a stream, code should prefer
        /// <see cref="AssemblyExtensions.FindAndGetManifestResourceStream(Assembly, Type, string)"/> using 
        /// <c>this.GetType().Assembly</c> and <c>this.GetType()</c>.
        /// </summary>
        protected Stream GetDataFile(string name)
        {
            try
            {
                return this.GetType().getResourceAsStream(name);
            }
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {
                throw new IOException("Cannot find resource: " + name);
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

        public void AssertReaderEquals(string info, IndexReader leftReader, IndexReader rightReader)
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
        public void AssertReaderStatisticsEquals(string info, IndexReader leftReader, IndexReader rightReader)
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
        public void AssertFieldsEquals(string info, IndexReader leftReader, Fields leftFields, Fields rightFields, bool deep)
        {
            // Fields could be null if there are no postings,
            // but then it must be null for both
            if (leftFields == null || rightFields == null)
            {
                Assert.IsNull(leftFields, info);
                Assert.IsNull(rightFields, info);
                return;
            }
            AssertFieldStatisticsEquals(info, leftFields, rightFields);

            using (IEnumerator<string> leftEnum = leftFields.GetEnumerator())
            using (IEnumerator<string> rightEnum = rightFields.GetEnumerator())
            {
                while (leftEnum.MoveNext())
                {
                    string field = leftEnum.Current;
                    rightEnum.MoveNext();
                    Assert.AreEqual(field, rightEnum.Current, info);
                    AssertTermsEquals(info, leftReader, leftFields.GetTerms(field), rightFields.GetTerms(field), deep);
                }
                Assert.IsFalse(rightEnum.MoveNext());
            }
        }

        /// <summary>
        /// Checks that top-level statistics on <see cref="Fields"/> are the same.
        /// </summary>
        public void AssertFieldStatisticsEquals(string info, Fields leftFields, Fields rightFields)
        {
            if (leftFields.Count != -1 && rightFields.Count != -1)
            {
                Assert.AreEqual(leftFields.Count, rightFields.Count, info);
            }
        }

        /// <summary>
        /// <see cref="Terms"/> API equivalency.
        /// </summary>
        public void AssertTermsEquals(string info, IndexReader leftReader, Terms leftTerms, Terms rightTerms, bool deep)
        {
            if (leftTerms == null || rightTerms == null)
            {
                Assert.IsNull(leftTerms, info);
                Assert.IsNull(rightTerms, info);
                return;
            }
            AssertTermsStatisticsEquals(info, leftTerms, rightTerms);
            Assert.AreEqual(leftTerms.HasOffsets, rightTerms.HasOffsets);
            Assert.AreEqual(leftTerms.HasPositions, rightTerms.HasPositions);
            Assert.AreEqual(leftTerms.HasPayloads, rightTerms.HasPayloads);

            TermsEnum leftTermsEnum = leftTerms.GetIterator(null);
            TermsEnum rightTermsEnum = rightTerms.GetIterator(null);
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
        public void AssertTermsStatisticsEquals(string info, Terms leftTerms, Terms rightTerms)
        {
            Debug.Assert(leftTerms.Comparer == rightTerms.Comparer);
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
            {
                return bits.Get(index);
            }

            public int Length
            {
                get { return bits.Length; }
            }
        }

        /// <summary>
        /// Checks the terms enum sequentially.
        /// If <paramref name="deep"/> is false, it does a 'shallow' test that doesnt go down to the docsenums.
        /// </summary>
        public void AssertTermsEnumEquals(string info, IndexReader leftReader, TermsEnum leftTermsEnum, TermsEnum rightTermsEnum, bool deep)
        {
            BytesRef term;
            IBits randomBits = new RandomBits(leftReader.MaxDoc, Random.NextDouble(), Random);
            DocsAndPositionsEnum leftPositions = null;
            DocsAndPositionsEnum rightPositions = null;
            DocsEnum leftDocs = null;
            DocsEnum rightDocs = null;

            while ((term = leftTermsEnum.Next()) != null)
            {
                Assert.AreEqual(term, rightTermsEnum.Next(), info);
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
            Assert.IsNull(rightTermsEnum.Next(), info);
        }

        /// <summary>
        /// Checks docs + freqs + positions + payloads, sequentially.
        /// </summary>
        public void AssertDocsAndPositionsEnumEquals(string info, DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
        {
            if (leftDocs == null || rightDocs == null)
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
        public void AssertDocsEnumEquals(string info, DocsEnum leftDocs, DocsEnum rightDocs, bool hasFreqs)
        {
            if (leftDocs == null)
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
        public void AssertDocsSkippingEquals(string info, IndexReader leftReader, int docFreq, DocsEnum leftDocs, DocsEnum rightDocs, bool hasFreqs)
        {
            if (leftDocs == null)
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
                    int skip = docid + (int)Math.Ceiling(Math.Abs(skipInterval + Random.NextDouble() * averageGap));
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
        public void AssertPositionsSkippingEquals(string info, IndexReader leftReader, int docFreq, DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
        {
            if (leftDocs == null || rightDocs == null)
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
                    int skip = docid + (int)Math.Ceiling(Math.Abs(skipInterval + Random.NextDouble() * averageGap));
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
            HashSet<BytesRef> tests = new HashSet<BytesRef>();
            int numPasses = 0;
            while (numPasses < 10 && tests.Count < numTests)
            {
                leftEnum = leftTerms.GetIterator(leftEnum);
                BytesRef term = null;
                while ((term = leftEnum.Next()) != null)
                {
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
                        Array.Copy(term.Bytes, term.Offset, newbytes, 5, term.Length);
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
                                throw new InvalidOperationException();
                        }
                    }
                }
                numPasses++;
            }

            rightEnum = rightTerms.GetIterator(rightEnum);

            IList<BytesRef> shuffledTests = new List<BytesRef>(tests);
            Collections.Shuffle(shuffledTests);

            foreach (BytesRef b in shuffledTests)
            {
                if (Rarely())
                {
                    // reuse the enums
                    leftEnum = leftTerms.GetIterator(leftEnum);
                    rightEnum = rightTerms.GetIterator(rightEnum);
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
        public void AssertTermStatsEquals(string info, TermsEnum leftTermsEnum, TermsEnum rightTermsEnum)
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
        public void AssertNormsEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            Fields leftFields = MultiFields.GetFields(leftReader);
            Fields rightFields = MultiFields.GetFields(rightReader);
            // Fields could be null if there are no postings,
            // but then it must be null for both
            if (leftFields == null || rightFields == null)
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
        public void AssertStoredFieldsEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            Debug.Assert(leftReader.MaxDoc == rightReader.MaxDoc);
            for (int i = 0; i < leftReader.MaxDoc; i++)
            {
                Document leftDoc = leftReader.Document(i);
                Document rightDoc = rightReader.Document(i);

                // TODO: I think this is bogus because we don't document what the order should be
                // from these iterators, etc. I think the codec/IndexReader should be free to order this stuff
                // in whatever way it wants (e.g. maybe it packs related fields together or something)
                // To fix this, we sort the fields in both documents by name, but
                // we still assume that all instances with same name are in order:
                Comparison<IIndexableField> comp = (a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal);
                leftDoc.Fields.Sort(comp);
                rightDoc.Fields.Sort(comp);

                using (var leftIterator = leftDoc.GetEnumerator())
                using (var rightIterator = rightDoc.GetEnumerator())
                {
                    while (leftIterator.MoveNext())
                    {
                        Assert.IsTrue(rightIterator.MoveNext(), info);
                        AssertStoredFieldEquals(info, leftIterator.Current, rightIterator.Current);
                    }
                    Assert.IsFalse(rightIterator.MoveNext(), info);
                }
            }
        }

        /// <summary>
        /// Checks that two stored fields are equivalent.
        /// </summary>
        public void AssertStoredFieldEquals(string info, IIndexableField leftField, IIndexableField rightField)
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
        public void AssertTermVectorsEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            Debug.Assert(leftReader.MaxDoc == rightReader.MaxDoc);
            for (int i = 0; i < leftReader.MaxDoc; i++)
            {
                Fields leftFields = leftReader.GetTermVectors(i);
                Fields rightFields = rightReader.GetTermVectors(i);
                AssertFieldsEquals(info, leftReader, leftFields, rightFields, Rarely());
            }
        }

        private static ISet<string> GetDVFields(IndexReader reader)
        {
            HashSet<string> fields = new HashSet<string>();
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
        public void AssertDocValuesEquals(string info, IndexReader leftReader, IndexReader rightReader)
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

        public void AssertDocValuesEquals(string info, int num, NumericDocValues leftDocValues, NumericDocValues rightDocValues)
        {
            Assert.IsNotNull(leftDocValues, info);
            Assert.IsNotNull(rightDocValues, info);
            for (int docID = 0; docID < num; docID++)
            {
                Assert.AreEqual(leftDocValues.Get(docID), rightDocValues.Get(docID));
            }
        }

        // TODO: this is kinda stupid, we don't delete documents in the test.
        public void AssertDeletedDocsEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            Debug.Assert(leftReader.NumDeletedDocs == rightReader.NumDeletedDocs);
            IBits leftBits = MultiFields.GetLiveDocs(leftReader);
            IBits rightBits = MultiFields.GetLiveDocs(rightReader);

            if (leftBits == null || rightBits == null)
            {
                Assert.IsNull(leftBits, info);
                Assert.IsNull(rightBits, info);
                return;
            }

            Debug.Assert(leftReader.MaxDoc == rightReader.MaxDoc);
            Assert.AreEqual(leftBits.Length, rightBits.Length, info);
            for (int i = 0; i < leftReader.MaxDoc; i++)
            {
                Assert.AreEqual(leftBits.Get(i), rightBits.Get(i), info);
            }
        }

        public void AssertFieldInfosEquals(string info, IndexReader leftReader, IndexReader rightReader)
        {
            FieldInfos leftInfos = MultiFields.GetMergedFieldInfos(leftReader);
            FieldInfos rightInfos = MultiFields.GetMergedFieldInfos(rightReader);

            // TODO: would be great to verify more than just the names of the fields!
            SortedSet<string> left = new SortedSet<string>(StringComparer.Ordinal);
            SortedSet<string> right = new SortedSet<string>(StringComparer.Ordinal);

            foreach (FieldInfo fi in leftInfos)
            {
                left.Add(fi.Name);
            }

            foreach (FieldInfo fi in rightInfos)
            {
                right.Add(fi.Name);
            }

            Assert.AreEqual(left, right, info);
        }

        /// <summary>
        /// Returns true if the file exists (can be opened), false
        /// if it cannot be opened, and (unlike .NET's 
        /// <see cref="System.IO.File.Exists(string)"/>) if there's some
        /// unexpected error.
        /// </summary>
        public static bool SlowFileExists(Directory dir, string fileName)
        {
            try
            {
                dir.OpenInput(fileName, IOContext.DEFAULT).Dispose();
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            // LUCENENET specific - .NET (thankfully) only has one FileNotFoundException, so we don't need this
            //catch (NoSuchFileException)
            //{
            //    return false;
            //}
            // LUCENENET specific - since NoSuchDirectoryException subclasses FileNotFoundException
            // in Lucene, we need to catch it here to be on the safe side.
            catch (System.IO.DirectoryNotFoundException)
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
        private static int TEMP_NAME_RETRY_THRESHOLD = 9999;

        // LUCENENET: Not Implemented
        /////// <summary>
        /////// this method is deprecated for a reason. Do not use it. Call <seealso cref="#createTempDir()"/>
        /////// or <seealso cref="#createTempDir(String)"/> or <seealso cref="#createTempFile(String, String)"/>.
        /////// </summary>
        /////*[Obsolete]
        ////public static DirectoryInfo BaseTempDirForTestClass()
        ////{
        ////    lock (typeof(LuceneTestCase))
        ////    {
        ////        if (TempDirBase == null)
        ////        {
        ////            DirectoryInfo directory = new DirectoryInfo(System.IO.Path.GetTempPath());
        ////            //Debug.Assert(directory.Exists && directory.Directory != null && directory.CanWrite());

        ////            RandomizedContext ctx = RandomizedContext.Current;
        ////            Type clazz = ctx.GetTargetType;
        ////            string prefix = clazz.Name;
        ////            prefix = prefix.replaceFirst("^org.apache.lucene.", "lucene.");
        ////            prefix = prefix.replaceFirst("^org.apache.solr.", "solr.");

        ////            int attempt = 0;
        ////            DirectoryInfo f;
        ////            bool iterate = true;
        ////            do
        ////            {
        ////                if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
        ////                {
        ////                    throw new Exception("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: " + directory.FullName);
        ////                }
        ////                f = new DirectoryInfo(Path.Combine(directory.FullName, prefix + "-" + ctx.RunnerSeed + "-" + string.Format(CultureInfo.InvariantCulture, "%03d", attempt)));

        ////                try
        ////                {
        ////                    f.Create();
        ////                }
        ////                catch (IOException)
        ////                {
        ////                    iterate = false;
        ////                }
        ////            } while (iterate);

        ////            TempDirBase = f;
        ////            RegisterToRemoveAfterSuite(TempDirBase);
        ////        }
        ////    }
        ////    return TempDirBase;
        ////}*/

        /// <summary>
        /// Creates an empty, temporary folder (when the name of the folder is of no importance).
        /// </summary>
        /// <seealso cref="CreateTempDir(string)"/>
        public static DirectoryInfo CreateTempDir()
        {
            return CreateTempDir("tempDir");
        }

        /// <summary>
        /// Creates an empty, temporary folder with the given name prefix under the
        /// system's <see cref="Path.GetTempPath()"/>.
        /// 
        /// <para/>The folder will be automatically removed after the
        /// test class completes successfully. The test should close any file handles that would prevent
        /// the folder from being removed.
        /// </summary>
        public static DirectoryInfo CreateTempDir(string prefix)
        {
            //DirectoryInfo @base = BaseTempDirForTestClass();

            int attempt = 0;
            DirectoryInfo f;
            bool iterate = true;
            do
            {
                if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
                {
                    throw new Exception("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: " + System.IO.Path.GetTempPath());
                }
                // LUCENENET specific - need to use a random file name instead of a sequential one or two threads may attempt to do 
                // two operations on a file at the same time.
                //f = new DirectoryInfo(Path.Combine(System.IO.Path.GetTempPath(), "LuceneTemp", prefix + "-" + attempt));
                f = new DirectoryInfo(Path.Combine(System.IO.Path.GetTempPath(), "LuceneTemp", prefix + "-" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));

                try
                {
                    if (!System.IO.Directory.Exists(f.FullName))
                    {
                        f.Create();
                        iterate = false;
                    }
                }
#pragma warning disable 168
                catch (IOException exc)
#pragma warning restore 168
                {
                    iterate = true;
                }
            } while (iterate);

            RegisterToRemoveAfterSuite(f);
            return f;
        }

        /// <summary>
        /// Creates an empty file with the given prefix and suffix under the
        /// system's <see cref="Path.GetTempPath()"/>.
        ///
        /// <para/>The file will be automatically removed after the
        /// test class completes successfully. The test should close any file handles that would prevent
        /// the folder from being removed.
        /// </summary>
        public static FileInfo CreateTempFile(string prefix, string suffix)
        {
            //DirectoryInfo @base = BaseTempDirForTestClass();

            //int attempt = 0;
            FileInfo f = FileSupport.CreateTempFile(prefix, suffix, new DirectoryInfo(System.IO.Path.GetTempPath()));
            //do
            //{
            //    if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
            //    {
            //        throw new Exception("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: " + System.IO.Path.GetTempPath());
            //    }
            //    //f = new FileInfo(Path.Combine(System.IO.Path.GetTempPath(), prefix + "-" + string.Format(CultureInfo.InvariantCulture, "{0:D3}", attempt) + suffix));
            //    f = FileSupport.CreateTempFile(prefix, suffix, new DirectoryInfo(System.IO.Path.GetTempPath()));
            //} while (f.Create() == null);

            RegisterToRemoveAfterSuite(f);
            return f;
        }

        /// <summary>
        /// Creates an empty temporary file.
        /// </summary>
        /// <seealso cref="CreateTempFile(String, String)"/>
        public static FileInfo CreateTempFile()
        {
            return CreateTempFile("tempFile", ".tmp");
        }

        /// <summary>
        /// A queue of temporary resources to be removed after the
        /// suite completes. </summary>
        /// <seealso cref="RegisterToRemoveAfterSuite(FileSystemInfo)"/>
        private static readonly ConcurrentQueue<string> cleanupQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// Register temporary folder for removal after the suite completes.
        /// </summary>
        private static void RegisterToRemoveAfterSuite(FileSystemInfo f)
        {
            Debug.Assert(f != null);

            if (LuceneTestCase.LEAVE_TEMPORARY)
            {
                Console.Error.WriteLine("INFO: Will leave temporary file: " + f.FullName);
                return;
            }

            cleanupQueue.Enqueue(f.FullName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected string GetFullMethodName([CallerMemberName] string memberName = "")
        {
            return string.Format("{0}+{1}", this.GetType().Name, memberName);
        }

        private void CleanupTemporaryFiles()
        {
            // Drain cleanup queue and clear it.
            var tempDirBasePath = (tempDirBase != null ? tempDirBase.FullName : null);
            tempDirBase = null;

            // Only check and throw an IOException on un-removable files if the test
            // was successful. Otherwise just report the path of temporary files
            // and leave them there.
            if (LuceneTestCase.SuiteFailureMarker /*.WasSuccessful()*/)
            {
                string f;
                while (cleanupQueue.TryDequeue(out f))
                {
                    try
                    {
                        if (System.IO.Directory.Exists(f))
                            System.IO.Directory.Delete(f, true);
                        else if (System.IO.File.Exists(f))
                            File.Delete(f);
                    }
                    // LUCENENET specific: UnauthorizedAccessException doesn't subclass IOException as
                    // AccessDeniedException does in Java, so we need a special case for it.
                    catch (UnauthorizedAccessException e)
                    {
                        //                    Type suiteClass = RandomizedContext.Current.GetTargetType;
                        //                    if (suiteClass.IsAnnotationPresent(typeof(SuppressTempFileChecks)))
                        //                    {
                        Console.Error.WriteLine("WARNING: Leftover undeleted temporary files " + e.Message);
                        return;
                        //                    }
                    }
                    catch (IOException e)
                    {
                        //                    Type suiteClass = RandomizedContext.Current.GetTargetType;
                        //                    if (suiteClass.IsAnnotationPresent(typeof(SuppressTempFileChecks)))
                        //                    {
                        Console.Error.WriteLine("WARNING: Leftover undeleted temporary files " + e.Message);
                        return;
                        //                    }
                    }
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

        /// <summary>
        /// Contains a list of all the Func&lt;IConcurrentMergeSchedulers&gt; to be tested.
        /// Delegate method allows them to be created on their target thread instead of the test thread
        /// and also ensures a separate instance is created in each case (which can affect the result of the test).
        /// <para/>
        /// LUCENENET specific for injection into tests (i.e. using NUnit.Framework.ValueSourceAttribute)
        /// </summary>
        public static class ConcurrentMergeSchedulerFactories
        {
            public static readonly Func<IConcurrentMergeScheduler>[] Values = new Func<IConcurrentMergeScheduler>[] {
#if FEATURE_CONCURRENTMERGESCHEDULER
                () => new ConcurrentMergeScheduler(),
#endif
                () => new TaskMergeScheduler()
            };
        }
    }

    //internal class ReaderClosedListenerAnonymousInnerClassHelper : IndexReader.IReaderClosedListener
    //{
    //    private TaskScheduler ex;

    //    public ReaderClosedListenerAnonymousInnerClassHelper(TaskScheduler ex)
    //    {
    //        this.ex = ex;
    //    }

    //    public void OnClose(IndexReader reader)
    //    {
    //        TestUtil.ShutdownExecutorService(ex);
    //    }
    //}
}