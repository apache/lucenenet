using System;
using System.Collections.Generic;

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
    using Bits = Lucene.Net.Util.Bits;
    using Document = Documents.Document;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IIndexableField = Lucene.Net.Index.IIndexableField;
    using MergeState = Lucene.Net.Index.MergeState;

    /// <summary>
    /// Codec API for writing stored fields:
    /// <p>
    /// <ol>
    ///   <li>For every document, <seealso cref="#startDocument(int)"/> is called,
    ///       informing the Codec how many fields will be written.
    ///   <li><seealso cref="#writeField(FieldInfo, IndexableField)"/> is called for
    ///       each field in the document.
    ///   <li>After all documents have been written, <seealso cref="#finish(FieldInfos, int)"/>
    ///       is called for verification/sanity-checks.
    ///   <li>Finally the writer is closed (<seealso cref="#close()"/>)
    /// </ol>
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class StoredFieldsWriter : IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected internal StoredFieldsWriter()
        {
        }

        /// <summary>
        /// Called before writing the stored fields of the document.
        ///  <seealso cref="#writeField(FieldInfo, IndexableField)"/> will be called
        ///  <code>numStoredFields</code> times. Note that this is
        ///  called even if the document has no stored fields, in
        ///  this case <code>numStoredFields</code> will be zero.
        /// </summary>
        public abstract void StartDocument(int numStoredFields);

        /// <summary>
        /// Called when a document and all its fields have been added. </summary>
        public virtual void FinishDocument()
        {
        }

        /// <summary>
        /// Writes a single stored field. </summary>
        public abstract void WriteField(FieldInfo info, IIndexableField field);

        /// <summary>
        /// Aborts writing entirely, implementation should remove
        ///  any partially-written files, etc.
        /// </summary>
        public abstract void Abort();

        /// <summary>
        /// Called before <seealso cref="#close()"/>, passing in the number
        ///  of documents that were written. Note that this is
        ///  intentionally redundant (equivalent to the number of
        ///  calls to <seealso cref="#startDocument(int)"/>, but a Codec should
        ///  check that this is the case to detect the JRE bug described
        ///  in LUCENE-1282.
        /// </summary>
        public abstract void Finish(FieldInfos fis, int numDocs);

        /// <summary>
        /// Merges in the stored fields from the readers in
        ///  <code>mergeState</code>. The default implementation skips
        ///  over deleted documents, and uses <seealso cref="#startDocument(int)"/>,
        ///  <seealso cref="#writeField(FieldInfo, IndexableField)"/>, and <seealso cref="#finish(FieldInfos, int)"/>,
        ///  returning the number of documents that were written.
        ///  Implementations can override this method for more sophisticated
        ///  merging (bulk-byte copying, etc).
        /// </summary>
        public virtual int Merge(MergeState mergeState)
        {
            int docCount = 0;
            foreach (AtomicReader reader in mergeState.Readers)
            {
                int maxDoc = reader.MaxDoc;
                Bits liveDocs = reader.LiveDocs;
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
        /// sugar method for startDocument() + writeField() for every stored field in the document </summary>
        protected void AddDocument<T1>(IEnumerable<T1> doc, FieldInfos fieldInfos) where T1 : Lucene.Net.Index.IIndexableField
        {
            int storedCount = 0;
            foreach (IIndexableField field in doc)
            {
                if (field.FieldType.IsStored)
                {
                    storedCount++;
                }
            }

            StartDocument(storedCount);

            foreach (IIndexableField field in doc)
            {
                if (field.FieldType.IsStored)
                {
                    WriteField(fieldInfos.FieldInfo(field.Name), field);
                }
            }

            FinishDocument();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}