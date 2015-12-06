using System;

namespace org.apache.lucene.analysis.shingle
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


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using WhitespaceTokenizer = org.apache.lucene.analysis.core.WhitespaceTokenizer;
	using org.apache.lucene.analysis.tokenattributes;

	public class ShingleFilterTest : BaseTokenStreamTestCase
	{

	  public static readonly Token[] TEST_TOKEN = new Token[] {createToken("please", 0, 6), createToken("divide", 7, 13), createToken("this", 14, 18), createToken("sentence", 19, 27), createToken("into", 28, 32), createToken("shingles", 33, 39)};

	  public static readonly int[] UNIGRAM_ONLY_POSITION_INCREMENTS = new int[] {1, 1, 1, 1, 1, 1};

	  public static readonly string[] UNIGRAM_ONLY_TYPES = new string[] {"word", "word", "word", "word", "word", "word"};

	  public static Token[] testTokenWithHoles;

	  public static readonly Token[] BI_GRAM_TOKENS = new Token[] {createToken("please", 0, 6), createToken("please divide", 0, 13), createToken("divide", 7, 13), createToken("divide this", 7, 18), createToken("this", 14, 18), createToken("this sentence", 14, 27), createToken("sentence", 19, 27), createToken("sentence into", 19, 32), createToken("into", 28, 32), createToken("into shingles", 28, 39), createToken("shingles", 33, 39)};

	  public static readonly int[] BI_GRAM_POSITION_INCREMENTS = new int[] {1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1};

	  public static readonly string[] BI_GRAM_TYPES = new string[] {"word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word"};

	  public static readonly Token[] BI_GRAM_TOKENS_WITH_HOLES = new Token[] {createToken("please", 0, 6), createToken("please divide", 0, 13), createToken("divide", 7, 13), createToken("divide _", 7, 19), createToken("_ sentence", 19, 27), createToken("sentence", 19, 27), createToken("sentence _", 19, 33), createToken("_ shingles", 33, 39), createToken("shingles", 33, 39)};

	  public static readonly int[] BI_GRAM_POSITION_INCREMENTS_WITH_HOLES = new int[] {1, 0, 1, 0, 1, 1, 0, 1, 1};

	  private static readonly string[] BI_GRAM_TYPES_WITH_HOLES = new string[] {"word", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word"};

	  public static readonly Token[] BI_GRAM_TOKENS_WITHOUT_UNIGRAMS = new Token[] {createToken("please divide", 0, 13), createToken("divide this", 7, 18), createToken("this sentence", 14, 27), createToken("sentence into", 19, 32), createToken("into shingles", 28, 39)};

	  public static readonly int[] BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS = new int[] {1, 1, 1, 1, 1};

	  public static readonly string[] BI_GRAM_TYPES_WITHOUT_UNIGRAMS = new string[] {"shingle", "shingle", "shingle", "shingle", "shingle"};

	  public static readonly Token[] BI_GRAM_TOKENS_WITH_HOLES_WITHOUT_UNIGRAMS = new Token[] {createToken("please divide", 0, 13), createToken("divide _", 7, 19), createToken("_ sentence", 19, 27), createToken("sentence _", 19, 33), createToken("_ shingles", 33, 39)};

	  public static readonly int[] BI_GRAM_POSITION_INCREMENTS_WITH_HOLES_WITHOUT_UNIGRAMS = new int[] {1, 1, 1, 1, 1, 1};


	  public static readonly Token[] TEST_SINGLE_TOKEN = new Token[] {createToken("please", 0, 6)};

	  public static readonly Token[] SINGLE_TOKEN = new Token[] {createToken("please", 0, 6)};

	  public static readonly int[] SINGLE_TOKEN_INCREMENTS = new int[] {1};

	  public static readonly string[] SINGLE_TOKEN_TYPES = new string[] {"word"};

	  public static readonly Token[] EMPTY_TOKEN_ARRAY = new Token[] { };

	  public static readonly int[] EMPTY_TOKEN_INCREMENTS_ARRAY = new int[] { };

	  public static readonly string[] EMPTY_TOKEN_TYPES_ARRAY = new string[] { };

	  public static readonly Token[] TRI_GRAM_TOKENS = new Token[] {createToken("please", 0, 6), createToken("please divide", 0, 13), createToken("please divide this", 0, 18), createToken("divide", 7, 13), createToken("divide this", 7, 18), createToken("divide this sentence", 7, 27), createToken("this", 14, 18), createToken("this sentence", 14, 27), createToken("this sentence into", 14, 32), createToken("sentence", 19, 27), createToken("sentence into", 19, 32), createToken("sentence into shingles", 19, 39), createToken("into", 28, 32), createToken("into shingles", 28, 39), createToken("shingles", 33, 39)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS = new int[] {1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1};

	  public static readonly string[] TRI_GRAM_TYPES = new string[] {"word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word"};

	  public static readonly Token[] TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS = new Token[] {createToken("please divide", 0, 13), createToken("please divide this", 0, 18), createToken("divide this", 7, 18), createToken("divide this sentence", 7, 27), createToken("this sentence", 14, 27), createToken("this sentence into", 14, 32), createToken("sentence into", 19, 32), createToken("sentence into shingles", 19, 39), createToken("into shingles", 28, 39)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS = new int[] {1, 0, 1, 0, 1, 0, 1, 0, 1};

	  public static readonly string[] TRI_GRAM_TYPES_WITHOUT_UNIGRAMS = new string[] {"shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle"};

	  public static readonly Token[] FOUR_GRAM_TOKENS = new Token[] {createToken("please", 0, 6), createToken("please divide", 0, 13), createToken("please divide this", 0, 18), createToken("please divide this sentence", 0, 27), createToken("divide", 7, 13), createToken("divide this", 7, 18), createToken("divide this sentence", 7, 27), createToken("divide this sentence into", 7, 32), createToken("this", 14, 18), createToken("this sentence", 14, 27), createToken("this sentence into", 14, 32), createToken("this sentence into shingles", 14, 39), createToken("sentence", 19, 27), createToken("sentence into", 19, 32), createToken("sentence into shingles", 19, 39), createToken("into", 28, 32), createToken("into shingles", 28, 39), createToken("shingles", 33, 39)};

	  public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS = new int[] {1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 1};

	  public static readonly string[] FOUR_GRAM_TYPES = new string[] {"word", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word"};

	  public static readonly Token[] FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS = new Token[] {createToken("please divide", 0, 13), createToken("please divide this", 0, 18), createToken("please divide this sentence", 0, 27), createToken("divide this", 7, 18), createToken("divide this sentence", 7, 27), createToken("divide this sentence into", 7, 32), createToken("this sentence", 14, 27), createToken("this sentence into", 14, 32), createToken("this sentence into shingles", 14, 39), createToken("sentence into", 19, 32), createToken("sentence into shingles", 19, 39), createToken("into shingles", 28, 39)};

	  public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS = new int[] {1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1};

	  public static readonly string[] FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS = new string[] {"shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle"};

	  public static readonly Token[] TRI_GRAM_TOKENS_MIN_TRI_GRAM = new Token[] {createToken("please", 0, 6), createToken("please divide this", 0, 18), createToken("divide", 7, 13), createToken("divide this sentence", 7, 27), createToken("this", 14, 18), createToken("this sentence into", 14, 32), createToken("sentence", 19, 27), createToken("sentence into shingles", 19, 39), createToken("into", 28, 32), createToken("shingles", 33, 39)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_MIN_TRI_GRAM = new int[] {1, 0, 1, 0, 1, 0, 1, 0, 1, 1};

	  public static readonly string[] TRI_GRAM_TYPES_MIN_TRI_GRAM = new string[] {"word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word", "word"};

	  public static readonly Token[] TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new Token[] {createToken("please divide this", 0, 18), createToken("divide this sentence", 7, 27), createToken("this sentence into", 14, 32), createToken("sentence into shingles", 19, 39)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new int[] {1, 1, 1, 1};

	  public static readonly string[] TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new string[] {"shingle", "shingle", "shingle", "shingle"};

	  public static readonly Token[] FOUR_GRAM_TOKENS_MIN_TRI_GRAM = new Token[] {createToken("please", 0, 6), createToken("please divide this", 0, 18), createToken("please divide this sentence", 0, 27), createToken("divide", 7, 13), createToken("divide this sentence", 7, 27), createToken("divide this sentence into", 7, 32), createToken("this", 14, 18), createToken("this sentence into", 14, 32), createToken("this sentence into shingles", 14, 39), createToken("sentence", 19, 27), createToken("sentence into shingles", 19, 39), createToken("into", 28, 32), createToken("shingles", 33, 39)};

	  public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS_MIN_TRI_GRAM = new int[] {1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1};

	  public static readonly string[] FOUR_GRAM_TYPES_MIN_TRI_GRAM = new string[] {"word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word", "word"};

	  public static readonly Token[] FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new Token[] {createToken("please divide this", 0, 18), createToken("please divide this sentence", 0, 27), createToken("divide this sentence", 7, 27), createToken("divide this sentence into", 7, 32), createToken("this sentence into", 14, 32), createToken("this sentence into shingles", 14, 39), createToken("sentence into shingles", 19, 39)};

	  public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new int[] {1, 0, 1, 0, 1, 0, 1};

	  public static readonly string[] FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_TRI_GRAM = new string[] {"shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle"};

	  public static readonly Token[] FOUR_GRAM_TOKENS_MIN_FOUR_GRAM = new Token[] {createToken("please", 0, 6), createToken("please divide this sentence", 0, 27), createToken("divide", 7, 13), createToken("divide this sentence into", 7, 32), createToken("this", 14, 18), createToken("this sentence into shingles", 14, 39), createToken("sentence", 19, 27), createToken("into", 28, 32), createToken("shingles", 33, 39)};

	  public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS_MIN_FOUR_GRAM = new int[] {1, 0, 1, 0, 1, 0, 1, 1, 1};

	  public static readonly string[] FOUR_GRAM_TYPES_MIN_FOUR_GRAM = new string[] {"word", "shingle", "word", "shingle", "word", "shingle", "word", "word", "word"};

	  public static readonly Token[] FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM = new Token[] {createToken("please divide this sentence", 0, 27), createToken("divide this sentence into", 7, 32), createToken("this sentence into shingles", 14, 39)};

	  public static readonly int[] FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM = new int[] {1, 1, 1};

	  public static readonly string[] FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM = new string[] {"shingle", "shingle", "shingle"};

	  public static readonly Token[] BI_GRAM_TOKENS_NO_SEPARATOR = new Token[] {createToken("please", 0, 6), createToken("pleasedivide", 0, 13), createToken("divide", 7, 13), createToken("dividethis", 7, 18), createToken("this", 14, 18), createToken("thissentence", 14, 27), createToken("sentence", 19, 27), createToken("sentenceinto", 19, 32), createToken("into", 28, 32), createToken("intoshingles", 28, 39), createToken("shingles", 33, 39)};

	  public static readonly int[] BI_GRAM_POSITION_INCREMENTS_NO_SEPARATOR = new int[] {1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1};

	  public static readonly string[] BI_GRAM_TYPES_NO_SEPARATOR = new string[] {"word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word"};

	  public static readonly Token[] BI_GRAM_TOKENS_WITHOUT_UNIGRAMS_NO_SEPARATOR = new Token[] {createToken("pleasedivide", 0, 13), createToken("dividethis", 7, 18), createToken("thissentence", 14, 27), createToken("sentenceinto", 19, 32), createToken("intoshingles", 28, 39)};

	  public static readonly int[] BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_NO_SEPARATOR = new int[] {1, 1, 1, 1, 1};

	  public static readonly string[] BI_GRAM_TYPES_WITHOUT_UNIGRAMS_NO_SEPARATOR = new string[] {"shingle", "shingle", "shingle", "shingle", "shingle"};

	  public static readonly Token[] TRI_GRAM_TOKENS_NO_SEPARATOR = new Token[] {createToken("please", 0, 6), createToken("pleasedivide", 0, 13), createToken("pleasedividethis", 0, 18), createToken("divide", 7, 13), createToken("dividethis", 7, 18), createToken("dividethissentence", 7, 27), createToken("this", 14, 18), createToken("thissentence", 14, 27), createToken("thissentenceinto", 14, 32), createToken("sentence", 19, 27), createToken("sentenceinto", 19, 32), createToken("sentenceintoshingles", 19, 39), createToken("into", 28, 32), createToken("intoshingles", 28, 39), createToken("shingles", 33, 39)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_NO_SEPARATOR = new int[] {1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1};

	  public static readonly string[] TRI_GRAM_TYPES_NO_SEPARATOR = new string[] {"word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word"};

	  public static readonly Token[] TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_NO_SEPARATOR = new Token[] {createToken("pleasedivide", 0, 13), createToken("pleasedividethis", 0, 18), createToken("dividethis", 7, 18), createToken("dividethissentence", 7, 27), createToken("thissentence", 14, 27), createToken("thissentenceinto", 14, 32), createToken("sentenceinto", 19, 32), createToken("sentenceintoshingles", 19, 39), createToken("intoshingles", 28, 39)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_NO_SEPARATOR = new int[] {1, 0, 1, 0, 1, 0, 1, 0, 1};

	  public static readonly string[] TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_NO_SEPARATOR = new string[] {"shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle"};

	  public static readonly Token[] BI_GRAM_TOKENS_ALT_SEPARATOR = new Token[] {createToken("please", 0, 6), createToken("please<SEP>divide", 0, 13), createToken("divide", 7, 13), createToken("divide<SEP>this", 7, 18), createToken("this", 14, 18), createToken("this<SEP>sentence", 14, 27), createToken("sentence", 19, 27), createToken("sentence<SEP>into", 19, 32), createToken("into", 28, 32), createToken("into<SEP>shingles", 28, 39), createToken("shingles", 33, 39)};

	  public static readonly int[] BI_GRAM_POSITION_INCREMENTS_ALT_SEPARATOR = new int[] {1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1};

	  public static readonly string[] BI_GRAM_TYPES_ALT_SEPARATOR = new string[] {"word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word", "shingle", "word"};

	  public static readonly Token[] BI_GRAM_TOKENS_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new Token[] {createToken("please<SEP>divide", 0, 13), createToken("divide<SEP>this", 7, 18), createToken("this<SEP>sentence", 14, 27), createToken("sentence<SEP>into", 19, 32), createToken("into<SEP>shingles", 28, 39)};

	  public static readonly int[] BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new int[] {1, 1, 1, 1, 1};

	  public static readonly string[] BI_GRAM_TYPES_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new string[] {"shingle", "shingle", "shingle", "shingle", "shingle"};

	  public static readonly Token[] TRI_GRAM_TOKENS_ALT_SEPARATOR = new Token[] {createToken("please", 0, 6), createToken("please<SEP>divide", 0, 13), createToken("please<SEP>divide<SEP>this", 0, 18), createToken("divide", 7, 13), createToken("divide<SEP>this", 7, 18), createToken("divide<SEP>this<SEP>sentence", 7, 27), createToken("this", 14, 18), createToken("this<SEP>sentence", 14, 27), createToken("this<SEP>sentence<SEP>into", 14, 32), createToken("sentence", 19, 27), createToken("sentence<SEP>into", 19, 32), createToken("sentence<SEP>into<SEP>shingles", 19, 39), createToken("into", 28, 32), createToken("into<SEP>shingles", 28, 39), createToken("shingles", 33, 39)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_ALT_SEPARATOR = new int[] {1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1};

	  public static readonly string[] TRI_GRAM_TYPES_ALT_SEPARATOR = new string[] {"word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word"};

	  public static readonly Token[] TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new Token[] {createToken("please<SEP>divide", 0, 13), createToken("please<SEP>divide<SEP>this", 0, 18), createToken("divide<SEP>this", 7, 18), createToken("divide<SEP>this<SEP>sentence", 7, 27), createToken("this<SEP>sentence", 14, 27), createToken("this<SEP>sentence<SEP>into", 14, 32), createToken("sentence<SEP>into", 19, 32), createToken("sentence<SEP>into<SEP>shingles", 19, 39), createToken("into<SEP>shingles", 28, 39)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new int[] {1, 0, 1, 0, 1, 0, 1, 0, 1};

	  public static readonly string[] TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_ALT_SEPARATOR = new string[] {"shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle"};

	  public static readonly Token[] TRI_GRAM_TOKENS_NULL_SEPARATOR = new Token[] {createToken("please", 0, 6), createToken("pleasedivide", 0, 13), createToken("pleasedividethis", 0, 18), createToken("divide", 7, 13), createToken("dividethis", 7, 18), createToken("dividethissentence", 7, 27), createToken("this", 14, 18), createToken("thissentence", 14, 27), createToken("thissentenceinto", 14, 32), createToken("sentence", 19, 27), createToken("sentenceinto", 19, 32), createToken("sentenceintoshingles", 19, 39), createToken("into", 28, 32), createToken("intoshingles", 28, 39), createToken("shingles", 33, 39)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_NULL_SEPARATOR = new int[] {1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1};

	  public static readonly string[] TRI_GRAM_TYPES_NULL_SEPARATOR = new string[] {"word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word"};

	  public static readonly Token[] TEST_TOKEN_POS_INCR_EQUAL_TO_N = new Token[] {createToken("please", 0, 6), createToken("divide", 7, 13), createToken("this", 14, 18), createToken("sentence", 29, 37, 3), createToken("into", 38, 42), createToken("shingles", 43, 49)};

	  public static readonly Token[] TRI_GRAM_TOKENS_POS_INCR_EQUAL_TO_N = new Token[] {createToken("please", 0, 6), createToken("please divide", 0, 13), createToken("please divide this", 0, 18), createToken("divide", 7, 13), createToken("divide this", 7, 18), createToken("divide this _", 7, 29), createToken("this", 14, 18), createToken("this _", 14, 29), createToken("this _ _", 14, 29), createToken("_ _ sentence", 29, 37), createToken("_ sentence", 29, 37), createToken("_ sentence into", 29, 42), createToken("sentence", 29, 37), createToken("sentence into", 29, 42), createToken("sentence into shingles", 29, 49), createToken("into", 38, 42), createToken("into shingles", 38, 49), createToken("shingles", 43, 49)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_POS_INCR_EQUAL_TO_N = new int[] {1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 1};

	  public static readonly string[] TRI_GRAM_TYPES_POS_INCR_EQUAL_TO_N = new string[] {"word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word"};

	  public static readonly Token[] TRI_GRAM_TOKENS_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS = new Token[] {createToken("please divide", 0, 13), createToken("please divide this", 0, 18), createToken("divide this", 7, 18), createToken("divide this _", 7, 29), createToken("this _", 14, 29), createToken("this _ _", 14, 29), createToken("_ _ sentence", 29, 37), createToken("_ sentence", 29, 37), createToken("_ sentence into", 29, 42), createToken("sentence into", 29, 42), createToken("sentence into shingles", 29, 49), createToken("into shingles", 38, 49)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS = new int[] {1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 1};

	  public static readonly string[] TRI_GRAM_TYPES_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS = new string[] {"shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle"};

	  public static readonly Token[] TEST_TOKEN_POS_INCR_GREATER_THAN_N = new Token[] {createToken("please", 0, 6), createToken("divide", 57, 63, 8), createToken("this", 64, 68), createToken("sentence", 69, 77), createToken("into", 78, 82), createToken("shingles", 83, 89)};

	  public static readonly Token[] TRI_GRAM_TOKENS_POS_INCR_GREATER_THAN_N = new Token[] {createToken("please", 0, 6), createToken("please _", 0, 57), createToken("please _ _", 0, 57), createToken("_ _ divide", 57, 63), createToken("_ divide", 57, 63), createToken("_ divide this", 57, 68), createToken("divide", 57, 63), createToken("divide this", 57, 68), createToken("divide this sentence", 57, 77), createToken("this", 64, 68), createToken("this sentence", 64, 77), createToken("this sentence into", 64, 82), createToken("sentence", 69, 77), createToken("sentence into", 69, 82), createToken("sentence into shingles", 69, 89), createToken("into", 78, 82), createToken("into shingles", 78, 89), createToken("shingles", 83, 89)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_POS_INCR_GREATER_THAN_N = new int[] {1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1};
	  public static readonly string[] TRI_GRAM_TYPES_POS_INCR_GREATER_THAN_N = new string[] {"word", "shingle", "shingle", "shingle", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "shingle", "word", "shingle", "word"};

	  public static readonly Token[] TRI_GRAM_TOKENS_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS = new Token[] {createToken("please _", 0, 57), createToken("please _ _", 0, 57), createToken("_ _ divide", 57, 63), createToken("_ divide", 57, 63), createToken("_ divide this", 57, 68), createToken("divide this", 57, 68), createToken("divide this sentence", 57, 77), createToken("this sentence", 64, 77), createToken("this sentence into", 64, 82), createToken("sentence into", 69, 82), createToken("sentence into shingles", 69, 89), createToken("into shingles", 78, 89)};

	  public static readonly int[] TRI_GRAM_POSITION_INCREMENTS_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS = new int[] {1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0, 1};

	  public static readonly string[] TRI_GRAM_TYPES_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS = new string[] {"shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle", "shingle"};

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setUp() throws Exception
	  public override void setUp()
	  {
		base.setUp();
		testTokenWithHoles = new Token[] {createToken("please", 0, 6), createToken("divide", 7, 13), createToken("sentence", 19, 27, 2), createToken("shingles", 33, 39, 2)};
	  }

	  /*
	   * Class under test for void ShingleFilter(TokenStream, int)
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilter() throws java.io.IOException
	  public virtual void testBiGramFilter()
	  {
		this.shingleFilterTest(2, TEST_TOKEN, BI_GRAM_TOKENS, BI_GRAM_POSITION_INCREMENTS, BI_GRAM_TYPES, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilterWithHoles() throws java.io.IOException
	  public virtual void testBiGramFilterWithHoles()
	  {
		this.shingleFilterTest(2, testTokenWithHoles, BI_GRAM_TOKENS_WITH_HOLES, BI_GRAM_POSITION_INCREMENTS_WITH_HOLES, BI_GRAM_TYPES_WITH_HOLES, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilterWithoutUnigrams() throws java.io.IOException
	  public virtual void testBiGramFilterWithoutUnigrams()
	  {
		this.shingleFilterTest(2, TEST_TOKEN, BI_GRAM_TOKENS_WITHOUT_UNIGRAMS, BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS, BI_GRAM_TYPES_WITHOUT_UNIGRAMS, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilterWithHolesWithoutUnigrams() throws java.io.IOException
	  public virtual void testBiGramFilterWithHolesWithoutUnigrams()
	  {
		this.shingleFilterTest(2, testTokenWithHoles, BI_GRAM_TOKENS_WITH_HOLES_WITHOUT_UNIGRAMS, BI_GRAM_POSITION_INCREMENTS_WITH_HOLES_WITHOUT_UNIGRAMS, BI_GRAM_TYPES_WITHOUT_UNIGRAMS, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilterWithSingleToken() throws java.io.IOException
	  public virtual void testBiGramFilterWithSingleToken()
	  {
		this.shingleFilterTest(2, TEST_SINGLE_TOKEN, SINGLE_TOKEN, SINGLE_TOKEN_INCREMENTS, SINGLE_TOKEN_TYPES, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilterWithSingleTokenWithoutUnigrams() throws java.io.IOException
	  public virtual void testBiGramFilterWithSingleTokenWithoutUnigrams()
	  {
		this.shingleFilterTest(2, TEST_SINGLE_TOKEN, EMPTY_TOKEN_ARRAY, EMPTY_TOKEN_INCREMENTS_ARRAY, EMPTY_TOKEN_TYPES_ARRAY, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilterWithEmptyTokenStream() throws java.io.IOException
	  public virtual void testBiGramFilterWithEmptyTokenStream()
	  {
		this.shingleFilterTest(2, EMPTY_TOKEN_ARRAY, EMPTY_TOKEN_ARRAY, EMPTY_TOKEN_INCREMENTS_ARRAY, EMPTY_TOKEN_TYPES_ARRAY, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilterWithEmptyTokenStreamWithoutUnigrams() throws java.io.IOException
	  public virtual void testBiGramFilterWithEmptyTokenStreamWithoutUnigrams()
	  {
		this.shingleFilterTest(2, EMPTY_TOKEN_ARRAY, EMPTY_TOKEN_ARRAY, EMPTY_TOKEN_INCREMENTS_ARRAY, EMPTY_TOKEN_TYPES_ARRAY, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTriGramFilter() throws java.io.IOException
	  public virtual void testTriGramFilter()
	  {
		this.shingleFilterTest(3, TEST_TOKEN, TRI_GRAM_TOKENS, TRI_GRAM_POSITION_INCREMENTS, TRI_GRAM_TYPES, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTriGramFilterWithoutUnigrams() throws java.io.IOException
	  public virtual void testTriGramFilterWithoutUnigrams()
	  {
		this.shingleFilterTest(3, TEST_TOKEN, TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS, TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS, TRI_GRAM_TYPES_WITHOUT_UNIGRAMS, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFourGramFilter() throws java.io.IOException
	  public virtual void testFourGramFilter()
	  {
		this.shingleFilterTest(4, TEST_TOKEN, FOUR_GRAM_TOKENS, FOUR_GRAM_POSITION_INCREMENTS, FOUR_GRAM_TYPES, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFourGramFilterWithoutUnigrams() throws java.io.IOException
	  public virtual void testFourGramFilterWithoutUnigrams()
	  {
		this.shingleFilterTest(4, TEST_TOKEN, FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS, FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS, FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS, false);
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTriGramFilterMinTriGram() throws java.io.IOException
	  public virtual void testTriGramFilterMinTriGram()
	  {
		this.shingleFilterTest(3, 3, TEST_TOKEN, TRI_GRAM_TOKENS_MIN_TRI_GRAM, TRI_GRAM_POSITION_INCREMENTS_MIN_TRI_GRAM, TRI_GRAM_TYPES_MIN_TRI_GRAM, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTriGramFilterWithoutUnigramsMinTriGram() throws java.io.IOException
	  public virtual void testTriGramFilterWithoutUnigramsMinTriGram()
	  {
		this.shingleFilterTest(3, 3, TEST_TOKEN, TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFourGramFilterMinTriGram() throws java.io.IOException
	  public virtual void testFourGramFilterMinTriGram()
	  {
		this.shingleFilterTest(3, 4, TEST_TOKEN, FOUR_GRAM_TOKENS_MIN_TRI_GRAM, FOUR_GRAM_POSITION_INCREMENTS_MIN_TRI_GRAM, FOUR_GRAM_TYPES_MIN_TRI_GRAM, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFourGramFilterWithoutUnigramsMinTriGram() throws java.io.IOException
	  public virtual void testFourGramFilterWithoutUnigramsMinTriGram()
	  {
		this.shingleFilterTest(3, 4, TEST_TOKEN, FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_TRI_GRAM, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFourGramFilterMinFourGram() throws java.io.IOException
	  public virtual void testFourGramFilterMinFourGram()
	  {
		this.shingleFilterTest(4, 4, TEST_TOKEN, FOUR_GRAM_TOKENS_MIN_FOUR_GRAM, FOUR_GRAM_POSITION_INCREMENTS_MIN_FOUR_GRAM, FOUR_GRAM_TYPES_MIN_FOUR_GRAM, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFourGramFilterWithoutUnigramsMinFourGram() throws java.io.IOException
	  public virtual void testFourGramFilterWithoutUnigramsMinFourGram()
	  {
		this.shingleFilterTest(4, 4, TEST_TOKEN, FOUR_GRAM_TOKENS_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM, FOUR_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM, FOUR_GRAM_TYPES_WITHOUT_UNIGRAMS_MIN_FOUR_GRAM, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilterNoSeparator() throws java.io.IOException
	  public virtual void testBiGramFilterNoSeparator()
	  {
		this.shingleFilterTest("", 2, 2, TEST_TOKEN, BI_GRAM_TOKENS_NO_SEPARATOR, BI_GRAM_POSITION_INCREMENTS_NO_SEPARATOR, BI_GRAM_TYPES_NO_SEPARATOR, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilterWithoutUnigramsNoSeparator() throws java.io.IOException
	  public virtual void testBiGramFilterWithoutUnigramsNoSeparator()
	  {
		this.shingleFilterTest("", 2, 2, TEST_TOKEN, BI_GRAM_TOKENS_WITHOUT_UNIGRAMS_NO_SEPARATOR, BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_NO_SEPARATOR, BI_GRAM_TYPES_WITHOUT_UNIGRAMS_NO_SEPARATOR, false);
	  }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTriGramFilterNoSeparator() throws java.io.IOException
	  public virtual void testTriGramFilterNoSeparator()
	  {
		this.shingleFilterTest("", 2, 3, TEST_TOKEN, TRI_GRAM_TOKENS_NO_SEPARATOR, TRI_GRAM_POSITION_INCREMENTS_NO_SEPARATOR, TRI_GRAM_TYPES_NO_SEPARATOR, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTriGramFilterWithoutUnigramsNoSeparator() throws java.io.IOException
	  public virtual void testTriGramFilterWithoutUnigramsNoSeparator()
	  {
		this.shingleFilterTest("", 2, 3, TEST_TOKEN, TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_NO_SEPARATOR, TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_NO_SEPARATOR, TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_NO_SEPARATOR, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilterAltSeparator() throws java.io.IOException
	  public virtual void testBiGramFilterAltSeparator()
	  {
		this.shingleFilterTest("<SEP>", 2, 2, TEST_TOKEN, BI_GRAM_TOKENS_ALT_SEPARATOR, BI_GRAM_POSITION_INCREMENTS_ALT_SEPARATOR, BI_GRAM_TYPES_ALT_SEPARATOR, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBiGramFilterWithoutUnigramsAltSeparator() throws java.io.IOException
	  public virtual void testBiGramFilterWithoutUnigramsAltSeparator()
	  {
		this.shingleFilterTest("<SEP>", 2, 2, TEST_TOKEN, BI_GRAM_TOKENS_WITHOUT_UNIGRAMS_ALT_SEPARATOR, BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_ALT_SEPARATOR, BI_GRAM_TYPES_WITHOUT_UNIGRAMS_ALT_SEPARATOR, false);
	  }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTriGramFilterAltSeparator() throws java.io.IOException
	  public virtual void testTriGramFilterAltSeparator()
	  {
		this.shingleFilterTest("<SEP>", 2, 3, TEST_TOKEN, TRI_GRAM_TOKENS_ALT_SEPARATOR, TRI_GRAM_POSITION_INCREMENTS_ALT_SEPARATOR, TRI_GRAM_TYPES_ALT_SEPARATOR, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTriGramFilterWithoutUnigramsAltSeparator() throws java.io.IOException
	  public virtual void testTriGramFilterWithoutUnigramsAltSeparator()
	  {
		this.shingleFilterTest("<SEP>", 2, 3, TEST_TOKEN, TRI_GRAM_TOKENS_WITHOUT_UNIGRAMS_ALT_SEPARATOR, TRI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS_ALT_SEPARATOR, TRI_GRAM_TYPES_WITHOUT_UNIGRAMS_ALT_SEPARATOR, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTriGramFilterNullSeparator() throws java.io.IOException
	  public virtual void testTriGramFilterNullSeparator()
	  {
		this.shingleFilterTest(null, 2, 3, TEST_TOKEN, TRI_GRAM_TOKENS_NULL_SEPARATOR, TRI_GRAM_POSITION_INCREMENTS_NULL_SEPARATOR, TRI_GRAM_TYPES_NULL_SEPARATOR, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPositionIncrementEqualToN() throws java.io.IOException
	  public virtual void testPositionIncrementEqualToN()
	  {
		this.shingleFilterTest(2, 3, TEST_TOKEN_POS_INCR_EQUAL_TO_N, TRI_GRAM_TOKENS_POS_INCR_EQUAL_TO_N, TRI_GRAM_POSITION_INCREMENTS_POS_INCR_EQUAL_TO_N, TRI_GRAM_TYPES_POS_INCR_EQUAL_TO_N, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPositionIncrementEqualToNWithoutUnigrams() throws java.io.IOException
	  public virtual void testPositionIncrementEqualToNWithoutUnigrams()
	  {
		this.shingleFilterTest(2, 3, TEST_TOKEN_POS_INCR_EQUAL_TO_N, TRI_GRAM_TOKENS_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS, TRI_GRAM_POSITION_INCREMENTS_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS, TRI_GRAM_TYPES_POS_INCR_EQUAL_TO_N_WITHOUT_UNIGRAMS, false);
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPositionIncrementGreaterThanN() throws java.io.IOException
	  public virtual void testPositionIncrementGreaterThanN()
	  {
		this.shingleFilterTest(2, 3, TEST_TOKEN_POS_INCR_GREATER_THAN_N, TRI_GRAM_TOKENS_POS_INCR_GREATER_THAN_N, TRI_GRAM_POSITION_INCREMENTS_POS_INCR_GREATER_THAN_N, TRI_GRAM_TYPES_POS_INCR_GREATER_THAN_N, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPositionIncrementGreaterThanNWithoutUnigrams() throws java.io.IOException
	  public virtual void testPositionIncrementGreaterThanNWithoutUnigrams()
	  {
		this.shingleFilterTest(2, 3, TEST_TOKEN_POS_INCR_GREATER_THAN_N, TRI_GRAM_TOKENS_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS, TRI_GRAM_POSITION_INCREMENTS_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS, TRI_GRAM_TYPES_POS_INCR_GREATER_THAN_N_WITHOUT_UNIGRAMS, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReset() throws Exception
	  public virtual void testReset()
	  {
		Tokenizer wsTokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("please divide this sentence"));
		TokenStream filter = new ShingleFilter(wsTokenizer, 2);
		assertTokenStreamContents(filter, new string[]{"please","please divide","divide","divide this","this","this sentence","sentence"}, new int[]{0,0,7,7,14,14,19}, new int[]{6,13,13,18,18,27,27}, new string[]{TypeAttribute.DEFAULT_TYPE,"shingle",TypeAttribute.DEFAULT_TYPE,"shingle",TypeAttribute.DEFAULT_TYPE,"shingle",TypeAttribute.DEFAULT_TYPE}, new int[]{1,0,1,0,1,0,1});
		wsTokenizer.Reader = new StringReader("please divide this sentence");
		assertTokenStreamContents(filter, new string[]{"please","please divide","divide","divide this","this","this sentence","sentence"}, new int[]{0,0,7,7,14,14,19}, new int[]{6,13,13,18,18,27,27}, new string[]{TypeAttribute.DEFAULT_TYPE,"shingle",TypeAttribute.DEFAULT_TYPE,"shingle",TypeAttribute.DEFAULT_TYPE,"shingle",TypeAttribute.DEFAULT_TYPE}, new int[]{1,0,1,0,1,0,1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOutputUnigramsIfNoShinglesSingleTokenCase() throws java.io.IOException
	  public virtual void testOutputUnigramsIfNoShinglesSingleTokenCase()
	  {
		// Single token input with outputUnigrams==false is the primary case where
		// enabling this option should alter program behavior.
		this.shingleFilterTest(2, 2, TEST_SINGLE_TOKEN, SINGLE_TOKEN, SINGLE_TOKEN_INCREMENTS, SINGLE_TOKEN_TYPES, false, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOutputUnigramsIfNoShinglesWithSimpleBigram() throws java.io.IOException
	  public virtual void testOutputUnigramsIfNoShinglesWithSimpleBigram()
	  {
		// Here we expect the same result as with testBiGramFilter().
		this.shingleFilterTest(2, 2, TEST_TOKEN, BI_GRAM_TOKENS, BI_GRAM_POSITION_INCREMENTS, BI_GRAM_TYPES, true, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOutputUnigramsIfNoShinglesWithSimpleUnigramlessBigram() throws java.io.IOException
	  public virtual void testOutputUnigramsIfNoShinglesWithSimpleUnigramlessBigram()
	  {
		// Here we expect the same result as with testBiGramFilterWithoutUnigrams().
		this.shingleFilterTest(2, 2, TEST_TOKEN, BI_GRAM_TOKENS_WITHOUT_UNIGRAMS, BI_GRAM_POSITION_INCREMENTS_WITHOUT_UNIGRAMS, BI_GRAM_TYPES_WITHOUT_UNIGRAMS, false, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOutputUnigramsIfNoShinglesWithMultipleInputTokens() throws java.io.IOException
	  public virtual void testOutputUnigramsIfNoShinglesWithMultipleInputTokens()
	  {
		// Test when the minimum shingle size is greater than the number of input tokens
		this.shingleFilterTest(7, 7, TEST_TOKEN, TEST_TOKEN, UNIGRAM_ONLY_POSITION_INCREMENTS, UNIGRAM_ONLY_TYPES, false, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected void shingleFilterTest(int maxSize, org.apache.lucene.analysis.Token[] tokensToShingle, org.apache.lucene.analysis.Token[] tokensToCompare, int[] positionIncrements, String[] types, boolean outputUnigrams) throws java.io.IOException
	  protected internal virtual void shingleFilterTest(int maxSize, Token[] tokensToShingle, Token[] tokensToCompare, int[] positionIncrements, string[] types, bool outputUnigrams)
	  {

		ShingleFilter filter = new ShingleFilter(new CannedTokenStream(tokensToShingle), maxSize);
		filter.OutputUnigrams = outputUnigrams;
		shingleFilterTestCommon(filter, tokensToCompare, positionIncrements, types);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected void shingleFilterTest(int minSize, int maxSize, org.apache.lucene.analysis.Token[] tokensToShingle, org.apache.lucene.analysis.Token[] tokensToCompare, int[] positionIncrements, String[] types, boolean outputUnigrams) throws java.io.IOException
	  protected internal virtual void shingleFilterTest(int minSize, int maxSize, Token[] tokensToShingle, Token[] tokensToCompare, int[] positionIncrements, string[] types, bool outputUnigrams)
	  {
		ShingleFilter filter = new ShingleFilter(new CannedTokenStream(tokensToShingle), minSize, maxSize);
		filter.OutputUnigrams = outputUnigrams;
		shingleFilterTestCommon(filter, tokensToCompare, positionIncrements, types);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected void shingleFilterTest(int minSize, int maxSize, org.apache.lucene.analysis.Token[] tokensToShingle, org.apache.lucene.analysis.Token[] tokensToCompare, int[] positionIncrements, String[] types, boolean outputUnigrams, boolean outputUnigramsIfNoShingles) throws java.io.IOException
	  protected internal virtual void shingleFilterTest(int minSize, int maxSize, Token[] tokensToShingle, Token[] tokensToCompare, int[] positionIncrements, string[] types, bool outputUnigrams, bool outputUnigramsIfNoShingles)
	  {
		ShingleFilter filter = new ShingleFilter(new CannedTokenStream(tokensToShingle), minSize, maxSize);
		filter.OutputUnigrams = outputUnigrams;
		filter.OutputUnigramsIfNoShingles = outputUnigramsIfNoShingles;
		shingleFilterTestCommon(filter, tokensToCompare, positionIncrements, types);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected void shingleFilterTest(String tokenSeparator, int minSize, int maxSize, org.apache.lucene.analysis.Token[] tokensToShingle, org.apache.lucene.analysis.Token[] tokensToCompare, int[] positionIncrements, String[] types, boolean outputUnigrams) throws java.io.IOException
	  protected internal virtual void shingleFilterTest(string tokenSeparator, int minSize, int maxSize, Token[] tokensToShingle, Token[] tokensToCompare, int[] positionIncrements, string[] types, bool outputUnigrams)
	  {
		ShingleFilter filter = new ShingleFilter(new CannedTokenStream(tokensToShingle), minSize, maxSize);
		filter.TokenSeparator = tokenSeparator;
		filter.OutputUnigrams = outputUnigrams;
		shingleFilterTestCommon(filter, tokensToCompare, positionIncrements, types);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected void shingleFilterTestCommon(ShingleFilter filter, org.apache.lucene.analysis.Token[] tokensToCompare, int[] positionIncrements, String[] types) throws java.io.IOException
	  protected internal virtual void shingleFilterTestCommon(ShingleFilter filter, Token[] tokensToCompare, int[] positionIncrements, string[] types)
	  {
		string[] text = new string[tokensToCompare.Length];
		int[] startOffsets = new int[tokensToCompare.Length];
		int[] endOffsets = new int[tokensToCompare.Length];

		for (int i = 0; i < tokensToCompare.Length; i++)
		{
		  text[i] = new string(tokensToCompare[i].buffer(),0, tokensToCompare[i].length());
		  startOffsets[i] = tokensToCompare[i].startOffset();
		  endOffsets[i] = tokensToCompare[i].endOffset();
		}

		assertTokenStreamContents(filter, text, startOffsets, endOffsets, types, positionIncrements);
	  }

	  private static Token createToken(string term, int start, int offset)
	  {
		return createToken(term, start, offset, 1);
	  }

	  private static Token createToken(string term, int start, int offset, int positionIncrement)
	  {
		Token token = new Token(start, offset);
		token.copyBuffer(term.ToCharArray(), 0, term.Length);
		token.PositionIncrement = positionIncrement;
		return token;
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly ShingleFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(ShingleFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new ShingleFilter(tokenizer));
		  }
	  }

	  /// <summary>
	  /// blast some random large strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomHugeStrings() throws Exception
	  public virtual void testRandomHugeStrings()
	  {
		Random random = random();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		checkRandomData(random, a, 100 * RANDOM_MULTIPLIER, 8192);
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly ShingleFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(ShingleFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new ShingleFilter(tokenizer));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly ShingleFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(ShingleFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new ShingleFilter(tokenizer));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTrailingHole1() throws java.io.IOException
	  public virtual void testTrailingHole1()
	  {
		// Analyzing "wizard of", where of is removed as a
		// stopword leaving a trailing hole:
		Token[] inputTokens = new Token[] {createToken("wizard", 0, 6)};
		ShingleFilter filter = new ShingleFilter(new CannedTokenStream(1, 9, inputTokens), 2, 2);

		assertTokenStreamContents(filter, new string[] {"wizard", "wizard _"}, new int[] {0, 0}, new int[] {6, 9}, new int[] {1, 0}, 9);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTrailingHole2() throws java.io.IOException
	  public virtual void testTrailingHole2()
	  {
		// Analyzing "purple wizard of", where of is removed as a
		// stopword leaving a trailing hole:
		Token[] inputTokens = new Token[] {createToken("purple", 0, 6), createToken("wizard", 7, 13)};
		ShingleFilter filter = new ShingleFilter(new CannedTokenStream(1, 16, inputTokens), 2, 2);

		assertTokenStreamContents(filter, new string[] {"purple", "purple wizard", "wizard", "wizard _"}, new int[] {0, 0, 7, 7}, new int[] {6, 13, 13, 16}, new int[] {1, 0, 1, 0}, 16);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTwoTrailingHoles() throws java.io.IOException
	  public virtual void testTwoTrailingHoles()
	  {
		// Analyzing "purple wizard of the", where of and the are removed as a
		// stopwords, leaving two trailing holes:
		Token[] inputTokens = new Token[] {createToken("purple", 0, 6), createToken("wizard", 7, 13)};
		ShingleFilter filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 2);

		assertTokenStreamContents(filter, new string[] {"purple", "purple wizard", "wizard", "wizard _"}, new int[] {0, 0, 7, 7}, new int[] {6, 13, 13, 20}, new int[] {1, 0, 1, 0}, 20);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTwoTrailingHolesTriShingle() throws java.io.IOException
	  public virtual void testTwoTrailingHolesTriShingle()
	  {
		// Analyzing "purple wizard of the", where of and the are removed as a
		// stopwords, leaving two trailing holes:
		Token[] inputTokens = new Token[] {createToken("purple", 0, 6), createToken("wizard", 7, 13)};
		ShingleFilter filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 3);

		assertTokenStreamContents(filter, new string[] {"purple", "purple wizard", "purple wizard _", "wizard", "wizard _", "wizard _ _"}, new int[] {0, 0, 0, 7, 7, 7}, new int[] {6, 13, 20, 13, 20, 20}, new int[] {1, 0, 0, 1, 0, 0}, 20);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTwoTrailingHolesTriShingleWithTokenFiller() throws java.io.IOException
	  public virtual void testTwoTrailingHolesTriShingleWithTokenFiller()
	  {
		// Analyzing "purple wizard of the", where of and the are removed as a
		// stopwords, leaving two trailing holes:
		Token[] inputTokens = new Token[] {createToken("purple", 0, 6), createToken("wizard", 7, 13)};
		ShingleFilter filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 3);
		filter.FillerToken = "--";

		assertTokenStreamContents(filter, new string[]{"purple", "purple wizard", "purple wizard --", "wizard", "wizard --", "wizard -- --"}, new int[]{0, 0, 0, 7, 7, 7}, new int[]{6, 13, 20, 13, 20, 20}, new int[]{1, 0, 0, 1, 0, 0}, 20);

		 filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 3);
		filter.FillerToken = "";

		assertTokenStreamContents(filter, new string[]{"purple", "purple wizard", "purple wizard ", "wizard", "wizard ", "wizard  "}, new int[]{0, 0, 0, 7, 7, 7}, new int[]{6, 13, 20, 13, 20, 20}, new int[]{1, 0, 0, 1, 0, 0}, 20);


		filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 3);
		filter.FillerToken = null;

		assertTokenStreamContents(filter, new string[] {"purple", "purple wizard", "purple wizard ", "wizard", "wizard ", "wizard  "}, new int[] {0, 0, 0, 7, 7, 7}, new int[] {6, 13, 20, 13, 20, 20}, new int[] {1, 0, 0, 1, 0, 0}, 20);


		filter = new ShingleFilter(new CannedTokenStream(2, 20, inputTokens), 2, 3);
		filter.FillerToken = null;
		filter.TokenSeparator = null;

		assertTokenStreamContents(filter, new string[] {"purple", "purplewizard", "purplewizard", "wizard", "wizard", "wizard"}, new int[] {0, 0, 0, 7, 7, 7}, new int[] {6, 13, 20, 13, 20, 20}, new int[] {1, 0, 0, 1, 0, 0}, 20);
	  }
	}

}