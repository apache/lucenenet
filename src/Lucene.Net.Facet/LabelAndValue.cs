using Lucene.Net.Support;
using System;
using System.Globalization;

namespace Lucene.Net.Facet
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
    /// Single label and its value, usually contained in a
    ///  <seealso cref="FacetResult"/>. 
    /// </summary>
    public sealed class LabelAndValue
    {
        /// <summary>
        /// Facet's label. </summary>
        public readonly string label;

        /// <summary>
        /// Value associated with this label. </summary>
        public readonly float value;

        /// <summary>
        /// The original data type of <see cref="value"/> that was passed through the constructor.
        /// </summary>
        public readonly Type typeOfValue;

        /// <summary>
        /// Constructor for <see cref="float"/> <paramref name="value"/>. Makes the <see cref="ToString()"/> method 
        /// print the <paramref name="value"/> as a <see cref="float"/> with at least 1 number after the decimal.
        /// </summary>
        public LabelAndValue(string label, float value)
        {
            this.label = label;
            this.value = value;
            this.typeOfValue = typeof(float);
        }

        /// <summary>
        /// Constructor for <see cref="int"/> <paramref name="value"/>. Makes the <see cref="ToString()"/> method 
        /// print the <paramref name="value"/> as an <see cref="int"/> with no decimal.
        /// </summary>
        public LabelAndValue(string label, int value)
        {
            this.label = label;
            this.value = value;
            this.typeOfValue = typeof(int);
        }

        public override string ToString()
        {
            string valueString = (typeOfValue == typeof(int)) 
                ? value.ToString("0", CultureInfo.InvariantCulture) 
                : value.ToString("0.0#####", CultureInfo.InvariantCulture);
            return label + " (" + valueString + ")";
        }

        public override bool Equals(object _other)
        {
            if ((_other is LabelAndValue) == false)
            {
                return false;
            }
            LabelAndValue other = (LabelAndValue)_other;
            return label.Equals(other.label) && value.Equals(other.value);
        }

        public override int GetHashCode()
        {
            return label.GetHashCode() + 1439 * value.GetHashCode();
        }
    }

}