using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Text;

namespace Lucene.Net.Documents
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
    /// Describes the properties of a field.
    /// </summary>
    public class FieldType : IIndexableFieldType
    {
        // LUCENENET specific: Moved the NumericType enum outside of this class

        private bool indexed;
        private bool stored;
        private bool tokenized = true;
        private bool storeTermVectors;
        private bool storeTermVectorOffsets;
        private bool storeTermVectorPositions;
        private bool storeTermVectorPayloads;
        private bool omitNorms;
        private IndexOptions indexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
        private NumericType numericType;
        private bool frozen;
        private int numericPrecisionStep = NumericUtils.PRECISION_STEP_DEFAULT;
        private DocValuesType docValueType;

        /// <summary>
        /// Create a new mutable <see cref="FieldType"/> with all of the properties from <paramref name="ref"/>
        /// </summary>
        public FieldType(FieldType @ref)
        {
            this.indexed = @ref.IsIndexed;
            this.stored = @ref.IsStored;
            this.tokenized = @ref.IsTokenized;
            this.storeTermVectors = @ref.StoreTermVectors;
            this.storeTermVectorOffsets = @ref.StoreTermVectorOffsets;
            this.storeTermVectorPositions = @ref.StoreTermVectorPositions;
            this.storeTermVectorPayloads = @ref.StoreTermVectorPayloads;
            this.omitNorms = @ref.OmitNorms;
            this.indexOptions = @ref.IndexOptions;
            this.docValueType = @ref.DocValueType;
            this.numericType = @ref.NumericType;
            // Do not copy frozen!
        }

        /// <summary>
        /// Create a new <see cref="FieldType"/> with default properties.
        /// </summary>
        public FieldType()
        {
        }

        private void CheckIfFrozen()
        {
            if (frozen)
            {
                throw IllegalStateException.Create("this FieldType is already frozen and cannot be changed");
            }
        }

        /// <summary>
        /// Prevents future changes. Note, it is recommended that this is called once
        /// the <see cref="FieldType"/>'s properties have been set, to prevent unintentional state
        /// changes.
        /// </summary>
        /// <returns><c>this</c></returns>
        // LUCENENET specific - returing self to make it possible to chain this to newing up the class so we can set and freeze on a single line.
        // This is especially important for static field initializers.
        public virtual FieldType Freeze()
        {
            this.frozen = true;
            return this;
        }

        /// <summary>
        /// Set to <c>true</c> to index (invert) this field. The default is <c>false</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual bool IsIndexed
        {
            get => this.indexed;
            set
            {
                CheckIfFrozen();
                this.indexed = value;
            }
        }

        /// <summary>
        /// Set to <c>true</c> to store this field. The default is <c>false</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual bool IsStored
        {
            get => this.stored;
            set
            {
                CheckIfFrozen();
                this.stored = value;
            }
        }

        /// <summary>
        /// Set to <c>true</c> to tokenize this field's contents via the
        /// configured <see cref="Analysis.Analyzer"/>. The default is <c>true</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual bool IsTokenized
        {
            get => this.tokenized;
            set
            {
                CheckIfFrozen();
                this.tokenized = value;
            }
        }

        /// <summary>
        /// Set to <c>true</c> if this field's indexed form should be also stored
        /// into term vectors. The default is <c>false</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual bool StoreTermVectors
        {
            get => this.storeTermVectors;

            set
            {
                CheckIfFrozen();
                this.storeTermVectors = value;
            }
        }

        /// <summary>
        /// Set to <c>true</c> to also store token character offsets into the term
        /// vector for this field. The default is <c>false</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual bool StoreTermVectorOffsets
        {
            get => this.storeTermVectorOffsets;
            set
            {
                CheckIfFrozen();
                this.storeTermVectorOffsets = value;
            }
        }

        /// <summary>
        /// Set to <c>true</c> to also store token positions into the term
        /// vector for this field. The default is <c>false</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual bool StoreTermVectorPositions
        {
            get => this.storeTermVectorPositions;
            set
            {
                CheckIfFrozen();
                this.storeTermVectorPositions = value;
            }
        }

        /// <summary>
        /// Set to <c>true</c> to also store token payloads into the term
        /// vector for this field. The default is <c>false</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual bool StoreTermVectorPayloads
        {
            get => this.storeTermVectorPayloads;
            set
            {
                CheckIfFrozen();
                this.storeTermVectorPayloads = value;
            }
        }

        /// <summary>
        /// Set to <c>true</c> to omit normalization values for the field. The default is <c>false</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual bool OmitNorms
        {
            get => this.omitNorms;
            set
            {
                CheckIfFrozen();
                this.omitNorms = value;
            }
        }

        /// <summary>
        /// Sets the indexing options for the field. 
        /// <para/>
        /// The default is <see cref="IndexOptions.DOCS_AND_FREQS_AND_POSITIONS"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual IndexOptions IndexOptions
        {
            get => this.indexOptions;
            set
            {
                CheckIfFrozen();
                this.indexOptions = value;
            }
        }

        /// <summary>
        /// Specifies the field's numeric type, or set to <see cref="NumericType.NONE"/> if the field has no numeric type.
        /// If not <see cref="NumericType.NONE"/> then the field's value will be indexed numerically so that 
        /// <see cref="Search.NumericRangeQuery"/> can be used at search time.
        /// <para/>
        /// The default is <see cref="NumericType.NONE"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual NumericType NumericType
        {
            get => this.numericType;
            set
            {
                CheckIfFrozen();
                numericType = value;
            }
        }

        /// <summary>
        /// Sets the numeric precision step for the field.
        /// <para/>
        /// This has no effect if <see cref="NumericType"/> is <see cref="NumericType.NONE"/>.
        /// <para/>
        /// The default is <see cref="NumericUtils.PRECISION_STEP_DEFAULT"/>.
        /// </summary>
        /// <exception cref="ArgumentException"> if precisionStep is less than 1. </exception>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual int NumericPrecisionStep
        {
            get => numericPrecisionStep;
            set
            {
                CheckIfFrozen();
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(NumericPrecisionStep), "precisionStep must be >= 1 (got " + value + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                this.numericPrecisionStep = value;
            }
        }

        /// <summary>
        /// Prints a <see cref="FieldType"/> for human consumption. </summary>
        public override sealed string ToString()
        {
            var result = new StringBuilder();
            if (IsStored)
            {
                result.Append("stored");
            }
            if (IsIndexed)
            {
                if (result.Length > 0)
                {
                    result.Append(',');
                }
                result.Append("indexed");
                if (IsTokenized)
                {
                    result.Append(",tokenized");
                }
                if (StoreTermVectors)
                {
                    result.Append(",termVector");
                }
                if (StoreTermVectorOffsets)
                {
                    result.Append(",termVectorOffsets");
                }
                if (StoreTermVectorPositions)
                {
                    result.Append(",termVectorPosition");
                    if (StoreTermVectorPayloads)
                    {
                        result.Append(",termVectorPayloads");
                    }
                }
                if (OmitNorms)
                {
                    result.Append(",omitNorms");
                }
                if (indexOptions != IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    result.Append(",indexOptions=");
                    // LUCENENET: duplcate what would happen if you print a null indexOptions in Java
                    result.Append(indexOptions != IndexOptions.NONE ? indexOptions.ToString() : string.Empty);
                }
                if (numericType != NumericType.NONE)
                {
                    result.Append(",numericType=");
                    result.Append(numericType);
                    result.Append(",numericPrecisionStep=");
                    result.Append(numericPrecisionStep);
                }
            }
            if (docValueType != DocValuesType.NONE)
            {
                if (result.Length > 0)
                {
                    result.Append(',');
                }
                result.Append("docValueType=");
                result.Append(docValueType);
            }

            return result.ToString();
        }

        /// <summary>
        /// Sets the field's <see cref="DocValuesType"/>, or set to <see cref="DocValuesType.NONE"/> if no <see cref="DocValues"/> should be stored.
        /// <para/>
        /// The default is <see cref="DocValuesType.NONE"/> (no <see cref="DocValues"/>).
        /// </summary>
        /// <exception cref="InvalidOperationException"> if this <see cref="FieldType"/> is frozen against
        ///         future modifications. </exception>
        public virtual DocValuesType DocValueType
        {
            get => docValueType;

            set
            {
                CheckIfFrozen();
                docValueType = value;
            }
        }
    }

    /// <summary>
    /// Data type of the numeric value
    /// @since 3.2
    /// </summary>
    public enum NumericType
    {
        /// <summary>
        /// No numeric type will be used.
        /// <para/>
        /// NOTE: This is the same as setting to <c>null</c> in Lucene
        /// </summary>
        // LUCENENET specific
        NONE,

        /// <summary>
        /// 32-bit integer numeric type
        /// <para/>
        /// NOTE: This was INT in Lucene
        /// </summary>
        INT32,

        /// <summary>
        /// 64-bit long numeric type
        /// <para/>
        /// NOTE: This was LONG in Lucene
        /// </summary>
        INT64,

        /// <summary>
        /// 32-bit float numeric type
        /// <para/>
        /// NOTE: This was FLOAT in Lucene
        /// </summary>
        SINGLE,

        /// <summary>
        /// 64-bit double numeric type </summary>
        DOUBLE
    }
}
