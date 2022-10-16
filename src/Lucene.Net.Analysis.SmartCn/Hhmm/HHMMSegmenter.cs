// lucene version compatibility level: 4.8.1
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Analysis.Cn.Smart.Hhmm
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
    /// Finds the optimal segmentation of a sentence into Chinese words
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class HHMMSegmenter
    {
        private static readonly WordDictionary wordDict = WordDictionary.GetInstance(); // LUCENENET: marked readonly

        /// <summary>
        /// Create the <see cref="SegGraph"/> for a sentence.
        /// </summary>
        /// <param name="sentence">input sentence, without start and end markers</param>
        /// <returns><see cref="SegGraph"/> corresponding to the input sentence.</returns>
        private static SegGraph CreateSegGraph(string sentence) // LUCENENET: CA1822: Mark members as static
        {
            int i = 0, j;
            int length = sentence.Length;
            int foundIndex;
            CharType[] charTypeArray = GetCharTypes(sentence);
            StringBuilder wordBuf = new StringBuilder();
            SegToken token;
            int frequency; // the number of times word appears. // LUCENENET: IDE0059: Remove unnecessary value assignment
            bool hasFullWidth;
            WordType wordType;
            char[] charArray;

            SegGraph segGraph = new SegGraph();
            while (i < length)
            {
                hasFullWidth = false;
                switch (charTypeArray[i])
                {
                    case CharType.SPACE_LIKE:
                        i++;
                        break;
                    case CharType.HANZI:
                        j = i + 1;
                        //wordBuf.delete(0, wordBuf.length());
                        wordBuf.Remove(0, wordBuf.Length);
                        // It doesn't matter if a single Chinese character (Hanzi) can form a phrase or not, 
                        // it will store that single Chinese character (Hanzi) in the SegGraph.  Otherwise, it will 
                        // cause word division.
                        wordBuf.Append(sentence[i]);
                        charArray = new char[] { sentence[i] };
                        frequency = wordDict.GetFrequency(charArray);
                        token = new SegToken(charArray, i, j, WordType.CHINESE_WORD,
                            frequency);
                        segGraph.AddToken(token);

                        foundIndex = wordDict.GetPrefixMatch(charArray);
                        while (j <= length && foundIndex != -1)
                        {
                            if (wordDict.IsEqual(charArray, foundIndex) && charArray.Length > 1)
                            {
                                // It is the phrase we are looking for; In other words, we have found a phrase SegToken
                                // from i to j.  It is not a monosyllabic word (single word).
                                frequency = wordDict.GetFrequency(charArray);
                                token = new SegToken(charArray, i, j, WordType.CHINESE_WORD,
                                    frequency);
                                segGraph.AddToken(token);
                            }

                            while (j < length && charTypeArray[j] == CharType.SPACE_LIKE)
                                j++;

                            if (j < length && charTypeArray[j] == CharType.HANZI)
                            {
                                wordBuf.Append(sentence[j]);
                                charArray = new char[wordBuf.Length];
                                //wordBuf.GetChars(0, charArray.Length, charArray, 0);
                                wordBuf.CopyTo(0, charArray, 0, charArray.Length);
                                // idArray has been found (foundWordIndex!=-1) as a prefix before.  
                                // Therefore, idArray after it has been lengthened can only appear after foundWordIndex.  
                                // So start searching after foundWordIndex.
                                foundIndex = wordDict.GetPrefixMatch(charArray, foundIndex);
                                j++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        i++;
                        break;
                    case CharType.FULLWIDTH_LETTER:
                        hasFullWidth = true; /* intentional fallthrough */

                        j = i + 1;
                        while (j < length
                            && (charTypeArray[j] == CharType.LETTER || charTypeArray[j] == CharType.FULLWIDTH_LETTER))
                        {
                            if (charTypeArray[j] == CharType.FULLWIDTH_LETTER)
                                hasFullWidth = true;
                            j++;
                        }
                        // Found a Token from i to j. Type is LETTER char string.
                        charArray = Utility.STRING_CHAR_ARRAY;
                        frequency = wordDict.GetFrequency(charArray);
                        wordType = hasFullWidth ? WordType.FULLWIDTH_STRING : WordType.STRING;
                        token = new SegToken(charArray, i, j, wordType, frequency);
                        segGraph.AddToken(token);
                        i = j;
                        break;

                    case CharType.LETTER:
                        j = i + 1;
                        while (j < length
                            && (charTypeArray[j] == CharType.LETTER || charTypeArray[j] == CharType.FULLWIDTH_LETTER))
                        {
                            if (charTypeArray[j] == CharType.FULLWIDTH_LETTER)
                                hasFullWidth = true;
                            j++;
                        }
                        // Found a Token from i to j. Type is LETTER char string.
                        charArray = Utility.STRING_CHAR_ARRAY;
                        frequency = wordDict.GetFrequency(charArray);
                        wordType = hasFullWidth ? WordType.FULLWIDTH_STRING : WordType.STRING;
                        token = new SegToken(charArray, i, j, wordType, frequency);
                        segGraph.AddToken(token);
                        i = j;
                        break;
                    case CharType.FULLWIDTH_DIGIT:
                        hasFullWidth = true; /* intentional fallthrough */

                        j = i + 1;
                        while (j < length
                            && (charTypeArray[j] == CharType.DIGIT || charTypeArray[j] == CharType.FULLWIDTH_DIGIT))
                        {
                            if (charTypeArray[j] == CharType.FULLWIDTH_DIGIT)
                                hasFullWidth = true;
                            j++;
                        }
                        // Found a Token from i to j. Type is NUMBER char string.
                        charArray = Utility.NUMBER_CHAR_ARRAY;
                        frequency = wordDict.GetFrequency(charArray);
                        wordType = hasFullWidth ? WordType.FULLWIDTH_NUMBER : WordType.NUMBER;
                        token = new SegToken(charArray, i, j, wordType, frequency);
                        segGraph.AddToken(token);
                        i = j;
                        break;

                    case CharType.DIGIT:
                        j = i + 1;
                        while (j < length
                            && (charTypeArray[j] == CharType.DIGIT || charTypeArray[j] == CharType.FULLWIDTH_DIGIT))
                        {
                            if (charTypeArray[j] == CharType.FULLWIDTH_DIGIT)
                                hasFullWidth = true;
                            j++;
                        }
                        // Found a Token from i to j. Type is NUMBER char string.
                        charArray = Utility.NUMBER_CHAR_ARRAY;
                        frequency = wordDict.GetFrequency(charArray);
                        wordType = hasFullWidth ? WordType.FULLWIDTH_NUMBER : WordType.NUMBER;
                        token = new SegToken(charArray, i, j, wordType, frequency);
                        segGraph.AddToken(token);
                        i = j;
                        break;
                    case CharType.DELIMITER:
                        j = i + 1;
                        // No need to search the weight for the punctuation.  Picking the highest frequency will work.
                        frequency = Utility.MAX_FREQUENCE;
                        charArray = new char[] { sentence[i] };
                        token = new SegToken(charArray, i, j, WordType.DELIMITER, frequency);
                        segGraph.AddToken(token);
                        i = j;
                        break;
                    default:
                        j = i + 1;
                        // Treat the unrecognized char symbol as unknown string.
                        // For example, any symbol not in GB2312 is treated as one of these.
                        charArray = Utility.STRING_CHAR_ARRAY;
                        frequency = wordDict.GetFrequency(charArray);
                        token = new SegToken(charArray, i, j, WordType.STRING, frequency);
                        segGraph.AddToken(token);
                        i = j;
                        break;
                }
            }

            // Add two more Tokens: "beginning xx beginning"
            charArray = Utility.START_CHAR_ARRAY;
            frequency = wordDict.GetFrequency(charArray);
            token = new SegToken(charArray, -1, 0, WordType.SENTENCE_BEGIN, frequency);
            segGraph.AddToken(token);

            // "end xx end"
            charArray = Utility.END_CHAR_ARRAY;
            frequency = wordDict.GetFrequency(charArray);
            token = new SegToken(charArray, length, length + 1, WordType.SENTENCE_END,
                frequency);
            segGraph.AddToken(token);

            return segGraph;
        }

        /// <summary>
        /// Get the character types for every character in a sentence.
        /// </summary>
        /// <param name="sentence">input sentence</param>
        /// <returns>array of character types corresponding to character positions in the sentence</returns>
        /// <seealso cref="Utility.GetCharType(char)"/>
        private static CharType[] GetCharTypes(string sentence)
        {
            int length = sentence.Length;
            CharType[] charTypeArray = new CharType[length];
            // the type of each character by position
            for (int i = 0; i < length; i++)
            {
                charTypeArray[i] = Utility.GetCharType(sentence[i]);
            }

            return charTypeArray;
        }

        /// <summary>
        /// Return a list of <see cref="SegToken"/> representing the best segmentation of a sentence
        /// </summary>
        /// <param name="sentence">input sentence</param>
        /// <returns>best segmentation as a <see cref="T:IList{SegToken}"/></returns>
        public virtual IList<SegToken> Process(string sentence)
        {
            SegGraph segGraph = CreateSegGraph(sentence);
            BiSegGraph biSegGraph = new BiSegGraph(segGraph);
            IList<SegToken> shortPath = biSegGraph.GetShortPath();
            return shortPath;
        }
    }
}
