// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Core
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

    public class TestStopAnalyzer : BaseTokenStreamTestCase
    {

        private StopAnalyzer stop = new StopAnalyzer(TEST_VERSION_CURRENT);
        private ISet<object> inValidTokens = new JCG.HashSet<object>();

        public override void SetUp()
        {
            base.SetUp();

            var it = StopAnalyzer.ENGLISH_STOP_WORDS_SET.GetEnumerator();
            while (it.MoveNext())
            {
                inValidTokens.Add(it.Current);
            }
        }

        [Test]
        public virtual void TestDefaults()
        {
            assertTrue(stop != null);
            TokenStream stream = stop.GetTokenStream("test", "This is a test of the english stop analyzer");
            try
            {
                assertTrue(stream != null);
                ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();
                stream.Reset();

                while (stream.IncrementToken())
                {
                    assertFalse(inValidTokens.Contains(termAtt.ToString()));
                }
                stream.End();
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(stream);
            }
        }

        [Test]
        public virtual void TestStopList()
        {
            CharArraySet stopWordsSet = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "good", "test", "analyzer" }, false);
            StopAnalyzer newStop = new StopAnalyzer(TEST_VERSION_CURRENT, stopWordsSet);
            TokenStream stream = newStop.GetTokenStream("test", "This is a good test of the english stop analyzer");
            try
            {
                assertNotNull(stream);
                ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();

                stream.Reset();
                while (stream.IncrementToken())
                {
                    string text = termAtt.ToString();
                    assertFalse(stopWordsSet.contains(text));
                }
                stream.End();
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(stream);
            }
        }

        [Test]
        public virtual void TestStopListPositions()
        {
            CharArraySet stopWordsSet = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "good", "test", "analyzer" }, false);
            StopAnalyzer newStop = new StopAnalyzer(TEST_VERSION_CURRENT, stopWordsSet);
            string s = "This is a good test of the english stop analyzer with positions";
            int[] expectedIncr = new int[] { 1, 1, 1, 3, 1, 1, 1, 2, 1 };
            TokenStream stream = newStop.GetTokenStream("test", s);
            try
            {
                assertNotNull(stream);
                int i = 0;
                ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();
                IPositionIncrementAttribute posIncrAtt = stream.AddAttribute<IPositionIncrementAttribute>();

                stream.Reset();
                while (stream.IncrementToken())
                {
                    string text = termAtt.ToString();
                    assertFalse(stopWordsSet.contains(text));
                    assertEquals(expectedIncr[i++], posIncrAtt.PositionIncrement);
                }
                stream.End();
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(stream);
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestStopAnalyzerWithStringFileName()
        {
            // Create a temp file with stop words
            var tempFile = LuceneTestCase.CreateTempFile("stopwords", ".txt");
            try
            {
                // Write custom stop words to the temp file
                using (var fs = new FileStream(tempFile.FullName, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    writer.WriteLine("custom");
                    writer.WriteLine("stop");
                    writer.WriteLine("words");
                    writer.WriteLine("test");
                }

                // Test the string-based constructor
                StopAnalyzer analyzer = new StopAnalyzer(TEST_VERSION_CURRENT, tempFile.FullName);

                string input = "This is a test with custom stop words included";
                TokenStream stream = analyzer.GetTokenStream("test", input);
                try
                {
                    assertNotNull(stream);
                    ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();

                    var tokens = new List<string>();
                    stream.Reset();
                    while (stream.IncrementToken())
                    {
                        tokens.Add(termAtt.ToString());
                    }
                    stream.End();

                    // Verify that our custom stop words were filtered out
                    assertFalse(tokens.Contains("custom"));
                    assertFalse(tokens.Contains("stop"));
                    assertFalse(tokens.Contains("words"));
                    assertFalse(tokens.Contains("test"));

                    // Verify that non-stop words remain
                    assertTrue(tokens.Contains("included"));
                }
                finally
                {
                    IOUtils.CloseWhileHandlingException(stream);
                }
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempFile.FullName))
                {
                    File.Delete(tempFile.FullName);
                }
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestStopAnalyzerWithRelativePath()
        {
            // Create a temp directory structure
            var tempDir = LuceneTestCase.CreateTempDir("stopAnalyzerRelative");
            var stopwordsDir = new DirectoryInfo(System.IO.Path.Combine(tempDir.FullName, "config"));
            stopwordsDir.Create();

            try
            {
                // Create stopwords file in subdirectory
                string stopwordsFile = System.IO.Path.Combine(stopwordsDir.FullName, "stops.txt");
                using (var fs = new FileStream(stopwordsFile, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    writer.WriteLine("relative");
                    writer.WriteLine("path");
                    writer.WriteLine("stop");
                    writer.WriteLine("words");
                }

                // Use SystemEnvironment to safely change current directory
                SystemEnvironment.WithCurrentDirectory(tempDir.FullName, () =>
                {
                    // Test with relative path
                    StopAnalyzer analyzer = new StopAnalyzer(TEST_VERSION_CURRENT, "config/stops.txt");

                    string input = "this is relative path with stop words testing";
                    TokenStream stream = analyzer.GetTokenStream("test", input);
                    try
                    {
                        assertNotNull(stream);
                        ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();

                        var tokens = new List<string>();
                        stream.Reset();
                        while (stream.IncrementToken())
                        {
                            tokens.Add(termAtt.ToString());
                        }
                        stream.End();

                        // Verify that stop words from relative path file were filtered out
                        assertFalse(tokens.Contains("relative"));
                        assertFalse(tokens.Contains("path"));
                        assertFalse(tokens.Contains("stop"));
                        assertFalse(tokens.Contains("words"));

                        // Verify that non-stop words remain
                        assertTrue(tokens.Contains("testing"));
                    }
                    finally
                    {
                        IOUtils.CloseWhileHandlingException(stream);
                    }

                    // Also test with "./" prefix
                    analyzer = new StopAnalyzer(TEST_VERSION_CURRENT, "./config/stops.txt");
                    stream = analyzer.GetTokenStream("test", input);
                    try
                    {
                        assertNotNull(stream);
                        ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();

                        var tokens = new List<string>();
                        stream.Reset();
                        while (stream.IncrementToken())
                        {
                            tokens.Add(termAtt.ToString());
                        }
                        stream.End();

                        // Verify same behavior with ./ prefix
                        assertFalse(tokens.Contains("relative"));
                        assertFalse(tokens.Contains("path"));
                        assertTrue(tokens.Contains("testing"));
                    }
                    finally
                    {
                        IOUtils.CloseWhileHandlingException(stream);
                    }
                });
            }
            finally
            {
                // Clean up temp files
                if (Directory.Exists(tempDir.FullName))
                {
                    Directory.Delete(tempDir.FullName, true);
                }
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestLoadStopwordSetWithStringPath()
        {
            // Create a temp file with stop words (without comments for simplicity)
            var tempFile = LuceneTestCase.CreateTempFile("stopwordset", ".txt");
            try
            {
                // Write stop words to the temp file (without comments - just test core functionality)
                using (var fs = new FileStream(tempFile.FullName, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    writer.WriteLine("word1");
                    writer.WriteLine("word2");
                    writer.WriteLine("word3");
                    writer.WriteLine("word4");
                }

                // Test that StopAnalyzer's string constructor loads the file properly
                // This internally uses StopwordAnalyzerBase.LoadStopwordSet(string, LuceneVersion)
                StopAnalyzer analyzer = new StopAnalyzer(TEST_VERSION_CURRENT, tempFile.FullName);

                string input = "this is word1 and word2 and word3 and word4 and other words";
                TokenStream stream = analyzer.GetTokenStream("test", input);
                try
                {
                    assertNotNull(stream);
                    ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();

                    var tokens = new List<string>();
                    stream.Reset();

                    try
                    {
                        while (stream.IncrementToken())
                        {
                            tokens.Add(termAtt.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        fail($"Failed to increment token: {ex.Message}");
                    }

                    stream.End();

                    // Verify that words from file were filtered out
                    assertFalse(tokens.Contains("word1"));
                    assertFalse(tokens.Contains("word2"));
                    assertFalse(tokens.Contains("word3"));
                    assertFalse(tokens.Contains("word4"));

                    // Verify that other words remain
                    assertTrue(tokens.Contains("other"));
                    assertTrue(tokens.Contains("words"));
                }
                finally
                {
                    IOUtils.CloseWhileHandlingException(stream);
                }
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempFile.FullName))
                {
                    File.Delete(tempFile.FullName);
                }
            }
        }
    }
}
