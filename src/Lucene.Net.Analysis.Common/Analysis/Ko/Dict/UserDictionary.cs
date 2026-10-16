using J2N.Text;
using Lucene.Net.Analysis.Ko;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Analysis.Ko.Dict
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

        private readonly short[] rightIds;

        public const int WORD_COST = -100000;

        public const int LEFT_ID = 1781;

        public const int RIGHT_ID = 3533;

        public const int RIGHT_ID_T = 3535;

        public const int RIGHT_ID_F = 3534;

        private static readonly Regex specialChars = new Regex(@"#.*$", RegexOptions.Compiled);

        public UserDictionary(TextReader reader)
        {
            string line = null;
            List<string> entries = new();

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
                entries.Add(line);
            }
            CharacterDefinition charDef = CharacterDefinition.Instance;
            entries.OrderBy(x => Regex.Split(x, "\\s+")[0]).ToList();

            PositiveInt32Outputs fstOutput = PositiveInt32Outputs.Singleton;
            Builder<Int64> fstBuilder = new (Lucene.Net.Util.Fst.FST.INPUT_TYPE.BYTE2, fstOutput);
            Int32sRef scratch = new Int32sRef();

            string lastToken = null;
            List<int[]> segmentations = new();
            List<short> rightIds = new();
            long ord = 0;

            foreach (string value in entries)
            {
                string[] splits = Regex.Split(value, "\\s+");
                string token = splits[0];

                if (token == lastToken) {
                    continue;
                }

                char lastChar = value[-1];
                if (charDef.IsHangul(lastChar)) {
                    if (charDef.HasCoda(lastChar)) {
                        rightIds.Add(RIGHT_ID_T);
                    } else {
                        rightIds.Add(RIGHT_ID_F);
                    }
                } else {
                    rightIds.Add(RIGHT_ID);
                }

                if (splits.Length == 1) {
                    segmentations.Add(null);
                } else {
                    int[] length = new int[splits.Length - 1];
                    int offset = 0;
                    for (int i = 1; i < splits.Length; i++)
                    {
                        length[i - 1] = splits[i].Length;
                        offset += splits[i].Length;
                    }
                    if (offset > token.Length)
                    {
                        throw RuntimeException.Create("Illegal user dictionary entry " + value +
                                                      " - the segmentation is bigger than the surface form (" + token + ")");
                    }
                    segmentations.Add(length);
                }

                // add mapping to FST
                scratch.Grow(token.Length);
                scratch.Length = token.Length;
                for (int i = 0; i < token.Length; i++)
                {
                    scratch.Int32s[i] = (int)token[i];
                }
                fstBuilder.Add(scratch, ord);
                lastToken = token;
                ord++;
            }
            this.fst = new TokenInfoFST(fstBuilder.Finish(), false);
            this.segmentations = segmentations.ToArray(/*new string[data.Count]*/);
            this.rightIds = new short[rightIds.Count];
            for (int i = 0; i < rightIds.Count; i++) {
                this.rightIds[i] = rightIds[i];
            }
        }

        public TokenInfoFST GetFST() { return fst;}

        public int GetLeftId(int wordId) { return LEFT_ID;}

        public int GetRightId(int wordId) { return rightIds[wordId]; }

        public int GetWordCost(int wordId) { return WORD_COST; }

        public POS.Type GetPOSType(int wordId)
        {
            if (segmentations[wordId] == null)
            {
                return POS.Type.MORPHEME;
            }
            return POS.Type.COMPOUND;
        }

        public POS.Tag GetLeftPOS(int wordId) { return POS.Tags["NNG"]; }

        public POS.Tag GetRightPOS(int wordId) { return POS.Tags["NNG"]; }

        public string GetReading(int wordId) { return null; }

        public TokenInfoFST FST => fst;

        public IDictionary.Morpheme[] GetMorphemes(int wordId, char[] surfaceForm, int off, int len)
        {
            int[] segs = segmentations[wordId];
            if (segs is null)
            {
                return null;
            }

            int offset = 0;
            IDictionary.Morpheme[] morphemes = new IDictionary.Morpheme[segs.Length];
            for (int i = 0; i < segs.Length; i++)
            {
                morphemes[i] = new IDictionary.Morpheme(POS.Tags["NNG"], new string(surfaceForm, off+offset, segs[i]).ToCharArray());
                offset += segs[i];
            }

            return morphemes;
        }

        /// <summary>
        /// Lookup words in text.
        /// </summary>
        /// <param name="chars">Text.</param>
        /// <param name="off">Offset into text.</param>
        /// <param name="len">Length of text.</param>
        /// <returns>Array of {wordId, position, length}.</returns>
        public List<int> Lookup(char[] chars, int off, int len)
        {
            List<int> result = new();
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
                        result.Add(finalOutput);
                    }
                }
            }

            return result;
        }
    }
}
