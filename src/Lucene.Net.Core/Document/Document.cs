namespace Lucene.Net.Document
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    // for javadoc
    // for javadoc
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    // for javadoc
    using IndexableField = Lucene.Net.Index.IndexableField;

    /// <summary>
    /// Documents are the unit of indexing and search.
    ///
    /// A Document is a set of fields.  Each field has a name and a textual value.
    /// A field may be <seealso cref="Lucene.Net.Index.IndexableFieldType#stored() stored"/> with the document, in which
    /// case it is returned with search hits on the document.  Thus each document
    /// should typically contain one or more stored fields which uniquely identify
    /// it.
    ///
    /// <p>Note that fields which are <i>not</i> <seealso cref="Lucene.Net.Index.IndexableFieldType#stored() stored"/> are
    /// <i>not</i> available in documents retrieved from the index, e.g. with {@link
    /// ScoreDoc#doc} or <seealso cref="IndexReader#document(int)"/>.
    /// </summary>

    public sealed class Document : IEnumerable<IndexableField>
    {
        private readonly List<IndexableField> Fields_Renamed = new List<IndexableField>();

        /// <summary>
        /// Constructs a new document with no fields. </summary>
        public Document()
        {
        }

        public IEnumerator<IndexableField> GetEnumerator()
        {
            return Fields_Renamed.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// <p>Adds a field to a document.  Several fields may be added with
        /// the same name.  In this case, if the fields are indexed, their text is
        /// treated as though appended for the purposes of search.</p>
        /// <p> Note that add like the removeField(s) methods only makes sense
        /// prior to adding a document to an index. These methods cannot
        /// be used to change the content of an existing index! In order to achieve this,
        /// a document has to be deleted from an index and a new changed version of that
        /// document has to be added.</p>
        /// </summary>
        public void Add(IndexableField field)
        {
            Fields_Renamed.Add(field);
        }

        /// <summary>
        /// <p>Removes field with the specified name from the document.
        /// If multiple fields exist with this name, this method removes the first field that has been added.
        /// If there is no field with the specified name, the document remains unchanged.</p>
        /// <p> Note that the removeField(s) methods like the add method only make sense
        /// prior to adding a document to an index. These methods cannot
        /// be used to change the content of an existing index! In order to achieve this,
        /// a document has to be deleted from an index and a new changed version of that
        /// document has to be added.</p>
        /// </summary>
        public void RemoveField(string name)
        {
            for (int i = Fields_Renamed.Count - 1; i >= 0; i--)
            {
                IndexableField field = Fields_Renamed[i];

                if (field.Name().Equals(name))
                {
                    Fields_Renamed.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// <p>Removes all fields with the given name from the document.
        /// If there is no field with the specified name, the document remains unchanged.</p>
        /// <p> Note that the removeField(s) methods like the add method only make sense
        /// prior to adding a document to an index. These methods cannot
        /// be used to change the content of an existing index! In order to achieve this,
        /// a document has to be deleted from an index and a new changed version of that
        /// document has to be added.</p>
        /// </summary>
        public void RemoveFields(string name)
        {
            for (int i = Fields_Renamed.Count - 1; i >= 0; i--)
            {
                IndexableField field = Fields_Renamed[i];

                if (field.Name().Equals(name))
                {
                    Fields_Renamed.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Returns an array of byte arrays for of the fields that have the name specified
        /// as the method parameter.  this method returns an empty
        /// array when there are no matching fields.  It never
        /// returns null.
        /// </summary>
        /// <param name="name"> the name of the field </param>
        /// <returns> a <code>BytesRef[]</code> of binary field values </returns>
        public BytesRef[] GetBinaryValues(string name)
        {
            List<BytesRef> result = new List<BytesRef>();

            foreach (IndexableField field in Fields_Renamed)
            {
                if (field.Name().Equals(name))
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

        /// <summary>
        /// Returns an array of bytes for the first (or only) field that has the name
        /// specified as the method parameter. this method will return <code>null</code>
        /// if no binary fields with the specified name are available.
        /// There may be non-binary fields with the same name.
        /// </summary>
        /// <param name="name"> the name of the field. </param>
        /// <returns> a <code>BytesRef</code> containing the binary field value or <code>null</code> </returns>
        public BytesRef GetBinaryValue(string name)
        {
            foreach (IndexableField field in Fields_Renamed)
            {
                if (field.Name().Equals(name))
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

        /// <summary>
        /// Returns a field with the given name if any exist in this document, or
        /// null.  If multiple fields exists with this name, this method returns the
        /// first value added.
        /// </summary>
        public IndexableField GetField(string name)
        {
            foreach (IndexableField field in Fields_Renamed)
            {
                if (field.Name().Equals(name))
                {
                    return field;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns an array of <seealso cref="IndexableField"/>s with the given name.
        /// this method returns an empty array when there are no
        /// matching fields.  It never returns null.
        /// </summary>
        /// <param name="name"> the name of the field </param>
        /// <returns> a <code>IndexableField[]</code> array </returns>
        public IndexableField[] GetFields(string name)
        {
            List<IndexableField> result = new List<IndexableField>();
            foreach (IndexableField field in Fields_Renamed)
            {
                if (field.Name().Equals(name))
                {
                    result.Add(field);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns a List of all the fields in a document.
        /// <p>Note that fields which are <i>not</i> stored are
        /// <i>not</i> available in documents retrieved from the
        /// index, e.g. <seealso cref="IndexSearcher#doc(int)"/> or {@link
        /// IndexReader#document(int)}.</p>
        /// </summary>
        public List<IndexableField> Fields
        {
            get
            {
                return Fields_Renamed;
            }
        }

        private static readonly string[] NO_STRINGS = new string[0];

        /// <summary>
        /// Returns an array of values of the field specified as the method parameter.
        /// this method returns an empty array when there are no
        /// matching fields.  It never returns null.
        /// For <seealso cref="IntField"/>, <seealso cref="LongField"/>, {@link
        /// FloatField} and <seealso cref="DoubleField"/> it returns the string value of the number. If you want
        /// the actual numeric field instances back, use <seealso cref="#getFields"/>. </summary>
        /// <param name="name"> the name of the field </param>
        /// <returns> a <code>String[]</code> of field values </returns>
        public string[] GetValues(string name)
        {
            List<string> result = new List<string>();
            foreach (IndexableField field in Fields_Renamed)
            {
                if (field.Name().Equals(name) && field.StringValue != null)
                {
                    result.Add(field.StringValue);
                }
            }

            if (result.Count == 0)
            {
                return NO_STRINGS;
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns the string value of the field with the given name if any exist in
        /// this document, or null.  If multiple fields exist with this name, this
        /// method returns the first value added. If only binary fields with this name
        /// exist, returns null.
        /// For <seealso cref="IntField"/>, <seealso cref="LongField"/>, {@link
        /// FloatField} and <seealso cref="DoubleField"/> it returns the string value of the number. If you want
        /// the actual numeric field instance back, use <seealso cref="#getField"/>.
        /// </summary>
        public string Get(string name)
        {
            foreach (IndexableField field in Fields_Renamed)
            {
                if (field.Name().Equals(name) && field.StringValue != null)
                {
                    return field.StringValue;
                }
            }
            return null;
        }

        /// <summary>
        /// Prints the fields of a document for human consumption. </summary>
        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("Document<");
            for (int i = 0; i < Fields_Renamed.Count; i++)
            {
                IndexableField field = Fields_Renamed[i];
                buffer.Append(field.ToString());
                if (i != Fields_Renamed.Count - 1)
                {
                    buffer.Append(" ");
                }
            }
            buffer.Append(">");
            return buffer.ToString();
        }
    }
}