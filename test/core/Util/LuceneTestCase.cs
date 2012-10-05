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
using CacheEntry = Lucene.Net.Search.CacheEntry;

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
	public abstract class LuceneTestCase
	{
		public static  System.IO.FileInfo TEMP_DIR;
        static LuceneTestCase()
        {
            String directory = Paths.TempDirectory;

            TEMP_DIR = new System.IO.FileInfo(directory);
        }

        bool allowDocsOutOfOrder = true;

		public LuceneTestCase() : base()
		{
		}
		
		public LuceneTestCase(System.String name)
		{
		}
        
		[SetUp]
		public virtual void  SetUp()
		{
			ConcurrentMergeScheduler.SetTestMode();
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
		protected internal virtual void  PurgeFieldCache(FieldCache fc)
		{
			fc.PurgeAllCaches();
		}
		
		protected internal virtual System.String GetTestLabel()
		{
			return NUnit.Framework.TestContext.CurrentContext.Test.FullName;
		}
		
		[TearDown]
		public virtual void  TearDown()
		{
			try
			{
				// this isn't as useful as calling directly from the scope where the 
				// index readers are used, because they could be gc'ed just before
				// tearDown is called.
				// But it's better then nothing.
				AssertSaneFieldCaches(GetTestLabel());
				
				if (ConcurrentMergeScheduler.AnyUnhandledExceptions())
				{
					// Clear the failure so that we don't just keep
					// failing subsequent test cases
					ConcurrentMergeScheduler.ClearUnhandledExceptions();
					Assert.Fail("ConcurrentMergeScheduler hit unhandled exceptions");
				}
			}
			finally
			{
				PurgeFieldCache(Lucene.Net.Search.FieldCache_Fields.DEFAULT);
			}
			
			//base.TearDown();  // {{Aroush-2.9}}
            this.seed = null;
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
		protected internal virtual void  AssertSaneFieldCaches(System.String msg)
		{
			CacheEntry[] entries = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetCacheEntries();
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
		public static void  DumpIterator(System.String label, System.Collections.IEnumerator iter, System.IO.StreamWriter stream)
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
		public static void  DumpArray(System.String label, System.Object[] objs, System.IO.StreamWriter stream)
		{
			System.Collections.IEnumerator iter = (null == objs)?null:new System.Collections.ArrayList(objs).GetEnumerator();
			DumpIterator(label, iter, stream);
		}
		
		/// <summary> Returns a {@link Random} instance for generating random numbers during the test.
		/// The random seed is logged during test execution and printed to System.out on any failure
		/// for reproducing the test using {@link #NewRandom(long)} with the recorded seed
		/// .
		/// </summary>
		public virtual System.Random NewRandom()
		{
			if (this.seed != null)
			{
				throw new System.SystemException("please call LuceneTestCase.newRandom only once per test");
			}
			return NewRandom(seedRnd.Next(System.Int32.MinValue, System.Int32.MaxValue));
		}
		
		/// <summary> Returns a {@link Random} instance for generating random numbers during the test.
		/// If an error occurs in the test that is not reproducible, you can use this method to
		/// initialize the number generator with the seed that was printed out during the failing test.
		/// </summary>
		public virtual System.Random NewRandom(int seed)
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
