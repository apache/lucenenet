/**
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
 */

namespace Lucene.Net.Support
{
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// String extenion methods to help with Java to C# conversion
    /// </summary>
    public static class StringExtensionMethods
    {

        /// <summary>
        /// Replaces the first substring of this string that matches the given regular expression with the given replacement.
        /// </summary>
        /// <param name="input">The input value.</param>
        /// <param name="expression">The regular expression.</param>
        /// <param name="replacement">The replacement value. </param>
        /// <returns>The string result after the replacement has been made.</returns>
        public static string ReplaceFirst(this string input, string expression, string replacement)
        {
            var regex = new Regex(expression);
            return regex.Replace(input, replacement, 1);
        }
    }
}