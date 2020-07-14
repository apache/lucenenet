using J2N;
using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Sandbox.Queries
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
     * Tests the results of fuzzy against pre-recorded output 
     * The format of the file is the following:
     * 
     * Header Row: # of bits: generate 2^n sequential documents 
     * with a value of Integer.toBinaryString
     * 
     * Entries: an entry is a param spec line, a resultCount line, and
     * then 'resultCount' results lines. The results lines are in the
     * expected order.
     * 
     * param spec line: a comma-separated list of params to FuzzyQuery
     *   (query, prefixLen, pqSize, minScore)
     * query = query text as a number (expand with Integer.toBinaryString)
     * prefixLen = prefix length
     * pqSize = priority queue maximum size for TopTermsBoostOnlyBooleanQueryRewrite
     * minScore = minimum similarity
     * 
     * resultCount line: total number of expected hits.
     * 
     * results line: comma-separated docID, score pair
     **/
    public class TestSlowFuzzyQuery2 : LuceneTestCase
    {
        /** epsilon for score comparisons */
        static readonly float epsilon = 0.00001f;

        static int[][] mappings = new int[][] {
            new int[] { 0x40, 0x41 },
            new int[] { 0x40, 0x0195 },
            new int[] { 0x40, 0x0906 },
            new int[] { 0x40, 0x1040F },
            new int[] { 0x0194, 0x0195 },
            new int[] { 0x0194, 0x0906 },
            new int[] { 0x0194, 0x1040F },
            new int[] { 0x0905, 0x0906 },
            new int[] { 0x0905, 0x1040F },
            new int[] { 0x1040E, 0x1040F }
          };

        [Test]
        public void TestFromTestData()
        {
            // TODO: randomize!
            assertFromTestData(mappings[Random.nextInt(mappings.Length)]);
        }

        public void assertFromTestData(int[] codePointTable)
        {
            if (Verbose)
            {
                Console.WriteLine("TEST: codePointTable=" + codePointTable);
            }
            Stream stream = GetType().getResourceAsStream("fuzzyTestData.txt");
            TextReader reader = new StreamReader(stream, Encoding.UTF8);

            int bits = int.Parse(reader.ReadLine(), CultureInfo.InvariantCulture);
            int terms = (int)Math.Pow(2, bits);

            Store.Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.KEYWORD, false)).SetMergePolicy(NewLogMergePolicy()));

            Document doc = new Document();
            Field field = NewTextField("field", "", Field.Store.NO);
            doc.Add(field);

            for (int i = 0; i < terms; i++)
            {
                field.SetStringValue(MapInt(codePointTable, i));
                writer.AddDocument(doc);
            }

            IndexReader r = writer.GetReader();
            IndexSearcher searcher = NewSearcher(r);
            if (Verbose)
            {
                Console.WriteLine("TEST: searcher=" + searcher);
            }
            // even though this uses a boost-only rewrite, this test relies upon queryNorm being the default implementation,
            // otherwise scores are different!
            searcher.Similarity = (new DefaultSimilarity());

            writer.Dispose();
            String line;
            int lineNum = 0;
            while ((line = reader.ReadLine()) != null)
            {
                lineNum++;
                String[] @params = line.Split(',').TrimEnd();
                String query = MapInt(codePointTable, int.Parse(@params[0], CultureInfo.InvariantCulture));
                int prefix = int.Parse(@params[1], CultureInfo.InvariantCulture);
                int pqSize = int.Parse(@params[2], CultureInfo.InvariantCulture);
                float minScore = float.Parse(@params[3], CultureInfo.InvariantCulture);
#pragma warning disable 612, 618
                SlowFuzzyQuery q = new SlowFuzzyQuery(new Term("field", query), minScore, prefix);
#pragma warning restore 612, 618
                q.MultiTermRewriteMethod = new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite(pqSize);
                int expectedResults = int.Parse(reader.ReadLine(), CultureInfo.InvariantCulture);
                TopDocs docs = searcher.Search(q, expectedResults);
                assertEquals(expectedResults, docs.TotalHits);
                for (int i = 0; i < expectedResults; i++)
                {
                    String[] scoreDoc = reader.ReadLine().Split(',').TrimEnd();
                    assertEquals(int.Parse(scoreDoc[0], CultureInfo.InvariantCulture), docs.ScoreDocs[i].Doc);
                    assertEquals(float.Parse(scoreDoc[1], CultureInfo.InvariantCulture), docs.ScoreDocs[i].Score, epsilon);
                }
            }
            r.Dispose();
            dir.Dispose();
        }

        /* map bits to unicode codepoints */
        private static String MapInt(int[] codePointTable, int i)
        {
            StringBuilder sb = new StringBuilder();
            String binary = i.ToBinaryString();
            for (int j = 0; j < binary.Length; j++)
                sb.AppendCodePoint(codePointTable[binary[j] - '0']);
            return sb.toString();
        }

        /* Code to generate test data
        public static void main(String args[]) throws Exception {
          int bits = 3;
          System.out.println(bits);
          int terms = (int) Math.pow(2, bits);

          RAMDirectory dir = new RAMDirectory();
          IndexWriter writer = new IndexWriter(dir, new KeywordAnalyzer(),
              IndexWriter.MaxFieldLength.UNLIMITED);

          Document doc = new Document();
          Field field = newField("field", "", Field.Store.NO, Field.Index.ANALYZED);
          doc.add(field);

          for (int i = 0; i < terms; i++) {
            field.setValue(Integer.toBinaryString(i));
            writer.addDocument(doc);
          }

          writer.forceMerge(1);
          writer.close();

          IndexSearcher searcher = new IndexSearcher(dir);
          for (int prefix = 0; prefix < bits; prefix++)
            for (int pqsize = 1; pqsize <= terms; pqsize++)
              for (float minscore = 0.1F; minscore < 1F; minscore += 0.2F)
                for (int query = 0; query < terms; query++) {
                  FuzzyQuery q = new FuzzyQuery(
                      new Term("field", Integer.toBinaryString(query)), minscore, prefix);
                  q.setRewriteMethod(new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite(pqsize));
                  System.out.println(query + "," + prefix + "," + pqsize + "," + minscore);
                  TopDocs docs = searcher.search(q, terms);
                  System.out.println(docs.totalHits);
                  for (int i = 0; i < docs.totalHits; i++)
                    System.out.println(docs.scoreDocs[i].doc + "," + docs.scoreDocs[i].score);
                }
        }
        */
    }
}
