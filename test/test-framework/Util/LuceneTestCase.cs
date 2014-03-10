/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using ConcurrentMergeScheduler = Lucene.Net.Index.ConcurrentMergeScheduler;
using Insanity = Lucene.Net.Util.FieldCacheSanityChecker.Insanity;
using FieldCache = Lucene.Net.Search.FieldCache;
using Lucene.Net.TestFramework.Support;
using System.Collections.Generic;
using Lucene.Net.Search;

using Lucene.Net.TestFramework;

namespace Lucene.Net.Util
{

    /// <summary> Base class for all Lucene unit tests.  
    /// <p/>
    /// Currently the
    /// only added functionality over JUnit's TestCase is
    /// asserting that no unhandled exceptions occurred in
    /// threads launched by ConcurrentMergeScheduler and asserting sane
    /// FieldCache usage athe moment of tearDown.
    /// <p/>
    /// If you
    /// override either <c>setUp()</c> or
    /// <c>tearDown()</c> in your unit test, make sure you
    /// call <c>super.setUp()</c> and
    /// <c>super.tearDown()</c>
    /// <p/>
    /// </summary>
    /// <seealso cref="assertSaneFieldCaches">
    /// </seealso>
    [Serializable]
    [TestFixture]
    public abstract class LuceneTestCase : Assert
    {
        // --------------------------------------------------------------------
        // Test groups, system properties and other annotations modifying tests
        // --------------------------------------------------------------------

        public const string SYSPROP_NIGHTLY = "tests.nightly";
        public const string SYSPROP_WEEKLY = "tests.weekly";
        public const string SYSPROP_AWAITSFIX = "tests.awaitsfix";
        public const string SYSPROP_SLOW = "tests.slow";
        public const string SYSPROP_BADAPPLES = "tests.badapples";

        /** @see #ignoreAfterMaxFailures*/
        private const string SYSPROP_MAXFAILURES = "tests.maxfailures";

        /** @see #ignoreAfterMaxFailures*/
        private const string SYSPROP_FAILFAST = "tests.failfast";


     

        public static readonly Util.Version TEST_VERSION_CURRENT = Util.Version.LUCENE_43;

        public static readonly bool VERBOSE = RandomizedTest.SystemPropertyAsBoolean("tests.verbose", false);

        public static readonly bool INFOSTREAM = RandomizedTest.SystemPropertyAsBoolean("tests.infostream", VERBOSE);

        public static readonly int RANDOM_MULTIPLIER = RandomizedTest.SystemPropertyAsInt("tests.multiplier", 1);

        public static readonly string DEFAULT_LINE_DOCS_FILE = "europarl.lines.txt.gz";

        public static readonly string JENKINS_LARGE_LINE_DOCS_FILE = "enwiki.random.lines.txt";

        public static readonly string TEST_CODEC = SystemProperties.GetProperty("tests.codec", "random");

        public static readonly string TEST_DOCVALUESFORMAT = SystemProperties.GetProperty("tests.docvaluesformat", "random");

        public static readonly string TEST_DIRECTORY = SystemProperties.GetProperty("tests.directory", "random");

        public static readonly string TEST_LINE_DOCS_FILE = SystemProperties.GetProperty("tests.linedocsfile", DEFAULT_LINE_DOCS_FILE);

        public static readonly bool TEST_NIGHTLY = RandomizedTest.SystemPropertyAsBoolean(NightlyAttribute.KEY, false);

        public static readonly bool TEST_WEEKLY = RandomizedTest.SystemPropertyAsBoolean(WeeklyAttribute.KEY, false);

        public static readonly bool TEST_AWAITSFIX = RandomizedTest.SystemPropertyAsBoolean(AwaitsFixAttribute.KEY, false);

        public static readonly bool TEST_SLOW = RandomizedTest.SystemPropertyAsBoolean(SlowAttribute.KEY, false);

        //public static readonly MockDirectoryWrapper.Throttling TEST_THROTTLING = TEST_NIGHTLY ? MockDirectoryWrapper.Throttling.SOMETIMES : MockDirectoryWrapper.Throttling.NEVER;

        public static readonly System.IO.DirectoryInfo TEMP_DIR;

        static LuceneTestCase()
        {
            String s = SystemProperties.GetProperty("tempDir", System.IO.Path.GetTempPath());
            if (s == null)
                throw new SystemException("To run tests, you need to define system property 'tempDir' or 'java.io.tmpdir'.");

            TEMP_DIR = new System.IO.DirectoryInfo(s);
            if (!TEMP_DIR.Exists) TEMP_DIR.Create();

            CORE_DIRECTORIES = new List<string>(FS_DIRECTORIES);
            CORE_DIRECTORIES.Add("RAMDirectory");

            
        }

        private static readonly string[] IGNORED_INVARIANT_PROPERTIES = {
            "user.timezone", "java.rmi.server.randomIDs"
        };

        private static readonly IList<String> FS_DIRECTORIES = new[] {
            "SimpleFSDirectory",
            "NIOFSDirectory",
            "MMapDirectory"
        };

        private static readonly IList<String> CORE_DIRECTORIES;

        // .NET Port: this Java code moved to static ctor above
        //static {
        //  CORE_DIRECTORIES = new ArrayList<String>(FS_DIRECTORIES);
        //  CORE_DIRECTORIES.add("RAMDirectory");
        //};

        protected static readonly ISet<String> doesntSupportOffsets = new HashSet<String>(new[] {
            "Lucene3x",
            "MockFixedIntBlock",
            "MockVariableIntBlock",
            "MockSep",
            "MockRandom"
        });

        public void Test()
        {
            
        }

        public static bool PREFLEX_IMPERSONATION_IS_ACTIVE;

        //private static readonly TestRuleStoreClassName classNameRule;

        //internal static readonly TestRuleSetupAndRestoreClassEnv classEnvRule;

        //public static readonly TestRuleMarkFailure suiteFailureMarker =
        //    new TestRuleMarkFailure();

        //internal static readonly TestRuleIgnoreAfterMaxFailures ignoreAfterMaxFailures;

        private const long STATIC_LEAK_THRESHOLD = 10 * 1024 * 1024;

        //private static readonly ISet<String> STATIC_LEAK_IGNORED_TYPES =
        //    new HashSet<String>(new[] {
        //    "org.slf4j.Logger",
        //    "org.apache.solr.SolrLogFormatter",
        //    typeof(EnumSet).FullName});

        //    public static TestRule classRules = RuleChain
        //.outerRule(new TestRuleIgnoreTestSuites())
        //.around(ignoreAfterMaxFailures)
        //.around(suiteFailureMarker)
        //.around(new TestRuleAssertionsRequired())
        //.around(new StaticFieldsInvariantRule(STATIC_LEAK_THRESHOLD, true) {
        //  @Override
        //  protected boolean accept(java.lang.reflect.Field field) {
        //    // Don't count known classes that consume memory once.
        //    if (STATIC_LEAK_IGNORED_TYPES.contains(field.getType().getName())) {
        //      return false;
        //    }
        //    // Don't count references from ourselves, we're top-level.
        //    if (field.getDeclaringClass() == LuceneTestCase.class) {
        //      return false;
        //    }
        //    return super.accept(field);
        //  }
        //})
        //.around(new NoClassHooksShadowingRule())
        //.around(new NoInstanceHooksOverridesRule() {
        //  @Override
        //  protected boolean verify(Method key) {
        //    String name = key.getName();
        //    return !(name.equals("setUp") || name.equals("tearDown"));
        //  }
        //})
        //.around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES))
        //.around(classNameRule = new TestRuleStoreClassName())
        //.around(classEnvRule = new TestRuleSetupAndRestoreClassEnv());

        //static {
        //  int maxFailures = systemPropertyAsInt(SYSPROP_MAXFAILURES, Integer.MAX_VALUE);
        //  boolean failFast = systemPropertyAsBoolean(SYSPROP_FAILFAST, false);

        //  if (failFast) {
        //    if (maxFailures == Integer.MAX_VALUE) {
        //      maxFailures = 1;
        //    } else {
        //      Logger.getLogger(LuceneTestCase.class.getSimpleName()).warning(
        //          "Property '" + SYSPROP_MAXFAILURES + "'=" + maxFailures + ", 'failfast' is" +
        //          " ignored.");
        //    }
        //  }

        //  ignoreAfterMaxFailures = new TestRuleIgnoreAfterMaxFailures(maxFailures);
        //}

        bool allowDocsOutOfOrder = true;

        public LuceneTestCase()
            : base()
        {
        }

        public LuceneTestCase(System.String name)
        {
        }

        [SetUp]
        public virtual void SetUp()
        {
            //ConcurrentMergeScheduler.SetTestMode();
            //parentChainCallRule.setupCalled = true;
        }

        /// <summary> Forcible purges all cache entries from the FieldCache.
        /// <p/>
        /// This method will be called by tearDown to clean up FieldCache.DEFAULT.
        /// If a (poorly written) test has some expectation that the FieldCache
        /// will persist across test methods (ie: a static IndexReader) this 
        /// method can be overridden to do nothing.
        /// <p/>
        /// </summary>
        /// <seealso cref="FieldCache.PurgeAllCaches()">
        /// </seealso>
        protected internal virtual void PurgeFieldCache(IFieldCache fc)
        {
            fc.PurgeAllCaches();
        }

        protected internal virtual string GetTestLabel()
        {
            return NUnit.Framework.TestContext.CurrentContext.Test.FullName;
        }

        [TearDown]
        public virtual void TearDown()
        {
            try
            {
                // this isn't as useful as calling directly from the scope where the 
                // index readers are used, because they could be gc'ed just before
                // tearDown is called.
                // But it's better then nothing.
                AssertSaneFieldCaches(GetTestLabel());

                //if (ConcurrentMergeScheduler.AnyUnhandledExceptions())
                //{
                //    // Clear the failure so that we don't just keep
                //    // failing subsequent test cases
                //    ConcurrentMergeScheduler.ClearUnhandledExceptions();
                //    Assert.Fail("ConcurrentMergeScheduler hit unhandled exceptions");
                //}
            }
            finally
            {
                PurgeFieldCache(Lucene.Net.Search.FieldCache.DEFAULT);
            }

            //base.TearDown();  // {{Aroush-2.9}}
            this.seed = null;

            //parentChainCallRule.teardownCalled = true;
        }



        /// <summary> Asserts that FieldCacheSanityChecker does not detect any 
        /// problems with FieldCache.DEFAULT.
        /// <p/>
        /// If any problems are found, they are logged to System.err 
        /// (allong with the msg) when the Assertion is thrown.
        /// <p/>
        /// This method is called by tearDown after every test method, 
        /// however IndexReaders scoped inside test methods may be garbage 
        /// collected prior to this method being called, causing errors to 
        /// be overlooked. Tests are encouraged to keep their IndexReaders 
        /// scoped at the class level, or to explicitly call this method 
        /// directly in the same scope as the IndexReader.
        /// <p/>
        /// </summary>
        /// <seealso cref="FieldCacheSanityChecker">
        /// </seealso>
        protected internal virtual void AssertSaneFieldCaches(string msg)
        {
            FieldCache.CacheEntry[] entries = Lucene.Net.Search.FieldCache.DEFAULT.GetCacheEntries();
            Insanity[] insanity = null;
            try
            {
                try
                {
                    insanity = FieldCacheSanityChecker.CheckSanity(entries);
                }
                catch (System.SystemException e)
                {
                    System.IO.StreamWriter temp_writer;
                    temp_writer = new System.IO.StreamWriter(System.Console.OpenStandardError(), System.Console.Error.Encoding);
                    temp_writer.AutoFlush = true;
                    DumpArray(msg + ": FieldCache", entries, temp_writer);
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
                    System.IO.StreamWriter temp_writer2;
                    temp_writer2 = new System.IO.StreamWriter(System.Console.OpenStandardError(), System.Console.Error.Encoding);
                    temp_writer2.AutoFlush = true;
                    DumpArray(msg + ": Insane FieldCache usage(s)", insanity, temp_writer2);
                }
            }
        }

        /// <summary> Convinience method for logging an iterator.</summary>
        /// <param name="label">String logged before/after the items in the iterator
        /// </param>
        /// <param name="iter">Each next() is toString()ed and logged on it's own line. If iter is null this is logged differnetly then an empty iterator.
        /// </param>
        /// <param name="stream">Stream to log messages to.
        /// </param>
        public static void DumpIterator(System.String label, System.Collections.IEnumerator iter, System.IO.StreamWriter stream)
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

        /// <summary> Convinience method for logging an array.  Wraps the array in an iterator and delegates</summary>
        /// <seealso cref="dumpIterator(String,Iterator,PrintStream)">
        /// </seealso>
        public static void DumpArray(System.String label, System.Object[] objs, System.IO.StreamWriter stream)
        {
            System.Collections.IEnumerator iter = (null == objs) ? null : new System.Collections.ArrayList(objs).GetEnumerator();
            DumpIterator(label, iter, stream);
        }

        /// <summary> Returns a {@link Random} instance for generating random numbers during the test.
        /// The random seed is logged during test execution and printed to System.out on any failure
        /// for reproducing the test using {@link #NewRandom(long)} with the recorded seed
        /// .
        /// </summary>
        public virtual Random NewRandom()
        {
            if (this.seed != null)
            {
                throw new SystemException("please call LuceneTestCase.newRandom only once per test");
            }
            return NewRandom(seedRnd.Next(Int32.MinValue, Int32.MaxValue));
        }

        /// <summary> Returns a {@link Random} instance for generating random numbers during the test.
        /// If an error occurs in the test that is not reproducible, you can use this method to
        /// initialize the number generator with the seed that was printed out during the failing test.
        /// </summary>
        public virtual Random NewRandom(int seed)
        {
            if (this.seed != null)
            {
                throw new System.SystemException("please call LuceneTestCase.newRandom only once per test");
            }
            this.seed = seed;
            return new System.Random(seed);
        }

        // recorded seed
        [NonSerialized]
        protected internal int? seed = null;
        //protected internal bool seed_init = false;

        // static members
        [NonSerialized]
        private static readonly System.Random seedRnd = new System.Random();

       
       

        protected static void Ok(bool condition, string message = null)
        {
            if (!string.IsNullOrWhiteSpace(message))
                Assert.True(condition, message);
            else
                Assert.True(condition);
        }

        #region Java porting shortcuts

        

        protected static void assertEquals(string msg, object obj1, object obj2)
        {
            Assert.AreEqual(obj1, obj2, msg);
        }

        protected static void assertEquals(object obj1, object obj2)
        {
            Assert.AreEqual(obj1, obj2);
        }

        
        protected static void assertEquals(double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta);
        }

        protected static void assertEquals(string msg, double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta, msg);
        }

        protected static void assertTrue(bool cnd)
        {
            Assert.IsTrue(cnd);
        }

        protected static void assertTrue(string msg, bool cnd)
        {
            Assert.IsTrue(cnd, msg);
        }

        protected static void assertNotNull(object o)
        {
            Assert.NotNull(o);
        }

        protected static void assertNotNull(string msg, object o)
        {
            Assert.NotNull(o, msg);
        }

        protected static void assertNull(object o)
        {
            Assert.Null(o);
        }

        protected static void assertNull(string msg, object o)
        {
            Assert.Null(o, msg);
        }
        #endregion
    }
}
