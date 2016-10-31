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

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Reflection;

namespace Lucene.Net.Util
{
    using Lucene.Net.TestFramework.Support;
    using Support.Configuration;
    using System.IO;
    using System.Reflection;
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
    using FCInvisibleMultiReader = Lucene.Net.Search.QueryUtils.FCInvisibleMultiReader;
    using Field = Field;
    using FieldFilterAtomicReader = Lucene.Net.Index.FieldFilterAtomicReader;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using Fields = Lucene.Net.Index.Fields;
    using FieldType = FieldType;
    using FlushInfo = Lucene.Net.Store.FlushInfo;
    using FSDirectory = Lucene.Net.Store.FSDirectory;
    using IndexableField = Lucene.Net.Index.IndexableField;
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
    using Analysis;
    using Search.Similarities;

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
    /// Base class for all Lucene unit tests, Junit3 or Junit4 variant.
    ///
    /// <h3>Class and instance setup.</h3>
    ///
    /// <p>
    /// The preferred way to specify class (suite-level) setup/cleanup is to use
    /// static methods annotated with <seealso cref="BeforeClass"/> and <seealso cref="AfterClass"/>. Any
    /// code in these methods is executed within the test framework's control and
    /// ensure proper setup has been made. <b>Try not to use static initializers
    /// (including complex final field initializers).</b> Static initializers are
    /// executed before any setup rules are fired and may cause you (or somebody
    /// else) headaches.
    ///
    /// <p>
    /// For instance-level setup, use <seealso cref="Before"/> and <seealso cref="After"/> annotated
    /// methods. If you override either <seealso cref="#setUp()"/> or <seealso cref="#tearDown()"/> in
    /// your subclass, make sure you call <code>super.setUp()</code> and
    /// <code>super.tearDown()</code>. this is detected and enforced.
    ///
    /// <h3>Specifying test cases</h3>
    ///
    /// <p>
    /// Any test method with a <code>testXXX</code> prefix is considered a test case.
    /// Any test method annotated with <seealso cref="Test"/> is considered a test case.
    ///
    /// <h3>Randomized execution and test facilities</h3>
    ///
    /// <p>
    /// <seealso cref="LuceneTestCase"/> uses <seealso cref="RandomizedRunner"/> to execute test cases.
    /// <seealso cref="RandomizedRunner"/> has built-in support for tests randomization
    /// including access to a repeatable <seealso cref="Random"/> instance. See
    /// <seealso cref="#random()"/> method. Any test using <seealso cref="Random"/> acquired from
    /// <seealso cref="#random()"/> should be fully reproducible (assuming no race conditions
    /// between threads etc.). The initial seed for a test case is reported in many
    /// ways:
    /// <ul>
    ///   <li>as part of any exception thrown from its body (inserted as a dummy stack
    ///   trace entry),</li>
    ///   <li>as part of the main thread executing the test case (if your test hangs,
    ///   just dump the stack trace of all threads and you'll see the seed),</li>
    ///   <li>the master seed can also be accessed manually by getting the current
    ///   context (<seealso cref="RandomizedContext#current()"/>) and then calling
    ///   <seealso cref="RandomizedContext#getRunnerSeedAsString()"/>.</li>
    /// </ul>
    /// </summary>
    [TestFixture]
    public abstract partial class LuceneTestCase : Assert // Wait long for leaked threads to complete before failure. zk needs this. -  See LUCENE-3995 for rationale.
    {
        public static System.IO.FileInfo TEMP_DIR;

        public LuceneTestCase()
        {
            OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;
            ClassEnvRule = new TestRuleSetupAndRestoreClassEnv();
            String directory = Paths.TempDirectory;
            TEMP_DIR = new System.IO.FileInfo(directory);
        }

        // --------------------------------------------------------------------
        // Test groups, system properties and other annotations modifying tests
        // --------------------------------------------------------------------

        public const string SYSPROP_NIGHTLY = "tests.nightly";
        public const string SYSPROP_WEEKLY = "tests.weekly";
        public const string SYSPROP_AWAITSFIX = "tests.awaitsfix";
        public const string SYSPROP_SLOW = "tests.slow";
        public const string SYSPROP_BADAPPLES = "tests.badapples";

        /// <seealso> cref= #ignoreAfterMaxFailures </seealso>
        public const string SYSPROP_MAXFAILURES = "tests.maxfailures";

        /// <seealso> cref= #ignoreAfterMaxFailures </seealso>
        public const string SYSPROP_FAILFAST = "tests.failfast";

        /*     

      /// <summary>
      /// Annotation for tests that are slow. Slow tests do run by default but can be
      /// disabled if a quick run is needed.
      /// </summary>
      public class Slow : System.Attribute
      /// <summary>
      /// Annotation for tests that fail frequently and should
      /// be moved to a <a href="https://builds.apache.org/job/Lucene-BadApples-trunk-java7/">"vault" plan in Jenkins</a>.
      ///
      /// Tests annotated with this will be turned off by default. If you want to enable
      /// them, set:
      /// <pre>
      /// -Dtests.badapples=true
      /// </pre>
      /// </summary>
      {
      }
      public class BadApple : System.Attribute
      {
        /// <summary>
        /// Point to JIRA entry. </summary>
        public string bugUrl();
      }*/

        /// <summary>
        /// Annotation for test classes that should avoid certain codec types
        /// (because they are expensive, for example).
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        public class SuppressCodecs : System.Attribute
        {
            public SuppressCodecs(params string[] value)
            {
                this.Value = value;
            }
            public string[] Value { get; private set; }
        }

        /// <summary>
        /// Marks any suites which are known not to close all the temporary
        /// files. this may prevent temp. files and folders from being cleaned
        /// up after the suite is completed.
        /// </summary>
        /// <seealso cref= LuceneTestCase#createTempDir() </seealso>
        /// <seealso cref= LuceneTestCase#createTempFile(String, String) </seealso>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        public class SuppressTempFileChecks : System.Attribute
        {
            /// <summary>
            /// Point to JIRA entry. </summary>
            public virtual string bugUrl()
            {
                return "None";
            }
        }

        // -----------------------------------------------------------------
        // Truly immutable fields and constants, initialized once and valid
        // for all suites ever since.
        // -----------------------------------------------------------------

        // :Post-Release-Update-Version.LUCENE_XY:
        /// <summary>
        /// Use this constant when creating Analyzers and any other version-dependent stuff.
        /// <p><b>NOTE:</b> Change this when development starts for new Lucene version:
        /// </summary>
        public static readonly LuceneVersion TEST_VERSION_CURRENT = LuceneVersion.LUCENE_48;

        /// <summary>
        /// True if and only if tests are run in verbose mode. If this flag is false
        /// tests are not expected to print any messages.
        /// </summary>
        public static readonly bool VERBOSE = RandomizedTest.SystemPropertyAsBoolean("tests.verbose",
#if DEBUG
 true
#else
            false
#endif
);

        /// <summary>
        /// TODO: javadoc? </summary>
        public static readonly bool INFOSTREAM = RandomizedTest.SystemPropertyAsBoolean("tests.infostream", VERBOSE);

        /// <summary>
        /// A random multiplier which you should use when writing random tests:
        /// multiply it by the number of iterations to scale your tests (for nightly builds).
        /// </summary>
        public static readonly int RANDOM_MULTIPLIER = RandomizedTest.SystemPropertyAsInt("tests.multiplier", 1);

        /// <summary>
        /// TODO: javadoc? </summary>
        public static readonly string DEFAULT_LINE_DOCS_FILE = "europarl.lines.txt.gz";

        /// <summary>
        /// TODO: javadoc? </summary>
        public static readonly string JENKINS_LARGE_LINE_DOCS_FILE = "enwiki.random.lines.txt";

        /// <summary>
        /// Gets the codec to run tests with. </summary>
        public static readonly string TEST_CODEC = Configuration.GetAppSetting("tests.codec", "random");

        /// <summary>
        /// Gets the postingsFormat to run tests with. </summary>
        public static readonly string TEST_POSTINGSFORMAT = Configuration.GetAppSetting("tests.postingsformat", "random");

        /// <summary>
        /// Gets the docValuesFormat to run tests with </summary>
        public static readonly string TEST_DOCVALUESFORMAT = Configuration.GetAppSetting("tests.docvaluesformat", "random");

        /// <summary>
        /// Gets the directory to run tests with </summary>
        public static readonly string TEST_DIRECTORY = Configuration.GetAppSetting("tests.directory", "random");

        /// <summary>
        /// the line file used by LineFileDocs </summary>
        public static readonly string TEST_LINE_DOCS_FILE = Configuration.GetAppSetting("tests.linedocsfile", DEFAULT_LINE_DOCS_FILE);

        /// <summary>
        /// Whether or not <seealso cref="Nightly"/> tests should run. </summary>
        public static readonly bool TEST_NIGHTLY = RandomizedTest.SystemPropertyAsBoolean(SYSPROP_NIGHTLY, false);

        /// <summary>
        /// Whether or not <seealso cref="Weekly"/> tests should run. </summary>
        public static readonly bool TEST_WEEKLY = RandomizedTest.SystemPropertyAsBoolean(SYSPROP_WEEKLY, false);

        /// <summary>
        /// Whether or not <seealso cref="AwaitsFix"/> tests should run. </summary>
        public static readonly bool TEST_AWAITSFIX = RandomizedTest.SystemPropertyAsBoolean(SYSPROP_AWAITSFIX, false);

        /// <summary>
        /// Whether or not <seealso cref="Slow"/> tests should run. </summary>
        public static readonly bool TEST_SLOW = RandomizedTest.SystemPropertyAsBoolean(SYSPROP_SLOW, false);

        /// <summary>
        /// Throttling, see <seealso cref="MockDirectoryWrapper#setThrottling(Throttling)"/>. </summary>
        public static readonly MockDirectoryWrapper.Throttling_e TEST_THROTTLING = TEST_NIGHTLY ? MockDirectoryWrapper.Throttling_e.SOMETIMES : MockDirectoryWrapper.Throttling_e.NEVER;

        /// <summary>
        /// Leave temporary files on disk, even on successful runs. </summary>
        public static readonly bool LEAVE_TEMPORARY;

        static LuceneTestCase()
        {
            bool defaultValue = false;
            foreach (string property in Arrays.AsList("tests.leaveTemporary", "tests.leavetemporary", "tests.leavetmpdir", "solr.test.leavetmpdir")) // Solr's legacy -  default -  lowercase -  ANT tasks's (junit4) flag.
            {
                defaultValue |= RandomizedTest.SystemPropertyAsBoolean(property, false);
            }
            LEAVE_TEMPORARY = defaultValue;
            CORE_DIRECTORIES = new List<string>(FS_DIRECTORIES);
            CORE_DIRECTORIES.Add("RAMDirectory");
            int maxFailures = RandomizedTest.SystemPropertyAsInt(SYSPROP_MAXFAILURES, int.MaxValue);
            bool failFast = RandomizedTest.SystemPropertyAsBoolean(SYSPROP_FAILFAST, false);

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

            AppSettings.Set("tests.seed", Random().NextLong().ToString());

            //IgnoreAfterMaxFailuresDelegate = new AtomicReference<TestRuleIgnoreAfterMaxFailures>(new TestRuleIgnoreAfterMaxFailures(maxFailures));
            //IgnoreAfterMaxFailures = TestRuleDelegate.Of(IgnoreAfterMaxFailuresDelegate);
        }

        /// <summary>
        /// These property keys will be ignored in verification of altered properties. </summary>
        /// <seealso> cref= SystemPropertiesInvariantRule </seealso>
        /// <seealso> cref= #ruleChain </seealso>
        /// <seealso> cref= #classRules </seealso>
        private static readonly string[] IGNORED_INVARIANT_PROPERTIES = { "user.timezone", "java.rmi.server.randomIDs" };

        /// <summary>
        /// Filesystem-based <seealso cref="Directory"/> implementations. </summary>
        private static readonly IList<string> FS_DIRECTORIES = Arrays.AsList("SimpleFSDirectory", "NIOFSDirectory", "MMapDirectory");

        /// <summary>
        /// All <seealso cref="Directory"/> implementations. </summary>
        private static readonly IList<string> CORE_DIRECTORIES;

        protected static readonly HashSet<string> DoesntSupportOffsets = new HashSet<string>(Arrays.AsList("Lucene3x", "MockFixedIntBlock", "MockVariableIntBlock", "MockSep", "MockRandom"));

        // -----------------------------------------------------------------
        // Fields initialized in class or instance rules.
        // -----------------------------------------------------------------

        /// <summary>
        /// When {@code true}, Codecs for old Lucene version will support writing
        /// indexes in that format. Defaults to {@code false}, can be disabled by
        /// specific tests on demand.
        ///
        /// @lucene.internal
        /// 
        /// LUCENENET specific
        /// Is non-static to remove inter-class dependencies on this variable
        /// </summary>
        public bool OLD_FORMAT_IMPERSONATION_IS_ACTIVE { get; protected set; }

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
        public Similarity Similarity { get { return ClassEnvRule.Similarity; } }

        /// <summary>
        /// Gets the Timezone from the Class Environment setup rule
        /// 
        /// LUCENENET specific
        /// Exposed because <see cref="TestRuleSetupAndRestoreClassEnv"/> is
        /// internal and this field is needed by other classes.
        /// </summary>
        public TimeZoneInfo TimeZone { get { return ClassEnvRule.TimeZone; } }

        // LUCENENET TODO
        /// <summary>
        /// Suite failure marker (any error in the test or suite scope).
        /// </summary>
        public static readonly /*TestRuleMarkFailure*/ bool SuiteFailureMarker = true; // Means: was successful

        /// <summary>
        /// Ignore tests after hitting a designated number of initial failures. this
        /// is truly a "static" global singleton since it needs to span the lifetime of all
        /// test classes running inside this JVM (it cannot be part of a class rule).
        ///
        /// <p>this poses some problems for the test framework's tests because these sometimes
        /// trigger intentional failures which add up to the global count. this field contains
        /// a (possibly) changing reference to <seealso cref="TestRuleIgnoreAfterMaxFailures"/> and we
        /// dispatch to its current value from the <seealso cref="#classRules"/> chain using <seealso cref="TestRuleDelegate"/>.
        /// </summary>
        //private static AtomicReference<TestRuleIgnoreAfterMaxFailures> IgnoreAfterMaxFailuresDelegate;

        //private static TestRule IgnoreAfterMaxFailures;

        /// <summary>
        /// Temporarily substitute the global <seealso cref="TestRuleIgnoreAfterMaxFailures"/>. See
        /// <seealso cref="#ignoreAfterMaxFailuresDelegate"/> for some explanation why this method
        /// is needed.
        /// </summary>
        /*public static TestRuleIgnoreAfterMaxFailures ReplaceMaxFailureRule(TestRuleIgnoreAfterMaxFailures newValue)
        {
          return IgnoreAfterMaxFailuresDelegate.GetAndSet(newValue);
        }*/

        /// <summary>
        /// Max 10mb of static data stored in a test suite class after the suite is complete.
        /// Prevents static data structures leaking and causing OOMs in subsequent tests.
        /// </summary>
        private static readonly long STATIC_LEAK_THRESHOLD = 10 * 1024 * 1024;

        /// <summary>
        /// By-name list of ignored types like loggers etc. </summary>
        //private static ISet<string> STATIC_LEAK_IGNORED_TYPES = new HashSet<string>(Arrays.AsList("org.slf4j.Logger", "org.apache.solr.SolrLogFormatter", typeof(EnumSet).Name));

        /// <summary>
        /// this controls how suite-level rules are nested. It is important that _all_ rules declared
        /// in <seealso cref="LuceneTestCase"/> are executed in proper order if they depend on each
        /// other.
        /// </summary>

        /* LUCENE TODO: WTF is this???
        public static TestRule ClassRules = RuleChain
          .outerRule(new TestRuleIgnoreTestSuites())
          .around(IgnoreAfterMaxFailures)
          .around(SuiteFailureMarker = new TestRuleMarkFailure())
          .around(new TestRuleAssertionsRequired())
          .around(new TemporaryFilesCleanupRule())
          .around(new StaticFieldsInvariantRule(STATIC_LEAK_THRESHOLD, true)
          {
              @Override
              protected bool accept(System.Reflection.FieldInfo field)
              {
                  if (STATIC_LEAK_IGNORED_TYPES.contains(field.Type.Name))
                  {
                      return false;
                  }
                  if (field.DeclaringClass == typeof(LuceneTestCase))
                  {
                      return false;
                  }
                  return base.accept(field);
              }})
              .around(new NoClassHooksShadowingRule())
              .around(new NoInstanceHooksOverridesRule()
              {
              @Override
              protected bool verify(Method key)
              {
                  string name = key.Name;
                  return !(name.Equals("SetUp") || name.Equals("TearDown"));
              }})
              .around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES))
              .around(ClassNameRule = new TestRuleStoreClassName())
              .around(ClassEnvRule = new TestRuleSetupAndRestoreClassEnv());
      */

        // Don't count known classes that consume memory once.
        // Don't count references from ourselves, we're top-level.

        // -----------------------------------------------------------------
        // Test level rules.
        // -----------------------------------------------------------------
        /// <summary>
        /// Enforces <seealso cref="#setUp()"/> and <seealso cref="#tearDown()"/> calls are chained. </summary>
        /*private TestRuleSetupTeardownChained ParentChainCallRule = new TestRuleSetupTeardownChained();

        /// <summary>
        /// Save test thread and name. </summary>
        private TestRuleThreadAndTestName ThreadAndTestNameRule = new TestRuleThreadAndTestName();

        /// <summary>
        /// Taint suite result with individual test failures. </summary>
        private TestRuleMarkFailure TestFailureMarker = new TestRuleMarkFailure(SuiteFailureMarker);*/

        /// <summary>
        /// this controls how individual test rules are nested. It is important that
        /// _all_ rules declared in <seealso cref="LuceneTestCase"/> are executed in proper order
        /// if they depend on each other.
        /// </summary>
        /* LUCENE TODO: more wtf
        public TestRule ruleChain = RuleChain
            .outerRule(TestFailureMarker)
            .around(IgnoreAfterMaxFailures)
            .around(ThreadAndTestNameRule)
            .around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES))
            .around(new TestRuleSetupAndRestoreInstanceEnv()).
            around(new TestRuleFieldCacheSanity()).
            around(ParentChainCallRule);
        */
        // -----------------------------------------------------------------
        // Suite and test case setup/ cleanup.
        // -----------------------------------------------------------------

        /// <summary>
        /// For subclasses to override. Overrides must call {@code super.setUp()}.
        /// </summary>
        [SetUp]
        public virtual void SetUp()
        {
            // LUCENENET TODO: Not sure how to convert these
            //ParentChainCallRule.SetupCalled = true;
        }

        /// <summary>
        /// For subclasses to override. Overrides must call {@code super.tearDown()}.
        /// </summary>
        [TearDown]
        public virtual void TearDown()
        {
            /* LUCENENET TODO: Not sure how to convert these
                ParentChainCallRule.TeardownCalled = true;
                */
            CleanupTemporaryFiles();
        }

        // -----------------------------------------------------------------
        // Test facilities and facades for subclasses.
        // -----------------------------------------------------------------

        /// <summary>
        /// Access to the current <seealso cref="RandomizedContext"/>'s Random instance. It is safe to use
        /// this method from multiple threads, etc., but it should be called while within a runner's
        /// scope (so no static initializers). The returned <seealso cref="Random"/> instance will be
        /// <b>different</b> when this method is called inside a <seealso cref="BeforeClass"/> hook (static
        /// suite scope) and within <seealso cref="Before"/>/ <seealso cref="After"/> hooks or test methods.
        ///
        /// <p>The returned instance must not be shared with other threads or cross a single scope's
        /// boundary. For example, a <seealso cref="Random"/> acquired within a test method shouldn't be reused
        /// for another test case.
        ///
        /// <p>There is an overhead connected with getting the <seealso cref="Random"/> for a particular context
        /// and thread. It is better to cache the <seealso cref="Random"/> locally if tight loops with multiple
        /// invocations are present or create a derivative local <seealso cref="Random"/> for millions of calls
        /// like this:
        /// <pre>
        /// Random random = new Random(random().nextLong());
        /// // tight loop with many invocations.
        /// </pre>
        /// </summary>
        public static Random Random()
        {
            return _random ?? (_random = new Random(/* LUCENENET TODO seed */));
            //return RandomizedContext.Current.Random;
        }

        protected static Random randon()
        {
            return Random();
        }

        [ThreadStatic]
        private static Random _random;

        /// <summary>
        /// Registers a <seealso cref="IDisposable"/> resource that should be closed after the test
        /// completes.
        /// </summary>
        /// <returns> <code>resource</code> (for call chaining). </returns>
        /*public static T CloseAfterTest<T>(T resource)
        {
            return RandomizedContext.Current.CloseAtEnd(resource, LifecycleScope.TEST);
        }*/

        /// <summary>
        /// Registers a <seealso cref="IDisposable"/> resource that should be closed after the suite
        /// completes.
        /// </summary>
        /// <returns> <code>resource</code> (for call chaining). </returns>
        /*public static T CloseAfterSuite<T>(T resource)
        {
            return RandomizedContext.Current.CloseAtEnd(resource, LifecycleScope.SUITE);
        }*/

        /// <summary>
        /// Return the current class being tested.
        /// </summary>
        public static Type TestClass
        {
            get
            {
                return typeof(LuceneTestCase);
            }
        }

        /// <summary>
        /// Return the name of the currently executing test case.
        /// </summary>
        public string TestName
        {
            get
            {
                //return ThreadAndTestNameRule.TestMethodName;
                return "LuceneTestCase";
            }
        }

        /// <summary>
        /// Some tests expect the directory to contain a single segment, and want to
        /// do tests on that segment's reader. this is an utility method to help them.
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
        /// </summary>
        public static bool TestThread()
        {
            /*Assert.IsNotNull(ThreadAndTestNameRule.TestCaseThread, "Test case thread not set?");
            return Thread.CurrentThread == ThreadAndTestNameRule.TestCaseThread;*/
            return true;
        }

        /// <summary>
        /// Asserts that FieldCacheSanityChecker does not detect any
        /// problems with FieldCache.DEFAULT.
        /// <p>
        /// If any problems are found, they are logged to System.err
        /// (allong with the msg) when the Assertion is thrown.
        /// </p>
        /// <p>
        /// this method is called by tearDown after every test method,
        /// however IndexReaders scoped inside test methods may be garbage
        /// collected prior to this method being called, causing errors to
        /// be overlooked. Tests are encouraged to keep their IndexReaders
        /// scoped at the class level, or to explicitly call this method
        /// directly in the same scope as the IndexReader.
        /// </p>
        /// </summary>
        /// <seealso cref= Lucene.Net.Util.FieldCacheSanityChecker </seealso>
        protected static void AssertSaneFieldCaches(string msg)
        {
            CacheEntry[] entries = FieldCache.DEFAULT.CacheEntries;
            Insanity[] insanity = null;
            try
            {
                try
                {
                    insanity = FieldCacheSanityChecker.CheckSanity(entries);
                }
                catch (Exception e)
                {
                    DumpArray(msg + ": FieldCache", entries, Console.Error);
                    throw e;
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
        /// Returns a number of at least <code>i</code>
        /// <p>
        /// The actual number returned will be influenced by whether <seealso cref="#TEST_NIGHTLY"/>
        /// is active and <seealso cref="#RANDOM_MULTIPLIER"/>, but also with some random fudge.
        /// </summary>
        public static int AtLeast(Random random, int i)
        {
            int min = (TEST_NIGHTLY ? 2 * i : i) * RANDOM_MULTIPLIER;
            int max = min + (min / 2);
            return TestUtil.NextInt(random, min, max);
        }

        public static int AtLeast(int i)
        {
            return AtLeast(Random(), i);
        }

        /// <summary>
        /// Returns true if something should happen rarely,
        /// <p>
        /// The actual number returned will be influenced by whether <seealso cref="#TEST_NIGHTLY"/>
        /// is active and <seealso cref="#RANDOM_MULTIPLIER"/>.
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
            return Rarely(Random());
        }

        public static bool Usually(Random random)
        {
            return !Rarely(random);
        }

        public static bool Usually()
        {
            return Usually(Random());
        }

        public static void AssumeTrue(string msg, bool condition)
        {
            RandomizedTest.AssumeTrue(msg, condition);
        }

        public static void AssumeFalse(string msg, bool condition)
        {
            RandomizedTest.AssumeFalse(msg, condition);
        }

        public static void AssumeNoException(string msg, Exception e)
        {
            //RandomizedTest.AssumeNoException(msg, e);
        }

        /// <summary>
        /// Return <code>args</code> as a <seealso cref="Set"/> instance. The order of elements is not
        /// preserved in iterators.
        /// </summary>
        public static ISet<object> AsSet(params object[] args)
        {
            return new HashSet<object>(Arrays.AsList(args));
        }

        /// <summary>
        /// Convenience method for logging an iterator.
        /// </summary>
        /// <param name="label">  String logged before/after the items in the iterator </param>
        /// <param name="iter">   Each next() is toString()ed and logged on it's own line. If iter is null this is logged differnetly then an empty iterator. </param>
        /// <param name="stream"> Stream to log messages to. </param>
        public static void DumpIterator(string label, System.Collections.IEnumerator iter, TextWriter stream)
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
                    stream.WriteLine(iter.Current.ToString());
                }
            }
            stream.WriteLine("*** END " + label + " ***");
        }

        /// <summary>
        /// Convenience method for logging an array.  Wraps the array in an iterator and delegates
        /// </summary>
        /// <seealso cref= #dumpIterator(String,Iterator,PrintStream) </seealso>
        public static void DumpArray(string label, Object[] objs, TextWriter stream)
        {
            System.Collections.IEnumerator iter = (null == objs) ? (System.Collections.IEnumerator)null : Arrays.AsList(objs).GetEnumerator();
            DumpIterator(label, iter, stream);
        }

        /// <summary>
        /// create a new index writer config with random defaults
        /// 
        /// LUCENENET specific
        /// Non-static so that we do not depend on any hidden static dependencies
        /// </summary>
        public IndexWriterConfig NewIndexWriterConfig(LuceneVersion v, Analyzer a)
        {
            return NewIndexWriterConfig(Random(), v, a);
        }

        /// <summary>
        /// LUCENENET specific
        /// Non-static so that we do not depend on any hidden static dependencies
        /// </summary>
        public IndexWriterConfig NewIndexWriterConfig(Random r, LuceneVersion v, Analyzer a)
        {
            return NewIndexWriterConfig(r, v, a, ClassEnvRule.Similarity, ClassEnvRule.TimeZone);
        }

        /// <summary>
        /// create a new index writer config with random defaults using the specified random
        /// 
        /// LUCENENET specific
        /// This is the only static ctor for IndexWriterConfig because it removes the dependency
        /// on ClassEnvRule by using parameters Similarity and TimeZone.
        /// </summary>
        public static IndexWriterConfig NewIndexWriterConfig(Random r, LuceneVersion v, Analyzer a, Similarity similarity, TimeZoneInfo timezone)
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
                c.InfoStream = new TestRuleSetupAndRestoreClassEnv.ThreadNameFixingPrintStreamInfoStream(Console.Out);
            }

            if (r.NextBoolean())
            {
                c.SetMergeScheduler(new SerialMergeScheduler());
            }
            else if (Rarely(r))
            {
                int maxThreadCount = TestUtil.NextInt(Random(), 1, 4);
                int maxMergeCount = TestUtil.NextInt(Random(), maxThreadCount, maxThreadCount + 4);
                IConcurrentMergeScheduler mergeScheduler;

#if NETSTANDARD
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
                    c.SetMaxBufferedDocs(TestUtil.NextInt(r, 2, 15));
                }
                else
                {
                    // reasonable value
                    c.SetMaxBufferedDocs(TestUtil.NextInt(r, 16, 1000));
                }
            }
            if (r.NextBoolean())
            {
                if (Rarely(r))
                {
                    // crazy value
                    c.SetTermIndexInterval(r.NextBoolean() ? TestUtil.NextInt(r, 1, 31) : TestUtil.NextInt(r, 129, 1000));
                }
                else
                {
                    // reasonable value
                    c.SetTermIndexInterval(TestUtil.NextInt(r, 32, 128));
                }
            }
            if (r.NextBoolean())
            {
                int maxNumThreadStates = Rarely(r) ? TestUtil.NextInt(r, 5, 20) : TestUtil.NextInt(r, 1, 4); // reasonable value -  crazy value

                if (Rarely(r))
                {
                    // Retrieve the package-private setIndexerThreadPool
                    // method:
                    MethodInfo setIndexerThreadPoolMethod = typeof(IndexWriterConfig).GetTypeInfo().GetMethod("SetIndexerThreadPool", new Type[] { typeof(DocumentsWriterPerThreadPool) });
                    //setIndexerThreadPoolMethod.setAccessible(true);
                    Type clazz = typeof(RandomDocumentsWriterPerThreadPool);
                    ConstructorInfo ctor = clazz.GetTypeInfo().GetConstructor(new[] { typeof(int), typeof(Random) });
                    //ctor.Accessible = true;
                    // random thread pool
                    setIndexerThreadPoolMethod.Invoke(c, new[] { ctor.Invoke(new object[] { maxNumThreadStates, r }) });
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
            c.SetReaderTermsIndexDivisor(TestUtil.NextInt(r, 1, 4));
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
            return NewMergePolicy(Random(), timezone);
        }

        public static LogMergePolicy NewLogMergePolicy()
        {
            return NewLogMergePolicy(Random());
        }

        public static TieredMergePolicy NewTieredMergePolicy()
        {
            return NewTieredMergePolicy(Random());
        }

        public AlcoholicMergePolicy NewAlcoholicMergePolicy()
        {
            return NewAlcoholicMergePolicy(Random());
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
                logmp.MergeFactor = TestUtil.NextInt(r, 2, 9);
            }
            else
            {
                logmp.MergeFactor = TestUtil.NextInt(r, 10, 50);
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
                tmp.MaxMergeAtOnce = TestUtil.NextInt(r, 2, 9);
                tmp.MaxMergeAtOnceExplicit = TestUtil.NextInt(r, 2, 9);
            }
            else
            {
                tmp.MaxMergeAtOnce = TestUtil.NextInt(r, 10, 50);
                tmp.MaxMergeAtOnceExplicit = TestUtil.NextInt(r, 10, 50);
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
                tmp.SegmentsPerTier = TestUtil.NextInt(r, 2, 20);
            }
            else
            {
                tmp.SegmentsPerTier = TestUtil.NextInt(r, 10, 50);
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
        /// Returns a new Directory instance. Use this when the test does not
        /// care about the specific Directory implementation (most tests).
        /// <p>
        /// The Directory is wrapped with <seealso cref="BaseDirectoryWrapper"/>.
        /// this means usually it will be picky, such as ensuring that you
        /// properly close it and all open files in your test. It will emulate
        /// some features of Windows, such as not allowing open files to be
        /// overwritten.
        /// </summary>
        public static BaseDirectoryWrapper NewDirectory()
        {
            return NewDirectory(Random());
        }

        /// <summary>
        /// Returns a new Directory instance, using the specified random.
        /// See <seealso cref="#newDirectory()"/> for more information.
        /// </summary>
        public static BaseDirectoryWrapper NewDirectory(Random r)
        {
            var newDir = NewDirectoryImpl(r, TEST_DIRECTORY);

            return WrapDirectory(r, newDir, Rarely(r));
        }

        public static MockDirectoryWrapper NewMockDirectory()
        {
            return NewMockDirectory(Random());
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
        /// Returns a new Directory instance, with contents copied from the
        /// provided directory. See <seealso cref="#newDirectory()"/> for more
        /// information.
        /// </summary>
        public static BaseDirectoryWrapper NewDirectory(Directory d)
        {
            return NewDirectory(Random(), d);
        }

        /// <summary>
        /// Returns a new FSDirectory instance over the given file, which must be a folder. </summary>
        public static BaseDirectoryWrapper NewFSDirectory(DirectoryInfo d)
        {
            return NewFSDirectory(d, null);
        }

        /// <summary>
        /// Returns a new FSDirectory instance over the given file, which must be a folder. </summary>
        public static BaseDirectoryWrapper NewFSDirectory(DirectoryInfo d, LockFactory lf)
        {
            return NewFSDirectory(d, lf, Rarely());
        }

        private static BaseDirectoryWrapper NewFSDirectory(DirectoryInfo d, LockFactory lf, bool bare)
        {
            string fsdirClass = TEST_DIRECTORY;
            if (fsdirClass.Equals("random"))
            {
                fsdirClass = RandomInts.RandomFrom(Random(), FS_DIRECTORIES);
            }

            Type clazz;
            try
            {
                clazz = CommandLineUtil.LoadFSDirectoryClass(fsdirClass);
            }
            catch (System.InvalidCastException e)
            {
                // TEST_DIRECTORY is not a sub-class of FSDirectory, so draw one at random
                fsdirClass = RandomInts.RandomFrom(Random(), FS_DIRECTORIES);
                clazz = CommandLineUtil.LoadFSDirectoryClass(fsdirClass);
            }

            Directory fsdir = NewFSDirectoryImpl(clazz, d);
            BaseDirectoryWrapper wrapped = WrapDirectory(Random(), fsdir, bare);
            if (lf != null)
            {
                wrapped.LockFactory = lf;
            }
            return wrapped;
        }

        /// <summary>
        /// Returns a new Directory instance, using the specified random
        /// with contents copied from the provided directory. See
        /// <seealso cref="#newDirectory()"/> for more information.
        /// </summary>
        public static BaseDirectoryWrapper NewDirectory(Random r, Directory d)
        {
            Directory impl = NewDirectoryImpl(r, TEST_DIRECTORY);
            foreach (string file in d.ListAll())
            {
                d.Copy(impl, file, file, NewIOContext(r));
            }
            return WrapDirectory(r, impl, Rarely(r));
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

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because <see cref="OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/>
        /// is now non-static.
        /// </summary>
        public Field NewStringField(string name, string value, Field.Store stored)
        {
            return NewField(Random(), name, value, stored == Field.Store.YES ? StringField.TYPE_STORED : StringField.TYPE_NOT_STORED);
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because <see cref="OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/>
        /// is now non-static.
        /// </summary>
        public Field NewTextField(string name, string value, Field.Store stored)
        {
            return NewField(Random(), name, value, stored == Field.Store.YES ? TextField.TYPE_STORED : TextField.TYPE_NOT_STORED);
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because <see cref="OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/>
        /// is now non-static.
        /// </summary>
        public Field NewStringField(Random random, string name, string value, Field.Store stored)
        {
            return NewField(random, name, value, stored == Field.Store.YES ? StringField.TYPE_STORED : StringField.TYPE_NOT_STORED);
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because <see cref="OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/>
        /// is also non-static to reduce hidden dependencies on this variable.
        /// </summary>
        public Field NewTextField(Random random, string name, string value, Field.Store stored)
        {
            return NewField(random, name, value, stored == Field.Store.YES ? TextField.TYPE_STORED : TextField.TYPE_NOT_STORED);
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because <see cref="OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/>
        /// is now non-static.
        /// </summary>
        public Field NewField(string name, string value, FieldType type)
        {
            return NewField(Random(), name, value, type);
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because <see cref="OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/>
        /// is now non-static.
        /// </summary>
        public Field NewField(Random random, string name, string value, FieldType type)
        {
            name = new string(name.ToCharArray());
            if (Usually(random) || !type.Indexed)
            {
                // most of the time, don't modify the params
                return new Field(name, value, type);
            }

            // TODO: once all core & test codecs can index
            // offsets, sometimes randomly turn on offsets if we are
            // already indexing positions...

            FieldType newType = new FieldType(type);
            if (!newType.Stored && random.NextBoolean())
            {
                newType.Stored = true; // randomly store it
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
		        if (newType.OmitsNorms()) {
		          newType.setOmitNorms(random.NextBoolean());
		        }
		    */

            return new Field(name, value, newType);
        }

        /* LUCENE TODO: removing until use is shown

            /// <summary>
            /// Return a random Locale from the available locales on the system. </summary>
            /// <seealso cref= "https://issues.apache.org/jira/browse/LUCENE-4020" </seealso>
            public static CultureInfo RandomLocale(Random random)
            {
                CultureInfo[] locales = CultureInfo.GetCultures();
                return locales[random.Next(locales.Length)];
            }
            /// <summary>
            /// Return a random TimeZone from the available timezones on the system </summary>
            /// <seealso cref= "https://issues.apache.org/jira/browse/LUCENE-4020"  </seealso>
            public static TimeZone RandomTimeZone(Random random)
            {
                string[] tzIds = TimeZone.AvailableIDs;
                return TimeZone.getTimeZone(tzIds[random.Next(tzIds.Length)]);
            }

            /// <summary>
            /// return a Locale object equivalent to its programmatic name </summary>
            public static Locale LocaleForName(string localeName)
            {
                string[] elements = localeName.Split("\\_");
                switch (elements.Length)
                {
                    case 4: // fallthrough for special cases
                    case 3:
                    return new Locale(elements[0], elements[1], elements[2]);

                    case 2:
                    return new Locale(elements[0], elements[1]);

                    case 1:
                    return new Locale(elements[0]);

                    default:
                    throw new System.ArgumentException("Invalid Locale: " + localeName);
                }
            }*/

        public static bool DefaultCodecSupportsDocValues()
        {
            return !Codec.Default.Name.Equals("Lucene3x");
        }

        private static Directory NewFSDirectoryImpl(Type clazz, DirectoryInfo file)
        {
            return CommandLineUtil.NewFSDirectory(clazz, file);
        }

        private static Directory NewDirectoryImpl(Random random, string clazzName)
        {
            if (clazzName.Equals("random"))
            {
                if (Rarely(random))
                {
                    clazzName = RandomInts.RandomFrom(random, CORE_DIRECTORIES);
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
            // If it is a FSDirectory type, try its ctor(File)
            if (clazz.GetTypeInfo().IsSubclassOf(typeof(FSDirectory)))
            {
                DirectoryInfo dir = CreateTempDir("index-" + clazzName);
                dir.Create(); // ensure it's created so we 'have' it.
                return NewFSDirectoryImpl(clazz, dir);
            }

            // try empty ctor
            return (Directory)Activator.CreateInstance(clazz);
        }

        /// <summary>
        /// Sometimes wrap the IndexReader as slow, parallel or filter reader (or
        /// combinations of that)
        /// </summary>
        public static IndexReader MaybeWrapReader(IndexReader r)
        {
            Random random = Random();
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
                            allFields = CollectionsHelper.Shuffle(allFields);
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
                return new IOContext(new MergeInfo(randomNumDocs, Math.Max(oldContext.MergeInfo.EstimatedMergeBytes, size), random.NextBoolean(), TestUtil.NextInt(random, 1, 100)));
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
                        context = IOContext.READONCE;
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
            return NewSearcher(r, ClassEnvRule.Similarity);
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

        public IndexSearcher NewSearcher(IndexReader r, bool maybeWrap, bool wrapWithAssertions)
        {
            return NewSearcher(r, maybeWrap, wrapWithAssertions, ClassEnvRule.Similarity);
        }

        /// <summary>
        /// Create a new searcher over the reader. this searcher might randomly use
        /// threads. if <code>maybeWrap</code> is true, this searcher might wrap the
        /// reader with one that returns null for getSequentialSubReaders. If
        /// <code>wrapWithAssertions</code> is true, this searcher might be an
        /// <seealso cref="AssertingIndexSearcher"/> instance.
        /// </summary>
        /// <param name="similarity">
        /// LUCENENET specific
        /// Removes dependency on <see cref="LuceneTestCase.ClassEnv.Similarity"/>
        /// </param>
        public static IndexSearcher NewSearcher(IndexReader r, bool maybeWrap, bool wrapWithAssertions, Similarity similarity)
        {
            Random random = Random();
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
                    threads = TestUtil.NextInt(random, 1, 8);
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
                    //r.AddReaderClosedListener(new ReaderClosedListenerAnonymousInnerClassHelper(ex));
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
        /// Gets a resource from the classpath as <seealso cref="File"/>. this method should only
        /// be used, if a real file is needed. To get a stream, code should prefer
        /// <seealso cref="Class#getResourceAsStream"/> using {@code this.getClass()}.
        /// </summary>
        protected Stream GetDataFile(string name)
        {
            try
            {
                var resourceLoader = new ClasspathResourceLoader(this.GetType(), "Lucene.Net");
                return resourceLoader.OpenResource(name);
            }
            catch (Exception e)
            {
                throw new IOException("Cannot find resource: " + name);
            }
        }

        /// <summary>
        /// Returns true if the default codec supports single valued docvalues with missing values </summary>
        public static bool DefaultCodecSupportsMissingDocValues()
        {
            string name = Codec.Default.Name;
            if (name.Equals("Lucene3x") || name.Equals("Lucene40") || name.Equals("Appending") || name.Equals("Lucene41") || name.Equals("Lucene42"))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns true if the default codec supports SORTED_SET docvalues </summary>
        public static bool DefaultCodecSupportsSortedSet()
        {
            if (!DefaultCodecSupportsDocValues())
            {
                return false;
            }
            string name = Codec.Default.Name;
            if (name.Equals("Lucene40") || name.Equals("Lucene41") || name.Equals("Appending"))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns true if the codec "supports" docsWithField
        /// (other codecs return MatchAllBits, because you couldnt write missing values before)
        /// </summary>
        public static bool DefaultCodecSupportsDocsWithField()
        {
            if (!DefaultCodecSupportsDocValues())
            {
                return false;
            }
            string name = Codec.Default.Name;
            if (name.Equals("Appending") || name.Equals("Lucene40") || name.Equals("Lucene41") || name.Equals("Lucene42"))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns true if the codec "supports" field updates. </summary>
        public static bool DefaultCodecSupportsFieldUpdates()
        {
            string name = Codec.Default.Name;
            if (name.Equals("Lucene3x") || name.Equals("Appending") || name.Equals("Lucene40") || name.Equals("Lucene41") || name.Equals("Lucene42") || name.Equals("Lucene45"))
            {
                return false;
            }
            return true;
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
        /// checks that reader-level statistics are the same
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
        /// Fields api equivalency
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

            IEnumerator<string> leftEnum = leftFields.GetEnumerator();
            IEnumerator<string> rightEnum = rightFields.GetEnumerator();

            while (leftEnum.MoveNext())
            {
                string field = leftEnum.Current;
                rightEnum.MoveNext();
                Assert.AreEqual(field, rightEnum.Current, info);
                AssertTermsEquals(info, leftReader, leftFields.Terms(field), rightFields.Terms(field), deep);
            }
            Assert.IsFalse(rightEnum.MoveNext());
        }

        /// <summary>
        /// checks that top-level statistics on Fields are the same
        /// </summary>
        public void AssertFieldStatisticsEquals(string info, Fields leftFields, Fields rightFields)
        {
            if (leftFields.Size != -1 && rightFields.Size != -1)
            {
                Assert.AreEqual(leftFields.Size, rightFields.Size, info);
            }
        }

        /// <summary>
        /// Terms api equivalency
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
            Assert.AreEqual(leftTerms.HasOffsets(), rightTerms.HasOffsets());
            Assert.AreEqual(leftTerms.HasPositions(), rightTerms.HasPositions());
            Assert.AreEqual(leftTerms.HasPayloads(), rightTerms.HasPayloads());

            TermsEnum leftTermsEnum = leftTerms.Iterator(null);
            TermsEnum rightTermsEnum = rightTerms.Iterator(null);
            AssertTermsEnumEquals(info, leftReader, leftTermsEnum, rightTermsEnum, true);

            AssertTermsSeekingEquals(info, leftTerms, rightTerms);

            if (deep)
            {
                int numIntersections = AtLeast(3);
                for (int i = 0; i < numIntersections; i++)
                {
                    string re = AutomatonTestUtil.RandomRegexp(Random());
                    CompiledAutomaton automaton = new CompiledAutomaton((new RegExp(re, RegExp.NONE)).ToAutomaton());
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
        /// checks collection-level statistics on Terms
        /// </summary>
        public void AssertTermsStatisticsEquals(string info, Terms leftTerms, Terms rightTerms)
        {
            Debug.Assert(leftTerms.Comparator == rightTerms.Comparator);
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
            if (leftTerms.Size() != -1 && rightTerms.Size() != -1)
            {
                Assert.AreEqual(leftTerms.Size(), rightTerms.Size(), info);
            }
        }

        internal class RandomBits : Bits
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

            public int Length()
            {
                return bits.Length();
            }
        }

        /// <summary>
        /// checks the terms enum sequentially
        /// if deep is false, it does a 'shallow' test that doesnt go down to the docsenums
        /// </summary>
        public void AssertTermsEnumEquals(string info, IndexReader leftReader, TermsEnum leftTermsEnum, TermsEnum rightTermsEnum, bool deep)
        {
            BytesRef term;
            Bits randomBits = new RandomBits(leftReader.MaxDoc, Random().NextDouble(), Random());
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

                    AssertPositionsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions));
                    AssertPositionsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions(randomBits, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions));

                    // with freqs:
                    AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(null, leftDocs), rightDocs = rightTermsEnum.Docs(null, rightDocs), true);
                    AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(randomBits, leftDocs), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs), true);

                    // w/o freqs:
                    AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(null, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(null, rightDocs, DocsEnum.FLAG_NONE), false);
                    AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(randomBits, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs, DocsEnum.FLAG_NONE), false);

                    // with freqs:
                    AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(null, leftDocs), rightDocs = rightTermsEnum.Docs(null, rightDocs), true);
                    AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(randomBits, leftDocs), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs), true);

                    // w/o freqs:
                    AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(null, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(null, rightDocs, DocsEnum.FLAG_NONE), false);
                    AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(randomBits, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs, DocsEnum.FLAG_NONE), false);
                }
            }
            Assert.IsNull(rightTermsEnum.Next(), info);
        }

        /// <summary>
        /// checks docs + freqs + positions + payloads, sequentially
        /// </summary>
        public void AssertDocsAndPositionsEnumEquals(string info, DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
        {
            if (leftDocs == null || rightDocs == null)
            {
                Assert.IsNull(leftDocs);
                Assert.IsNull(rightDocs);
                return;
            }
            Assert.AreEqual(-1, leftDocs.DocID(), info);
            Assert.AreEqual(-1, rightDocs.DocID(), info);
            int docid;
            while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                Assert.AreEqual(docid, rightDocs.NextDoc(), info);
                int freq = leftDocs.Freq();
                Assert.AreEqual(freq, rightDocs.Freq(), info);
                for (int i = 0; i < freq; i++)
                {
                    Assert.AreEqual(leftDocs.NextPosition(), rightDocs.NextPosition(), info);
                    Assert.AreEqual(leftDocs.Payload, rightDocs.Payload, info);
                    Assert.AreEqual(leftDocs.StartOffset(), rightDocs.StartOffset(), info);
                    Assert.AreEqual(leftDocs.EndOffset(), rightDocs.EndOffset(), info);
                }
            }
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc(), info);
        }

        /// <summary>
        /// checks docs + freqs, sequentially
        /// </summary>
        public void AssertDocsEnumEquals(string info, DocsEnum leftDocs, DocsEnum rightDocs, bool hasFreqs)
        {
            if (leftDocs == null)
            {
                Assert.IsNull(rightDocs);
                return;
            }
            Assert.AreEqual(-1, leftDocs.DocID(), info);
            Assert.AreEqual(-1, rightDocs.DocID(), info);
            int docid;
            while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                Assert.AreEqual(docid, rightDocs.NextDoc(), info);
                if (hasFreqs)
                {
                    Assert.AreEqual(leftDocs.Freq(), rightDocs.Freq(), info);
                }
            }
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc(), info);
        }

        /// <summary>
        /// checks advancing docs
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
                if (Random().NextBoolean())
                {
                    // nextDoc()
                    docid = leftDocs.NextDoc();
                    Assert.AreEqual(docid, rightDocs.NextDoc(), info);
                }
                else
                {
                    // advance()
                    int skip = docid + (int)Math.Ceiling(Math.Abs(skipInterval + Random().NextDouble() * averageGap));
                    docid = leftDocs.Advance(skip);
                    Assert.AreEqual(docid, rightDocs.Advance(skip), info);
                }

                if (docid == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return;
                }
                if (hasFreqs)
                {
                    Assert.AreEqual(leftDocs.Freq(), rightDocs.Freq(), info);
                }
            }
        }

        /// <summary>
        /// checks advancing docs + positions
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
                if (Random().NextBoolean())
                {
                    // nextDoc()
                    docid = leftDocs.NextDoc();
                    Assert.AreEqual(docid, rightDocs.NextDoc(), info);
                }
                else
                {
                    // advance()
                    int skip = docid + (int)Math.Ceiling(Math.Abs(skipInterval + Random().NextDouble() * averageGap));
                    docid = leftDocs.Advance(skip);
                    Assert.AreEqual(docid, rightDocs.Advance(skip), info);
                }

                if (docid == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return;
                }
                int freq = leftDocs.Freq();
                Assert.AreEqual(freq, rightDocs.Freq(), info);
                for (int i = 0; i < freq; i++)
                {
                    Assert.AreEqual(leftDocs.NextPosition(), rightDocs.NextPosition(), info);
                    Assert.AreEqual(leftDocs.Payload, rightDocs.Payload, info);
                }
            }
        }

        private void AssertTermsSeekingEquals(string info, Terms leftTerms, Terms rightTerms)
        {
            TermsEnum leftEnum = null;
            TermsEnum rightEnum = null;

            // just an upper bound
            int numTests = AtLeast(20);
            Random random = Random();

            // collect this number of terms from the left side
            HashSet<BytesRef> tests = new HashSet<BytesRef>();
            int numPasses = 0;
            while (numPasses < 10 && tests.Count < numTests)
            {
                leftEnum = leftTerms.Iterator(leftEnum);
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
                        switch (Random().Next(3))
                        {
                            case 0:
                                tests.Add(new BytesRef()); // before the first term
                                break;

                            case 1:
                                tests.Add(new BytesRef(new byte[] { unchecked((byte)0xFF), unchecked((byte)0xFF) })); // past the last term
                                break;

                            case 2:
                                tests.Add(new BytesRef(TestUtil.RandomSimpleString(Random()))); // random term
                                break;

                            default:
                                throw new InvalidOperationException();
                        }
                    }
                }
                numPasses++;
            }

            rightEnum = rightTerms.Iterator(rightEnum);

            IList<BytesRef> shuffledTests = new List<BytesRef>(tests);
            shuffledTests = CollectionsHelper.Shuffle(shuffledTests);

            foreach (BytesRef b in shuffledTests)
            {
                if (Rarely())
                {
                    // reuse the enums
                    leftEnum = leftTerms.Iterator(leftEnum);
                    rightEnum = rightTerms.Iterator(rightEnum);
                }

                bool seekExact = Random().NextBoolean();

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
                        Assert.AreEqual(leftEnum.Term(), rightEnum.Term(), info);
                        AssertTermStatsEquals(info, leftEnum, rightEnum);
                    }
                }
            }
        }

        /// <summary>
        /// checks term-level statistics
        /// </summary>
        public void AssertTermStatsEquals(string info, TermsEnum leftTermsEnum, TermsEnum rightTermsEnum)
        {
            Assert.AreEqual(leftTermsEnum.DocFreq(), rightTermsEnum.DocFreq(), info);
            if (leftTermsEnum.TotalTermFreq() != -1 && rightTermsEnum.TotalTermFreq() != -1)
            {
                Assert.AreEqual(leftTermsEnum.TotalTermFreq(), rightTermsEnum.TotalTermFreq(), info);
            }
        }

        /// <summary>
        /// checks that norms are the same across all fields
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
        /// checks that stored fields of all documents are the same
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
                Comparison<IndexableField> comp = (a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal);
                leftDoc.Fields.Sort(comp);
                rightDoc.Fields.Sort(comp);

                var leftIterator = leftDoc.GetEnumerator();
                var rightIterator = rightDoc.GetEnumerator();
                while (leftIterator.MoveNext())
                {
                    Assert.IsTrue(rightIterator.MoveNext(), info);
                    AssertStoredFieldEquals(info, leftIterator.Current, rightIterator.Current);
                }
                Assert.IsFalse(rightIterator.MoveNext(), info);
            }
        }

        /// <summary>
        /// checks that two stored fields are equivalent
        /// </summary>
        public void AssertStoredFieldEquals(string info, IndexableField leftField, IndexableField rightField)
        {
            Assert.AreEqual(leftField.Name, rightField.Name, info);
            Assert.AreEqual(leftField.BinaryValue, rightField.BinaryValue, info);
            Assert.AreEqual(leftField.StringValue, rightField.StringValue, info);
            Assert.AreEqual(leftField.NumericValue, rightField.NumericValue, info);
            // TODO: should we check the FT at all?
        }

        /// <summary>
        /// checks that term vectors across all fields are equivalent
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
                if (fi.HasDocValues())
                {
                    fields.Add(fi.Name);
                }
            }

            return fields;
        }

        /// <summary>
        /// checks that docvalues across all fields are equivalent
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
                            leftValues.Document = docID;
                            rightValues.Document = docID;
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
                    Bits leftBits = MultiDocValues.GetDocsWithField(leftReader, field);
                    Bits rightBits = MultiDocValues.GetDocsWithField(rightReader, field);
                    if (leftBits != null && rightBits != null)
                    {
                        Assert.AreEqual(leftBits.Length(), rightBits.Length(), info);
                        for (int i = 0; i < leftBits.Length(); i++)
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
            Bits leftBits = MultiFields.GetLiveDocs(leftReader);
            Bits rightBits = MultiFields.GetLiveDocs(rightReader);

            if (leftBits == null || rightBits == null)
            {
                Assert.IsNull(leftBits, info);
                Assert.IsNull(rightBits, info);
                return;
            }

            Debug.Assert(leftReader.MaxDoc == rightReader.MaxDoc);
            Assert.AreEqual(leftBits.Length(), rightBits.Length(), info);
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
            SortedSet<string> left = new SortedSet<string>();
            SortedSet<string> right = new SortedSet<string>();

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
        ///  if it cannot be opened, and (unlike Java's
        ///  File.exists)  if there's some
        ///  unexpected error.
        /// </summary>
        public static bool SlowFileExists(Directory dir, string fileName)
        {
            return dir.FileExists(fileName);
            /*try
            {
                dir.OpenInput(fileName, IOContext.DEFAULT).Dispose();
                return true;
            }
            catch (FileNotFoundException e)
            {
                return false;
            }*/
        }

        /// <summary>
        /// A base location for temporary files of a given test. Helps in figuring out
        /// which tests left which files and where.
        /// </summary>
        private static DirectoryInfo TempDirBase;

        /// <summary>
        /// Retry to create temporary file name this many times.
        /// </summary>
        private static int TEMP_NAME_RETRY_THRESHOLD = 9999;

        /// <summary>
        /// this method is deprecated for a reason. Do not use it. Call <seealso cref="#createTempDir()"/>
        /// or <seealso cref="#createTempDir(String)"/> or <seealso cref="#createTempFile(String, String)"/>.
        /// </summary>
        /*[Obsolete]
        public static DirectoryInfo BaseTempDirForTestClass()
        {
            lock (typeof(LuceneTestCase))
            {
                if (TempDirBase == null)
                {
                    DirectoryInfo directory = new DirectoryInfo(System.IO.Path.GetTempPath());
                    //Debug.Assert(directory.Exists && directory.Directory != null && directory.CanWrite());

                    RandomizedContext ctx = RandomizedContext.Current;
                    Type clazz = ctx.GetTargetType;
                    string prefix = clazz.Name;
                    prefix = prefix.replaceFirst("^org.apache.lucene.", "lucene.");
                    prefix = prefix.replaceFirst("^org.apache.solr.", "solr.");

                    int attempt = 0;
                    DirectoryInfo f;
                    bool iterate = true;
                    do
                    {
                        if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
                        {
                            throw new Exception("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: " + directory.FullName);
                        }
                        f = new DirectoryInfo(Path.Combine(directory.FullName, prefix + "-" + ctx.RunnerSeed + "-" + string.Format(CultureInfo.InvariantCulture, "%03d", attempt)));

                        try
                        {
                            f.Create();
                        }
                        catch (IOException)
                        {
                            iterate = false;
                        }
                    } while (iterate);

                    TempDirBase = f;
                    RegisterToRemoveAfterSuite(TempDirBase);
                }
            }
            return TempDirBase;
        }*/

        /// <summary>
        /// Creates an empty, temporary folder (when the name of the folder is of no importance).
        /// </summary>
        /// <seealso cref= #createTempDir(String) </seealso>
        public static DirectoryInfo CreateTempDir()
        {
            return CreateTempDir("tempDir");
        }

        /// <summary>
        /// Creates an empty, temporary folder with the given name prefix under the
        /// test class's <seealso cref="#getBaseTempDirForTestClass()"/>.
        ///
        /// <p>The folder will be automatically removed after the
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
                f = new DirectoryInfo(Path.Combine(System.IO.Path.GetTempPath(), "LuceneTemp", prefix + "-" + attempt));

                try
                {
                    if (!System.IO.Directory.Exists(f.FullName))
                    {
                        f.Create();
                        iterate = false;
                    }
                }
                catch (IOException exc)
                {
                    iterate = true;
                }
            } while (iterate);

            RegisterToRemoveAfterSuite(f);
            return f;
        }

        /// <summary>
        /// Creates an empty file with the given prefix and suffix under the
        /// test class's <seealso cref="#getBaseTempDirForTestClass()"/>.
        ///
        /// <p>The file will be automatically removed after the
        /// test class completes successfully. The test should close any file handles that would prevent
        /// the folder from being removed.
        /// </summary>
        public static FileInfo CreateTempFile(string prefix, string suffix)
        {
            //DirectoryInfo @base = BaseTempDirForTestClass();

            int attempt = 0;
            FileInfo f;
            do
            {
                if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
                {
                    throw new Exception("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: " + System.IO.Path.GetTempPath());
                }
                f = new FileInfo(Path.Combine(System.IO.Path.GetTempPath(), prefix + "-" + string.Format(CultureInfo.InvariantCulture, "%03d", attempt) + suffix));
            } while (f.Create() == null);

            RegisterToRemoveAfterSuite(f);
            return f;
        }

        /// <summary>
        /// Creates an empty temporary file.
        /// </summary>
        /// <seealso cref= #createTempFile(String, String)  </seealso>
        public static FileInfo CreateTempFile()
        {
            return CreateTempFile("tempFile", ".tmp");
        }

        /// <summary>
        /// A queue of temporary resources to be removed after the
        /// suite completes. </summary>
        /// <seealso cref= #registerToRemoveAfterSuite(File) </seealso>
        private static readonly ConcurrentQueue<string> CleanupQueue = new ConcurrentQueue<string>();

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

            CleanupQueue.Enqueue(f.FullName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected string GetFullMethodName([CallerMemberName] string memberName = "")
        {
            return string.Format("{0}+{1}", this.GetType().Name, memberName);
        }

        private void CleanupTemporaryFiles()
        {
            // Drain cleanup queue and clear it.
            var tempDirBasePath = (TempDirBase != null ? TempDirBase.FullName : null);
            TempDirBase = null;

            // Only check and throw an IOException on un-removable files if the test
            // was successful. Otherwise just report the path of temporary files
            // and leave them there.
            if (LuceneTestCase.SuiteFailureMarker /*.WasSuccessful()*/)
            {
                string f;
                while (CleanupQueue.TryDequeue(out f))
                {
                    try
                    {
                        if (System.IO.Directory.Exists(f))
                            System.IO.Directory.Delete(f, true);
                        else if (System.IO.File.Exists(f))
                            File.Delete(f);
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
        /// Contains a list of all the IConcurrentMergeSchedulers to be tested.
        /// 
        /// LUCENENET specific
        /// </summary>
        public static class ConcurrentMergeSchedulers
        {
            public static readonly IConcurrentMergeScheduler[] Values = new IConcurrentMergeScheduler[] {
#if !NETSTANDARD
                new ConcurrentMergeScheduler(),
#endif
                new TaskMergeScheduler()
            };
        }
    }

    /*internal class ReaderClosedListenerAnonymousInnerClassHelper : IndexReader.ReaderClosedListener
    {
        private ThreadPoolExecutor ex;

        public ReaderClosedListenerAnonymousInnerClassHelper(ThreadPoolExecutor ex)
        {
            this.ex = ex;
        }

        public void OnClose(IndexReader reader)
        {
            TestUtil.ShutdownExecutorService(ex);
        }
    }*/

    internal class ComparatorAnonymousInnerClassHelper : System.Collections.IComparer
    {
        private readonly LuceneTestCase outerInstance;

        public ComparatorAnonymousInnerClassHelper(LuceneTestCase outerInstance)
        {
            this.outerInstance = outerInstance;
        }

        public virtual int Compare(object arg0, object arg1)
        {
            return System.String.Compare(((IndexableField)arg0).Name, ((IndexableField)arg1).Name, System.StringComparison.Ordinal);
        }
    }
}