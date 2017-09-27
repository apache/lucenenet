using Lucene.Net.Attributes;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Text;
using Console = Lucene.Net.Support.SystemConsole;

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

    public class CacheSubSequencePerformanceTest
    {
        [Test, LongRunningTest]
        public void Test()
        {
            //int times = 10000000;
            int times = 100000; // LUCENENET: 10 million times would take several minutes to run - decreasing to 100,000
            Console.WriteLine("Test with String : ");
            Test("Angelo", times);
            Console.WriteLine("Test with StringBuilder : ");
            Test(new StringBuilder("Angelo"), times);
            Console.WriteLine("Test with cached String : ");
            Test(CacheSubSequence("Angelo").ToString(), times);
            Console.WriteLine("Test with cached StringBuilder : ");
            Test(CacheSubSequence(new StringBuilder("Angelo")).ToString(), times);
        }

        private void Test(string input, int times)
        {
            long beginTime = DateTime.UtcNow.Ticks;
            for (int i = 0; i < times; i++)
            {
                Test(input);
            }
            Console.WriteLine(DateTime.UtcNow.Ticks - beginTime + " millis");
        }

        private void Test(StringBuilder input, int times)
        {
            long beginTime = DateTime.UtcNow.Ticks;
            for (int i = 0; i < times; i++)
            {
                Test(input);
            }
            Console.WriteLine(DateTime.UtcNow.Ticks - beginTime + " millis");
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
            public char this[int index]
            {
                get
                {
                    return cached[index];
                }
            }

            public int Length
            {
                get
                {
                    return cached.Length;
                }
            }

            public ICharSequence SubSequence(int start, int end)
            {
                if (start == end)
                {
                    return "".ToCharSequence();
                }
                string res = cache[start][end - 1];
                if (res == null)
                {
                    res = cached.Substring(start, end - start);
                    cache[start][end - 1] = res;
                }
                return res.ToCharSequence();
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
