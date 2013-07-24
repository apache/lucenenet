using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;
using Lucene.Net.Index;

namespace Lucene.Net.Documents
{
    public class FieldType : IIndexableFieldType
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
        private FieldInfo.IndexOptions indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
        private NumericType? numericType;
        private bool frozen;
        private int numericPrecisionStep = NumericUtils.PRECISION_STEP_DEFAULT;
        private FieldInfo.DocValuesType docValueType;
                
        public FieldType(FieldType refFieldType)
        {
            this.indexed = refFieldType.Indexed;
            this.stored = refFieldType.Stored;
            this.tokenized = refFieldType.Tokenized;
            this.storeTermVectors = refFieldType.StoreTermVectors;
            this.storeTermVectorOffsets = refFieldType.StoreTermVectorOffsets;
            this.storeTermVectorPositions = refFieldType.StoreTermVectorPositions;
            this.storeTermVectorPayloads = refFieldType.StoreTermVectorPayloads;
            this.omitNorms = refFieldType.OmitNorms;
            this.indexOptions = refFieldType.IndexOptions;
            this.docValueType = refFieldType.DocValueType;
            this.numericType = refFieldType.NumericTypeValue;
            // Do not copy frozen!
        }

        
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
        
        public void Freeze()
        {
            this.frozen = true;
        }
        
        public bool Indexed
        {
            get { return this.indexed; }
            set
            {
                CheckIfFrozen();
                this.indexed = value;
            }
        }
        
        public bool Stored
        {
            get { return this.stored; }
            set
            {
                CheckIfFrozen();
                this.stored = value;
            }
        }

        public bool Tokenized
        {
            get { return this.tokenized; }
            set 
            {
                CheckIfFrozen();
                this.tokenized = value;
            }
        }

        public bool StoreTermVectors
        {
            get { return this.storeTermVectors; }
            set
            {
                CheckIfFrozen();
                this.storeTermVectors = value;
            }
        }

        public bool StoreTermVectorOffsets
        {
            get { return this.storeTermVectorOffsets; }
            set
            {
                CheckIfFrozen();
                this.storeTermVectorOffsets = value;
            }
        }

        public bool StoreTermVectorPositions
        {
            get { return this.storeTermVectorPositions; }
            set
            {
                CheckIfFrozen();
                this.storeTermVectorPositions = value;
            }
        }

        public bool StoreTermVectorPayloads
        {
            get { return this.storeTermVectorPayloads; }
            set
            {
                CheckIfFrozen();
                this.storeTermVectorPayloads = value;
            }
        }

        public bool OmitNorms
        {
            get { return this.omitNorms; }
            set
            {
                CheckIfFrozen();
                this.omitNorms = value;
            }
        }
 
        public FieldInfo.IndexOptions IndexOptions
        {
            get { return this.indexOptions; }
            set
            {
                CheckIfFrozen();
                this.indexOptions = value;
            }
        }

        public NumericType? NumericTypeValue
        {
            get { return numericType; }
            set
            {
                CheckIfFrozen();
                numericType = value;
            }
        }

        public int NumericPrecisionStep
        {
            get { return numericPrecisionStep; }
            set
            {
                CheckIfFrozen();
                if (value < 1)
                {
                    throw new ArgumentException("precisionStep must be >= 1 (got " + value + ")");
                }
                this.numericPrecisionStep = value;
            }
        }
        
        /** Prints a Field for human consumption. */

        public override String ToString()
        {
            StringBuilder result = new StringBuilder();

            if (Stored)
            {
                result.Append("stored");
            }
            if (Indexed)
            {
                if (result.Length > 0)
                    result.Append(",");
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
                if (indexOptions != FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
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

        public FieldInfo.DocValuesType DocValueType
        {
            get { return docValueType; }
            set
            {
                CheckIfFrozen();
                docValueType = value;
            }
        }

    }
}
