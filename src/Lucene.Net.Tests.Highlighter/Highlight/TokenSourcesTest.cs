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

    // LUCENE-2874
    [SuppressCodecs("Lucene3x")]
    public class TokenSourcesTest : LuceneTestCase
    {
        private static readonly String FIELD = "text";

        private sealed class OverlappingTokenStream : TokenStream
        {
            private Token[] tokens;

            private int i = -1;

            private readonly ICharTermAttribute termAttribute;
            private readonly IOffsetAttribute offsetAttribute;
            private readonly IPositionIncrementAttribute positionIncrementAttribute;

            public OverlappingTokenStream()
            {
                termAttribute = AddAttribute<ICharTermAttribute>();
                offsetAttribute = AddAttribute<IOffsetAttribute>();
                positionIncrementAttribute = AddAttribute<IPositionIncrementAttribute>();
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
                offsetAttribute.SetOffset(this.tokens[i].StartOffset,
                    this.tokens[i].EndOffset);
                positionIncrementAttribute.PositionIncrement = (this.tokens[i]
                    .PositionIncrement);
                return true;
            }


            public override void Reset()
            {
                this.i = -1;
                this.tokens = new Token[] {
                new Token(new char[] {'t', 'h', 'e'}, 0, 3, 0, 3),
                new Token(new char[] {'{', 'f', 'o', 'x', '}'}, 0, 5, 0, 7),
                new Token(new char[] {'f', 'o', 'x'}, 0, 3, 4, 7),
                new Token(new char[] {'d', 'i', 'd'}, 0, 3, 8, 11),
                new Token(new char[] {'n', 'o', 't'}, 0, 3, 12, 15),
                new Token(new char[] {'j', 'u', 'm', 'p'}, 0, 4, 16, 20)};
                this.tokens[1].PositionIncrement = (0);
            }
        }

        [Test]
        public void TestOverlapWithOffset()
        {
            String TEXT = "the fox did not jump";
            Directory directory = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(directory,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            try
            {
                Document document = new Document();
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectors = (true);
                customType.StoreTermVectorOffsets = (true);
                document.Add(new Field(FIELD, new OverlappingTokenStream(), customType));
                indexWriter.AddDocument(document);
            }
            finally
            {
                indexWriter.Dispose();
            }
            IndexReader indexReader = DirectoryReader.Open(directory);
            assertEquals(1, indexReader.NumDocs);
            IndexSearcher indexSearcher = NewSearcher(indexReader);
            try
            {
                DisjunctionMaxQuery query = new DisjunctionMaxQuery(1);
                query.Add(new SpanTermQuery(new Term(FIELD, "{fox}")));
                query.Add(new SpanTermQuery(new Term(FIELD, "fox")));
                // final Query phraseQuery = new SpanNearQuery(new SpanQuery[] {
                // new SpanTermQuery(new Term(FIELD, "{fox}")),
                // new SpanTermQuery(new Term(FIELD, "fox")) }, 0, true);

                TopDocs hits = indexSearcher.Search(query, 1);
                assertEquals(1, hits.TotalHits);
                Highlighter highlighter = new Highlighter(
                         new SimpleHTMLFormatter(), new SimpleHTMLEncoder(),
                         new QueryScorer(query));
                TokenStream tokenStream = TokenSources
                   .GetTokenStream(
                       indexReader.GetTermVector(0, FIELD),
                       false);
                assertEquals("<B>the fox</B> did not jump",
                    highlighter.GetBestFragment(tokenStream, TEXT));
            }
            finally
            {
                indexReader.Dispose();
                directory.Dispose();
            }
        }

        [Test]
        public void TestOverlapWithPositionsAndOffset()
        {
            String TEXT = "the fox did not jump";
            Directory directory = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(directory,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            try
            {
                Document document = new Document();
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectors = (true);
                customType.StoreTermVectorOffsets = (true);
                customType.StoreTermVectorPositions = (true);
                document.Add(new Field(FIELD, new OverlappingTokenStream(), customType));
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
                DisjunctionMaxQuery query = new DisjunctionMaxQuery(1);
                query.Add(new SpanTermQuery(new Term(FIELD, "{fox}")));
                query.Add(new SpanTermQuery(new Term(FIELD, "fox")));
                // final Query phraseQuery = new SpanNearQuery(new SpanQuery[] {
                // new SpanTermQuery(new Term(FIELD, "{fox}")),
                // new SpanTermQuery(new Term(FIELD, "fox")) }, 0, true);

                TopDocs hits = indexSearcher.Search(query, 1);
                assertEquals(1, hits.TotalHits);
                Highlighter highlighter = new Highlighter(
                         new SimpleHTMLFormatter(), new SimpleHTMLEncoder(),
                         new QueryScorer(query));
                TokenStream tokenStream = TokenSources
                   .GetTokenStream(
                       indexReader.GetTermVector(0, FIELD),
                       false);
                assertEquals("<B>the fox</B> did not jump",
                    highlighter.GetBestFragment(tokenStream, TEXT));
            }
            finally
            {
                indexReader.Dispose();
                directory.Dispose();
            }
        }

        [Test]
        public void TestOverlapWithOffsetExactPhrase()
        {
            String TEXT = "the fox did not jump";
            Directory directory = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(directory,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            try
            {
                Document document = new Document();
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectors = (true);
                customType.StoreTermVectorOffsets = (true);
                document.Add(new Field(FIELD, new OverlappingTokenStream(), customType));
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
                // final DisjunctionMaxQuery query = new DisjunctionMaxQuery(1);
                // query.Add(new SpanTermQuery(new Term(FIELD, "{fox}")));
                // query.Add(new SpanTermQuery(new Term(FIELD, "fox")));
                Query phraseQuery = new SpanNearQuery(new SpanQuery[] {
                    new SpanTermQuery(new Term(FIELD, "the")),
                    new SpanTermQuery(new Term(FIELD, "fox"))}, 0, true);

                TopDocs hits = indexSearcher.Search(phraseQuery, 1);
                assertEquals(1, hits.TotalHits);
                Highlighter highlighter = new Highlighter(
                         new SimpleHTMLFormatter(), new SimpleHTMLEncoder(),
                         new QueryScorer(phraseQuery));
                TokenStream tokenStream = TokenSources
                   .GetTokenStream(
                       indexReader.GetTermVector(0, FIELD),
                       false);
                assertEquals("<B>the fox</B> did not jump",
                    highlighter.GetBestFragment(tokenStream, TEXT));
            }
            finally
            {
                indexReader.Dispose();
                directory.Dispose();
            }
        }

        [Test]
        public void TestOverlapWithPositionsAndOffsetExactPhrase()
        {
            String TEXT = "the fox did not jump";
            Directory directory = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(directory,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            try
            {
                Document document = new Document();
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectors = (true);
                customType.StoreTermVectorOffsets = (true);
                document.Add(new Field(FIELD, new OverlappingTokenStream(), customType));
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
                // final DisjunctionMaxQuery query = new DisjunctionMaxQuery(1);
                // query.Add(new SpanTermQuery(new Term(FIELD, "the")));
                // query.Add(new SpanTermQuery(new Term(FIELD, "fox")));
                Query phraseQuery = new SpanNearQuery(new SpanQuery[] {
                    new SpanTermQuery(new Term(FIELD, "the")),
                    new SpanTermQuery(new Term(FIELD, "fox"))}, 0, true);

                TopDocs hits = indexSearcher.Search(phraseQuery, 1);
                assertEquals(1, hits.TotalHits);
                Highlighter highlighter = new Highlighter(
                         new SimpleHTMLFormatter(), new SimpleHTMLEncoder(),
                         new QueryScorer(phraseQuery));
                TokenStream tokenStream = TokenSources
                   .GetTokenStream(
                       indexReader.GetTermVector(0, FIELD),
                       false);
                assertEquals("<B>the fox</B> did not jump",
                    highlighter.GetBestFragment(tokenStream, TEXT));
            }
            finally
            {
                indexReader.Dispose();
                directory.Dispose();
            }
        }

        [Test]
        public void TestTermVectorWithoutOffsetsThrowsException()
        {
            Directory directory = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(directory,
               NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            try
            {
                Document document = new Document();
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectors = (true);
                customType.StoreTermVectorOffsets = (false);
                customType.StoreTermVectorPositions = (true);
                document.Add(new Field(FIELD, new OverlappingTokenStream(), customType));
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
                TokenSources.GetTokenStream(
                        indexReader.GetTermVector(0, FIELD),
                        false);
                fail("TokenSources.getTokenStream should throw IllegalArgumentException if term vector has no offsets");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            finally
            {
                indexReader.Dispose();
                directory.Dispose();
            }
        }

        int curOffset;

        /** Just make a token with the text, and set the payload
         *  to the text as well.  Offets increment "naturally". */
        private Token getToken(String text)
        {
            Token t = new Token(text, curOffset, curOffset + text.Length);
            t.Payload = (new BytesRef(text));
            curOffset++;
            return t;
        }

        // LUCENE-5294
        [Test]
        public void TestPayloads()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            FieldType myFieldType = new FieldType(TextField.TYPE_NOT_STORED);
            myFieldType.StoreTermVectors = (true);
            myFieldType.StoreTermVectorOffsets = (true);
            myFieldType.StoreTermVectorPositions = (true);
            myFieldType.StoreTermVectorPayloads = (true);

            curOffset = 0;

            Token[] tokens = new Token[] {
                getToken("foxes"),
                getToken("can"),
                getToken("jump"),
                getToken("high")
            };

            Document doc = new Document();
            doc.Add(new Field("field", new CannedTokenStream(tokens), myFieldType));
            writer.AddDocument(doc);

            IndexReader reader = writer.GetReader();
            writer.Dispose();
            assertEquals(1, reader.NumDocs);

            for (int i = 0; i < 2; i++)
            {
                // Do this twice, once passing true and then passing
                // false: they are entirely different code paths
                // under-the-hood:
                TokenStream ts = TokenSources.GetTokenStream(reader.GetTermVectors(0).GetTerms("field"), i == 0);

                ICharTermAttribute termAtt = ts.GetAttribute<ICharTermAttribute>();
                IPositionIncrementAttribute posIncAtt = ts.GetAttribute<IPositionIncrementAttribute>();
                IOffsetAttribute offsetAtt = ts.GetAttribute<IOffsetAttribute>();
                IPayloadAttribute payloadAtt = ts.GetAttribute<IPayloadAttribute>();

                foreach (Token token in tokens)
                {
                    assertTrue(ts.IncrementToken());
                    assertEquals(token.toString(), termAtt.toString());
                    assertEquals(token.PositionIncrement, posIncAtt.PositionIncrement);
                    assertEquals(token.Payload, payloadAtt.Payload);
                    assertEquals(token.StartOffset, offsetAtt.StartOffset);
                    assertEquals(token.EndOffset, offsetAtt.EndOffset);
                }

                assertFalse(ts.IncrementToken());
            }

            reader.Dispose();
            dir.Dispose();
        }
    }
}
