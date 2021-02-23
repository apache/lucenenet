// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Core
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
    /// Removes tokens whose types appear in a set of blocked types from a token stream.
    /// </summary>
    public sealed class TypeTokenFilter : FilteringTokenFilter
    {
        private readonly ICollection<string> stopTypes;
        private readonly ITypeAttribute typeAttribute;
        private readonly bool useWhiteList;

        /// @deprecated enablePositionIncrements=false is not supported anymore as of Lucene 4.4. 
        [Obsolete("enablePositionIncrements=false is not supported anymore as of Lucene 4.4.")]
        public TypeTokenFilter(LuceneVersion version, bool enablePositionIncrements, TokenStream input, ICollection<string> stopTypes, bool useWhiteList)
            : base(version, enablePositionIncrements, input)
        {
            typeAttribute = AddAttribute<ITypeAttribute>();
            this.stopTypes = stopTypes;
            this.useWhiteList = useWhiteList;
        }

        /// @deprecated enablePositionIncrements=false is not supported anymore as of Lucene 4.4. 
        [Obsolete("enablePositionIncrements=false is not supported anymore as of Lucene 4.4.")]
        public TypeTokenFilter(LuceneVersion version, bool enablePositionIncrements, TokenStream input, ICollection<string> stopTypes)
            : this(version, enablePositionIncrements, input, stopTypes, false)
        {
        }

        /// <summary>
        /// Create a new <see cref="TypeTokenFilter"/>. </summary>
        /// <param name="version">      the <see cref="LuceneVersion"/> match version </param>
        /// <param name="input">        the <see cref="TokenStream"/> to consume </param>
        /// <param name="stopTypes">    the types to filter </param>
        /// <param name="useWhiteList"> if true, then tokens whose type is in <paramref name="stopTypes"/> will
        ///                     be kept, otherwise they will be filtered out </param>
        public TypeTokenFilter(LuceneVersion version, TokenStream input, ICollection<string> stopTypes, bool useWhiteList)
            : base(version, input)
        {
            typeAttribute = AddAttribute<ITypeAttribute>();
            this.stopTypes = stopTypes;
            this.useWhiteList = useWhiteList;
        }

        /// <summary>
        /// Create a new <see cref="TypeTokenFilter"/> that filters tokens out
        /// (useWhiteList=false). </summary>
        /// <seealso cref="TypeTokenFilter.TypeTokenFilter(LuceneVersion, TokenStream, ICollection{string}, bool)"/>
        public TypeTokenFilter(LuceneVersion version, TokenStream input, ICollection<string> stopTypes)
            : this(version, input, stopTypes, false)
        {
        }

        /// <summary>
        /// By default accept the token if its type is not a stop type.
        /// When the <see cref="useWhiteList"/> parameter is set to true then accept the token if its type is contained in the <see cref="stopTypes"/>
        /// </summary>
        protected override bool Accept()
        {
            return useWhiteList == stopTypes.Contains(typeAttribute.Type);
        }
    }
}