using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;

namespace Lucene.Net.Documents
{
    public class DocumentStoredFieldVisitor : StoredFieldVisitor
    {
        private readonly Document doc = new Document();
        private readonly ISet<String> fieldsToAdd;

        public DocumentStoredFieldVisitor(ISet<String> fieldsToAdd)
        {
            this.fieldsToAdd = fieldsToAdd;
        }

        public DocumentStoredFieldVisitor(params String[] fields)
        {
            fieldsToAdd = new HashSet<String>();
            foreach (String field in fields)
            {
                fieldsToAdd.Add(field);
            }
        }

        public DocumentStoredFieldVisitor()
        {
            this.fieldsToAdd = null;
        }

        public override void BinaryField(FieldInfo fieldInfo, sbyte[] value)
        {
            doc.Add(new StoredField(fieldInfo.name, value));
        }

        public override void StringField(FieldInfo fieldInfo, string value)
        {
            FieldType ft = new FieldType(TextField.TYPE_STORED);
            ft.StoreTermVectors = fieldInfo.HasVectors;
            ft.Indexed = fieldInfo.IsIndexed;
            ft.OmitNorms = fieldInfo.OmitsNorms;
            ft.IndexOptions = fieldInfo.IndexOptionsValue.GetValueOrDefault();
            doc.Add(new Field(fieldInfo.name, value, ft));
        }

        public override void IntField(FieldInfo fieldInfo, int value)
        {
            doc.Add(new StoredField(fieldInfo.name, value));
        }

        public override void LongField(FieldInfo fieldInfo, long value)
        {
            doc.Add(new StoredField(fieldInfo.name, value));
        }

        public override void FloatField(FieldInfo fieldInfo, float value)
        {
            doc.Add(new StoredField(fieldInfo.name, value));
        }

        public override void DoubleField(FieldInfo fieldInfo, double value)
        {
            doc.Add(new StoredField(fieldInfo.name, value)); 
        }

        public override Status NeedsField(FieldInfo fieldInfo)
        {
            return fieldsToAdd == null || fieldsToAdd.Contains(fieldInfo.name) ? Status.YES : Status.NO;
        }

        public Document Document
        {
            get { return doc; }
        }
    }
}
