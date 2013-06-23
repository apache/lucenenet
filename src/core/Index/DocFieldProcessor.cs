/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Index
{

    /// <summary> This is a DocConsumer that gathers all fields under the
    /// same name, and calls per-field consumers to process field
    /// by field.  This class doesn't doesn't do any "real" work
    /// of its own: it just forwards the fields to a
    /// DocFieldConsumer.
    /// </summary>

    internal sealed class DocFieldProcessor : DocConsumer
    {
        internal readonly DocFieldConsumer consumer;
        internal readonly StoredFieldsConsumer storedConsumer;
        internal readonly Codec codec;

        // Holds all fields seen in current doc
        internal DocFieldProcessorPerField[] fields = new DocFieldProcessorPerField[1];
        internal int fieldCount;

        // Hash table for all fields ever seen
        internal DocFieldProcessorPerField[] fieldHash = new DocFieldProcessorPerField[2];
        internal int hashMask = 1;
        internal int totalFieldCount;

        internal int fieldGen;
        internal readonly DocumentsWriterPerThread.DocState docState;

        internal readonly Counter bytesUsed;

        public DocFieldProcessor(DocumentsWriterPerThread docWriter, DocFieldConsumer consumer, StoredFieldsConsumer storedConsumer)
        {
            this.docState = docWriter.docState;
            this.codec = docWriter.codec;
            this.bytesUsed = docWriter.bytesUsed;
            this.consumer = consumer;
            this.storedConsumer = storedConsumer;
        }

        public override void Flush(SegmentWriteState state)
        {
            IDictionary<String, DocFieldConsumerPerField> childFields = new HashMap<String, DocFieldConsumerPerField>();
            ICollection<DocFieldConsumerPerField> fields = this.Fields;

            foreach (DocFieldConsumerPerField f in fields)
            {
                childFields[f.FieldInfo.name] = f;
            }

            //assert fields.size() == totalFieldCount;

            storedConsumer.Flush(state);
            consumer.Flush(childFields, state);

            // Important to save after asking consumer to flush so
            // consumer can alter the FieldInfo* if necessary.  EG,
            // FreqProxTermsWriter does this with
            // FieldInfo.storePayload.
            FieldInfosWriter infosWriter = codec.FieldInfosFormat().FieldInfosWriter;
            infosWriter.Write(state.directory, state.segmentInfo.name, state.fieldInfos, IOContext.DEFAULT);
        }

        public override void Abort()
        {
            Exception th = null;

            foreach (DocFieldProcessorPerField field in fieldHash)
            {
                DocFieldProcessorPerField fieldWritable = field; // .NET port: foreach range variables not mutable like in java
                while (fieldWritable != null)
                {
                    DocFieldProcessorPerField next = fieldWritable.next;
                    try
                    {
                        fieldWritable.Abort();
                    }
                    catch (Exception t)
                    {
                        if (th == null)
                        {
                            th = t;
                        }
                    }
                    fieldWritable = next;
                }
            }

            try
            {
                storedConsumer.Abort();
            }
            catch (Exception t)
            {
                if (th == null)
                {
                    th = t;
                }
            }

            try
            {
                consumer.Abort();
            }
            catch (Exception t)
            {
                if (th == null)
                {
                    th = t;
                }
            }

            // If any errors occured, throw it.
            if (th != null)
            {
                //if (th instanceof RuntimeException) throw (RuntimeException) th;
                //if (th instanceof Error) throw (Error) th;
                // defensive code - we should not hit unchecked exceptions
                throw th;
            }
        }

        public ICollection<DocFieldConsumerPerField> Fields
        {
            get
            {
                ICollection<DocFieldConsumerPerField> fields = new HashSet<DocFieldConsumerPerField>();
                for (int i = 0; i < fieldHash.Length; i++)
                {
                    DocFieldProcessorPerField field = fieldHash[i];
                    while (field != null)
                    {
                        fields.Add(field.consumer);
                        field = field.next;
                    }
                }
                //assert fields.size() == totalFieldCount;
                return fields;
            }
        }

        public override void DoAfterFlush()
        {
            fieldHash = new DocFieldProcessorPerField[2];
            hashMask = 1;
            totalFieldCount = 0;
        }

        private void Rehash()
        {
            int newHashSize = (fieldHash.Length * 2);
            //assert newHashSize > fieldHash.length;

            DocFieldProcessorPerField[] newHashArray = new DocFieldProcessorPerField[newHashSize];

            // Rehash
            int newHashMask = newHashSize - 1;
            for (int j = 0; j < fieldHash.Length; j++)
            {
                DocFieldProcessorPerField fp0 = fieldHash[j];
                while (fp0 != null)
                {
                    int hashPos2 = fp0.fieldInfo.name.GetHashCode() & newHashMask;
                    DocFieldProcessorPerField nextFP0 = fp0.next;
                    fp0.next = newHashArray[hashPos2];
                    newHashArray[hashPos2] = fp0;
                    fp0 = nextFP0;
                }
            }

            fieldHash = newHashArray;
            hashMask = newHashMask;
        }

        public override void ProcessDocument(FieldInfos.Builder fieldInfos)
        {
            consumer.StartDocument();
            storedConsumer.StartDocument();

            fieldCount = 0;

            int thisFieldGen = fieldGen++;

            // Absorb any new fields first seen in this document.
            // Also absorb any changes to fields we had already
            // seen before (eg suddenly turning on norms or
            // vectors, etc.):

            foreach (IIndexableField field in docState.doc)
            {
                String fieldName = field.Name;

                // Make sure we have a PerField allocated
                int hashPos = fieldName.GetHashCode() & hashMask;
                DocFieldProcessorPerField fp = fieldHash[hashPos];
                while (fp != null && !fp.fieldInfo.name.Equals(fieldName))
                {
                    fp = fp.next;
                }

                if (fp == null)
                {

                    // TODO FI: we need to genericize the "flags" that a
                    // field holds, and, how these flags are merged; it
                    // needs to be more "pluggable" such that if I want
                    // to have a new "thing" my Fields can do, I can
                    // easily add it
                    FieldInfo fi = fieldInfos.AddOrUpdate(fieldName, field.FieldType);

                    fp = new DocFieldProcessorPerField(this, fi);
                    fp.next = fieldHash[hashPos];
                    fieldHash[hashPos] = fp;
                    totalFieldCount++;

                    if (totalFieldCount >= fieldHash.Length / 2)
                    {
                        Rehash();
                    }
                }
                else
                {
                    fp.fieldInfo.Update(field.FieldType);
                }

                if (thisFieldGen != fp.lastGen)
                {

                    // First time we're seeing this field for this doc
                    fp.fieldCount = 0;

                    if (fieldCount == fields.Length)
                    {
                        int newSize = fields.Length * 2;
                        DocFieldProcessorPerField[] newArray = new DocFieldProcessorPerField[newSize];
                        Array.Copy(fields, 0, newArray, 0, fieldCount);
                        fields = newArray;
                    }

                    fields[fieldCount++] = fp;
                    fp.lastGen = thisFieldGen;
                }

                fp.AddField(field);
                storedConsumer.AddField(docState.docID, field, fp.fieldInfo);
            }

            // If we are writing vectors then we must visit
            // fields in sorted order so they are written in
            // sorted order.  TODO: we actually only need to
            // sort the subset of fields that have vectors
            // enabled; we could save [small amount of] CPU
            // here.
            ArrayUtil.QuickSort(fields, 0, fieldCount, fieldsComp);
            for (int i = 0; i < fieldCount; i++)
            {
                DocFieldProcessorPerField perField = fields[i];
                perField.consumer.ProcessFields(perField.fields, perField.fieldCount);
            }

            if (docState.maxTermPrefix != null && docState.infoStream.IsEnabled("IW"))
            {
                docState.infoStream.Message("IW", "WARNING: document contains at least one immense term (whose UTF8 encoding is longer than the max length " + DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8 + "), all of which were skipped.  Please correct the analyzer to not produce such terms.  The prefix of the first immense term is: '" + docState.maxTermPrefix + "...'");
                docState.maxTermPrefix = null;
            }
        }

        private sealed class AnonymousFieldsComparer : IComparer<DocFieldProcessorPerField>
        {
            public int Compare(DocFieldProcessorPerField o1, DocFieldProcessorPerField o2)
            {
                return o1.fieldInfo.name.CompareTo(o2.fieldInfo.name);
            }
        }

        private static readonly IComparer<DocFieldProcessorPerField> fieldsComp = new AnonymousFieldsComparer();

        public override void FinishDocument()
        {
            try
            {
                storedConsumer.FinishDocument();
            }
            finally
            {
                consumer.FinishDocument();
            }
        }
    }
}