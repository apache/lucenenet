using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Lucene.Net.Store;

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

//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	//import static com.carrotsearch.randomizedtesting.RandomizedTest.systemPropertyAsBoolean;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	//import static com.carrotsearch.randomizedtesting.RandomizedTest.systemPropertyAsInt;


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using Codec = Lucene.Net.Codecs.Codec;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using Store = Lucene.Net.Document.Field.Store;
	using FieldType = Lucene.Net.Document.FieldType;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using AlcoholicMergePolicy = Lucene.Net.Index.AlcoholicMergePolicy;
	using AssertingAtomicReader = Lucene.Net.Index.AssertingAtomicReader;
	using AssertingDirectoryReader = Lucene.Net.Index.AssertingDirectoryReader;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
	using CompositeReader = Lucene.Net.Index.CompositeReader;
	using ConcurrentMergeScheduler = Lucene.Net.Index.ConcurrentMergeScheduler;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
	using DocsEnum = Lucene.Net.Index.DocsEnum;
	using FieldFilterAtomicReader = Lucene.Net.Index.FieldFilterAtomicReader;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using Fields = Lucene.Net.Index.Fields;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using ReaderClosedListener = Lucene.Net.Index.IndexReader.ReaderClosedListener;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using IndexableField = Lucene.Net.Index.IndexableField;
	using LogByteSizeMergePolicy = Lucene.Net.Index.LogByteSizeMergePolicy;
	using LogDocMergePolicy = Lucene.Net.Index.LogDocMergePolicy;
	using LogMergePolicy = Lucene.Net.Index.LogMergePolicy;
	using MergePolicy = Lucene.Net.Index.MergePolicy;
	using MockRandomMergePolicy = Lucene.Net.Index.MockRandomMergePolicy;
	using MultiDocValues = Lucene.Net.Index.MultiDocValues;
	using MultiFields = Lucene.Net.Index.MultiFields;
	using NumericDocValues = Lucene.Net.Index.NumericDocValues;
	using ParallelAtomicReader = Lucene.Net.Index.ParallelAtomicReader;
	using ParallelCompositeReader = Lucene.Net.Index.ParallelCompositeReader;
	using SegmentReader = Lucene.Net.Index.SegmentReader;
	using SerialMergeScheduler = Lucene.Net.Index.SerialMergeScheduler;
	using SimpleMergedSegmentWarmer = Lucene.Net.Index.SimpleMergedSegmentWarmer;
	using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
	using SortedDocValues = Lucene.Net.Index.SortedDocValues;
	using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
	using TieredMergePolicy = Lucene.Net.Index.TieredMergePolicy;
	using AssertingIndexSearcher = Lucene.Net.Search.AssertingIndexSearcher;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using FieldCache = Lucene.Net.Search.FieldCache;
	//using CacheEntry = Lucene.Net.Search.FieldCache.CacheEntry;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using FCInvisibleMultiReader = Lucene.Net.Search.QueryUtils.FCInvisibleMultiReader;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using Directory = Lucene.Net.Store.Directory;
	using FSDirectory = Lucene.Net.Store.FSDirectory;
	using FlushInfo = Lucene.Net.Store.FlushInfo;
	using IOContext = Lucene.Net.Store.IOContext;
	using Context = Lucene.Net.Store.IOContext.Context;
	using LockFactory = Lucene.Net.Store.LockFactory;
	using MergeInfo = Lucene.Net.Store.MergeInfo;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using Throttling = Lucene.Net.Store.MockDirectoryWrapper.Throttling;
	using NRTCachingDirectory = Lucene.Net.Store.NRTCachingDirectory;
	using RateLimitedDirectoryWrapper = Lucene.Net.Store.RateLimitedDirectoryWrapper;
	using Insanity = Lucene.Net.Util.FieldCacheSanityChecker.Insanity;
	using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
	using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
	using RegExp = Lucene.Net.Util.Automaton.RegExp;
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
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @RunWith(RandomizedRunner.class) @TestMethodProviders({ LuceneJUnit3MethodProvider.class, JUnit4MethodProvider.class }) @Listeners({ RunListenerPrintReproduceInfo.class, FailureMarker.class }) @SeedDecorators({MixWithSuiteName.class}) @ThreadLeakScope(Scope.SUITE) @ThreadLeakGroup(Group.MAIN) @ThreadLeakAction({Action.WARN, Action.INTERRUPT}) @ThreadLeakLingering(linger = 20000) @ThreadLeakZombies(Consequence.IGNORE_REMAINING_TESTS) @TimeoutSuite(millis = 2 * TimeUnits.HOUR) @ThreadLeakFilters(defaultFilters = true, filters = { QuickPatchThreadsFilter.class }) public abstract class LuceneTestCase : org.junit.Assert
	[TestFixture]
    public abstract class LuceneTestCase : Assert // Wait long for leaked threads to complete before failure. zk needs this. -  See LUCENE-3995 for rationale.
	{
        public static  System.IO.FileInfo TEMP_DIR;
		public LuceneTestCase()
		{
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

	  /// <seealso cref= #ignoreAfterMaxFailures </seealso>
	  public const string SYSPROP_MAXFAILURES = "tests.maxfailures";

	  /// <seealso cref= #ignoreAfterMaxFailures </seealso>
	  public const string SYSPROP_FAILFAST = "tests.failfast";

	  /// <summary>
	  /// Annotation for tests that should only be run during nightly builds.
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Documented @Inherited @Retention(RetentionPolicy.RUNTIME) @TestGroup(enabled = false, sysProperty = SYSPROP_NIGHTLY) public class Nightly : System.Attribute
	  public class Nightly : System.Attribute
	  /// <summary>
	  /// Annotation for tests that should only be run during weekly builds
	  /// </summary>
	  {
		  private readonly LuceneTestCase OuterInstance;

		  public Nightly(LuceneTestCase outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

	  }
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Documented @Inherited @Retention(RetentionPolicy.RUNTIME) @TestGroup(enabled = false, sysProperty = SYSPROP_WEEKLY) public class Weekly : System.Attribute
	  public class Weekly : System.Attribute
	  /// <summary>
	  /// Annotation for tests which exhibit a known issue and are temporarily disabled.
	  /// </summary>
	  {
		  private readonly LuceneTestCase OuterInstance;

		  public Weekly(LuceneTestCase outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

	  }
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Documented @Inherited @Retention(RetentionPolicy.RUNTIME) @TestGroup(enabled = false, sysProperty = SYSPROP_AWAITSFIX) public class AwaitsFix : System.Attribute
	  public class AwaitsFix : System.Attribute
	  {
		  private readonly LuceneTestCase OuterInstance;

		  public AwaitsFix(LuceneTestCase outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		/// <summary>
		/// Point to JIRA entry. </summary>
		public string bugUrl();
	  }

	  /// <summary>
	  /// Annotation for tests that are slow. Slow tests do run by default but can be
	  /// disabled if a quick run is needed.
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Documented @Inherited @Retention(RetentionPolicy.RUNTIME) @TestGroup(enabled = true, sysProperty = SYSPROP_SLOW) public class Slow : System.Attribute
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
	  }

	  /// <summary>
	  /// Annotation for test classes that should avoid certain codec types
	  /// (because they are expensive, for example).
	  /// </summary>
	  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	  public class SuppressCodecs : System.Attribute
	  {
		string[] value();
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
		public string bugUrl() default "None";
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
	  public static Version TEST_VERSION_CURRENT = Version.LUCENE_48;

	  /// <summary>
	  /// True if and only if tests are run in verbose mode. If this flag is false
	  /// tests are not expected to print any messages.
	  /// </summary>
	  public static bool VERBOSE = systemPropertyAsBoolean("tests.verbose", false);

	  /// <summary>
	  /// TODO: javadoc? </summary>
	  public static bool INFOSTREAM = systemPropertyAsBoolean("tests.infostream", VERBOSE);

	  /// <summary>
	  /// A random multiplier which you should use when writing random tests:
	  /// multiply it by the number of iterations to scale your tests (for nightly builds).
	  /// </summary>
	  public static int RANDOM_MULTIPLIER = systemPropertyAsInt("tests.multiplier", 1);

	  /// <summary>
	  /// TODO: javadoc? </summary>
	  public static string DEFAULT_LINE_DOCS_FILE = "europarl.lines.txt.gz";

	  /// <summary>
	  /// TODO: javadoc? </summary>
	  public static string JENKINS_LARGE_LINE_DOCS_FILE = "enwiki.random.lines.txt";

	  /// <summary>
	  /// Gets the codec to run tests with. </summary>
	  public static string TEST_CODEC = System.getProperty("tests.codec", "random");

	  /// <summary>
	  /// Gets the postingsFormat to run tests with. </summary>
	  public static string TEST_POSTINGSFORMAT = System.getProperty("tests.postingsformat", "random");

	  /// <summary>
	  /// Gets the docValuesFormat to run tests with </summary>
	  public static string TEST_DOCVALUESFORMAT = System.getProperty("tests.docvaluesformat", "random");

	  /// <summary>
	  /// Gets the directory to run tests with </summary>
	  public static string TEST_DIRECTORY = System.getProperty("tests.directory", "random");

	  /// <summary>
	  /// the line file used by LineFileDocs </summary>
	  public static string TEST_LINE_DOCS_FILE = System.getProperty("tests.linedocsfile", DEFAULT_LINE_DOCS_FILE);

	  /// <summary>
	  /// Whether or not <seealso cref="Nightly"/> tests should run. </summary>
	  public static bool TEST_NIGHTLY = systemPropertyAsBoolean(SYSPROP_NIGHTLY, false);

	  /// <summary>
	  /// Whether or not <seealso cref="Weekly"/> tests should run. </summary>
	  public static bool TEST_WEEKLY = systemPropertyAsBoolean(SYSPROP_WEEKLY, false);

	  /// <summary>
	  /// Whether or not <seealso cref="AwaitsFix"/> tests should run. </summary>
	  public static bool TEST_AWAITSFIX = systemPropertyAsBoolean(SYSPROP_AWAITSFIX, false);

	  /// <summary>
	  /// Whether or not <seealso cref="Slow"/> tests should run. </summary>
	  public static bool TEST_SLOW = systemPropertyAsBoolean(SYSPROP_SLOW, false);

	  /// <summary>
	  /// Throttling, see <seealso cref="MockDirectoryWrapper#setThrottling(Throttling)"/>. </summary>
	  public static MockDirectoryWrapper.Throttling TEST_THROTTLING = TEST_NIGHTLY ? MockDirectoryWrapper.Throttling.SOMETIMES : MockDirectoryWrapper.Throttling.NEVER;

	  /// <summary>
	  /// Leave temporary files on disk, even on successful runs. </summary>
	  public static bool LEAVE_TEMPORARY;
	  static LuceneTestCase()
	  {
		bool defaultValue = false;
		foreach (string property in Arrays.asList("tests.leaveTemporary", "tests.leavetemporary", "tests.leavetmpdir", "solr.test.leavetmpdir")) // Solr's legacy -  default -  lowercase -  ANT tasks's (junit4) flag.
		{
		  defaultValue |= systemPropertyAsBoolean(property, false);
		}
		LEAVE_TEMPORARY = defaultValue;
		CORE_DIRECTORIES = new List<>(FS_DIRECTORIES);
		CORE_DIRECTORIES.Add("RAMDirectory");
		int maxFailures = systemPropertyAsInt(SYSPROP_MAXFAILURES, int.MaxValue);
		bool failFast = systemPropertyAsBoolean(SYSPROP_FAILFAST, false);

		if (failFast)
		{
		  if (maxFailures == int.MaxValue)
		  {
			maxFailures = 1;
		  }
		  else
		  {
			Logger.getLogger(typeof(LuceneTestCase).SimpleName).warning("Property '" + SYSPROP_MAXFAILURES + "'=" + maxFailures + ", 'failfast' is" + " ignored.");
		  }
		}

		IgnoreAfterMaxFailuresDelegate = new AtomicReference<>(new TestRuleIgnoreAfterMaxFailures(maxFailures));
		IgnoreAfterMaxFailures = TestRuleDelegate.Of(IgnoreAfterMaxFailuresDelegate);
	  }

	  /// <summary>
	  /// These property keys will be ignored in verification of altered properties. </summary>
	  /// <seealso cref= SystemPropertiesInvariantRule </seealso>
	  /// <seealso cref= #ruleChain </seealso>
	  /// <seealso cref= #classRules </seealso>
	  private static string [] IGNORED_INVARIANT_PROPERTIES = {"user.timezone", "java.rmi.server.randomIDs"};

	  /// <summary>
	  /// Filesystem-based <seealso cref="Directory"/> implementations. </summary>
	  private static IList<string> FS_DIRECTORIES = Arrays.asList("SimpleFSDirectory", "NIOFSDirectory", "MMapDirectory");

	  /// <summary>
	  /// All <seealso cref="Directory"/> implementations. </summary>
	  private static IList<string> CORE_DIRECTORIES;

	  protected static Set<string> DoesntSupportOffsets = new HashSet<>(Arrays.asList("Lucene3x", "MockFixedIntBlock", "MockVariableIntBlock", "MockSep", "MockRandom"));

	  // -----------------------------------------------------------------
	  // Fields initialized in class or instance rules.
	  // -----------------------------------------------------------------

	  /// <summary>
	  /// When {@code true}, Codecs for old Lucene version will support writing
	  /// indexes in that format. Defaults to {@code false}, can be disabled by
	  /// specific tests on demand.
	  /// 
	  /// @lucene.internal
	  /// </summary>
	  public static bool OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;

	  // -----------------------------------------------------------------
	  // Class level (suite) rules.
	  // -----------------------------------------------------------------

	  /// <summary>
	  /// Stores the currently class under test.
	  /// </summary>
	  private static TestRuleStoreClassName ClassNameRule;

	  /// <summary>
	  /// Class environment setup rule.
	  /// </summary>
	  static TestRuleSetupAndRestoreClassEnv ClassEnvRule;

	  /// <summary>
	  /// Suite failure marker (any error in the test or suite scope).
	  /// </summary>
	  public static TestRuleMarkFailure SuiteFailureMarker;

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
	  private static AtomicReference<TestRuleIgnoreAfterMaxFailures> IgnoreAfterMaxFailuresDelegate;
	  private static TestRule IgnoreAfterMaxFailures;

	  /// <summary>
	  /// Temporarily substitute the global <seealso cref="TestRuleIgnoreAfterMaxFailures"/>. See
	  /// <seealso cref="#ignoreAfterMaxFailuresDelegate"/> for some explanation why this method 
	  /// is needed.
	  /// </summary>
	  public static TestRuleIgnoreAfterMaxFailures ReplaceMaxFailureRule(TestRuleIgnoreAfterMaxFailures newValue)
	  {
		return IgnoreAfterMaxFailuresDelegate.getAndSet(newValue);
	  }

	  /// <summary>
	  /// Max 10mb of static data stored in a test suite class after the suite is complete.
	  /// Prevents static data structures leaking and causing OOMs in subsequent tests.
	  /// </summary>
	  private static long STATIC_LEAK_THRESHOLD = 10 * 1024 * 1024;

	  /// <summary>
	  /// By-name list of ignored types like loggers etc. </summary>
	  private static Set<string> STATIC_LEAK_IGNORED_TYPES = Collections.unmodifiableSet(new HashSet<>(Arrays.asList("org.slf4j.Logger", "org.apache.solr.SolrLogFormatter", typeof(EnumSet).Name)));

	  /// <summary>
	  /// this controls how suite-level rules are nested. It is important that _all_ rules declared
	  /// in <seealso cref="LuceneTestCase"/> are executed in proper order if they depend on each 
	  /// other.
	  /// </summary>
	  public static TestRule ClassRules = RuleChain.outerRule(new TestRuleIgnoreTestSuites()).around(IgnoreAfterMaxFailures).around(SuiteFailureMarker = new TestRuleMarkFailure()).around(new TestRuleAssertionsRequired()).around(new TemporaryFilesCleanupRule()).around(new StaticFieldsInvariantRule(STATIC_LEAK_THRESHOLD, true) {@Override protected bool accept(System.Reflection.FieldInfo field) {if (STATIC_LEAK_IGNORED_TYPES.contains(field.Type.Name)) {return false;} if (field.DeclaringClass == typeof(LuceneTestCase)) {return false;} return base.accept(field);}}).around(new NoClassHooksShadowingRule()).around(new NoInstanceHooksOverridesRule() {@Override protected bool verify(Method key) {string name = key.Name; return !(name.Equals("setUp") || name.Equals("tearDown"));}}).around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES)).around(ClassNameRule = new TestRuleStoreClassName()).around(ClassEnvRule = new TestRuleSetupAndRestoreClassEnv());
			// Don't count known classes that consume memory once.
			// Don't count references from ourselves, we're top-level.


	  // -----------------------------------------------------------------
	  // Test level rules.
	  // -----------------------------------------------------------------

	  /// <summary>
	  /// Enforces <seealso cref="#setUp()"/> and <seealso cref="#tearDown()"/> calls are chained. </summary>
	  private TestRuleSetupTeardownChained ParentChainCallRule = new TestRuleSetupTeardownChained();

	  /// <summary>
	  /// Save test thread and name. </summary>
	  private TestRuleThreadAndTestName ThreadAndTestNameRule = new TestRuleThreadAndTestName();

	  /// <summary>
	  /// Taint suite result with individual test failures. </summary>
	  private TestRuleMarkFailure TestFailureMarker = new TestRuleMarkFailure(SuiteFailureMarker);

	  /// <summary>
	  /// this controls how individual test rules are nested. It is important that
	  /// _all_ rules declared in <seealso cref="LuceneTestCase"/> are executed in proper order
	  /// if they depend on each other.
	  /// </summary>
	  public TestRule RuleChain = RuleChain.outerRule(TestFailureMarker).around(IgnoreAfterMaxFailures).around(ThreadAndTestNameRule).around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES)).around(new TestRuleSetupAndRestoreInstanceEnv()).around(new TestRuleFieldCacheSanity()).around(ParentChainCallRule);

	  // -----------------------------------------------------------------
	  // Suite and test case setup/ cleanup.
	  // -----------------------------------------------------------------

	  /// <summary>
	  /// For subclasses to override. Overrides must call {@code super.setUp()}.
	  /// </summary>
	  public void setUp() throws Exception
	  {
		ParentChainCallRule.SetupCalled = true;
	  }

	  /// <summary>
	  /// For subclasses to override. Overrides must call {@code super.tearDown()}.
	  /// </summary>
	  public void tearDown() throws Exception
	  {
		ParentChainCallRule.TeardownCalled = true;
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
		return RandomizedContext.current().Random;
	  }

	  /// <summary>
	  /// Registers a <seealso cref="IDisposable"/> resource that should be closed after the test
	  /// completes.
	  /// </summary>
	  /// <returns> <code>resource</code> (for call chaining). </returns>
	  public static T CloseAfterTest<T>(T resource)
	  {
		return RandomizedContext.current().closeAtEnd(resource, LifecycleScope.TEST);
	  }

	  /// <summary>
	  /// Registers a <seealso cref="IDisposable"/> resource that should be closed after the suite
	  /// completes.
	  /// </summary>
	  /// <returns> <code>resource</code> (for call chaining). </returns>
	  public static T CloseAfterSuite<T>(T resource)
	  {
		return RandomizedContext.current().closeAtEnd(resource, LifecycleScope.SUITE);
	  }

	  /// <summary>
	  /// Return the current class being tested.
	  /// </summary>
	  public static Type TestClass
	  {
		return ClassNameRule.TestClass;
	  }

	  /// <summary>
	  /// Return the name of the currently executing test case.
	  /// </summary>
	  public string TestName
	  {
		return ThreadAndTestNameRule.TestMethodName;
	  }

	  /// <summary>
	  /// Some tests expect the directory to contain a single segment, and want to 
	  /// do tests on that segment's reader. this is an utility method to help them.
	  /// </summary>
	  public static SegmentReader GetOnlySegmentReader(DirectoryReader reader)
	  {
		IList<AtomicReaderContext> subReaders = reader.leaves();
		if (subReaders.Count != 1)
		{
		  throw new System.ArgumentException(reader + " has " + subReaders.Count + " segments instead of exactly one");
		}
		AtomicReader r = subReaders[0].reader();
		Assert.IsTrue(r is SegmentReader);
		return (SegmentReader) r;
	  }

	  /// <summary>
	  /// Returns true if and only if the calling thread is the primary thread 
	  /// executing the test case. 
	  /// </summary>
	  protected bool TestThread
	  {
		Assert.IsNotNull(ThreadAndTestNameRule.TestCaseThread, "Test case thread not set?");
		return Thread.CurrentThread == ThreadAndTestNameRule.TestCaseThread;
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
	  protected static void assertSaneFieldCaches(final string msg)
	  {
		FieldCache.CacheEntry[] entries = FieldCache.DEFAULT.CacheEntries;
		Insanity[] insanity = null;
		try
		{
		  try
		  {
			insanity = FieldCacheSanityChecker.checkSanity(entries);
		  }
		  catch (Exception e)
		  {
			DumpArray(msg + ": FieldCache", entries, System.err);
			throw e;
		  }

		  Assert.AreEqual(msg + ": Insane FieldCache usage(s) found", 0, insanity.Length);
		  insanity = null;
		}
		finally
		{

		  // report this in the event of any exception/failure
		  // if no failure, then insanity will be null anyway
		  if (null != insanity)
		  {
			DumpArray(msg + ": Insane FieldCache usage(s)", insanity, System.err);
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
		p += (p * Math.Log(RANDOM_MULTIPLIER));
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

	  public static void assumeTrue(string msg, bool condition)
	  {
		RandomizedTest.assumeTrue(msg, condition);
	  }

	  public static void assumeFalse(string msg, bool condition)
	  {
		RandomizedTest.assumeFalse(msg, condition);
	  }

	  public static void assumeNoException(string msg, Exception e)
	  {
		RandomizedTest.assumeNoException(msg, e);
	  }

	  /// <summary>
	  /// Return <code>args</code> as a <seealso cref="Set"/> instance. The order of elements is not
	  /// preserved in iterators.
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SafeVarargs @SuppressWarnings("varargs") public static <T> java.util.Set<T> asSet(T... args)
	  public static <T> Set<T> AsSet(T... args)
	  {
		return new HashSet<>(Arrays.asList(args));
	  }

	  /// <summary>
	  /// Convenience method for logging an iterator.
	  /// </summary>
	  /// <param name="label">  String logged before/after the items in the iterator </param>
	  /// <param name="iter">   Each next() is toString()ed and logged on it's own line. If iter is null this is logged differnetly then an empty iterator. </param>
	  /// <param name="stream"> Stream to log messages to. </param>
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: public static void dumpIterator(String label, java.util.Iterator<?> iter, java.io.PrintStream stream)
	  public static void dumpIterator(string label, IEnumerator<?> iter, PrintStream stream)
	  {
		stream.println("*** BEGIN " + label + " ***");
		if (null == iter)
		{
		  stream.println(" ... NULL ...");
		}
		else
		{
		  while (iter.hasNext())
		  {
			stream.println(iter.next().ToString());
		  }
		}
		stream.println("*** END " + label + " ***");
	  }

	  /// <summary>
	  /// Convenience method for logging an array.  Wraps the array in an iterator and delegates
	  /// </summary>
	  /// <seealso cref= #dumpIterator(String,Iterator,PrintStream) </seealso>
	  public static void dumpArray(string label, Object[] objs, PrintStream stream)
	  {
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: java.util.Iterator<?> iter = (null == objs) ? null : java.util.Arrays.asList(objs).iterator();
		IEnumerator<?> iter = (null == objs) ? null : Arrays.asList(objs).GetEnumerator();
		DumpIterator(label, iter, stream);
	  }

	  /// <summary>
	  /// create a new index writer config with random defaults </summary>
	  public static IndexWriterConfig NewIndexWriterConfig(Version v, Analyzer a)
	  {
		return NewIndexWriterConfig(Random(), v, a);
	  }

	  /// <summary>
	  /// create a new index writer config with random defaults using the specified random </summary>
	  public static IndexWriterConfig NewIndexWriterConfig(Random r, Version v, Analyzer a)
	  {
		IndexWriterConfig c = new IndexWriterConfig(v, a);
		c.Similarity = ClassEnvRule.Similarity;
		if (VERBOSE)
		{
		  // Even though TestRuleSetupAndRestoreClassEnv calls
		  // InfoStream.setDefault, we do it again here so that
		  // the PrintStreamInfoStream.messageID increments so
		  // that when there are separate instances of
		  // IndexWriter created we see "IW 0", "IW 1", "IW 2",
		  // ... instead of just always "IW 0":
		  c.InfoStream = new TestRuleSetupAndRestoreClassEnv.ThreadNameFixingPrintStreamInfoStream(System.out);
		}

		if (r.nextBoolean())
		{
		  c.MergeScheduler = new SerialMergeScheduler();
		}
		else if (Rarely(r))
		{
		  int maxThreadCount = TestUtil.NextInt(Random(), 1, 4);
		  int maxMergeCount = TestUtil.NextInt(Random(), maxThreadCount, maxThreadCount + 4);
		  ConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
		  cms.setMaxMergesAndThreads(maxMergeCount, maxThreadCount);
		  c.MergeScheduler = cms;
		}
		if (r.nextBoolean())
		{
		  if (Rarely(r))
		  {
			// crazy value
			c.MaxBufferedDocs = TestUtil.NextInt(r, 2, 15);
		  }
		  else
		  {
			// reasonable value
			c.MaxBufferedDocs = TestUtil.NextInt(r, 16, 1000);
		  }
		}
		if (r.nextBoolean())
		{
		  if (Rarely(r))
		  {
			// crazy value
			c.TermIndexInterval = r.nextBoolean() ? TestUtil.NextInt(r, 1, 31) : TestUtil.NextInt(r, 129, 1000);
		  }
		  else
		  {
			// reasonable value
			c.TermIndexInterval = TestUtil.NextInt(r, 32, 128);
		  }
		}
		if (r.nextBoolean())
		{
		  int maxNumThreadStates = Rarely(r) ? TestUtil.NextInt(r, 5, 20) : TestUtil.NextInt(r, 1, 4); // reasonable value -  crazy value

		  try
		  {
			if (Rarely(r))
			{
			  // Retrieve the package-private setIndexerThreadPool
			  // method:
			  Method setIndexerThreadPoolMethod = typeof(IndexWriterConfig).getDeclaredMethod("setIndexerThreadPool", Type.GetType("Lucene.Net.Index.DocumentsWriterPerThreadPool"));
			  setIndexerThreadPoolMethod.setAccessible(true);
			  Type clazz = Type.GetType("Lucene.Net.Index.RandomDocumentsWriterPerThreadPool");
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: Constructor<?> ctor = clazz.getConstructor(int.class, java.util.Random.class);
			  Constructor<?> ctor = clazz.GetConstructor(typeof(int), typeof(Random));
			  ctor.Accessible = true;
			  // random thread pool
			  setIndexerThreadPoolMethod.invoke(c, ctor.newInstance(maxNumThreadStates, r));
			}
			else
			{
			  // random thread pool
			  c.MaxThreadStates = maxNumThreadStates;
			}
		  }
		  catch (Exception e)
		  {
			Rethrow.Rethrow(e);
		  }
		}

		c.MergePolicy = NewMergePolicy(r);

		if (Rarely(r))
		{
		  c.MergedSegmentWarmer = new SimpleMergedSegmentWarmer(c.InfoStream);
		}
		c.UseCompoundFile = r.nextBoolean();
		c.ReaderPooling = r.nextBoolean();
		c.ReaderTermsIndexDivisor = TestUtil.NextInt(r, 1, 4);
		c.CheckIntegrityAtMerge = r.nextBoolean();
		return c;
	  }

	  public static MergePolicy NewMergePolicy(Random r)
	  {
		if (Rarely(r))
		{
		  return new MockRandomMergePolicy(r);
		}
		else if (r.nextBoolean())
		{
		  return NewTieredMergePolicy(r);
		}
		else if (r.Next(5) == 0)
		{
		  return NewAlcoholicMergePolicy(r, ClassEnvRule.TimeZone);
		}
		return NewLogMergePolicy(r);
	  }

	  public static MergePolicy NewMergePolicy()
	  {
		return NewMergePolicy(Random());
	  }

	  public static LogMergePolicy NewLogMergePolicy()
	  {
		return NewLogMergePolicy(Random());
	  }

	  public static TieredMergePolicy NewTieredMergePolicy()
	  {
		return NewTieredMergePolicy(Random());
	  }

	  public static AlcoholicMergePolicy NewAlcoholicMergePolicy()
	  {
		return NewAlcoholicMergePolicy(Random(), ClassEnvRule.TimeZone);
	  }

	  public static AlcoholicMergePolicy NewAlcoholicMergePolicy(Random r, TimeZone tz)
	  {
		return new AlcoholicMergePolicy(tz, new Random(r.nextLong()));
	  }

	  public static LogMergePolicy NewLogMergePolicy(Random r)
	  {
		LogMergePolicy logmp = r.nextBoolean() ? new LogDocMergePolicy() : new LogByteSizeMergePolicy();
		logmp.CalibrateSizeByDeletes = r.nextBoolean();
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

	  private static void configureRandom(Random r, MergePolicy mergePolicy)
	  {
		if (r.nextBoolean())
		{
		  mergePolicy.NoCFSRatio = 0.1 + r.NextDouble() * 0.8;
		}
		else
		{
		  mergePolicy.NoCFSRatio = r.nextBoolean() ? 1.0 : 0.0;
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
		return WrapDirectory(r, NewDirectoryImpl(r, TEST_DIRECTORY), Rarely(r));
	  }

	  public static MockDirectoryWrapper NewMockDirectory()
	  {
		return NewMockDirectory(Random());
	  }

	  public static MockDirectoryWrapper NewMockDirectory(Random r)
	  {
		return (MockDirectoryWrapper) WrapDirectory(r, NewDirectoryImpl(r, TEST_DIRECTORY), false);
	  }

	  public static MockDirectoryWrapper NewMockFSDirectory(File f)
	  {
		return (MockDirectoryWrapper) NewFSDirectory(f, null, false);
	  }

	  /// <summary>
	  /// Returns a new Directory instance, with contents copied from the
	  /// provided directory. See <seealso cref="#newDirectory()"/> for more
	  /// information.
	  /// </summary>
	  public static BaseDirectoryWrapper NewDirectory(Directory d) throws IOException
	  {
		return NewDirectory(Random(), d);
	  }

	  /// <summary>
	  /// Returns a new FSDirectory instance over the given file, which must be a folder. </summary>
	  public static BaseDirectoryWrapper NewFSDirectory(File f)
	  {
		return NewFSDirectory(f, null);
	  }

	  /// <summary>
	  /// Returns a new FSDirectory instance over the given file, which must be a folder. </summary>
	  public static BaseDirectoryWrapper NewFSDirectory(File f, LockFactory lf)
	  {
		return NewFSDirectory(f, lf, Rarely());
	  }

	  private static BaseDirectoryWrapper NewFSDirectory(File f, LockFactory lf, bool bare)
	  {
		string fsdirClass = TEST_DIRECTORY;
		if (fsdirClass.Equals("random"))
		{
		  fsdirClass = RandomPicks.randomFrom(Random(), FS_DIRECTORIES);
		}

		Type clazz;
		try
		{
		  try
		  {
			clazz = CommandLineUtil.loadFSDirectoryClass(fsdirClass);
		  }
		  catch (System.InvalidCastException e)
		  {
			// TEST_DIRECTORY is not a sub-class of FSDirectory, so draw one at random
			fsdirClass = RandomPicks.randomFrom(Random(), FS_DIRECTORIES);
			clazz = CommandLineUtil.loadFSDirectoryClass(fsdirClass);
		  }

		  Directory fsdir = NewFSDirectoryImpl(clazz, f);
		  BaseDirectoryWrapper wrapped = WrapDirectory(Random(), fsdir, bare);
		  if (lf != null)
		  {
			wrapped.LockFactory = lf;
		  }
		  return wrapped;
		}
		catch (Exception e)
		{
		  Rethrow.Rethrow(e);
		  throw null; // dummy to prevent compiler failure
		}
	  }

	  /// <summary>
	  /// Returns a new Directory instance, using the specified random
	  /// with contents copied from the provided directory. See 
	  /// <seealso cref="#newDirectory()"/> for more information.
	  /// </summary>
	  public static BaseDirectoryWrapper NewDirectory(Random r, Directory d) throws IOException
	  {
		Directory impl = NewDirectoryImpl(r, TEST_DIRECTORY);
		foreach (string file in d.listAll())
		{
		 d.copy(impl, file, file, NewIOContext(r));
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
			  rateLimitedDirectoryWrapper.setMaxWriteMBPerSec(maxMBPerSec, IOContext.Context.FLUSH);
			  break;
			case 2: // sometimes rate limit flush & merge
			  rateLimitedDirectoryWrapper.setMaxWriteMBPerSec(maxMBPerSec, IOContext.Context.FLUSH);
			  rateLimitedDirectoryWrapper.setMaxWriteMBPerSec(maxMBPerSec, IOContext.Context.MERGE);
			  break;
			default:
			  rateLimitedDirectoryWrapper.setMaxWriteMBPerSec(maxMBPerSec, IOContext.Context.MERGE);
		  break;
		  }
		  directory = rateLimitedDirectoryWrapper;

		}

		if (bare)
		{
		  BaseDirectoryWrapper @base = new BaseDirectoryWrapper(directory);
		  CloseAfterSuite(new IDisposableDirectory(@base, SuiteFailureMarker));
		  return @base;
		}
		else
		{
		  MockDirectoryWrapper mock = new MockDirectoryWrapper(random, directory);

		  mock.Throttling = TEST_THROTTLING;
		  CloseAfterSuite(new IDisposableDirectory(mock, SuiteFailureMarker));
		  return mock;
		}
	  }

	  public static Field NewStringField(string name, string value, Field.Store stored)
	  {
		return NewField(Random(), name, value, stored == Field.Store.YES ? StringField.TYPE_STORED : StringField.TYPE_NOT_STORED);
	  }

	  public static Field NewTextField(string name, string value, Field.Store stored)
	  {
		return NewField(Random(), name, value, stored == Field.Store.YES ? TextField.TYPE_STORED : TextField.TYPE_NOT_STORED);
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
		return NewField(Random(), name, value, type);
	  }

	  public static Field NewField(Random random, string name, string value, FieldType type)
	  {
		name = new string(name);
		if (Usually(random) || !type.indexed())
		{
		  // most of the time, don't modify the params
		  return new Field(name, value, type);
		}

		// TODO: once all core & test codecs can index
		// offsets, sometimes randomly turn on offsets if we are
		// already indexing positions...

		FieldType newType = new FieldType(type);
		if (!newType.stored() && random.nextBoolean())
		{
		  newType.Stored = true; // randomly store it
		}

		if (!newType.storeTermVectors() && random.nextBoolean())
		{
		  newType.StoreTermVectors = true;
		  if (!newType.storeTermVectorOffsets())
		  {
			newType.StoreTermVectorOffsets = random.nextBoolean();
		  }
		  if (!newType.storeTermVectorPositions())
		  {
			newType.StoreTermVectorPositions = random.nextBoolean();

			if (newType.storeTermVectorPositions() && !newType.storeTermVectorPayloads() && !OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
			{
			  newType.StoreTermVectorPayloads = random.nextBoolean();
			}
		  }
		}

		// TODO: we need to do this, but smarter, ie, most of
		// the time we set the same value for a given field but
		// sometimes (rarely) we change it up:
		/*
		if (newType.omitNorms()) {
		  newType.setOmitNorms(random.nextBoolean());
		}
		*/

		return new Field(name, value, newType);
	  }

	  /// <summary>
	  /// Return a random Locale from the available locales on the system. </summary>
	  /// <seealso cref= "https://issues.apache.org/jira/browse/LUCENE-4020" </seealso>
	  public static Locale RandomLocale(Random random)
	  {
		Locale[] locales = Locale.AvailableLocales;
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
	  }

	  public static bool DefaultCodecSupportsDocValues()
	  {
		return !Codec.Default.Name.Equals("Lucene3x");
	  }

	  private static Directory NewFSDirectoryImpl(Type clazz, File file) throws IOException
	  {
		FSDirectory d = null;
		try
		{
		  d = CommandLineUtil.newFSDirectory(clazz, file);
		}
		catch (Exception e)
		{
		  Rethrow.Rethrow(e);
		}
		return d;
	  }

	  static Directory NewDirectoryImpl(Random random, string clazzName)
	  {
		if (clazzName.Equals("random"))
		{
		  if (Rarely(random))
		  {
			clazzName = RandomPicks.randomFrom(random, CORE_DIRECTORIES);
		  }
		  else
		  {
			clazzName = "RAMDirectory";
		  }
		}

		try
		{
		  Type clazz = CommandLineUtil.loadDirectoryClass(clazzName);
		  // If it is a FSDirectory type, try its ctor(File)
		  if (clazz.IsSubclassOf(typeof(FSDirectory)))
		  {
			File dir = CreateTempDir("index-" + clazzName);
			dir.mkdirs(); // ensure it's created so we 'have' it.
			return NewFSDirectoryImpl(clazz.asSubclass(typeof(FSDirectory)), dir);
		  }

		  // try empty ctor
		  return clazz.newInstance();
		}
		catch (Exception e)
		{
		  Rethrow.Rethrow(e);
		  throw null; // dummy to prevent compiler failure
		}
	  }

	  /// <summary>
	  /// Sometimes wrap the IndexReader as slow, parallel or filter reader (or
	  /// combinations of that)
	  /// </summary>
	  public static IndexReader MaybeWrapReader(IndexReader r) throws IOException
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
				r = SlowCompositeReaderWrapper.wrap(r);
				break;
			  case 1:
				// will create no FC insanity in atomic case, as ParallelAtomicReader has own cache key:
				r = (r is AtomicReader) ? new ParallelAtomicReader((AtomicReader) r) : new ParallelCompositeReader((CompositeReader) r);
				break;
			  case 2:
				// Häckidy-Hick-Hack: a standard MultiReader will cause FC insanity, so we use
				// QueryUtils' reader with a fake cache key, so insanity checker cannot walk
				// along our reader:
				r = new FCInvisibleMultiReader(r);
				break;
			  case 3:
				AtomicReader ar = SlowCompositeReaderWrapper.wrap(r);
				IList<string> allFields = new List<string>();
				foreach (FieldInfo fi in ar.FieldInfos)
				{
				  allFields.Add(fi.name);
				}
				Collections.shuffle(allFields, random);
				int end = allFields.Count == 0 ? 0 : random.Next(allFields.Count);
				Set<string> fields = new HashSet<string>(allFields.subList(0, end));
				// will create no FC insanity as ParallelAtomicReader has own cache key:
				r = new ParallelAtomicReader(new FieldFilterAtomicReader(ar, fields, false), new FieldFilterAtomicReader(ar, fields, true)
			   );
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
			r = SlowCompositeReaderWrapper.wrap(r);
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
		if (oldContext.flushInfo != null)
		{
		  // Always return at least the estimatedSegmentSize of
		  // the incoming IOContext:
		  return new IOContext(new FlushInfo(randomNumDocs, Math.Max(oldContext.flushInfo.estimatedSegmentSize, size)));
		}
		else if (oldContext.mergeInfo != null)
		{
		  // Always return at least the estimatedMergeBytes of
		  // the incoming IOContext:
		  return new IOContext(new MergeInfo(randomNumDocs, Math.Max(oldContext.mergeInfo.estimatedMergeBytes, size), random.nextBoolean(), TestUtil.NextInt(random, 1, 100)));
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
	  /// </summary>
	  public static IndexSearcher NewSearcher(IndexReader r)
	  {
		return NewSearcher(r, true);
	  }

	  /// <summary>
	  /// Create a new searcher over the reader. this searcher might randomly use
	  /// threads.
	  /// </summary>
	  public static IndexSearcher NewSearcher(IndexReader r, bool maybeWrap)
	  {
		return NewSearcher(r, maybeWrap, true);
	  }

	  /// <summary>
	  /// Create a new searcher over the reader. this searcher might randomly use
	  /// threads. if <code>maybeWrap</code> is true, this searcher might wrap the
	  /// reader with one that returns null for getSequentialSubReaders. If
	  /// <code>wrapWithAssertions</code> is true, this searcher might be an
	  /// <seealso cref="AssertingIndexSearcher"/> instance.
	  /// </summary>
	  public static IndexSearcher NewSearcher(IndexReader r, bool maybeWrap, bool wrapWithAssertions)
	  {
		Random random = Random();
		if (Usually())
		{
		  if (maybeWrap)
		  {
			try
			{
			  r = MaybeWrapReader(r);
			}
			catch (IOException e)
			{
			  Rethrow.Rethrow(e);
			}
		  }
		  // TODO: this whole check is a coverage hack, we should move it to tests for various filterreaders.
		  // ultimately whatever you do will be checkIndex'd at the end anyway. 
		  if (random.Next(500) == 0 && r is AtomicReader)
		  {
			// TODO: not useful to check DirectoryReader (redundant with checkindex)
			// but maybe sometimes run this on the other crazy readers maybeWrapReader creates?
			try
			{
			  TestUtil.CheckReader(r);
			}
			catch (IOException e)
			{
			  Rethrow.Rethrow(e);
			}
		  }
		  IndexSearcher ret;
		  if (wrapWithAssertions)
		  {
			ret = random.nextBoolean() ? new AssertingIndexSearcher(random, r) : new AssertingIndexSearcher(random, r.Context);
		  }
		  else
		  {
			ret = random.nextBoolean() ? new IndexSearcher(r) : new IndexSearcher(r.Context);
		  }
		  ret.Similarity = ClassEnvRule.Similarity;
		  return ret;
		}
		else
		{
		  int threads = 0;
		  ThreadPoolExecutor ex;
		  if (random.nextBoolean())
		  {
			ex = null;
		  }
		  else
		  {
			threads = TestUtil.NextInt(random, 1, 8);
			ex = new ThreadPoolExecutor(threads, threads, 0L, TimeUnit.MILLISECONDS, new LinkedBlockingQueue<Runnable>(), new NamedThreadFactory("LuceneTestCase"));
			// uncomment to intensify LUCENE-3840
			// ex.prestartAllCoreThreads();
		  }
		  if (ex != null)
		  {
		   if (VERBOSE)
		   {
			Console.WriteLine("NOTE: newSearcher using ExecutorService with " + threads + " threads");
		   }
		   r.addReaderClosedListener(new ReaderClosedListenerAnonymousInnerClassHelper(this, ex));
		  }
		  IndexSearcher ret;
		  if (wrapWithAssertions)
		  {
			ret = random.nextBoolean() ? new AssertingIndexSearcher(random, r, ex) : new AssertingIndexSearcher(random, r.Context, ex);
		  }
		  else
		  {
			ret = random.nextBoolean() ? new IndexSearcher(r, ex) : new IndexSearcher(r.Context, ex);
		  }
		  ret.Similarity = ClassEnvRule.Similarity;
		  return ret;
		}
	  }

	  /// <summary>
	  /// Gets a resource from the classpath as <seealso cref="File"/>. this method should only
	  /// be used, if a real file is needed. To get a stream, code should prefer
	  /// <seealso cref="Class#getResourceAsStream"/> using {@code this.getClass()}.
	  /// </summary>
	  protected File GetDataFile(string name) throws IOException
	  {
		try
		{
		  return new File(this.GetType().getResource(name).toURI());
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

	  public void assertReaderEquals(string info, IndexReader leftReader, IndexReader rightReader) throws IOException
	  {
		AssertReaderStatisticsEquals(info, leftReader, rightReader);
		AssertFieldsEquals(info, leftReader, MultiFields.getFields(leftReader), MultiFields.getFields(rightReader), true);
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
	  public void assertReaderStatisticsEquals(string info, IndexReader leftReader, IndexReader rightReader) throws IOException
	  {
		// Somewhat redundant: we never delete docs
		Assert.AreEqual(info, leftReader.maxDoc(), rightReader.maxDoc());
		Assert.AreEqual(info, leftReader.numDocs(), rightReader.numDocs());
		Assert.AreEqual(info, leftReader.numDeletedDocs(), rightReader.numDeletedDocs());
		Assert.AreEqual(info, leftReader.hasDeletions(), rightReader.hasDeletions());
	  }

	  /// <summary>
	  /// Fields api equivalency 
	  /// </summary>
	  public void assertFieldsEquals(string info, IndexReader leftReader, Fields leftFields, Fields rightFields, bool deep) throws IOException
	  {
		// Fields could be null if there are no postings,
		// but then it must be null for both
		if (leftFields == null || rightFields == null)
		{
		  assertNull(info, leftFields);
		  assertNull(info, rightFields);
		  return;
		}
		AssertFieldStatisticsEquals(info, leftFields, rightFields);

		IEnumerator<string> leftEnum = leftFields.GetEnumerator();
		IEnumerator<string> rightEnum = rightFields.GetEnumerator();

		while (leftEnum.MoveNext())
		{
		  string field = leftEnum.Current;
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Assert.AreEqual(info, field, rightEnum.next());
		  AssertTermsEquals(info, leftReader, leftFields.terms(field), rightFields.terms(field), deep);
		}
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(rightEnum.hasNext());
	  }

	  /// <summary>
	  /// checks that top-level statistics on Fields are the same 
	  /// </summary>
	  public void assertFieldStatisticsEquals(string info, Fields leftFields, Fields rightFields) throws IOException
	  {
		if (leftFields.size() != -1 && rightFields.size() != -1)
		{
		  Assert.AreEqual(info, leftFields.size(), rightFields.size());
		}
	  }

	  /// <summary>
	  /// Terms api equivalency 
	  /// </summary>
	  public void assertTermsEquals(string info, IndexReader leftReader, Terms leftTerms, Terms rightTerms, bool deep) throws IOException
	  {
		if (leftTerms == null || rightTerms == null)
		{
		  assertNull(info, leftTerms);
		  assertNull(info, rightTerms);
		  return;
		}
		AssertTermsStatisticsEquals(info, leftTerms, rightTerms);
		Assert.AreEqual(leftTerms.hasOffsets(), rightTerms.hasOffsets());
		Assert.AreEqual(leftTerms.hasPositions(), rightTerms.hasPositions());
		Assert.AreEqual(leftTerms.hasPayloads(), rightTerms.hasPayloads());

		TermsEnum leftTermsEnum = leftTerms.iterator(null);
		TermsEnum rightTermsEnum = rightTerms.iterator(null);
		AssertTermsEnumEquals(info, leftReader, leftTermsEnum, rightTermsEnum, true);

		AssertTermsSeekingEquals(info, leftTerms, rightTerms);

		if (deep)
		{
		  int numIntersections = AtLeast(3);
		  for (int i = 0; i < numIntersections; i++)
		  {
			string re = AutomatonTestUtil.RandomRegexp(Random());
			CompiledAutomaton automaton = new CompiledAutomaton((new RegExp(re, RegExp.NONE)).toAutomaton());
			if (automaton.type == CompiledAutomaton.AUTOMATON_TYPE.NORMAL)
			{
			  // TODO: test start term too
			  TermsEnum leftIntersection = leftTerms.intersect(automaton, null);
			  TermsEnum rightIntersection = rightTerms.intersect(automaton, null);
			  AssertTermsEnumEquals(info, leftReader, leftIntersection, rightIntersection, Rarely());
			}
		  }
		}
	  }

	  /// <summary>
	  /// checks collection-level statistics on Terms 
	  /// </summary>
	  public void assertTermsStatisticsEquals(string info, Terms leftTerms, Terms rightTerms) throws IOException
	  {
		Debug.Assert(leftTerms.Comparator == rightTerms.Comparator);
		if (leftTerms.DocCount != -1 && rightTerms.DocCount != -1)
		{
		  Assert.AreEqual(info, leftTerms.DocCount, rightTerms.DocCount);
		}
		if (leftTerms.SumDocFreq != -1 && rightTerms.SumDocFreq != -1)
		{
		  Assert.AreEqual(info, leftTerms.SumDocFreq, rightTerms.SumDocFreq);
		}
		if (leftTerms.SumTotalTermFreq != -1 && rightTerms.SumTotalTermFreq != -1)
		{
		  Assert.AreEqual(info, leftTerms.SumTotalTermFreq, rightTerms.SumTotalTermFreq);
		}
		if (leftTerms.size() != -1 && rightTerms.size() != -1)
		{
		  Assert.AreEqual(info, leftTerms.size(), rightTerms.size());
		}
	  }

	  private static class RandomBits implements Bits
	  {
		FixedBitSet bits;

		RandomBits(int maxDoc, double pctLive, Random random)
		{
		  bits = new FixedBitSet(maxDoc);
		  for (int i = 0; i < maxDoc; i++)
		  {
			if (random.NextDouble() <= pctLive)
			{
			  bits.set(i);
			}
		  }
		}

		public bool get(int index)
		{
		  return bits.get(index);
		}

		public int length()
		{
		  return bits.length();
		}
	  }

	  /// <summary>
	  /// checks the terms enum sequentially
	  /// if deep is false, it does a 'shallow' test that doesnt go down to the docsenums
	  /// </summary>
	  public void assertTermsEnumEquals(string info, IndexReader leftReader, TermsEnum leftTermsEnum, TermsEnum rightTermsEnum, bool deep) throws IOException
	  {
		BytesRef term;
		Bits randomBits = new RandomBits(leftReader.maxDoc(), Random().NextDouble(), Random());
		DocsAndPositionsEnum leftPositions = null;
		DocsAndPositionsEnum rightPositions = null;
		DocsEnum leftDocs = null;
		DocsEnum rightDocs = null;

		while ((term = leftTermsEnum.next()) != null)
		{
		  Assert.AreEqual(info, term, rightTermsEnum.next());
		  AssertTermStatsEquals(info, leftTermsEnum, rightTermsEnum);
		  if (deep)
		  {
			AssertDocsAndPositionsEnumEquals(info, leftPositions = leftTermsEnum.docsAndPositions(null, leftPositions), rightPositions = rightTermsEnum.docsAndPositions(null, rightPositions));
			AssertDocsAndPositionsEnumEquals(info, leftPositions = leftTermsEnum.docsAndPositions(randomBits, leftPositions), rightPositions = rightTermsEnum.docsAndPositions(randomBits, rightPositions));

			AssertPositionsSkippingEquals(info, leftReader, leftTermsEnum.docFreq(), leftPositions = leftTermsEnum.docsAndPositions(null, leftPositions), rightPositions = rightTermsEnum.docsAndPositions(null, rightPositions));
			AssertPositionsSkippingEquals(info, leftReader, leftTermsEnum.docFreq(), leftPositions = leftTermsEnum.docsAndPositions(randomBits, leftPositions), rightPositions = rightTermsEnum.docsAndPositions(randomBits, rightPositions));

			// with freqs:
			AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.docs(null, leftDocs), rightDocs = rightTermsEnum.docs(null, rightDocs), true);
			AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.docs(randomBits, leftDocs), rightDocs = rightTermsEnum.docs(randomBits, rightDocs), true);

			// w/o freqs:
			AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.docs(null, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.docs(null, rightDocs, DocsEnum.FLAG_NONE), false);
			AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.docs(randomBits, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.docs(randomBits, rightDocs, DocsEnum.FLAG_NONE), false);

			// with freqs:
			AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.docFreq(), leftDocs = leftTermsEnum.docs(null, leftDocs), rightDocs = rightTermsEnum.docs(null, rightDocs), true);
			AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.docFreq(), leftDocs = leftTermsEnum.docs(randomBits, leftDocs), rightDocs = rightTermsEnum.docs(randomBits, rightDocs), true);

			// w/o freqs:
			AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.docFreq(), leftDocs = leftTermsEnum.docs(null, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.docs(null, rightDocs, DocsEnum.FLAG_NONE), false);
			AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.docFreq(), leftDocs = leftTermsEnum.docs(randomBits, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.docs(randomBits, rightDocs, DocsEnum.FLAG_NONE), false);
		  }
		}
		assertNull(info, rightTermsEnum.next());
	  }


	  /// <summary>
	  /// checks docs + freqs + positions + payloads, sequentially
	  /// </summary>
	  public void assertDocsAndPositionsEnumEquals(string info, DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs) throws IOException
	  {
		if (leftDocs == null || rightDocs == null)
		{
		  assertNull(leftDocs);
		  assertNull(rightDocs);
		  return;
		}
		Assert.AreEqual(info, -1, leftDocs.docID());
		Assert.AreEqual(info, -1, rightDocs.docID());
		int docid;
		while ((docid = leftDocs.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
		{
		  Assert.AreEqual(info, docid, rightDocs.nextDoc());
		  int freq = leftDocs.freq();
		  Assert.AreEqual(info, freq, rightDocs.freq());
		  for (int i = 0; i < freq; i++)
		  {
			Assert.AreEqual(info, leftDocs.nextPosition(), rightDocs.nextPosition());
			Assert.AreEqual(info, leftDocs.Payload, rightDocs.Payload);
			Assert.AreEqual(info, leftDocs.StartOffset(), rightDocs.StartOffset());
			Assert.AreEqual(info, leftDocs.EndOffset(), rightDocs.EndOffset());
		  }
		}
		Assert.AreEqual(info, DocIdSetIterator.NO_MORE_DOCS, rightDocs.nextDoc());
	  }

	  /// <summary>
	  /// checks docs + freqs, sequentially
	  /// </summary>
	  public void assertDocsEnumEquals(string info, DocsEnum leftDocs, DocsEnum rightDocs, bool hasFreqs) throws IOException
	  {
		if (leftDocs == null)
		{
		  assertNull(rightDocs);
		  return;
		}
		Assert.AreEqual(info, -1, leftDocs.docID());
		Assert.AreEqual(info, -1, rightDocs.docID());
		int docid;
		while ((docid = leftDocs.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
		{
		  Assert.AreEqual(info, docid, rightDocs.nextDoc());
		  if (hasFreqs)
		  {
			Assert.AreEqual(info, leftDocs.freq(), rightDocs.freq());
		  }
		}
		Assert.AreEqual(info, DocIdSetIterator.NO_MORE_DOCS, rightDocs.nextDoc());
	  }

	  /// <summary>
	  /// checks advancing docs
	  /// </summary>
	  public void assertDocsSkippingEquals(string info, IndexReader leftReader, int docFreq, DocsEnum leftDocs, DocsEnum rightDocs, bool hasFreqs) throws IOException
	  {
		if (leftDocs == null)
		{
		  assertNull(rightDocs);
		  return;
		}
		int docid = -1;
		int averageGap = leftReader.maxDoc() / (1 + docFreq);
		int skipInterval = 16;

		while (true)
		{
		  if (Random().nextBoolean())
		  {
			// nextDoc()
			docid = leftDocs.nextDoc();
			Assert.AreEqual(info, docid, rightDocs.nextDoc());
		  }
		  else
		  {
			// advance()
			int skip = docid + (int) Math.Ceiling(Math.Abs(skipInterval + Random().nextGaussian() * averageGap));
			docid = leftDocs.advance(skip);
			Assert.AreEqual(info, docid, rightDocs.advance(skip));
		  }

		  if (docid == DocIdSetIterator.NO_MORE_DOCS)
		  {
			return;
		  }
		  if (hasFreqs)
		  {
			Assert.AreEqual(info, leftDocs.freq(), rightDocs.freq());
		  }
		}
	  }

	  /// <summary>
	  /// checks advancing docs + positions
	  /// </summary>
	  public void assertPositionsSkippingEquals(string info, IndexReader leftReader, int docFreq, DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs) throws IOException
	  {
		if (leftDocs == null || rightDocs == null)
		{
		  assertNull(leftDocs);
		  assertNull(rightDocs);
		  return;
		}

		int docid = -1;
		int averageGap = leftReader.maxDoc() / (1 + docFreq);
		int skipInterval = 16;

		while (true)
		{
		  if (Random().nextBoolean())
		  {
			// nextDoc()
			docid = leftDocs.nextDoc();
			Assert.AreEqual(info, docid, rightDocs.nextDoc());
		  }
		  else
		  {
			// advance()
			int skip = docid + (int) Math.Ceiling(Math.Abs(skipInterval + Random().nextGaussian() * averageGap));
			docid = leftDocs.advance(skip);
			Assert.AreEqual(info, docid, rightDocs.advance(skip));
		  }

		  if (docid == DocIdSetIterator.NO_MORE_DOCS)
		  {
			return;
		  }
		  int freq = leftDocs.freq();
		  Assert.AreEqual(info, freq, rightDocs.freq());
		  for (int i = 0; i < freq; i++)
		  {
			Assert.AreEqual(info, leftDocs.nextPosition(), rightDocs.nextPosition());
			Assert.AreEqual(info, leftDocs.Payload, rightDocs.Payload);
		  }
		}
	  }


	  private void assertTermsSeekingEquals(string info, Terms leftTerms, Terms rightTerms) throws IOException
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
		  leftEnum = leftTerms.iterator(leftEnum);
		  BytesRef term = null;
		  while ((term = leftEnum.next()) != null)
		  {
			int code = random.Next(10);
			if (code == 0)
			{
			  // the term
			  tests.Add(BytesRef.deepCopyOf(term));
			}
			else if (code == 1)
			{
			  // truncated subsequence of term
			  term = BytesRef.deepCopyOf(term);
			  if (term.length > 0)
			  {
				// truncate it
				term.length = random.Next(term.length);
			  }
			}
			else if (code == 2)
			{
			  // term, but ensure a non-zero offset
			  sbyte[] newbytes = new sbyte[term.length + 5];
			  Array.Copy(term.bytes, term.offset, newbytes, 5, term.length);
			  tests.Add(new BytesRef(newbytes, 5, term.length));
			}
			else if (code == 3)
			{
			  switch (Random().Next(3))
			  {
				case 0:
				  tests.Add(new BytesRef()); // before the first term
				  break;
				case 1:
				  tests.Add(new BytesRef(new sbyte[] {unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF)})); // past the last term
				  break;
				case 2:
				  tests.Add(new BytesRef(TestUtil.RandomSimpleString(Random()))); // random term
				  break;
				default:
				  throw new AssertionError();
			  }
			}
		  }
		  numPasses++;
		}

		rightEnum = rightTerms.iterator(rightEnum);

		List<BytesRef> shuffledTests = new List<BytesRef>(tests);
		Collections.shuffle(shuffledTests, random);

		foreach (BytesRef b in shuffledTests)
		{
		  if (Rarely())
		  {
			// reuse the enums
			leftEnum = leftTerms.iterator(leftEnum);
			rightEnum = rightTerms.iterator(rightEnum);
		  }

		  bool seekExact = Random().nextBoolean();

		  if (seekExact)
		  {
			Assert.AreEqual(info, leftEnum.seekExact(b), rightEnum.seekExact(b));
		  }
		  else
		  {
			TermsEnum.SeekStatus leftStatus = leftEnum.seekCeil(b);
			TermsEnum.SeekStatus rightStatus = rightEnum.seekCeil(b);
			Assert.AreEqual(info, leftStatus, rightStatus);
			if (leftStatus != TermsEnum.SeekStatus.END)
			{
			  Assert.AreEqual(info, leftEnum.term(), rightEnum.term());
			  AssertTermStatsEquals(info, leftEnum, rightEnum);
			}
		  }
		}
	  }

	  /// <summary>
	  /// checks term-level statistics
	  /// </summary>
	  public void assertTermStatsEquals(string info, TermsEnum leftTermsEnum, TermsEnum rightTermsEnum) throws IOException
	  {
		Assert.AreEqual(info, leftTermsEnum.docFreq(), rightTermsEnum.docFreq());
		if (leftTermsEnum.totalTermFreq() != -1 && rightTermsEnum.totalTermFreq() != -1)
		{
		  Assert.AreEqual(info, leftTermsEnum.totalTermFreq(), rightTermsEnum.totalTermFreq());
		}
	  }

	  /// <summary>
	  /// checks that norms are the same across all fields 
	  /// </summary>
	  public void assertNormsEquals(string info, IndexReader leftReader, IndexReader rightReader) throws IOException
	  {
		Fields leftFields = MultiFields.getFields(leftReader);
		Fields rightFields = MultiFields.getFields(rightReader);
		// Fields could be null if there are no postings,
		// but then it must be null for both
		if (leftFields == null || rightFields == null)
		{
		  assertNull(info, leftFields);
		  assertNull(info, rightFields);
		  return;
		}

		foreach (string field in leftFields)
		{
		  NumericDocValues leftNorms = MultiDocValues.getNormValues(leftReader, field);
		  NumericDocValues rightNorms = MultiDocValues.getNormValues(rightReader, field);
		  if (leftNorms != null && rightNorms != null)
		  {
			AssertDocValuesEquals(info, leftReader.maxDoc(), leftNorms, rightNorms);
		  }
		  else
		  {
			assertNull(info, leftNorms);
			assertNull(info, rightNorms);
		  }
		}
	  }

	  /// <summary>
	  /// checks that stored fields of all documents are the same 
	  /// </summary>
	  public void assertStoredFieldsEquals(string info, IndexReader leftReader, IndexReader rightReader) throws IOException
	  {
		Debug.Assert(leftReader.maxDoc() == rightReader.maxDoc());
		for (int i = 0; i < leftReader.maxDoc(); i++)
		{
		  Document leftDoc = leftReader.document(i);
		  Document rightDoc = rightReader.document(i);

		  // TODO: I think this is bogus because we don't document what the order should be
		  // from these iterators, etc. I think the codec/IndexReader should be free to order this stuff
		  // in whatever way it wants (e.g. maybe it packs related fields together or something)
		  // To fix this, we sort the fields in both documents by name, but
		  // we still assume that all instances with same name are in order:
		  IComparer<IndexableField> comp = new ComparatorAnonymousInnerClassHelper(this);
		  Collections.sort(leftDoc.Fields, comp);
		  Collections.sort(rightDoc.Fields, comp);

		  IEnumerator<IndexableField> leftIterator = leftDoc.GetEnumerator();
		  IEnumerator<IndexableField> rightIterator = rightDoc.GetEnumerator();
		  while (leftIterator.MoveNext())
		  {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			Assert.IsTrue(info, rightIterator.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			AssertStoredFieldEquals(info, leftIterator.Current, rightIterator.next());
		  }
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Assert.IsFalse(info, rightIterator.hasNext());
		}
	  }

	  /// <summary>
	  /// checks that two stored fields are equivalent 
	  /// </summary>
	  public void assertStoredFieldEquals(string info, IndexableField leftField, IndexableField rightField)
	  {
		Assert.AreEqual(info, leftField.name(), rightField.name());
		Assert.AreEqual(info, leftField.binaryValue(), rightField.binaryValue());
		Assert.AreEqual(info, leftField.stringValue(), rightField.stringValue());
		Assert.AreEqual(info, leftField.numericValue(), rightField.numericValue());
		// TODO: should we check the FT at all?
	  }

	  /// <summary>
	  /// checks that term vectors across all fields are equivalent 
	  /// </summary>
	  public void assertTermVectorsEquals(string info, IndexReader leftReader, IndexReader rightReader) throws IOException
	  {
		Debug.Assert(leftReader.maxDoc() == rightReader.maxDoc());
		for (int i = 0; i < leftReader.maxDoc(); i++)
		{
		  Fields leftFields = leftReader.getTermVectors(i);
		  Fields rightFields = rightReader.getTermVectors(i);
		  AssertFieldsEquals(info, leftReader, leftFields, rightFields, Rarely());
		}
	  }

	  private static Set<string> GetDVFields(IndexReader reader)
	  {
		Set<string> fields = new HashSet<string>();
		foreach (FieldInfo fi in MultiFields.getMergedFieldInfos(reader))
		{
		  if (fi.hasDocValues())
		  {
			fields.add(fi.name);
		  }
		}

		return fields;
	  }

	  /// <summary>
	  /// checks that docvalues across all fields are equivalent
	  /// </summary>
	  public void assertDocValuesEquals(string info, IndexReader leftReader, IndexReader rightReader) throws IOException
	  {
		Set<string> leftFields = GetDVFields(leftReader);
		Set<string> rightFields = GetDVFields(rightReader);
		Assert.AreEqual(info, leftFields, rightFields);

		foreach (string field in leftFields)
		{
		  // TODO: clean this up... very messy
		{
			NumericDocValues leftValues = MultiDocValues.getNumericValues(leftReader, field);
			NumericDocValues rightValues = MultiDocValues.getNumericValues(rightReader, field);
			if (leftValues != null && rightValues != null)
			{
			  AssertDocValuesEquals(info, leftReader.maxDoc(), leftValues, rightValues);
			}
			else
			{
			  assertNull(info, leftValues);
			  assertNull(info, rightValues);
			}
		  }

		  {
			BinaryDocValues leftValues = MultiDocValues.getBinaryValues(leftReader, field);
			BinaryDocValues rightValues = MultiDocValues.getBinaryValues(rightReader, field);
			if (leftValues != null && rightValues != null)
			{
			  BytesRef scratchLeft = new BytesRef();
			  BytesRef scratchRight = new BytesRef();
			  for (int docID = 0;docID < leftReader.maxDoc();docID++)
			  {
				leftValues.get(docID, scratchLeft);
				rightValues.get(docID, scratchRight);
				Assert.AreEqual(info, scratchLeft, scratchRight);
			  }
			}
			else
			{
			  assertNull(info, leftValues);
			  assertNull(info, rightValues);
			}
		  }

		  {
			SortedDocValues leftValues = MultiDocValues.getSortedValues(leftReader, field);
			SortedDocValues rightValues = MultiDocValues.getSortedValues(rightReader, field);
			if (leftValues != null && rightValues != null)
			{
			  // numOrds
			  Assert.AreEqual(info, leftValues.ValueCount, rightValues.ValueCount);
			  // ords
			  BytesRef scratchLeft = new BytesRef();
			  BytesRef scratchRight = new BytesRef();
			  for (int i = 0; i < leftValues.ValueCount; i++)
			  {
				leftValues.lookupOrd(i, scratchLeft);
				rightValues.lookupOrd(i, scratchRight);
				Assert.AreEqual(info, scratchLeft, scratchRight);
			  }
			  // bytes
			  for (int docID = 0;docID < leftReader.maxDoc();docID++)
			  {
				leftValues.get(docID, scratchLeft);
				rightValues.get(docID, scratchRight);
				Assert.AreEqual(info, scratchLeft, scratchRight);
			  }
			}
			else
			{
			  assertNull(info, leftValues);
			  assertNull(info, rightValues);
			}
		  }

		  {
			SortedSetDocValues leftValues = MultiDocValues.getSortedSetValues(leftReader, field);
			SortedSetDocValues rightValues = MultiDocValues.getSortedSetValues(rightReader, field);
			if (leftValues != null && rightValues != null)
			{
			  // numOrds
			  Assert.AreEqual(info, leftValues.ValueCount, rightValues.ValueCount);
			  // ords
			  BytesRef scratchLeft = new BytesRef();
			  BytesRef scratchRight = new BytesRef();
			  for (int i = 0; i < leftValues.ValueCount; i++)
			  {
				leftValues.lookupOrd(i, scratchLeft);
				rightValues.lookupOrd(i, scratchRight);
				Assert.AreEqual(info, scratchLeft, scratchRight);
			  }
			  // ord lists
			  for (int docID = 0;docID < leftReader.maxDoc();docID++)
			  {
				leftValues.Document = docID;
				rightValues.Document = docID;
				long ord;
				while ((ord = leftValues.nextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
				{
				  Assert.AreEqual(info, ord, rightValues.nextOrd());
				}
				Assert.AreEqual(info, SortedSetDocValues.NO_MORE_ORDS, rightValues.nextOrd());
			  }
			}
			else
			{
			  assertNull(info, leftValues);
			  assertNull(info, rightValues);
			}
		  }

		  {
			Bits leftBits = MultiDocValues.getDocsWithField(leftReader, field);
			Bits rightBits = MultiDocValues.getDocsWithField(rightReader, field);
			if (leftBits != null && rightBits != null)
			{
			  Assert.AreEqual(info, leftBits.length(), rightBits.length());
			  for (int i = 0; i < leftBits.length(); i++)
			  {
				Assert.AreEqual(info, leftBits.get(i), rightBits.get(i));
			  }
			}
			else
			{
			  assertNull(info, leftBits);
			  assertNull(info, rightBits);
			}
		  }
		}
	  }

	  public void assertDocValuesEquals(string info, int num, NumericDocValues leftDocValues, NumericDocValues rightDocValues) throws IOException
	  {
		Assert.IsNotNull(info, leftDocValues);
		Assert.IsNotNull(info, rightDocValues);
		for (int docID = 0;docID < num;docID++)
		{
		  Assert.AreEqual(leftDocValues.get(docID), rightDocValues.get(docID));
		}
	  }

	  // TODO: this is kinda stupid, we don't delete documents in the test.
	  public void assertDeletedDocsEquals(string info, IndexReader leftReader, IndexReader rightReader) throws IOException
	  {
		Debug.Assert(leftReader.numDeletedDocs() == rightReader.numDeletedDocs());
		Bits leftBits = MultiFields.getLiveDocs(leftReader);
		Bits rightBits = MultiFields.getLiveDocs(rightReader);

		if (leftBits == null || rightBits == null)
		{
		  assertNull(info, leftBits);
		  assertNull(info, rightBits);
		  return;
		}

		Debug.Assert(leftReader.maxDoc() == rightReader.maxDoc());
		Assert.AreEqual(info, leftBits.length(), rightBits.length());
		for (int i = 0; i < leftReader.maxDoc(); i++)
		{
		  Assert.AreEqual(info, leftBits.get(i), rightBits.get(i));
		}
	  }

	  public void assertFieldInfosEquals(string info, IndexReader leftReader, IndexReader rightReader) throws IOException
	  {
		FieldInfos leftInfos = MultiFields.getMergedFieldInfos(leftReader);
		FieldInfos rightInfos = MultiFields.getMergedFieldInfos(rightReader);

		// TODO: would be great to verify more than just the names of the fields!
		SortedSet<string> left = new SortedSet<string>();
		SortedSet<string> right = new SortedSet<string>();

		foreach (FieldInfo fi in leftInfos)
		{
		  left.Add(fi.name);
		}

		foreach (FieldInfo fi in rightInfos)
		{
		  right.Add(fi.name);
		}

		Assert.AreEqual(info, left, right);
	  }

	  /// <summary>
	  /// Returns true if the file exists (can be opened), false
	  ///  if it cannot be opened, and (unlike Java's
	  ///  File.exists) throws IOException if there's some
	  ///  unexpected error. 
	  /// </summary>
	  public static bool SlowFileExists(Directory dir, string fileName) throws IOException
	  {
		try
		{
		  dir.openInput(fileName, IOContext.DEFAULT).close();
		  return true;
		}
//JAVA TO C# CONVERTER TODO TASK: There is no equivalent in C# to Java 'multi-catch' syntax:
		catch (NoSuchFileException | FileNotFoundException e)
		{
		  return false;
		}
	  }

	  /// <summary>
	  /// A base location for temporary files of a given test. Helps in figuring out
	  /// which tests left which files and where.
	  /// </summary>
	  private static File TempDirBase;

	  /// <summary>
	  /// Retry to create temporary file name this many times.
	  /// </summary>
	  private static final int TEMP_NAME_RETRY_THRESHOLD = 9999;

	  /// <summary>
	  /// this method is deprecated for a reason. Do not use it. Call <seealso cref="#createTempDir()"/>
	  /// or <seealso cref="#createTempDir(String)"/> or <seealso cref="#createTempFile(String, String)"/>.
	  /// </summary>
	  [Obsolete]
	  public static File BaseTempDirForTestClass
	  {
		lock (typeof(LuceneTestCase))
		{
		  if (TempDirBase == null)
		  {
			File directory = new File(System.getProperty("tempDir", System.getProperty("java.io.tmpdir")));
			Debug.Assert(directory.exists() && directory.Directory && directory.canWrite());

			RandomizedContext ctx = RandomizedContext.current();
			Type clazz = ctx.TargetClass;
			string prefix = clazz.Name;
			prefix = prefix.replaceFirst("^org.apache.lucene.", "lucene.");
			prefix = prefix.replaceFirst("^org.apache.solr.", "solr.");

			int attempt = 0;
			File f;
			do
			{
			  if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
			  {
				throw new Exception("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: " + directory.AbsolutePath);
			  }
			  f = new File(directory, prefix + "-" + ctx.RunnerSeedAsString + "-" + string.format(Locale.ENGLISH, "%03d", attempt));
			} while (!f.mkdirs());

			TempDirBase = f;
			RegisterToRemoveAfterSuite(TempDirBase);
		  }
		}
		return TempDirBase;
	  }


	  /// <summary>
	  /// Creates an empty, temporary folder (when the name of the folder is of no importance).
	  /// </summary>
	  /// <seealso cref= #createTempDir(String) </seealso>
	  public static File CreateTempDir()
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
	  public static File CreateTempDir(string prefix)
	  {
		File @base = BaseTempDirForTestClass;

		int attempt = 0;
		File f;
		do
		{
		  if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
		  {
			throw new Exception("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: " + @base.AbsolutePath);
		  }
		  f = new File(@base, prefix + "-" + string.format(Locale.ENGLISH, "%03d", attempt));
		} while (!f.mkdirs());

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
	  public static File CreateTempFile(string prefix, string suffix) throws IOException
	  {
		File @base = BaseTempDirForTestClass;

		int attempt = 0;
		File f;
		do
		{
		  if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
		  {
			throw new Exception("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: " + @base.AbsolutePath);
		  }
		  f = new File(@base, prefix + "-" + string.format(Locale.ENGLISH, "%03d", attempt) + suffix);
		} while (!f.createNewFile());

		RegisterToRemoveAfterSuite(f);
		return f;
	  }

	  /// <summary>
	  /// Creates an empty temporary file.
	  /// </summary>
	  /// <seealso cref= #createTempFile(String, String)  </seealso>
	  public static File CreateTempFile() throws IOException
	  {
		return CreateTempFile("tempFile", ".tmp");
	  }

	  /// <summary>
	  /// A queue of temporary resources to be removed after the
	  /// suite completes. </summary>
	  /// <seealso cref= #registerToRemoveAfterSuite(File) </seealso>
	  private final static IList<File> CleanupQueue = new List<File>();

	  /// <summary>
	  /// Register temporary folder for removal after the suite completes.
	  /// </summary>
	  private static void registerToRemoveAfterSuite(File f)
	  {
		Debug.Assert(f != null);

		if (LuceneTestCase.LEAVE_TEMPORARY)
		{
		  Console.Error.WriteLine("INFO: Will leave temporary file: " + f.AbsolutePath);
		  return;
		}

		lock (CleanupQueue)
		{
		  CleanupQueue.Add(f);
		}
	  }

	  private static class TemporaryFilesCleanupRule : TestRuleAdapter
	  {
		protected void before() throws Exception
		{
		  base.before();
		  Debug.Assert(TempDirBase == null);
		}

		protected void afterAlways(IList<Exception> errors) throws Exception
		{
		  // Drain cleanup queue and clear it.
		  File[] everything;
		  string tempDirBasePath;
		  lock (CleanupQueue)
		  {
			tempDirBasePath = (TempDirBase != null ? TempDirbase.AbsolutePath : null);
			TempDirBase = null;

			CleanupQueue.Reverse();
			everything = new File [CleanupQueue.Count];
			CleanupQueue.toArray(everything);
			CleanupQueue.Clear();
		  }

		  // Only check and throw an IOException on un-removable files if the test
		  // was successful. Otherwise just report the path of temporary files
		  // and leave them there.
		  if (LuceneTestCase.SuiteFailureMarker.WasSuccessful())
		  {
			try
			{
			  TestUtil.Rm(everything);
			}
			catch (IOException e)
			{
			  Type suiteClass = RandomizedContext.current().TargetClass;
			  if (suiteClass.isAnnotationPresent(typeof(SuppressTempFileChecks)))
			  {
				Console.Error.WriteLine("WARNING: Leftover undeleted temporary files (bugUrl: " + suiteClass.getAnnotation(typeof(SuppressTempFileChecks)).bugUrl() + "): " + e.Message);
				return;
			  }
			  throw e;
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
	  }
	}


	private class ReaderClosedListenerAnonymousInnerClassHelper : IndexReader.ReaderClosedListener
	{
		private readonly LuceneTestCase outerInstance;

		private ThreadPoolExecutor ex;

		public ReaderClosedListenerAnonymousInnerClassHelper(LuceneTestCase outerInstance, ThreadPoolExecutor ex)
		{
			this.outerInstance = outerInstance;
			this.ex = ex;
		}

		public override void OnClose(IndexReader reader)
		{
		  TestUtil.ShutdownExecutorService(ex);
		}
	}

	private class ComparatorAnonymousInnerClassHelper : IComparer<IndexableField>
	{
		private readonly LuceneTestCase outerInstance;

		public ComparatorAnonymousInnerClassHelper(LuceneTestCase outerInstance)
		{
			this.outerInstance = outerInstance;
		}

		public virtual int Compare(IndexableField arg0, IndexableField arg1)
		{
		  return arg0.name().compareTo(arg1.name());
		}
	}
}