// Lucene version compatibility level 4.8.1
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Analysis.Util
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

    [TestFixture]
    public class TestRollingCharBuffer : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            int ITERS;

            // LUCENENET specific: NUnit will crash with an OOM if we do the full test
            // with verbosity enabled. So, decreasing the number of iterations by 1/2
            // to keep it from crashing.
            if (Verbose)
                ITERS = AtLeast(500);
            else
                ITERS = AtLeast(1000);


            var buffer = new RollingCharBuffer();

            var random = LuceneTestCase.Random;
            for (var iter = 0; iter < ITERS; iter++)
            {   
                var stringLen = random.NextBoolean() ? random.Next(50) : random.Next(20000);
                
                string s;
                if (stringLen == 0)
                {
                    s = "";
                }
                else
                {
                    s = TestUtil.RandomUnicodeString(random, stringLen);
                }
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter=" + iter + " s.length()=" + s.Length);
                }
                buffer.Reset(new StringReader(s));
                var nextRead = 0;
                var availCount = 0;
                while (nextRead < s.Length)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("  cycle nextRead=" + nextRead + " avail=" + availCount);
                    }
                    if (availCount == 0 || random.NextBoolean())
                    {
                        // Read next char
                        if (Verbose)
                        {
                            Console.WriteLine("    new char");
                        }
                        assertEquals(s[nextRead], buffer.Get(nextRead));
                        nextRead++;
                        availCount++;
                    }
                    else if (random.NextBoolean())
                    {
                        // Read previous char
                        var pos = TestUtil.NextInt32(random, nextRead - availCount, nextRead - 1);
                        if (Verbose)
                        {
                            Console.WriteLine("    old char pos=" + pos);
                        }
                        assertEquals(s[pos], buffer.Get(pos));
                    }
                    else
                    {
                        // Read slice
                        int length;
                        if (availCount == 1)
                        {
                            length = 1;
                        }
                        else
                        {
                            length = TestUtil.NextInt32(random, 1, availCount);
                        }
                        int start;
                        if (length == availCount)
                        {
                            start = nextRead - availCount;
                        }
                        else
                        {
                            start = nextRead - availCount + random.Next(availCount - length);
                        }
                        if (Verbose)
                        {
                            Console.WriteLine("    slice start=" + start + " length=" + length);
                        }
                        assertEquals(s.Substring(start, length), new string(buffer.Get(start, length)));
                    }

                    if (availCount > 0 && random.Next(20) == 17)
                    {
                        var toFree = random.Next(availCount);
                        if (Verbose)
                        {
                            Console.WriteLine("    free " + toFree + " (avail=" + (availCount - toFree) + ")");
                        }
                        buffer.FreeBefore(nextRead - (availCount - toFree));
                        availCount -= toFree;
                    }
                }
            }
        }
    }

}