// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Shingle;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Position
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

    public class PositionFilterTest : BaseTokenStreamTestCase
    {

        public class TestTokenStream : TokenStream
        {
            private readonly PositionFilterTest outerInstance;


            protected internal int index = 0;
            protected internal string[] testToken;
            protected internal readonly ICharTermAttribute termAtt;

            public TestTokenStream(PositionFilterTest outerInstance, string[] testToken) : base()
            {
                this.outerInstance = outerInstance;
                this.testToken = testToken;
                termAtt = AddAttribute<ICharTermAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                ClearAttributes();
                if (index < testToken.Length)
                {
                    termAtt.SetEmpty().Append(testToken[index++]);
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
        }

        public static readonly string[] TEST_TOKEN = new string[] { "please", "divide", "this", "sentence", "into", "shingles" };
        public static readonly int[] TEST_TOKEN_POSITION_INCREMENTS = new int[] { 1, 0, 0, 0, 0, 0 };
        public static readonly int[] TEST_TOKEN_NON_ZERO_POSITION_INCREMENTS = new int[] { 1, 5, 5, 5, 5, 5 };

        public static readonly string[] SIX_GRAM_NO_POSITIONS_TOKENS = new string[] { "please", "please divide", "please divide this", "please divide this sentence", "please divide this sentence into", "please divide this sentence into shingles", "divide", "divide this", "divide this sentence", "divide this sentence into", "divide this sentence into shingles", "this", "this sentence", "this sentence into", "this sentence into shingles", "sentence", "sentence into", "sentence into shingles", "into", "into shingles", "shingles" };
        public static readonly int[] SIX_GRAM_NO_POSITIONS_INCREMENTS = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static readonly string[] SIX_GRAM_NO_POSITIONS_TYPES = new string[] { "word", "shingle", "shingle", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word" };

        [Test]
        public virtual void TestFilter()
        {

            AssertTokenStreamContents(
#pragma warning disable 612, 618
                new PositionFilter(
#pragma warning restore 612, 618
                    new TestTokenStream(this, TEST_TOKEN)), TEST_TOKEN, TEST_TOKEN_POSITION_INCREMENTS);
        }

        [Test]
        public virtual void TestNonZeroPositionIncrement()
        {

            AssertTokenStreamContents(
#pragma warning disable 612, 618
                new PositionFilter(
#pragma warning restore 612, 618
                    new TestTokenStream(this, TEST_TOKEN), 5), TEST_TOKEN, TEST_TOKEN_NON_ZERO_POSITION_INCREMENTS);
        }

        [Test]
        public virtual void TestReset()
        {
#pragma warning disable 612, 618
            PositionFilter filter = new PositionFilter(new TestTokenStream(this, TEST_TOKEN));
#pragma warning restore 612, 618
            AssertTokenStreamContents(filter, TEST_TOKEN, TEST_TOKEN_POSITION_INCREMENTS);
            filter.Reset();
            // Make sure that the reset filter provides correct position increments
            AssertTokenStreamContents(filter, TEST_TOKEN, TEST_TOKEN_POSITION_INCREMENTS);
        }

        /// <summary>
        /// Tests ShingleFilter up to six shingles against six terms.
        ///  Tests PositionFilter setting all but the first positionIncrement to zero. </summary> </exception>
        /// <exception cref="java.io.IOException"> <seealso cref= Token#next(Token) </seealso>
        [Test]
        public virtual void Test6GramFilterNoPositions()
        {

            ShingleFilter filter = new ShingleFilter(new TestTokenStream(this, TEST_TOKEN), 6);
            AssertTokenStreamContents
#pragma warning disable 612, 618
                (new PositionFilter(filter),
#pragma warning restore 612, 618
                SIX_GRAM_NO_POSITIONS_TOKENS, SIX_GRAM_NO_POSITIONS_INCREMENTS);
        }
    }
}