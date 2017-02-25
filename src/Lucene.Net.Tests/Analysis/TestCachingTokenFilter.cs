using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;

namespace Lucene.Net.Analysis
{
    using Lucene.Net.Store;
    using NUnit.Framework;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;

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

    using Document = Documents.Document;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using TextField = TextField;

    [TestFixture]
    public class TestCachingTokenFilter : BaseTokenStreamTestCase
    {
        private string[] Tokens = new string[] { "term1", "term2", "term3", "term2" };

        [Test]
        public virtual void TestCaching()
        {
            Directory dir = new RAMDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            TokenStream stream = new TokenStreamAnonymousInnerClassHelper(this);

            stream = new CachingTokenFilter(stream);

            doc.Add(new TextField("preanalyzed", stream));

            // 1) we consume all tokens twice before we add the doc to the index
            CheckTokens(stream);
            stream.Reset();
            CheckTokens(stream);

            // 2) now add the document to the index and verify if all tokens are indexed
            //    don't reset the stream here, the DocumentWriter should do that implicitly
            writer.AddDocument(doc);

            IndexReader reader = writer.Reader;
            DocsAndPositionsEnum termPositions = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), "preanalyzed", new BytesRef("term1"));
            Assert.IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(1, termPositions.Freq);
            Assert.AreEqual(0, termPositions.NextPosition());

            termPositions = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), "preanalyzed", new BytesRef("term2"));
            Assert.IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(2, termPositions.Freq);
            Assert.AreEqual(1, termPositions.NextPosition());
            Assert.AreEqual(3, termPositions.NextPosition());

            termPositions = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), "preanalyzed", new BytesRef("term3"));
            Assert.IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(1, termPositions.Freq);
            Assert.AreEqual(2, termPositions.NextPosition());
            reader.Dispose();
            writer.Dispose();
            // 3) reset stream and consume tokens again
            stream.Reset();
            CheckTokens(stream);
            dir.Dispose();
        }

        private class TokenStreamAnonymousInnerClassHelper : TokenStream
        {
            private TestCachingTokenFilter OuterInstance;

            public TokenStreamAnonymousInnerClassHelper(TestCachingTokenFilter outerInstance)
            {
                InitMembers(outerInstance);
            }

            public void InitMembers(TestCachingTokenFilter outerInstance)
            {
                this.OuterInstance = outerInstance;
                index = 0;
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
            }

            private int index;
            private ICharTermAttribute termAtt;
            private IOffsetAttribute offsetAtt;

            public sealed override bool IncrementToken()
            {
                if (index == OuterInstance.Tokens.Length)
                {
                    return false;
                }
                else
                {
                    ClearAttributes();
                    termAtt.Append(OuterInstance.Tokens[index++]);
                    offsetAtt.SetOffset(0, 0);
                    return true;
                }
            }
        }

        private void CheckTokens(TokenStream stream)
        {
            int count = 0;

            ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();
            while (stream.IncrementToken())
            {
                Assert.IsTrue(count < Tokens.Length);
                Assert.AreEqual(Tokens[count], termAtt.ToString());
                count++;
            }

            Assert.AreEqual(Tokens.Length, count);
        }
    }
}