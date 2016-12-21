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
        private IndexOptions? indexOptions = Index.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
        private NumericType? numericType;
        private bool Frozen;
        private int numericPrecisionStep = NumericUtils.PRECISION_STEP_DEFAULT;
        private DocValuesType? docValueType;

        /// <summary>
        /// Create a new mutable FieldType with all of the properties from <code>ref</code>
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
        /// Create a new FieldType with default properties.
        /// </summary>
        public FieldType()
        {
        }

        private void CheckIfFrozen()
        {
            if (Frozen)
            {
                throw new Exception("this FieldType is already frozen and cannot be changed");
            }
        }

        /// <summary>
        /// Prevents future changes. Note, it is recommended that this is called once
        /// the FieldTypes's properties have been set, to prevent unintentional state
        /// changes.
        /// </summary>
        public virtual void Freeze()
        {
            this.Frozen = true;
        }

        /// <summary>
        /// Set to <code>true</code> to index (invert) this field. </summary>
        /// <param name="value"> true if this field should be indexed. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #indexed() </seealso>
        public virtual bool IsIndexed
        {
            get { return this.indexed; }
            set
            {
                CheckIfFrozen();
                this.indexed = value;
            }
        }

        /// <summary>
        /// Set to <code>true</code> to store this field. </summary>
        /// <param name="value"> true if this field should be stored. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #stored() </seealso>
        public virtual bool IsStored
        {
            get
            {
                return this.stored;
            }
            set
            {
                CheckIfFrozen();
                this.stored = value;
            }
        }

        /// <summary>
        /// Set to <code>true</code> to tokenize this field's contents via the
        /// configured <seealso cref="Analyzer"/>. </summary>
        /// <param name="value"> true if this field should be tokenized. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #tokenized() </seealso>
        public virtual bool IsTokenized
        {
            get
            {
                return this.tokenized;
            }
            set
            {
                CheckIfFrozen();
                this.tokenized = value;
            }
        }

        /// <summary>
        /// Set to <code>true</code> if this field's indexed form should be also stored
        /// into term vectors. </summary>
        /// <param name="value"> true if this field should store term vectors. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #storeTermVectors() </seealso>
        public virtual bool StoreTermVectors
        {
            get { return this.storeTermVectors; }

            set
            {
                CheckIfFrozen();
                this.storeTermVectors = value;
            }
        }

        /// <summary>
        /// Set to <code>true</code> to also store token character offsets into the term
        /// vector for this field. </summary>
        /// <param name="value"> true if this field should store term vector offsets. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #storeTermVectorOffsets() </seealso>
        public virtual bool StoreTermVectorOffsets
        {
            get
            {
                return this.storeTermVectorOffsets;
            }
            set
            {
                CheckIfFrozen();
                this.storeTermVectorOffsets = value;
            }
        }

        /// <summary>
        /// Set to <code>true</code> to also store token positions into the term
        /// vector for this field. </summary>
        /// <param name="value"> true if this field should store term vector positions. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #storeTermVectorPositions() </seealso>
        public virtual bool StoreTermVectorPositions
        {
            get
            {
                return this.storeTermVectorPositions;
            }
            set
            {
                CheckIfFrozen();
                this.storeTermVectorPositions = value;
            }
        }

        /// <summary>
        /// Set to <code>true</code> to also store token payloads into the term
        /// vector for this field. </summary>
        /// <param name="value"> true if this field should store term vector payloads. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #storeTermVectorPayloads() </seealso>
        public virtual bool StoreTermVectorPayloads
        {
            get
            {
                return this.storeTermVectorPayloads;
            }
            set
            {
                CheckIfFrozen();
                this.storeTermVectorPayloads = value;
            }
        }

        /// <summary>
        /// Set to <code>true</code> to omit normalization values for the field. </summary>
        /// <param name="value"> true if this field should omit norms. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #omitNorms() </seealso>
        public virtual bool OmitNorms
        {
            get { return this.omitNorms; }
            set
            {
                CheckIfFrozen();
                this.omitNorms = value;
            }
        }

        /// <summary>
        /// Sets the indexing options for the field: </summary>
        /// <param name="value"> indexing options </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #indexOptions() </seealso>
        // LUCENENET TODO: Can we remove the nullable here?
        public virtual IndexOptions? IndexOptions
        {
            get
            {
                return this.indexOptions;
            }
            set
            {
                CheckIfFrozen();
                this.indexOptions = value;
            }
        }

        /// <summary>
        /// Specifies the field's numeric type. </summary>
        /// <param name="type"> numeric type, or null if the field has no numeric type. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #numericType() </seealso>
        // LUCENENET TODO: Can we remove the nullable here?
        public virtual NumericType? NumericType
        {
            get
            {
                return this.numericType;
            }
            set
            {
                CheckIfFrozen();
                numericType = value;
            }
        }

        /// <summary>
        /// Sets the numeric precision step for the field. </summary>
        /// <param name="precisionStep"> numeric precision step for the field </param>
        /// <exception cref="ArgumentException"> if precisionStep is less than 1. </exception>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #numericPrecisionStep() </seealso>
        public virtual int NumericPrecisionStep
        {
            set
            {
                CheckIfFrozen();
                if (value < 1)
                {
                    throw new System.ArgumentException("precisionStep must be >= 1 (got " + value + ")");
                }
                this.numericPrecisionStep = value;
            }
            get
            {
                return numericPrecisionStep;
            }
        }

        /// <summary>
        /// Prints a Field for human consumption. </summary>
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
                    result.Append(",");
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
                if (indexOptions != Index.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    result.Append(",indexOptions=");
                    result.Append(indexOptions);
                }
                if (numericType != null)
                {
                    result.Append(",numericType=");
                    result.Append(numericType);
                    result.Append(",numericPrecisionStep=");
                    result.Append(numericPrecisionStep);
                }
            }
            if (docValueType != null)
            {
                if (result.Length > 0)
                {
                    result.Append(",");
                }
                result.Append("docValueType=");
                result.Append(docValueType);
            }

            return result.ToString();
        }

        // LUCENENET TODO: Cleanup
        /// <summary>
        /// {@inheritDoc}
        /// <p>
        /// The default is <code>null</code> (no docValues) </summary>
        /// <seealso cref= #setDocValueType(Lucene.Net.Index.FieldInfo.DocValuesType) </seealso>
        /*public override DocValuesType DocValueType()
        {
          return DocValueType_Renamed;
        }*/

        /// <summary>
        /// Set's the field's DocValuesType </summary>
        /// <param name="type"> DocValues type, or null if no DocValues should be stored. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #docValueType() </seealso>
        public virtual DocValuesType? DocValueType
        {
            get
            {
                return docValueType;
            }

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
    // LUCENENET TODO: Add a NOT_SET = 0 state so we ca get rid of nullables?
    public enum NumericType
    {
        /// <summary>
        /// 32-bit integer numeric type </summary>
        INT,

        /// <summary>
        /// 64-bit long numeric type </summary>
        LONG,

        /// <summary>
        /// 32-bit float numeric type </summary>
        FLOAT,

        /// <summary>
        /// 64-bit double numeric type </summary>
        DOUBLE
    }
}