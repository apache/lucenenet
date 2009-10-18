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

using Fieldable = Lucene.Net.Documents.Fieldable;

namespace Lucene.Net.Index
{
    internal sealed class DocFieldConsumersPerField : DocFieldConsumerPerField
    {

        internal readonly DocFieldConsumerPerField one;
        internal readonly DocFieldConsumerPerField two;
        internal readonly DocFieldConsumersPerThread perThread;

        public DocFieldConsumersPerField(DocFieldConsumersPerThread perThread, DocFieldConsumerPerField one, DocFieldConsumerPerField two)
        {
            this.perThread = perThread;
            this.one = one;
            this.two = two;
        }

        internal override void processFields(Fieldable[] fields, int count)
        {
            one.processFields(fields, count);
            two.processFields(fields, count);
        }

        internal override void abort()
        {
            try
            {
                one.abort();
            }
            finally
            {
                two.abort();
            }
        }
    }
}
