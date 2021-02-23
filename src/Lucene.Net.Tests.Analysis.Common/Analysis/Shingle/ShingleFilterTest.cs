// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Shingle
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

    public class ShingleFilterTest : BaseTokenStreamTestCase
    {

        public static readonly Token[] TEST_TOKEN = new Token[] { CreateToken("please", 0, 6), CreateToken("divide", 7, 13), CreateToken("this", 14, 18), CreateToken("sentence", 19, 27), CreateToken("into", 28, 32), CreateToken("shingles", 33, 39) };

        public static readonly int[] UNIGRAM_ONLY_POSITION_INCREMENTS = new int[] { 1, 1, 1, 1, 1, 1 };

        public static readonly string[] UNIGRAM_ONLY_TYPES = new string[] { "word", "word", "word", "word", "word", "word" };

        public static Token[] testTokenWithHoles;

        public static readonly Token[] BI_GRAM_TOKENS = new Token[] { CreateToken("please", 0, 6), CreateToken("please divide", 0, 13), CreateToken("divide", 7, 13), CreateToken("divide this", 7, 18), CreateToken("this", 14, 18), CreateToken("this sentence", 14, 27), CreateToken("sentence", 19, 27), CreateToken("sentence into", 19, 32), CreateToken("into", 28, 32), CreateToken("into shingles", 28, 39), CreateToken("shingles", 33, 39) };

        public static readonly int[] BI_GRAM_POSITION_INCREMENTS = new int[] { 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };

        public static readonly string[] BI_GRAM_TYPES = new string[] { "word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word" };

        public static readonly Token[] BI_GRAM_TOKENS_WITH_HOLES = new Token[] { CreateToken("please", 0, 6), CreateToken("please divide", 0, 13), CreateToken("divide", 7, 13), CreateToken("divide _", 7, 19), CreateToken("_ sentence", 19, 27), CreateToken("sentence", 19, 27), CreateToken("sentence _", 19, 33), CreateToken("_ shingles", 33, 39), CreateToken("shingles", 33, 39) };

        public static readonly int[] BI_GRAM_POSITION_INCREMENTS_WITH_HOLES = new int[] { 1, 0, 1, 0, 1, 1, 0, 1, 1 };

        private static readonly string[] BI_GRAM_TYPES_WITH_HOLES = new string[] { "word", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word" };

        public static readonly Token[] BI_GRAM_TOKENS_WITHOUT_UNIGRAMS = new Token[] { CreateToken("please divide", 0, 13), CreateToken("divide this", 7, 18), CreateToken("this sentence", 14, 27), CreateToken("sentence into", 19, 32), CreateToken("into shingles", 28, 39) };

        public static readonly int[] BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS = new int[] { 1, 1, 1, 1, 1 };

        public static readonly string[] BI_GRAM_TYPES_WITHOUT_UNIGRAMS = new string[] { "shingle", "shingle", "shingle", "shingle", "shingle" };

        public static readonly Token[] BI_GRAM_TOKENS_WITH_HOLES_WITHOUT_UNIGRAMS = new Token[] { CreateToken("please divide", 0, 13), CreateToken("divide _", 7, 19), CreateToken("_ sentence", 19, 27), CreateToken("sentence _", 19, 33), CreateToken("_ shingles", 33, 39) };

        public static readonly int[] BI_GRAM_POSITION_INCREMENTS_WITH_HOLES_WITHOUT_UNIGRAMS = new int[] { 1, 1, 1, 1, 1, 1 };


        public static readonly Token[] TEST_SINGLE_TOKEN = new Token[] { CreateToken("please", 0, 6) };

        public static readonly Token[] SINGLE_TOKEN = new Token[] { CreateToken("please", 0, 6) };

        public static readonly int[] SINGLE_TOKEN_INCREMENTS = new int[] { 1 };

        public static readonly string[] SINGLE_TOKEN_TYPES = new string[] { "word" };

        public static readonly Token[] EMPTY_TOKEN_ARRAY = new Token[] { };

        public static readonly int[] EMPTY_TOKEN_INCREMENTS_ARRAY = new int[] { };

        public static readonly string[] EMPTY_TOKEN_TYPES_ARRAY = new string[] { };

        public static readonly Token[] TRI_GRAM_TOKENS = new Token[] { CreateToken("please", 0, 6), CreateToken("please divide", 0, 13), CreateToken("please divide this", 0, 18), CreateToken("divide", 7, 13), CreateToken("divide this", 7, 18), CreateToken("divide this sentence", 7, 27), CreateToken("this", 14, 18), CreateToken("this sentence", 14, 27), CreateToken("this sentence into", 14, 32), CreateToken("sentence", 19, 27), CreateToken("sentence into", 19, 32), CreateToken("sentence into shingles", 19, 39), CreateToken("into", 28, 32), CreateToken("into shingles", 28, 39), CreateToken("shingles", 33, 39) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS = new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1 };

        public static readonly string[] TRI_GRAM_TYPES = new string[] { "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word" };

        public static readonly Token[] TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS = new Token[] { CreateToken("please divide", 0, 13), CreateToken("please divide this", 0, 18), CreateToken("divide this", 7, 18), CreateToken("divide this sentence", 7, 27), CreateToken("this sentence", 14, 27), CreateToken("this sentence into", 14, 32), CreateToken("sentence into", 19, 32), CreateToken("sentence into shingles", 19, 39), CreateToken("into shingles", 28, 39) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS = new int[] { 1, 0, 1, 0, 1, 0, 1, 0, 1 };

        public static readonly string[] TRI_GRAM_TYPES_WITHOUT_UNIGRAMS = new string[] { "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle" };

        public static readonly Token[] FOUR_GRAM_TOKENS = new Token[] { CreateToken("please", 0, 6), CreateToken("please divide", 0, 13), CreateToken("please divide this", 0, 18), CreateToken("please divide this sentence", 0, 27), CreateToken("divide", 7, 13), CreateToken("divide this", 7, 18), CreateToken("divide this sentence", 7, 27), CreateToken("divide this sentence into", 7, 32), CreateToken("this", 14, 18), CreateToken("this sentence", 14, 27), CreateToken("this sentence into", 14, 32), CreateToken("this sentence into shingles", 14, 39), CreateToken("sentence", 19, 27), CreateToken("sentence into", 19, 32), CreateToken("sentence into shingles", 19, 39), CreateToken("into", 28, 32), CreateToken("into shingles", 28, 39), CreateToken("shingles", 33, 39) };

        public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS = new int[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 1 };

        public static readonly string[] FOUR_GRAM_TYPES = new string[] { "word", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word" };

        public static readonly Token[] FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS = new Token[] { CreateToken("please divide", 0, 13), CreateToken("please divide this", 0, 18), CreateToken("please divide this sentence", 0, 27), CreateToken("divide this", 7, 18), CreateToken("divide this sentence", 7, 27), CreateToken("divide this sentence into", 7, 32), CreateToken("this sentence", 14, 27), CreateToken("this sentence into", 14, 32), CreateToken("this sentence into shingles", 14, 39), CreateToken("sentence into", 19, 32), CreateToken("sentence into shingles", 19, 39), CreateToken("into shingles", 28, 39) };

        public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS = new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1 };

        public static readonly string[] FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS = new string[] { "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle" };

        public static readonly Token[] TRI_GRAM_TOKENS_MIN_TRI_GRAM = new Token[] { CreateToken("please", 0, 6), CreateToken("please divide this", 0, 18), CreateToken("divide", 7, 13), CreateToken("divide this sentence", 7, 27), CreateToken("this", 14, 18), CreateToken("this sentence into", 14, 32), CreateToken("sentence", 19, 27), CreateToken("sentence into shingles", 19, 39), CreateToken("into", 28, 32), CreateToken("shingles", 33, 39) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_MIN_TRI_GRAM = new int[] { 1, 0, 1, 0, 1, 0, 1, 0, 1, 1 };

        public static readonly string[] TRI_GRAM_TYPES_MIN_TRI_GRAM = new string[] { "word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word", "word" };

        public static readonly Token[] TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new Token[] { CreateToken("please divide this", 0, 18), CreateToken("divide this sentence", 7, 27), CreateToken("this sentence into", 14, 32), CreateToken("sentence into shingles", 19, 39) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new int[] { 1, 1, 1, 1 };

        public static readonly string[] TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new string[] { "shingle", "shingle", "shingle", "shingle" };

        public static readonly Token[] FOUR_GRAM_TOKENS_MIN_TRI_GRAM = new Token[] { CreateToken("please", 0, 6), CreateToken("please divide this", 0, 18), CreateToken("please divide this sentence", 0, 27), CreateToken("divide", 7, 13), CreateToken("divide this sentence", 7, 27), CreateToken("divide this sentence into", 7, 32), CreateToken("this", 14, 18), CreateToken("this sentence into", 14, 32), CreateToken("this sentence into shingles", 14, 39), CreateToken("sentence", 19, 27), CreateToken("sentence into shingles", 19, 39), CreateToken("into", 28, 32), CreateToken("shingles", 33, 39) };

        public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS_MIN_TRI_GRAM = new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1 };

        public static readonly string[] FOUR_GRAM_TYPES_MIN_TRI_GRAM = new string[] { "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word", "word" };

        public static readonly Token[] FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new Token[] { CreateToken("please divide this", 0, 18), CreateToken("please divide this sentence", 0, 27), CreateToken("divide this sentence", 7, 27), CreateToken("divide this sentence into", 7, 32), CreateToken("this sentence into", 14, 32), CreateToken("this sentence into shingles", 14, 39), CreateToken("sentence into shingles", 19, 39) };

        public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new int[] { 1, 0, 1, 0, 1, 0, 1 };

        public static readonly string[] FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new string[] { "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle" };

        public static readonly Token[] FOUR_GRAM_TOKENS_MIN_FOUR_GRAM = new Token[] { CreateToken("please", 0, 6), CreateToken("please divide this sentence", 0, 27), CreateToken("divide", 7, 13), CreateToken("divide this sentence into", 7, 32), CreateToken("this", 14, 18), CreateToken("this sentence into shingles", 14, 39), CreateToken("sentence", 19, 27), CreateToken("into", 28, 32), CreateToken("shingles", 33, 39) };

        public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS_MIN_FOUR_GRAM = new int[] { 1, 0, 1, 0, 1, 0, 1, 1, 1 };

        public static readonly string[] FOUR_GRAM_TYPES_MIN_FOUR_GRAM = new string[] { "word", "shingle", "word", "shingle", "word", "shingle", "word", "word", "word" };

        public static readonly Token[] FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM = new Token[] { CreateToken("please divide this sentence", 0, 27), CreateToken("divide this sentence into", 7, 32), CreateToken("this sentence into shingles", 14, 39) };

        public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM = new int[] { 1, 1, 1 };

        public static readonly string[] FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM = new string[] { "shingle", "shingle", "shingle" };

        public static readonly Token[] BI_GRAM_TOKENS_NO_SEPARATOR = new Token[] { CreateToken("please", 0, 6), CreateToken("pleasedivide", 0, 13), CreateToken("divide", 7, 13), CreateToken("dividethis", 7, 18), CreateToken("this", 14, 18), CreateToken("thissentence", 14, 27), CreateToken("sentence", 19, 27), CreateToken("sentenceinto", 19, 32), CreateToken("into", 28, 32), CreateToken("intoshingles", 28, 39), CreateToken("shingles", 33, 39) };

        public static readonly int[] BI_GRAM_POSITION_INCREMENTS_NO_SEPARATOR = new int[] { 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };

        public static readonly string[] BI_GRAM_TYPES_NO_SEPARATOR = new string[] { "word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word" };

        public static readonly Token[] BI_GRAM_TOKENS_WITHOUT_UNIGRAMS_NO_SEPARATOR = new Token[] { CreateToken("pleasedivide", 0, 13), CreateToken("dividethis", 7, 18), CreateToken("thissentence", 14, 27), CreateToken("sentenceinto", 19, 32), CreateToken("intoshingles", 28, 39) };

        public static readonly int[] BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_NO_SEPARATOR = new int[] { 1, 1, 1, 1, 1 };

        public static readonly string[] BI_GRAM_TYPES_WITHOUT_UNIGRAMS_NO_SEPARATOR = new string[] { "shingle", "shingle", "shingle", "shingle", "shingle" };

        public static readonly Token[] TRI_GRAM_TOKENS_NO_SEPARATOR = new Token[] { CreateToken("please", 0, 6), CreateToken("pleasedivide", 0, 13), CreateToken("pleasedividethis", 0, 18), CreateToken("divide", 7, 13), CreateToken("dividethis", 7, 18), CreateToken("dividethissentence", 7, 27), CreateToken("this", 14, 18), CreateToken("thissentence", 14, 27), CreateToken("thissentenceinto", 14, 32), CreateToken("sentence", 19, 27), CreateToken("sentenceinto", 19, 32), CreateToken("sentenceintoshingles", 19, 39), CreateToken("into", 28, 32), CreateToken("intoshingles", 28, 39), CreateToken("shingles", 33, 39) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_NO_SEPARATOR = new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1 };

        public static readonly string[] TRI_GRAM_TYPES_NO_SEPARATOR = new string[] { "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word" };

        public static readonly Token[] TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_NO_SEPARATOR = new Token[] { CreateToken("pleasedivide", 0, 13), CreateToken("pleasedividethis", 0, 18), CreateToken("dividethis", 7, 18), CreateToken("dividethissentence", 7, 27), CreateToken("thissentence", 14, 27), CreateToken("thissentenceinto", 14, 32), CreateToken("sentenceinto", 19, 32), CreateToken("sentenceintoshingles", 19, 39), CreateToken("intoshingles", 28, 39) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_NO_SEPARATOR = new int[] { 1, 0, 1, 0, 1, 0, 1, 0, 1 };

        public static readonly string[] TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_NO_SEPARATOR = new string[] { "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle" };

        public static readonly Token[] BI_GRAM_TOKENS_ALT_SEPARATOR = new Token[] { CreateToken("please", 0, 6), CreateToken("please<SEP>divide", 0, 13), CreateToken("divide", 7, 13), CreateToken("divide<SEP>this", 7, 18), CreateToken("this", 14, 18), CreateToken("this<SEP>sentence", 14, 27), CreateToken("sentence", 19, 27), CreateToken("sentence<SEP>into", 19, 32), CreateToken("into", 28, 32), CreateToken("into<SEP>shingles", 28, 39), CreateToken("shingles", 33, 39) };

        public static readonly int[] BI_GRAM_POSITION_INCREMENTS_ALT_SEPARATOR = new int[] { 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };

        public static readonly string[] BI_GRAM_TYPES_ALT_SEPARATOR = new string[] { "word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word" };

        public static readonly Token[] BI_GRAM_TOKENS_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new Token[] { CreateToken("please<SEP>divide", 0, 13), CreateToken("divide<SEP>this", 7, 18), CreateToken("this<SEP>sentence", 14, 27), CreateToken("sentence<SEP>into", 19, 32), CreateToken("into<SEP>shingles", 28, 39) };

        public static readonly int[] BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new int[] { 1, 1, 1, 1, 1 };

        public static readonly string[] BI_GRAM_TYPES_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new string[] { "shingle", "shingle", "shingle", "shingle", "shingle" };

        public static readonly Token[] TRI_GRAM_TOKENS_ALT_SEPARATOR = new Token[] { CreateToken("please", 0, 6), CreateToken("please<SEP>divide", 0, 13), CreateToken("please<SEP>divide<SEP>this", 0, 18), CreateToken("divide", 7, 13), CreateToken("divide<SEP>this", 7, 18), CreateToken("divide<SEP>this<SEP>sentence", 7, 27), CreateToken("this", 14, 18), CreateToken("this<SEP>sentence", 14, 27), CreateToken("this<SEP>sentence<SEP>into", 14, 32), CreateToken("sentence", 19, 27), CreateToken("sentence<SEP>into", 19, 32), CreateToken("sentence<SEP>into<SEP>shingles", 19, 39), CreateToken("into", 28, 32), CreateToken("into<SEP>shingles", 28, 39), CreateToken("shingles", 33, 39) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_ALT_SEPARATOR = new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1 };

        public static readonly string[] TRI_GRAM_TYPES_ALT_SEPARATOR = new string[] { "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word" };

        public static readonly Token[] TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new Token[] { CreateToken("please<SEP>divide", 0, 13), CreateToken("please<SEP>divide<SEP>this", 0, 18), CreateToken("divide<SEP>this", 7, 18), CreateToken("divide<SEP>this<SEP>sentence", 7, 27), CreateToken("this<SEP>sentence", 14, 27), CreateToken("this<SEP>sentence<SEP>into", 14, 32), CreateToken("sentence<SEP>into", 19, 32), CreateToken("sentence<SEP>into<SEP>shingles", 19, 39), CreateToken("into<SEP>shingles", 28, 39) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new int[] { 1, 0, 1, 0, 1, 0, 1, 0, 1 };

        public static readonly string[] TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new string[] { "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle" };

        public static readonly Token[] TRI_GRAM_TOKENS_NULL_SEPARATOR = new Token[] { CreateToken("please", 0, 6), CreateToken("pleasedivide", 0, 13), CreateToken("pleasedividethis", 0, 18), CreateToken("divide", 7, 13), CreateToken("dividethis", 7, 18), CreateToken("dividethissentence", 7, 27), CreateToken("this", 14, 18), CreateToken("thissentence", 14, 27), CreateToken("thissentenceinto", 14, 32), CreateToken("sentence", 19, 27), CreateToken("sentenceinto", 19, 32), CreateToken("sentenceintoshingles", 19, 39), CreateToken("into", 28, 32), CreateToken("intoshingles", 28, 39), CreateToken("shingles", 33, 39) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_NULL_SEPARATOR = new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1 };

        public static readonly string[] TRI_GRAM_TYPES_NULL_SEPARATOR = new string[] { "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word" };

        public static readonly Token[] TEST_TOKEN_POS_INCR_EQUAL_TO_N = new Token[] { CreateToken("please", 0, 6), CreateToken("divide", 7, 13), CreateToken("this", 14, 18), CreateToken("sentence", 29, 37, 3), CreateToken("into", 38, 42), CreateToken("shingles", 43, 49) };

        public static readonly Token[] TRI_GRAM_TOKENS_POS_INCR_EQUAL_TO_N = new Token[] { CreateToken("please", 0, 6), CreateToken("please divide", 0, 13), CreateToken("please divide this", 0, 18), CreateToken("divide", 7, 13), CreateToken("divide this", 7, 18), CreateToken("divide this _", 7, 29), CreateToken("this", 14, 18), CreateToken("this _", 14, 29), CreateToken("this _ _", 14, 29), CreateToken("_ _ sentence", 29, 37), CreateToken("_ sentence", 29, 37), CreateToken("_ sentence into", 29, 42), CreateToken("sentence", 29, 37), CreateToken("sentence into", 29, 42), CreateToken("sentence into shingles", 29, 49), CreateToken("into", 38, 42), CreateToken("into shingles", 38, 49), CreateToken("shingles", 43, 49) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_POS_INCR_EQUAL_TO_N = new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 1 };

        public static readonly string[] TRI_GRAM_TYPES_POS_INCR_EQUAL_TO_N = new string[] { "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word" };

        public static readonly Token[] TRI_GRAM_TOKENS_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS = new Token[] { CreateToken("please divide", 0, 13), CreateToken("please divide this", 0, 18), CreateToken("divide this", 7, 18), CreateToken("divide this _", 7, 29), CreateToken("this _", 14, 29), CreateToken("this _ _", 14, 29), CreateToken("_ _ sentence", 29, 37), CreateToken("_ sentence", 29, 37), CreateToken("_ sentence into", 29, 42), CreateToken("sentence into", 29, 42), CreateToken("sentence into shingles", 29, 49), CreateToken("into shingles", 38, 49) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS = new int[] { 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 1 };

        public static readonly string[] TRI_GRAM_TYPES_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS = new string[] { "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle" };

        public static readonly Token[] TEST_TOKEN_POS_INCR_GREATER_THAN_N = new Token[] { CreateToken("please", 0, 6), CreateToken("divide", 57, 63, 8), CreateToken("this", 64, 68), CreateToken("sentence", 69, 77), CreateToken("into", 78, 82), CreateToken("shingles", 83, 89) };

        public static readonly Token[] TRI_GRAM_TOKENS_POS_INCR_GREATER_THAN_N = new Token[] { CreateToken("please", 0, 6), CreateToken("please _", 0, 57), CreateToken("please _ _", 0, 57), CreateToken("_ _ divide", 57, 63), CreateToken("_ divide", 57, 63), CreateToken("_ divide this", 57, 68), CreateToken("divide", 57, 63), CreateToken("divide this", 57, 68), CreateToken("divide this sentence", 57, 77), CreateToken("this", 64, 68), CreateToken("this sentence", 64, 77), CreateToken("this sentence into", 64, 82), CreateToken("sentence", 69, 77), CreateToken("sentence into", 69, 82), CreateToken("sentence into shingles", 69, 89), CreateToken("into", 78, 82), CreateToken("into shingles", 78, 89), CreateToken("shingles", 83, 89) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_POS_INCR_GREATER_THAN_N = new int[] { 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1 };
        public static readonly string[] TRI_GRAM_TYPES_POS_INCR_GREATER_THAN_N = new string[] { "word", "shingle", "shingle", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word" };

        public static readonly Token[] TRI_GRAM_TOKENS_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS = new Token[] { CreateToken("please _", 0, 57), CreateToken("please _ _", 0, 57), CreateToken("_ _ divide", 57, 63), CreateToken("_ divide", 57, 63), CreateToken("_ divide this", 57, 68), CreateToken("divide this", 57, 68), CreateToken("divide this sentence", 57, 77), CreateToken("this sentence", 64, 77), CreateToken("this sentence into", 64, 82), CreateToken("sentence into", 69, 82), CreateToken("sentence into shingles", 69, 89), CreateToken("into shingles", 78, 89) };

        public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS = new int[] { 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0, 1 };

        public static readonly string[] TRI_GRAM_TYPES_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS = new string[] { "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle" };

        public override void SetUp()
        {
            base.SetUp();
            testTokenWithHoles = new Token[] { CreateToken("please", 0, 6), CreateToken("divide", 7, 13), CreateToken("sentence", 19, 27, 2), CreateToken("shingles", 33, 39, 2) };
        }

        /*
         * Class under test for void ShingleFilter(TokenStream, int)
         */
        [Test]
        public virtual void TestBiGramFilter()
        {
            this.shingleFilterTest(2, TEST_TOKEN, BI_GRAM_TOKENS, BI_GRAM_POSITION_INCREMENTS, BI_GRAM_TYPES, true);
        }

        [Test]
        public virtual void TestBiGramFilterWithHoles()
        {
            this.shingleFilterTest(2, testTokenWithHoles, BI_GRAM_TOKENS_WITH_HOLES, BI_GRAM_POSITION_INCREMENTS_WITH_HOLES, BI_GRAM_TYPES_WITH_HOLES, true);
        }

        [Test]
        public virtual void TestBiGramFilterWithoutUnigrams()
        {
            this.shingleFilterTest(2, TEST_TOKEN, BI_GRAM_TOKENS_WITHOUT_UNIGRAMS, BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS, BI_GRAM_TYPES_WITHOUT_UNIGRAMS, false);
        }

        [Test]
        public virtual void TestBiGramFilterWithHolesWithoutUnigrams()
        {
            this.shingleFilterTest(2, testTokenWithHoles, BI_GRAM_TOKENS_WITH_HOLES_WITHOUT_UNIGRAMS, BI_GRAM_POSITION_INCREMENTS_WITH_HOLES_WITHOUT_UNIGRAMS, BI_GRAM_TYPES_WITHOUT_UNIGRAMS, false);
        }

        [Test]
        public virtual void TestBiGramFilterWithSingleToken()
        {
            this.shingleFilterTest(2, TEST_SINGLE_TOKEN, SINGLE_TOKEN, SINGLE_TOKEN_INCREMENTS, SINGLE_TOKEN_TYPES, true);
        }

        [Test]
        public virtual void TestBiGramFilterWithSingleTokenWithoutUnigrams()
        {
            this.shingleFilterTest(2, TEST_SINGLE_TOKEN, EMPTY_TOKEN_ARRAY, EMPTY_TOKEN_INCREMENTS_ARRAY, EMPTY_TOKEN_TYPES_ARRAY, false);
        }

        [Test]
        public virtual void TestBiGramFilterWithEmptyTokenStream()
        {
            this.shingleFilterTest(2, EMPTY_TOKEN_ARRAY, EMPTY_TOKEN_ARRAY, EMPTY_TOKEN_INCREMENTS_ARRAY, EMPTY_TOKEN_TYPES_ARRAY, true);
        }

        [Test]
        public virtual void TestBiGramFilterWithEmptyTokenStreamWithoutUnigrams()
        {
            this.shingleFilterTest(2, EMPTY_TOKEN_ARRAY, EMPTY_TOKEN_ARRAY, EMPTY_TOKEN_INCREMENTS_ARRAY, EMPTY_TOKEN_TYPES_ARRAY, false);
        }

        [Test]
        public virtual void TestTriGramFilter()
        {
            this.shingleFilterTest(3, TEST_TOKEN, TRI_GRAM_TOKENS, TRI_GRAM_POSITION_INCREMENTS, TRI_GRAM_TYPES, true);
        }

        [Test]
        public virtual void TestTriGramFilterWithoutUnigrams()
        {
            this.shingleFilterTest(3, TEST_TOKEN, TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS, TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS, TRI_GRAM_TYPES_WITHOUT_UNIGRAMS, false);
        }

        [Test]
        public virtual void TestFourGramFilter()
        {
            this.shingleFilterTest(4, TEST_TOKEN, FOUR_GRAM_TOKENS, FOUR_GRAM_POSITION_INCREMENTS, FOUR_GRAM_TYPES, true);
        }

        [Test]
        public virtual void TestFourGramFilterWithoutUnigrams()
        {
            this.shingleFilterTest(4, TEST_TOKEN, FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS, FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS, FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS, false);
        }


        [Test]
        public virtual void TestTriGramFilterMinTriGram()
        {
            this.shingleFilterTest(3, 3, TEST_TOKEN, TRI_GRAM_TOKENS_MIN_TRI_GRAM, TRI_GRAM_POSITION_INCREMENTS_MIN_TRI_GRAM, TRI_GRAM_TYPES_MIN_TRI_GRAM, true);
        }

        [Test]
        public virtual void TestTriGramFilterWithoutUnigramsMinTriGram()
        {
            this.shingleFilterTest(3, 3, TEST_TOKEN, TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, false);
        }

        [Test]
        public virtual void TestFourGramFilterMinTriGram()
        {
            this.shingleFilterTest(3, 4, TEST_TOKEN, FOUR_GRAM_TOKENS_MIN_TRI_GRAM, FOUR_GRAM_POSITION_INCREMENTS_MIN_TRI_GRAM, FOUR_GRAM_TYPES_MIN_TRI_GRAM, true);
        }

        [Test]
        public virtual void TestFourGramFilterWithoutUnigramsMinTriGram()
        {
            this.shingleFilterTest(3, 4, TEST_TOKEN, FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, false);
        }

        [Test]
        public virtual void TestFourGramFilterMinFourGram()
        {
            this.shingleFilterTest(4, 4, TEST_TOKEN, FOUR_GRAM_TOKENS_MIN_FOUR_GRAM, FOUR_GRAM_POSITION_INCREMENTS_MIN_FOUR_GRAM, FOUR_GRAM_TYPES_MIN_FOUR_GRAM, true);
        }

        [Test]
        public virtual void TestFourGramFilterWithoutUnigramsMinFourGram()
        {
            this.shingleFilterTest(4, 4, TEST_TOKEN, FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM, FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM, FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM, false);
        }

        [Test]
        public virtual void TestBiGramFilterNoSeparator()
        {
            this.shingleFilterTest("", 2, 2, TEST_TOKEN, BI_GRAM_TOKENS_NO_SEPARATOR, BI_GRAM_POSITION_INCREMENTS_NO_SEPARATOR, BI_GRAM_TYPES_NO_SEPARATOR, true);
        }

        [Test]
        public virtual void TestBiGramFilterWithoutUnigramsNoSeparator()
        {
            this.shingleFilterTest("", 2, 2, TEST_TOKEN, BI_GRAM_TOKENS_WITHOUT_UNIGRAMS_NO_SEPARATOR, BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_NO_SEPARATOR, BI_GRAM_TYPES_WITHOUT_UNIGRAMS_NO_SEPARATOR, false);
        }
        [Test]
        public virtual void TestTriGramFilterNoSeparator()
        {
            this.shingleFilterTest("", 2, 3, TEST_TOKEN, TRI_GRAM_TOKENS_NO_SEPARATOR, TRI_GRAM_POSITION_INCREMENTS_NO_SEPARATOR, TRI_GRAM_TYPES_NO_SEPARATOR, true);
        }

        [Test]
        public virtual void TestTriGramFilterWithoutUnigramsNoSeparator()
        {
            this.shingleFilterTest("", 2, 3, TEST_TOKEN, TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_NO_SEPARATOR, TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_NO_SEPARATOR, TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_NO_SEPARATOR, false);
        }

        [Test]
        public virtual void TestBiGramFilterAltSeparator()
        {
            this.shingleFilterTest("<SEP>", 2, 2, TEST_TOKEN, BI_GRAM_TOKENS_ALT_SEPARATOR, BI_GRAM_POSITION_INCREMENTS_ALT_SEPARATOR, BI_GRAM_TYPES_ALT_SEPARATOR, true);
        }

        [Test]
        public virtual void TestBiGramFilterWithoutUnigramsAltSeparator()
        {
            this.shingleFilterTest("<SEP>", 2, 2, TEST_TOKEN, BI_GRAM_TOKENS_WITHOUT_UNIGRAMS_ALT_SEPARATOR, BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_ALT_SEPARATOR, BI_GRAM_TYPES_WITHOUT_UNIGRAMS_ALT_SEPARATOR, false);
        }
        [Test]
        public virtual void TestTriGramFilterAltSeparator()
        {
            this.shingleFilterTest("<SEP>", 2, 3, TEST_TOKEN, TRI_GRAM_TOKENS_ALT_SEPARATOR, TRI_GRAM_POSITION_INCREMENTS_ALT_SEPARATOR, TRI_GRAM_TYPES_ALT_SEPARATOR, true);
        }

        [Test]
        public virtual void TestTriGramFilterWithoutUnigramsAltSeparator()
        {
            this.shingleFilterTest("<SEP>", 2, 3, TEST_TOKEN, TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_ALT_SEPARATOR, TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_ALT_SEPARATOR, TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_ALT_SEPARATOR, false);
        }

        [Test]
        public virtual void TestTriGramFilterNullSeparator()
        {
            this.shingleFilterTest(null, 2, 3, TEST_TOKEN, TRI_GRAM_TOKENS_NULL_SEPARATOR, TRI_GRAM_POSITION_INCREMENTS_NULL_SEPARATOR, TRI_GRAM_TYPES_NULL_SEPARATOR, true);
        }

        [Test]
        public virtual void TestPositionIncrementEqualToN()
        {
            this.shingleFilterTest(2, 3, TEST_TOKEN_POS_INCR_EQUAL_TO_N, TRI_GRAM_TOKENS_POS_INCR_EQUAL_TO_N, TRI_GRAM_POSITION_INCREMENTS_POS_INCR_EQUAL_TO_N, TRI_GRAM_TYPES_POS_INCR_EQUAL_TO_N, true);
        }

        [Test]
        public virtual void TestPositionIncrementEqualToNWithoutUnigrams()
        {
            this.shingleFilterTest(2, 3, TEST_TOKEN_POS_INCR_EQUAL_TO_N, TRI_GRAM_TOKENS_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS, TRI_GRAM_POSITION_INCREMENTS_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS, TRI_GRAM_TYPES_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS, false);
        }


        [Test]
        public virtual void TestPositionIncrementGreaterThanN()
        {
            this.shingleFilterTest(2, 3, TEST_TOKEN_POS_INCR_GREATER_THAN_N, TRI_GRAM_TOKENS_POS_INCR_GREATER_THAN_N, TRI_GRAM_POSITION_INCREMENTS_POS_INCR_GREATER_THAN_N, TRI_GRAM_TYPES_POS_INCR_GREATER_THAN_N, true);
        }

        [Test]
        public virtual void TestPositionIncrementGreaterThanNWithoutUnigrams()
        {
            this.shingleFilterTest(2, 3, TEST_TOKEN_POS_INCR_GREATER_THAN_N, TRI_GRAM_TOKENS_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS, TRI_GRAM_POSITION_INCREMENTS_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS, TRI_GRAM_TYPES_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS, false);
        }

        [Test]
        public virtual void TestReset()
        {
            Tokenizer wsTokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("please divide this sentence"));
            TokenStream filter = new ShingleFilter(wsTokenizer, 2);
            AssertTokenStreamContents(filter, new string[] { "please", "please divide", "divide", "divide this", "this", "this sentence", "sentence" }, new int[] { 0, 0, 7, 7, 14, 14, 19 }, new int[] { 6, 13, 13, 18, 18, 27, 27 }, new string[] { TypeAttribute.DEFAULT_TYPE, "shingle", TypeAttribute.DEFAULT_TYPE, "shingle", TypeAttribute.DEFAULT_TYPE, "shingle", TypeAttribute.DEFAULT_TYPE }, new int[] { 1, 0, 1, 0, 1, 0, 1 });
            wsTokenizer.SetReader(new StringReader("please divide this sentence"));
            AssertTokenStreamContents(filter, new string[] { "please", "please divide", "divide", "divide this", "this", "this sentence", "sentence" }, new int[] { 0, 0, 7, 7, 14, 14, 19 }, new int[] { 6, 13, 13, 18, 18, 27, 27 }, new string[] { TypeAttribute.DEFAULT_TYPE, "shingle", TypeAttribute.DEFAULT_TYPE, "shingle", TypeAttribute.DEFAULT_TYPE, "shingle", TypeAttribute.DEFAULT_TYPE }, new int[] { 1, 0, 1, 0, 1, 0, 1 });
        }

        [Test]
        public virtual void TestOutputUnigramsIfNoShinglesSingleTokenCase()
        {
            // Single token input with outputUnigrams==false is the primary case where
            // enabling this option should alter program behavior.
            this.shingleFilterTest(2, 2, TEST_SINGLE_TOKEN, SINGLE_TOKEN, SINGLE_TOKEN_INCREMENTS, SINGLE_TOKEN_TYPES, false, true);
        }

        [Test]
        public virtual void TestOutputUnigramsIfNoShinglesWithSimpleBigram()
        {
            // Here we expect the same result as with testBiGramFilter().
            this.shingleFilterTest(2, 2, TEST_TOKEN, BI_GRAM_TOKENS, BI_GRAM_POSITION_INCREMENTS, BI_GRAM_TYPES, true, true);
        }

        [Test]
        public virtual void TestOutputUnigramsIfNoShinglesWithSimpleUnigramlessBigram()
        {
            // Here we expect the same result as with testBiGramFilterWithoutUnigrams().
            this.shingleFilterTest(2, 2, TEST_TOKEN, BI_GRAM_TOKENS_WITHOUT_UNIGRAMS, BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS, BI_GRAM_TYPES_WITHOUT_UNIGRAMS, false, true);
        }

        [Test]
        public virtual void TestOutputUnigramsIfNoShinglesWithMultipleInputTokens()
        {
            // Test when the minimum shingle size is greater than the number of input tokens
            this.shingleFilterTest(7, 7, TEST_TOKEN, TEST_TOKEN, UNIGRAM_ONLY_POSITION_INCREMENTS, UNIGRAM_ONLY_TYPES, false, true);
        }

        protected internal virtual void shingleFilterTest(int maxSize, Token[] tokensToShingle, Token[] tokensToCompare, int[] positionIncrements, string[] types, bool outputUnigrams)
        {

            ShingleFilter filter = new ShingleFilter(new CannedTokenStream(tokensToShingle), maxSize);
            filter.SetOutputUnigrams(outputUnigrams);
            shingleFilterTestCommon(filter, tokensToCompare, positionIncrements, types);
        }

        protected internal virtual void shingleFilterTest(int minSize, int maxSize, Token[] tokensToShingle, Token[] tokensToCompare, int[] positionIncrements, string[] types, bool outputUnigrams)
        {
            ShingleFilter filter = new ShingleFilter(new CannedTokenStream(tokensToShingle), minSize, maxSize);
            filter.SetOutputUnigrams(outputUnigrams);
            shingleFilterTestCommon(filter, tokensToCompare, positionIncrements, types);
        }

        protected internal virtual void shingleFilterTest(int minSize, int maxSize, Token[] tokensToShingle, Token[] tokensToCompare, int[] positionIncrements, string[] types, bool outputUnigrams, bool outputUnigramsIfNoShingles)
        {
            ShingleFilter filter = new ShingleFilter(new CannedTokenStream(tokensToShingle), minSize, maxSize);
            filter.SetOutputUnigrams(outputUnigrams);
            filter.SetOutputUnigramsIfNoShingles(outputUnigramsIfNoShingles);
            shingleFilterTestCommon(filter, tokensToCompare, positionIncrements, types);
        }

        protected internal virtual void shingleFilterTest(string tokenSeparator, int minSize, int maxSize, Token[] tokensToShingle, Token[] tokensToCompare, int[] positionIncrements, string[] types, bool outputUnigrams)
        {
            ShingleFilter filter = new ShingleFilter(new CannedTokenStream(tokensToShingle), minSize, maxSize);
            filter.SetTokenSeparator(tokenSeparator);
            filter.SetOutputUnigrams(outputUnigrams);
            shingleFilterTestCommon(filter, tokensToCompare, positionIncrements, types);
        }
        protected internal virtual void shingleFilterTestCommon(ShingleFilter filter, Token[] tokensToCompare, int[] positionIncrements, string[] types)
        {
            string[] text = new string[tokensToCompare.Length];
            int[] startOffsets = new int[tokensToCompare.Length];
            int[] endOffsets = new int[tokensToCompare.Length];

            for (int i = 0; i < tokensToCompare.Length; i++)
            {
                text[i] = new string(tokensToCompare[i].Buffer, 0, tokensToCompare[i].Length);
                startOffsets[i] = tokensToCompare[i].StartOffset;
                endOffsets[i] = tokensToCompare[i].EndOffset;
            }

            AssertTokenStreamContents(filter, text, startOffsets, endOffsets, types, positionIncrements);
        }

        private static Token CreateToken(string term, int start, int offset)
        {
            return CreateToken(term, start, offset, 1);
        }

        private static Token CreateToken(string term, int start, int offset, int positionIncrement)
        {
            Token token = new Token(start, offset);
            token.CopyBuffer(term.ToCharArray(), 0, term.Length);
            token.PositionIncrement = positionIncrement;
            return token;
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new ShingleFilter(tokenizer));
            });
            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }

        /// <summary>
        /// blast some random large strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomHugeStrings()
        {
            Random random = Random;
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new ShingleFilter(tokenizer));
            });
            CheckRandomData(random, a, 100 * RandomMultiplier, 8192);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new ShingleFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }

        [Test]
        public virtual void TestTrailingHole1()
        {
            // Analyzing "wizard of", where of is removed as a
            // stopword leaving a trailing hole:
            Token[] inputTokens = new Token[] { CreateToken("wizard", 0, 6) };
            ShingleFilter filter = new ShingleFilter(new CannedTokenStream(1, 9, inputTokens), 2, 2);

            AssertTokenStreamContents(filter, new string[] { "wizard", "wizard _" }, new int[] { 0, 0 }, new int[] { 6, 9 }, new int[] { 1, 0 }, 9);
        }

        [Test]
        public virtual void TestTrailingHole2()
        {
            // Analyzing "purple wizard of", where of is removed as a
            // stopword leaving a trailing hole:
            Token[] inputTokens = new Token[] { CreateToken("purple", 0, 6), CreateToken("wizard", 7, 13) };
            ShingleFilter filter = new ShingleFilter(new CannedTokenStream(1, 16, inputTokens), 2, 2);

            AssertTokenStreamContents(filter, new string[] { "purple", "purple wizard", "wizard", "wizard _" }, new int[] { 0, 0, 7, 7 }, new int[] { 6, 13, 13, 16 }, new int[] { 1, 0, 1, 0 }, 16);
        }

        [Test]
        public virtual void TestTwoTrailingHoles()
        {
            // Analyzing "purple wizard of the", where of and the are removed as a
            // stopwords, leaving two trailing holes:
            Token[] inputTokens = new Token[] { CreateToken("purple", 0, 6), CreateToken("wizard", 7, 13) };
            ShingleFilter filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 2);

            AssertTokenStreamContents(filter, new string[] { "purple", "purple wizard", "wizard", "wizard _" }, new int[] { 0, 0, 7, 7 }, new int[] { 6, 13, 13, 20 }, new int[] { 1, 0, 1, 0 }, 20);
        }

        [Test]
        public virtual void TestTwoTrailingHolesTriShingle()
        {
            // Analyzing "purple wizard of the", where of and the are removed as a
            // stopwords, leaving two trailing holes:
            Token[] inputTokens = new Token[] { CreateToken("purple", 0, 6), CreateToken("wizard", 7, 13) };
            ShingleFilter filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 3);

            AssertTokenStreamContents(filter, new string[] { "purple", "purple wizard", "purple wizard _", "wizard", "wizard _", "wizard _ _" }, new int[] { 0, 0, 0, 7, 7, 7 }, new int[] { 6, 13, 20, 13, 20, 20 }, new int[] { 1, 0, 0, 1, 0, 0 }, 20);
        }

        [Test]
        public virtual void TestTwoTrailingHolesTriShingleWithTokenFiller()
        {
            // Analyzing "purple wizard of the", where of and the are removed as a
            // stopwords, leaving two trailing holes:
            Token[] inputTokens = new Token[] { CreateToken("purple", 0, 6), CreateToken("wizard", 7, 13) };
            ShingleFilter filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 3);
            filter.SetFillerToken("--");

            AssertTokenStreamContents(filter, new string[] { "purple", "purple wizard", "purple wizard --", "wizard", "wizard --", "wizard -- --" }, new int[] { 0, 0, 0, 7, 7, 7 }, new int[] { 6, 13, 20, 13, 20, 20 }, new int[] { 1, 0, 0, 1, 0, 0 }, 20);

            filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 3);
            filter.SetFillerToken("");

            AssertTokenStreamContents(filter, new string[] { "purple", "purple wizard", "purple wizard ", "wizard", "wizard ", "wizard  " }, new int[] { 0, 0, 0, 7, 7, 7 }, new int[] { 6, 13, 20, 13, 20, 20 }, new int[] { 1, 0, 0, 1, 0, 0 }, 20);


            filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 3);
            filter.SetFillerToken(null);

            AssertTokenStreamContents(filter, new string[] { "purple", "purple wizard", "purple wizard ", "wizard", "wizard ", "wizard  " }, new int[] { 0, 0, 0, 7, 7, 7 }, new int[] { 6, 13, 20, 13, 20, 20 }, new int[] { 1, 0, 0, 1, 0, 0 }, 20);


            filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 3);
            filter.SetFillerToken(null);
            filter.SetTokenSeparator(null);

            AssertTokenStreamContents(filter, new string[] { "purple", "purplewizard", "purplewizard", "wizard", "wizard", "wizard" }, new int[] { 0, 0, 0, 7, 7, 7 }, new int[] { 6, 13, 20, 13, 20, 20 }, new int[] { 1, 0, 0, 1, 0, 0 }, 20);
        }
    }
}