using Lucene.Net.Support;
using System;
using System.Globalization;
using System.Text;

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
    /// Counts or aggregates for a single dimension. </summary>
    public sealed class FacetResult
    {
        /// <summary>
        /// Dimension that was requested. </summary>
        public readonly string Dim;

        /// <summary>
        /// Path whose children were requested. </summary>
        public readonly string[] Path;

        /// <summary>
        /// Total value for this path (sum of all child counts, or
        ///  sum of all child values), even those not included in
        ///  the topN. 
        /// </summary>
        public readonly float Value;

        /// <summary>
        /// How many child labels were encountered. </summary>
        public readonly int ChildCount;

        /// <summary>
        /// Child counts. </summary>
        public readonly LabelAndValue[] LabelValues;

        /// <summary>
        /// The original data type of <see cref="Value"/> that was passed through the constructor.
        /// </summary>
        public readonly Type typeOfValue;

        /// <summary>
        /// Constructor for <see cref="float"/> <paramref name="value"/>. Makes the <see cref="ToString()"/> method 
        /// print the <paramref name="value"/> as a <see cref="float"/> with at least 1 number after the decimal.
        /// </summary>
        public FacetResult(string dim, string[] path, float value, LabelAndValue[] labelValues, int childCount)
            : this(dim, path, labelValues, childCount)
        {
            this.Value = value;
            this.typeOfValue = typeof(float);
        }

        /// <summary>
        /// Constructor for <see cref="int"/> <paramref name="value"/>. Makes the <see cref="ToString()"/> method 
        /// print the <paramref name="value"/> as an <see cref="int"/> with no decimal.
        /// </summary>
        public FacetResult(string dim, string[] path, int value, LabelAndValue[] labelValues, int childCount)
            : this(dim, path, labelValues, childCount)
        {
            this.Value = value;
            this.typeOfValue = typeof(int);
        }

        /// <summary>
        /// Private constructor for shared parameters to be called by public constructors.
        /// </summary>
        private FacetResult(string dim, string[] path, LabelAndValue[] labelValues, int childCount)
        {
            this.Dim = dim;
            this.Path = path;
            this.LabelValues = labelValues;
            this.ChildCount = childCount;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("dim=");
            sb.Append(Dim);
            sb.Append(" path=");
            sb.Append("[" + Arrays.ToString(Path) + "]");
            sb.Append(" value=");
            if (typeOfValue == typeof(int))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0:0}", Value); // No formatting (looks like int)
            }
            else
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0:0.0#####}", Value); // Decimal formatting
            }
            sb.Append(" childCount=");
            sb.Append(ChildCount);
            sb.Append('\n');
            foreach (LabelAndValue labelValue in LabelValues)
            {
                sb.Append("  " + labelValue + "\n");
            }
            return sb.ToString();
        }

        public override bool Equals(object _other)
        {
            if ((_other is FacetResult) == false)
            {
                return false;
            }
            FacetResult other = (FacetResult)_other;
            return Value.Equals(other.Value) && ChildCount == other.ChildCount && Arrays.Equals(LabelValues, other.LabelValues);
        }

        public override int GetHashCode()
        {
            int hashCode = Value.GetHashCode() + 31 * ChildCount;
            foreach (LabelAndValue labelValue in LabelValues)
            {
                hashCode = labelValue.GetHashCode() + 31 * hashCode;
            }
            return hashCode;
        }
    }
}