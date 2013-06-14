using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;

namespace Lucene.Net.Document
{
    public class DocumentStoredFieldVisitor : StoredFieldVisitor
    {
        private Document doc = new Document();
        private HashSet<String> fieldstoAdd;
        public Document Document
        {
            get { return doc; }
        }
        public DocumentStoredFieldVisitor(HashSet<String> fieldsToAdd)
        {
            this.fieldstoAdd = fieldsToAdd;
        }

        public DocumentStoredFieldVisitor(String[] fields)
        {
            fieldstoAdd = new HashSet<String>();
            foreach (String field in fields)
            {
                fieldstoAdd.Add(field);
            }
        }

        public DocumentStoredFieldVisitor()
        {
            this.fieldstoAdd = null;
        }

        public override void BinaryField(FieldInfo fieldInfo, sbyte[] value)
        {
            doc.Add(new StoredField(fieldInfo.Name, value));
        }

        public override void StringField(FieldInfo fieldInfo, String value)
        {
            FieldType ft = new FieldType(TextField.TYPE_STORED);
            ft.SetStoreTermVectors(fieldInfo.HasVectors);
            ft.SetIndexed(fieldInfo.IsIndexed);
            ft.SetOmitNorms(fieldInfo.OmitNorms);
            ft.SetIndexOptions(fieldInfo.GetIndexOptions());
            doc.Add(new Field(fieldInfo.Name, value, ft));
        }

        public override void IntField(FieldInfo fieldInfo, int value)
        {
            doc.Add(new StoredField(fieldInfo.Name, value));
        }

        public override void LongField(FieldInfo fieldInfo, long value)
        {
            doc.Add(new StoredField(fieldInfo.Name, value));
        }

        public override void FloatField(FieldInfo fieldInfo, float value)
        {
            doc.Add(new StoredField(fieldInfo.Name, value));
        }

        public override void DoubleField(FieldInfo fieldInfo, double value)
        {
            doc.Add(new StoredField(fieldInfo.Name, value));
        }

        public override Status NeedsField(FieldInfo fieldInfo)
        {
            return fieldstoAdd == null || fieldstoAdd.Contains(fieldInfo.Name) ? Status.YES : Status.NO;
        }


    }
}
