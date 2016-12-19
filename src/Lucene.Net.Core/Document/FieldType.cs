using System;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Util;

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
    public class FieldType : IndexableFieldType
    {
        /// <summary>
        /// Data type of the numeric value
        /// @since 3.2
        /// </summary>
        // LUCENENET TODO: Add a NOT_SET = 0 state so we ca get rid of nullables?
        public enum NumericType // LUCENENET TODO: Move outside of FieldType class
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

        private bool Indexed_Renamed;
        private bool Stored_Renamed;
        private bool Tokenized_Renamed = true;
        private bool StoreTermVectors_Renamed;
        private bool StoreTermVectorOffsets_Renamed;
        private bool StoreTermVectorPositions_Renamed;
        private bool StoreTermVectorPayloads_Renamed;
        private bool OmitNorms_Renamed;
        private FieldInfo.IndexOptions? _indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
        private NumericType? numericType;
        private bool Frozen;
        private int NumericPrecisionStep_Renamed = NumericUtils.PRECISION_STEP_DEFAULT;
        private FieldInfo.DocValuesType_e? docValueType;

        /// <summary>
        /// Create a new mutable FieldType with all of the properties from <code>ref</code>
        /// </summary>
        public FieldType(FieldType @ref)
        {
            this.Indexed_Renamed = @ref.Indexed;
            this.Stored_Renamed = @ref.Stored;
            this.Tokenized_Renamed = @ref.Tokenized;
            this.StoreTermVectors_Renamed = @ref.StoreTermVectors;
            this.StoreTermVectorOffsets_Renamed = @ref.StoreTermVectorOffsets;
            this.StoreTermVectorPositions_Renamed = @ref.StoreTermVectorPositions;
            this.StoreTermVectorPayloads_Renamed = @ref.StoreTermVectorPayloads;
            this.OmitNorms_Renamed = @ref.OmitNorms;
            this._indexOptions = @ref.IndexOptions;
            this.docValueType = @ref.DocValueType;
            this.numericType = @ref.NumericTypeValue;
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
        public virtual bool Indexed
        {
            get { return this.Indexed_Renamed; }
            set
            {
                CheckIfFrozen();
                this.Indexed_Renamed = value;
            }
        }

        /// <summary>
        /// Set to <code>true</code> to store this field. </summary>
        /// <param name="value"> true if this field should be stored. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #stored() </seealso>
        public virtual bool Stored
        {
            get
            {
                return this.Stored_Renamed;
            }
            set
            {
                CheckIfFrozen();
                this.Stored_Renamed = value;
            }
        }

        /// <summary>
        /// Set to <code>true</code> to tokenize this field's contents via the
        /// configured <seealso cref="Analyzer"/>. </summary>
        /// <param name="value"> true if this field should be tokenized. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #tokenized() </seealso>
        public virtual bool Tokenized
        {
            get
            {
                return this.Tokenized_Renamed;
            }
            set
            {
                CheckIfFrozen();
                this.Tokenized_Renamed = value;
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
            get { return this.StoreTermVectors_Renamed; }

            set
            {
                CheckIfFrozen();
                this.StoreTermVectors_Renamed = value;
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
                return this.StoreTermVectorOffsets_Renamed;
            }
            set
            {
                CheckIfFrozen();
                this.StoreTermVectorOffsets_Renamed = value;
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
                return this.StoreTermVectorPositions_Renamed;
            }
            set
            {
                CheckIfFrozen();
                this.StoreTermVectorPositions_Renamed = value;
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
                return this.StoreTermVectorPayloads_Renamed;
            }
            set
            {
                CheckIfFrozen();
                this.StoreTermVectorPayloads_Renamed = value;
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
            get { return this.OmitNorms_Renamed; }
            set
            {
                CheckIfFrozen();
                this.OmitNorms_Renamed = value;
            }
        }

        /// <summary>
        /// Sets the indexing options for the field: </summary>
        /// <param name="value"> indexing options </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #indexOptions() </seealso>
        public virtual FieldInfo.IndexOptions? IndexOptions
        {
            get
            {
                return this._indexOptions;
            }
            set
            {
                CheckIfFrozen();
                this._indexOptions = value;
            }
        }

        /// <summary>
        /// Specifies the field's numeric type. </summary>
        /// <param name="type"> numeric type, or null if the field has no numeric type. </param>
        /// <exception cref="InvalidOperationException"> if this FieldType is frozen against
        ///         future modifications. </exception>
        /// <seealso cref= #numericType() </seealso>
        public virtual NumericType? NumericTypeValue // LUCENENET TODO: Rename back to NumericType (de-nest enum)
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
                this.NumericPrecisionStep_Renamed = value;
            }
            get
            {
                return NumericPrecisionStep_Renamed;
            }
        }

        /// <summary>
        /// Prints a Field for human consumption. </summary>
        public override sealed string ToString()
        {
            var result = new StringBuilder();
            if (Stored)
            {
                result.Append("stored");
            }
            if (Indexed)
            {
                if (result.Length > 0)
                {
                    result.Append(",");
                }
                result.Append("indexed");
                if (Tokenized)
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
                if (_indexOptions != FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    result.Append(",indexOptions=");
                    result.Append(_indexOptions);
                }
                if (numericType != null)
                {
                    result.Append(",numericType=");
                    result.Append(numericType);
                    result.Append(",numericPrecisionStep=");
                    result.Append(NumericPrecisionStep_Renamed);
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
        public virtual FieldInfo.DocValuesType_e? DocValueType
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
}