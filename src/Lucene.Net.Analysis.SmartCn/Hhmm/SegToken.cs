// lucene version compatibility level: 4.8.1
using Lucene.Net.Support;
using System.Diagnostics.CodeAnalysis;

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
    /// SmartChineseAnalyzer internal token
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{new string(CharArray)}")]
    public class SegToken
    {
        /// <summary>
        /// Character array containing token text
        /// </summary>
        [WritableArray]
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Lucene's design requires some array properties")]
        public char[] CharArray { get; set; }

        /// <summary>
        /// start offset into original sentence
        /// </summary>
        public int StartOffset { get; set; }

        /// <summary>
        /// end offset into original sentence
        /// </summary>
        public int EndOffset { get; set; }

        /// <summary>
        /// <see cref="Smart.WordType"/> of the text
        /// </summary>
        public WordType WordType { get; set; }

        /// <summary>
        /// word frequency
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// during segmentation, this is used to store the index of the token in the token list table
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Create a new <see cref="SegToken"/> from a character array.
        /// </summary>
        /// <param name="idArray">character array containing text</param>
        /// <param name="start">start offset of <see cref="SegToken"/> in original sentence</param>
        /// <param name="end">end offset of <see cref="SegToken"/> in original sentence</param>
        /// <param name="wordType"><see cref="Smart.WordType"/> of the text</param>
        /// <param name="weight">word frequency</param>
        public SegToken(char[] idArray, int start, int end, WordType wordType, int weight)
        {
            this.CharArray = idArray;
            this.StartOffset = start;
            this.EndOffset = end;
            this.WordType = wordType;
            this.Weight = weight;
        }

        /// <summary>
        /// <see cref="object.GetHashCode()"/>
        /// </summary>
        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            for (int i = 0; i < CharArray.Length; i++)
            {
                result = prime * result + CharArray[i];
            }
            result = prime * result + EndOffset;
            result = prime * result + Index;
            result = prime * result + StartOffset;
            result = prime * result + Weight;
            result = prime * result + (int)WordType;
            return result;
        }

        /// <summary>
        /// <see cref="object.Equals(object)"/>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj is null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            SegToken other = (SegToken)obj;
            if (!Arrays.Equals(CharArray, other.CharArray))
                return false;
            if (EndOffset != other.EndOffset)
                return false;
            if (Index != other.Index)
                return false;
            if (StartOffset != other.StartOffset)
                return false;
            if (Weight != other.Weight)
                return false;
            if (WordType != other.WordType)
                return false;
            return true;
        }
    }
}
