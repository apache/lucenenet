using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;

namespace Lucene.Net.Search.VectorHighlight
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

    public class SimpleFragmentsBuilderTest : AbstractTestCase
    {
        [Test]
        public void Test1TermIndex()
        {
            FieldFragList ffl = Ffl(new TermQuery(new Term(F, "a")), "a");
            SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
            assertEquals("<b>a</b>", sfb.CreateFragment(reader, 0, F, ffl));

            // change tags
            sfb = new SimpleFragmentsBuilder(new String[] { "[" }, new String[] { "]" });
            assertEquals("[a]", sfb.CreateFragment(reader, 0, F, ffl));
        }

        [Test]
        public void Test2Frags()
        {
            FieldFragList ffl = Ffl(new TermQuery(new Term(F, "a")), "a b b b b b b b b b b b a b a b");
            SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
            String[] f = sfb.CreateFragments(reader, 0, F, ffl, 3);
            // 3 snippets requested, but should be 2
            assertEquals(2, f.Length);
            assertEquals("<b>a</b> b b b b b b b b b b", f[0]);
            assertEquals("b b <b>a</b> b <b>a</b> b", f[1]);
        }

        [Test]
        public void Test3Frags()
        {
            BooleanQuery booleanQuery = new BooleanQuery();
            booleanQuery.Add(new TermQuery(new Term(F, "a")), BooleanClause.Occur.SHOULD);
            booleanQuery.Add(new TermQuery(new Term(F, "c")), BooleanClause.Occur.SHOULD);

            FieldFragList ffl = Ffl(booleanQuery, "a b b b b b b b b b b b a b a b b b b b c a a b b");
            SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
            String[] f = sfb.CreateFragments(reader, 0, F, ffl, 3);
            assertEquals(3, f.Length);
            assertEquals("<b>a</b> b b b b b b b b b b", f[0]);
            assertEquals("b b <b>a</b> b <b>a</b> b b b b b c", f[1]);
            assertEquals("<b>c</b> <b>a</b> <b>a</b> b b", f[2]);
        }

        [Test]
        public void TestTagsAndEncoder()
        {
            FieldFragList ffl = Ffl(new TermQuery(new Term(F, "a")), "<h1> a </h1>");
            SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
            String[] preTags = { "[" };
            String[] postTags = { "]" };
            assertEquals("&lt;h1&gt; [a] &lt;&#x2F;h1&gt;",
                sfb.CreateFragment(reader, 0, F, ffl, preTags, postTags, new SimpleHTMLEncoder()));
        }

        private FieldFragList Ffl(Query query, String indexValue)
        {
            make1d1fIndex(indexValue);
            FieldQuery fq = new FieldQuery(query, true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            return new SimpleFragListBuilder().CreateFieldFragList(fpl, 20);
        }

        [Test]
        public void Test1PhraseShortMV()
        {
            makeIndexShortMV();

            FieldQuery fq = new FieldQuery(tq("d"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl, 100);
            SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
            // Should we probably be trimming?
            assertEquals("  a b c  <b>d</b> e", sfb.CreateFragment(reader, 0, F, ffl));
        }

        [Test]
        public void Test1PhraseLongMV()
        {
            makeIndexLongMV();

            FieldQuery fq = new FieldQuery(pqF("search", "engines"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl, 100);
            SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
            assertEquals("customization: The most <b>search engines</b> use only one of these methods. Even the <b>search engines</b> that says they can",
                sfb.CreateFragment(reader, 0, F, ffl));
        }

        [Test]
        public void Test1PhraseLongMVB()
        {
            makeIndexLongMVB();

            FieldQuery fq = new FieldQuery(pqF("sp", "pe", "ee", "ed"), true, true); // "speed" -(2gram)-> "sp","pe","ee","ed"
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl, 100);
            SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
            assertEquals("additional hardware. \nWhen you talk about processing <b>speed</b>, the", sfb.CreateFragment(reader, 0, F, ffl));
        }

        [Test]
        public void TestUnstoredField()
        {
            makeUnstoredIndex();

            FieldQuery fq = new FieldQuery(tq("aaa"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl, 100);
            SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
            assertNull(sfb.CreateFragment(reader, 0, F, ffl));
        }

        protected void makeUnstoredIndex()
        {
            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(
                TEST_VERSION_CURRENT, analyzerW).SetOpenMode(OpenMode.CREATE));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = (true);
            customType.StoreTermVectorOffsets = (true);
            customType.StoreTermVectorPositions = (true);
            doc.Add(new Field(F, "aaa", customType));
            //doc.Add( new Field( F, "aaa", Store.NO, Index.ANALYZED, TermVector.WITH_POSITIONS_OFFSETS ) );
            writer.AddDocument(doc);
            writer.Dispose();
            if (reader != null) reader.Dispose();
            reader = DirectoryReader.Open(dir);
        }

        [Test]
        public void Test1StrMV()
        {
            makeIndexStrMV();

            FieldQuery fq = new FieldQuery(tq("defg"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl, 100);
            SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
            sfb.SetMultiValuedSeparator('/');
            assertEquals("abc/<b>defg</b>/hijkl", sfb.CreateFragment(reader, 0, F, ffl));
        }

        [Test]
        public void TestMVSeparator()
        {
            makeIndexShortMV();

            FieldQuery fq = new FieldQuery(tq("d"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl, 100);
            SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
            sfb.SetMultiValuedSeparator('/');
            assertEquals("//a b c//<b>d</b> e", sfb.CreateFragment(reader, 0, F, ffl));
        }

        [Test]
        public void TestDiscreteMultiValueHighlighting()
        {
            makeIndexShortMV();

            FieldQuery fq = new FieldQuery(tq("d"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            SimpleFragListBuilder sflb = new SimpleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl, 100);
            SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
            sfb.IsDiscreteMultiValueHighlighting = (true);
            assertEquals("<b>d</b> e", sfb.CreateFragment(reader, 0, F, ffl));

            make1dmfIndex("some text to highlight", "highlight other text");
            fq = new FieldQuery(tq("text"), true, true);
            stack = new FieldTermStack(reader, 0, F, fq);
            fpl = new FieldPhraseList(stack, fq);
            sflb = new SimpleFragListBuilder();
            ffl = sflb.CreateFieldFragList(fpl, 32);
            String[] result = sfb.CreateFragments(reader, 0, F, ffl, 3);
            assertEquals(2, result.Length);
            assertEquals("some <b>text</b> to highlight", result[0]);
            assertEquals("highlight other <b>text</b>", result[1]);

            fq = new FieldQuery(tq("highlight"), true, true);
            stack = new FieldTermStack(reader, 0, F, fq);
            fpl = new FieldPhraseList(stack, fq);
            sflb = new SimpleFragListBuilder();
            ffl = sflb.CreateFieldFragList(fpl, 32);
            result = sfb.CreateFragments(reader, 0, F, ffl, 3);
            assertEquals(2, result.Length);
            assertEquals("text to <b>highlight</b>", result[0]);
            assertEquals("<b>highlight</b> other text", result[1]);
        }

        [Test]
        public void TestRandomDiscreteMultiValueHighlighting()
        {
            String[]
            randomValues = new String[3 + Random().nextInt(10 * RANDOM_MULTIPLIER)];
            for (int i = 0; i < randomValues.Length; i++)
            {
                String randomValue;
                do
                {
                    randomValue = TestUtil.RandomSimpleString(Random());
                } while ("".equals(randomValue));
                randomValues[i] = randomValue;
            }

            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(
                Random(),
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));

            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = (true);
            customType.StoreTermVectorOffsets = (true);
            customType.StoreTermVectorPositions = (true);

            int numDocs = randomValues.Length * 5;
            int numFields = 2 + Random().nextInt(5);
            int numTerms = 2 + Random().nextInt(3);
            List<Doc> docs = new List<Doc>(numDocs);
            List<Document> documents = new List<Document>(numDocs);
            IDictionary<String, ISet<int>> valueToDocId = new HashMap<String, ISet<int>>();
            for (int i = 0; i < numDocs; i++)
            {
                Document document = new Document();
                String[][] fields = RectangularArrays.ReturnRectangularStringArray(numFields, numTerms); //new String[numFields][numTerms];
                for (int j = 0; j < numFields; j++)
                {
                    String[] fieldValues = new String[numTerms];
                    fieldValues[0] = getRandomValue(randomValues, valueToDocId, i);
                    StringBuilder builder = new StringBuilder(fieldValues[0]);
                    for (int k = 1; k < numTerms; k++)
                    {
                        fieldValues[k] = getRandomValue(randomValues, valueToDocId, i);
                        builder.append(' ').append(fieldValues[k]);
                    }
                    document.Add(new Field(F, builder.toString(), customType));
                    fields[j] = fieldValues;
                }
                docs.Add(new Doc(fields));
                documents.Add(document);
            }
            writer.AddDocuments(documents);
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);

            try
            {
                int highlightIters = 1 + Random().nextInt(120 * RANDOM_MULTIPLIER);
                for (int highlightIter = 0; highlightIter < highlightIters; highlightIter++)
                {
                    String queryTerm = randomValues[Random().nextInt(randomValues.Length)];
                    var iter = valueToDocId[queryTerm].GetEnumerator();
                    iter.MoveNext();
                    int randomHit = iter.Current;
                    List<StringBuilder> builders = new List<StringBuilder>();
                    foreach (String[] fieldValues in docs[randomHit].fieldValues)
                    {
                        StringBuilder builder = new StringBuilder();
                        bool hit = false;
                        for (int i = 0; i < fieldValues.Length; i++)
                        {
                            if (queryTerm.equals(fieldValues[i]))
                            {
                                builder.append("<b>").append(queryTerm).append("</b>");
                                hit = true;
                            }
                            else
                            {
                                builder.append(fieldValues[i]);
                            }
                            if (i != fieldValues.Length - 1)
                            {
                                builder.append(' ');
                            }
                        }
                        if (hit)
                        {
                            builders.Add(builder);
                        }
                    }

                    FieldQuery fq = new FieldQuery(tq(queryTerm), true, true);
                    FieldTermStack stack = new FieldTermStack(reader, randomHit, F, fq);

                    FieldPhraseList fpl = new FieldPhraseList(stack, fq);
                    SimpleFragListBuilder sflb = new SimpleFragListBuilder(100);
                    FieldFragList ffl = sflb.CreateFieldFragList(fpl, 300);

                    SimpleFragmentsBuilder sfb = new SimpleFragmentsBuilder();
                    sfb.IsDiscreteMultiValueHighlighting = (true);
                    String[] actualFragments = sfb.CreateFragments(reader, randomHit, F, ffl, numFields);
                    assertEquals(builders.size(), actualFragments.Length);
                    for (int i = 0; i < actualFragments.Length; i++)
                    {
                        assertEquals(builders[i].toString(), actualFragments[i]);
                    }
                }
            }
            finally
            {
                reader.Dispose();
                dir.Dispose();
            }
        }

        private String getRandomValue(String[] randomValues, IDictionary<String, ISet<int>> valueToDocId, int docId)
        {
            String value = randomValues[Random().nextInt(randomValues.Length)];
            if (!valueToDocId.ContainsKey(value))
            {
                valueToDocId.Put(value, new HashSet<int>());
            }
            valueToDocId[value].Add(docId);
            return value;
        }

        internal class Doc
        {

            internal readonly String[][] fieldValues;

            internal Doc(String[][] fieldValues)
            {
                this.fieldValues = fieldValues;
            }
        }
    }
}
