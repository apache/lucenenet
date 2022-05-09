// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Analysis.Sinks
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
    /// Adds a token to the sink if it has a specific type.
    /// </summary>
    public class TokenTypeSinkFilter : TeeSinkTokenFilter.SinkFilter
    {
        private readonly string typeToMatch;
        private ITypeAttribute typeAtt;

        public TokenTypeSinkFilter(string typeToMatch)
        {
            this.typeToMatch = typeToMatch;
        }

        public override bool Accept(AttributeSource source)
        {
            if (typeAtt is null)
            {
                typeAtt = source.AddAttribute<ITypeAttribute>();
            }

            //check to see if this is a Category
            return (typeToMatch.Equals(typeAtt.Type, StringComparison.Ordinal));
        }
    }
}