using Lucene.Net.Util;
using NUnit.Framework;
using System.Diagnostics;
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

    /**
     * Tests performance for <see cref="PhoneticEngine"/>.
     * <p>
     * See <a href="https://issues.apache.org/jira/browse/CODEC-174">[CODEC-174] Improve performance of Beider Morse
     * encoder</a>.
     * </p>
     * <p>
     * Results for November 7, 2013, project SVN revision 1539678.
     * </p>
     * <p>
     * Environment:
     * </p>
     * <ul>
     * <li>java version "1.7.0_45"</li>
     * <li>Java(TM) SE Runtime Environment (build 1.7.0_45-b18)</li>
     * <li>Java HotSpot(TM) 64-Bit Server VM (build 24.45-b08, mixed mode)</li>
     * <li>OS name: "windows 7", version: "6.1", arch: "amd64", family: "windows")</li>
     * </ul>
     * <ol>
     * <li>Time for encoding 80,000 times the input 'Angelo': 33,039 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 32,297 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 32,857 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': <b>31,561 millis.</b></li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 32,665 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 32,215 millis.</li>
     * </ol>
     * <p>
     * On this file's revision 1539678, with patch <a
     * href="https://issues.apache.org/jira/secure/attachment/12611963/CODEC-174-change-rules-storage-to-Map.patch"
     * >CODEC-174-change-rules-storage-to-Map</a>:
     * </p>
     * <ol>
     * <li>Time for encoding 80,000 times the input 'Angelo': 18,196 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,858 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,644 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': <b>13,591 millis.</b></li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,861 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,696 millis.</li>
     * </ol>
     * <p>
     * Patch applied, committed revision 1539783.
     * </p>
     * <p>
     * On this file's revision 1539783, with patch <a
     * href="https://issues.apache.org/jira/secure/attachment/12611962/CODEC-174-delete-subsequence-cache.patch"
     * >CODEC-174-delete-subsequence-cache.patch</a>:
     * </p>
     * <ol>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,547 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': <b>13,501 millis.</b></li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,528 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 17,110 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,910 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 16,969 millis.</li>
     * </ol>
     * <p>
     * Patch not applied.
     * </p>
     * <p>
     * On this file's revision 1539787, with patch <a
     * href="https://issues.apache.org/jira/secure/attachment/12612178/CODEC-174-reuse-set-in-PhonemeBuilder.patch"
     * >CODEC-174-reuse-set-in-PhonemeBuilder.patch</a>:
     * </p>
     * <ol>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,724 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,451 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,742 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': <b>13,186 millis.</b></li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,600 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 16,405 millis.</li>
     * </ol>
     * <p>
     * Patch applied, committed revision 1539788.
     * </p>
     * <p>
     * Before patch https://issues.apache.org/jira/secure/attachment/12613371/CODEC-174-refactor-restrictTo-method-in-SomeLanguages.patch
     * </p>
     * <ol>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,133 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,064 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': <b>12,838 millis.</b></li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 12,970 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,122 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 13,293 millis.</li>
     * </ol>
     * <p>
     * After patch https://issues.apache.org/jira/secure/attachment/12613371/CODEC-174-refactor-restrictTo-method-in-SomeLanguages.patch
     * </p>
     * <ol>
     * <li>Time for encoding 80,000 times the input 'Angelo': 11,576 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 11,506 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 11,361 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': <b>11,142 millis.</b></li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 11,430 millis.</li>
     * <li>Time for encoding 80,000 times the input 'Angelo': 11,297 millis.</li>
     * </ol>
     * <p>
     * Patch applied, committed revision 1541234.
     * </p>
     */
    public class PhoneticEnginePerformanceTest : LuceneTestCase
    {
        private const int LOOP = 80000;

        [Test]
        [Slow]
        public void Test()
        {
            PhoneticEngine engine = new PhoneticEngine(NameType.GENERIC, RuleType.APPROX, true);
            string input = "Angelo";
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < LOOP; i++)
            {
                engine.Encode(input);
            }
            sw.Stop();
            long totalMillis = sw.ElapsedMilliseconds;
            Console.WriteLine(string.Format("Time for encoding {0} times the input '{1}': {2} millis.", LOOP, input, totalMillis));
        }
    }
}
