// -----------------------------------------------------------------------
// <copyright company="Apache" file="StringExtensions.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------



namespace Lucene.Net.Support
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Extension methods for strings
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Alias for string.Format that uses <see cref="CultureInfo.InvariantCulture"/>
        /// for formatting strings.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="args">The args.</param>
        /// <returns>an instance of <see cref="string"/></returns>
        public static string Inject(this string obj, params object[] args)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, obj, args);            
        }
    }
}