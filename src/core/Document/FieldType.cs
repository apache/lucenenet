using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Document
{

    public class FieldType : IndexableFieldType
    {

        /** Data type of the numeric value
   * @since 3.2
   */

        /**
 * Describes the properties of a field.
 */
        public enum NumericType
        {
            /** 32-bit integer numeric type */
            INT,
            /** 64-bit long numeric type */
            LONG,
            /** 32-bit float numeric type */
            FLOAT,
            /** 64-bit double numeric type */
            DOUBLE
        }

        private bool indexed;
        private bool stored;
        private bool tokenized = true;
        private bool storeTermVectors;
        private bool storeTermVectorOffsets;
        private bool storeTermVectorPositions;
        private bool storeTermVectorPayloads;
        private bool omitNorms;
        private IndexOptions indexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
        private NumericType? numericType;
        private bool frozen;
        private int numericPrecisionStep = NumericUtils.PRECISION_STEP_DEFAULT;
        private DocValuesType docValueType;

        /**
   * Create a new mutable FieldType with all of the properties from <code>ref</code>
   */

        public FieldType(FieldType refFieldType)
        {
            this.indexed = refFieldType.Indexed();
            this.stored = refFieldType.Stored();
            this.tokenized = refFieldType.Tokenized();
            this.storeTermVectors = refFieldType.StoreTermVectors();
            this.storeTermVectorOffsets = refFieldType.StoreTermVectorOffsets();
            this.storeTermVectorPositions = refFieldType.StoreTermVectorPositions();
            this.storeTermVectorPayloads = refFieldType.StoreTermVectorPayloads();
            this.omitNorms = refFieldType.OmitNorms();
            this.indexOptions = refFieldType.IndexOptions();
            this.docValueType = refFieldType.DocValueType();
            this.numericType = refFieldType.GetNumericType;
            // Do not copy frozen!
        }

        /**
   * Create a new FieldType with default properties.
   */

        public FieldType()
        {
        }

        private void CheckIfFrozen()
        {
            if (frozen)
            {
                throw new Exception("this FieldType is already frozen and cannot be changed");
            }
        }

        /**
   * Prevents future changes. Note, it is recommended that this is called once
   * the FieldTypes's properties have been set, to prevent unintentional state
   * changes.
   */

        public void Freeze()
        {
            this.frozen = true;
        }

        /**
   * {@inheritDoc}
   * <p>
   * The default is <code>false</code>.
   * @see #setIndexed(boolean)
   */

        public override bool Indexed()
        {
            return this.indexed;
        }

        /**
   * Set to <code>true</code> to index (invert) this field.
   * @param value true if this field should be indexed.
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #indexed()
   */

        public override void SetIndexed(bool value)
        {
            CheckIfFrozen();
            this.indexed = value;
        }

        /**
   * {@inheritDoc}
   * <p>
   * The default is <code>false</code>.
   * @see #setStored(boolean)
   */

        public override bool Stored()
        {
            return this.stored;
        }

        /**
   * Set to <code>true</code> to store this field.
   * @param value true if this field should be stored.
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #stored()
   */

        public override void SetStored(bool value)
        {
            CheckIfFrozen();
            this.stored = value;
        }


        /**
   * {@inheritDoc}
   * <p>
   * The default is <code>true</code>.
   * @see #setTokenized(boolean)
   */

        public override bool Tokenized()
        {
            return this.tokenized;
        }

        /**
   * Set to <code>true</code> to tokenize this field's contents via the 
   * configured {@link Analyzer}.
   * @param value true if this field should be tokenized.
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #tokenized()
   */

        public override void SetTokenized(bool value)
        {
            CheckIfFrozen();
            this.tokenized = value;
        }

        /**
   * {@inheritDoc}
   * <p>
   * The default is <code>false</code>. 
   * @see #setStoreTermVectors(boolean)
   */

        public override bool StoreTermVectors()
        {
            return this.storeTermVectors;
        }

        /**
   * Set to <code>true</code> if this field's indexed form should be also stored 
   * into term vectors.
   * @param value true if this field should store term vectors.
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #storeTermVectors()
   */

        public virtual void SetStoreTermVectors(bool value)
        {
            CheckIfFrozen();
            this.storeTermVectors = value;
        }

        /**
   * {@inheritDoc}
   * <p>
   * The default is <code>false</code>.
   * @see #setStoreTermVectorOffsets(boolean)
   */

        public override bool StoreTermVectorOffsets()
        {
            return this.storeTermVectorOffsets;
        }

        /**
   * Set to <code>true</code> to also store token character offsets into the term
   * vector for this field.
   * @param value true if this field should store term vector offsets.
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #storeTermVectorOffsets()
   */

        public void SetStoreTermVectorOffsets(bool value)
        {
            CheckIfFrozen();
            this.storeTermVectorOffsets = value;
        }

        /**
   * {@inheritDoc}
   * <p>
   * The default is <code>false</code>.
   * @see #setStoreTermVectorPositions(boolean)
   */

        public override bool StoreTermVectorPositions()
        {
            return this.storeTermVectorPositions;
        }

        /**
   * Set to <code>true</code> to also store token positions into the term
   * vector for this field.
   * @param value true if this field should store term vector positions.
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #storeTermVectorPositions()
   */

        public override void SetStoreTermVectorPositions(bool value)
        {
            CheckIfFrozen();
            this.storeTermVectorPositions = value;
        }

        /**
   * {@inheritDoc}
   * <p>
   * The default is <code>false</code>.
   * @see #setStoreTermVectorPayloads(boolean) 
   */

        public override bool StoreTermVectorPayloads()
        {
            return this.storeTermVectorPayloads;
        }

        /**
   * Set to <code>true</code> to also store token payloads into the term
   * vector for this field.
   * @param value true if this field should store term vector payloads.
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #storeTermVectorPayloads()
   */

        public void SetStoreTermVectorPayloads(bool value)
        {
            CheckIfFrozen();
            this.storeTermVectorPayloads = value;
        }

        /**
   * {@inheritDoc}
   * <p>
   * The default is <code>false</code>.
   * @see #setOmitNorms(boolean)
   */

        public override bool OmitNorms()
        {
            return this.omitNorms;
        }

        /**
   * Set to <code>true</code> to omit normalization values for the field.
   * @param value true if this field should omit norms.
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #omitNorms()
   */

        public override void SetOmitNorms(bool value)
        {
            CheckIfFrozen();
            this.omitNorms = value;
        }

        /**
   * {@inheritDoc}
   * <p>
   * The default is {@link IndexOptions#DOCS_AND_FREQS_AND_POSITIONS}.
   * @see #setIndexOptions(org.apache.lucene.index.FieldInfo.IndexOptions)
   */

        public override IndexOptions IndexOptions()
        {
            return this.indexOptions;
        }

        /**
   * Sets the indexing options for the field:
   * @param value indexing options
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #indexOptions()
   */

        public void SetIndexOptions(IndexOptions value)
        {
            CheckIfFrozen();
            this.indexOptions = value;
        }

        /**
   * Specifies the field's numeric type.
   * @param type numeric type, or null if the field has no numeric type.
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #numericType()
   */

        public void SetNumericType(NumericType type)
        {
            CheckIfFrozen();
            numericType = type;
        }

        /** 
   * NumericType: if non-null then the field's value will be indexed
   * numerically so that {@link NumericRangeQuery} can be used at 
   * search time. 
   * <p>
   * The default is <code>null</code> (no numeric type) 
   * @see #setNumericType(NumericType)
   */

        public NumericType? GetNumericType
        {
            get { return numericType; }
        }

        /**
   * Sets the numeric precision step for the field.
   * @param precisionStep numeric precision step for the field
   * @throws IllegalArgumentException if precisionStep is less than 1. 
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #numericPrecisionStep()
   */

        public void SetNumericPrecisionStep(int precisionStep)
        {
            CheckIfFrozen();
            if (precisionStep < 1)
            {
                throw new ArgumentException("precisionStep must be >= 1 (got " + precisionStep + ")");
            }
            this.numericPrecisionStep = precisionStep;
        }

        /** 
   * Precision step for numeric field. 
   * <p>
   * This has no effect if {@link #numericType()} returns null.
   * <p>
   * The default is {@link NumericUtils#PRECISION_STEP_DEFAULT}
   * @see #setNumericPrecisionStep(int)
   */

        public int NumericPrecisionStep()
        {
            return numericPrecisionStep;
        }

        /** Prints a Field for human consumption. */

        public override String ToString()
        {
            StringBuilder result = new StringBuilder();
            if (Stored())
            {
                result.Append("stored");
            }
            if (Indexed())
            {
                if (result.Length > 0)
                    result.Append(",");
                result.Append("indexed");
                if (Tokenized())
                {
                    result.Append(",tokenized");
                }
                if (StoreTermVectors())
                {
                    result.Append(",termVector");
                }
                if (StoreTermVectorOffsets())
                {
                    result.Append(",termVectorOffsets");
                }
                if (StoreTermVectorPositions())
                {
                    result.Append(",termVectorPosition");
                    if (StoreTermVectorPayloads())
                    {
                        result.Append(",termVectorPayloads");
                    }
                }
                if (OmitNorms())
                {
                    result.Append(",omitNorms");
                }
                if (indexOptions != IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
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
                    result.Append(",");
                result.Append("docValueType=");
                result.Append(docValueType);
            }

            return result.ToString();
        }

        /* from StorableFieldType */

        /**
   * {@inheritDoc}
   * <p>
   * The default is <code>null</code> (no docValues) 
   * @see #setDocValueType(org.apache.lucene.index.FieldInfo.DocValuesType)
   */

        public override DocValuesType DocValueType()
        {
            return docValueType;
        }

        /**
   * Set's the field's DocValuesType
   * @param type DocValues type, or null if no DocValues should be stored.
   * @throws IllegalStateException if this FieldType is frozen against
   *         future modifications.
   * @see #docValueType()
   */

        public void SetDocValueType(DocValuesType type)
        {
            CheckIfFrozen();
            docValueType = type;
        }
    }
}
