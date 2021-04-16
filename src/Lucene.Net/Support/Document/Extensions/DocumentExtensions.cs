using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Documents.Extensions
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
    /// LUCENENET specific extensions to the <see cref="Document"/> class.
    /// </summary>
    public static class DocumentExtensions
    {
        /// <summary>
        /// Returns a field with the given name if any exist in this document cast to type <typeparamref name="T"/>, or
        /// <c>null</c>. If multiple fields exists with this name, this method returns the
        /// first value added.
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name">Field name</param>
        /// <exception cref="InvalidCastException">If the field type cannot be cast to <typeparamref name="T"/>.</exception>
        /// <exception cref="ArgumentNullException">This <paramref name="document"/> is <c>null</c>.</exception>
        public static T GetField<T>(this Document document, string name) where T : IIndexableField
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            return (T)document.GetField(name);
        }

        /// <summary>
        /// Returns an array of <see cref="IIndexableField"/>s with the given name, cast to type <typeparamref name="T"/>.
        /// This method returns an empty array when there are no
        /// matching fields. It never returns <c>null</c>.
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> the name of the field </param>
        /// <returns> a <see cref="T:IndexableField[]"/> array </returns>
        /// <exception cref="InvalidCastException">If the field type cannot be cast to <typeparam name="T"/>.</exception>
        /// <exception cref="ArgumentNullException">This <paramref name="document"/> is <c>null</c>.</exception>
        public static T[] GetFields<T>(this Document document, string name) where T : IIndexableField
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var fields = document.GetFields(name);
            var result = new T[fields.Length];
            fields.CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// Adds a new <see cref="BinaryDocValuesField"/>.
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> binary content </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/> or the field <paramref name="name"/> is <c>null</c>. </exception>
        public static BinaryDocValuesField AddBinaryDocValuesField(this Document document, string name, BytesRef value)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new BinaryDocValuesField(name, value);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new <see cref="DoubleDocValuesField"/> field with the specified 64-bit double value </summary>
        /// <remarks>
        /// Syntactic sugar for encoding doubles as <see cref="Index.NumericDocValues"/>
        /// via <see cref="J2N.BitConversion.DoubleToRawInt64Bits(double)"/>.
        /// <para/>
        /// Per-document double values can be retrieved via
        /// <see cref="Search.IFieldCache.GetDoubles(Lucene.Net.Index.AtomicReader, string, bool)"/>.
        /// <para/>
        /// <b>NOTE</b>: In most all cases this will be rather inefficient,
        /// requiring eight bytes per document. Consider encoding double
        /// values yourself with only as much precision as you require.
        /// </remarks>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit double value </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/> or the field <paramref name="name"/> is <c>null</c> </exception>
        public static DoubleDocValuesField AddDoubleDocValuesField(this Document document, string name, double value)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new DoubleDocValuesField(name, value);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored or un-stored <see cref="DoubleField"/> with the provided value
        /// and default <c>precisionStep</c> 
        /// <see cref="Util.NumericUtils.PRECISION_STEP_DEFAULT"/> (4).
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit <see cref="double"/> value </param>
        /// <param name="stored"> <see cref="Field.Store.YES"/> if the content should also be stored </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/> or the field <paramref name="name"/> is <c>null</c>.  </exception>
        public static DoubleField AddDoubleField(this Document document, string name, double value, Field.Store stored)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new DoubleField(name, value, stored);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored or un-stored <see cref="DoubleField"/> with the provided value.
        /// <para/>
        /// Expert: allows you to customize the <see cref="FieldType"/>. 
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit double value </param>
        /// <param name="type"> customized field type: must have <see cref="FieldType.NumericType"/>
        ///         of <see cref="NumericType.DOUBLE"/>. </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> or <paramref name="type"/> is <c>null</c>, or
        ///          if the field type does not have a <see cref="NumericType.DOUBLE"/> <see cref="FieldType.NumericType"/> </exception>
        public static DoubleField AddDoubleField(this Document document, string name, double value, FieldType type)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new DoubleField(name, value, type);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new <see cref="SingleDocValuesField"/> field with the specified 32-bit <see cref="float"/> value </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> 32-bit <see cref="float"/> value </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/> or the field <paramref name="name"/> is <c>null</c> </exception>
        public static SingleDocValuesField AddSingleDocValuesField(this Document document, string name, float value)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new SingleDocValuesField(name, value);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored or un-stored <see cref="SingleField"/> with the provided value
        /// and default <c>precisionStep</c> <see cref="Util.NumericUtils.PRECISION_STEP_DEFAULT"/>
        /// (4).
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> 32-bit <see cref="float"/> value </param>
        /// <param name="stored"> <see cref="Field.Store.YES"/> if the content should also be stored </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/> or the field <paramref name="name"/> is <c>null</c>. </exception>
        public static SingleField AddSingleField(this Document document, string name, float value, Field.Store stored)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new SingleField(name, value, stored);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored or un-stored <see cref="SingleField"/> with the provided value.
        /// <para/>
        /// Expert: allows you to customize the <see cref="FieldType"/>. 
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> 32-bit <see cref="float"/> value </param>
        /// <param name="type"> customized field type: must have <see cref="FieldType.NumericType"/>
        ///         of <see cref="NumericType.SINGLE"/>. </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> or <paramref name="type"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException">if the field type does not have a <see cref="NumericType.SINGLE"/> <see cref="FieldType.NumericType"/></exception>
        public static SingleField AddSingleField(this Document document, string name, float value, FieldType type)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new SingleField(name, value, type);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored or un-stored <see cref="Int32Field"/> with the provided value
        /// and default <c>precisionStep</c> 
        /// <see cref="Util.NumericUtils.PRECISION_STEP_DEFAULT"/> (4). 
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> 32-bit <see cref="int"/> value </param>
        /// <param name="stored"> <see cref="Field.Store.YES"/> if the content should also be stored </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/> or the field <paramref name="name"/> is <c>null</c>. </exception>
        public static Int32Field AddInt32Field(this Document document, string name, int value, Field.Store stored)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new Int32Field(name, value, stored);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored or un-stored <see cref="Int32Field"/> with the provided value.
        /// <para/>
        /// Expert: allows you to customize the 
        /// <see cref="FieldType"/>.
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> 32-bit <see cref="int"/> value </param>
        /// <param name="type"> customized field type: must have <see cref="FieldType.NumericType"/>
        ///         of <see cref="NumericType.INT32"/>. </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> or <paramref name="type"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException">if the field type does not have a 
        ///         <see cref="FieldType.NumericType"/> of <see cref="NumericType.INT32"/> </exception>
        public static Int32Field AddInt32Field(this Document document, string name, int value, FieldType type)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new Int32Field(name, value, type);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored or un-stored <see cref="Int64Field"/> with the provided value
        /// and default <c>precisionStep</c> 
        /// <see cref="Util.NumericUtils.PRECISION_STEP_DEFAULT"/> (4). 
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit <see cref="long"/> value </param>
        /// <param name="stored"> <see cref="Field.Store.YES"/> if the content should also be stored </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/> or the field <paramref name="name"/> is <c>null</c>. </exception>
        public static Int64Field AddInt64Field(this Document document, string name, long value, Field.Store stored)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new Int64Field(name, value, stored);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored or un-stored <see cref="Int64Field"/> with the provided value.
        /// <para/>
        /// Expert: allows you to customize the <see cref="FieldType"/>. 
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit <see cref="long"/> value </param>
        /// <param name="type"> customized field type: must have <see cref="FieldType.NumericType"/>
        ///         of <see cref="NumericType.INT64"/>. </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> or <paramref name="type"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> if the field type does not have a 
        /// <see cref="FieldType.NumericType"/> of <see cref="NumericType.INT64"/> </exception>
        public static Int64Field AddInt64Field(this Document document, string name, long value, FieldType type)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new Int64Field(name, value, type);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new <see cref="NumericDocValuesField"/> field with the specified 64-bit <see cref="long"/> value </summary>
        /// <remarks>
        /// If you also need to store the value, you should add a
        /// separate <see cref="StoredField"/> instance.
        /// </remarks>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit <see cref="long"/> value </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> is <c>null</c>. </exception>
        public static NumericDocValuesField AddNumericDocValuesField(this Document document, string name, long value)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new NumericDocValuesField(name, value);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new <see cref="SortedDocValuesField"/> field. </summary>
        /// <remarks>
        /// If you also need to store the value, you should add a
        /// separate <see cref="StoredField"/> instance.
        /// </remarks>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="bytes"> binary content </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> is <c>null</c>. </exception>
        public static SortedDocValuesField AddSortedDocValuesField(this Document document, string name, BytesRef bytes)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new SortedDocValuesField(name, bytes);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new <see cref="SortedSetDocValuesField"/> field. </summary>
        /// <remarks>
        /// If you also need to store the value, you should add a
        /// separate <see cref="StoredField"/> instance.
        /// </remarks>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="bytes"> binary content </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> is <c>null</c>. </exception>
        public static SortedSetDocValuesField AddSortedSetDocValuesField(this Document document, string name, BytesRef bytes)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new SortedSetDocValuesField(name, bytes);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored-only field with the given binary value.
        /// <para>NOTE: the provided <see cref="T:byte[]"/> is not copied so be sure
        /// not to change it until you're done with this field.</para>
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> byte array pointing to binary content (not copied) </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> is <c>null</c>. </exception>
        public static StoredField AddStoredField(this Document document, string name, byte[] value)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new StoredField(name, value);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored-only field with the given binary value.
        /// <para>NOTE: the provided <see cref="T:byte[]"/> is not copied so be sure
        /// not to change it until you're done with this field.</para>
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="byte"/> array pointing to binary content (not copied) </param>
        /// <param name="offset"> starting position of the byte array </param>
        /// <param name="length"> valid length of the byte array </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> is <c>null</c>. </exception>
        public static StoredField AddStoredField(this Document document, string name, byte[] value, int offset, int length)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new StoredField(name, value, offset, length);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored-only field with the given binary value.
        /// <para>NOTE: the provided <see cref="BytesRef"/> is not copied so be sure
        /// not to change it until you're done with this field.</para>
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="BytesRef"/> pointing to binary content (not copied) </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> is <c>null</c>. </exception>
        public static StoredField AddStoredField(this Document document, string name, BytesRef value)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new StoredField(name, value);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored-only field with the given <see cref="string"/> value. </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="string"/> value </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> or <paramref name="value"/> is <c>null</c>. </exception>
        public static StoredField AddStoredField(this Document document, string name, string value)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new StoredField(name, value);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored-only field with the given <see cref="int"/> value. </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="int"/> value </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> is <c>null</c>. </exception>
        public static StoredField AddStoredField(this Document document, string name, int value)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new StoredField(name, value);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored-only field with the given <see cref="float"/> value. </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="float"/> value </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> is <c>null</c>. </exception>
        public static StoredField AddStoredField(this Document document, string name, float value)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new StoredField(name, value);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored-only field with the given <see cref="long"/> value. </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="long"/> value </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> is <c>null</c>. </exception>
        public static StoredField AddStoredField(this Document document, string name, long value)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new StoredField(name, value);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a stored-only field with the given <see cref="double"/> value. </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="double"/> value </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> is <c>null</c>. </exception>
        public static StoredField AddStoredField(this Document document, string name, double value)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new StoredField(name, value);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new <see cref="StringField"/> (a field that is indexed but not tokenized)
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="string"/> value </param>
        /// <param name="stored"> <see cref="Field.Store.YES"/> if the content should also be stored </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> or <paramref name="value"/> is <c>null</c>. </exception>
        public static StringField AddStringField(this Document document, string name, string value, Field.Store stored)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new StringField(name, value, stored);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new un-stored <see cref="TextField"/> with <see cref="TextReader"/> value. </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="reader"> <see cref="TextReader"/> value </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> or <paramref name="reader"/> is <c>null</c>. </exception>
        public static TextField AddTextField(this Document document, string name, TextReader reader)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new TextField(name, reader);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new <see cref="TextField"/> with <see cref="string"/> value. </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="string"/> value </param>
        /// <param name="stored"> <see cref="Field.Store.YES"/> if the content should also be stored </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> or <paramref name="value"/> is <c>null</c>. </exception>
        public static TextField AddTextField(this Document document, string name, string value, Field.Store stored)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new TextField(name, value, stored);
            document.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a new un-stored <see cref="TextField"/> with <see cref="TokenStream"/> value. </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name"> field name </param>
        /// <param name="stream"> <see cref="TokenStream"/> value </param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException"> if this <paramref name="document"/>, the field <paramref name="name"/> or <paramref name="stream"/> is <c>null</c>. </exception>
        public static TextField AddTextField(this Document document, string name, TokenStream stream)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new TextField(name, stream);
            document.Add(field);
            return field;
        }
    }
}
