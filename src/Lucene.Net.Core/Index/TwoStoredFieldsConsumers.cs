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

    /// <summary>
    /// Just switches between two <seealso cref="DocFieldConsumer"/>s. </summary>

    internal class TwoStoredFieldsConsumers : StoredFieldsConsumer
    {
        private readonly StoredFieldsConsumer first;
        private readonly StoredFieldsConsumer fecond;

        public TwoStoredFieldsConsumers(StoredFieldsConsumer first, StoredFieldsConsumer second)
        {
            this.first = first;
            this.fecond = second;
        }

        public override void AddField(int docID, IndexableField field, FieldInfo fieldInfo)
        {
            first.AddField(docID, field, fieldInfo);
            fecond.AddField(docID, field, fieldInfo);
        }

        public override void Flush(SegmentWriteState state) // LUCENENET NOTE: original was internal, but other implementations require public
        {
            first.Flush(state);
            fecond.Flush(state);
        }

        public override void Abort() // LUCENENET NOTE: original was internal, but other implementations require public
        {
            try
            {
                first.Abort();
            }
            catch (Exception)
            {
            }
            try
            {
                fecond.Abort();
            }
            catch (Exception)
            {
            }
        }

        public override void StartDocument() // LUCENENET NOTE: original was internal, but other implementations require public
        {
            first.StartDocument();
            fecond.StartDocument();
        }

        internal override void FinishDocument()
        {
            first.FinishDocument();
            fecond.FinishDocument();
        }
    }
}