using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Ngram
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

    /// <summary>
    /// Old broken version of <seealso cref="NGramTokenizer"/>.
    /// </summary>
    [Obsolete]
    public sealed class Lucene43NGramTokenizer : Tokenizer
    {
        public const int DEFAULT_MIN_NGRAM_SIZE = 1;
        public const int DEFAULT_MAX_NGRAM_SIZE = 2;

        private int minGram, maxGram;
        private int gramSize;
        private int pos;
        private int inLen; // length of the input AFTER trim()
        private int charsRead; // length of the input
        private string inStr;
        private bool started;

        private ICharTermAttribute termAtt;
        private IOffsetAttribute offsetAtt;

        /// <summary>
        /// Creates NGramTokenizer with given min and max n-grams. </summary>
        /// <param name="input"> <seealso cref="TextReader"/> holding the input to be tokenized </param>
        /// <param name="minGram"> the smallest n-gram to generate </param>
        /// <param name="maxGram"> the largest n-gram to generate </param>
        public Lucene43NGramTokenizer(TextReader input, int minGram, int maxGram)
            : base(input)
        {
            Init(minGram, maxGram);
        }

        /// <summary>
        /// Creates NGramTokenizer with given min and max n-grams. </summary>
        /// <param name="factory"> <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/> to use </param>
        /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
        /// <param name="minGram"> the smallest n-gram to generate </param>
        /// <param name="maxGram"> the largest n-gram to generate </param>
        public Lucene43NGramTokenizer(AttributeFactory factory, TextReader input, int minGram, int maxGram)
            : base(factory, input)
        {
            Init(minGram, maxGram);
        }

        /// <summary>
        /// Creates NGramTokenizer with default min and max n-grams. </summary>
        /// <param name="input"> <seealso cref="TextReader"/> holding the input to be tokenized </param>
        public Lucene43NGramTokenizer(TextReader input)
            : this(input, DEFAULT_MIN_NGRAM_SIZE, DEFAULT_MAX_NGRAM_SIZE)
        {
        }

        private void Init(int minGram, int maxGram)
        {
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
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        /// <summary>
        /// Returns the next token in the stream, or null at EOS. </summary>
        public override bool IncrementToken()
        {
            ClearAttributes();
            if (!started)
            {
                started = true;
                gramSize = minGram;
                char[] chars = new char[1024];
                charsRead = 0;
                // TODO: refactor to a shared readFully somewhere:
                while (charsRead < chars.Length)
                {
                    int inc = input.Read(chars, charsRead, chars.Length - charsRead);
                    if (inc == -1)
                    {
                        break;
                    }
                    charsRead += inc;
                }
                inStr = (new string(chars, 0, charsRead)).Trim(); // remove any trailing empty strings

                if (charsRead == chars.Length)
                {
                    // Read extra throwaway chars so that on end() we
                    // report the correct offset:
                    var throwaway = new char[1024];
                    while (true)
                    {
                        int inc = input.Read(throwaway, 0, throwaway.Length);
                        if (inc == -1)
                        {
                            break;
                        }
                        charsRead += inc;
                    }
                }

                inLen = inStr.Length;
                if (inLen == 0)
                {
                    return false;
                }
            }

            if (pos + gramSize > inLen) // if we hit the end of the string
            {
                pos = 0; // reset to beginning of string
                gramSize++; // increase n-gram size
                if (gramSize > maxGram) // we are done
                {
                    return false;
                }
                if (pos + gramSize > inLen)
                {
                    return false;
                }
            }

            int oldPos = pos;
            pos++;
            termAtt.SetEmpty().Append(inStr, oldPos, oldPos + gramSize);
            offsetAtt.SetOffset(CorrectOffset(oldPos), CorrectOffset(oldPos + gramSize));
            return true;
        }

        public override void End()
        {
            base.End();
            // set final offset
            int finalOffset = CorrectOffset(charsRead);
            this.offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset()
        {
            base.Reset();
            started = false;
            pos = 0;
        }
    }
}