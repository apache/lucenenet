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

using IndexOutput = Lucene.Net.Store.IndexOutput;

namespace Lucene.Net.Index
{
    internal sealed class StoredFieldsWriterPerThread : DocFieldConsumerPerThread
    {

        internal readonly FieldsWriter localFieldsWriter;
        internal readonly StoredFieldsWriter storedFieldsWriter;
        internal readonly DocumentsWriter.DocState docState;

        internal StoredFieldsWriter.PerDoc doc;

        public StoredFieldsWriterPerThread(DocFieldProcessorPerThread docFieldProcessorPerThread, StoredFieldsWriter storedFieldsWriter)
        {
            this.storedFieldsWriter = storedFieldsWriter;
            this.docState = docFieldProcessorPerThread.docState;
            localFieldsWriter = new FieldsWriter((IndexOutput)null, (IndexOutput)null, storedFieldsWriter.fieldInfos);
        }

        internal override void startDocument()
        {
            if (doc != null)
            {
                // Only happens if previous document hit non-aborting
                // exception while writing stored fields into
                // localFieldsWriter:
                doc.reset();
                doc.docID = docState.docID;
            }
        }

        internal override DocumentsWriter.DocWriter finishDocument()
        {
            // If there were any stored fields in this doc, doc will
            // be non-null; else it's null.
            try
            {
                return doc;
            }
            finally
            {
                doc = null;
            }
        }

        internal override void abort()
        {
            if (doc != null)
            {
                doc.Abort();
                doc = null;
            }
        }

        internal override DocFieldConsumerPerField addField(FieldInfo fieldInfo)
        {
            return new StoredFieldsWriterPerField(this, fieldInfo);
        }
    }
}
