// Lucene version compatibility level 4.8.1
using System.Globalization;

namespace Lucene.Net.Collation
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
    /// LUCENENET specific helpers for working with the platform collator
    /// (<see cref="CompareInfo"/>) used by <see cref="CollationKeyAnalyzer"/> and
    /// <see cref="CollationAttributeFactory"/>.
    /// </summary>
    public static class CollationUtil
    {
        /// <summary>
        /// Indicates whether the active globalization backend is ICU (the default on .NET 5+)
        /// rather than the legacy NLS implementation. .NET Framework always uses NLS, while .NET 5+
        /// uses ICU by default but can be configured to use NLS (for example via the
        /// <c>System.Globalization.UseNls</c> runtime switch or the <c>DOTNET_SYSTEM_GLOBALIZATION_USENLS</c>
        /// environment variable). NLS and ICU produce different sort keys and orderings, so this value
        /// (along with the runtime version and culture) is part of what must match between index time
        /// and query time when searching against stored sort keys.
        /// <para/>
        /// See <a href="https://learn.microsoft.com/en-us/dotnet/core/extensions/globalization-icu#determine-if-your-app-is-using-icu">Determine if your app is using ICU</a>.
        /// </summary>
        public static bool IsICU { get; } = DetectICU();

        /// <summary>
        /// Indicates whether the active globalization backend is the legacy NLS implementation
        /// (always the case on .NET Framework, and the default on Windows when the app opts out of ICU)
        /// rather than ICU. This is the inverse of <see cref="IsICU"/>.
        /// </summary>
        public static bool IsNLS => !IsICU;

        // LUCENENET: Detects whether globalization is backed by ICU. See the link in the IsICU docs.
        private static bool DetectICU()
        {
            SortVersion sortVersion = CultureInfo.InvariantCulture.CompareInfo.Version;
            byte[] bytes = sortVersion.SortId.ToByteArray();
            int version = bytes[3] << 24 | bytes[2] << 16 | bytes[1] << 8 | bytes[0];
            return version != 0 && version == sortVersion.FullVersion;
        }
    }
}
