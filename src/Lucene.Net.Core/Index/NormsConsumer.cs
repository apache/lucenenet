using System.Collections.Generic;
using System.Diagnostics;

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

    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType_e;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using NormsFormat = Lucene.Net.Codecs.NormsFormat;

    // TODO FI: norms could actually be stored as doc store

    /// <summary>
    /// Writes norms.  Each thread X field accumulates the norms
    ///  for the doc/fields it saw, then the flush method below
    ///  merges all of these together into a single _X.nrm file.
    /// </summary>

    internal sealed class NormsConsumer : InvertedDocEndConsumer
    {
        internal override void Abort()
        {
        }

        internal override void Flush(IDictionary<string, InvertedDocEndConsumerPerField> fieldsToFlush, SegmentWriteState state)
        {
            bool success = false;
            DocValuesConsumer normsConsumer = null;
            try
            {
                if (state.FieldInfos.HasNorms())
                {
                    NormsFormat normsFormat = state.SegmentInfo.Codec.NormsFormat;
                    Debug.Assert(normsFormat != null);
                    normsConsumer = normsFormat.NormsConsumer(state);

                    foreach (FieldInfo fi in state.FieldInfos)
                    {
                        NormsConsumerPerField toWrite = (NormsConsumerPerField)fieldsToFlush[fi.Name];
                        // we must check the final value of omitNorms for the fieldinfo, it could have
                        // changed for this field since the first time we added it.
                        if (!fi.OmitsNorms())
                        {
                            if (toWrite != null && !toWrite.Empty)
                            {
                                toWrite.Flush(state, normsConsumer);
                                Debug.Assert(fi.NormType == DocValuesType.NUMERIC);
                            }
                            else if (fi.Indexed)
                            {
                                Debug.Assert(fi.NormType == null, "got " + fi.NormType + "; field=" + fi.Name);
                            }
                        }
                    }
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(normsConsumer);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(normsConsumer);
                }
            }
        }

        internal override void FinishDocument()
        {
        }

        internal override void StartDocument()
        {
        }

        internal override InvertedDocEndConsumerPerField AddField(DocInverterPerField docInverterPerField, FieldInfo fieldInfo)
        {
            return new NormsConsumerPerField(docInverterPerField, fieldInfo, this);
        }
    }
}