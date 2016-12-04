using Lucene.Net.Support;
using System;
using static Lucene.Net.Documents.FieldType;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
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
    /// This class holds the configuration used to parse numeric queries and create
    /// {@link NumericRangeQuery}s.
    /// </summary>
    /// <seealso cref="NumericRangeQuery"/>
    /// <seealso cref="NumberFormat"/>
    public class NumericConfig
    {
        private int precisionStep;

        private NumberFormat format;

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
        public NumericConfig(int precisionStep, NumberFormat format,
            NumericType type)
        {
            PrecisionStep = precisionStep;
            NumberFormat = format;
            Type = type;
        }

        /**
         * Gets or sets the precision used to index the numeric values
         * 
         * @return the precision used to index the numeric values
         * 
         * @see NumericRangeQuery#getPrecisionStep()
         */
        public virtual int PrecisionStep
        {
            get { return precisionStep; }
            set { precisionStep = value; }
        }

        /**
         * Gets or Sets the {@link NumberFormat} used to parse a {@link String} to
         * {@link Number}, cannot be <code>null</code>
         * 
         * @return the {@link NumberFormat} used to parse a {@link String} to
         *         {@link Number}
         */
        public virtual NumberFormat NumberFormat
        {
            get { return format; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException("format cannot be null!");
                }

                this.format = value;
            }
        }

        /**
         * Gets or Sets the numeric type used to index the numeric values
         * 
         * @return the numeric type used to index the numeric values
         */
        public virtual NumericType Type
        {
            get { return type; }
            set { type = value; }
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
