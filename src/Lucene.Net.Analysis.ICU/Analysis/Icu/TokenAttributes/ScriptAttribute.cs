using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Icu.TokenAttributes
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
    /// This attribute stores the UTR #24 script value for a token of text.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public interface IScriptAttribute : IAttribute
    {
        /// <summary>
        /// Gets or Sets the numeric code for this script value.
        /// <para/>
        /// This is the constant value from <see cref="ICU4N.Globalization.UScript"/>.
        /// </summary>
        int Code { get; set; }

        /// <summary>
        /// Get the full name.
        /// </summary>
        /// <returns>UTR #24 full name.</returns>
        string GetName();

        /// <summary>
        /// Get the abbreviated name.
        /// </summary>
        /// <returns>UTR #24 abbreviated name.</returns>
        [ExceptionToNetNumericConvention]
        string GetShortName();
    }
}
