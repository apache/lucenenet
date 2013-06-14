/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

// for javadoc
using System.Collections.Generic;
using Lucene.Net.Util;
using IndexReader = Lucene.Net.Index.IndexReader;
using ScoreDoc = Lucene.Net.Search.ScoreDoc;
using Searcher = Lucene.Net.Search.Searcher;

namespace Lucene.Net.Document
{

    /// <summary>Documents are the unit of indexing and search.
    /// 
    /// A Document is a set of fields.  Each field has a name and a textual value.
    /// A field may be <see cref="IFieldable.IsStored()">stored</see> with the document, in which
    /// case it is returned with search hits on the document.  Thus each document
    /// should typically contain one or more stored fields which uniquely identify
    /// it.
    /// 
    /// <p/>Note that fields which are <i>not</i> <see cref="IFieldable.IsStored()">stored</see> are
    /// <i>not</i> available in documents retrieved from the index, e.g. with <see cref="ScoreDoc.Doc" />,
    /// <see cref="Searcher.Doc(int)" /> or <see cref="IndexReader.Document(int)" />.
    /// </summary>

    [Serializable]
    public sealed class Document
    {
        private readonly List<IndexableField> fields = new List<IndexableField>();

        public List<IndexableField> Fields
        {
            get { return fields; }
        }

        /// <summary>Constructs a new document with no fields. </summary>
        public Document()
        {
        }

        public Document(StoredDocument storedDoc)
        {
            foreach (StorableField field in storedDoc.GetFields())
            {
                Field newField = new Field(field.Name(), (FieldType)field.FieldType());

                newField.FieldsData = field.StringValue();
                if (newField.FieldsData == null)
                    newField.FieldsData = field.NumericValue();
                if (newField.FieldsData == null)
                    newField.FieldsData = field.BinaryValue();
                if (newField.FieldsData == null)
                    newField.FieldsData = field.ReaderValue();

                Add(newField);
            }
        }

        public void Add(IndexableField field)
        {
            fields.Add(field);
        }

        public void RemoveField(String name)
        {
            System.Collections.Generic.IEnumerator<IndexableField> it = fields.GetEnumerator();
            while (it.MoveNext())
            {
                IndexableField field = it.Current;
                if (field.Name.Equals(name))
                {
                    fields.Remove(field);
                    return;
                }
            }
        }

        /// <summary> <p/>Removes all fields with the given name from the document.
        /// If there is no field with the specified name, the document remains unchanged.<p/>
        /// <p/> Note that the removeField(s) methods like the add method only make sense 
        /// prior to adding a document to an index. These methods cannot
        /// be used to change the content of an existing index! In order to achieve this,
        /// a document has to be deleted from an index and a new changed version of that
        /// document has to be added.<p/>
        /// </summary>
        public void RemoveFields(System.String name)
        {
            for (int i = fields.Count - 1; i >= 0; i--)
            {
                IndexableField field = fields[i];
                if (field.Name.Equals(name))
                {
                    fields.RemoveAt(i);
                }
            }
        }

        public BytesRef[] GetBinaryValues(String name)
        {
            List<BytesRef> result = new List<BytesRef>();

            System.Collections.Generic.IEnumerator<IndexableField> it = fields.GetEnumerator();
            while (it.MoveNext())
            {
                IndexableField field = it.Current;
                if (field.Name.Equals(name))
                {
                    BytesRef bytes = field.BinaryValue();
                    if (bytes != null)
                    {
                        result.Add(bytes);
                    }
                }
            }
            return result.ToArray();
        }


        /// <summary>Returns a field with the given name if any exist in this document, or
        /// null.  If multiple fields exists with this name, this method returns the
        /// first value added.
        /// Do not use this method with lazy loaded fields.
        /// </summary>
        public BytesRef GetBinaryValue(String name)
        {
            foreach (IndexableField field in fields)
            {
                if (field.Name.Equals(name))
                {
                    BytesRef bytes = field.BinaryValue();
                    if (bytes != null)
                    {
                        return bytes;
                    }
                }
            }
            return null;
        }


        public IndexableField GetField(String name)
        {
            foreach (IndexableField field in fields)
            {
                if (field.Name.Equals(name))
                {
                    return field;
                }
            }
            return null;
        }

        public IndexableField[] GetFields(String name)
        {
            List<IndexableField> result = new List<IndexableField>();
            foreach (IndexableField field in fields)
            {
                if (field.Name.Equals(name))
                {
                    result.Add(field);
                }
            }

            return result.ToArray();
        }

        public List<IndexableField> GetFields()
        {
            return fields;
        }

        private static readonly String[] NO_STRINGS = new String[0];


        public String[] GetValues(String name)
        {
            List<String> result = new List<String>();
            foreach (IndexableField field in fields)
            {
                if (field.Name.Equals(name) && field.StringValue() != null)
                {
                    result.Add(field.StringValue());
                }
            }

            if (result.Count == 0)
            {
                return NO_STRINGS;
            }

            return result.ToArray();
        }


        public String Get(String name)
        {
            foreach (IndexableField field in fields)
            {
                if (field.Name.Equals(name) && field.StringValue() != null)
                {
                    return field.StringValue();
                }
            }
            return null;
        }

        /// <summary>Prints the fields of a document for human consumption. </summary>
        public override System.String ToString()
        {
            System.Text.StringBuilder buffer = new System.Text.StringBuilder();
            buffer.Append("Document<");
            for (int i = 0; i < fields.Count; i++)
            {
                IndexableField field = fields[i];
                buffer.Append(field.ToString());
                if (i != fields.Count - 1)
                    buffer.Append(" ");
            }
            buffer.Append(">");
            return buffer.ToString();
        }

    }
}