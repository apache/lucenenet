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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// Holds all per thread, per field state.
    /// </summary>

    internal sealed class DocFieldProcessorPerField
    {
        internal readonly DocFieldConsumerPerField Consumer;
        internal readonly FieldInfo FieldInfo;

        internal DocFieldProcessorPerField Next;
        internal int LastGen = -1;

        internal int FieldCount;
        internal IIndexableField[] Fields = new IIndexableField[1];

        public DocFieldProcessorPerField(DocFieldProcessor docFieldProcessor, FieldInfo fieldInfo)
        {
            this.Consumer = docFieldProcessor.consumer.AddField(fieldInfo);
            this.FieldInfo = fieldInfo;
        }

        public void AddField(IIndexableField field)
        {
            if (FieldCount == Fields.Length)
            {
                int newSize = ArrayUtil.Oversize(FieldCount + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                IIndexableField[] newArray = new IIndexableField[newSize];
                Array.Copy(Fields, 0, newArray, 0, FieldCount);
                Fields = newArray;
            }

            Fields[FieldCount++] = field;
        }

        public void Abort()
        {
            Consumer.Abort();
        }
    }
}