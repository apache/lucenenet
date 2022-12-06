using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs
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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using IBits = Lucene.Net.Util.IBits;
    using Document = Documents.Document;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IIndexableField = Lucene.Net.Index.IIndexableField;
    using MergeState = Lucene.Net.Index.MergeState;

    /// <summary>
    /// Codec API for writing stored fields:
    /// <para/>
    /// <list type="number">
    ///   <item><description>For every document, <see cref="StartDocument(int)"/> is called,
    ///       informing the Codec how many fields will be written.</description></item>
    ///   <item><description><see cref="WriteField(FieldInfo, IIndexableField)"/> is called for
    ///       each field in the document.</description></item>
    ///   <item><description>After all documents have been written, <see cref="Finish(FieldInfos, int)"/>
    ///       is called for verification/sanity-checks.</description></item>
    ///   <item><description>Finally the writer is disposed (<see cref="Dispose(bool)"/>)</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class StoredFieldsWriter : IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected StoredFieldsWriter()
        {
        }

        /// <summary>
        /// Called before writing the stored fields of the document.
        /// <see cref="WriteField(FieldInfo, IIndexableField)"/> will be called
        /// <paramref name="numStoredFields"/> times. Note that this is
        /// called even if the document has no stored fields, in
        /// this case <paramref name="numStoredFields"/> will be zero.
        /// </summary>
        public abstract void StartDocument(int numStoredFields);

        /// <summary>
        /// Called when a document and all its fields have been added. </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void FinishDocument()
        {
        }

        /// <summary>
        /// Writes a single stored field. </summary>
        public abstract void WriteField(FieldInfo info, IIndexableField field);

        /// <summary>
        /// Aborts writing entirely, implementation should remove
        /// any partially-written files, etc.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public abstract void Abort();

        /// <summary>
        /// Called before <see cref="Dispose()"/>, passing in the number
        /// of documents that were written. Note that this is
        /// intentionally redundant (equivalent to the number of
        /// calls to <see cref="StartDocument(int)"/>, but a <see cref="Codec"/> should
        /// check that this is the case to detect the bug described
        /// in LUCENE-1282.
        /// </summary>
        public abstract void Finish(FieldInfos fis, int numDocs);

        /// <summary>
        /// Merges in the stored fields from the readers in
        /// <paramref name="mergeState"/>. The default implementation skips
        /// over deleted documents, and uses <see cref="StartDocument(int)"/>,
        /// <see cref="WriteField(FieldInfo, IIndexableField)"/>, and <see cref="Finish(FieldInfos, int)"/>,
        /// returning the number of documents that were written.
        /// Implementations can override this method for more sophisticated
        /// merging (bulk-byte copying, etc).
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual int Merge(MergeState mergeState)
        {
            int docCount = 0;
            foreach (AtomicReader reader in mergeState.Readers)
            {
                int maxDoc = reader.MaxDoc;
                IBits liveDocs = reader.LiveDocs;
                for (int i = 0; i < maxDoc; i++)
                {
                    if (liveDocs != null && !liveDocs.Get(i))
                    {
                        // skip deleted docs
                        continue;
                    }
                    // TODO: this could be more efficient using
                    // FieldVisitor instead of loading/writing entire
                    // doc; ie we just have to renumber the field number
                    // on the fly?
                    // NOTE: it's very important to first assign to doc then pass it to
                    // fieldsWriter.addDocument; see LUCENE-1282
                    Document doc = reader.Document(i);
                    AddDocument(doc, mergeState.FieldInfos);
                    docCount++;
                    mergeState.CheckAbort.Work(300);
                }
            }
            Finish(mergeState.FieldInfos, docCount);
            return docCount;
        }

        /// <summary>
        /// Sugar method for <see cref="StartDocument(int)"/> + <see cref="WriteField(FieldInfo, IIndexableField)"/> 
        /// for every stored field in the document. </summary>
        protected void AddDocument<T1>(IEnumerable<T1> doc, FieldInfos fieldInfos) where T1 : Lucene.Net.Index.IIndexableField
        {
            int storedCount = 0;
            foreach (IIndexableField field in doc)
            {
                if (field.IndexableFieldType.IsStored)
                {
                    storedCount++;
                }
            }

            StartDocument(storedCount);

            foreach (IIndexableField field in doc)
            {
                if (field.IndexableFieldType.IsStored)
                {
                    WriteField(fieldInfos.FieldInfo(field.Name), field);
                }
            }

            FinishDocument();
        }

        /// <summary>
        /// Disposes all resources used by this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implementations must override and should dispose all resources used by this instance.
        /// </summary>
        protected abstract void Dispose(bool disposing);
    }
}