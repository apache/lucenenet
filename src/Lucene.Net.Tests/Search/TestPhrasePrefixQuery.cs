using J2N.Collections.Generic.Extensions;
using Lucene.Net.Documents;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// this class tests PhrasePrefixQuery class.
    /// </summary>
    [TestFixture]
    public class TestPhrasePrefixQuery : LuceneTestCase
    {
        ///
        [Test]
        public virtual void TestPhrasePrefix()
        {
            Directory indexStore = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, indexStore);
            Document doc1 = new Document();
            Document doc2 = new Document();
            Document doc3 = new Document();
            Document doc4 = new Document();
            Document doc5 = new Document();
            doc1.Add(NewTextField("body", "blueberry pie", Field.Store.YES));
            doc2.Add(NewTextField("body", "blueberry strudel", Field.Store.YES));
            doc3.Add(NewTextField("body", "blueberry pizza", Field.Store.YES));
            doc4.Add(NewTextField("body", "blueberry chewing gum", Field.Store.YES));
            doc5.Add(NewTextField("body", "piccadilly circus", Field.Store.YES));
            writer.AddDocument(doc1);
            writer.AddDocument(doc2);
            writer.AddDocument(doc3);
            writer.AddDocument(doc4);
            writer.AddDocument(doc5);
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);

            // PhrasePrefixQuery query1 = new PhrasePrefixQuery();
            MultiPhraseQuery query1 = new MultiPhraseQuery();
            // PhrasePrefixQuery query2 = new PhrasePrefixQuery();
            MultiPhraseQuery query2 = new MultiPhraseQuery();
            query1.Add(new Term("body", "blueberry"));
            query2.Add(new Term("body", "strawberry"));

            LinkedList<Term> termsWithPrefix = new LinkedList<Term>();

            // this TermEnum gives "piccadilly", "pie" and "pizza".
            string prefix = "pi";
            TermsEnum te = MultiFields.GetFields(reader).GetTerms("body").GetEnumerator();
            te.SeekCeil(new BytesRef(prefix));
            do
            {
                string s = te.Term.Utf8ToString();
                if (s.StartsWith(prefix, StringComparison.Ordinal))
                {
                    termsWithPrefix.AddLast(new Term("body", s));
                }
                else
                {
                    break;
                }
            } while (te.MoveNext());

            query1.Add(termsWithPrefix.ToArray(/*new Term[0]*/));
            query2.Add(termsWithPrefix.ToArray(/*new Term[0]*/));

            ScoreDoc[] result;
            result = searcher.Search(query1, null, 1000).ScoreDocs;
            Assert.AreEqual(2, result.Length);

            result = searcher.Search(query2, null, 1000).ScoreDocs;
            Assert.AreEqual(0, result.Length);
            reader.Dispose();
            indexStore.Dispose();
        }
    }
}