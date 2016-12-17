using System;

namespace Lucene.Net.Codecs.Compressing
{
    using Lucene.Net.Randomized.Generators;

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

    using DummyCompressingCodec = Lucene.Net.Codecs.Compressing.dummy.DummyCompressingCodec;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;

    //using RandomInts = com.carrotsearch.randomizedtesting.generators.RandomInts;

    /// <summary>
    /// A codec that uses <seealso cref="CompressingStoredFieldsFormat"/> for its stored
    /// fields and delegates to <seealso cref="Lucene46Codec"/> for everything else.
    /// </summary>
    public abstract class CompressingCodec : FilterCodec
    {
        /// <summary>
        /// Create a random instance.
        /// </summary>
        public static CompressingCodec RandomInstance(Random random, int chunkSize, bool withSegmentSuffix)
        {
            switch (random.Next(4))
            {
                case 0:
                    return new FastCompressingCodec(chunkSize, withSegmentSuffix);

                case 1:
                    return new FastDecompressionCompressingCodec(chunkSize, withSegmentSuffix);

                case 2:
                    return new HighCompressionCompressingCodec(chunkSize, withSegmentSuffix);

                case 3:
                    return new DummyCompressingCodec(chunkSize, withSegmentSuffix);

                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Creates a random <seealso cref="CompressingCodec"/> that is using an empty segment
        /// suffix
        /// </summary>
        public static CompressingCodec RandomInstance(Random random)
        {
            return RandomInstance(random, RandomInts.NextIntBetween(random, 1, 500), false);
        }

        /// <summary>
        /// Creates a random <seealso cref="CompressingCodec"/> that is using a segment suffix
        /// </summary>
        public static CompressingCodec RandomInstance(Random random, bool withSegmentSuffix)
        {
            return RandomInstance(random, RandomInts.NextIntBetween(random, 1, 500), withSegmentSuffix);
        }

        private readonly CompressingStoredFieldsFormat StoredFieldsFormat_Renamed;
        private readonly CompressingTermVectorsFormat TermVectorsFormat_Renamed;

        /// <summary>
        /// Creates a compressing codec with a given segment suffix
        /// </summary>
        protected CompressingCodec(string name, string segmentSuffix, CompressionMode compressionMode, int chunkSize)
            : base(name, new Lucene46Codec())
        {
            this.StoredFieldsFormat_Renamed = new CompressingStoredFieldsFormat(name, segmentSuffix, compressionMode, chunkSize);
            this.TermVectorsFormat_Renamed = new CompressingTermVectorsFormat(name, segmentSuffix, compressionMode, chunkSize);
        }

        /// <summary>
        /// Creates a compressing codec with an empty segment suffix
        /// </summary>
        protected CompressingCodec(string name, CompressionMode compressionMode, int chunkSize)
            : this(name, "", compressionMode, chunkSize)
        {
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get { return StoredFieldsFormat_Renamed; }
        }

        public override TermVectorsFormat TermVectorsFormat
        {
            get { return TermVectorsFormat_Renamed; }
        }

        public override string ToString()
        {
            return Name + "(storedFieldsFormat=" + StoredFieldsFormat_Renamed + ", termVectorsFormat=" + TermVectorsFormat_Renamed + ")";
        }
    }
}