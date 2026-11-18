using J2N.Text;
using Lucene.Net.Support.IO;
using Lucene.Net.Text;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis
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
    /// Utility class for doing vocabulary-based stemming tests. </summary>
    public static class VocabularyAssert // LUCENENET specific - made static because all members are static
    {
        /// <summary>
        /// Run a vocabulary test against two data files. </summary>
        public static void AssertVocabulary(Analyzer a, Stream voc, Stream @out)
        {
            // LUCENENET: Reuse the memory rather than allocating on each loop
            ValueStringBuilder inputWord = new(stackalloc char[32]);
            ValueStringBuilder expectedWord = new(stackalloc char[32]);
            try
            {
                using BufferedTextReader vocReader = new BufferedTextReader(new StreamReader(voc, Encoding.UTF8));
                using BufferedTextReader outputReader = new BufferedTextReader(new StreamReader(@out, Encoding.UTF8));
                while (vocReader.TryReadLine(ref inputWord))
                {
                    Assert.IsTrue(outputReader.TryReadLine(ref expectedWord));
                    BaseTokenStreamTestCase.CheckOneTerm(a, inputWord.AsSpan(), expectedWord.AsSpan());
                }
            }
            finally
            {
                inputWord.Dispose();
                expectedWord.Dispose();
            }
        }

        /// <summary>
        /// Run a vocabulary test against one file: tab separated. </summary>
        public static void AssertVocabulary(Analyzer a, Stream vocOut)
        {
            // LUCENENET: Reuse the memory rather than allocating on each loop
            ValueStringBuilder line = new(stackalloc char[64]);
            try
            {
                using BufferedTextReader vocReader = new BufferedTextReader(new StreamReader(vocOut, Encoding.UTF8));
                while (vocReader.TryReadLine(ref line))
                {
                    ReadOnlySpan<char> inputLine = line.AsSpan();
                    if (inputLine.StartsWith("#") || inputLine.Trim().Length == 0)
                    {
                        continue; // comment
                    }
                    // LUCENENET: Use IndexOf() instead of Split()
                    int tabPos1 = inputLine.IndexOf('\t');
                    int tabPos2 = inputLine.Slice(tabPos1 + 1).IndexOf('\t');

                    // LUCENENET: We knoe the pos of the first 2 tabs, now
                    // slice to get the first 2 values
                    ReadOnlySpan<char> input = inputLine.Slice(0, tabPos1);
                    ReadOnlySpan<char> expected = tabPos2 > -1
                        ? inputLine.Slice(tabPos1 + 1, tabPos2)
                        : inputLine.Slice(tabPos1 + 1);

                    BaseTokenStreamTestCase.CheckOneTerm(a, input, expected);
                }
            }
            finally
            {
                line.Dispose();
            }
        }

        /// <summary>
        /// Run a vocabulary test against two data files inside a zip file. </summary>
        public static void AssertVocabulary(Analyzer a, Stream zipFile, string voc, string @out)
        {
            using ZipArchive zip = new ZipArchive(zipFile, ZipArchiveMode.Read, false, Encoding.UTF8);
            using Stream v = zip.GetEntry(voc).Open();
            using Stream o = zip.GetEntry(@out).Open();
            AssertVocabulary(a, v, o);
        }

        /// <summary>
        /// Run a vocabulary test against a tab-separated data file inside a zip file. </summary>
        public static void AssertVocabulary(Analyzer a, Stream zipFile, string vocOut)
        {
            using ZipArchive zip = new ZipArchive(zipFile, ZipArchiveMode.Read, false, Encoding.UTF8);
            using Stream vo = zip.GetEntry(vocOut).Open();
            AssertVocabulary(a, vo);
        }
    }
}
