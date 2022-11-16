using System;
using Lucene.Net.Support;

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
        internal readonly DocFieldConsumerPerField consumer;
        internal readonly FieldInfo fieldInfo;

        internal DocFieldProcessorPerField next;
        internal int lastGen = -1;

        internal int fieldCount;
        internal IIndexableField[] fields = new IIndexableField[1];

        public DocFieldProcessorPerField(DocFieldProcessor docFieldProcessor, FieldInfo fieldInfo)
        {
            this.consumer = docFieldProcessor.consumer.AddField(fieldInfo);
            this.fieldInfo = fieldInfo;
        }

        public void AddField(IIndexableField field)
        {
            if (fieldCount == fields.Length)
            {
                int newSize = ArrayUtil.Oversize(fieldCount + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                IIndexableField[] newArray = new IIndexableField[newSize];
                Arrays.Copy(fields, 0, newArray, 0, fieldCount);
                fields = newArray;
            }

            fields[fieldCount++] = field;
        }

        public void Abort()
        {
            consumer.Abort();
        }
    }
}