// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Snowball
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

    /// <summary>
    /// Test the snowball filters against the snowball data tests
    /// </summary>
    // LUCENENET specific - Run each of the snowball tests in parallel to improve performance
    [Parallelizable(ParallelScope.Children)] // Runs individual language test cases in parallel for this class only
    public class TestSnowballVocab : LuceneTestCase
    {
        /// <summary>
        /// This value must remain in sync whenever the TestSnowballVocabData.zip file is updated.
        /// </summary>
        private const int SNOWBALL_VOCAB_FILE_BYTE_LENGTH = 3_128_133;

        private byte[] cachedZipBytes;

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // LUCENENET specific: Read the embedded zip resource once into a byte array
            // in RAM. This prevents each test from having to cache the file.
            //
            // Preloading based on input.Length hurts performance because the stream will
            // seek all the way to the end to get the length. So, we use a constant,
            // SNOWBALL_VOCAB_FILE_BYTE_LENGTH, that must be kept in sync with the file
            // length manually.
            using (Stream input = GetDataFile("TestSnowballVocabData.zip"))
            using (MemoryStream ms = new MemoryStream(SNOWBALL_VOCAB_FILE_BYTE_LENGTH))
            {
                input.CopyTo(ms);
                Assert.AreEqual(input.Length, SNOWBALL_VOCAB_FILE_BYTE_LENGTH,
                    "If this assert fails, the SNOWBALL_VOCAB_FILE_BYTE_LENGTH value must be updated with the new length of the file.");
                cachedZipBytes = ms.GetBuffer();
            }
        }

        public override void OneTimeTearDown()
        {
            cachedZipBytes = null;
            base.OneTimeTearDown();
        }

        // Define test cases explicitly via a source method to control queueing order
        private static IEnumerable<TestCaseData> GetTestCases()
        {
            // 1. FRONT-LOAD THE HEAVIEST TESTS FIRST:
            // Put your known slowest languages here so parallel workers pick them up immediately.
            yield return new TestCaseData("Turkish", "turkish");
            yield return new TestCaseData("Dutch", "dutch");
            yield return new TestCaseData("Russian", "russian");
            yield return new TestCaseData("German2", "german2");
            yield return new TestCaseData("Hungarian", "hungarian");
            yield return new TestCaseData("Kp", "kraaij_pohlmann");

            // 2. Followed by the rest of the languages in any order
            yield return new TestCaseData("Danish", "danish");
            
            yield return new TestCaseData("English", "english");
            // disabled due to snowball java code generation bug:
            // see http://article.gmane.org/gmane.comp.search.snowball/1139
            // yield return new TestCaseData("Finnish", "finnish");
            yield return new TestCaseData("French", "french");
            yield return new TestCaseData("German", "german");

            yield return new TestCaseData("Italian", "italian");
            // disabled due to snowball java code generation bug:
            // see http://article.gmane.org/gmane.comp.search.snowball/1139
            // yield return new TestCaseData("Lovins", "lovins");
            yield return new TestCaseData("Norwegian", "norwegian");
            yield return new TestCaseData("Porter", "porter");
            yield return new TestCaseData("Portuguese", "portuguese");
            yield return new TestCaseData("Romanian", "romanian");
            
            yield return new TestCaseData("Spanish", "spanish");
            yield return new TestCaseData("Swedish", "swedish");
        }

        /// <summary>
        /// Run all languages against their snowball vocabulary tests.
        /// </summary>
        [Slow]
        [TestCaseSource(nameof(GetTestCases))]
        public virtual void TestStemmer(string snowballLanguage, string dataDirectory)
        {
            // LUCENENET: Turkish has 3x as many words as the next language.
            // Queue it first and give it a few seconds head start.
            if (snowballLanguage != "Turkish")
                System.Threading.Thread.Sleep(2000);

            AssertCorrectOutput(snowballLanguage, dataDirectory);
        }

        /// <summary>
        /// For the supplied language, run the stemmer against all strings in voc.txt
        /// The output should be the same as the string in output.txt
        /// </summary>
        private void AssertCorrectOutput(string snowballLanguage, string dataDirectory)
        {
            if (Verbose)
            {
                Console.WriteLine("checking snowball language: " + snowballLanguage);
            }

            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer t = new KeywordTokenizer(reader);
                return new TokenStreamComponents(t, new SnowballFilter(t, snowballLanguage));
            });

            // LUCENENET specific - We cached the archive in OneTimeSetUp. Create a fresh, zero-allocation
            // MemoryStream view over the cached zip bytes for each parallel test case. This is completely thread-safe.
            using MemoryStream zipStream = new MemoryStream(cachedZipBytes, writable: false);
            VocabularyAssert.AssertVocabulary(a, zipStream, $"{dataDirectory}/voc.txt", $"{dataDirectory}/output.txt");
        }
    }
}
