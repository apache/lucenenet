// commons-codec version compatibility level: 1.9
using System;

namespace Lucene.Net.Analysis.Phonetic.Language.Bm
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
    /// Supported types of names. Unless you are matching particular family names, use <see cref="GENERIC"/>. The
    /// <c>GENERIC</c> NameType should work reasonably well for non-name words. The other encodings are
    /// specifically tuned to family names, and may not work well at all for general text.
    /// <para/>
    /// since 1.6
    /// </summary>
    public enum NameType
    {
        /// <summary>
        /// Ashkenazi family names
        /// </summary>
        ASHKENAZI,

        /// <summary>
        /// Generic names and words
        /// </summary>
        GENERIC,

        /// <summary>
        /// Sephardic family names
        /// </summary>
        SEPHARDIC
    }

    public static class NameTypeExtensions
    {
        /// <summary>
        /// Gets the short version of the name type.
        /// </summary>
        /// <param name="nameType">the <see cref="NameType"/></param>
        /// <returns> the <see cref="NameType"/> short string</returns>
        public static string GetName(this NameType nameType)
        {
            switch (nameType)
            {
                case NameType.ASHKENAZI:
                    return "ash";
                case NameType.GENERIC:
                    return "gen";
                case NameType.SEPHARDIC:
                    return "sep";
            }
            throw new ArgumentException("Invalid nameType.");
        }
    }
}
