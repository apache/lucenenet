/**
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

using System.Collections.Generic;

namespace Lucene.Net.Index
{
    internal abstract class InvertedDocConsumer
    {
        /// <summary>  Add a new thread </summary>
        internal abstract InvertedDocConsumerPerThread addThread(DocInverterPerThread docInverterPerThread);

        /// <summary>  Abort (called after hitting AbortException) </summary>
        internal abstract void abort();

        /// <summary>  Flush a new segment </summary>
        internal abstract void flush(IDictionary<object,ICollection<object>> threadsAndFields, DocumentsWriter.FlushState state);

        /// <summary>  Close doc stores </summary>
        internal abstract void closeDocStore(DocumentsWriter.FlushState state);

        /// <summary>  Attempt to free RAM, returning true if any RAM was freed </summary>
        internal abstract bool freeRAM();

        internal FieldInfos fieldInfos;

        internal virtual void setFieldInfos(FieldInfos fieldInfos)
        {
            this.fieldInfos = fieldInfos;
        }
    }
}
