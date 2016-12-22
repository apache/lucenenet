using System;
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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using Codec = Lucene.Net.Codecs.Codec;
    using Counter = Lucene.Net.Util.Counter;
    using FieldInfosWriter = Lucene.Net.Codecs.FieldInfosWriter;
    using IOContext = Lucene.Net.Store.IOContext;

    /// <summary>
    /// this is a DocConsumer that gathers all fields under the
    /// same name, and calls per-field consumers to process field
    /// by field.  this class doesn't doesn't do any "real" work
    /// of its own: it just forwards the fields to a
    /// DocFieldConsumer.
    /// </summary>

    internal sealed class DocFieldProcessor : DocConsumer
    {
        internal readonly DocFieldConsumer Consumer;
        internal readonly StoredFieldsConsumer StoredConsumer;
        internal readonly Codec Codec;

        // Holds all fields seen in current doc
        internal DocFieldProcessorPerField[] fields = new DocFieldProcessorPerField[1];

        internal int FieldCount;

        // Hash table for all fields ever seen
        internal DocFieldProcessorPerField[] FieldHash = new DocFieldProcessorPerField[2];

        internal int HashMask = 1;
        internal int TotalFieldCount;

        internal int FieldGen;
        internal readonly DocumentsWriterPerThread.DocState DocState;

        internal readonly Counter BytesUsed;

        public DocFieldProcessor(DocumentsWriterPerThread docWriter, DocFieldConsumer consumer, StoredFieldsConsumer storedConsumer)
        {
            this.DocState = docWriter.docState;
            this.Codec = docWriter.Codec;
            this.BytesUsed = docWriter.bytesUsed;
            this.Consumer = consumer;
            this.StoredConsumer = storedConsumer;
        }

        public override void Flush(SegmentWriteState state)
        {
            IDictionary<string, DocFieldConsumerPerField> childFields = new Dictionary<string, DocFieldConsumerPerField>();
            ICollection<DocFieldConsumerPerField> fields = Fields();
            foreach (DocFieldConsumerPerField f in fields)
            {
                childFields[f.FieldInfo.Name] = f;
            }

            Debug.Assert(fields.Count == TotalFieldCount);

            StoredConsumer.Flush(state);
            Consumer.Flush(childFields, state);

            // Important to save after asking consumer to flush so
            // consumer can alter the FieldInfo* if necessary.  EG,
            // FreqProxTermsWriter does this with
            // FieldInfo.storePayload.
            FieldInfosWriter infosWriter = Codec.FieldInfosFormat.FieldInfosWriter;
            infosWriter.Write(state.Directory, state.SegmentInfo.Name, "", state.FieldInfos, IOContext.DEFAULT);
        }

        public override void Abort()
        {
            Exception th = null;

            foreach (DocFieldProcessorPerField field in FieldHash)
            {
                DocFieldProcessorPerField fieldNext = field;
                while (fieldNext != null)
                {
                    DocFieldProcessorPerField next = fieldNext.Next;
                    try
                    {
                        fieldNext.Abort();
                    }
                    catch (Exception t)
                    {
                        if (th == null)
                        {
                            th = t;
                        }
                    }
                    fieldNext = next;
                }
            }

            try
            {
                StoredConsumer.Abort();
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
                Consumer.Abort();
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
                if (th is Exception)
                {
                    throw (Exception)th;
                }
                // defensive code - we should not hit unchecked exceptions
                throw new Exception(th.Message, th);
            }
        }

        public ICollection<DocFieldConsumerPerField> Fields()
        {
            ICollection<DocFieldConsumerPerField> fields = new HashSet<DocFieldConsumerPerField>();
            for (int i = 0; i < FieldHash.Length; i++)
            {
                DocFieldProcessorPerField field = FieldHash[i];
                while (field != null)
                {
                    fields.Add(field.Consumer);
                    field = field.Next;
                }
            }
            Debug.Assert(fields.Count == TotalFieldCount);
            return fields;
        }

        private void Rehash()
        {
            int newHashSize = (FieldHash.Length * 2);
            Debug.Assert(newHashSize > FieldHash.Length);

            DocFieldProcessorPerField[] newHashArray = new DocFieldProcessorPerField[newHashSize];

            // Rehash
            int newHashMask = newHashSize - 1;
            for (int j = 0; j < FieldHash.Length; j++)
            {
                DocFieldProcessorPerField fp0 = FieldHash[j];
                while (fp0 != null)
                {
                    int hashPos2 = fp0.FieldInfo.Name.GetHashCode() & newHashMask;
                    DocFieldProcessorPerField nextFP0 = fp0.Next;
                    fp0.Next = newHashArray[hashPos2];
                    newHashArray[hashPos2] = fp0;
                    fp0 = nextFP0;
                }
            }

            FieldHash = newHashArray;
            HashMask = newHashMask;
        }

        public override void ProcessDocument(FieldInfos.Builder fieldInfos)
        {
            Consumer.StartDocument();
            StoredConsumer.StartDocument();

            FieldCount = 0;

            int thisFieldGen = FieldGen++;

            // Absorb any new fields first seen in this document.
            // Also absorb any changes to fields we had already
            // seen before (eg suddenly turning on norms or
            // vectors, etc.):

            foreach (IIndexableField field in DocState.Doc)
            {
                string fieldName = field.Name;

                // Make sure we have a PerField allocated
                int hashPos = fieldName.GetHashCode() & HashMask;
                DocFieldProcessorPerField fp = FieldHash[hashPos];
                while (fp != null && !fp.FieldInfo.Name.Equals(fieldName))
                {
                    fp = fp.Next;
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
                    fp.Next = FieldHash[hashPos];
                    FieldHash[hashPos] = fp;
                    TotalFieldCount++;

                    if (TotalFieldCount >= FieldHash.Length / 2)
                    {
                        Rehash();
                    }
                }
                else
                {
                    // need to addOrUpdate so that FieldInfos can update globalFieldNumbers
                    // with the correct DocValue type (LUCENE-5192)
                    FieldInfo fi = fieldInfos.AddOrUpdate(fieldName, field.FieldType);
                    Debug.Assert(fi == fp.FieldInfo, "should only have updated an existing FieldInfo instance");
                }

                if (thisFieldGen != fp.LastGen)
                {
                    // First time we're seeing this field for this doc
                    fp.FieldCount = 0;

                    if (FieldCount == fields.Length)
                    {
                        int newSize = fields.Length * 2;
                        DocFieldProcessorPerField[] newArray = new DocFieldProcessorPerField[newSize];
                        Array.Copy(fields, 0, newArray, 0, FieldCount);
                        fields = newArray;
                    }

                    fields[FieldCount++] = fp;
                    fp.LastGen = thisFieldGen;
                }

                fp.AddField(field);
                StoredConsumer.AddField(DocState.DocID, field, fp.FieldInfo);
            }

            // If we are writing vectors then we must visit
            // fields in sorted order so they are written in
            // sorted order.  TODO: we actually only need to
            // sort the subset of fields that have vectors
            // enabled; we could save [small amount of] CPU
            // here.
            ArrayUtil.IntroSort(fields, 0, FieldCount, fieldsComp);
            for (int i = 0; i < FieldCount; i++)
            {
                DocFieldProcessorPerField perField = fields[i];
                perField.Consumer.ProcessFields(perField.Fields, perField.FieldCount);
            }
        }

        private static readonly IComparer<DocFieldProcessorPerField> fieldsComp = new ComparatorAnonymousInnerClassHelper();

        private class ComparatorAnonymousInnerClassHelper : IComparer<DocFieldProcessorPerField>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(DocFieldProcessorPerField o1, DocFieldProcessorPerField o2)
            {
                return o1.FieldInfo.Name.CompareTo(o2.FieldInfo.Name);
            }
        }

        internal override void FinishDocument()
        {
            try
            {
                StoredConsumer.FinishDocument();
            }
            finally
            {
                Consumer.FinishDocument();
            }
        }
    }
}