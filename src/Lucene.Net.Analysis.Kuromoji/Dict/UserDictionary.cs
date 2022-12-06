using J2N.Text;
using Lucene.Net.Analysis.Ja.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Analysis.Ja.Dict
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
    /// Class for building a User Dictionary.
    /// This class allows for custom segmentation of phrases.
    /// </summary>
    public sealed class UserDictionary : IDictionary
    {
        // phrase text -> phrase ID
        private readonly TokenInfoFST fst;

        // holds wordid, length, length... indexed by phrase ID
        private readonly int[][] segmentations;

        // holds readings and POS, indexed by wordid
        private readonly string[] data;

        private const int CUSTOM_DICTIONARY_WORD_ID_OFFSET = 100000000;

        public const int WORD_COST = -100000;

        public const int LEFT_ID = 5;

        public const int RIGHT_ID = 5;

        private static readonly Regex specialChars = new Regex(@"#.*$", RegexOptions.Compiled);
        private static readonly Regex commentLine = new Regex(@"  *", RegexOptions.Compiled);

        public UserDictionary(TextReader reader)
        {
            string line = null;
            int wordId = CUSTOM_DICTIONARY_WORD_ID_OFFSET;
            JCG.List<string[]> featureEntries = new JCG.List<string[]>();

            // text, segmentation, readings, POS
            while ((line = reader.ReadLine()) != null)
            {
                // Remove comments
                line = specialChars.Replace(line, "");

                // Skip empty lines or comment lines
                if (line.Trim().Length == 0)
                {
                    continue;
                }
                string[] values = CSVUtil.Parse(line);
                featureEntries.Add(values);
            }

            // TODO: should we allow multiple segmentations per input 'phrase'?
            // the old treemap didn't support this either, and i'm not sure if its needed/useful?
            featureEntries.Sort(Comparer<string[]>.Create((left, right) => left[0].CompareToOrdinal(right[0])));

            JCG.List<string> data = new JCG.List<string>(featureEntries.Count);
            JCG.List<int[]> segmentations = new JCG.List<int[]>(featureEntries.Count);

            PositiveInt32Outputs fstOutput = PositiveInt32Outputs.Singleton;
            Builder<Int64> fstBuilder = new Builder<Int64>(Lucene.Net.Util.Fst.FST.INPUT_TYPE.BYTE2, fstOutput);
            Int32sRef scratch = new Int32sRef();
            long ord = 0;

            foreach (string[] values in featureEntries)
            {
                string[] segmentation = commentLine.Replace(values[1], " ").Split(' ').TrimEnd();
                string[] readings = commentLine.Replace(values[2], " ").Split(' ').TrimEnd();
                string pos = values[3];

                if (segmentation.Length != readings.Length)
                {
                    throw RuntimeException.Create("Illegal user dictionary entry " + values[0] +
                                               " - the number of segmentations (" + segmentation.Length + ")" +
                                               " does not the match number of readings (" + readings.Length + ")");
                }

                int[] wordIdAndLength = new int[segmentation.Length + 1]; // wordId offset, length, length....
                wordIdAndLength[0] = wordId;
                for (int i = 0; i < segmentation.Length; i++)
                {
                    wordIdAndLength[i + 1] = segmentation[i].Length;
                    data.Add(readings[i] + Dictionary.INTERNAL_SEPARATOR + pos);
                    wordId++;
                }
                // add mapping to FST
                string token = values[0];
                scratch.Grow(token.Length);
                scratch.Length = token.Length;
                for (int i = 0; i < token.Length; i++)
                {
                    scratch.Int32s[i] = (int)token[i];
                }
                fstBuilder.Add(scratch, ord);
                segmentations.Add(wordIdAndLength);
                ord++;
            }
            this.fst = new TokenInfoFST(fstBuilder.Finish(), false);
            this.data = data.ToArray(/*new string[data.Count]*/);
            this.segmentations = segmentations.ToArray(/*new int[segmentations.Count][]*/);
        }
        
        /// <summary>
        /// Lookup words in text.
        /// </summary>
        /// <param name="chars">Text.</param>
        /// <param name="off">Offset into text.</param>
        /// <param name="len">Length of text.</param>
        /// <returns>Array of {wordId, position, length}.</returns>
        public int[][] Lookup(char[] chars, int off, int len)
        {
            // TODO: can we avoid this treemap/toIndexArray?
            IDictionary<int, int[]> result = new JCG.SortedDictionary<int, int[]>(); // index, [length, length...]
            bool found = false; // true if we found any results

            FST.BytesReader fstReader = fst.GetBytesReader();

            FST.Arc<Int64> arc = new FST.Arc<Int64>();
            int end = off + len;
            for (int startOffset = off; startOffset < end; startOffset++)
            {
                arc = fst.GetFirstArc(arc);
                int output = 0;
                int remaining = end - startOffset;
                for (int i = 0; i < remaining; i++)
                {
                    int ch = chars[startOffset + i];
                    if (fst.FindTargetArc(ch, arc, arc, i == 0, fstReader) is null)
                    {
                        break; // continue to next position
                    }
                    output += (int)arc.Output;
                    if (arc.IsFinal)
                    {
                        int finalOutput = output + (int)arc.NextFinalOutput;
                        result[startOffset - off] = segmentations[finalOutput];
                        found = true;
                    }
                }
            }

            return found ? ToIndexArray(result) : EMPTY_RESULT;
        }

        public TokenInfoFST FST => fst;

        private static readonly int[][] EMPTY_RESULT = Arrays.Empty<int[]>();

        /// <summary>
        /// Convert Map of index and wordIdAndLength to array of {wordId, index, length}
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Array of {wordId, index, length}.</returns>
        private static int[][] ToIndexArray(IDictionary<int, int[]> input) // LUCENENET: CA1822: Mark members as static
        {
            JCG.List<int[]> result = new JCG.List<int[]>();
            foreach (int i in input.Keys)
            {
                int[] wordIdAndLength = input[i];
                int wordId = wordIdAndLength[0];
                // convert length to index
                int current = i;
                for (int j = 1; j < wordIdAndLength.Length; j++)
                { // first entry is wordId offset
                    int[] token = { wordId + j - 1, current, wordIdAndLength[j] };
                    result.Add(token);
                    current += wordIdAndLength[j];
                }
            }
            return result.ToArray(/*new int[result.size()][]*/);
        }

        public int[] LookupSegmentation(int phraseID)
        {
            return segmentations[phraseID];
        }

        public int GetLeftId(int wordId)
        {
            return LEFT_ID;
        }

        public int GetRightId(int wordId)
        {
            return RIGHT_ID;
        }

        public int GetWordCost(int wordId)
        {
            return WORD_COST;
        }

        public string GetReading(int wordId, char[] surface, int off, int len)
        {
            return GetFeature(wordId, 0);
        }

        public string GetPartOfSpeech(int wordId)
        {
            return GetFeature(wordId, 1);
        }

        public string GetBaseForm(int wordId, char[] surface, int off, int len)
        {
            return null; // TODO: add support?
        }

        public string GetPronunciation(int wordId, char[] surface, int off, int len)
        {
            return null; // TODO: add support?
        }

        public string GetInflectionType(int wordId)
        {
            return null; // TODO: add support?
        }

        public string GetInflectionForm(int wordId)
        {
            return null; // TODO: add support?
        }

        private string[] GetAllFeaturesArray(int wordId)
        {
            string allFeatures = data[wordId - CUSTOM_DICTIONARY_WORD_ID_OFFSET];
            if (allFeatures is null)
            {
                return null;
            }

            return allFeatures.Split(new string[] { Dictionary.INTERNAL_SEPARATOR }, StringSplitOptions.None).TrimEnd();
        }

        private string GetFeature(int wordId, params int[] fields)
        {
            string[] allFeatures = GetAllFeaturesArray(wordId);
            if (allFeatures is null)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder();
            if (fields.Length == 0)
            { // All features
                foreach (string feature in allFeatures)
                {
                    sb.Append(CSVUtil.QuoteEscape(feature)).Append(',');
                }
            }
            else if (fields.Length == 1)
            { // One feature doesn't need to escape value
                sb.Append(allFeatures[fields[0]]).Append(',');
            }
            else
            {
                foreach (int field in fields)
                {
                    sb.Append(CSVUtil.QuoteEscape(allFeatures[field])).Append(',');
                }
            }
            return sb.Remove(sb.Length - 1, 1).ToString();
        }
    }
}
