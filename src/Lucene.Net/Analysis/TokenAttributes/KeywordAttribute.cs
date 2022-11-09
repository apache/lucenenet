using Lucene.Net.Util;

namespace Lucene.Net.Analysis.TokenAttributes
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
    /// This attribute can be used to mark a token as a keyword. Keyword aware
    /// <see cref="TokenStream"/>s can decide to modify a token based on the return value
    /// of <see cref="IsKeyword"/> if the token is modified. Stemming filters for
    /// instance can use this attribute to conditionally skip a term if
    /// <see cref="IsKeyword"/> returns <c>true</c>.
    /// </summary>
    public interface IKeywordAttribute : IAttribute
    {
        /// <summary>
        /// Gets or Sets whether the current token is a keyword. <c>true</c> if the current token is a keyword, otherwise
        /// <c>false</c>.
        /// </summary>
        bool IsKeyword { get; set; }
    }
}