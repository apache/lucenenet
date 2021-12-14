// Lucene version compatibility level 4.8.1
using System;
using System.Globalization;
#nullable enable

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
    /// <see cref="FacetResult"/>. 
    /// </summary>
    public sealed class LabelAndValue : IFormattable
    {
        /// <summary>
        /// Facet's label. </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Value associated with this label. </summary>
        public float Value { get; private set; }

        /// <summary>
        /// The original data type of <see cref="Value"/> that was passed through the constructor.
        /// </summary>
        public Type TypeOfValue { get; private set; }

        /// <summary>
        /// Constructor for <see cref="float"/> <paramref name="value"/>. Makes the <see cref="ToString()"/> method 
        /// print the <paramref name="value"/> as a <see cref="float"/> with at least 1 number after the decimal.
        /// </summary>
        public LabelAndValue(string label, float value)
        {
            this.Label = label;
            this.Value = value;
            this.TypeOfValue = typeof(float);
        }

        /// <summary>
        /// Constructor for <see cref="int"/> <paramref name="value"/>. Makes the <see cref="ToString()"/> method 
        /// print the <paramref name="value"/> as an <see cref="int"/> with no decimal.
        /// </summary>
        public LabelAndValue(string label, int value)
        {
            this.Label = label;
            this.Value = value;
            this.TypeOfValue = typeof(int);
        }

        /// <summary>
        /// Converts the value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the label and value of this instance.</returns>
        public override string ToString()
        {
            return ToString(null);
        }

        /// <summary>
        /// Converts the value of this instance to its equivalent string representation
        /// using the specified culture-specific format information.
        /// </summary>
        /// <returns>The string representation of the label and value of this instance
        /// as specified by <paramref name="provider"/>.</returns>
        // LUCENENET specific - allow passing in a format provider to format the numbers.
        public string ToString(IFormatProvider? provider)
        {
            string valueString = (TypeOfValue == typeof(int))
                ? J2N.Numerics.Int32.ToString(Convert.ToInt32(Value), provider)
                : J2N.Numerics.Single.ToString(Value, provider);
            return Label + " (" + valueString + ")";
        }

        string IFormattable.ToString(string? format, IFormatProvider? provider) => ToString(provider);

        public override bool Equals(object? other)
        {
            if ((other is LabelAndValue) == false)
            {
                return false;
            }
            LabelAndValue _other = (LabelAndValue)other;
            return Label.Equals(_other.Label, StringComparison.Ordinal) && Value.Equals(_other.Value);
        }

        public override int GetHashCode()
        {
            return Label.GetHashCode() + 1439 * Value.GetHashCode();
        }
    }
}