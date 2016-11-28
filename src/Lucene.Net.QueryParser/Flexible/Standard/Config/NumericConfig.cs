using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lucene.Net.Documents.FieldType;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
{
    /// <summary>
    /// This class holds the configuration used to parse numeric queries and create
    /// {@link NumericRangeQuery}s.
    /// </summary>
    /// <seealso cref="NumericRangeQuery"/>
    /// <seealso cref="NumberFormat"/>
    public class NumericConfig
    {
        private int precisionStep;

        private /*NumberFormat*/ string format;

        private NumericType type;

        /**
         * Constructs a {@link NumericConfig} object.
         * 
         * @param precisionStep
         *          the precision used to index the numeric values
         * @param format
         *          the {@link NumberFormat} used to parse a {@link String} to
         *          {@link Number}
         * @param type
         *          the numeric type used to index the numeric values
         * 
         * @see NumericConfig#setPrecisionStep(int)
         * @see NumericConfig#setNumberFormat(NumberFormat)
         * @see #setType(org.apache.lucene.document.FieldType.NumericType)
         */
        public NumericConfig(int precisionStep, /*NumberFormat*/ string format,
            NumericType type)
        {
            SetPrecisionStep(precisionStep);
            SetNumberFormat(format);
            SetType(type);

        }

        /**
         * Returns the precision used to index the numeric values
         * 
         * @return the precision used to index the numeric values
         * 
         * @see NumericRangeQuery#getPrecisionStep()
         */
        public int GetPrecisionStep()
        {
            return precisionStep;
        }

        /**
         * Sets the precision used to index the numeric values
         * 
         * @param precisionStep
         *          the precision used to index the numeric values
         * 
         * @see NumericRangeQuery#getPrecisionStep()
         */
        public void SetPrecisionStep(int precisionStep)
        {
            this.precisionStep = precisionStep;
        }

        /**
         * Returns the {@link NumberFormat} used to parse a {@link String} to
         * {@link Number}
         * 
         * @return the {@link NumberFormat} used to parse a {@link String} to
         *         {@link Number}
         */
        public /*NumberFormat*/ string GetNumberFormat()
        {
            return format;
        }

        /**
         * Returns the numeric type used to index the numeric values
         * 
         * @return the numeric type used to index the numeric values
         */
        public NumericType GetType()
        {
            return type;
        }

        /**
         * Sets the numeric type used to index the numeric values
         * 
         * @param type the numeric type used to index the numeric values
         */
        public void SetType(NumericType type)
        {

            //if (type == null)
            //{
            //    throw new ArgumentException("type cannot be null!");
            //}

            this.type = type;

        }

        /**
         * Sets the {@link NumberFormat} used to parse a {@link String} to
         * {@link Number}
         * 
         * @param format
         *          the {@link NumberFormat} used to parse a {@link String} to
         *          {@link Number}, cannot be <code>null</code>
         */
        public void SetNumberFormat(/*NumberFormat*/ string format)
        {

            if (format == null)
            {
                throw new ArgumentException("format cannot be null!");
            }

            this.format = format;

        }


        public override bool Equals(object obj)
        {

            if (obj == this) return true;

            if (obj is NumericConfig)
            {
                NumericConfig other = (NumericConfig)obj;

                if (this.precisionStep == other.precisionStep
                    && this.type == other.type
                    && (this.format == other.format || (this.format.Equals(other.format))))
                {
                    return true;
                }

            }

            return false;

        }
    }
}
