namespace Lucene.Net.QueryParsers.Ext
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
    /// <see cref="ExtensionQuery"/> holds all query components extracted from the original
    /// query string like the query field and the extension query string.
    /// </summary>
    public class ExtensionQuery
    {
        /// <summary>
        /// Creates a new <see cref="ExtensionQuery"/>
        /// </summary>
        /// <param name="topLevelParser"></param>
        /// <param name="field">the query field</param>
        /// <param name="rawQueryString">the raw extension query string</param>
        public ExtensionQuery(Classic.QueryParser topLevelParser, string field, string rawQueryString)
        {
            this.Field = field;
            this.RawQueryString = rawQueryString;
            this.TopLevelParser = topLevelParser;
        }

        /// <summary>
        /// Returns the query field
        /// </summary>
        public virtual string Field { get; protected set; }

        /// <summary>
        /// Returns the raw extension query string
        /// </summary>
        public virtual string RawQueryString { get; protected set; }

        /// <summary>
        /// Returns the top level parser which created this <see cref="ExtensionQuery"/>
        /// </summary>
        public virtual Classic.QueryParser TopLevelParser { get; protected set; }
    }
}
