using Lucene.Net.Diagnostics;
using System;

namespace Lucene.Net.Index
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

    using IndexReaderWarmer = Lucene.Net.Index.IndexWriter.IndexReaderWarmer;
    using InfoStream = Lucene.Net.Util.InfoStream;

    /// <summary>
    /// A very simple merged segment warmer that just ensures
    /// data structures are initialized.
    /// </summary>
    public class SimpleMergedSegmentWarmer : IndexReaderWarmer
    {
        private readonly InfoStream infoStream;

        /// <summary>
        /// Creates a new <see cref="SimpleMergedSegmentWarmer"/> </summary>
        /// <param name="infoStream"> <see cref="InfoStream"/> to log statistics about warming. </param>
        public SimpleMergedSegmentWarmer(InfoStream infoStream)
        {
            this.infoStream = infoStream;
        }

        public override void Warm(AtomicReader reader)
        {
            long startTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            int indexedCount = 0;
            int docValuesCount = 0;
            int normsCount = 0;
            foreach (FieldInfo info in reader.FieldInfos)
            {
                if (info.IsIndexed)
                {
                    reader.GetTerms(info.Name);
                    indexedCount++;

                    if (info.HasNorms)
                    {
                        reader.GetNormValues(info.Name);
                        normsCount++;
                    }
                }

                if (info.HasDocValues)
                {
                    switch (info.DocValuesType)
                    {
                        case DocValuesType.NUMERIC:
                            reader.GetNumericDocValues(info.Name);
                            break;

                        case DocValuesType.BINARY:
                            reader.GetBinaryDocValues(info.Name);
                            break;

                        case DocValuesType.SORTED:
                            reader.GetSortedDocValues(info.Name);
                            break;

                        case DocValuesType.SORTED_SET:
                            reader.GetSortedSetDocValues(info.Name);
                            break;

                        default:
                            if (Debugging.AssertsEnabled) Debugging.Assert(false); // unknown dv type
                            break;
                    }
                    docValuesCount++;
                }
            }

            reader.Document(0);
            reader.GetTermVectors(0);

            if (infoStream.IsEnabled("SMSW"))
            {
                infoStream.Message("SMSW",
                    "Finished warming segment: " + reader +
                    ", indexed=" + indexedCount +
                    ", docValues=" + docValuesCount +
                    ", norms=" + normsCount +
                    ", time=" + ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - startTime)); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            }
        }
    }
}