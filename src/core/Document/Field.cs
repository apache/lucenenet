using System;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Support;
using TokenStream = Lucene.Net.Analysis.TokenStream;


namespace Lucene.Net.Documents
{
    public class Field : IIndexableField
    {
        protected readonly FieldType type;
        protected readonly string name;
        protected Object fieldsData;
        protected TokenStream tokenStream;

        [NonSerialized]
        private TokenStream internalTokenStream;
        [NonSerialized]
        private ReusableStringReader internalReader;

        protected float boost = 1.0f;

        /**
         * Expert: creates a field with no initial value.
         * Intended only for custom Field subclasses.
         * @param name field name
         * @param type field type
         * @throws IllegalArgumentException if either the name or type
         *         is null.
         */
        protected Field(String name, FieldType type)
        {
            if (name == null)
            {
                throw new ArgumentException("name cannot be null");
            }
            this.name = name;
            if (type == null)
            {
                throw new ArgumentException("type cannot be null");
            }
            this.type = type;
        }

        public Field(String name, TextReader reader, FieldType type)
        {
            if (name == null)
            {
                throw new ArgumentException("name cannot be null");
            }
            if (type == null)
            {
                throw new ArgumentException("type cannot be null");
            }
            if (reader == null)
            {
                throw new NullReferenceException("reader cannot be null");
            }
            if (type.Stored)
            {
                throw new ArgumentException("fields with a Reader value cannot be stored");
            }
            if (type.Indexed && !type.Tokenized)
            {
                throw new ArgumentException("non-tokenized fields must use String values");
            }

            this.name = name;
            this.fieldsData = reader;
            this.type = type;
        }

        public Field(String name, TokenStream tokenStream, FieldType type)
        {
            if (name == null)
            {
                throw new ArgumentException("name cannot be null");
            }
            if (tokenStream == null)
            {
                throw new ArgumentException("tokenStream cannot be null");
            }
            if (!type.Indexed || !type.Tokenized)
            {
                throw new ArgumentException("TokenStream fields must be indexed and tokenized");
            }
            if (type.Stored)
            {
                throw new ArgumentException("TokenStream fields cannot be stored");
            }

            this.name = name;
            this.fieldsData = null;
            this.tokenStream = tokenStream;
            this.type = type;
        }

        public Field(String name, sbyte[] value, FieldType type)
            : this(name, value, 0, value.Length, type)
        {

        }

        public Field(String name, sbyte[] value, int offset, int length, FieldType type)
            : this(name, new BytesRef(value, offset, length), type)
        {
        }


        /**
         * Create field with binary value.
         *
         * <p>NOTE: the provided BytesRef is not copied so be sure
         * not to change it until you're done with this field.
         * @param name field name
         * @param bytes BytesRef pointing to binary content (not copied)
         * @param type field type
         * @throws IllegalArgumentException if the field name is null,
         *         or the field's type is indexed()
         * @throws NullPointerException if the type is null
         */
        public Field(String name, BytesRef bytes, FieldType type)
        {
            if (name == null)
            {
                throw new ArgumentException("name cannot be null");
            }
            if (type.Indexed)
            {
                throw new ArgumentException("Fields with BytesRef values cannot be indexed");
            }
            this.fieldsData = bytes;
            this.type = type;
            this.name = name;
        }
        // TODO: allow direct construction of int, long, float, double value too..?

        /**
         * Create field with String value.
         * @param name field name
         * @param value string value
         * @param type field type
         * @throws IllegalArgumentException if either the name or value
         *         is null, or if the field's type is neither indexed() nor stored(), 
         *         or if indexed() is false but storeTermVectors() is true.
         * @throws NullPointerException if the type is null
         */
        public Field(String name, String value, FieldType type)
        {
            if (name == null)
            {
                throw new ArgumentException("name cannot be null");
            }
            if (value == null)
            {
                throw new ArgumentException("value cannot be null");
            }
            if (!type.Stored && !type.Indexed)
            {
                throw new ArgumentException("it doesn't make sense to have a field that "
                  + "is neither indexed nor stored");
            }
            if (!type.Indexed && (type.StoreTermVectors))
            {
                throw new ArgumentException("cannot store term vector information "
                    + "for a field that is not indexed");
            }

            this.type = type;
            this.name = name;
            this.fieldsData = value;
        }


        /**
         * The value of the field as a String, or null. If null, the Reader value or
         * binary value is used. Exactly one of stringValue(), readerValue(), and
         * getBinaryValue() must be set.
         */

        public String StringValue
        {
            get
            {
                if (fieldsData is string || fieldsData is Number)
                {
                    return fieldsData.ToString();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (!(fieldsData is String))
                {
                    throw new ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to String");
                }
                fieldsData = value;
            }
        }

        /**
         * The value of the field as a Reader, or null. If null, the String value or
         * binary value is used. Exactly one of stringValue(), readerValue(), and
         * getBinaryValue() must be set.
         */

        public TextReader ReaderValue
        {
            get
            {
                return fieldsData is TextReader ? (TextReader)fieldsData : null;
            }
            set
            {
                if (!(fieldsData is TextReader))
                {
                    throw new ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Reader");
                }
                fieldsData = value;
            }
        }

        /**
         * The TokenStream for this field to be used when indexing, or null. If null,
         * the Reader value or String value is analyzed to produce the indexed tokens.
         */
        // .NET Port: Can't use property here due to overloaded TokenStream method
        public virtual TokenStream GetTokenStream()
        {
            return tokenStream;
        }

        public virtual void SetTokenStream(TokenStream value)
        {
            if (!type.Indexed || !type.Tokenized)
            {
                throw new ArgumentException("TokenStream fields must be indexed and tokenized");
            }
            if (type.NumericTypeValue != null)
            {
                throw new ArgumentException("cannot set private TokenStream on numeric fields");
            }
            this.tokenStream = value;
        }


        /**
         * Expert: change the value of this field. See 
         * {@link #setStringValue(String)}.
         */
        public virtual void SetBytesValue(sbyte[] value)
        {
            SetBytesValue(new BytesRef(value));
        }

        public virtual void SetBytesValue(BytesRef value)
        {
            if (!(fieldsData is BytesRef))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to BytesRef");
            }
            if (type.Indexed)
            {
                throw new ArgumentException("cannot set a BytesRef value on an indexed field");
            }
            fieldsData = value;
        }

        public virtual void SetByteValue(sbyte value)
        {
            if (!(fieldsData is SByte))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Byte");
            }
            fieldsData = value;
        }

        public virtual void SetShortValue(short value)
        {
            if (!(fieldsData is short))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Short");
            }
            fieldsData = value;
        }

        public virtual void SetIntValue(int value)
        {
            if (!(fieldsData is int))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Integer");
            }
            fieldsData = value;
        }

        public virtual void SetLongValue(long value)
        {
            if (!(fieldsData is long))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Long");
            }
            fieldsData = value;
        }

        public virtual void SetFloatValue(float value)
        {
            if (!(fieldsData is float))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Float");
            }
            fieldsData = value;
        }

        public virtual void SetDoubleValue(double value)
        {
            if (!(fieldsData is double))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetType().Name + " to Double");
            }
            fieldsData = value;
        }

        public String Name
        {
            get { return name; }
        }

        public float Boost
        {
            get { return boost; }
            set
            {
                if (type.Indexed == false || type.OmitNorms)
                {
                    throw new ArgumentException("You cannot set an index-time boost on an unindexed field, or one that omits norms");
                }
                boost = value;
            }
        }

        public object NumericValue
        {
            get
            {
                // .NET Port: No base type for all numeric types, so unless we want to rewrite this
                // to be LongValue, IntValue, FloatValue, etc, this will have to do.
                return fieldsData;
            }
        }

        public BytesRef BinaryValue
        {
            get
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
        }

        public override String ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append(type.ToString());
            result.Append('<');
            result.Append(name);
            result.Append(':');

            if (fieldsData != null)
            {
                result.Append(fieldsData);
            }

            result.Append('>');
            return result.ToString();
        }

        public IIndexableFieldType FieldTypeValue
        {
            get { return type; }
        }

        public TokenStream TokenStream(Analyzer analyzer)
        {
            if (!FieldTypeValue.Indexed)
            {
                return null;
            }

            FieldType.NumericType? numericType = ((FieldType)FieldTypeValue).NumericTypeValue;

            if (numericType != null)
            {
                if (!(internalTokenStream is NumericTokenStream))
                {
                    // lazy init the TokenStream as it is heavy to instantiate
                    // (attributes,...) if not needed (stored field loading)
                    internalTokenStream = new NumericTokenStream(type.NumericPrecisionStep);
                }
                NumericTokenStream nts = (NumericTokenStream)internalTokenStream;
                // initialize value in TokenStream
                Number val = (Number)fieldsData;
                switch (numericType)
                {
                    case FieldType.NumericType.INT:
                        nts.SetIntValue(Convert.ToInt32(val));
                        break;
                    case FieldType.NumericType.LONG:
                        nts.SetLongValue(Convert.ToInt64(val));
                        break;
                    case FieldType.NumericType.FLOAT:
                        nts.SetFloatValue(Convert.ToSingle(val));
                        break;
                    case FieldType.NumericType.DOUBLE:
                        nts.SetDoubleValue(Convert.ToDouble(val));
                        break;
                    default:
                        throw new Exception("Should never get here");
                }
                return internalTokenStream;
            }

            if (!FieldTypeValue.Tokenized)
            {
                if (StringValue == null)
                {
                    throw new ArgumentException("Non-Tokenized Fields must have a String value");
                }
                if (!(internalTokenStream is StringTokenStream))
                {
                    // lazy init the TokenStream as it is heavy to instantiate
                    // (attributes,...) if not needed (stored field loading)
                    internalTokenStream = new StringTokenStream();
                }
                ((StringTokenStream)internalTokenStream).SetValue(StringValue);
                return internalTokenStream;
            }

            if (tokenStream != null)
            {
                return tokenStream;
            }
            else if (ReaderValue != null)
            {
                return analyzer.TokenStream(Name, ReaderValue);
            }
            else if (StringValue != null)
            {
                if (internalReader == null)
                {
                    internalReader = new ReusableStringReader();
                }
                internalReader.SetValue(StringValue);
                return analyzer.TokenStream(Name, internalReader);
            }

            throw new ArgumentException("Field must have either TokenStream, String, Reader or Number value");
        }

        private sealed class ReusableStringReader : TextReader
        {
            private int pos = 0, size = 0;
            private String s = null;

            internal void SetValue(String s)
            {
                this.s = s;
                this.size = s.Length;
                this.pos = 0;
            }

            public override int Read()
            {
                if (pos < size)
                {
                    return s[pos++];
                }
                else
                {
                    s = null;
                    return -1;
                }
            }

            public override int Read(char[] c, int off, int len)
            {
                if (pos < size)
                {
                    len = Math.Min(len, size - pos);
                    TextSupport.GetCharsFromString(s, pos, pos + len, c, off);
                    pos += len;
                    return len;
                }
                else
                {
                    s = null;
                    return -1;
                }
            }

            public void Dispose()
            {
                pos = size; // this prevents NPE when reading after close!
                s = null;
            }
        }


        sealed class StringTokenStream : TokenStream
        {
            private ICharTermAttribute termAttribute;
            private IOffsetAttribute offsetAttribute;
            private bool used = false;
            private String value = null;

            public StringTokenStream()
            {
                InitBlock();
            }

            private void InitBlock()
            {
                termAttribute = AddAttribute<ICharTermAttribute>();
                offsetAttribute = AddAttribute<IOffsetAttribute>();
            }

            /** Sets the string value. */
            internal void SetValue(String value)
            {
                this.value = value;
            }

            public override bool IncrementToken()
            {
                if (used)
                {
                    return false;
                }
                ClearAttributes();
                termAttribute.Append(value);
                offsetAttribute.SetOffset(0, value.Length);
                used = true;
                return true;
            }

            public override void End()
            {
                int finalOffset = value.Length;
                offsetAttribute.SetOffset(finalOffset, finalOffset);
            }

            public override void Reset()
            {
                used = false;
            }

            protected override void Dispose(bool disposing)
            {
                value = null;
            }
        }

        /// <summary>Specifies whether and how a field should be stored. </summary>
        public enum Store
        {
            /// <summary>Store the original field value in the index. This is useful for short texts
            /// like a document's title which should be displayed with the results. The
            /// value is stored in its original form, i.e. no analyzer is used before it is
            /// stored.
            /// </summary>
            YES,


            /// <summary>Do not store the field value in the index. </summary>
            NO
        }

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

        public static FieldType TranslateFieldType(Store store, Index index, TermVector termVector)
        {
            FieldType ft = new FieldType();

            ft.Stored = store == Store.YES;

            switch (index)
            {
                case Index.ANALYZED:
                    ft.Indexed = true;
                    ft.Tokenized = true;
                    break;
                case Index.ANALYZED_NO_NORMS:
                    ft.Indexed = true;
                    ft.Tokenized = true;
                    ft.OmitNorms = true;
                    break;
                case Index.NOT_ANALYZED:
                    ft.Indexed = true;
                    ft.Tokenized = false;
                    break;
                case Index.NOT_ANALYZED_NO_NORMS:
                    ft.Indexed = true;
                    ft.Tokenized = false;
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

        [Obsolete("Use StringField, TextField instead.")]
        public Field(String name, String value, Store store, Index index)
            : this(name, value, TranslateFieldType(store, index, TermVector.NO))
        {
        }

        [Obsolete("Use StringField, TextField instead.")]
        public Field(String name, String value, Store store, Index index, TermVector termVector)
            : this(name, value, TranslateFieldType(store, index, termVector))
        {
        }

        [Obsolete("Use TextField instead.")]
        public Field(String name, TextReader reader)
            : this(name, reader, TermVector.NO)
        {
        }

        [Obsolete("Use TextField instead.")]
        public Field(String name, TextReader reader, TermVector termVector)
            : this(name, reader, TranslateFieldType(Store.NO, Index.ANALYZED, termVector))
        {
        }

        [Obsolete("Use TextField instead.")]
        public Field(String name, TokenStream tokenStream)
            : this(name, tokenStream, TermVector.NO)
        {
        }

        [Obsolete("Use TextField instead.")]
        public Field(String name, TokenStream tokenStream, TermVector termVector)
            : this(name, tokenStream, TranslateFieldType(Store.NO, Index.ANALYZED, termVector))
        {
        }

        [Obsolete("Use StoredField instead.")]
        public Field(String name, sbyte[] value)
            : this(name, value, TranslateFieldType(Store.YES, Index.NO, TermVector.NO))
        {
        }

        [Obsolete("Use StoredField instead.")]
        public Field(String name, sbyte[] value, int offset, int length)
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