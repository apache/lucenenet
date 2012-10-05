/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Index.Memory.Test
{
    /*
     * Verifies that Lucene MemoryIndex and RAMDirectory have the same behaviour,
     * returning the same results for queries on some randomish indexes.
     */

    public class MemoryIndexTest : BaseTokenStreamTestCase
    {
        private readonly HashSet<String> _queries = new HashSet<String>();
        private Random random;

        public static int ITERATIONS = 100;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _queries.UnionWith(ReadQueries("testqueries.txt"));
            _queries.UnionWith(ReadQueries("testqueries2.txt"));
            random = NewRandom();
        }

        /*
         * read a set of queries from a resource file
         */

        private IEnumerable<string> ReadQueries(String resource)
        {
            var queries = new HashSet<String>();
            using (var fs = File.Open(resource, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(fs, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length > 0 && !line.StartsWith("#") && !line.StartsWith("//"))
                    {
                        queries.Add(line);
                    }
                }
                return queries;
            }
        }

        /*
         * runs random tests, up to ITERATIONS times.
         */
        [Test]
        public void TestRandomQueries()
        {
            for (int i = 0; i < ITERATIONS; i++)
                AssertAgainstRAMDirectory();
        }

        /*
         * Build a randomish document for both RAMDirectory and MemoryIndex,
         * and run all the queries against it.
         */

        public void AssertAgainstRAMDirectory()
        {
            var fooField = new StringBuilder();
            var termField = new StringBuilder();

            // add up to 250 terms to field "foo"
            for (int i = 0; i < random.Next(250); i++)
            {
                fooField.Append(" ");
                fooField.Append(RandomTerm());
            }

            // add up to 250 terms to field "term"
            for (int i = 0; i < random.Next(250); i++)
            {
                termField.Append(" ");
                termField.Append(RandomTerm());
            }

            var ramdir = new RAMDirectory();
            var analyzer = RandomAnalyzer();
            var writer = new IndexWriter(ramdir, analyzer,
                                                 IndexWriter.MaxFieldLength.UNLIMITED);
            var doc = new Document();
            var field1 = new Field("foo", fooField.ToString(), Field.Store.NO, Field.Index.ANALYZED);
            var field2 = new Field("term", termField.ToString(), Field.Store.NO, Field.Index.ANALYZED);
            doc.Add(field1);
            doc.Add(field2);
            writer.AddDocument(doc);
            writer.Close();

            var memory = new MemoryIndex();
            memory.AddField("foo", fooField.ToString(), analyzer);
            memory.AddField("term", termField.ToString(), analyzer);
            AssertAllQueries(memory, ramdir, analyzer);
        }

        /*
         * Run all queries against both the RAMDirectory and MemoryIndex, ensuring they are the same.
         */

        public void AssertAllQueries(MemoryIndex memory, RAMDirectory ramdir, Analyzer analyzer)
        {
            var ram = new IndexSearcher(ramdir);
            var mem = memory.CreateSearcher();
            var qp = new QueryParser(Version.LUCENE_CURRENT, "foo", analyzer);

            foreach (String query in _queries)
            {
                var ramDocs = ram.Search(qp.Parse(query), 1);
                var memDocs = mem.Search(qp.Parse(query), 1);
                Assert.AreEqual(ramDocs.TotalHits, memDocs.TotalHits);
            }
        }

        /*
         * Return a random analyzer (Simple, Stop, Standard) to analyze the terms.
         */

        private Analyzer RandomAnalyzer()
        {
            switch (random.Next(3))
            {
                case 0:
                    return new SimpleAnalyzer();
                case 1:
                    return new StopAnalyzer(Version.LUCENE_CURRENT);
                default:
                    return new StandardAnalyzer(Version.LUCENE_CURRENT);
            }
        }

        /*
         * Some terms to be indexed, in addition to random words. 
         * These terms are commonly used in the queries. 
         */

        private static readonly string[] TEST_TERMS = {
                                                          "term", "Term", "tErm", "TERM",
                                                          "telm", "stop", "drop", "roll", "phrase", "a", "c", "bar",
                                                          "blar",
                                                          "gack", "weltbank", "worlbank", "hello", "on", "the", "apache"
                                                          , "Apache",
                                                          "copyright", "Copyright"
                                                      };


        /*
         * half of the time, returns a random term from TEST_TERMS.
         * the other half of the time, returns a random unicode string.
         */

        private String RandomTerm()
        {
            if (random.Next(2) == 1)
            {
                // return a random TEST_TERM
                return TEST_TERMS[random.Next(TEST_TERMS.Length)];
            }
            else
            {
                // return a random unicode term
                return RandomString();
            }
        }

        /*
         * Return a random unicode term, like TestStressIndexing.
         */

        private String RandomString()
        {
            int end = random.Next(20);
            if (buffer.Length < 1 + end)
            {
                char[] newBuffer = new char[(int) ((1 + end)*1.25)];
                Array.Copy(buffer, 0, newBuffer, 0, buffer.Length);
                buffer = newBuffer;
            }
            for (int i = 0; i < end - 1; i++)
            {
                int t = random.Next(6);
                if (0 == t && i < end - 1)
                {
                    // Make a surrogate pair
                    // High surrogate
                    buffer[i++] = (char) NextInt(0xd800, 0xdc00);
                    // Low surrogate
                    buffer[i] = (char) NextInt(0xdc00, 0xe000);
                }
                else if (t <= 1) buffer[i] = (char) random.Next(0x80);
                else if (2 == t) buffer[i] = (char) NextInt(0x80, 0x800);
                else if (3 == t) buffer[i] = (char) NextInt(0x800, 0xd7ff);
                else if (4 == t) buffer[i] = (char) NextInt(0xe000, 0xffff);
                else if (5 == t)
                {
                    // Illegal unpaired surrogate
                    if (random.Next(1) == 1) buffer[i] = (char) NextInt(0xd800, 0xdc00);
                    else buffer[i] = (char) NextInt(0xdc00, 0xe000);
                }
            }
            return new String(buffer, 0, end);
        }

        private char[] buffer = new char[20];
        // start is inclusive and end is exclusive
        private int NextInt(int start, int end)
        {
            return start + random.Next(end - start);
        }
    }
}
