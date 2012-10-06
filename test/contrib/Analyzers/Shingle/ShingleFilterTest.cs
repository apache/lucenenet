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
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Shingle;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Shingle
{
    public class ShingleFilterTests : BaseTokenStreamTestCase
    {
        public static readonly Token[] TestToken = new[]
                                                       {
                                                           CreateToken("please", 0, 6),
                                                           CreateToken("divide", 7, 13),
                                                           CreateToken("this", 14, 18),
                                                           CreateToken("sentence", 19, 27),
                                                           CreateToken("into", 28, 32),
                                                           CreateToken("shingles", 33, 39),
                                                       };

        public static Token[] TestTokenWithHoles;

        public static readonly Token[] BiGramTokens = new[]
                                                          {
                                                              CreateToken("please", 0, 6),
                                                              CreateToken("please divide", 0, 13),
                                                              CreateToken("divide", 7, 13),
                                                              CreateToken("divide this", 7, 18),
                                                              CreateToken("this", 14, 18),
                                                              CreateToken("this sentence", 14, 27),
                                                              CreateToken("sentence", 19, 27),
                                                              CreateToken("sentence into", 19, 32),
                                                              CreateToken("into", 28, 32),
                                                              CreateToken("into shingles", 28, 39),
                                                              CreateToken("shingles", 33, 39),
                                                          };

        public static readonly int[] BiGramPositionIncrements = new[]
                                                                    {
                                                                        1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1
                                                                    };

        public static readonly String[] BiGramTypes = new[]
                                                          {
                                                              "word", "shingle", "word", "shingle", "word", "shingle",
                                                              "word",
                                                              "shingle", "word", "shingle", "word"
                                                          };

        public static readonly Token[] BiGramTokensWithHoles = new[]
                                                                   {
                                                                       CreateToken("please", 0, 6),
                                                                       CreateToken("please divide", 0, 13),
                                                                       CreateToken("divide", 7, 13),
                                                                       CreateToken("divide _", 7, 19),
                                                                       CreateToken("_", 19, 19),
                                                                       CreateToken("_ sentence", 19, 27),
                                                                       CreateToken("sentence", 19, 27),
                                                                       CreateToken("sentence _", 19, 33),
                                                                       CreateToken("_", 33, 33),
                                                                       CreateToken("_ shingles", 33, 39),
                                                                       CreateToken("shingles", 33, 39),
                                                                   };

        public static readonly int[] BiGramPositionIncrementsWithHoles = new[]
                                                                             {
                                                                                 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1
                                                                             };

        public static readonly Token[] BiGramTokensWithoutUnigrams = new[]
                                                                         {
                                                                             CreateToken("please divide", 0, 13),
                                                                             CreateToken("divide this", 7, 18),
                                                                             CreateToken("this sentence", 14, 27),
                                                                             CreateToken("sentence into", 19, 32),
                                                                             CreateToken("into shingles", 28, 39),
                                                                         };

        public static readonly int[] BiGramPositionIncrementsWithoutUnigrams = new[]
                                                                                   {
                                                                                       1, 1, 1, 1, 1
                                                                                   };

        public static readonly String[] BiGramTypesWithoutUnigrams = new[]
                                                                         {
                                                                             "shingle", "shingle", "shingle",
                                                                             "shingle", "shingle"
                                                                         };

        public static readonly Token[] BiGramTokensWithHolesWithoutUnigrams = new[]
                                                                                  {
                                                                                      CreateToken(
                                                                                          "please divide", 0, 13),
                                                                                      CreateToken("divide _", 7,
                                                                                                  19),
                                                                                      CreateToken("_ sentence", 19,
                                                                                                  27),
                                                                                      CreateToken("sentence _", 19,
                                                                                                  33),
                                                                                      CreateToken("_ shingles", 33,
                                                                                                  39),
                                                                                  };

        public static readonly int[] BiGramPositionIncrementsWithHolesWithoutUnigrams = new[]
                                                                                            {
                                                                                                1, 1, 1, 1, 1, 1
                                                                                            };


        public static readonly Token[] TestSingleToken = new[] { CreateToken("please", 0, 6) };

        public static readonly Token[] SingleToken = new[] { CreateToken("please", 0, 6) };

        public static readonly int[] SingleTokenIncrements = new[] { 1 };

        public static readonly String[] SingleTokenTypes = new[] { "word" };

        public static readonly Token[] EmptyTokenArray = new Token[] { };

        public static readonly int[] EmptyTokenIncrementsArray = new int[] { };

        public static readonly String[] EmptyTokenTypesArray = new String[] { };

        public static readonly Token[] TriGramTokens = new[]
                                                           {
                                                               CreateToken("please", 0, 6),
                                                               CreateToken("please divide", 0, 13),
                                                               CreateToken("please divide this", 0, 18),
                                                               CreateToken("divide", 7, 13),
                                                               CreateToken("divide this", 7, 18),
                                                               CreateToken("divide this sentence", 7, 27),
                                                               CreateToken("this", 14, 18),
                                                               CreateToken("this sentence", 14, 27),
                                                               CreateToken("this sentence into", 14, 32),
                                                               CreateToken("sentence", 19, 27),
                                                               CreateToken("sentence into", 19, 32),
                                                               CreateToken("sentence into shingles", 19, 39),
                                                               CreateToken("into", 28, 32),
                                                               CreateToken("into shingles", 28, 39),
                                                               CreateToken("shingles", 33, 39)
                                                           };

        public static readonly int[] TriGramPositionIncrements = new[]
                                                                     {
                                                                         1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1
                                                                     };

        public static readonly String[] TriGramTypes = new[]
                                                           {
                                                               "word", "shingle", "shingle",
                                                               "word", "shingle", "shingle",
                                                               "word", "shingle", "shingle",
                                                               "word", "shingle", "shingle",
                                                               "word", "shingle",
                                                               "word"
                                                           };

        public static readonly Token[] TriGramTokensWithoutUnigrams = new[]
                                                                          {
                                                                              CreateToken("please divide", 0, 13),
                                                                              CreateToken("please divide this", 0,
                                                                                          18),
                                                                              CreateToken("divide this", 7, 18),
                                                                              CreateToken("divide this sentence", 7,
                                                                                          27),
                                                                              CreateToken("this sentence", 14, 27),
                                                                              CreateToken("this sentence into", 14,
                                                                                          32),
                                                                              CreateToken("sentence into", 19, 32),
                                                                              CreateToken("sentence into shingles",
                                                                                          19, 39),
                                                                              CreateToken("into shingles", 28, 39),
                                                                          };

        public static readonly int[] TriGramPositionIncrementsWithoutUnigrams = new[]
                                                                                    {
                                                                                        1, 0, 1, 0, 1, 0, 1, 0, 1
                                                                                    };

        public static readonly String[] TriGramTypesWithoutUnigrams = new[]
                                                                          {
                                                                              "shingle", "shingle",
                                                                              "shingle", "shingle",
                                                                              "shingle", "shingle",
                                                                              "shingle", "shingle",
                                                                              "shingle",
                                                                          };

        public static readonly Token[] FourGramTokens = new[]
                                                            {
                                                                CreateToken("please", 0, 6),
                                                                CreateToken("please divide", 0, 13),
                                                                CreateToken("please divide this", 0, 18),
                                                                CreateToken("please divide this sentence", 0, 27),
                                                                CreateToken("divide", 7, 13),
                                                                CreateToken("divide this", 7, 18),
                                                                CreateToken("divide this sentence", 7, 27),
                                                                CreateToken("divide this sentence into", 7, 32),
                                                                CreateToken("this", 14, 18),
                                                                CreateToken("this sentence", 14, 27),
                                                                CreateToken("this sentence into", 14, 32),
                                                                CreateToken("this sentence into shingles", 14, 39),
                                                                CreateToken("sentence", 19, 27),
                                                                CreateToken("sentence into", 19, 32),
                                                                CreateToken("sentence into shingles", 19, 39),
                                                                CreateToken("into", 28, 32),
                                                                CreateToken("into shingles", 28, 39),
                                                                CreateToken("shingles", 33, 39)
                                                            };

        public static readonly int[] FourGramPositionIncrements = new[]
                                                                      {
                                                                          1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0
                                                                          , 1, 0, 1
                                                                      };

        public static readonly String[] FourGramTypes = new[]
                                                            {
                                                                "word", "shingle", "shingle", "shingle",
                                                                "word", "shingle", "shingle", "shingle",
                                                                "word", "shingle", "shingle", "shingle",
                                                                "word", "shingle", "shingle",
                                                                "word", "shingle",
                                                                "word"
                                                            };

        public static readonly Token[] FourGramTokensWithoutUnigrams = new[]
                                                                           {
                                                                               CreateToken("please divide", 0, 13),
                                                                               CreateToken("please divide this", 0,
                                                                                           18),
                                                                               CreateToken(
                                                                                   "please divide this sentence", 0,
                                                                                   27),
                                                                               CreateToken("divide this", 7, 18),
                                                                               CreateToken("divide this sentence", 7,
                                                                                           27),
                                                                               CreateToken(
                                                                                   "divide this sentence into", 7,
                                                                                   32),
                                                                               CreateToken("this sentence", 14, 27),
                                                                               CreateToken("this sentence into", 14,
                                                                                           32),
                                                                               CreateToken(
                                                                                   "this sentence into shingles", 14,
                                                                                   39),
                                                                               CreateToken("sentence into", 19, 32),
                                                                               CreateToken(
                                                                                   "sentence into shingles", 19, 39)
                                                                               ,
                                                                               CreateToken("into shingles", 28, 39),
                                                                           };

        public static readonly int[] FourGramPositionIncrementsWithoutUnigrams = new[]
                                                                                     {
                                                                                         1, 0, 0, 1, 0, 0, 1, 0, 0,
                                                                                         1, 0, 1
                                                                                     };

        public static readonly String[] FourGramTypesWithoutUnigrams = new[]
                                                                           {
                                                                               "shingle", "shingle",
                                                                               "shingle", "shingle",
                                                                               "shingle", "shingle",
                                                                               "shingle", "shingle",
                                                                               "shingle", "shingle",
                                                                               "shingle", "shingle",
                                                                           };

        private static Token CreateToken(String term, int start, int offset)
        {
            var token = new Token(start, offset);
            token.SetTermBuffer(term);
            return token;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            TestTokenWithHoles = new[]
                                     {
                                         CreateToken("please", 0, 6),
                                         CreateToken("divide", 7, 13),
                                         CreateToken("sentence", 19, 27),
                                         CreateToken("shingles", 33, 39),
                                     };

            TestTokenWithHoles[2].PositionIncrement = 2;
            TestTokenWithHoles[3].PositionIncrement = 2;
        }


        /// <summary>
        /// Class under test for void ShingleFilter(TokenStream, int)
        /// </summary>
        [Test]
        public void TestBiGramFilter()
        {
            ShingleFilterTest(2, TestToken, BiGramTokens,
                              BiGramPositionIncrements, BiGramTypes,
                              true);
        }

        [Test]
        public void TestBiGramFilterWithHoles()
        {
            ShingleFilterTest(2, TestTokenWithHoles, BiGramTokensWithHoles,
                              BiGramPositionIncrements, BiGramTypes,
                              true);
        }

        [Test]
        public void TestBiGramFilterWithoutUnigrams()
        {
            ShingleFilterTest(2, TestToken, BiGramTokensWithoutUnigrams,
                              BiGramPositionIncrementsWithoutUnigrams, BiGramTypesWithoutUnigrams,
                              false);
        }

        [Test]
        public void TestBiGramFilterWithHolesWithoutUnigrams()
        {
            ShingleFilterTest(2, TestTokenWithHoles, BiGramTokensWithHolesWithoutUnigrams,
                              BiGramPositionIncrementsWithHolesWithoutUnigrams, BiGramTypesWithoutUnigrams,
                              false);
        }

        [Test]
        public void TestBiGramFilterWithSingleToken()
        {
            ShingleFilterTest(2, TestSingleToken, SingleToken,
                              SingleTokenIncrements, SingleTokenTypes,
                              true);
        }

        [Test]
        public void TestBiGramFilterWithSingleTokenWithoutUnigrams()
        {
            ShingleFilterTest(2, TestSingleToken, EmptyTokenArray,
                              EmptyTokenIncrementsArray, EmptyTokenTypesArray,
                              false);
        }

        [Test]
        public void TestBiGramFilterWithEmptyTokenStream()
        {
            ShingleFilterTest(2, EmptyTokenArray, EmptyTokenArray,
                              EmptyTokenIncrementsArray, EmptyTokenTypesArray,
                              true);
        }

        [Test]
        public void TestBiGramFilterWithEmptyTokenStreamWithoutUnigrams()
        {
            ShingleFilterTest(2, EmptyTokenArray, EmptyTokenArray,
                              EmptyTokenIncrementsArray, EmptyTokenTypesArray,
                              false);
        }

        [Test]
        public void TestTriGramFilter()
        {
            ShingleFilterTest(3, TestToken, TriGramTokens,
                              TriGramPositionIncrements, TriGramTypes,
                              true);
        }

        [Test]
        public void TestTriGramFilterWithoutUnigrams()
        {
            ShingleFilterTest(3, TestToken, TriGramTokensWithoutUnigrams,
                              TriGramPositionIncrementsWithoutUnigrams, TriGramTypesWithoutUnigrams,
                              false);
        }

        [Test]
        public void TestFourGramFilter()
        {
            ShingleFilterTest(4, TestToken, FourGramTokens,
                              FourGramPositionIncrements, FourGramTypes,
                              true);
        }

        [Test]
        public void TestFourGramFilterWithoutUnigrams()
        {
            ShingleFilterTest(4, TestToken, FourGramTokensWithoutUnigrams,
                              FourGramPositionIncrementsWithoutUnigrams,
                              FourGramTypesWithoutUnigrams, false);
        }

        [Test]
        public void TestReset()
        {
            Tokenizer wsTokenizer = new WhitespaceTokenizer(new StringReader("please divide this sentence"));
            TokenStream filter = new ShingleFilter(wsTokenizer, 2);

            AssertTokenStreamContents(filter,
                                      new[]
                                          {
                                              "please", "please divide", "divide", "divide this", "this",
                                              "this sentence",
                                              "sentence"
                                          },
                                      new[] {0, 0, 7, 7, 14, 14, 19}, new[] {6, 13, 13, 18, 18, 27, 27},
                                      new[]
                                          {
                                              TypeAttribute.DEFAULT_TYPE, "shingle", TypeAttribute.DEFAULT_TYPE,
                                              "shingle", TypeAttribute.DEFAULT_TYPE, "shingle",
                                              TypeAttribute.DEFAULT_TYPE
                                          },
                                      new[] {1, 0, 1, 0, 1, 0, 1}
                );

            wsTokenizer.Reset(new StringReader("please divide this sentence"));

            AssertTokenStreamContents(filter,
                                      new[]
                                          {
                                              "please", "please divide", "divide", "divide this", "this",
                                              "this sentence",
                                              "sentence"
                                          },
                                      new[] {0, 0, 7, 7, 14, 14, 19}, new[] {6, 13, 13, 18, 18, 27, 27},
                                      new[]
                                          {
                                              TypeAttribute.DEFAULT_TYPE, "shingle", TypeAttribute.DEFAULT_TYPE,
                                              "shingle", TypeAttribute.DEFAULT_TYPE, "shingle",
                                              TypeAttribute.DEFAULT_TYPE
                                          },
                                      new[] {1, 0, 1, 0, 1, 0, 1}
                );
        }

        protected void ShingleFilterTest(int maxSize, Token[] tokensToShingle, Token[] tokensToCompare,
                                         int[] positionIncrements, String[] types, bool outputUnigrams)
        {
            var filter = new ShingleFilter(new TestTokenStream(tokensToShingle), maxSize);
            filter.SetOutputUnigrams(outputUnigrams);

            var termAtt = filter.AddAttribute<ITermAttribute>();
            var offsetAtt = filter.AddAttribute<IOffsetAttribute>();
            var posIncrAtt = filter.AddAttribute<IPositionIncrementAttribute>();
            var typeAtt = filter.AddAttribute<ITypeAttribute>();

            int i = 0;
            while (filter.IncrementToken())
            {
                Assert.IsTrue(i < tokensToCompare.Length, "ShingleFilter outputted more tokens than expected");

                String termText = termAtt.Term;
                String goldText = tokensToCompare[i].Term;

                Assert.AreEqual(goldText, termText, "Wrong termText");
                Assert.AreEqual(tokensToCompare[i].StartOffset, offsetAtt.StartOffset,
                                "Wrong startOffset for token \"" + termText + "\"");
                Assert.AreEqual(tokensToCompare[i].EndOffset, offsetAtt.EndOffset,
                                "Wrong endOffset for token \"" + termText + "\"");
                Assert.AreEqual(positionIncrements[i], posIncrAtt.PositionIncrement,
                                "Wrong positionIncrement for token \"" + termText + "\"");
                Assert.AreEqual(types[i], typeAtt.Type, "Wrong type for token \"" + termText + "\"");

                i++;
            }

            Assert.AreEqual(tokensToCompare.Length, i,
                            "ShingleFilter outputted wrong # of tokens. (# output = " + i + "; # expected =" +
                            tokensToCompare.Length + ")");
        }

        #region Nested type: TestTokenStream

        public sealed class TestTokenStream : TokenStream
        {
            private readonly IOffsetAttribute _offsetAtt;
            private readonly IPositionIncrementAttribute _posIncrAtt;
            private readonly ITermAttribute _termAtt;
            private readonly Token[] _testToken;
            private readonly ITypeAttribute _typeAtt;
            private int _index;

            public TestTokenStream(Token[] testToken)
            {
                _testToken = testToken;

                _termAtt = AddAttribute<ITermAttribute>();
                _offsetAtt = AddAttribute<IOffsetAttribute>();
                _posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                _typeAtt = AddAttribute<ITypeAttribute>();
            }

            public override bool IncrementToken()
            {
                ClearAttributes();

                if (_index >= _testToken.Length)
                    return false;

                Token t = _testToken[_index++];

                _termAtt.SetTermBuffer(t.TermBuffer(), 0, t.TermLength());
                _offsetAtt.SetOffset(t.StartOffset, t.EndOffset);
                _posIncrAtt.PositionIncrement = t.PositionIncrement;
                _typeAtt.Type = TypeAttribute.DEFAULT_TYPE;

                return true;
            }

            protected override void Dispose(bool disposing)
            {
                // do nothing
            }
        }

        #endregion
    }
}