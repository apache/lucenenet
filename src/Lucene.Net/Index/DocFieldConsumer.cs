using System.Collections.Generic;

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

    internal abstract class DocFieldConsumer
    {
        /// <summary>
        /// Called when <see cref="DocumentsWriterPerThread"/> decides to create a new
        /// segment
        /// </summary>
        internal abstract void Flush(IDictionary<string, DocFieldConsumerPerField> fieldsToFlush, SegmentWriteState state);

        /// <summary>
        /// Called when an aborting exception is hit </summary>
        internal abstract void Abort();

        public abstract void StartDocument();

        public abstract DocFieldConsumerPerField AddField(FieldInfo fi);

        public abstract void FinishDocument();
    }
}
