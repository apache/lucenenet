using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene3x
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

    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Lucene3x ReadOnly <see cref="SegmentInfoFormat"/> implementation.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [Obsolete("(4.0) this is only used to read indexes created before 4.0.")]
    public class Lucene3xSegmentInfoFormat : SegmentInfoFormat
    {
        private readonly SegmentInfoReader reader = new Lucene3xSegmentInfoReader();

        /// <summary>
        /// This format adds optional per-segment String
        /// diagnostics storage, and switches userData to Map.
        /// </summary>
        public static readonly int FORMAT_DIAGNOSTICS = -9;

        /// <summary>
        /// Each segment records whether it has term vectors. </summary>
        public static readonly int FORMAT_HAS_VECTORS = -10;

        /// <summary>
        /// Each segment records the Lucene version that created it. </summary>
        public static readonly int FORMAT_3_1 = -11;

        /// <summary>
        /// Extension used for saving each SegmentInfo, once a 3.x
        /// index is first committed to with 4.0.
        /// </summary>
        public static readonly string UPGRADED_SI_EXTENSION = "si";

        public static readonly string UPGRADED_SI_CODEC_NAME = "Lucene3xSegmentInfo";
        public static readonly int UPGRADED_SI_VERSION_START = 0;
        public static readonly int UPGRADED_SI_VERSION_CURRENT = UPGRADED_SI_VERSION_START;

        public override SegmentInfoReader SegmentInfoReader => reader;

        public override SegmentInfoWriter SegmentInfoWriter => throw UnsupportedOperationException.Create("this codec can only be used for reading");

        // only for backwards compat
        public static readonly string DS_OFFSET_KEY = typeof(Lucene3xSegmentInfoFormat).Name + ".dsoffset";

        public static readonly string DS_NAME_KEY = typeof(Lucene3xSegmentInfoFormat).Name + ".dsname";
        public static readonly string DS_COMPOUND_KEY = typeof(Lucene3xSegmentInfoFormat).Name + ".dscompound";
        public static readonly string NORMGEN_KEY = typeof(Lucene3xSegmentInfoFormat).Name + ".normgen";
        public static readonly string NORMGEN_PREFIX = typeof(Lucene3xSegmentInfoFormat).Name + ".normfield";

        /// <returns> If this segment shares stored fields &amp; vectors, this
        ///         offset is where in that file this segment's docs begin.  </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDocStoreOffset(SegmentInfo si)
        {
            string v = si.GetAttribute(DS_OFFSET_KEY);
            return v is null ? -1 : Convert.ToInt32(v, CultureInfo.InvariantCulture);
        }

        /// <returns> Name used to derive fields/vectors file we share with other segments. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDocStoreSegment(SegmentInfo si)
        {
            string v = si.GetAttribute(DS_NAME_KEY);
            return v ?? si.Name;
        }

        /// <returns> Whether doc store files are stored in compound file (*.cfx). </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetDocStoreIsCompoundFile(SegmentInfo si)
        {
            string v = si.GetAttribute(DS_COMPOUND_KEY);
            return v is null ? false : Convert.ToBoolean(v, CultureInfo.InvariantCulture);
        }
    }
}