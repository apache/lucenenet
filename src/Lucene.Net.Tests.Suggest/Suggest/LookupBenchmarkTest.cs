using Lucene.Net.Analysis;
using Lucene.Net.Search.Suggest.Analyzing;
using Lucene.Net.Search.Suggest.Fst;
using Lucene.Net.Search.Suggest.Jaspell;
using Lucene.Net.Search.Suggest.Tst;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Lucene.Net.Search.Suggest
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

    [Ignore("COMMENT ME TO RUN BENCHMARKS!")]
    public class LookupBenchmarkTest : LuceneTestCase
    {
        private readonly List<Type> benchmarkClasses = Arrays.AsList(
            typeof(FuzzySuggester),
            typeof(AnalyzingSuggester),
            typeof(AnalyzingInfixSuggester),
            typeof(JaspellLookup),
            typeof(TSTLookup),
            typeof(FSTCompletionLookup),
            typeof(WFSTCompletionLookup)
            );

        private readonly static int rounds = 15;
        private readonly static int warmup = 5;

        internal readonly int num = 7;
        internal readonly bool onlyMorePopular = false;

        private readonly static Random random = new Random(0xdeadbee); // LUCENENET NOTE: Changed seed so it would fit in an int

        /**
         * Input term/weight pairs.
         */
        private static Input[] dictionaryInput;

        /**
         * Benchmark term/weight pairs (randomized order).
         */
        private static IList<Input> benchmarkInput;

        /**
         * Loads terms and frequencies from Wikipedia (cached).
         */

        public override void SetUp()
        {
            Debug.Assert(false, "disable assertions before running benchmarks!");
            IList<Input> input = ReadTop50KWiki();
            input = CollectionsHelper.Shuffle(input);
            dictionaryInput = input.ToArray();
            input = CollectionsHelper.Shuffle(input);
            benchmarkInput = input;
        }

        static readonly Encoding UTF_8 = Encoding.UTF8;

        /**
         * Collect the multilingual input for benchmarks/ tests.
         */
        public static IList<Input> ReadTop50KWiki()
        {
            List<Input> input = new List<Input>();

            //URL resource = LookupBenchmarkTest.class.getResource("Top50KWiki.utf8");
            var resource = typeof(LookupBenchmarkTest).GetTypeInfo().Assembly.GetManifestResourceStream("Lucene.Net.Tests.Suggest.Suggest.Top50KWiki.utf8");
            Debug.Assert(resource != null, "Resource missing: Top50KWiki.utf8");

            string line = null;
            using (TextReader br = new StreamReader(resource, UTF_8))
            {
                while ((line = br.ReadLine()) != null)
                {
                    int tab = line.IndexOf('|');
                    assertTrue("No | separator?: " + line, tab >= 0);
                    int weight = int.Parse(line.Substring(tab + 1), CultureInfo.InvariantCulture);
                    string key = line.Substring(0, tab - 0);
                    input.Add(new Input(key, weight));
                }
            }
            return input;
        }

        /**
         * Test construction time.
         */
        [Test]
        public void TestConstructionTime()
        {
            Console.WriteLine("-- construction time");
            foreach (var cls in benchmarkClasses)
            {
                BenchmarkResult result = Measure(new CallableIntHelper(this, cls));

                Console.WriteLine(
                    string.Format(CultureInfo.InvariantCulture, "{0,15}s input: {1}, time[ms]: {2}" /*"%-15s input: %d, time[ms]: %s"*/,
                        cls.Name,
                        dictionaryInput.Length,
                        result.average.ToString()));
            }
        }

        private class CallableIntHelper : ICallable<int>
        {
            private readonly Type cls;
            private readonly LookupBenchmarkTest outerInstance;
            public CallableIntHelper(LookupBenchmarkTest outerInstance, Type cls)
            {
                this.cls = cls;
                this.outerInstance = outerInstance;
            }
            public int Call()
            {
                Lookup lookup = outerInstance.BuildLookup(cls, LookupBenchmarkTest.dictionaryInput);
                return lookup.GetHashCode();
            }
        }

        /**
         * Test memory required for the storage.
         */
        [Test]
        public void TestStorageNeeds()
        {
            Console.WriteLine("-- RAM consumption");
            foreach (Type cls in benchmarkClasses)
            {
                Lookup lookup = BuildLookup(cls, dictionaryInput);
                long sizeInBytes = lookup.GetSizeInBytes();
                Console.WriteLine(
            string.Format(CultureInfo.InvariantCulture, "{0,15}s size[B]:{1:#,##0}" /*"%-15s size[B]:%,13d"*/,
                lookup.GetType().Name,
                sizeInBytes));
            }
        }

        /**
         * Create {@link Lookup} instance and populate it. 
         */
        internal Lookup BuildLookup(Type cls, Input[] input)
        {
            Lookup lookup = null;
            try
            {
                //lookup = cls.newInstance();
                lookup = (Lookup)Activator.CreateInstance(cls);
            }
            catch (MissingMethodException /*e*/)
            {
                Analyzer a = new MockAnalyzer(random, MockTokenizer.KEYWORD, false);
                if (cls == typeof(AnalyzingInfixSuggester))
                {
                    lookup = new AnalyzingInfixSuggester(TEST_VERSION_CURRENT, FSDirectory.Open(CreateTempDir("LookupBenchmarkTest")), a);
                }
                else
                {
                    ConstructorInfo ctor = cls.GetConstructor(new Type[] { typeof(Analyzer) });
                    //lookup = ctor.newInstance(a);
                    lookup = (Lookup)ctor.Invoke(new object[] { a });
                }
            }
            lookup.Build(new InputArrayIterator(input));
            return lookup;
        }

        /**
         * Test performance of lookup on full hits.
         */
        [Test]
        public void TestPerformanceOnFullHits()
        {
            int minPrefixLen = 100;
            int maxPrefixLen = 200;
            RunPerformanceTest(minPrefixLen, maxPrefixLen, num, onlyMorePopular);
        }

        /**
         * Test performance of lookup on longer term prefixes (6-9 letters or shorter).
         */
        [Test]
        public void TestPerformanceOnPrefixes6_9()
        {
            int minPrefixLen = 6;
            int maxPrefixLen = 9;
            RunPerformanceTest(minPrefixLen, maxPrefixLen, num, onlyMorePopular);
        }

        /**
         * Test performance of lookup on short term prefixes (2-4 letters or shorter).
         */
        [Test]
        public void TestPerformanceOnPrefixes2_4()
        {
            int minPrefixLen = 2;
            int maxPrefixLen = 4;
            RunPerformanceTest(minPrefixLen, maxPrefixLen, num, onlyMorePopular);
        }

        /**
         * Run the actual benchmark. 
         */
        public void RunPerformanceTest(int minPrefixLen, int maxPrefixLen,
            int num, bool onlyMorePopular)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "-- prefixes: {0}-{1}, num: {2}, onlyMorePopular: {3}",
                //"-- prefixes: %d-%d, num: %d, onlyMorePopular: %s",
                minPrefixLen, maxPrefixLen, num, onlyMorePopular));

            foreach (Type cls in benchmarkClasses)
            {
                Lookup lookup = BuildLookup(cls, dictionaryInput);

                List<string> input = new List<string>(benchmarkInput.size());
                foreach (Input tf in benchmarkInput)
                {
                    string s = tf.term.Utf8ToString();
                    string sub = s.Substring(0, Math.Min(s.Length,
                minPrefixLen + random.nextInt(maxPrefixLen - minPrefixLen + 1)));
                    input.Add(sub);
                }

                BenchmarkResult result = Measure(new PerformanceTestCallableIntHelper(this, input, lookup));

                Console.WriteLine(
                    string.Format(CultureInfo.InvariantCulture, "{0,15}s queries: {1}, time[ms]: {2}, ~kQPS: {3:#.0}" /*"%-15s queries: %d, time[ms]: %s, ~kQPS: %.0f"*/,
                lookup.GetType().Name,
                input.size(),
                result.average.toString(),
                input.size() / result.average.avg));
            }
        }

        internal class PerformanceTestCallableIntHelper : ICallable<int>
        {
            private readonly IEnumerable<string> input;
            private readonly Lookup lookup;
            private readonly LookupBenchmarkTest outerInstance;

            public PerformanceTestCallableIntHelper(LookupBenchmarkTest outerInstance, IEnumerable<string> input, Lookup lookup)
            {
                this.outerInstance = outerInstance;
                this.input = input;
                this.lookup = lookup;
            }

            public int Call()
            {
                int v = 0;
                foreach (string term in input)
                {
                    v += lookup.DoLookup(term, outerInstance.onlyMorePopular, outerInstance.num).Count;
                }
                return v;
            }
        }

        /**
         * Do the measurements.
         */
        private BenchmarkResult Measure(ICallable<int> callable)
        {
            //double NANOS_PER_MS = 1000000;

            try
            {
                List<double> times = new List<double>();
                for (int i = 0; i < warmup + rounds; i++)
                {
                    long start = Environment.TickCount;
                    guard = Convert.ToInt32(callable.Call());
                    times.Add((Environment.TickCount - start) /* / NANOS_PER_MS */);
                }
                return new BenchmarkResult(times, warmup, rounds);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                //e.printStackTrace();
                throw new Exception(e.Message, e);

            }
        }

        /** Guard against opts. */
        //@SuppressWarnings("unused")
        private static volatile int guard;

        internal class BenchmarkResult
        {
            /** Average time per round (ms). */
            public readonly Average average;

            public BenchmarkResult(IList<double> times, int warmup, int rounds)
            {
                this.average = Average.From(times.SubList(warmup, times.Count));
            }
        }
    }
}
