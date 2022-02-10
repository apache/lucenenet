// lucene version compatibility level: 4.8.1
using J2N.Numerics;
using Lucene.Net.Support;

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
    /// A pair of tokens in <see cref="SegGraph"/>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal class SegTokenPair
    {
        [WritableArray]
        public char[] CharArray { get; set; }

        /// <summary>
        /// index of the first token in <see cref="SegGraph"/>
        /// </summary>
        public int From { get; set; }

        /// <summary>
        /// index of the second token in <see cref="SegGraph"/>
        /// </summary>
        public int To { get; set; }

        public double Weight { get; set; }

        public SegTokenPair(char[] idArray, int from, int to, double weight)
        {
            this.CharArray = idArray;
            this.From = from;
            this.To = to;
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
            result = prime * result + From;
            result = prime * result + To;
            long temp;
            temp = J2N.BitConversion.DoubleToInt64Bits(Weight);
            result = prime * result + (int)(temp ^ temp.TripleShift(32));
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
            SegTokenPair other = (SegTokenPair)obj;
            if (!Arrays.Equals(CharArray, other.CharArray))
                return false;
            if (From != other.From)
                return false;
            if (To != other.To)
                return false;
            if (J2N.BitConversion.DoubleToInt64Bits(Weight) != J2N.BitConversion
                .DoubleToInt64Bits(other.Weight))
                return false;
            return true;
        }
    }
}
