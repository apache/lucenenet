using Lucene.Net.Search;
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
    /// Builder for <see cref="TermRangeFilter"/>
    /// </summary>
    public class RangeFilterBuilder : IFilterBuilder
    {
        public virtual Filter GetFilter(XmlElement e)
        {
            string fieldName = DOMUtils.GetAttributeWithInheritance(e, "fieldName");

            string lowerTerm = e.GetAttribute("lowerTerm");
            string upperTerm = e.GetAttribute("upperTerm");
            bool includeLower = DOMUtils.GetAttribute(e, "includeLower", true);
            bool includeUpper = DOMUtils.GetAttribute(e, "includeUpper", true);
            return TermRangeFilter.NewStringRange(fieldName, lowerTerm, upperTerm, includeLower, includeUpper);
        }
    }
}
