using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public class ParallelAtomicReader : AtomicReader
    {
        private readonly FieldInfos fieldInfos;
        private readonly ParallelFields fields = new ParallelFields();
        private readonly AtomicReader[] parallelReaders, storedFieldsReaders;
        private readonly ISet<AtomicReader> completeReaderSet = new IdentityHashSet<AtomicReader>();
        private readonly bool closeSubReaders;
        private readonly int maxDoc, numDocs;
        private readonly bool hasDeletions;
        private readonly IDictionary<String, AtomicReader> fieldToReader = new TreeMap<String, AtomicReader>();
        private readonly IDictionary<String, AtomicReader> tvFieldToReader = new TreeMap<String, AtomicReader>();

        public ParallelAtomicReader(params AtomicReader[] readers)
            : this(true, readers)
        {
        }

        public ParallelAtomicReader(bool closeSubReaders, params AtomicReader[] readers)
            : this(closeSubReaders, readers, readers)
        {
        }

        public ParallelAtomicReader(bool closeSubReaders, AtomicReader[] readers, AtomicReader[] storedFieldsReaders)
        {
            this.closeSubReaders = closeSubReaders;
            if (readers.Length == 0 && storedFieldsReaders.Length > 0)
                throw new ArgumentException("There must be at least one main reader if storedFieldsReaders are used.");
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
            Collections.AddAll(completeReaderSet, this.parallelReaders);
            Collections.AddAll(completeReaderSet, this.storedFieldsReaders);

            // check compatibility:
            foreach (AtomicReader reader in completeReaderSet)
            {
                if (reader.MaxDoc != maxDoc)
                {
                    throw new ArgumentException("All readers must have same maxDoc: " + maxDoc + "!=" + reader.MaxDoc);
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
                    if (!fieldToReader.ContainsKey(fieldInfo.name))
                    {
                        builder.Add(fieldInfo);
                        fieldToReader[fieldInfo.name] = reader;
                        if (fieldInfo.HasVectors)
                        {
                            tvFieldToReader[fieldInfo.name] = reader;
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
                    foreach (String field in readerFields)
                    {
                        // only add if the reader responsible for that field name is the current:
                        if (fieldToReader[field] == reader)
                        {
                            this.fields.AddField(field, readerFields.Terms(field));
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
                buffer.Remove(buffer.Length - 2, 2);

            return buffer.Append(')').ToString();
        }

        private sealed class ParallelFields : Fields
        {
            internal readonly IDictionary<String, Terms> fields = new TreeMap<String, Terms>();

            public ParallelFields()
            {
            }

            internal void AddField(String fieldName, Terms terms)
            {
                fields[fieldName] = terms;
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return fields.Keys.GetEnumerator();
            }

            public override Terms Terms(string field)
            {
                return fields[field];
            }

            public override int Size
            {
                get { return fields.Count; }
            }
        }

        public override FieldInfos FieldInfos
        {
            get { return fieldInfos; }
        }

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

        public override int NumDocs
        {
            get { return numDocs; }
        }

        public override int MaxDoc
        {
            get { return maxDoc; }
        }

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
            foreach (KeyValuePair<String, AtomicReader> ent in tvFieldToReader)
            {
                String fieldName = ent.Key;
                Terms vector = ent.Value.GetTermVector(docID, fieldName);
                if (vector != null)
                {
                    if (fields == null)
                    {
                        fields = new ParallelFields();
                    }
                    fields.AddField(fieldName, vector);
                }
            }

            return fields;
        }

        protected override void DoClose()
        {
            System.IO.IOException ioe = null;
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
                catch (System.IO.IOException e)
                {
                    if (ioe == null) ioe = e;
                }
            }
            // throw the first exception
            if (ioe != null) throw ioe;
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            EnsureOpen();
            AtomicReader reader = fieldToReader[field];
            return reader == null ? null : reader.GetNumericDocValues(field);
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
            AtomicReader reader = fieldToReader[field];
            return reader == null ? null : reader.GetBinaryDocValues(field);
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
            AtomicReader reader = fieldToReader[field];
            return reader == null ? null : reader.GetSortedDocValues(field);
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
            AtomicReader reader = fieldToReader[field];
            return reader == null ? null : reader.GetSortedSetDocValues(field);
        }

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
            AtomicReader reader = fieldToReader[field];
            NumericDocValues values = reader == null ? null : reader.GetNormValues(field);
            return values;
        }
    }
}
