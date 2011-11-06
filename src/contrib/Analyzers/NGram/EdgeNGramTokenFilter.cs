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

using System.IO;
using System.Collections;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.NGram
{

    /**
     * Tokenizes the given token into n-grams of given size(s).
     * <p>
     * This <see cref="TokenFilter"/> create n-grams from the beginning edge or ending edge of a input token.
     * </p>
     */
    public class EdgeNGramTokenFilter : TokenFilter
    {
        public static Side DEFAULT_SIDE = Side.FRONT;
        public static int DEFAULT_MAX_GRAM_SIZE = 1;
        public static int DEFAULT_MIN_GRAM_SIZE = 1;

        // Replace this with an enum when the Java 1.5 upgrade is made, the impl will be simplified
        /** Specifies which side of the input the n-gram should be generated from */
        public class Side
        {
            private string label;

            /** Get the n-gram from the front of the input */
            public static Side FRONT = new Side("front");

            /** Get the n-gram from the end of the input */
            public static Side BACK = new Side("back");

            // Private ctor
            private Side(string label) { this.label = label; }

            public string getLabel() { return label; }

            // Get the appropriate Side from a string
            public static Side getSide(string sideName)
            {
                if (FRONT.getLabel().Equals(sideName))
                {
                    return FRONT;
                }
                else if (BACK.getLabel().Equals(sideName))
                {
                    return BACK;
                }
                return null;
            }
        }

        private int minGram;
        private int maxGram;
        private Side side;
        private char[] curTermBuffer;
        private int curTermLength;
        private int curGramSize;
        private int tokStart;

        private TermAttribute termAtt;
        private OffsetAttribute offsetAtt;


        protected EdgeNGramTokenFilter(TokenStream input) : base(input)
        {
            this.termAtt = (TermAttribute)AddAttribute(typeof(TermAttribute));
            this.offsetAtt = (OffsetAttribute)AddAttribute(typeof(OffsetAttribute));
        }

        /**
         * Creates EdgeNGramTokenFilter that can generate n-grams in the sizes of the given range
         *
         * <param name="input"><see cref="TokenStream"/> holding the input to be tokenized</param>
         * <param name="side">the <see cref="Side"/> from which to chop off an n-gram</param>
         * <param name="minGram">the smallest n-gram to generate</param>
         * <param name="maxGram">the largest n-gram to generate</param>
         */
        public EdgeNGramTokenFilter(TokenStream input, Side side, int minGram, int maxGram)
            : base(input)
        {


            if (side == null)
            {
                throw new System.ArgumentException("sideLabel must be either front or back");
            }

            if (minGram < 1)
            {
                throw new System.ArgumentException("minGram must be greater than zero");
            }

            if (minGram > maxGram)
            {
                throw new System.ArgumentException("minGram must not be greater than maxGram");
            }

            this.minGram = minGram;
            this.maxGram = maxGram;
            this.side = side;
            this.termAtt = (TermAttribute)AddAttribute(typeof(TermAttribute));
            this.offsetAtt = (OffsetAttribute)AddAttribute(typeof(OffsetAttribute));
        }

        /**
         * Creates EdgeNGramTokenFilter that can generate n-grams in the sizes of the given range
         *
         * <param name="input"><see cref="TokenStream"/> holding the input to be tokenized</param>
         * <param name="sideLabel">the name of the <see cref="Side"/> from which to chop off an n-gram</param>
         * <param name="minGram">the smallest n-gram to generate</param>
         * <param name="maxGram">the largest n-gram to generate</param>
         */
        public EdgeNGramTokenFilter(TokenStream input, string sideLabel, int minGram, int maxGram)
            : this(input, Side.getSide(sideLabel), minGram, maxGram)
        {

        }

        public override bool IncrementToken()
        {
            while (true)
            {
                if (curTermBuffer == null)
                {
                    if (!input.IncrementToken())
                    {
                        return false;
                    }
                    else
                    {
                        curTermBuffer = (char[])termAtt.TermBuffer().Clone();
                        curTermLength = termAtt.TermLength();
                        curGramSize = minGram;
                        tokStart = offsetAtt.StartOffset();
                    }
                }
                if (curGramSize <= maxGram)
                {
                    if (!(curGramSize > curTermLength         // if the remaining input is too short, we can't generate any n-grams
                        || curGramSize > maxGram))
                    {       // if we have hit the end of our n-gram size range, quit
                        // grab gramSize chars from front or back
                        int start = side == Side.FRONT ? 0 : curTermLength - curGramSize;
                        int end = start + curGramSize;
                        ClearAttributes();
                        offsetAtt.SetOffset(tokStart + start, tokStart + end);
                        termAtt.SetTermBuffer(curTermBuffer, start, curGramSize);
                        curGramSize++;
                        return true;
                    }
                }
                curTermBuffer = null;
            }
        }

        /** @deprecated Will be removed in Lucene 3.0. This method is final, as it should
         * not be overridden. Delegates to the backwards compatibility layer. */
        [System.Obsolete("Will be removed in Lucene 3.0. This method is final, as it should not be overridden. Delegates to the backwards compatibility layer.")]
        public override  Token Next(Token reusableToken)
        {
            return base.Next(reusableToken);
        }

        /** @deprecated Will be removed in Lucene 3.0. This method is final, as it should
         * not be overridden. Delegates to the backwards compatibility layer. */
        [System.Obsolete("Will be removed in Lucene 3.0. This method is final, as it should not be overridden. Delegates to the backwards compatibility layer.")]
        public override Token Next()
        {
            return base.Next();
        }

        public override void Reset()
        {
            base.Reset();
            curTermBuffer = null;
        }
    }
}