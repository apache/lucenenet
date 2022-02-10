using J2N.Text;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Analysis.Ja
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

    public class TestSearchMode : BaseTokenStreamTestCase
    {
        private readonly static String SEGMENTATION_FILENAME = "search-segmentation-tests.txt";
        private readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new JapaneseTokenizer(reader, null, true, JapaneseTokenizerMode.SEARCH);
            return new TokenStreamComponents(tokenizer, tokenizer);
        });


        /** Test search mode segmentation */
        [Test]
        public void TestSearchSegmentation()
        {
            Stream @is = typeof(TestSearchMode).getResourceAsStream(SEGMENTATION_FILENAME);
            if (@is is null)
            {
                throw new FileNotFoundException("Cannot find " + SEGMENTATION_FILENAME + " in test classpath");
            }
            try
            {
                TextReader reader = new StreamReader(@is, Encoding.UTF8);
                String line = null;
                int lineNumber = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    // Remove comments
                    line = Regex.Replace(line, "#.*$", "");
                    // Skip empty lines or comment lines
                    if (line.Trim().Length == 0) // LUCENENET: CA1820: Test for empty strings using string length
                    {
                        continue;
                    }
                    if (Verbose)
                    {
                        Console.WriteLine("Line no. " + lineNumber + ": " + line);
                    }
                    String[] fields = new Regex("\t").Split(line, 2);
                    String sourceText = fields[0];
                    String[] expectedTokens = Regex.Split(fields[1], "\\s+").TrimEnd();
                    int[] expectedPosIncrs = new int[expectedTokens.Length];
                    int[] expectedPosLengths = new int[expectedTokens.Length];
                    for (int tokIDX = 0; tokIDX < expectedTokens.Length; tokIDX++)
                    {
                        if (expectedTokens[tokIDX].EndsWith("/0", StringComparison.Ordinal))
                        {
                            expectedTokens[tokIDX] = Regex.Replace(expectedTokens[tokIDX], "/0", "");
                            expectedPosLengths[tokIDX] = expectedTokens.Length - 1;
                        }
                        else
                        {
                            expectedPosIncrs[tokIDX] = 1;
                            expectedPosLengths[tokIDX] = 1;
                        }
                    }
                    AssertAnalyzesTo(analyzer, sourceText, expectedTokens, expectedPosIncrs);
                }
            }
            finally
            {
                @is.Dispose();
            }
        }
    }
}
