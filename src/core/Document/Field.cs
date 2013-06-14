using System;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Support;
using TokenStream = Lucene.Net.Analysis.TokenStream;


namespace Lucene.Net.Document
{

    public class Field : IndexableField
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
            if (type.Stored())
            {
                throw new ArgumentException("fields with a Reader value cannot be stored");
            }
            if (type.Indexed() && !type.Tokenized())
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
            if (!type.Indexed() || !type.Tokenized())
            {
                throw new ArgumentException("TokenStream fields must be indexed and tokenized");
            }
            if (type.Stored())
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
            if (type.Indexed())
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
            if (!type.Stored() && !type.Indexed())
            {
                throw new ArgumentException("it doesn't make sense to have a field that "
                  + "is neither indexed nor stored");
            }
            if (!type.Indexed() && (type.StoreTermVectors()))
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

        public override String StringValue()
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

        /**
         * The value of the field as a Reader, or null. If null, the String value or
         * binary value is used. Exactly one of stringValue(), readerValue(), and
         * getBinaryValue() must be set.
         */

        public override TextReader ReaderValue()
        {
            return fieldsData is TextReader ? (TextReader)fieldsData : null;
        }

        /**
         * The TokenStream for this field to be used when indexing, or null. If null,
         * the Reader value or String value is analyzed to produce the indexed tokens.
         */
        public virtual TokenStream TokenStreamValue()
        {
            return tokenStream;
        }

        public virtual void SetStringValue(String value)
        {
            if (!(fieldsData is String))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetClass().GetSimpleName() + " to String");
            }
            fieldsData = value;
        }

        /**
         * Expert: change the value of this field. See 
         * {@link #setStringValue(String)}.
         */
        public virtual void SetReaderValue(TextReader value)
        {
            if (!(fieldsData is TextReader))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetClass().GetSimpleName() + " to Reader");
            }
            fieldsData = value;
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
                throw new ArgumentException("cannot change value type from " + fieldsData.GetClass().GetSimpleName() + " to BytesRef");
            }
            if (type.Indexed())
            {
                throw new ArgumentException("cannot set a BytesRef value on an indexed field");
            }
            fieldsData = value;
        }

        public virtual void SetByteValue(sbyte value)
        {
            if (!(fieldsData is SByte))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetClass().GetSimpleName() + " to Byte");
            }
            fieldsData = Convert.ToByte(value);
        }

        public virtual void SetShortValue(short value)
        {
            if (!(fieldsData is short))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetClass().GetSimpleName() + " to Short");
            }
            fieldsData = Convert.ToInt16(value);
        }

        public void SetIntValue(int value)
        {
            if (!(fieldsData is int))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetClass().GetSimpleName() + " to Integer");
            }
            fieldsData = Convert.ToInt32(value);
        }

        public virtual void SetLongValue(long value)
        {
            if (!(fieldsData is long))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetClass().GetSimpleName() + " to Long");
            }
            fieldsData = Convert.ToInt64(value);
        }

        public virtual void SetFloatValue(float value)
        {
            if (!(fieldsData is float))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetClass().GetSimpleName() + " to Float");
            }
            fieldsData = Convert.ToSingle(value);
        }

        public virtual void SetDoubleValue(double value)
        {
            if (!(fieldsData is double))
            {
                throw new ArgumentException("cannot change value type from " + fieldsData.GetClass().GetSimpleName() + " to Double");
            }
            fieldsData = Convert.ToDouble(value);
        }

        public virtual void SetTokenStream(TokenStream tokenStream)
        {
            if (!type.Indexed() || !type.Tokenized())
            {
                throw new ArgumentException("TokenStream fields must be indexed and tokenized");
            }
            if (type.NumericType() != null)
            {
                throw new ArgumentException("cannot set private TokenStream on numeric fields");
            }
            this.tokenStream = tokenStream;
        }

         public override String Name
	    {
             get { return name; }
	    }

        public override float Boost
        {
            get { return boost; }
            set
            {
                if (boost != 1.0f)
                {
                    if (type.Indexed() == false || type.OmitNorms())
                    {
                        throw new ArgumentException("You cannot set an index-time boost on an unindexed field, or one that omits norms");
                    }
                }
                boost = value;
            }
        }

        public override Number NumericValue()
        {
            if (fieldsData is Number)
            {
                return (Number)fieldsData;
            }
            else
            {
                return null;
            }
        }

        public override BytesRef BinaryValue()
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

        public override FieldType FieldType()
        {
            return type;
        }

        public override TokenStream TokenStream(Analyzer analyzer)
        {
            if (!FieldType().Indexed())
            {
                return null;
            }

            FieldType.NumericType? numericType = FieldType().GetNumericType;
            if (numericType != null)
            {
                if (!(internalTokenStream is NumericTokenStream))
                {
                    // lazy init the TokenStream as it is heavy to instantiate
                    // (attributes,...) if not needed (stored field loading)
                    internalTokenStream = new NumericTokenStream(type.NumericPrecisionStep());
                }
                NumericTokenStream nts = (NumericTokenStream)internalTokenStream;
                // initialize value in TokenStream
                Number val = (Number)fieldsData;
                switch (numericType)
                {
                    case Net.Document.FieldType.NumericType.INT:
                        nts.SetIntValue(Convert.ToInt32(val));
                        break;
                    case Net.Document.FieldType.NumericType.LONG:
                        nts.SetLongValue(Convert.ToInt64(val));
                        break;
                    case Net.Document.FieldType.NumericType.FLOAT:
                        nts.SetFloatValue(Convert.ToSingle(val));
                        break;
                    case Net.Document.FieldType.NumericType.DOUBLE:
                        nts.SetDoubleValue(Convert.ToDouble(val));
                        break;
                    default:
                        throw new Exception("Should never get here");
                }
                return internalTokenStream;
            }

            if (!FieldType().Tokenized())
            {
                if (StringValue() == null)
                {
                    throw new ArgumentException("Non-Tokenized Fields must have a String value");
                }
                if (!(internalTokenStream is StringTokenStream))
                {
                    // lazy init the TokenStream as it is heavy to instantiate
                    // (attributes,...) if not needed (stored field loading)
                    internalTokenStream = new StringTokenStream();
                }
                ((StringTokenStream)internalTokenStream).SetValue(StringValue());
                return internalTokenStream;
            }

            if (tokenStream != null)
            {
                return tokenStream;
            }
            else if (ReaderValue() != null)
            {
                return analyzer.TokenStream(Name, ReaderValue());
            }
            else if (StringValue() != null)
            {
                if (internalReader == null)
                {
                    internalReader = new ReusableStringReader();
                }
                internalReader.SetValue(StringValue());
                return analyzer.TokenStream(Name, internalReader);
            }

            throw new ArgumentException("Field must have either TokenStream, String, Reader or Number value");
        }

        sealed class ReusableStringReader : TextReader
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

            public override void Dispose()
            {
                pos = size; // this prevents NPE when reading after close!
                s = null;
            }
        }


        sealed class StringTokenStream : TokenStream
        {
            private readonly CharTermAttribute termAttribute = AddAttribute<CharTermAttribute>;
            private readonly OffsetAttribute offsetAttribute = AddAttribute<OffsetAttribute>;
            private bool used = false;
            private String value = null;

            /** Creates a new TokenStream that returns a String as single token.
             * <p>Warning: Does not initialize the value, you must call
             * {@link #setValue(String)} afterwards!
             */


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

        
        public enum Store 
        {
            YES,
            NO
        }
    }
}