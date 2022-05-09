using J2N.Runtime.CompilerServices;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
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

    using IBits = Lucene.Net.Util.IBits;

    /// <summary>
    /// An <see cref="AtomicReader"/> which reads multiple, parallel indexes.  Each index
    /// added must have the same number of documents, but typically each contains
    /// different fields. Deletions are taken from the first reader.
    /// Each document contains the union of the fields of all documents
    /// with the same document number.  When searching, matches for a
    /// query term are from the first index added that has the field.
    ///
    /// <para/>This is useful, e.g., with collections that have large fields which
    /// change rarely and small fields that change more frequently.  The smaller
    /// fields may be re-indexed in a new index and both indexes may be searched
    /// together.
    ///
    /// <para/><strong>Warning:</strong> It is up to you to make sure all indexes
    /// are created and modified the same way. For example, if you add
    /// documents to one index, you need to add the same documents in the
    /// same order to the other indexes. <em>Failure to do so will result in
    /// undefined behavior</em>.
    /// </summary>
    public class ParallelAtomicReader : AtomicReader
    {
        private readonly FieldInfos fieldInfos;
        private readonly ParallelFields fields = new ParallelFields();
        private readonly AtomicReader[] parallelReaders, storedFieldsReaders;
        private readonly ISet<AtomicReader> completeReaderSet = new JCG.HashSet<AtomicReader>(IdentityEqualityComparer<AtomicReader>.Default);
        private readonly bool closeSubReaders;
        private readonly int maxDoc, numDocs;
        private readonly bool hasDeletions;

        // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
        private readonly IDictionary<string, AtomicReader> fieldToReader = new JCG.SortedDictionary<string, AtomicReader>(StringComparer.Ordinal);
        private readonly IDictionary<string, AtomicReader> tvFieldToReader = new JCG.SortedDictionary<string, AtomicReader>(StringComparer.Ordinal);

        /// <summary>
        /// Create a <see cref="ParallelAtomicReader"/> based on the provided
        /// readers; auto-disposes the given <paramref name="readers"/> on <see cref="IndexReader.Dispose()"/>.
        /// </summary>
        public ParallelAtomicReader(params AtomicReader[] readers)
            : this(true, readers)
        {
        }

        /// <summary>
        /// Create a <see cref="ParallelAtomicReader"/> based on the provided
        /// <paramref name="readers"/>.
        /// </summary>
        public ParallelAtomicReader(bool closeSubReaders, params AtomicReader[] readers)
            : this(closeSubReaders, readers, readers)
        {
        }

        /// <summary>
        /// Expert: create a <see cref="ParallelAtomicReader"/> based on the provided
        /// <paramref name="readers"/> and <paramref name="storedFieldsReaders"/>; when a document is
        /// loaded, only <paramref name="storedFieldsReaders"/> will be used.
        /// </summary>
        public ParallelAtomicReader(bool closeSubReaders, AtomicReader[] readers, AtomicReader[] storedFieldsReaders)
        {
            this.closeSubReaders = closeSubReaders;
            if (readers.Length == 0 && storedFieldsReaders.Length > 0)
            {
                throw new ArgumentException("There must be at least one main reader if storedFieldsReaders are used.");
            }
            this.parallelReaders = (AtomicReader[])readers.Clone();
            this.storedFieldsReaders = (AtomicReader[])storedFieldsReaders.Clone();
            if (parallelReaders.Length > 0)
            {
                AtomicReader first = parallelReaders[0];
                this.maxDoc = first.MaxDoc;
                this.numDocs = first.NumDocs;
                this.hasDeletions = first.HasDeletions;
            }
            else
            {
                this.maxDoc = this.numDocs = 0;
                this.hasDeletions = false;
            }
            completeReaderSet.UnionWith(this.parallelReaders);
            completeReaderSet.UnionWith(this.storedFieldsReaders);

            // check compatibility:
            foreach (AtomicReader reader in completeReaderSet)
            {
                if (reader.MaxDoc != maxDoc)
                {
                    throw new ArgumentException("All readers must have same MaxDoc: " + maxDoc + "!=" + reader.MaxDoc);
                }
            }

            // TODO: make this read-only in a cleaner way?
            FieldInfos.Builder builder = new FieldInfos.Builder();
            // build FieldInfos and fieldToReader map:
            foreach (AtomicReader reader in this.parallelReaders)
            {
                FieldInfos readerFieldInfos = reader.FieldInfos;
                foreach (FieldInfo fieldInfo in readerFieldInfos)
                {
                    // NOTE: first reader having a given field "wins":
                    if (!fieldToReader.ContainsKey(fieldInfo.Name))
                    {
                        builder.Add(fieldInfo);
                        fieldToReader[fieldInfo.Name] = reader;
                        if (fieldInfo.HasVectors)
                        {
                            tvFieldToReader[fieldInfo.Name] = reader;
                        }
                    }
                }
            }
            fieldInfos = builder.Finish();

            // build Fields instance
            foreach (AtomicReader reader in this.parallelReaders)
            {
                Fields readerFields = reader.Fields;
                if (readerFields != null)
                {
                    foreach (string field in readerFields)
                    {
                        // only add if the reader responsible for that field name is the current:
                        if (fieldToReader[field].Equals(reader))
                        {
                            this.fields.AddField(field, readerFields.GetTerms(field));
                        }
                    }
                }
            }

            // do this finally so any Exceptions occurred before don't affect refcounts:
            foreach (AtomicReader reader in completeReaderSet)
            {
                if (!closeSubReaders)
                {
                    reader.IncRef();
                }
                reader.RegisterParentReader(this);
            }
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder("ParallelAtomicReader(");
            bool removeLastCommaSpace = false;
            foreach (AtomicReader reader in completeReaderSet)
            {
                buffer.Append(reader);
                buffer.Append(", ");
                removeLastCommaSpace = true;
            }

            if (removeLastCommaSpace)
            {
                buffer.Remove(buffer.Length - 2, 2);
            }

            return buffer.Append(')').ToString();
        }

        // Single instance of this, per ParallelReader instance
        private sealed class ParallelFields : Fields
        {
            // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
            internal readonly IDictionary<string, Terms> fields = new JCG.SortedDictionary<string, Terms>(StringComparer.Ordinal);

            internal ParallelFields()
            {
            }

            internal void AddField(string fieldName, Terms terms)
            {
                fields[fieldName] = terms;
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return fields.Keys.GetEnumerator();
            }

            public override Terms GetTerms(string field)
            {
                fields.TryGetValue(field, out Terms result);
                return result;
            }

            public override int Count => fields.Count;
        }

        /// <summary>
        /// Get the <see cref="Index.FieldInfos"/> describing all fields in
        /// this reader.
        /// <para/>
        /// NOTE: the returned field numbers will likely not
        /// correspond to the actual field numbers in the underlying
        /// readers, and codec metadata (<see cref="FieldInfo.GetAttribute(string)"/>
        /// will be unavailable.
        /// </summary>
        public override FieldInfos FieldInfos => fieldInfos;

        public override IBits LiveDocs
        {
            get
            {
                EnsureOpen();
                return hasDeletions ? parallelReaders[0].LiveDocs : null;
            }
        }

        public override Fields Fields
        {
            get
            {
                EnsureOpen();
                return fields;
            }
        }

        public override int NumDocs =>
            // Don't call ensureOpen() here (it could affect performance)
            numDocs;

        public override int MaxDoc =>
            // Don't call ensureOpen() here (it could affect performance)
            maxDoc;

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            EnsureOpen();
            foreach (AtomicReader reader in storedFieldsReaders)
            {
                reader.Document(docID, visitor);
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            EnsureOpen();
            ParallelFields fields = null;
            foreach (KeyValuePair<string, AtomicReader> ent in tvFieldToReader/*.EntrySet()*/)
            {
                string fieldName = ent.Key;
                Terms vector = ent.Value.GetTermVector(docID, fieldName);
                if (vector != null)
                {
                    if (fields is null)
                    {
                        fields = new ParallelFields();
                    }
                    fields.AddField(fieldName, vector);
                }
            }

            return fields;
        }

        protected internal override void DoClose()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                Exception ioe = null; // LUCENENET: No need to cast to IOExcpetion
                foreach (AtomicReader reader in completeReaderSet)
                {
                    try
                    {
                        if (closeSubReaders)
                        {
                            reader.Dispose();
                        }
                        else
                        {
                            reader.DecRef();
                        }
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        if (ioe is null)
                        {
                            ioe = e;
                        }
                    }
                }
                // throw the first exception
                if (ioe != null)
                {
                    ExceptionDispatchInfo.Capture(ioe).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            EnsureOpen();
            return fieldToReader.TryGetValue(field, out AtomicReader reader) ? reader.GetNumericDocValues(field) : null;
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
            return fieldToReader.TryGetValue(field, out AtomicReader reader) ? reader.GetBinaryDocValues(field) : null;
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
            return fieldToReader.TryGetValue(field, out AtomicReader reader) ? reader.GetSortedDocValues(field) : null;
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
            return fieldToReader.TryGetValue(field, out AtomicReader reader) ? reader.GetSortedSetDocValues(field) : null;
        }

        public override IBits GetDocsWithField(string field)
        {
            EnsureOpen();
            return fieldToReader.TryGetValue(field, out AtomicReader reader) ? reader.GetDocsWithField(field) : null;
        }

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
            NumericDocValues values = null;
            if (fieldToReader.TryGetValue(field, out AtomicReader reader))
            {
                values = reader.GetNormValues(field);
            }
            return values;
        }

        public override void CheckIntegrity()
        {
            EnsureOpen();
            foreach (AtomicReader reader in completeReaderSet)
            {
                reader.CheckIntegrity();
            }
        }
    }
}