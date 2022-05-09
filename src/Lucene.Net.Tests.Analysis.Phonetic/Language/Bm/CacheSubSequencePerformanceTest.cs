using J2N.Text;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Diagnostics;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Analysis.Phonetic.Language.Bm
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

    public class CacheSubSequencePerformanceTest : LuceneTestCase
    {
        [Test]
        [Slow]
        [Nightly] // LUCENENET: Since this is more of a benchmark than a test, moving to Nightly to keep us from buring testing time on it
        public void Test()
        {
            int times = 10000000;
            var stopwatch = new Stopwatch();
            Console.WriteLine("Test with String : ");
            Test("Angelo", times, stopwatch);
            Console.WriteLine("Test with StringBuilder : ");
            Test(new StringBuilder("Angelo"), times, stopwatch);
            Console.WriteLine("Test with cached String : ");
            Test(CacheSubSequence("Angelo"), times, stopwatch);
            Console.WriteLine("Test with cached StringBuilder : ");
            Test(CacheSubSequence(new StringBuilder("Angelo")), times, stopwatch);
        }

        private void Test(ICharSequence input, int times, Stopwatch stopwatch)
        {
            stopwatch.Reset();
            stopwatch.Start();
            for (int i = 0; i < times; i++)
            {
                Test(input);
            }
            stopwatch.Stop();
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds} millis");
        }

        private void Test(string input, int times, Stopwatch stopwatch)
        {
            stopwatch.Reset();
            stopwatch.Start();
            for (int i = 0; i < times; i++)
            {
                Test(input);
            }
            stopwatch.Stop();
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds} millis");
        }

        private void Test(StringBuilder input, int times, Stopwatch stopwatch)
        {
            stopwatch.Reset();
            stopwatch.Start();
            for (int i = 0; i < times; i++)
            {
                Test(input);
            }
            stopwatch.Stop();
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds} millis");
        }

        private void Test(ICharSequence input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                for (int j = i; j <= input.Length; j++)
                {
                    input.Subsequence(i, (j - i));
                }
            }
        }

        private void Test(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                for (int j = i; j <= input.Length; j++)
                {
                    input.Substring(i, (j - i));
                }
            }
        }

        private void Test(StringBuilder input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                for (int j = i; j <= input.Length; j++)
                {
                    input.ToString(i, (j - i));
                }
            }
        }

        private class CachedCharSequence : ICharSequence
        {
            private readonly string[][] cache;
            private readonly string cached;
            public CachedCharSequence(string[][] cache, string cached)
            {
                this.cache = cache;
                this.cached = cached;
            }

            bool ICharSequence.HasValue => true; // LUCENENET specific

            public char this[int index] => cached[index];

            public int Length => cached.Length;

            // LUCENENET: Convert the startIndex/length to start/end
            public ICharSequence Subsequence(int startIndex, int length)
                => SubSequence(startIndex, startIndex + length);

            private ICharSequence SubSequence(int start, int end)
            {
                if (start == end)
                {
                    return "".AsCharSequence();
                }
                string res = cache[start][end - 1];
                if (res is null)
                {
                    res = cached.Substring(start, end - start);
                    cache[start][end - 1] = res;
                }
                return res.AsCharSequence();
            }
        }

        private ICharSequence CacheSubSequence(string cached)
        {
            string[][] cache = Support.RectangularArrays.ReturnRectangularArray<string>(cached.Length, cached.Length);
            return new CachedCharSequence(cache, cached);
        }

        private ICharSequence CacheSubSequence(StringBuilder cached)
        {
            string[][] cache = Support.RectangularArrays.ReturnRectangularArray<string>(cached.Length, cached.Length);
            return new CachedCharSequence(cache, cached.ToString());
        }
    }
}
