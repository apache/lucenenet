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

    // The test data was created by downloading the analysis-data.zip file from https://issues.apache.org/jira/browse/LUCENE-1629,
    // then the following console application was run to replace frequency values for specific strings.
    // This ensures our test covers loading custom data sets and will fail for the stock data.

    #region .dct file conversion app

    //using System.Text;

    //namespace DctFileRewriter
    //{
    //    internal class Program
    //    {
    //        protected static readonly Encoding gb2312Encoding = Encoding.GetEncoding("GB2312",
    //            EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

    //        /// <summary>
    //        /// First Chinese Character in GB2312 (15 * 94)
    //        /// Characters in GB2312 are arranged in a grid of 94 * 94, 0-14 are unassigned or punctuation.
    //        /// </summary>
    //        public const int GB2312_FIRST_CHAR = 1410;

    //        /// <summary>
    //        /// Last Chinese Character in GB2312 (87 * 94).
    //        /// Characters in GB2312 are arranged in a grid of 94 * 94, 88-94 are unassigned.
    //        /// </summary>
    //        public const int GB2312_CHAR_NUM = 87 * 94;

    //        /// <summary>
    //        /// Dictionary data contains 6768 Chinese characters with frequency statistics.
    //        /// </summary>
    //        public const int CHAR_NUM_IN_FILE = 6768;


    //        /// <summary>
    //        /// <para>
    //        /// Transcode from GB2312 ID to Unicode
    //        /// </para>
    //        /// <para>
    //        /// GB2312 is divided into a 94 * 94 grid, containing 7445 characters consisting of 6763 Chinese characters and 682 symbols.
    //        /// Some regions are unassigned (reserved).
    //        /// </para>
    //        /// </summary>
    //        /// <param name="ccid">GB2312 id</param>
    //        /// <returns>unicode String</returns>
    //        public static string GetCCByGB2312Id(int ccid)
    //        {
    //            if (ccid < 0 || ccid > GB2312_CHAR_NUM)
    //                return "";
    //            int cc1 = ccid / 94 + 161;
    //            int cc2 = ccid % 94 + 161;
    //            byte[] buffer = new byte[2];
    //            buffer[0] = (byte)cc1;
    //            buffer[1] = (byte)cc2;
    //            try
    //            {
    //                //String cchar = new String(buffer, "GB2312");
    //                string cchar = gb2312Encoding.GetString(buffer); // LUCENENET specific: use cached encoding instance
    //                return cchar;
    //            }
    //            catch (Exception e) //when (e.IsUnsupportedEncodingException()) // Encoding is not supported by the platform
    //            {
    //                return "";
    //            }
    //        }


    //        static void Main(string[] args)
    //        {
    //            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    //            string bigramInput = @"real/path/analysis-data/bigramdict.dct";
    //            string bigramOutput = @"real/path/analysis-data/custom-dictionary-input/bigramdict.dct";
    //            var bigramReplacementFrequencies = new Dictionary<string, int>
    //            {
    //                ["锲而不舍@、"] = 10000,
    //                ["聆听@了"] = 15000,
    //                ["魅力@，"] = 20000,
    //            };

    //            RewriteDct(bigramInput, bigramOutput, bigramReplacementFrequencies);


    //            string coreInput = @"real/path/analysis-data/analysis-data/coredict.dct";
    //            string coreOutput = @"real/path/analysis-data/analysis-data/custom-dictionary-input/coredict.dct";
    //            var coreReplacementFrequencies = new Dictionary<string, int>
    //            {
    //                ["魅力"] = 10000,
    //                ["聆听"] = 15000,
    //            };

    //            RewriteDct(coreInput, coreOutput, coreReplacementFrequencies);
    //        }

    //        public static void RewriteDct(string inputPath, string outputPath, IDictionary<string, int> replacementFrequencies)
    //        {
    //            using var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
    //            using var reader = new BinaryReader(input);

    //            using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
    //            using var writer = new BinaryWriter(output);


    //            int i, cnt, length, total = 0;

    //            // The file only counted 6763 Chinese characters plus 5 reserved slots 3756~3760.
    //            // The 3756th is used (as a header) to store information.

    //            Span<int> buffer = stackalloc int[3];
    //            string tmpword;

    //            // LUCENENET: Removed intBuffer arrays since BinaryReader handles reading values directly in a more type-safe and readable way.
    //            // LUCENENET specific - refactored constants for clarity

    //            // The 3756th position (using 1-based counting) corresponds to index 3755 (using 0-based indexing)
    //            // This matches the original Java implementation which used 3755 + GB2312_FIRST_CHAR in the condition
    //            const int HEADER_POSITION = 3755;



    //            // GB2312 characters 0 - 6768
    //            for (i = GB2312_FIRST_CHAR; i < GB2312_FIRST_CHAR + CHAR_NUM_IN_FILE; i++)
    //            {

    //                string currentStr = GetCCByGB2312Id(i);
    //                // if (i == 5231)
    //                // System.out.println(i);
    //                try
    //                {
    //                    // READ

    //                    cnt = reader.ReadInt32();  // LUCENENET: Use BinaryReader to decode little endian instead of ByteBuffer, since this is the default in .NET
    //                }
    //                catch (EndOfStreamException ex)
    //                {
    //                    throw new IOException($"Bigram dictionary file is incomplete at character index {i}.", ex);
    //                }

    //                writer.Write(cnt); // WRITE

    //                if (cnt <= 0)
    //                {
    //                    continue;
    //                }
    //                total += cnt;
    //                int j = 0;
    //                while (j < cnt)
    //                {
    //                    // READ

    //                    // LUCENENET: Use BinaryReader to decode little endian instead of ByteBuffer, since this is the default in .NET
    //                    buffer[0] = reader.ReadInt32(); // frequency
    //                    buffer[1] = reader.ReadInt32(); // length
    //                    buffer[2] = reader.ReadInt32(); // handle
    //                                                    //reader.BaseStream.Seek(4, SeekOrigin.Current); // Skip handle value (unused)

    //                    length = buffer[1];
    //                    if (length > 0 /* && input.Position + length <= input.Length*/)
    //                    {
    //                        // READ

    //                        byte[] lchBuffer = reader.ReadBytes(length);  // LUCENENET: Use BinaryReader to decode little endian instead of ByteBuffer, since this is the default in .NET

    //                        //tmpword = new String(lchBuffer, "GB2312");
    //                        tmpword = gb2312Encoding.GetString(lchBuffer); // LUCENENET specific: use cached encoding instance from base class
    //                                                                       //tmpword = Encoding.GetEncoding("hz-gb-2312").GetString(lchBuffer);

    //                        if (i != HEADER_POSITION + GB2312_FIRST_CHAR)
    //                        {
    //                            tmpword = currentStr + tmpword;
    //                        }


    //                        // Make the replacement if we hit the right word
    //                        if (replacementFrequencies.TryGetValue(tmpword, out int newFrequency))
    //                            buffer[0] = newFrequency;

    //                        // WRITE

    //                        writer.Write(buffer[0]); // frequency
    //                        writer.Write(buffer[1]); // length
    //                        writer.Write(buffer[2]); // handle
    //                        writer.Write(lchBuffer);
    //                    }
    //                    else
    //                    {
    //                        // WRITE

    //                        writer.Write(buffer[0]); // frequency
    //                        writer.Write(buffer[1]); // length
    //                        writer.Write(buffer[2]); // handle
    //                    }

    //                    j++;
    //                }
    //            }

    //        }
    //    }
    //}

    #endregion .dct file conversion app

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
            Assert.AreEqual(10000, bigramDict.GetFrequency("锲而不舍@、".AsSpan()), "Frequency for '锲而不舍@、' should be 10000.");
            Assert.AreEqual(15000, bigramDict.GetFrequency("聆听@了".AsSpan()), "Frequency for '聆听@了' should be 15000.");
            Assert.AreEqual(20000, bigramDict.GetFrequency("魅力@，".AsSpan()), "Frequency for '魅力@，' should be 20000.");
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
            Assert.AreEqual(10000, wordDict.GetFrequency("魅力".ToCharArray()), "Frequency for '魅力' should be 10000.");
            Assert.AreEqual(15000, wordDict.GetFrequency("聆听".ToCharArray()), "Frequency for '聆听' should be 15000.");
            Assert.AreEqual(0, wordDict.GetFrequency("missing".ToCharArray()), "Expected frequency 0 for unknown word.");
        }
    }
}
