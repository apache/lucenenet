using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using System;
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
    /// Builder for <see cref="DuplicateFilter"/>
    /// </summary>
    public class DuplicateFilterBuilder : IFilterBuilder
    {
        public virtual Filter GetFilter(XmlElement e)
        {
            string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            DuplicateFilter df = new DuplicateFilter(fieldName);

            string keepMode = DOMUtils.GetAttribute(e, "keepMode", "first");
            if (keepMode.Equals("first", StringComparison.OrdinalIgnoreCase))
            {
                df.KeepMode = KeepMode.KM_USE_FIRST_OCCURRENCE;
            }
            else if (keepMode.Equals("last", StringComparison.OrdinalIgnoreCase))
            {
                df.KeepMode = KeepMode.KM_USE_LAST_OCCURRENCE;
            }
            else
            {
                throw new ParserException("Illegal keepMode attribute in DuplicateFilter:" + keepMode);
            }

            string processingMode = DOMUtils.GetAttribute(e, "processingMode", "full");
            if (processingMode.Equals("full", StringComparison.OrdinalIgnoreCase))
            {
                df.ProcessingMode = ProcessingMode.PM_FULL_VALIDATION;
            }
            else if (processingMode.Equals("fast", StringComparison.OrdinalIgnoreCase))
            {
                df.ProcessingMode = ProcessingMode.PM_FAST_INVALIDATION;
            }
            else
            {
                throw new ParserException("Illegal processingMode attribute in DuplicateFilter:" + processingMode);
            }

            return df;
        }
    }
}
