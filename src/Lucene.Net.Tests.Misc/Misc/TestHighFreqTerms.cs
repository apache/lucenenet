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

using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Misc
{
    [SuppressCodecs("Lucene3x")]
    public class TestHighFreqTerms : LuceneTestCase
    {
        private static IndexWriter writer = null;
        private static Directory dir = null;
        private static IndexReader reader = null;

        [OneTimeSetUp]
        public override void BeforeClass() // LUCENENET specific - renamed from SetUpClass() to ensure calling order vs base class
        {
            base.BeforeClass();

            dir = NewDirectory();
            writer = new IndexWriter(dir, NewIndexWriterConfig(Random,
               TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false))
               .SetMaxBufferedDocs(2));
            IndexDocs(writer);
            reader = DirectoryReader.Open(dir);
            TestUtil.CheckIndex(dir);
        }

        [OneTimeTearDown]
        public override void AfterClass() // LUCENENET specific - renamed from TearDownClass() to ensure calling order vs base class
        {
            reader.Dispose();
            dir.Dispose();
            dir = null;
            reader = null;
            writer = null;

            base.AfterClass();
        }
        /******************** Tests for getHighFreqTerms **********************************/

        // test without specifying field (i.e. if we pass in field=null it should examine all fields)
        // the term "diff" in the field "different_field" occurs 20 times and is the highest df term
        [Test]
        public void TestFirstTermHighestDocFreqAllFields()
        {
            int numTerms = 12;
            string field = null;
            TermStats[]
            terms = HighFreqTerms.GetHighFreqTerms(reader, numTerms, field, HighFreqTerms.DocFreqComparer.Default);
            assertEquals("Term with highest docfreq is first", 20, terms[0].DocFreq);
        }

        [Test]
        public void TestFirstTermHighestDocFreq()
        {
            int numTerms = 12;
            string field = "FIELD_1";
            TermStats[]
            terms = HighFreqTerms.GetHighFreqTerms(reader, numTerms, field, HighFreqTerms.DocFreqComparer.Default);
            assertEquals("Term with highest docfreq is first", 10, terms[0].DocFreq);
        }

        [Test]
        public void TestOrderedByDocFreqDescending()
        {
            int numTerms = 12;
            string field = "FIELD_1";
            TermStats[]
            terms = HighFreqTerms.GetHighFreqTerms(reader, numTerms, field, HighFreqTerms.DocFreqComparer.Default);
            for (int i = 0; i < terms.Length; i++)
            {
                if (i > 0)
                {
                    assertTrue("out of order " + terms[i - 1].DocFreq + "should be >= " + terms[i].DocFreq, terms[i - 1].DocFreq >= terms[i].DocFreq);
                }
            }
        }

        [Test]
        public void TestNumTerms()
        {
            int numTerms = 12;
            string field = null;
            TermStats[]
            terms = HighFreqTerms.GetHighFreqTerms(reader, numTerms, field, HighFreqTerms.DocFreqComparer.Default);
            assertEquals("length of terms array equals numTerms :" + numTerms, numTerms, terms.Length);
        }

        [Test]
        public void TestGetHighFreqTerms()
        {
            int numTerms = 12;
            string field = "FIELD_1";
            TermStats[]
            terms = HighFreqTerms.GetHighFreqTerms(reader, numTerms, field, HighFreqTerms.DocFreqComparer.Default);

            for (int i = 0; i < terms.Length; i++)
            {
                string termtext = terms[i].termtext.Utf8ToString();
                // hardcoded highTF or highTFmedDF
                if (termtext.Contains("highTF"))
                {
                    if (termtext.Contains("medDF"))
                    {
                        assertEquals("doc freq is not as expected", 5, terms[i].DocFreq);
                    }
                    else
                    {
                        assertEquals("doc freq is not as expected", 1, terms[i].DocFreq);
                    }
                }
                else
                {
                    int n = Convert.ToInt32(termtext);
                    assertEquals("doc freq is not as expected", GetExpecteddocFreq(n),
                        terms[i].DocFreq);
                }
            }
        }

        /********************Test sortByTotalTermFreq**********************************/

        [Test]
        public void TestFirstTermHighestTotalTermFreq()
        {
            int numTerms = 20;
            string field = null;
            TermStats[]
            terms = HighFreqTerms.GetHighFreqTerms(reader, numTerms, field, HighFreqTerms.TotalTermFreqComparer.Default);
            assertEquals("Term with highest totalTermFreq is first", 200, terms[0].TotalTermFreq);
        }

        [Test]
        public void TestFirstTermHighestTotalTermFreqDifferentField()
        {
            int numTerms = 20;
            string field = "different_field";
            TermStats[]
            terms = HighFreqTerms.GetHighFreqTerms(reader, numTerms, field, HighFreqTerms.TotalTermFreqComparer.Default);
            assertEquals("Term with highest totalTermFreq is first" + terms[0].GetTermText(), 150, terms[0].TotalTermFreq);
        }

        [Test]
        public void TestOrderedByTermFreqDescending()
        {
            int numTerms = 12;
            string field = "FIELD_1";
            TermStats[]
            terms = HighFreqTerms.GetHighFreqTerms(reader, numTerms, field, HighFreqTerms.TotalTermFreqComparer.Default);

            for (int i = 0; i < terms.Length; i++)
            {
                // check that they are sorted by descending termfreq
                // order
                if (i > 0)
                {
                    assertTrue("out of order" + terms[i - 1] + " > " + terms[i], terms[i - 1].TotalTermFreq >= terms[i].TotalTermFreq);
                }
            }
        }

        [Test]
        public void TestGetTermFreqOrdered()
        {
            int numTerms = 12;
            string field = "FIELD_1";
            TermStats[]
            terms = HighFreqTerms.GetHighFreqTerms(reader, numTerms, field, HighFreqTerms.TotalTermFreqComparer.Default);

            for (int i = 0; i < terms.Length; i++)
            {
                string text = terms[i].termtext.Utf8ToString();
                if (text.Contains("highTF"))
                {
                    if (text.Contains("medDF"))
                    {
                        assertEquals("total term freq is expected", 125,
                                     terms[i].TotalTermFreq);
                    }
                    else
                    {
                        assertEquals("total term freq is expected", 200,
                                     terms[i].TotalTermFreq);
                    }

                }
                else
                {
                    int n = Convert.ToInt32(text);
                    assertEquals("doc freq is expected", GetExpecteddocFreq(n),
                                 terms[i].DocFreq);
                    assertEquals("total term freq is expected", GetExpectedtotalTermFreq(n),
                                 terms[i].TotalTermFreq);
                }
            }
        }

        /********************Testing Utils**********************************/

        /// <summary>
        /// LUCENENET NOTE: Made non-static because it depends on NewIndexField that is also non-static
        /// </summary>
        private void IndexDocs(IndexWriter writer)
        {
            Random rnd = Random;

            /**
             * Generate 10 documents where term n  has a docFreq of n and a totalTermFreq of n*2 (squared). 
             */
            for (int i = 1; i <= 10; i++)
            {
                Document doc = new Document();
                string content = GetContent(i);

                doc.Add(NewTextField(rnd, "FIELD_1", content, Field.Store.YES));
                //add a different field
                doc.Add(NewTextField(rnd, "different_field", "diff", Field.Store.YES));
                writer.AddDocument(doc);
            }

            //add 10 more docs with the term "diff" this will make it have the highest docFreq if we don't ask for the
            //highest freq terms for a specific field.
            for (int i = 1; i <= 10; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField(rnd, "different_field", "diff", Field.Store.YES));
                writer.AddDocument(doc);
            }
            // add some docs where tf < df so we can see if sorting works
            // highTF low df
            int highTF = 200;
            Document doc2 = new Document();
            string content2 = "";
            for (int i = 0; i < highTF; i++)
            {
                content2 += "highTF ";
            }
            doc2.Add(NewTextField(rnd, "FIELD_1", content2, Field.Store.YES));
            writer.AddDocument(doc2);
            // highTF medium df =5
            int medium_df = 5;
            for (int i = 0; i < medium_df; i++)
            {
                int tf = 25;
                Document newdoc = new Document();
                string newcontent = "";
                for (int j = 0; j < tf; j++)
                {
                    newcontent += "highTFmedDF ";
                }
                newdoc.Add(NewTextField(rnd, "FIELD_1", newcontent, Field.Store.YES));
                writer.AddDocument(newdoc);
            }
            // add a doc with high tf in field different_field
            int targetTF = 150;
            doc2 = new Document();
            content2 = "";
            for (int i = 0; i < targetTF; i++)
            {
                content2 += "TF150 ";
            }
            doc2.Add(NewTextField(rnd, "different_field", content2, Field.Store.YES));
            writer.AddDocument(doc2);
            writer.Dispose();

        }

        /**
         *  getContent
         *  return string containing numbers 1 to i with each number n occurring n times.
         *  i.e. for input of 3 return string "3 3 3 2 2 1" 
         */

        private static string GetContent(int i)
        {
            string s = "";
            for (int j = 10; j >= i; j--)
            {
                for (int k = 0; k < j; k++)
                {
                    // if j is 3 we return "3 3 3"
                    s += j.ToString() + " ";
                }
            }
            return s;
        }

        private static int GetExpectedtotalTermFreq(int i)
        {
            return GetExpecteddocFreq(i) * i;
        }

        private static int GetExpecteddocFreq(int i)
        {
            return i;
        }
    }
}
