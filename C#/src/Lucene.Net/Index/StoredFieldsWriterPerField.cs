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
    internal sealed class StoredFieldsWriterPerField : DocFieldConsumerPerField
    {

        internal readonly StoredFieldsWriterPerThread perThread;
        internal readonly FieldInfo fieldInfo;
        internal readonly DocumentsWriter.DocState docState;

        public StoredFieldsWriterPerField(StoredFieldsWriterPerThread perThread, FieldInfo fieldInfo)
        {
            this.perThread = perThread;
            this.fieldInfo = fieldInfo;
            docState = perThread.docState;
        }

        // Process all occurrences of a single field in one doc;
        // count is 1 if a given field occurs only once in the
        // Document, which is the "typical" case
        internal override void processFields(Fieldable[] fields, int count)
        {
            StoredFieldsWriter.PerDoc doc;
            if (perThread.doc == null)
            {
                doc = perThread.doc = perThread.storedFieldsWriter.getPerDoc();
                doc.docID = docState.docID;
                perThread.localFieldsWriter.SetFieldsStream(doc.fdt);
                System.Diagnostics.Debug.Assert(doc.numStoredFields == 0, "doc.numStoredFields=" + doc.numStoredFields);
                System.Diagnostics.Debug.Assert(0 == doc.fdt.Length());
                System.Diagnostics.Debug.Assert(0 == doc.fdt.GetFilePointer());
            }
            else
            {
                doc = perThread.doc;
                System.Diagnostics.Debug.Assert(doc.docID == docState.docID, "doc.docID=" + doc.docID + " docState.docID=" + docState.docID);
            }

            for (int i = 0; i < count; i++)
            {
                Fieldable field = fields[i];
                if (field.IsStored())
                {
                    perThread.localFieldsWriter.WriteField(fieldInfo, field);
                    System.Diagnostics.Debug.Assert(docState.TestPoint("StoredFieldsWriterPerField.processFields.writeField"));
                    doc.numStoredFields++;
                }
            }
        }

        internal override void abort()
        {
        }
    }
}
