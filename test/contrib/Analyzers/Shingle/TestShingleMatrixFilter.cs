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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Payloads;
using Lucene.Net.Analysis.Shingle;
using Lucene.Net.Analysis.Shingle.Codec;
using Lucene.Net.Analysis.Shingle.Matrix;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analyzers.Miscellaneous;
using Lucene.Net.Analyzers.Payloads;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Shingle
{
    public class TestShingleMatrixFilter : BaseTokenStreamTestCase
    {
        public TestShingleMatrixFilter() : this(typeof (TestShingleMatrixFilter).Name)
        {
        }

        // use this ctor, because SingleTokenTokenStream only uses next(Token), so exclude it
        public TestShingleMatrixFilter(String name) :
            base(name)
        {
        }

        [Test]
        public void TestIterator()
        {
            var wst = new WhitespaceTokenizer(new StringReader("one two three four five"));
            var smf = new ShingleMatrixFilter(wst, 2, 2, '_', false,
                                              new OneDimensionalNonWeightedTokenSettingsCodec());

            int i;
            for (i = 0; smf.IncrementToken(); i++) { }

            Assert.AreEqual(4, i);

            // call next once more. this should return false again rather than throwing an exception (LUCENE-1939)
            Assert.IsFalse(smf.IncrementToken());

            //System.DateTime.Now;
        }

        [Test]
        public void TestBehavingAsShingleFilter()
        {
            ShingleMatrixFilter.DefaultSettingsCodec = null;

            TokenStream ts = new ShingleMatrixFilter(new EmptyTokenStream(), 1, 2, ' ', false,
                                                     new OneDimensionalNonWeightedTokenSettingsCodec
                                                         ());
            Assert.IsFalse(ts.IncrementToken());

            // test a plain old token stream with synonyms translated to rows.

            var tokens = new LinkedList<Token>();
            tokens.AddLast(CreateToken("please", 0, 6));
            tokens.AddLast(CreateToken("divide", 7, 13));
            tokens.AddLast(CreateToken("this", 14, 18));
            tokens.AddLast(CreateToken("sentence", 19, 27));
            tokens.AddLast(CreateToken("into", 28, 32));
            tokens.AddLast(CreateToken("shingles", 33, 39));

            var tls = new TokenListStream(tokens);

            // bi-grams

            ts = new ShingleMatrixFilter(tls, 1, 2, ' ', false, new OneDimensionalNonWeightedTokenSettingsCodec());

            //for (Token token = ts.Next(new Token()); token != null; token = ts.Next(token))
            //{
            //    Console.Out.WriteLine("AssertNext(ts, \"" + token.Term() + "\", " + token.GetPositionIncrement() + ", " + (token.GetPayload() == null ? "1.0" : PayloadHelper.decodeFloat(token.GetPayload().GetData()).ToString()) + "f, " + token.StartOffset() + ", " + token.EndOffset() + ");");
            //    token.Clear();
            //}

            AssertTokenStreamContents(ts,
                                      new[]
                                          {
                                              "please", "please divide", "divide", "divide this",
                                              "this", "this sentence", "sentence", "sentence into", "into",
                                              "into shingles", "shingles"
                                          },
                                      new[] {0, 0, 7, 7, 14, 14, 19, 19, 28, 28, 33},
                                      new[] {6, 13, 13, 18, 18, 27, 27, 32, 32, 39, 39});
        }


        /// <summary>
        /// Extracts a matrix from a token stream.
        /// </summary>
        [Test]
        public void TestTokenStream()
        {
            ShingleMatrixFilter.DefaultSettingsCodec = null;
            //new ShingleMatrixFilter.SimpleThreeDimensionalTokenSettingsCodec();

            // test a plain old token stream with synonyms tranlated to rows.

            var tokens = new LinkedList<Token>();
            tokens.AddLast(TokenFactory("hello", 1, 0, 4));
            tokens.AddLast(TokenFactory("greetings", 0, 0, 4));
            tokens.AddLast(TokenFactory("world", 1, 5, 10));
            tokens.AddLast(TokenFactory("earth", 0, 5, 10));
            tokens.AddLast(TokenFactory("tellus", 0, 5, 10));

            TokenStream tls = new TokenListStream(tokens);

            // bi-grams

            TokenStream ts = new ShingleMatrixFilter(tls, 2, 2, '_', false,
                                                     new TwoDimensionalNonWeightedSynonymTokenSettingsCodec());

            AssertNext(ts, "hello_world");
            AssertNext(ts, "greetings_world");
            AssertNext(ts, "hello_earth");
            AssertNext(ts, "greetings_earth");
            AssertNext(ts, "hello_tellus");
            AssertNext(ts, "greetings_tellus");
            Assert.IsFalse(ts.IncrementToken());

            // bi-grams with no spacer character, start offset, end offset

            tls.Reset();
            ts = new ShingleMatrixFilter(tls, 2, 2, null, false,
                                         new TwoDimensionalNonWeightedSynonymTokenSettingsCodec());
            AssertNext(ts, "helloworld", 0, 10);
            AssertNext(ts, "greetingsworld", 0, 10);
            AssertNext(ts, "helloearth", 0, 10);
            AssertNext(ts, "greetingsearth", 0, 10);
            AssertNext(ts, "hellotellus", 0, 10);
            AssertNext(ts, "greetingstellus", 0, 10);
            Assert.IsFalse(ts.IncrementToken());


            // add ^_prefix_and_suffix_$
            //
            // using 3d codec as it supports weights

            ShingleMatrixFilter.DefaultSettingsCodec =
                new SimpleThreeDimensionalTokenSettingsCodec();

            tokens = new LinkedList<Token>();
            tokens.AddLast(TokenFactory("hello", 1, 1f, 0, 4, TokenPositioner.NewColumn));
            tokens.AddLast(TokenFactory("greetings", 0, 1f, 0, 4, TokenPositioner.NewRow));
            tokens.AddLast(TokenFactory("world", 1, 1f, 5, 10, TokenPositioner.NewColumn));
            tokens.AddLast(TokenFactory("earth", 0, 1f, 5, 10, TokenPositioner.NewRow));
            tokens.AddLast(TokenFactory("tellus", 0, 1f, 5, 10, TokenPositioner.NewRow));

            tls = new TokenListStream(tokens);

            // bi-grams, position incrememnt, weight, start offset, end offset

            ts = new PrefixAndSuffixAwareTokenFilter(
                new SingleTokenTokenStream(TokenFactory("^", 1, 100f, 0, 0)),
                tls,
                new SingleTokenTokenStream(TokenFactory("$", 1, 50f, 0, 0))
                );
            tls = new CachingTokenFilter(ts);

            ts = new ShingleMatrixFilter(tls, 2, 2, '_', false);

            //for (Token token = ts.Next(new Token()); token != null; token = ts.Next(token)) {
            //    Console.Out.WriteLine("AssertNext(ts, \"" + token.Term() + "\", " + token.GetPositionIncrement() + ", " + (token.GetPayload() == null ? "1.0" : PayloadHelper.decodeFloat(token.GetPayload().GetData()).ToString()) + "f, " + token.StartOffset() + ", " + token.EndOffset() + ");");
            //    token.Clear();
            //}

            AssertNext(ts, "^_hello", 1, 10.049875f, 0, 4);
            AssertNext(ts, "^_greetings", 1, 10.049875f, 0, 4);
            AssertNext(ts, "hello_world", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "greetings_world", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "hello_earth", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "greetings_earth", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "hello_tellus", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "greetings_tellus", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "world_$", 1, 7.1414285f, 5, 10);
            AssertNext(ts, "earth_$", 1, 7.1414285f, 5, 10);
            AssertNext(ts, "tellus_$", 1, 7.1414285f, 5, 10);
            Assert.IsFalse(ts.IncrementToken());

            // test unlimited size and allow single boundary token as shingle
            tls.Reset();

            ts = new ShingleMatrixFilter(tls, 1, Int32.MaxValue, '_', false);


            //for (Token token = ts.Next(new Token()); token != null; token = ts.Next(token))
            //{
            //    Console.Out.WriteLine("AssertNext(ts, \"" + token.Term() + "\", " + token.GetPositionIncrement() + ", " + (token.GetPayload() == null ? "1.0" : PayloadHelper.decodeFloat(token.GetPayload().GetData()).ToString()) + "f, " + token.StartOffset() + ", " + token.EndOffset() + ");");
            //    token.Clear();
            //}

            AssertNext(ts, "^", 1, 10.0f, 0, 0);
            AssertNext(ts, "^_hello", 1, 10.049875f, 0, 4);
            AssertNext(ts, "^_hello_world", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_hello_world_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "hello", 1, 1.0f, 0, 4);
            AssertNext(ts, "hello_world", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "hello_world_$", 1, 7.2111025f, 0, 10);
            AssertNext(ts, "world", 1, 1.0f, 5, 10);
            AssertNext(ts, "world_$", 1, 7.1414285f, 5, 10);
            AssertNext(ts, "$", 1, 7.071068f, 10, 10);
            AssertNext(ts, "^_greetings", 1, 10.049875f, 0, 4);
            AssertNext(ts, "^_greetings_world", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_greetings_world_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "greetings", 1, 1.0f, 0, 4);
            AssertNext(ts, "greetings_world", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "greetings_world_$", 1, 7.2111025f, 0, 10);
            AssertNext(ts, "^_hello_earth", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_hello_earth_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "hello_earth", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "hello_earth_$", 1, 7.2111025f, 0, 10);
            AssertNext(ts, "earth", 1, 1.0f, 5, 10);
            AssertNext(ts, "earth_$", 1, 7.1414285f, 5, 10);
            AssertNext(ts, "^_greetings_earth", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_greetings_earth_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "greetings_earth", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "greetings_earth_$", 1, 7.2111025f, 0, 10);
            AssertNext(ts, "^_hello_tellus", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_hello_tellus_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "hello_tellus", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "hello_tellus_$", 1, 7.2111025f, 0, 10);
            AssertNext(ts, "tellus", 1, 1.0f, 5, 10);
            AssertNext(ts, "tellus_$", 1, 7.1414285f, 5, 10);
            AssertNext(ts, "^_greetings_tellus", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_greetings_tellus_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "greetings_tellus", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "greetings_tellus_$", 1, 7.2111025f, 0, 10);

            Assert.IsFalse(ts.IncrementToken());

            // test unlimited size but don't allow single boundary token as shingle

            tls.Reset();
            ts = new ShingleMatrixFilter(tls, 1, Int32.MaxValue, '_', true);

            //  for (Token token = ts.next(new Token()); token != null; token = ts.next(token)) {
            //      System.out.println("assertNext(ts, \"" + token.term() + "\", " + token.getPositionIncrement() + ", " + (token.getPayload() == null ? "1.0" : PayloadHelper.decodeFloat(token.getPayload().getData())) + "f, " + token.startOffset() + ", " + token.endOffset() + ");");
            //      token.clear();
            //    }

            AssertNext(ts, "^_hello", 1, 10.049875f, 0, 4);
            AssertNext(ts, "^_hello_world", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_hello_world_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "hello", 1, 1.0f, 0, 4);
            AssertNext(ts, "hello_world", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "hello_world_$", 1, 7.2111025f, 0, 10);
            AssertNext(ts, "world", 1, 1.0f, 5, 10);
            AssertNext(ts, "world_$", 1, 7.1414285f, 5, 10);
            AssertNext(ts, "^_greetings", 1, 10.049875f, 0, 4);
            AssertNext(ts, "^_greetings_world", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_greetings_world_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "greetings", 1, 1.0f, 0, 4);
            AssertNext(ts, "greetings_world", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "greetings_world_$", 1, 7.2111025f, 0, 10);
            AssertNext(ts, "^_hello_earth", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_hello_earth_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "hello_earth", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "hello_earth_$", 1, 7.2111025f, 0, 10);
            AssertNext(ts, "earth", 1, 1.0f, 5, 10);
            AssertNext(ts, "earth_$", 1, 7.1414285f, 5, 10);
            AssertNext(ts, "^_greetings_earth", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_greetings_earth_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "greetings_earth", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "greetings_earth_$", 1, 7.2111025f, 0, 10);
            AssertNext(ts, "^_hello_tellus", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_hello_tellus_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "hello_tellus", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "hello_tellus_$", 1, 7.2111025f, 0, 10);
            AssertNext(ts, "tellus", 1, 1.0f, 5, 10);
            AssertNext(ts, "tellus_$", 1, 7.1414285f, 5, 10);
            AssertNext(ts, "^_greetings_tellus", 1, 10.099504f, 0, 10);
            AssertNext(ts, "^_greetings_tellus_$", 1, 12.328828f, 0, 10);
            AssertNext(ts, "greetings_tellus", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "greetings_tellus_$", 1, 7.2111025f, 0, 10);


            Assert.IsFalse(ts.IncrementToken());

            //System.currentTimeMillis();

            // multi-token synonyms
            //
            // Token[][][] {
            //    {{hello}, {greetings, and, salutations},
            //    {{world}, {earth}, {tellus}}
            // }
            //


            tokens = new LinkedList<Token>();
            tokens.AddLast(TokenFactory("hello", 1, 1f, 0, 4, TokenPositioner.NewColumn));
            tokens.AddLast(TokenFactory("greetings", 1, 1f, 0, 4, TokenPositioner.NewRow));
            tokens.AddLast(TokenFactory("and", 1, 1f, 0, 4, TokenPositioner.SameRow));
            tokens.AddLast(TokenFactory("salutations", 1, 1f, 0, 4, TokenPositioner.SameRow));
            tokens.AddLast(TokenFactory("world", 1, 1f, 5, 10, TokenPositioner.NewColumn));
            tokens.AddLast(TokenFactory("earth", 1, 1f, 5, 10, TokenPositioner.NewRow));
            tokens.AddLast(TokenFactory("tellus", 1, 1f, 5, 10, TokenPositioner.NewRow));

            tls = new TokenListStream(tokens);

            // 2-3 grams

            ts = new ShingleMatrixFilter(tls, 2, 3, '_', false);

            //  for (Token token = ts.next(new Token()); token != null; token = ts.next(token)) {
            //      System.out.println("assertNext(ts, \"" + token.term() + "\", " + token.getPositionIncrement() + ", " + (token.getPayload() == null ? "1.0" : PayloadHelper.decodeFloat(token.getPayload().getData())) + "f, " + token.startOffset() + ", " + token.endOffset() + ");");
            //      token.clear();
            //    }

            // shingle, position increment, weight, start offset, end offset

            AssertNext(ts, "hello_world", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "greetings_and", 1, 1.4142135f, 0, 4);
            AssertNext(ts, "greetings_and_salutations", 1, 1.7320508f, 0, 4);
            AssertNext(ts, "and_salutations", 1, 1.4142135f, 0, 4);
            AssertNext(ts, "and_salutations_world", 1, 1.7320508f, 0, 10);
            AssertNext(ts, "salutations_world", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "hello_earth", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "and_salutations_earth", 1, 1.7320508f, 0, 10);
            AssertNext(ts, "salutations_earth", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "hello_tellus", 1, 1.4142135f, 0, 10);
            AssertNext(ts, "and_salutations_tellus", 1, 1.7320508f, 0, 10);
            AssertNext(ts, "salutations_tellus", 1, 1.4142135f, 0, 10);

            Assert.IsFalse(ts.IncrementToken());

            //System.currentTimeMillis();
        }

        /// <summary>
        /// Tests creat shingles from a pre-assembled matrix
        /// 
        /// Tests the row token z-axis, multi token synonyms. 
        /// </summary>
        [Test]
        public void TestMatrix()
        {
            // some other tests set this to null.
            // set it here in case tests are run out of the usual order.
            ShingleMatrixFilter.DefaultSettingsCodec = new SimpleThreeDimensionalTokenSettingsCodec();

            var matrix = new Matrix();

            new Column(TokenFactory("no", 1), matrix);
            new Column(TokenFactory("surprise", 1), matrix);
            new Column(TokenFactory("to", 1), matrix);
            new Column(TokenFactory("see", 1), matrix);
            new Column(TokenFactory("england", 1), matrix);
            new Column(TokenFactory("manager", 1), matrix);

            var col = new Column(matrix);

            // sven göran eriksson is a multi token synonym to svennis
            new Row(col).Tokens.AddLast(TokenFactory("svennis", 1));

            var row = new Row(col);
            row.Tokens.AddLast(TokenFactory("sven", 1));
            row.Tokens.AddLast(TokenFactory("göran", 1));
            row.Tokens.AddLast(TokenFactory("eriksson", 1));

            new Column(TokenFactory("in", 1), matrix);
            new Column(TokenFactory("the", 1), matrix);
            new Column(TokenFactory("croud", 1), matrix);

            TokenStream ts = new ShingleMatrixFilter(matrix, 2, 4, '_', true,
                                                     new SimpleThreeDimensionalTokenSettingsCodec());

            //  for (Token token = ts.next(new Token()); token != null; token = ts.next(token)) {
            //      System.out.println("assertNext(ts, \"" + token.term() + "\", " + token.getPositionIncrement() + ", " + (token.getPayload() == null ? "1.0" : PayloadHelper.decodeFloat(token.getPayload().getData())) + "f, " + token.startOffset() + ", " + token.endOffset() + ");");
            //      token.clear();
            //    }

            AssertNext(ts, "no_surprise", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "no_surprise_to", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "no_surprise_to_see", 1, 2.0f, 0, 0);
            AssertNext(ts, "surprise_to", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "surprise_to_see", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "surprise_to_see_england", 1, 2.0f, 0, 0);
            AssertNext(ts, "to_see", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "to_see_england", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "to_see_england_manager", 1, 2.0f, 0, 0);
            AssertNext(ts, "see_england", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "see_england_manager", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "see_england_manager_svennis", 1, 2.0f, 0, 0);
            AssertNext(ts, "england_manager", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "england_manager_svennis", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "england_manager_svennis_in", 1, 2.0f, 0, 0);
            AssertNext(ts, "manager_svennis", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "manager_svennis_in", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "manager_svennis_in_the", 1, 2.0f, 0, 0);
            AssertNext(ts, "svennis_in", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "svennis_in_the", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "svennis_in_the_croud", 1, 2.0f, 0, 0);
            AssertNext(ts, "in_the", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "in_the_croud", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "the_croud", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "see_england_manager_sven", 1, 2.0f, 0, 0);
            AssertNext(ts, "england_manager_sven", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "england_manager_sven_göran", 1, 2.0f, 0, 0);
            AssertNext(ts, "manager_sven", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "manager_sven_göran", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "manager_sven_göran_eriksson", 1, 2.0f, 0, 0);
            AssertNext(ts, "sven_göran", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "sven_göran_eriksson", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "sven_göran_eriksson_in", 1, 2.0f, 0, 0);
            AssertNext(ts, "göran_eriksson", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "göran_eriksson_in", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "göran_eriksson_in_the", 1, 2.0f, 0, 0);
            AssertNext(ts, "eriksson_in", 1, 1.4142135f, 0, 0);
            AssertNext(ts, "eriksson_in_the", 1, 1.7320508f, 0, 0);
            AssertNext(ts, "eriksson_in_the_croud", 1, 2.0f, 0, 0);

            Assert.IsFalse(ts.IncrementToken());
        }

        private Token TokenFactory(String text, int startOffset, int endOffset)
        {
            return TokenFactory(text, 1, 1f, startOffset, endOffset);
        }

        private Token TokenFactory(String text, int posIncr, int startOffset, int endOffset)
        {
            Token token = new Token(startOffset, endOffset);
            token.SetTermBuffer(text);
            token.PositionIncrement = posIncr;
            return token;
        }

        private Token TokenFactory(String text, int posIncr)
        {
            return TokenFactory(text, posIncr, 1f, 0, 0);
        }

        private Token TokenFactory(String text, int posIncr, float weight)
        {
            return TokenFactory(text, posIncr, weight, 0, 0);
        }

        private Token TokenFactory(String text, int posIncr, float weight, int startOffset, int endOffset)
        {
            Token token = new Token(startOffset, endOffset);
            token.SetTermBuffer(text);
            token.PositionIncrement = posIncr;
            ShingleMatrixFilter.DefaultSettingsCodec.SetWeight(token, weight);
            return token;
        }

        private Token TokenFactory(String text, int posIncr, float weight, int startOffset, int endOffset, TokenPositioner positioner)
        {
            Token token = new Token(startOffset, endOffset);
            token.SetTermBuffer(text);
            token.PositionIncrement = posIncr;
            ShingleMatrixFilter.DefaultSettingsCodec.SetWeight(token, weight);
            ShingleMatrixFilter.DefaultSettingsCodec.SetTokenPositioner(token, positioner);
            return token;
        }

        // assert-methods start here

        private static void AssertNext(TokenStream ts, String text)
        {
            var termAtt = ts.AddAttribute<ITermAttribute>();

            Assert.IsTrue(ts.IncrementToken());
            Assert.AreEqual(text, termAtt.Term);
        }

        private static void AssertNext(TokenStream ts, String text, int positionIncrement, float boost, int startOffset,
                                       int endOffset)
        {
            var termAtt = ts.AddAttribute<ITermAttribute>();
            var posIncrAtt = ts.AddAttribute<IPositionIncrementAttribute>();
            var payloadAtt = ts.AddAttribute<IPayloadAttribute>();
            var offsetAtt = ts.AddAttribute<IOffsetAttribute>();

            Assert.IsTrue(ts.IncrementToken());
            Assert.AreEqual(text, termAtt.Term);
            Assert.AreEqual(positionIncrement, posIncrAtt.PositionIncrement);
            Assert.AreEqual(boost,
                            payloadAtt.Payload == null
                                ? 1f
                                : PayloadHelper.DecodeFloat(payloadAtt.Payload.GetData()), 0);
            Assert.AreEqual(startOffset, offsetAtt.StartOffset);
            Assert.AreEqual(endOffset, offsetAtt.EndOffset);
        }

        private static void AssertNext(TokenStream ts, String text, int startOffset, int endOffset)
        {
            var termAtt = ts.AddAttribute<ITermAttribute>();
            var offsetAtt = ts.AddAttribute<IOffsetAttribute>();

            Assert.IsTrue(ts.IncrementToken());
            Assert.AreEqual(text, termAtt.Term);
            Assert.AreEqual(startOffset, offsetAtt.StartOffset);
            Assert.AreEqual(endOffset, offsetAtt.EndOffset);
        }

        private static Token CreateToken(String term, int start, int offset)
        {
            var token = new Token(start, offset);
            token.SetTermBuffer(term);
            return token;
        }

        #region Nested type: TokenListStream

        public sealed class TokenListStream : TokenStream
        {
            private readonly IFlagsAttribute _flagsAtt;
            private readonly IOffsetAttribute _offsetAtt;
            private readonly IPayloadAttribute _payloadAtt;
            private readonly IPositionIncrementAttribute _posIncrAtt;
            private readonly ITermAttribute _termAtt;
            private readonly ICollection<Token> _tokens;
            private readonly ITypeAttribute _typeAtt;

            private IEnumerator<Token> _iterator;

            public TokenListStream(ICollection<Token> tokens)
            {
                _tokens = tokens;
                _termAtt = AddAttribute<ITermAttribute>();
                _posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                _payloadAtt = AddAttribute<IPayloadAttribute>();
                _offsetAtt = AddAttribute<IOffsetAttribute>();
                _typeAtt = AddAttribute<ITypeAttribute>();
                _flagsAtt = AddAttribute<IFlagsAttribute>();
            }

            public override bool IncrementToken()
            {
                if (_iterator == null)
                    _iterator = _tokens.GetEnumerator();

                if (!_iterator.MoveNext())
                    return false;

                Token prototype = _iterator.Current;

                ClearAttributes();

                _termAtt.SetTermBuffer(prototype.TermBuffer(), 0, prototype.TermLength());
                _posIncrAtt.PositionIncrement = prototype.PositionIncrement;
                _flagsAtt.Flags = prototype.Flags;
                _offsetAtt.SetOffset(prototype.StartOffset, prototype.EndOffset);
                _typeAtt.Type = prototype.Type;
                _payloadAtt.Payload = prototype.Payload;

                return true;
            }


            public override void Reset()
            {
                _iterator = null;
            }

            protected override void Dispose(bool disposing)
            {
                // do nothing
            }
        }

        #endregion
    }
}