using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Position;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analyzers.Shingle;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Position
{
    [TestFixture]
    public class PositionFilterTest : BaseTokenStreamTestCase
    {
        public class TestTokenStream : TokenStream
        {
            protected int index = 0;
            protected String[] testToken;
            protected TermAttribute termAtt;

            public TestTokenStream(String[] testToken)
            {
                this.testToken = testToken;
                termAtt = AddAttribute<TermAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                ClearAttributes();
                if (index < testToken.Length)
                {
                    termAtt.SetTermBuffer(testToken[index++]);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override void Reset()
            {
                index = 0;
            }

            protected override void Dispose(bool disposing)
            {
                // do nothing
            }
        }

        public static readonly String[] TEST_TOKEN = new String[]
                                                         {
                                                             "please",
                                                             "divide",
                                                             "this",
                                                             "sentence",
                                                             "into",
                                                             "shingles",
                                                         };

        public static readonly int[] TEST_TOKEN_POSITION_INCREMENTS = new int[]
                                                                          {
                                                                              1, 0, 0, 0, 0, 0
                                                                          };

        public static readonly int[] TEST_TOKEN_NON_ZERO_POSITION_INCREMENTS = new int[]
                                                                                   {
                                                                                       1, 5, 5, 5, 5, 5
                                                                                   };

        public static readonly String[] SIX_GRAM_NO_POSITIONS_TOKENS = new String[]
                                                                           {
                                                                               "please",
                                                                               "please divide",
                                                                               "please divide this",
                                                                               "please divide this sentence",
                                                                               "please divide this sentence into",
                                                                               "please divide this sentence into shingles"
                                                                               ,
                                                                               "divide",
                                                                               "divide this",
                                                                               "divide this sentence",
                                                                               "divide this sentence into",
                                                                               "divide this sentence into shingles",
                                                                               "this",
                                                                               "this sentence",
                                                                               "this sentence into",
                                                                               "this sentence into shingles",
                                                                               "sentence",
                                                                               "sentence into",
                                                                               "sentence into shingles",
                                                                               "into",
                                                                               "into shingles",
                                                                               "shingles",
                                                                           };

        public static readonly int[] SIX_GRAM_NO_POSITIONS_INCREMENTS = new int[]
                                                                            {
                                                                                1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                                                                                , 0, 0, 0, 0, 0, 0, 0
                                                                            };

        public static readonly String[] SIX_GRAM_NO_POSITIONS_TYPES = new String[]
                                                                          {
                                                                              "word", "shingle", "shingle", "shingle",
                                                                              "shingle", "shingle",
                                                                              "word", "shingle", "shingle", "shingle",
                                                                              "shingle",
                                                                              "word", "shingle", "shingle", "shingle",
                                                                              "word", "shingle", "shingle",
                                                                              "word", "shingle",
                                                                              "word"
                                                                          };

        [Test]
        public void TestFilter()
        {
            AssertTokenStreamContents(new PositionFilter(new TestTokenStream(TEST_TOKEN)),
                                      TEST_TOKEN,
                                      TEST_TOKEN_POSITION_INCREMENTS);
        }

        [Test]
        public void TestNonZeroPositionIncrement()
        {
            AssertTokenStreamContents(new PositionFilter(new TestTokenStream(TEST_TOKEN), 5),
                                      TEST_TOKEN,
                                      TEST_TOKEN_NON_ZERO_POSITION_INCREMENTS);
        }

        [Test]
        public void TestReset()
        {
            PositionFilter filter = new PositionFilter(new TestTokenStream(TEST_TOKEN));
            AssertTokenStreamContents(filter, TEST_TOKEN, TEST_TOKEN_POSITION_INCREMENTS);
            filter.Reset();
            // Make sure that the reset filter provides correct position increments
            AssertTokenStreamContents(filter, TEST_TOKEN, TEST_TOKEN_POSITION_INCREMENTS);
        }

        /** Tests ShingleFilter up to six shingles against six terms.
         *  Tests PositionFilter setting all but the first positionIncrement to zero.
         * @throws java.io.IOException @see Token#next(Token)
         */
        [Test]
        public void Test6GramFilterNoPositions()
        {
            ShingleFilter filter = new ShingleFilter(new TestTokenStream(TEST_TOKEN), 6);
            AssertTokenStreamContents(new PositionFilter(filter),
                                      SIX_GRAM_NO_POSITIONS_TOKENS,
                                      SIX_GRAM_NO_POSITIONS_INCREMENTS);
        }
    }
}