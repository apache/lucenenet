using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Documents
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

    /// <summary>
    /// Documents are the unit of indexing and search.
    /// <para/>
    /// A Document is a set of fields.  Each field has a name and a textual value.
    /// A field may be stored (<see cref="IIndexableFieldType.IsStored"/>) with the document, in which
    /// case it is returned with search hits on the document.  Thus each document
    /// should typically contain one or more stored fields which uniquely identify
    /// it.
    /// <para/>
    /// Note that fields which are <i>not</i> <see cref="Lucene.Net.Index.IIndexableFieldType.IsStored"/> are
    /// <i>not</i> available in documents retrieved from the index, e.g. with 
    /// <see cref="Search.ScoreDoc.Doc"/> or <see cref="IndexReader.Document(int)"/>.
    /// </summary>
    public sealed class Document : IEnumerable<IIndexableField>, IFormattable
    {
        private readonly IList<IIndexableField> fields = new JCG.List<IIndexableField>();

        /// <summary>
        /// Constructs a new document with no fields. </summary>
        public Document()
        {
        }

        public IEnumerator<IIndexableField> GetEnumerator()
        {
            return fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// <para>Adds a field to a document.  Several fields may be added with
        /// the same name.  In this case, if the fields are indexed, their text is
        /// treated as though appended for the purposes of search.</para>
        /// <para> Note that add like the <see cref="RemoveField(string)"/> and <see cref="RemoveFields(string)"/> methods only makes sense
        /// prior to adding a document to an index. These methods cannot
        /// be used to change the content of an existing index! In order to achieve this,
        /// a document has to be deleted from an index and a new changed version of that
        /// document has to be added.</para>
        /// </summary>
        public void Add(IIndexableField field)
        {
            fields.Add(field);
        }

        /// <summary>
        /// <para>Removes field with the specified name from the document.
        /// If multiple fields exist with this name, this method removes the first field that has been added.
        /// If there is no field with the specified name, the document remains unchanged.</para>
        /// <para> Note that the <see cref="RemoveField(string)"/> and <see cref="RemoveFields(string)"/> methods like the add method only make sense
        /// prior to adding a document to an index. These methods cannot
        /// be used to change the content of an existing index! In order to achieve this,
        /// a document has to be deleted from an index and a new changed version of that
        /// document has to be added.</para>
        /// </summary>
        public void RemoveField(string name)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                IIndexableField field = fields[i];

                if (field.Name.Equals(name, StringComparison.Ordinal))
                {
                    fields.Remove(field);
                    return;
                }
            }
        }

        /// <summary>
        /// <para>Removes all fields with the given name from the document.
        /// If there is no field with the specified name, the document remains unchanged.</para>
        /// <para> Note that the <see cref="RemoveField(string)"/> and <see cref="RemoveFields(string)"/> methods like the add method only make sense
        /// prior to adding a document to an index. These methods cannot
        /// be used to change the content of an existing index! In order to achieve this,
        /// a document has to be deleted from an index and a new changed version of that
        /// document has to be added.</para>
        /// </summary>
        public void RemoveFields(string name)
        {
            for (int i = fields.Count - 1; i >= 0; i--)
            {
                IIndexableField field = fields[i];

                if (field.Name.Equals(name, StringComparison.Ordinal))
                {
                    fields.Remove(field);
                }
            }
        }

        /// <summary>
        /// Returns an array of byte arrays for of the fields that have the name specified
        /// as the method parameter. This method returns an empty
        /// array when there are no matching fields.  It never
        /// returns <c>null</c>.
        /// </summary>
        /// <param name="name"> the name of the field </param>
        /// <returns> a <see cref="T:BytesRef[]"/> of binary field values </returns>
        public BytesRef[] GetBinaryValues(string name)
        {
            var result = new JCG.List<BytesRef>();

            foreach (IIndexableField field in fields)
            {
                if (field.Name.Equals(name, StringComparison.Ordinal))
                {
                    BytesRef bytes = field.GetBinaryValue();

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
        /// specified as the method parameter. this method will return <c>null</c>
        /// if no binary fields with the specified name are available.
        /// There may be non-binary fields with the same name.
        /// </summary>
        /// <param name="name"> the name of the field. </param>
        /// <returns> a <see cref="BytesRef"/> containing the binary field value or <c>null</c> </returns>
        public BytesRef GetBinaryValue(string name)
        {
            foreach (IIndexableField field in fields)
            {
                if (field.Name.Equals(name, StringComparison.Ordinal))
                {
                    BytesRef bytes = field.GetBinaryValue();
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
        /// <c>null</c>. If multiple fields exists with this name, this method returns the
        /// first value added.
        /// </summary>
        public IIndexableField GetField(string name)
        {
            foreach (IIndexableField field in fields)
            {
                if (field.Name.Equals(name, StringComparison.Ordinal))
                {
                    return field;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns an array of <see cref="IIndexableField"/>s with the given name.
        /// This method returns an empty array when there are no
        /// matching fields. It never returns <c>null</c>.
        /// </summary>
        /// <param name="name"> the name of the field </param>
        /// <returns> a <see cref="T:IndexableField[]"/> array </returns>
        public IIndexableField[] GetFields(string name)
        {
            var result = new JCG.List<IIndexableField>();
            foreach (IIndexableField field in fields)
            {
                if (field.Name.Equals(name, StringComparison.Ordinal))
                {
                    result.Add(field);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns a List of all the fields in a document.
        /// <para>Note that fields which are <i>not</i> stored are
        /// <i>not</i> available in documents retrieved from the
        /// index, e.g. <see cref="Search.IndexSearcher.Doc(int)"/> or 
        /// <see cref="IndexReader.Document(int)"/>.
        /// </para>
        /// </summary>
        public IList<IIndexableField> Fields => fields;

        private static readonly string[] NO_STRINGS = Arrays.Empty<string>();

        /// <summary>
        /// Returns an array of values of the field specified as the method parameter.
        /// This method returns an empty array when there are no
        /// matching fields. It never returns <c>null</c>.
        /// For <see cref="Int32Field"/>, <see cref="Int64Field"/>, 
        /// <see cref="SingleField"/> and <seealso cref="DoubleField"/> it returns the string value of the number. If you want
        /// the actual numeric field instances back, use <see cref="GetFields(string)"/>. </summary>
        /// <param name="name"> the name of the field </param>
        /// <returns> a <see cref="T:string[]"/> of field values </returns>
        public string[] GetValues(string name)
        {
            var result = new JCG.List<string>();
            foreach (IIndexableField field in fields)
            {
                string fieldStringValue;
                if (field.Name.Equals(name, StringComparison.Ordinal) && (fieldStringValue = field.GetStringValue()) != null)
                {
                    result.Add(fieldStringValue);
                }
            }

            if (result.Count == 0)
            {
                return NO_STRINGS;
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns an array of values of the field specified as the method parameter.
        /// This method returns an empty array when there are no
        /// matching fields. It never returns <c>null</c>.
        /// For <see cref="Int32Field"/>, <see cref="Int64Field"/>, 
        /// <see cref="SingleField"/> and <seealso cref="DoubleField"/> it returns the string value of the number. If you want
        /// the actual numeric field instances back, use <see cref="GetFields(string)"/>. </summary>
        /// <param name="name"> the name of the field </param>
        /// <param name="format">A standard or custom numeric format string. This parameter has no effect if this field is non-numeric.</param>
        /// <returns> a <see cref="T:string[]"/> of field values </returns>
        // LUCENENET specific - method added for better .NET compatibility
        public string[] GetValues(string name, string format)
        {
            var result = new JCG.List<string>();
            foreach (IIndexableField field in fields)
            {
                string fieldStringValue;
                if (field.Name.Equals(name, StringComparison.Ordinal) && (fieldStringValue = field.GetStringValue(format)) != null)
                {
                    result.Add(fieldStringValue);
                }
            }

            if (result.Count == 0)
            {
                return NO_STRINGS;
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns an array of values of the field specified as the method parameter.
        /// This method returns an empty array when there are no
        /// matching fields. It never returns <c>null</c>.
        /// For <see cref="Int32Field"/>, <see cref="Int64Field"/>, 
        /// <see cref="SingleField"/> and <seealso cref="DoubleField"/> it returns the string value of the number. If you want
        /// the actual numeric field instances back, use <see cref="GetFields(string)"/>. </summary>
        /// <param name="name"> the name of the field </param>
        /// <param name = "provider" > An object that supplies culture-specific formatting information.This parameter has no effect if this field is non-numeric.</param>
        /// <returns> a <see cref="T:string[]"/> of field values </returns>
        // LUCENENET specific - method added for better .NET compatibility
        public string[] GetValues(string name, IFormatProvider provider)
        {
            var result = new JCG.List<string>();
            foreach (IIndexableField field in fields)
            {
                string fieldStringValue;
                if (field.Name.Equals(name, StringComparison.Ordinal) && (fieldStringValue = field.GetStringValue(provider)) != null)
                {
                    result.Add(fieldStringValue);
                }
            }

            if (result.Count == 0)
            {
                return NO_STRINGS;
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns an array of values of the field specified as the method parameter.
        /// This method returns an empty array when there are no
        /// matching fields. It never returns <c>null</c>.
        /// For <see cref="Int32Field"/>, <see cref="Int64Field"/>, 
        /// <see cref="SingleField"/> and <seealso cref="DoubleField"/> it returns the string value of the number. If you want
        /// the actual numeric field instances back, use <see cref="GetFields(string)"/>. </summary>
        /// <param name="name"> the name of the field </param>
        /// <param name="format">A standard or custom numeric format string. This parameter has no effect if this field is non-numeric.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information. This parameter has no effect if this field is non-numeric.</param>
        /// <returns> a <see cref="T:string[]"/> of field values </returns>
        // LUCENENET specific - method added for better .NET compatibility
        public string[] GetValues(string name, string format, IFormatProvider provider)
        {
            var result = new JCG.List<string>();
            foreach (IIndexableField field in fields)
            {
                string fieldStringValue;
                if (field.Name.Equals(name, StringComparison.Ordinal) && (fieldStringValue = field.GetStringValue(format, provider)) != null)
                {
                    result.Add(fieldStringValue);
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
        /// this document, or <c>null</c>.  If multiple fields exist with this name, this
        /// method returns the first value added. If only binary fields with this name
        /// exist, returns <c>null</c>.
        /// For <see cref="Int32Field"/>, <see cref="Int64Field"/>, 
        /// <see cref="SingleField"/> and <seealso cref="DoubleField"/> it returns the string value of the number. If you want
        /// the actual numeric field instance back, use <see cref="GetField(string)"/>.
        /// </summary>
        public string Get(string name)
        {
            foreach (IIndexableField field in fields)
            {
                string fieldStringValue;
                if (field.Name.Equals(name, StringComparison.Ordinal) && (fieldStringValue = field.GetStringValue()) != null)
                {
                    return fieldStringValue;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the string value of the field with the given name if any exist in
        /// this document, or <c>null</c>.  If multiple fields exist with this name, this
        /// method returns the first value added. If only binary fields with this name
        /// exist, returns <c>null</c>.
        /// For <see cref="Int32Field"/>, <see cref="Int64Field"/>, 
        /// <see cref="SingleField"/> and <seealso cref="DoubleField"/> it returns the string value of the number. If you want
        /// the actual numeric field instance back, use <see cref="GetField(string)"/>.
        /// </summary>
        /// <param name="format">A standard or custom numeric format string. This parameter has no effect if this field is non-numeric.</param>
        // LUCENENET specific - method added for better .NET compatibility
        public string Get(string name, string format)
        {
            foreach (IIndexableField field in fields)
            {
                string fieldStringValue;
                if (field.Name.Equals(name, StringComparison.Ordinal) && (fieldStringValue = field.GetStringValue(format)) != null)
                {
                    return fieldStringValue;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the string value of the field with the given name if any exist in
        /// this document, or <c>null</c>.  If multiple fields exist with this name, this
        /// method returns the first value added. If only binary fields with this name
        /// exist, returns <c>null</c>.
        /// For <see cref="Int32Field"/>, <see cref="Int64Field"/>, 
        /// <see cref="SingleField"/> and <seealso cref="DoubleField"/> it returns the string value of the number. If you want
        /// the actual numeric field instance back, use <see cref="GetField(string)"/>.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information. This parameter has no effect if this field is non-numeric.</param>
        // LUCENENET specific - method added for better .NET compatibility
        public string Get(string name, IFormatProvider provider)
        {
            foreach (IIndexableField field in fields)
            {
                string fieldStringValue;
                if (field.Name.Equals(name, StringComparison.Ordinal) && (fieldStringValue = field.GetStringValue(provider)) != null)
                {
                    return fieldStringValue;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the string value of the field with the given name if any exist in
        /// this document, or <c>null</c>.  If multiple fields exist with this name, this
        /// method returns the first value added. If only binary fields with this name
        /// exist, returns <c>null</c>.
        /// For <see cref="Int32Field"/>, <see cref="Int64Field"/>, 
        /// <see cref="SingleField"/> and <seealso cref="DoubleField"/> it returns the string value of the number. If you want
        /// the actual numeric field instance back, use <see cref="GetField(string)"/>.
        /// </summary>
        /// <param name="format">A standard or custom numeric format string. This parameter has no effect if this field is non-numeric.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information. This parameter has no effect if this field is non-numeric.</param>
        // LUCENENET specific - method added for better .NET compatibility
        public string Get(string name, string format, IFormatProvider provider)
        {
            foreach (IIndexableField field in fields)
            {
                string fieldStringValue;
                if (field.Name.Equals(name, StringComparison.Ordinal) && (fieldStringValue = field.GetStringValue(format, provider)) != null)
                {
                    return fieldStringValue;
                }
            }
            return null;
        }

        /// <summary>
        /// Prints the fields of a document for human consumption. </summary>
        public override string ToString()
        {
            return ToString(null, J2N.Text.StringFormatter.CurrentCulture);
        }

        /// <summary>
        /// Prints the fields of a document for human consumption. 
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information. This parameter has no effect if this field is non-numeric.</param>
        // LUCENENET specific - method added for better .NET compatibility
        public string ToString(IFormatProvider provider)
        {
            return ToString(null, provider);
        }

        /// <summary>
        /// Prints the fields of a document for human consumption. 
        /// </summary>
        /// <param name="format">A standard or custom numeric format string. This parameter has no effect if this field is non-numeric.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information. This parameter has no effect if this field is non-numeric.</param>
        // LUCENENET specific - method added for better .NET compatibility
        private string ToString(string format, IFormatProvider provider)
        {
            var buffer = new StringBuilder();
            buffer.Append("Document<");
            for (int i = 0; i < fields.Count; i++)
            {
                IIndexableField field = fields[i];
                if (field is IFormattable formattable)
                {
                    buffer.Append(formattable.ToString(format, provider));
                }
                else
                {
                    buffer.Append(field.ToString());
                }
                if (i != fields.Count - 1)
                {
                    buffer.Append(' ');
                }
            }
            buffer.Append('>');
            return buffer.ToString();
        }

        /// <summary>
        /// Prints the fields of a document for human consumption. 
        /// </summary>
        /// <param name="format">A standard or custom numeric format string. This parameter has no effect if this field is non-numeric.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information. This parameter has no effect if this field is non-numeric.</param>
        // LUCENENET specific - method added for better .NET compatibility
        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            return ToString(format, provider);
        }
    }
}