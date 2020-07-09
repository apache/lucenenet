namespace Lucene.Net.Index
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

    // javadocs
    using AttributeSource = Lucene.Net.Util.AttributeSource;

    /// <summary>
    /// This class tracks the number and position / offset parameters of terms
    /// being added to the index. The information collected in this class is
    /// also used to calculate the normalization factor for a field.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class FieldInvertState
    {
        // LUCENENET specific - made fields private and added internal setters so they can be set
        private string name;
        private int position;
        private int length;
        private int numOverlap;
        private int offset;
        private int maxTermFrequency;
        private int uniqueTermCount;
        private float boost;
        private AttributeSource attributeSource;

        /// <summary>
        /// Creates <see cref="FieldInvertState"/> for the specified
        /// field name.
        /// </summary>
        public FieldInvertState(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Creates <see cref="FieldInvertState"/> for the specified
        /// field name and values for all fields.
        /// </summary>
        public FieldInvertState(string name, int position, int length, int numOverlap, int offset, float boost)
        {
            this.name = name;
            this.position = position;
            this.length = length;
            this.numOverlap = numOverlap;
            this.offset = offset;
            this.boost = boost;
        }

        /// <summary>
        /// Re-initialize the state
        /// </summary>
        internal void Reset()
        {
            position = 0;
            length = 0;
            numOverlap = 0;
            offset = 0;
            maxTermFrequency = 0;
            uniqueTermCount = 0;
            boost = 1.0f;
            attributeSource = null;
        }

        /// <summary>
        /// Gets the last processed term position. </summary>
        /// <returns> the position </returns>
        public int Position
        {
            get => position;
            internal set => position = value; // LUCENENET specific to protect private field
        }

        /// <summary>
        /// Gets or Sets total number of terms in this field. </summary>
        /// <returns> the length </returns>
        public int Length
        {
            get => length;
            set => this.length = value;
        }

        /// <summary>
        /// Gets or Sets the number of terms with <c>positionIncrement == 0</c>. </summary>
        /// <returns> the numOverlap </returns>
        public int NumOverlap
        {
            get => numOverlap;
            set => this.numOverlap = value;
        }

        /// <summary>
        /// Gets end offset of the last processed term. </summary>
        /// <returns> the offset </returns>
        public int Offset
        {
            get => offset;
            internal set => offset = value; // LUCENENET specific to protect private field
        }

        /// <summary>
        /// Gets or Sets boost value. This is the cumulative product of
        /// document boost and field boost for all field instances
        /// sharing the same field name. </summary>
        /// <returns> the boost </returns>
        public float Boost
        {
            get => boost;
            set => this.boost = value;
        }

        /// <summary>
        /// Get the maximum term-frequency encountered for any term in the field.  A
        /// field containing "the quick brown fox jumps over the lazy dog" would have
        /// a value of 2, because "the" appears twice.
        /// </summary>
        public int MaxTermFrequency
        {
            get => maxTermFrequency;
            internal set => maxTermFrequency = value; // LUCENENET specific to protect private field
        }

        /// <summary>
        /// Gets the number of unique terms encountered in this field.
        /// </summary>
        public int UniqueTermCount
        {
            get => uniqueTermCount;
            internal set => uniqueTermCount = value; // LUCENENET specific to protect private field
        }

        /// <summary>
        /// Gets the <see cref="Util.AttributeSource"/> from the
        /// <see cref="Analysis.TokenStream"/> that provided the indexed tokens for this
        /// field.
        /// </summary>
        public AttributeSource AttributeSource
        {
            get => attributeSource;
            internal set => attributeSource = value; // LUCENENET specific to protect private field
        }

        /// <summary>
        /// Gets the field's name
        /// </summary>
        public string Name
        {
            get => name;
            internal set => name = value; // LUCENENET specific to protect private field
        }
    }
}