using System.Collections.Generic;

namespace Lucene.Net.Util
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
    /// Use by certain classes to match version compatibility
    /// across releases of Lucene.
    ///
    /// <p><b>WARNING</b>: When changing the version parameter
    /// that you supply to components in Lucene, do not simply
    /// change the version at search-time, but instead also adjust
    /// your indexing code to match, and re-index.</p>
    /// </summary>
    public enum LuceneVersion
    {
        /// <summary>
        /// Match settings and bugs in Lucene's 3.0 release. </summary>
        /// @deprecated (4.0) Use latest
        [System.Obsolete("(4.0) Use latest")]
        LUCENE_30,

        /// <summary>
        /// Match settings and bugs in Lucene's 3.1 release. </summary>
        /// @deprecated (4.0) Use latest
        [System.Obsolete("(4.0) Use latest")]
        LUCENE_31,

        /// <summary>
        /// Match settings and bugs in Lucene's 3.2 release. </summary>
        /// @deprecated (4.0) Use latest
        [System.Obsolete("(4.0) Use latest")]
        LUCENE_32,

        /// <summary>
        /// Match settings and bugs in Lucene's 3.3 release. </summary>
        /// @deprecated (4.0) Use latest
        [System.Obsolete("(4.0) Use latest")]
        LUCENE_33,

        /// <summary>
        /// Match settings and bugs in Lucene's 3.4 release. </summary>
        /// @deprecated (4.0) Use latest
        [System.Obsolete("(4.0) Use latest")]
        LUCENE_34,

        /// <summary>
        /// Match settings and bugs in Lucene's 3.5 release. </summary>
        /// @deprecated (4.0) Use latest
        [System.Obsolete("(4.0) Use latest")]
        LUCENE_35,

        /// <summary>
        /// Match settings and bugs in Lucene's 3.6 release. </summary>
        /// @deprecated (4.0) Use latest
        [System.Obsolete("(4.0) Use latest")]
        LUCENE_36,

        /// <summary>
        /// Match settings and bugs in Lucene's 3.6 release. </summary>
        /// @deprecated (4.1) Use latest
        [System.Obsolete("(4.1) Use latest")]
        LUCENE_40,

        /// <summary>
        /// Match settings and bugs in Lucene's 4.1 release. </summary>
        /// @deprecated (4.2) Use latest
        [System.Obsolete("(4.2) Use latest")]
        LUCENE_41,

        /// <summary>
        /// Match settings and bugs in Lucene's 4.2 release. </summary>
        /// @deprecated (4.3) Use latest
        [System.Obsolete("(4.3) Use latest")]
        LUCENE_42,

        /// <summary>
        /// Match settings and bugs in Lucene's 4.3 release. </summary>
        /// @deprecated (4.4) Use latest
        [System.Obsolete("(4.4) Use latest")]
        LUCENE_43,

        /// <summary>
        /// Match settings and bugs in Lucene's 4.4 release. </summary>
        /// @deprecated (4.5) Use latest
        [System.Obsolete("(4.5) Use latest")]
        LUCENE_44,

        /// <summary>
        /// Match settings and bugs in Lucene's 4.5 release. </summary>
        /// @deprecated (4.6) Use latest
        [System.Obsolete("(4.6) Use latest")]
        LUCENE_45,

        /// <summary>
        /// Match settings and bugs in Lucene's 4.6 release. </summary>
        /// @deprecated (4.7) Use latest
        [System.Obsolete("(4.7) Use latest")]
        LUCENE_46,

        /// <summary>
        /// Match settings and bugs in Lucene's 4.7 release. </summary>
        /// @deprecated (4.8) Use latest
        [System.Obsolete("(4.8) Use latest")]
        LUCENE_47,

        /// <summary>
        /// Match settings and bugs in Lucene's 4.8 release.
        ///  <p>
        ///  Use this to get the latest &amp; greatest settings, bug
        ///  fixes, etc, for Lucene.
        /// </summary>
        LUCENE_48,

        /* Add new constants for later versions **here** to respect order! */

        /// <summary>
        /// <p><b>WARNING</b>: if you use this setting, and then
        /// upgrade to a newer release of Lucene, sizable changes
        /// may happen.  If backwards compatibility is important
        /// then you should instead explicitly specify an actual
        /// version.
        /// <para/>
        /// If you use this constant then you  may need to
        /// <b>re-index all of your documents</b> when upgrading
        /// Lucene, as the way text is indexed may have changed.
        /// Additionally, you may need to <b>re-test your entire
        /// application</b> to ensure it behaves as expected, as
        /// some defaults may have changed and may break functionality
        /// in your application. </summary>
        /// @deprecated Use an actual version instead.
        [System.Obsolete("Use an actual version instead.")]
        LUCENE_CURRENT
    }

    public static class LuceneVersionHelpers // LUCENENET TODO: rename Version ?
    {
        // LUCENENET TODO: The simple Regex in the original in combination with Enum.Parse() would make this unnecessary
        private static readonly Dictionary<string, LuceneVersion> stringToEnum = new Dictionary<string, LuceneVersion>()
        {
            {"LUCENE_30", LuceneVersion.LUCENE_30},
            {"LUCENE_31", LuceneVersion.LUCENE_31},
            {"LUCENE_32", LuceneVersion.LUCENE_32},
            {"LUCENE_33", LuceneVersion.LUCENE_33},
            {"LUCENE_34", LuceneVersion.LUCENE_34},
            {"LUCENE_35", LuceneVersion.LUCENE_35},
            {"LUCENE_36", LuceneVersion.LUCENE_36},
            {"LUCENE_40", LuceneVersion.LUCENE_40},
            {"LUCENE_41", LuceneVersion.LUCENE_41},
            {"LUCENE_42", LuceneVersion.LUCENE_42},
            {"LUCENE_43", LuceneVersion.LUCENE_43},
            {"LUCENE_44", LuceneVersion.LUCENE_44},
            {"LUCENE_45", LuceneVersion.LUCENE_45},
            {"LUCENE_46", LuceneVersion.LUCENE_46},
            {"LUCENE_47", LuceneVersion.LUCENE_47},
            {"LUCENE_48", LuceneVersion.LUCENE_48},
            {"LUCENE_CURRENT", LuceneVersion.LUCENE_CURRENT}
        };

        // LUCENENET TODO: The simple Regex in the original in combination with Enum.Parse() would make this unnecessary
        private static readonly Dictionary<string, LuceneVersion> longToEnum = new Dictionary<string, LuceneVersion>()
        {
            {"3.0", LuceneVersion.LUCENE_30},
            {"3.1", LuceneVersion.LUCENE_31},
            {"3.2", LuceneVersion.LUCENE_32},
            {"3.3", LuceneVersion.LUCENE_33},
            {"3.4", LuceneVersion.LUCENE_34},
            {"3.5", LuceneVersion.LUCENE_35},
            {"3.6", LuceneVersion.LUCENE_36},
            {"4.0", LuceneVersion.LUCENE_40},
            {"4.1", LuceneVersion.LUCENE_41},
            {"4.2", LuceneVersion.LUCENE_42},
            {"4.3", LuceneVersion.LUCENE_43},
            {"4.4", LuceneVersion.LUCENE_44},
            {"4.5", LuceneVersion.LUCENE_45},
            {"4.6", LuceneVersion.LUCENE_46},
            {"4.7", LuceneVersion.LUCENE_47},
            {"4.8", LuceneVersion.LUCENE_48}
        };

        public static bool OnOrAfter(this LuceneVersion instance, LuceneVersion other)
        {
            return other <= instance;
            //return other >= 0; //LUCENENET TODO
        }

        public static LuceneVersion ParseLeniently(string version)
        {
            string upperVersionString = version.ToUpper(); // LUCENENET TODO: culture
            LuceneVersion ret;
            if (stringToEnum.TryGetValue(upperVersionString, out ret))
            {
                return ret;
            }
            else if (longToEnum.TryGetValue(upperVersionString, out ret))
            {
                return ret;
            }
            return ret;
        }
    }
}