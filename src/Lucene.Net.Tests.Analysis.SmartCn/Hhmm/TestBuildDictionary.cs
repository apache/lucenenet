using J2N;
using Lucene.Net.Analysis.Cn.Smart;
using Lucene.Net.Analysis.Cn.Smart.Hhmm;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Cn.Smart.Hhmm
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

    [LuceneNetSpecific]
    public class TestBuildDictionary : LuceneTestCase
    {
        private DirectoryInfo tempDir;

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            tempDir = CreateTempDir("smartcn-data");
            AnalyzerProfile.ANALYSIS_DATA_DIR = tempDir.FullName;
            using (var zipFileStream = typeof(TestBuildDictionary).FindAndGetManifestResourceStream("custom-dictionary-input.zip"))
            {
                TestUtil.Unzip(zipFileStream, tempDir);
            }
        }

        public override void OneTimeTearDown()
        {
            AnalyzerProfile.ANALYSIS_DATA_DIR = null; // Ensure this test data is not loaded for other tests
            base.OneTimeTearDown();
        }

        [Test]
        public void TestBigramDictionary()
        {
            BigramDictionary bigramDict = BigramDictionary.GetInstance();
            CheckBigramDictionary(bigramDict);

            string memFile = System.IO.Path.Combine(tempDir.FullName, "bigramdict.mem");
            Assert.IsTrue(File.Exists(memFile), "Memory file should be created after first load");

            string dictFile = System.IO.Path.Combine(tempDir.FullName, "bigramdict.dct");
            Assert.IsTrue(File.Exists(dictFile), $"{dictFile} does not exist.");
            File.Delete(dictFile);

            bigramDict = BigramDictionary.GetInstance();
            CheckBigramDictionary(bigramDict);
        }

        private static void CheckBigramDictionary(BigramDictionary bigramDict)
        {
            Assert.IsTrue(bigramDict.GetFrequency("锲而不舍@、".AsSpan()) > 0, "Frequency for '锲而不舍@、' should be positive.");
            Assert.IsTrue(bigramDict.GetFrequency("聆听@了".AsSpan()) > 0, "Frequency for '聆听@了' should be positive.");
            Assert.IsTrue(bigramDict.GetFrequency("魅力@，".AsSpan()) > 0, "Frequency for '魅力@，' should be positive.");
        }

        [Test]
        public void TestWordDictionary()
        {
            WordDictionary wordDict = WordDictionary.GetInstance();
            CheckWordDictionary(wordDict);

            string memFile = System.IO.Path.Combine(tempDir.FullName, "coredict.mem");
            Assert.IsTrue(File.Exists(memFile), "Memory file should be created after first load");

            string dictFile = System.IO.Path.Combine(tempDir.FullName, "coredict.dct");
            Assert.IsTrue(File.Exists(dictFile), $"{dictFile} does not exist.");
            File.Delete(dictFile);

            wordDict = WordDictionary.GetInstance();
            CheckWordDictionary(wordDict);
        }

        private static void CheckWordDictionary(WordDictionary wordDict)
        {
            Assert.IsTrue(wordDict.GetFrequency("魅力".ToCharArray()) > 0, "Frequency for '魅力' should be positive.");
            Assert.IsTrue(wordDict.GetFrequency("聆听".ToCharArray()) > 0, "Frequency for '聆听' should be positive.");
            Assert.AreEqual(0, wordDict.GetFrequency("missing".ToCharArray()), "Expected frequency 0 for unknown word.");
        }
    }
}
