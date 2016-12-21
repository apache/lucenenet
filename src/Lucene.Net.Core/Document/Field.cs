using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;

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
    /// Expert: directly create a field for a document.  Most
    /// users should use one of the sugar subclasses: {@link
    /// IntField}, <seealso cref="LongField"/>, <seealso cref="FloatField"/>, {@link
    /// DoubleField}, <seealso cref="BinaryDocValuesField"/>, {@link
    /// NumericDocValuesField}, <seealso cref="SortedDocValuesField"/>, {@link
    /// StringField}, <seealso cref="TextField"/>, <seealso cref="StoredField"/>.
    ///
    /// <p/> A field is a section of a Document. Each field has three
    /// parts: name, type and value. Values may be text
    /// (String, TextReader or pre-analyzed TokenStream), binary
    /// (byte[]), or numeric (a Number).  Fields are optionally stored in the
    /// index, so that they may be returned with hits on the document.
    ///
    /// <p/>
    /// NOTE: the field type is an <seealso cref="IIndexableFieldType"/>.  Making changes
    /// to the state of the IndexableFieldType will impact any
    /// Field it is used in.  It is strongly recommended that no
    /// changes be made after Field instantiation.
    /// </summary>
    public class Field : IIndexableField
    {
        /// <summary>
        /// Field's type
        /// </summary>
        protected internal readonly FieldType mType;

        /// <summary>
        /// Field's name
        /// </summary>
        protected internal readonly string mName;

        /// <summary>
        /// Field's value </summary>
        protected internal object fieldsData;

        /// <summary>
        /// Pre-analyzed tokenStream for indexed fields; this is
        /// separate from fieldsData because you are allowed to
        /// have both; eg maybe field has a String value but you
        /// customize how it's tokenized
        /// </summary>
        protected internal TokenStream tokenStream;

        private TokenStream internalTokenStream;

        /// <summary>
        /// Field's boost </summary>
        /// <seealso cref= #boost() </seealso>
        protected internal float mBoost = 1.0f;

        /// <summary>
        /// Expert: creates a field with no initial value.
        /// Intended only for custom Field subclasses. </summary>
        /// <param name="name"> field name </param>
        /// <param name="type"> field type </param>
        /// <exception cref="ArgumentNullException"> if either the name or type
        ///         is null. </exception>
        protected internal Field(string name, FieldType type)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException("name", "name cannot be null");
            }
            this.mName = name;
            if (type == null)
            {
                throw new System.ArgumentNullException("type", "type cannot be null");
            }
            this.mType = type;
        }

        /// <summary>
        /// Create field with TextReader value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="reader"> reader value </param>
        /// <param name="type"> field type </param>
        /// <exception cref="ArgumentNullException"> if either the name or type
        ///         is null, or if the field's type is stored(), or
        ///         if tokenized() is false. </exception>
        /// <exception cref="ArgumentNullException"> if the reader is null </exception>
        public Field(string name, TextReader reader, FieldType type)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException("name", "name cannot be null");
            }
            if (type == null)
            {
                throw new System.ArgumentNullException("type", "type cannot be null");
            }
            if (reader == null)
            {
                throw new System.ArgumentNullException("reader", "reader cannot be null");
            }
            if (type.IsStored)
            {
                throw new System.ArgumentException("fields with a Reader value cannot be stored");
            }
            if (type.IsIndexed && !type.IsTokenized)
            {
                throw new System.ArgumentException("non-tokenized fields must use String values");
            }

            this.mName = name;
            this.fieldsData = reader;
            this.mType = type;
        }

        /// <summary>
        /// Create field with TokenStream value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="tokenStream"> TokenStream value </param>
        /// <param name="type"> field type </param>
        /// <exception cref="ArgumentException"> if either the name or type
        ///         is null, or if the field's type is stored(), or
        ///         if tokenized() is false, or if indexed() is false. </exception>
        /// <exception cref="ArgumentNullException"> if the tokenStream is null </exception>
        public Field(string name, TokenStream tokenStream, FieldType type)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException("name", "name cannot be null");
            }
            if (tokenStream == null)
            {
                throw new System.ArgumentNullException("tokenStream","tokenStream cannot be null");
            }
            if (!type.IsIndexed || !type.IsTokenized)
            {
                throw new System.ArgumentException("TokenStream fields must be indexed and tokenized");
            }
            if (type.IsStored)
            {
                throw new System.ArgumentException("TokenStream fields cannot be stored");
            }

            this.mName = name;
            this.fieldsData = null;
            this.tokenStream = tokenStream;
            this.mType = type;
        }

        /// <summary>
        /// Create field with binary value.
        ///
        /// <p>NOTE: the provided byte[] is not copied so be sure
        /// not to change it until you're done with this field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> byte array pointing to binary content (not copied) </param>
        /// <param name="type"> field type </param>
        /// <exception cref="ArgumentException"> if the field name is null,
        ///         or the field's type is indexed() </exception>
        /// <exception cref="ArgumentNullException"> if the type is null </exception>
        public Field(string name, byte[] value, FieldType type)
            : this(name, value, 0, value.Length, type)
        {
        }

        /// <summary>
        /// Create field with binary value.
        ///
        /// <p>NOTE: the provided byte[] is not copied so be sure
        /// not to change it until you're done with this field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> byte array pointing to binary content (not copied) </param>
        /// <param name="offset"> starting position of the byte array </param>
        /// <param name="length"> valid length of the byte array </param>
        /// <param name="type"> field type </param>
        /// <exception cref="ArgumentException"> if the field name is null,
        ///         or the field's type is indexed() </exception>
        /// <exception cref="ArgumentNullException"> if the type is null </exception>
        public Field(string name, byte[] value, int offset, int length, FieldType type)
            : this(name, new BytesRef(value, offset, length), type)
        {
        }

        /// <summary>
        /// Create field with binary value.
        ///
        /// <p>NOTE: the provided BytesRef is not copied so be sure
        /// not to change it until you're done with this field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="bytes"> BytesRef pointing to binary content (not copied) </param>
        /// <param name="type"> field type </param>
        /// <exception cref="ArgumentException"> if the field name is null,
        ///         or the field's type is indexed() </exception>
        /// <exception cref="ArgumentNullException"> if the type is null </exception>
        public Field(string name, BytesRef bytes, FieldType type)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException("name", "name cannot be null");
            }
            if (type.IsIndexed)
            {
                throw new System.ArgumentException("Fields with BytesRef values cannot be indexed");
            }
            this.fieldsData = bytes;
            this.mType = type;
            this.mName = name;
        }

        // TODO: allow direct construction of int, long, float, double value too..?

        /// <summary>
        /// Create field with String value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> string value </param>
        /// <param name="type"> field type </param>
        /// <exception cref="ArgumentException"> if either the name or value
        ///         is null, or if the field's type is neither indexed() nor stored(),
        ///         or if indexed() is false but storeTermVectors() is true. </exception>
        /// <exception cref="ArgumentNullException"> if the type is null </exception>
        public Field(string name, string value, FieldType type)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException("name", "name cannot be null");
            }
            if (value == null)
            {
                throw new System.ArgumentNullException("value", "value cannot be null");
            }
            if (!type.IsStored && !type.IsIndexed)
            {
                throw new System.ArgumentException("it doesn't make sense to have a field that " + "is neither indexed nor stored");
            }
            if (!type.IsIndexed && (type.StoreTermVectors))
            {
                throw new System.ArgumentException("cannot store term vector information " + "for a field that is not indexed");
            }

            this.mType = type;
            this.mName = name;
            this.fieldsData = value;
        }

        /// <summary>
        /// The value of the field as a string, or null. If null, the <see cref="TextReader"/> value or
        /// binary value is used. Exactly one of <see cref="GetStringValue()"/>, <see cref="GetReaderValue()"/>, and
        /// <see cref="GetBinaryValue()"/> must be set.
        /// </summary>
        public virtual string GetStringValue() // LUCENENET specific: Added verb Get to make it more clear that this returns the value
        {
            return fieldsData == null ? null : fieldsData.ToString();

            /*if (FieldsData is string || FieldsData is Number)
            {
            return FieldsData.ToString();
            }
            else
            {
                return null;
            }*/
        }

        /// <summary>
        /// The value of the field as a <see cref="TextReader"/>, or null. If null, the string value or
        /// binary value is used. Exactly one of <see cref="GetStringValue()"/>, <see cref="GetReaderValue()"/>, and
        /// <see cref="GetBinaryValue()"/> must be set.
        /// </summary>
        public virtual TextReader GetReaderValue() // LUCENENET specific: Added verb Get to make it more clear that this returns the value
        {
            return fieldsData is TextReader ? (TextReader)fieldsData : null;
        }

        /// <summary>
        /// The TokenStream for this field to be used when indexing, or null. If null,
        /// the TextReader value or String value is analyzed to produce the indexed tokens.
        /// </summary>
        public virtual TokenStream GetTokenStreamValue() // LUCENENET specific: Added verb Get to make it more clear that this returns the value
        {
            return tokenStream;
        }

        /// <summary>
        /// <p>
        /// Expert: change the value of this field. this can be used during indexing to
        /// re-use a single Field instance to improve indexing speed by avoiding GC
        /// cost of new'ing and reclaiming Field instances. Typically a single
        /// <seealso cref="Document"/> instance is re-used as well. this helps most on small
        /// documents.
        /// </p>
        ///
        /// <p>
        /// Each Field instance should only be used once within a single
        /// <seealso cref="Document"/> instance. See <a
        /// href="http://wiki.apache.org/lucene-java/ImproveIndexingSpeed"
        /// >ImproveIndexingSpeed</a> for details.
        /// </p>
        /// </summary>
        public virtual void SetStringValue(string value)
        {
            if (!(fieldsData is string))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to string");
            }
            fieldsData = value;
        }

        /// <summary>
        /// Expert: change the value of this field. See 
        /// <see cref="SetStringValue(string)"/>.
        /// </summary>
        public virtual void SetReaderValue(TextReader value)
        {
            if (!(fieldsData is TextReader))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to TextReader");
            }
            fieldsData = value;
        }

        /// <summary>
        /// Expert: change the value of this field. See
        /// <see cref="SetStringValue(string)"/>.
        ///
        /// <p>NOTE: the provided BytesRef is not copied so be sure
        /// not to change it until you're done with this field.
        /// </summary>
        public virtual void SetBytesValue(BytesRef value)
        {
            if (!(fieldsData is BytesRef))
            {
                throw new System.ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to BytesRef");
            }
            if (mType.IsIndexed)
            {
                throw new System.ArgumentException("cannot set a BytesRef value on an indexed field");
            }
            fieldsData = value;
        }

        /// <summary>
        /// Expert: change the value of this field. See
        /// <see cref="SetStringValue(string)"/>.
        /// </summary>
        public virtual void SetBytesValue(byte[] value)
        {
            SetBytesValue(new BytesRef(value));
        }

        /// <summary>
        /// Expert: change the value of this field. See
        /// <see cref="SetStringValue(string)"/>.
        /// </summary>
        public virtual void SetByteValue(byte value)
        {
            if (!(fieldsData is byte?))
            {
                throw new System.ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Byte");
            }
            fieldsData = Convert.ToByte(value);
        }

        /// <summary>
        /// Expert: change the value of this field. See
        /// <see cref="SetStringValue(string)"/>.
        /// </summary>
        public virtual void SetInt16Value(short value) // LUCENENET specific: Renamed from SetShortValue to follow .NET conventions
        {
            if (!(fieldsData is short?))
            {
                throw new System.ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Short");
            }
            fieldsData = Convert.ToInt16(value);
        }

        /// <summary>
        /// Expert: change the value of this field. See
        /// <see cref="SetStringValue(string)"/>.
        /// </summary>
        public virtual void SetInt32Value(int value) // LUCENENET specific: Renamed from SetIntValue to follow .NET conventions
        {
            if (!(fieldsData is int?))
            {
                throw new System.ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Integer");
            }
            fieldsData = Convert.ToInt32(value);
        }

        /// <summary>
        /// Expert: change the value of this field. See
        /// <see cref="SetStringValue(string)"/>.
        /// </summary>
        public virtual void SetInt64Value(long value) // LUCENENET specific: Renamed from SetLongValue to follow .NET conventions
        {
            if (!(fieldsData is long?))
            {
                throw new System.ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Long");
            }
            fieldsData = Convert.ToInt64(value);
        }

        /// <summary>
        /// Expert: change the value of this field. See
        /// <see cref="SetStringValue(string)"/>.
        /// </summary>
        public virtual void SetSingleValue(float value) // LUCENENET specific: Renamed from SetFloatValue to follow .NET conventions
        {
            if (!(fieldsData is float?))
            {
                throw new System.ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Float");
            }
            fieldsData = Convert.ToSingle(value);
        }

        /// <summary>
        /// Expert: change the value of this field. See
        /// <see cref="SetStringValue(string)"/>.
        /// </summary>
        public virtual void SetDoubleValue(double value)
        {
            if (!(fieldsData is double?))
            {
                throw new System.ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Double");
            }
            fieldsData = Convert.ToDouble(value);
        }

        // LUCENENET TODO: Add SetValue() overloads for each type?
        // Upside: Simpler API.
        // Downside: Must be vigilant about what type is passed or the wrong overload will be called and will get a runtime exception. 

        /// <summary>
        /// Expert: sets the token stream to be used for indexing and causes
        /// isIndexed() and isTokenized() to return true. May be combined with stored
        /// values from stringValue() or getBinaryValue()
        /// </summary>
        public virtual void SetTokenStream(TokenStream tokenStream)
        {
            if (!mType.IsIndexed || !mType.IsTokenized)
            {
                throw new System.ArgumentException("TokenStream fields must be indexed and tokenized");
            }
            if (mType.NumericType != null)
            {
                throw new System.ArgumentException("cannot set private TokenStream on numeric fields");
            }
            this.tokenStream = tokenStream;
        }

        public virtual string Name
        {
            get { return mName; }
        }

        /// <summary>
        /// Gets or sets the boost factor on this field. </summary>
        /// <remarks>The default value is <c>1.0f</c> (no boost).</remarks>
        /// <exception cref="ArgumentException"> if this field is not indexed,
        ///         or if it omits norms. </exception>
        public virtual float Boost
        {
            get
            {
                return mBoost;
            }
            set
            {
                if (value != 1.0f)
                {
                    if (mType.IsIndexed == false || mType.OmitNorms)
                    {
                        throw new System.ArgumentException("You cannot set an index-time boost on an unindexed field, or one that omits norms");
                    }
                }
                this.mBoost = value;
            }
        }

        public virtual object GetNumericValue() // LUCENENET specific: Added verb Get to make it more clear that this returns the value
        {
            // LUCENENET TODO: There was no expensive conversion from string in the original
            string str = fieldsData as string;
            if (str != null)
            {
                long ret;
                if (long.TryParse(str, out ret))
                {
                    return ret;
                }
            }

            if (fieldsData is int || fieldsData is float || fieldsData is double || fieldsData is long)
            {
                return fieldsData;
            }

            return null;
        }

        public virtual BytesRef GetBinaryValue() // LUCENENET specific: Added verb Get to make it more clear that this returns the value
        {
            if (fieldsData is BytesRef)
            {
                return (BytesRef)fieldsData;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Prints a Field for human consumption. </summary>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append(mType.ToString());
            result.Append('<');
            result.Append(mName);
            result.Append(':');

            if (fieldsData != null)
            {
                result.Append(fieldsData);
            }

            result.Append('>');
            return result.ToString();
        }

        /// <summary>
        /// Returns the <seealso cref="FieldType"/> for this field. </summary>
        public virtual IIndexableFieldType FieldType
        {
            get { return mType; }
        }

        public virtual TokenStream GetTokenStream(Analyzer analyzer)
        {
            if (!((FieldType)FieldType).IsIndexed)
            {
                return null;
            }
            NumericType? numericType = ((FieldType)FieldType).NumericType;
            if (numericType != null)
            {
                if (!(internalTokenStream is NumericTokenStream))
                {
                    // lazy init the TokenStream as it is heavy to instantiate
                    // (attributes,...) if not needed (stored field loading)
                    internalTokenStream = new NumericTokenStream(mType.NumericPrecisionStep);
                }
                var nts = (NumericTokenStream)internalTokenStream;
                // initialize value in TokenStream
                object val = fieldsData;
                switch (numericType)
                {
                    case NumericType.INT:
                        nts.SetIntValue(Convert.ToInt32(val));
                        break;

                    case NumericType.LONG:
                        nts.SetLongValue(Convert.ToInt64(val));
                        break;

                    case NumericType.FLOAT:
                        nts.SetFloatValue(Convert.ToSingle(val));
                        break;

                    case NumericType.DOUBLE:
                        nts.SetDoubleValue(Convert.ToDouble(val));
                        break;

                    default:
                        throw new Exception("Should never get here");
                }
                return internalTokenStream;
            }

            if (!((FieldType)FieldType).IsTokenized)
            {
                if (GetStringValue() == null)
                {
                    throw new System.ArgumentException("Non-Tokenized Fields must have a String value");
                }
                if (!(internalTokenStream is StringTokenStream))
                {
                    // lazy init the TokenStream as it is heavy to instantiate
                    // (attributes,...) if not needed (stored field loading)
                    internalTokenStream = new StringTokenStream();
                }
                ((StringTokenStream)internalTokenStream).SetValue(GetStringValue());
                return internalTokenStream;
            }

            if (tokenStream != null)
            {
                return tokenStream;
            }
            else if (GetReaderValue() != null)
            {
                return analyzer.TokenStream(Name, GetReaderValue());
            }
            else if (GetStringValue() != null)
            {
                TextReader sr = new StringReader(GetStringValue());
                return analyzer.TokenStream(Name, sr);
            }

            throw new System.ArgumentException("Field must have either TokenStream, String, Reader or Number value; got " + this);
        }

        internal sealed class StringTokenStream : TokenStream
        {
            internal void InitializeInstanceFields()
            {
                TermAttribute = AddAttribute<ICharTermAttribute>();
                OffsetAttribute = AddAttribute<IOffsetAttribute>();
            }

            internal ICharTermAttribute TermAttribute;
            internal IOffsetAttribute OffsetAttribute;
            internal bool Used = false;
            internal string value = null;

            /// <summary>
            /// Creates a new TokenStream that returns a String as single token.
            /// <p>Warning: Does not initialize the value, you must call
            /// <seealso cref="#setValue(String)"/> afterwards!
            /// </summary>
            internal StringTokenStream()
            {
                InitializeInstanceFields();
            }

            /// <summary>
            /// Sets the string value. </summary>
            internal void SetValue(string value)
            {
                this.value = value;
            }

            public override bool IncrementToken()
            {
                if (Used)
                {
                    return false;
                }
                ClearAttributes();
                TermAttribute.Append(value);
                OffsetAttribute.SetOffset(0, value.Length);
                Used = true;
                return true;
            }

            public override void End()
            {
                base.End();
                int finalOffset = value.Length;
                OffsetAttribute.SetOffset(finalOffset, finalOffset);
            }

            public override void Reset()
            {
                Used = false;
            }

            public void Dispose(bool disposing)
            {
                if (disposing)
                    value = null;
            }
        }

        /// <summary>
        /// Specifies whether and how a field should be stored. </summary>
        public enum Store
        {
            /// <summary>
            /// Store the original field value in the index. this is useful for short texts
            /// like a document's title which should be displayed with the results. The
            /// value is stored in its original form, i.e. no analyzer is used before it is
            /// stored.
            /// </summary>
            YES,

            /// <summary>
            /// Do not store the field's value in the index. </summary>
            NO
        }

        //
        // Deprecated transition API below:
        //

        /// <summary>
        /// Specifies whether and how a field should be indexed.
        /// <summary>Specifies whether and how a field should be indexed. </summary>
        [Obsolete]
        public enum Index
        {
            /// <summary>Do not index the field value. This field can thus not be searched,
            /// but one can still access its contents provided it is
            /// <see cref="Field.Store">stored</see>.
            /// </summary>
            NO,

            /// <summary>Index the tokens produced by running the field's
            /// value through an Analyzer.  This is useful for
            /// common text.
            /// </summary>
            ANALYZED,

            /// <summary>Index the field's value without using an Analyzer, so it can be searched.
            /// As no analyzer is used the value will be stored as a single term. This is
            /// useful for unique Ids like product numbers.
            /// </summary>
            NOT_ANALYZED,

            /// <summary>Expert: Index the field's value without an Analyzer,
            /// and also disable the storing of norms.  Note that you
            /// can also separately enable/disable norms by setting
            /// <see cref="AbstractField.OmitNorms" />.  No norms means that
            /// index-time field and document boosting and field
            /// length normalization are disabled.  The benefit is
            /// less memory usage as norms take up one byte of RAM
            /// per indexed field for every document in the index,
            /// during searching.  Note that once you index a given
            /// field <i>with</i> norms enabled, disabling norms will
            /// have no effect.  In other words, for this to have the
            /// above described effect on a field, all instances of
            /// that field must be indexed with NOT_ANALYZED_NO_NORMS
            /// from the beginning.
            /// </summary>
            NOT_ANALYZED_NO_NORMS,

            /// <summary>Expert: Index the tokens produced by running the
            /// field's value through an Analyzer, and also
            /// separately disable the storing of norms.  See
            /// <see cref="NOT_ANALYZED_NO_NORMS" /> for what norms are
            /// and why you may want to disable them.
            /// </summary>
            ANALYZED_NO_NORMS,
        }

        /// <summary>Specifies whether and how a field should have term vectors. </summary>
        [Obsolete]
        public enum TermVector
        {
            /// <summary>Do not store term vectors. </summary>
            NO,

            /// <summary>Store the term vectors of each document. A term vector is a list
            /// of the document's terms and their number of occurrences in that document.
            /// </summary>
            YES,

            /// <summary> Store the term vector + token position information
            ///
            /// </summary>
            /// <seealso cref="YES">
            /// </seealso>
            WITH_POSITIONS,

            /// <summary> Store the term vector + Token offset information
            ///
            /// </summary>
            /// <seealso cref="YES">
            /// </seealso>
            WITH_OFFSETS,

            /// <summary> Store the term vector + Token position and offset information
            ///
            /// </summary>
            /// <seealso cref="YES">
            /// </seealso>
            /// <seealso cref="WITH_POSITIONS">
            /// </seealso>
            /// <seealso cref="WITH_OFFSETS">
            /// </seealso>
            WITH_POSITIONS_OFFSETS,
        }

        /// <summary>
        /// Translates the pre-4.0 enums for specifying how a
        /// field should be indexed into the 4.0 {@link FieldType}
        /// approach.
        /// </summary>
        [Obsolete("This is here only to ease transition from the pre-4.0 APIs.")]
        public static FieldType TranslateFieldType(Store store, Index index, TermVector termVector)
        {
            FieldType ft = new FieldType();

            ft.IsStored = store == Store.YES;

            switch (index)
            {
                case Index.ANALYZED:
                    ft.IsIndexed = true;
                    ft.IsTokenized = true;
                    break;

                case Index.ANALYZED_NO_NORMS:
                    ft.IsIndexed = true;
                    ft.IsTokenized = true;
                    ft.OmitNorms = true;
                    break;

                case Index.NOT_ANALYZED:
                    ft.IsIndexed = true;
                    ft.IsTokenized = false;
                    break;

                case Index.NOT_ANALYZED_NO_NORMS:
                    ft.IsIndexed = true;
                    ft.IsTokenized = false;
                    ft.OmitNorms = true;
                    break;

                case Index.NO:
                    break;
            }

            switch (termVector)
            {
                case TermVector.NO:
                    break;

                case TermVector.YES:
                    ft.StoreTermVectors = true;
                    break;

                case TermVector.WITH_POSITIONS:
                    ft.StoreTermVectors = true;
                    ft.StoreTermVectorPositions = true;
                    break;

                case TermVector.WITH_OFFSETS:
                    ft.StoreTermVectors = true;
                    ft.StoreTermVectorOffsets = true;
                    break;

                case TermVector.WITH_POSITIONS_OFFSETS:
                    ft.StoreTermVectors = true;
                    ft.StoreTermVectorPositions = true;
                    ft.StoreTermVectorOffsets = true;
                    break;
            }
            ft.Freeze();
            return ft;
        }

        // LUCENENET TODO: Documentation
        [Obsolete("Use StringField, TextField instead.")]
        public Field(string name, string value, Store store, Index index)
            : this(name, value, TranslateFieldType(store, index, TermVector.NO))
        {
        }

        // LUCENENET TODO: Documentation
        [Obsolete("Use StringField, TextField instead.")]
        public Field(string name, string value, Store store, Index index, TermVector termVector)
            : this(name, value, TranslateFieldType(store, index, termVector))
        {
        }

        // LUCENENET TODO: Documentation
        [Obsolete("Use TextField instead.")]
        public Field(string name, TextReader reader)
            : this(name, reader, TermVector.NO)
        {
        }

        // LUCENENET TODO: Documentation
        [Obsolete("Use TextField instead.")]
        public Field(string name, TextReader reader, TermVector termVector)
            : this(name, reader, TranslateFieldType(Store.NO, Index.ANALYZED, termVector))
        {
        }

        // LUCENENET TODO: Documentation
        [Obsolete("Use TextField instead.")]
        public Field(string name, TokenStream tokenStream)
            : this(name, tokenStream, TermVector.NO)
        {
        }

        // LUCENENET TODO: Documentation
        [Obsolete("Use TextField instead.")]
        public Field(string name, TokenStream tokenStream, TermVector termVector)
            : this(name, tokenStream, TranslateFieldType(Store.NO, Index.ANALYZED, termVector))
        {
        }

        // LUCENENET TODO: Documentation
        [Obsolete("Use StoredField instead.")]
        public Field(string name, byte[] value)
            : this(name, value, TranslateFieldType(Store.YES, Index.NO, TermVector.NO))
        {
        }

        // LUCENENET TODO: Documentation
        [Obsolete("Use StoredField instead.")]
        public Field(string name, byte[] value, int offset, int length)
            : this(name, value, offset, length, TranslateFieldType(Store.YES, Index.NO, TermVector.NO))
        {
        }
    }

    public static class FieldExtensions
    {
        public static bool IsStored(this Field.Store store)
        {
            switch (store)
            {
                case Field.Store.YES:
                    return true;

                case Field.Store.NO:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException("store", "Invalid value for Field.Store");
            }
        }

        public static bool IsIndexed(this Field.Index index)
        {
            switch (index)
            {
                case Field.Index.NO:
                    return false;

                case Field.Index.ANALYZED:
                case Field.Index.NOT_ANALYZED:
                case Field.Index.NOT_ANALYZED_NO_NORMS:
                case Field.Index.ANALYZED_NO_NORMS:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException("index", "Invalid value for Field.Index");
            }
        }

        public static bool IsAnalyzed(this Field.Index index)
        {
            switch (index)
            {
                case Field.Index.NO:
                case Field.Index.NOT_ANALYZED:
                case Field.Index.NOT_ANALYZED_NO_NORMS:
                    return false;

                case Field.Index.ANALYZED:
                case Field.Index.ANALYZED_NO_NORMS:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException("index", "Invalid value for Field.Index");
            }
        }

        public static bool OmitNorms(this Field.Index index)
        {
            switch (index)
            {
                case Field.Index.ANALYZED:
                case Field.Index.NOT_ANALYZED:
                    return false;

                case Field.Index.NO:
                case Field.Index.NOT_ANALYZED_NO_NORMS:
                case Field.Index.ANALYZED_NO_NORMS:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException("index", "Invalid value for Field.Index");
            }
        }

        public static bool IsStored(this Field.TermVector tv)
        {
            switch (tv)
            {
                case Field.TermVector.NO:
                    return false;

                case Field.TermVector.YES:
                case Field.TermVector.WITH_OFFSETS:
                case Field.TermVector.WITH_POSITIONS:
                case Field.TermVector.WITH_POSITIONS_OFFSETS:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException("tv", "Invalid value for Field.TermVector");
            }
        }

        public static bool WithPositions(this Field.TermVector tv)
        {
            switch (tv)
            {
                case Field.TermVector.NO:
                case Field.TermVector.YES:
                case Field.TermVector.WITH_OFFSETS:
                    return false;

                case Field.TermVector.WITH_POSITIONS:
                case Field.TermVector.WITH_POSITIONS_OFFSETS:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException("tv", "Invalid value for Field.TermVector");
            }
        }

        public static bool WithOffsets(this Field.TermVector tv)
        {
            switch (tv)
            {
                case Field.TermVector.NO:
                case Field.TermVector.YES:
                case Field.TermVector.WITH_POSITIONS:
                    return false;

                case Field.TermVector.WITH_OFFSETS:
                case Field.TermVector.WITH_POSITIONS_OFFSETS:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException("tv", "Invalid value for Field.TermVector");
            }
        }

        public static Field.Index ToIndex(bool indexed, bool analyed)
        {
            return ToIndex(indexed, analyed, false);
        }

        public static Field.Index ToIndex(bool indexed, bool analyzed, bool omitNorms)
        {
            // If it is not indexed nothing else matters
            if (!indexed)
            {
                return Field.Index.NO;
            }

            // typical, non-expert
            if (!omitNorms)
            {
                if (analyzed)
                {
                    return Field.Index.ANALYZED;
                }
                return Field.Index.NOT_ANALYZED;
            }

            // Expert: Norms omitted
            if (analyzed)
            {
                return Field.Index.ANALYZED_NO_NORMS;
            }
            return Field.Index.NOT_ANALYZED_NO_NORMS;
        }

        /// <summary>
        /// Get the best representation of a TermVector given the flags.
        /// </summary>
        public static Field.TermVector ToTermVector(bool stored, bool withOffsets, bool withPositions)
        {
            // If it is not stored, nothing else matters.
            if (!stored)
            {
                return Field.TermVector.NO;
            }

            if (withOffsets)
            {
                if (withPositions)
                {
                    return Field.TermVector.WITH_POSITIONS_OFFSETS;
                }
                return Field.TermVector.WITH_OFFSETS;
            }

            if (withPositions)
            {
                return Field.TermVector.WITH_POSITIONS;
            }
            return Field.TermVector.YES;
        }
    }
}