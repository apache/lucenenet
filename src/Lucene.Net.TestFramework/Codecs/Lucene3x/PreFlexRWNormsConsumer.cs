using System;
using Debug = Lucene.Net.Diagnostics.Debug; // LUCENENET NOTE: We cannot use System.Diagnostics.Debug because those calls will be optimized out of the release!

namespace Lucene.Net.Codecs.Lucene3x
{
    using System.Collections.Generic;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;

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

    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Writes and Merges Lucene 3.x norms format
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal class PreFlexRWNormsConsumer : DocValuesConsumer
    {
        /// <summary>
        /// norms header placeholder </summary>
        private static readonly sbyte[] NORMS_HEADER = new sbyte[] { (sbyte)'N', (sbyte)'R', (sbyte)'M', -1 };

        /// <summary>
        /// Extension of norms file </summary>
        private const string NORMS_EXTENSION = "nrm";

        /// <summary>
        /// Extension of separate norms file </summary>
        [Obsolete("Only for reading existing 3.x indexes")]
        private const string SEPARATE_NORMS_EXTENSION = "s";

        private readonly IndexOutput @out;
        private int lastFieldNumber = -1; // only for assert

        public PreFlexRWNormsConsumer(Directory directory, string segment, IOContext context)
        {
            string normsFileName = IndexFileNames.SegmentFileName(segment, "", NORMS_EXTENSION);
            bool success = false;
            IndexOutput output = null;
            try
            {
                output = directory.CreateOutput(normsFileName, context);
                // output.WriteBytes(NORMS_HEADER, 0, NORMS_HEADER.Length);
                foreach (var @sbyte in NORMS_HEADER)
                {
                    output.WriteByte((byte)@sbyte);
                }
                @out = output;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(output);
                }
            }
        }

        public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
        {
            Debug.Assert(field.Number > lastFieldNumber, "writing norms fields out of order" + lastFieldNumber + " -> " + field.Number);
            foreach (var n in values)
            {
                if (((sbyte)(byte)(long)n) < sbyte.MinValue || ((sbyte)(byte)(long)n) > sbyte.MaxValue)
                {
                    throw new System.NotSupportedException("3.x cannot index norms that won't fit in a byte, got: " + ((sbyte)(byte)(long)n));
                }
                @out.WriteByte((byte)(sbyte)n);
            }
            lastFieldNumber = field.Number;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IOUtils.Dispose(@out);
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            throw new InvalidOperationException(); // LUCENENET TODO: This should be AssertionError (AssertionException)
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
        {
            throw new InvalidOperationException(); // LUCENENET TODO: This should be AssertionError (AssertionException)
        }

        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
        {
            throw new InvalidOperationException(); // LUCENENET TODO: This should be AssertionError (AssertionException)
        }
    }
}