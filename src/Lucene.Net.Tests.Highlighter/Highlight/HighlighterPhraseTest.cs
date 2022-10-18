using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Search.Highlight
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

    public class HighlighterPhraseTest : LuceneTestCase
    {
        private static readonly String FIELD = "text";

        [Test]
        public void TestConcurrentPhrase()
        {
            String TEXT = "the fox jumped";
            Directory directory = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(directory,
               NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            try
            {
                Document document = new Document();
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectorOffsets = (true);
                customType.StoreTermVectorPositions = (true);
                customType.StoreTermVectors = (true);
                document.Add(new Field(FIELD, new TokenStreamConcurrent(), customType));
                indexWriter.AddDocument(document);
            }
            finally
            {
                indexWriter.Dispose();
            }
            IndexReader indexReader = DirectoryReader.Open(directory);
            try
            {
                assertEquals(1, indexReader.NumDocs);
                IndexSearcher indexSearcher = NewSearcher(indexReader);
                PhraseQuery phraseQuery = new PhraseQuery();
                phraseQuery.Add(new Term(FIELD, "fox"));
                phraseQuery.Add(new Term(FIELD, "jumped"));
                phraseQuery.Slop = (0);
                TopDocs hits = indexSearcher.Search(phraseQuery, 1);
                assertEquals(1, hits.TotalHits);
                Highlighter highlighter = new Highlighter(
                         new SimpleHTMLFormatter(), new SimpleHTMLEncoder(),
                         new QueryScorer(phraseQuery));

                TokenStream tokenStream = TokenSources
                   .GetTokenStream(indexReader.GetTermVector(
                       0, FIELD), false);
                assertEquals(highlighter.GetBestFragment(new TokenStreamConcurrent(),
                    TEXT), highlighter.GetBestFragment(tokenStream, TEXT));
            }
            finally
            {
                indexReader.Dispose();
                directory.Dispose();
            }
        }
        [Test]
        public void TestConcurrentSpan()
        {
            String TEXT = "the fox jumped";
            Directory directory = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(directory,
               NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            try
            {
                Document document = new Document();

                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectorOffsets = (true);
                customType.StoreTermVectorPositions = (true);
                customType.StoreTermVectors = (true);
                document.Add(new Field(FIELD, new TokenStreamConcurrent(), customType));
                indexWriter.AddDocument(document);
            }
            finally
            {
                indexWriter.Dispose();
            }
            IndexReader indexReader = DirectoryReader.Open(directory);
            try
            {
                assertEquals(1, indexReader.NumDocs);
                IndexSearcher indexSearcher = NewSearcher(indexReader);
                Query phraseQuery = new SpanNearQuery(new SpanQuery[] {
                    new SpanTermQuery(new Term(FIELD, "fox")),
                    new SpanTermQuery(new Term(FIELD, "jumped")) }, 0, true);
                FixedBitSet bitset = new FixedBitSet(indexReader.MaxDoc);
                indexSearcher.Search(phraseQuery, new ConcurrentSpanCollectorAnonymousClass(this, bitset));

                assertEquals(1, bitset.Cardinality);
                int maxDoc = indexReader.MaxDoc;
                Highlighter highlighter = new Highlighter(
                         new SimpleHTMLFormatter(), new SimpleHTMLEncoder(),
                         new QueryScorer(phraseQuery));
                for (int position = bitset.NextSetBit(0); position >= 0 && position < maxDoc - 1; position = bitset
                  .NextSetBit(position + 1))
                {
                    assertEquals(0, position);
                    TokenStream tokenStream = TokenSources.GetTokenStream(
                               indexReader.GetTermVector(position,
                                   FIELD), false);
                    assertEquals(highlighter.GetBestFragment(new TokenStreamConcurrent(),
                        TEXT), highlighter.GetBestFragment(tokenStream, TEXT));
                }
            }
            finally
            {
                indexReader.Dispose();
                directory.Dispose();
            }
        }

        private sealed class ConcurrentSpanCollectorAnonymousClass : ICollector
        {
            private readonly HighlighterPhraseTest outerInstance;
            private readonly FixedBitSet bitset;
            public ConcurrentSpanCollectorAnonymousClass(HighlighterPhraseTest outerInstance, FixedBitSet bitset)
            {
                this.outerInstance = outerInstance;
                this.bitset = bitset;
            }

            private int baseDoc;

            public bool AcceptsDocsOutOfOrder => true;

            public void Collect(int i)
            {
                bitset.Set(this.baseDoc + i);
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                this.baseDoc = context.DocBase;
            }

            public void SetScorer(Scorer scorer)
            {
                // Do Nothing
            }
        }

        [Test]
        public void TestSparsePhrase()
        {
            String TEXT = "the fox did not jump";
            Directory directory = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(directory,
               NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            try
            {
                Document document = new Document();

                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectorOffsets = (true);
                customType.StoreTermVectorPositions = (true);
                customType.StoreTermVectors = (true);
                document.Add(new Field(FIELD, new TokenStreamSparse(), customType));
                indexWriter.AddDocument(document);
            }
            finally
            {
                indexWriter.Dispose();
            }
            IndexReader indexReader = DirectoryReader.Open(directory);
            try
            {
                assertEquals(1, indexReader.NumDocs);
                IndexSearcher indexSearcher = NewSearcher(indexReader);
                PhraseQuery phraseQuery = new PhraseQuery();
                phraseQuery.Add(new Term(FIELD, "did"));
                phraseQuery.Add(new Term(FIELD, "jump"));
                phraseQuery.Slop = (0);
                TopDocs hits = indexSearcher.Search(phraseQuery, 1);
                assertEquals(0, hits.TotalHits);
                Highlighter highlighter = new Highlighter(
                         new SimpleHTMLFormatter(), new SimpleHTMLEncoder(),
                         new QueryScorer(phraseQuery));
                TokenStream tokenStream = TokenSources
                   .GetTokenStream(indexReader.GetTermVector(
                       0, FIELD), false);
                assertEquals(
                    highlighter.GetBestFragment(new TokenStreamSparse(), TEXT),
                    highlighter.GetBestFragment(tokenStream, TEXT));
            }
            finally
            {
                indexReader.Dispose();
                directory.Dispose();
            }
        }

        [Test]
        public void TestSparsePhraseWithNoPositions()
        {
            String TEXT = "the fox did not jump";
            Directory directory = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(directory,
               NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            try
            {
                Document document = new Document();

                FieldType customType = new FieldType(TextField.TYPE_STORED);
                customType.StoreTermVectorOffsets = (true);
                customType.StoreTermVectors = (true);
                document.Add(new Field(FIELD, TEXT, customType));
                indexWriter.AddDocument(document);
            }
            finally
            {
                indexWriter.Dispose();
            }
            IndexReader indexReader = DirectoryReader.Open(directory);
            try
            {
                assertEquals(1, indexReader.NumDocs);
                IndexSearcher indexSearcher = NewSearcher(indexReader);
                PhraseQuery phraseQuery = new PhraseQuery();
                phraseQuery.Add(new Term(FIELD, "did"));
                phraseQuery.Add(new Term(FIELD, "jump"));
                phraseQuery.Slop = (1);
                TopDocs hits = indexSearcher.Search(phraseQuery, 1);
                assertEquals(1, hits.TotalHits);
                Highlighter highlighter = new Highlighter(
                         new SimpleHTMLFormatter(), new SimpleHTMLEncoder(),
                         new QueryScorer(phraseQuery));
                TokenStream tokenStream = TokenSources.GetTokenStream(
                   indexReader.GetTermVector(0, FIELD), true);
                assertEquals("the fox <B>did</B> not <B>jump</B>", highlighter
                    .GetBestFragment(tokenStream, TEXT));
            }
            finally
            {
                indexReader.Dispose();
                directory.Dispose();
            }
        }

        [Test]
        public void TestSparseSpan()
        {
            String TEXT = "the fox did not jump";
            Directory directory = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(directory,
               NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            try
            {
                Document document = new Document();
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectorOffsets = (true);
                customType.StoreTermVectorPositions = (true);
                customType.StoreTermVectors = (true);
                document.Add(new Field(FIELD, new TokenStreamSparse(), customType));
                indexWriter.AddDocument(document);
            }
            finally
            {
                indexWriter.Dispose();
            }
            IndexReader indexReader = DirectoryReader.Open(directory);
            try
            {
                assertEquals(1, indexReader.NumDocs);
                IndexSearcher indexSearcher = NewSearcher(indexReader);
                Query phraseQuery = new SpanNearQuery(new SpanQuery[] {
                    new SpanTermQuery(new Term(FIELD, "did")),
                    new SpanTermQuery(new Term(FIELD, "jump")) }, 0, true);

                TopDocs hits = indexSearcher.Search(phraseQuery, 1);
                assertEquals(0, hits.TotalHits);
                Highlighter highlighter = new Highlighter(
                         new SimpleHTMLFormatter(), new SimpleHTMLEncoder(),
                         new QueryScorer(phraseQuery));
                TokenStream tokenStream = TokenSources
                   .GetTokenStream(indexReader.GetTermVector(
                       0, FIELD), false);
                assertEquals(
                    highlighter.GetBestFragment(new TokenStreamSparse(), TEXT),
                    highlighter.GetBestFragment(tokenStream, TEXT));
            }
            finally
            {
                indexReader.Dispose();
                directory.Dispose();
            }
        }

        private sealed class TokenStreamSparse : TokenStream
        {
            private Token[] tokens;

            private int i = -1;

            private readonly ICharTermAttribute termAttribute;
            private readonly IOffsetAttribute offsetAttribute;
            private readonly IPositionIncrementAttribute positionIncrementAttribute;

            public TokenStreamSparse()
            {
                termAttribute = AddAttribute<ICharTermAttribute>();
                offsetAttribute = AddAttribute<IOffsetAttribute>();
                positionIncrementAttribute = AddAttribute<IPositionIncrementAttribute>();
                Reset();
            }


            public override bool IncrementToken()
            {
                this.i++;
                if (this.i >= this.tokens.Length)
                {
                    return false;
                }
                ClearAttributes();
                termAttribute.SetEmpty().Append(this.tokens[i]);
                offsetAttribute.SetOffset(this.tokens[i].StartOffset, this.tokens[i]
                    .EndOffset);
                positionIncrementAttribute.PositionIncrement = (this.tokens[i]
                    .PositionIncrement);
                return true;
            }


            public override void Reset()
            {
                this.i = -1;
                this.tokens = new Token[] {
                    new Token(new char[] { 't', 'h', 'e' }, 0, 3, 0, 3),
                    new Token(new char[] { 'f', 'o', 'x' }, 0, 3, 4, 7),
                    new Token(new char[] { 'd', 'i', 'd' }, 0, 3, 8, 11),
                    new Token(new char[] { 'j', 'u', 'm', 'p' }, 0, 4, 16, 20) };
                this.tokens[3].PositionIncrement = (2);
            }
        }

        private sealed class TokenStreamConcurrent : TokenStream
        {
            private Token[] tokens;

            private int i = -1;

            private readonly ICharTermAttribute termAttribute;
            private readonly IOffsetAttribute offsetAttribute;
            private readonly IPositionIncrementAttribute positionIncrementAttribute;

            public TokenStreamConcurrent()
            {
                termAttribute = AddAttribute<ICharTermAttribute>();
                offsetAttribute = AddAttribute<IOffsetAttribute>();
                positionIncrementAttribute = AddAttribute<IPositionIncrementAttribute>();
                Reset();
            }


            public override bool IncrementToken()
            {
                this.i++;
                if (this.i >= this.tokens.Length)
                {
                    return false;
                }
                ClearAttributes();
                termAttribute.SetEmpty().Append(this.tokens[i]);
                offsetAttribute.SetOffset(this.tokens[i].StartOffset, this.tokens[i]
                    .EndOffset);
                positionIncrementAttribute.PositionIncrement = (this.tokens[i]
                    .PositionIncrement);
                return true;
            }


            public override void Reset()
            {
                this.i = -1;
                this.tokens = new Token[] {
                    new Token(new char[] { 't', 'h', 'e' }, 0, 3, 0, 3),
                    new Token(new char[] { 'f', 'o', 'x' }, 0, 3, 4, 7),
                    new Token(new char[] { 'j', 'u', 'm', 'p' }, 0, 4, 8, 14),
                    new Token(new char[] { 'j', 'u', 'm', 'p', 'e', 'd' }, 0, 6, 8, 14) };
                this.tokens[3].PositionIncrement = (0);
            }
        }
    }
}
