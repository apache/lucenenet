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

namespace Lucene.Net.Index
{
    // javadocs
    using AttributeSource = Lucene.Net.Util.AttributeSource;

    /// <summary>
    /// this class tracks the number and position / offset parameters of terms
    /// being added to the index. The information collected in this class is
    /// also used to calculate the normalization factor for a field.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class FieldInvertState
    {
        internal string Name_Renamed;
        internal int Position_Renamed;
        internal int Length_Renamed;
        internal int NumOverlap_Renamed;
        internal int Offset_Renamed;
        internal int MaxTermFrequency_Renamed;
        internal int UniqueTermCount_Renamed;
        internal float Boost_Renamed;
        internal AttributeSource AttributeSource_Renamed;

        /// <summary>
        /// Creates {code FieldInvertState} for the specified
        ///  field name.
        /// </summary>
        public FieldInvertState(string name)
        {
            this.Name_Renamed = name;
        }

        /// <summary>
        /// Creates {code FieldInvertState} for the specified
        ///  field name and values for all fields.
        /// </summary>
        public FieldInvertState(string name, int position, int length, int numOverlap, int offset, float boost)
        {
            this.Name_Renamed = name;
            this.Position_Renamed = position;
            this.Length_Renamed = length;
            this.NumOverlap_Renamed = numOverlap;
            this.Offset_Renamed = offset;
            this.Boost_Renamed = boost;
        }

        /// <summary>
        /// Re-initialize the state
        /// </summary>
        internal void Reset()
        {
            Position_Renamed = 0;
            Length_Renamed = 0;
            NumOverlap_Renamed = 0;
            Offset_Renamed = 0;
            MaxTermFrequency_Renamed = 0;
            UniqueTermCount_Renamed = 0;
            Boost_Renamed = 1.0f;
            AttributeSource_Renamed = null;
        }

        /// <summary>
        /// Get the last processed term position. </summary>
        /// <returns> the position </returns>
        public int Position
        {
            get
            {
                return Position_Renamed;
            }
        }

        /// <summary>
        /// Get total number of terms in this field. </summary>
        /// <returns> the length </returns>
        public int Length
        {
            get
            {
                return Length_Renamed;
            }
            set
            {
                this.Length_Renamed = value;
            }
        }

        /// <summary>
        /// Get the number of terms with <code>positionIncrement == 0</code>. </summary>
        /// <returns> the numOverlap </returns>
        public int NumOverlap
        {
            get
            {
                return NumOverlap_Renamed;
            }
            set
            {
                this.NumOverlap_Renamed = value;
            }
        }

        /// <summary>
        /// Get end offset of the last processed term. </summary>
        /// <returns> the offset </returns>
        public int Offset
        {
            get
            {
                return Offset_Renamed;
            }
        }

        /// <summary>
        /// Get boost value. this is the cumulative product of
        /// document boost and field boost for all field instances
        /// sharing the same field name. </summary>
        /// <returns> the boost </returns>
        public float Boost
        {
            get
            {
                return Boost_Renamed;
            }
            set
            {
                this.Boost_Renamed = value;
            }
        }

        /// <summary>
        /// Get the maximum term-frequency encountered for any term in the field.  A
        /// field containing "the quick brown fox jumps over the lazy dog" would have
        /// a value of 2, because "the" appears twice.
        /// </summary>
        public int MaxTermFrequency
        {
            get
            {
                return MaxTermFrequency_Renamed;
            }
        }

        /// <summary>
        /// Return the number of unique terms encountered in this field.
        /// </summary>
        public int UniqueTermCount
        {
            get
            {
                return UniqueTermCount_Renamed;
            }
        }

        /// <summary>
        /// Returns the <seealso cref="AttributeSource"/> from the {@link
        ///  TokenStream} that provided the indexed tokens for this
        ///  field.
        /// </summary>
        public AttributeSource AttributeSource
        {
            get
            {
                return AttributeSource_Renamed;
            }
        }

        /// <summary>
        /// Return the field's name
        /// </summary>
        public string Name
        {
            get
            {
                return Name_Renamed;
            }
        }
    }
}