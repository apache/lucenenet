using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using JCG = J2N.Collections.Generic;

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
    /// This is a <see cref="DocConsumer"/> that gathers all fields under the
    /// same name, and calls per-field consumers to process field
    /// by field.  This class doesn't doesn't do any "real" work
    /// of its own: it just forwards the fields to a
    /// <see cref="DocFieldConsumer"/>.
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Flush(SegmentWriteState state)
        {
            IDictionary<string, DocFieldConsumerPerField> childFields = new Dictionary<string, DocFieldConsumerPerField>();
            ICollection<DocFieldConsumerPerField> fields = Fields();
            foreach (DocFieldConsumerPerField f in fields)
            {
                childFields[f.FieldInfo.Name] = f;
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(fields.Count == totalFieldCount);

            storedConsumer.Flush(state);
            consumer.Flush(childFields, state);

            // Important to save after asking consumer to flush so
            // consumer can alter the FieldInfo* if necessary.  EG,
            // FreqProxTermsWriter does this with
            // FieldInfo.storePayload.
            FieldInfosWriter infosWriter = codec.FieldInfosFormat.FieldInfosWriter;
            infosWriter.Write(state.Directory, state.SegmentInfo.Name, "", state.FieldInfos, IOContext.DEFAULT);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Abort()
        {
            Exception th = null;

            foreach (DocFieldProcessorPerField field in fieldHash)
            {
                DocFieldProcessorPerField fieldNext = field;
                while (fieldNext != null)
                {
                    DocFieldProcessorPerField next = fieldNext.next;
                    try
                    {
                        fieldNext.Abort();
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        if (th is null)
                        {
                            th = t;
                        }
                    }
                    fieldNext = next;
                }
            }

            try
            {
                storedConsumer.Abort();
            }
            catch (Exception t) when (t.IsThrowable())
            {
                if (th is null)
                {
                    th = t;
                }
            }

            try
            {
                consumer.Abort();
            }
            catch (Exception t) when (t.IsThrowable())
            {
                if (th is null)
                {
                    th = t;
                }
            }

            // If any errors occured, throw it.
            if (th != null)
            {
                if (th.IsRuntimeException()) ExceptionDispatchInfo.Capture(th).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                if (th.IsError()) ExceptionDispatchInfo.Capture(th).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                // defensive code - we should not hit unchecked exceptions
                throw RuntimeException.Create(th);
            }
        }

        public ICollection<DocFieldConsumerPerField> Fields()
        {
            ICollection<DocFieldConsumerPerField> fields = new JCG.HashSet<DocFieldConsumerPerField>();
            for (int i = 0; i < fieldHash.Length; i++)
            {
                DocFieldProcessorPerField field = fieldHash[i];
                while (field != null)
                {
                    fields.Add(field.consumer);
                    field = field.next;
                }
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(fields.Count == totalFieldCount);
            return fields;
        }

        private void Rehash()
        {
            int newHashSize = (fieldHash.Length * 2);
            if (Debugging.AssertsEnabled) Debugging.Assert(newHashSize > fieldHash.Length);

            DocFieldProcessorPerField[] newHashArray = new DocFieldProcessorPerField[newHashSize];

            // Rehash
            int newHashMask = newHashSize - 1;
            for (int j = 0; j < fieldHash.Length; j++)
            {
                DocFieldProcessorPerField fp0 = fieldHash[j];
                while (fp0 != null)
                {
                    int hashPos2 = fp0.fieldInfo.Name.GetHashCode() & newHashMask;
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
                string fieldName = field.Name;

                // Make sure we have a PerField allocated
                int hashPos = fieldName.GetHashCode() & hashMask;
                DocFieldProcessorPerField fp = fieldHash[hashPos];
                while (fp != null && !fp.fieldInfo.Name.Equals(fieldName, StringComparison.Ordinal))
                {
                    fp = fp.next;
                }

                if (fp is null)
                {
                    // TODO FI: we need to genericize the "flags" that a
                    // field holds, and, how these flags are merged; it
                    // needs to be more "pluggable" such that if I want
                    // to have a new "thing" my Fields can do, I can
                    // easily add it
                    FieldInfo fi = fieldInfos.AddOrUpdate(fieldName, field.IndexableFieldType);

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
                    // need to addOrUpdate so that FieldInfos can update globalFieldNumbers
                    // with the correct DocValue type (LUCENE-5192)
                    FieldInfo fi = fieldInfos.AddOrUpdate(fieldName, field.IndexableFieldType);
                    if (Debugging.AssertsEnabled) Debugging.Assert(fi == fp.fieldInfo, "should only have updated an existing FieldInfo instance");
                }

                if (thisFieldGen != fp.lastGen)
                {
                    // First time we're seeing this field for this doc
                    fp.fieldCount = 0;

                    if (fieldCount == fields.Length)
                    {
                        int newSize = fields.Length * 2;
                        DocFieldProcessorPerField[] newArray = new DocFieldProcessorPerField[newSize];
                        Arrays.Copy(fields, 0, newArray, 0, fieldCount);
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
            ArrayUtil.IntroSort(fields, 0, fieldCount, fieldsComp);
            for (int i = 0; i < fieldCount; i++)
            {
                DocFieldProcessorPerField perField = fields[i];
                perField.consumer.ProcessFields(perField.fields, perField.fieldCount);
            }
        }

        private static readonly IComparer<DocFieldProcessorPerField> fieldsComp = Comparer<DocFieldProcessorPerField>.Create((o1, o2) => o1.fieldInfo.Name.CompareToOrdinal(o2.fieldInfo.Name));

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal override void FinishDocument()
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