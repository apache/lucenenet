using Lucene.Net.Support;

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
        /// Sole constructor. </summary>
        public LabelAndValue(string label, float value)
        {
            this.label = label;
            this.value = value;
        }

        public override string ToString()
        {
            return label + " (" + value + ")";
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