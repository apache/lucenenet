using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class StoredFieldsWriter : IDisposable
    {
        protected StoredFieldsWriter()
        {
        }

        public abstract void StartDocument(int numStoredFields);

        public virtual void FinishDocument()
        {
        }

        public abstract void WriteField(FieldInfo info, IIndexableField field);

        public abstract void Abort();

        public abstract void Finish(FieldInfos fis, int numDocs);

        public virtual int Merge(MergeState mergeState)
        {
            int docCount = 0;
            foreach (AtomicReader reader in mergeState.readers)
            {
                int maxDoc = reader.MaxDoc;
                IBits liveDocs = reader.LiveDocs;
                for (int i = 0; i < maxDoc; i++)
                {
                    if (liveDocs != null && !liveDocs[i])
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
                    AddDocument(doc, mergeState.fieldInfos);
                    docCount++;
                    mergeState.checkAbort.Work(300);
                }
            }
            Finish(mergeState.fieldInfos, docCount);
            return docCount;
        }

        protected void AddDocument<T>(IEnumerable<T> doc, FieldInfos fieldInfos)
            where T : IIndexableField
        {
            int storedCount = 0;
            foreach (IIndexableField field in doc)
            {
                if (field.FieldType.Stored)
                {
                    storedCount++;
                }
            }

            StartDocument(storedCount);

            foreach (IIndexableField field in doc)
            {
                if (field.FieldType.Stored)
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
