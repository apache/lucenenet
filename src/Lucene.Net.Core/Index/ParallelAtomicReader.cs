using Lucene.Net.Support;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Index
{
    using System.IO;

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

    using Bits = Lucene.Net.Util.Bits;

    /// <summary>
    /// An <seealso cref="AtomicReader"/> which reads multiple, parallel indexes.  Each index
    /// added must have the same number of documents, but typically each contains
    /// different fields. Deletions are taken from the first reader.
    /// Each document contains the union of the fields of all documents
    /// with the same document number.  When searching, matches for a
    /// query term are from the first index added that has the field.
    ///
    /// <p>this is useful, e.g., with collections that have large fields which
    /// change rarely and small fields that change more frequently.  The smaller
    /// fields may be re-indexed in a new index and both indexes may be searched
    /// together.
    ///
    /// <p><strong>Warning:</strong> It is up to you to make sure all indexes
    /// are created and modified the same way. For example, if you add
    /// documents to one index, you need to add the same documents in the
    /// same order to the other indexes. <em>Failure to do so will result in
    /// undefined behavior</em>.
    /// </summary>
    public class ParallelAtomicReader : AtomicReader
    {
        private void InitializeInstanceFields()
        {
            Fields_Renamed = new ParallelFields(this);
        }

        private readonly FieldInfos FieldInfos_Renamed;
        private ParallelFields Fields_Renamed;
        private readonly AtomicReader[] ParallelReaders, StoredFieldsReaders;
        private readonly ISet<AtomicReader> CompleteReaderSet = new IdentityHashSet<AtomicReader>();
        private readonly bool CloseSubReaders;
        private readonly int maxDoc, numDocs;
        private readonly bool hasDeletions;
        private readonly IDictionary<string, AtomicReader> FieldToReader = new SortedDictionary<string, AtomicReader>();
        private readonly IDictionary<string, AtomicReader> TvFieldToReader = new SortedDictionary<string, AtomicReader>();

        /// <summary>
        /// Create a ParallelAtomicReader based on the provided
        ///  readers; auto-closes the given readers on <seealso cref="#close()"/>.
        /// </summary>
        public ParallelAtomicReader(params AtomicReader[] readers)
            : this(true, readers)
        {
        }

        /// <summary>
        /// Create a ParallelAtomicReader based on the provided
        ///  readers.
        /// </summary>
        public ParallelAtomicReader(bool closeSubReaders, params AtomicReader[] readers)
            : this(closeSubReaders, readers, readers)
        {
        }

        /// <summary>
        /// Expert: create a ParallelAtomicReader based on the provided
        ///  readers and storedFieldReaders; when a document is
        ///  loaded, only storedFieldsReaders will be used.
        /// </summary>
        public ParallelAtomicReader(bool closeSubReaders, AtomicReader[] readers, AtomicReader[] storedFieldsReaders)
        {
            InitializeInstanceFields();
            this.CloseSubReaders = closeSubReaders;
            if (readers.Length == 0 && storedFieldsReaders.Length > 0)
            {
                throw new System.ArgumentException("There must be at least one main reader if storedFieldsReaders are used.");
            }
            this.ParallelReaders = (AtomicReader[])readers.Clone();
            this.StoredFieldsReaders = (AtomicReader[])storedFieldsReaders.Clone();
            if (ParallelReaders.Length > 0)
            {
                AtomicReader first = ParallelReaders[0];
                this.maxDoc = first.MaxDoc;
                this.numDocs = first.NumDocs;
                this.hasDeletions = first.HasDeletions;
            }
            else
            {
                this.maxDoc = this.numDocs = 0;
                this.hasDeletions = false;
            }
            CollectionsHelper.AddAll(CompleteReaderSet, this.ParallelReaders);
            CollectionsHelper.AddAll(CompleteReaderSet, this.StoredFieldsReaders);

            // check compatibility:
            foreach (AtomicReader reader in CompleteReaderSet)
            {
                if (reader.MaxDoc != maxDoc)
                {
                    throw new System.ArgumentException("All readers must have same maxDoc: " + maxDoc + "!=" + reader.MaxDoc);
                }
            }

            // TODO: make this read-only in a cleaner way?
            FieldInfos.Builder builder = new FieldInfos.Builder();
            // build FieldInfos and fieldToReader map:
            foreach (AtomicReader reader in this.ParallelReaders)
            {
                FieldInfos readerFieldInfos = reader.FieldInfos;
                foreach (FieldInfo fieldInfo in readerFieldInfos)
                {
                    // NOTE: first reader having a given field "wins":
                    if (!FieldToReader.ContainsKey(fieldInfo.Name))
                    {
                        builder.Add(fieldInfo);
                        FieldToReader[fieldInfo.Name] = reader;
                        if (fieldInfo.HasVectors())
                        {
                            TvFieldToReader[fieldInfo.Name] = reader;
                        }
                    }
                }
            }
            FieldInfos_Renamed = builder.Finish();

            // build Fields instance
            foreach (AtomicReader reader in this.ParallelReaders)
            {
                Fields readerFields = reader.Fields;
                if (readerFields != null)
                {
                    foreach (string field in readerFields)
                    {
                        // only add if the reader responsible for that field name is the current:
                        if (FieldToReader[field].Equals(reader))
                        {
                            this.Fields_Renamed.AddField(field, readerFields.Terms(field));
                        }
                    }
                }
            }

            // do this finally so any Exceptions occurred before don't affect refcounts:
            foreach (AtomicReader reader in CompleteReaderSet)
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
            foreach (AtomicReader reader in CompleteReaderSet)
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
            private readonly ParallelAtomicReader OuterInstance;

            internal readonly IDictionary<string, Terms> Fields = new SortedDictionary<string, Terms>();

            internal ParallelFields(ParallelAtomicReader outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            internal void AddField(string fieldName, Terms terms)
            {
                Fields[fieldName] = terms;
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return Fields.Keys.GetEnumerator();
            }

            public override Terms Terms(string field)
            {
                Terms ret;
                Fields.TryGetValue(field, out ret);
                return ret;
            }

            public override int Size
            {
                get { return Fields.Count; }
            }
        }

        /// <summary>
        /// {@inheritDoc}
        /// <p>
        /// NOTE: the returned field numbers will likely not
        /// correspond to the actual field numbers in the underlying
        /// readers, and codec metadata (<seealso cref="FieldInfo#getAttribute(String)"/>
        /// will be unavailable.
        /// </summary>
        public override FieldInfos FieldInfos
        {
            get
            {
                return FieldInfos_Renamed;
            }
        }

        public override Bits LiveDocs
        {
            get
            {
                EnsureOpen();
                return hasDeletions ? ParallelReaders[0].LiveDocs : null;
            }
        }

        public override Fields Fields
        {
            get
            {
                EnsureOpen();
                return Fields_Renamed;
            }
        }

        public override int NumDocs
        {
            get
            {
                // Don't call ensureOpen() here (it could affect performance)
                return numDocs;
            }
        }

        public override int MaxDoc
        {
            get
            {
                // Don't call ensureOpen() here (it could affect performance)
                return maxDoc;
            }
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            EnsureOpen();
            foreach (AtomicReader reader in StoredFieldsReaders)
            {
                reader.Document(docID, visitor);
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            EnsureOpen();
            ParallelFields fields = null;
            foreach (KeyValuePair<string, AtomicReader> ent in TvFieldToReader/*.EntrySet()*/)
            {
                string fieldName = ent.Key;
                Terms vector = ent.Value.GetTermVector(docID, fieldName);
                if (vector != null)
                {
                    if (fields == null)
                    {
                        fields = new ParallelFields(this);
                    }
                    fields.AddField(fieldName, vector);
                }
            }

            return fields;
        }

        protected internal override void DoClose()
        {
            lock (this)
            {
                IOException ioe = null;
                foreach (AtomicReader reader in CompleteReaderSet)
                {
                    try
                    {
                        if (CloseSubReaders)
                        {
                            reader.Dispose();
                        }
                        else
                        {
                            reader.DecRef();
                        }
                    }
                    catch (IOException e)
                    {
                        if (ioe == null)
                        {
                            ioe = e;
                        }
                    }
                }
                // throw the first exception
                if (ioe != null)
                {
                    throw ioe;
                }
            }
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            EnsureOpen();
            AtomicReader reader;
            return FieldToReader.TryGetValue(field, out reader) ? reader.GetNumericDocValues(field) : null;
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
            AtomicReader reader;
            return FieldToReader.TryGetValue(field, out reader) ? reader.GetBinaryDocValues(field) : null;
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
            AtomicReader reader;
            return FieldToReader.TryGetValue(field, out reader) ? reader.GetSortedDocValues(field) : null;
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
            AtomicReader reader;
            return FieldToReader.TryGetValue(field, out reader) ? reader.GetSortedSetDocValues(field) : null;
        }

        public override Bits GetDocsWithField(string field)
        {
            EnsureOpen();
            AtomicReader reader;
            return FieldToReader.TryGetValue(field, out reader) ? reader.GetDocsWithField(field) : null;
        }

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
            AtomicReader reader;
            NumericDocValues values = null;
            if (FieldToReader.TryGetValue(field, out reader))
            {
                values = reader.GetNormValues(field);
            }
            return values;
        }

        public override void CheckIntegrity()
        {
            EnsureOpen();
            foreach (AtomicReader reader in CompleteReaderSet)
            {
                reader.CheckIntegrity();
            }
        }
    }
}