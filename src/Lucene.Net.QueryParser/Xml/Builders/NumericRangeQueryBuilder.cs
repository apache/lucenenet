using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Globalization;
using System.Xml;

namespace Lucene.Net.QueryParsers.Xml.Builders
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
    /// Creates a <see cref="NumericRangeQuery"/>. The table below specifies the required
    /// attributes and the defaults if optional attributes are omitted. For more
    /// detail on what each of the attributes actually do, consult the documentation
    /// for <see cref="NumericRangeQuery"/>:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Attribute name</term>
    ///         <term>Values</term>
    ///         <term>Required</term>
    ///         <term>Default</term>
    ///     </listheader>
    ///     <item>
    ///         <term>fieldName</term>
    ///         <term>String</term>
    ///         <term>Yes</term>
    ///         <term>N/A</term>
    ///     </item>
    ///     <item>
    ///         <term>lowerTerm</term>
    ///         <term>Specified by <c>type</c></term>
    ///         <term>Yes</term>
    ///         <term>N/A</term>
    ///     </item>
    ///     <item>
    ///         <term>upperTerm</term>
    ///         <term>Specified by <c>type</c></term>
    ///         <term>Yes</term>
    ///         <term>N/A</term>
    ///     </item>
    ///     <item>
    ///         <term>type</term>
    ///         <term>int, long, float, double</term>
    ///         <term>No</term>
    ///         <term>int</term>
    ///     </item>
    ///     <item>
    ///         <term>includeLower</term>
    ///         <term>true, false</term>
    ///         <term>No</term>
    ///         <term>true</term>
    ///     </item>
    ///     <item>
    ///         <term>includeUpper</term>
    ///         <term>true, false</term>
    ///         <term>No</term>
    ///         <term>true</term>
    ///     </item>
    ///     <item>
    ///         <term>precisionStep</term>
    ///         <term>int</term>
    ///         <term>No</term>
    ///         <term>4</term>
    ///     </item>
    /// </list>
    /// <para/>
    /// A <see cref="ParserException"/> will be thrown if an error occurs parsing the
    /// supplied <c>lowerTerm</c> or <c>upperTerm</c> into the numeric type
    /// specified by <c>type</c>.
    /// </summary>
    public class NumericRangeQueryBuilder : IQueryBuilder
    {
        public virtual Query GetQuery(XmlElement e)
        {
            string field = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            string lowerTerm = DOMUtils.GetAttributeOrFail(e, "lowerTerm");
            string upperTerm = DOMUtils.GetAttributeOrFail(e, "upperTerm");
            bool lowerInclusive = DOMUtils.GetAttribute(e, "includeLower", true);
            bool upperInclusive = DOMUtils.GetAttribute(e, "includeUpper", true);
            int precisionStep = DOMUtils.GetAttribute(e, "precisionStep", NumericUtils.PRECISION_STEP_DEFAULT);

            string type = DOMUtils.GetAttribute(e, "type", "int");
            try
            {
                Query filter;
                if (type.Equals("int", StringComparison.OrdinalIgnoreCase))
                {
                    filter = NumericRangeQuery.NewInt32Range(field, precisionStep,
                        J2N.Numerics.Int32.Parse(lowerTerm, NumberFormatInfo.InvariantInfo),
                        J2N.Numerics.Int32.Parse(upperTerm, NumberFormatInfo.InvariantInfo),
                        lowerInclusive,
                        upperInclusive);
                }
                else if (type.Equals("long", StringComparison.OrdinalIgnoreCase))
                {
                    filter = NumericRangeQuery.NewInt64Range(field, precisionStep,
                        J2N.Numerics.Int64.Parse(lowerTerm, NumberFormatInfo.InvariantInfo),
                        J2N.Numerics.Int64.Parse(upperTerm, NumberFormatInfo.InvariantInfo),
                        lowerInclusive,
                        upperInclusive);
                }
                else if (type.Equals("double", StringComparison.OrdinalIgnoreCase))
                {
                    filter = NumericRangeQuery.NewDoubleRange(field, precisionStep,
                        J2N.Numerics.Double.Parse(lowerTerm, NumberFormatInfo.InvariantInfo),
                        J2N.Numerics.Double.Parse(upperTerm, NumberFormatInfo.InvariantInfo),
                        lowerInclusive,
                        upperInclusive);
                }
                else if (type.Equals("float", StringComparison.OrdinalIgnoreCase))
                {
                    filter = NumericRangeQuery.NewSingleRange(field, precisionStep,
                        J2N.Numerics.Single.Parse(lowerTerm, NumberFormatInfo.InvariantInfo),
                        J2N.Numerics.Single.Parse(upperTerm, NumberFormatInfo.InvariantInfo),
                        lowerInclusive,
                        upperInclusive);
                }
                else
                {
                    throw new ParserException("type attribute must be one of: [long, int, double, float]");
                }
                return filter;
            }
            catch (Exception nfe) when (nfe.IsNumberFormatException())
            {
                throw new ParserException("Could not parse lowerTerm or upperTerm into a number", nfe);
            }
        }
    }
}
