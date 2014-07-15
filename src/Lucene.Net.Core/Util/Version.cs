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

namespace Lucene.Net.Util
{
    using Lucene.Net.Support;
    using System;
    using System.Text.RegularExpressions;


    /// <summary> Use by certain classes to match version compatibility
    /// across releases of Lucene.
    ///  
    ///  <b>WARNING</b>: When changing the version parameter
    ///  that you supply to components in Lucene, do not simply
    ///  change the version at search-time, but instead also adjust
    ///  your indexing code to match, and re-index.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Java <see href="https://github.com/apache/lucene-solr/blob/trunk/lucene/core/src/test/org/apache/lucene/util/TestVersion.java">Source</see>
    ///     </para>
    /// </remarks>
    public enum Version
    {

        /// <summary>
        /// Match settings and bugs in Lucene's 4.3 release.
        /// <para>
        /// Use this to get the latest and greatest settings, bug fixes,
        /// etc, for Lucene.
        /// </para>
        /// </summary>
        [Obsolete()]
        LUCENE_4_3,


        /// <summary>
        /// Match settings and bugs in Lucene.Net's 4.3 release.
        /// <para>
        /// Use this to get the latest and greatest settings, bug fixes,
        /// etc, for Lucene.
        /// </para>
        /// </summary>
        LUCENE_5_0,

        // NOTE: Add new constants for later versions **here** to respect order!

        /// <summary>
        /// <p/><b>WARNING</b>: if you use this setting, and then
        /// upgrade to a newer release of Lucene, sizable changes
        /// may happen.  If precise back compatibility is important
        /// then you should instead explicitly specify an actual
        /// version.
        /// If you use this constant then you may need to
        /// <b>re-index all of your documents</b> when upgrading
        /// Lucene, as the way text is indexed may have changed.
        /// Additionally, you may need to <b>re-test your entire
        /// application</b> to ensure it behaves as expected, as
        /// some defaults may have changed and may break functionality
        /// in your application.
        /// </summary>
        [Obsolete("Use an actual version instead.")]
        LUCENE_CURRENT,
    }

    public static class VersionEnumExtensions
    {
        /// <summary>
        /// Verifies that the other version number is equal to or greater than the first version.
        /// </summary>
        /// <param name="first">The floor version.</param>
        /// <param name="other">The actual version.</param>
        /// <returns><see cref="bool"/> True, if the other version is equal or greater than the first, otherwise, false.</returns>
        public static bool OnOrAfter(this Version first, Version other)
        {
            return first.CompareTo(other) >= 0;
        }

        /// <summary>
        /// Parses the string value and converts it to the version. Some examples are: "4.3", "LUCENE_43", and "LUCENE_4_3" 
        /// </summary>
        /// <remarks>
        ///     <code>
        ///         var version default(Version).ParseLeniently("4.3");
        ///     </code>
        ///     <para>
        ///         This is a convience method. C# does not support methods inside of Enums.  This is close as it gets.
        ///     </para>
        /// </remarks>
        /// <param name="enumVersion">The enum value.</param>
        /// <param name="version">The string representation of the version.</param>
        /// <returns><see cref="Lucene.Net.Util.Version"/></returns>
        public static Version ParseLeniently(this Version enumVersion, string version)
        {
            return ParseLeniently(version);
        }

        /// <summary>
        /// Parses the string value and converts it to the version. Some examples are: "4.3", "LUCENE_43", and "LUCENE_4_3" 
        /// </summary>
        /// <param name="version">The string representation of the version.</param>
        /// <param name="version">The string representation of the version.</param>
        /// <returns><see cref="Lucene.Net.Util.Version"/></returns>
        public static Version ParseLeniently(string version)
        { 
            var parsedMatchVersion = Check.NotEmptyOrWhitespace("version", version);

            parsedMatchVersion = parsedMatchVersion.ToUpperInvariant()
                                        .ReplaceFirst("^(\\d+)\\.(\\d+)$", "LUCENE_$1_$2")
                                        .ReplaceFirst("^LUCENE_(\\d)(\\d)$", "LUCENE_$1_$2");

            return (Version)Enum.Parse(typeof(Version), parsedMatchVersion);
        }

    
        
    }
}