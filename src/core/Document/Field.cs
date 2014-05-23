using System;
using System.Text;

namespace Lucene.Net.Document
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


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using NumericTokenStream = Lucene.Net.Analysis.NumericTokenStream;
	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using NumericType = Lucene.Net.Document.FieldType.NumericType;
	using IndexWriter = Lucene.Net.Index.IndexWriter; // javadocs
	using IndexableField = Lucene.Net.Index.IndexableField;
	using IndexableFieldType = Lucene.Net.Index.IndexableFieldType;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using Lucene.Net.Support; // javadocs
    using System;
    using System.IO;
    using System.Text;
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Tokenattributes;
    using Lucene.Net.Index;
    using Lucene.Net.Util;
    using Lucene.Net.Support;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

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
	/// (String, Reader or pre-analyzed TokenStream), binary
	/// (byte[]), or numeric (a Number).  Fields are optionally stored in the
	/// index, so that they may be returned with hits on the document.
	/// 
	/// <p/>
	/// NOTE: the field type is an <seealso cref="IndexableFieldType"/>.  Making changes
	/// to the state of the IndexableFieldType will impact any
	/// Field it is used in.  It is strongly recommended that no
	/// changes be made after Field instantiation.
	/// </summary>
	public class Field : IndexableField
	{

	  /// <summary>
	  /// Field's type
	  /// </summary>
	  protected internal readonly FieldType Type;
	  /// <summary>
	  /// Field's name
	  /// </summary>
	  protected internal readonly string Name_Renamed;

	  /// <summary>
	  /// Field's value </summary>
	  protected internal object FieldsData;

	  /// <summary>
	  /// Pre-analyzed tokenStream for indexed fields; this is
	  /// separate from fieldsData because you are allowed to
	  /// have both; eg maybe field has a String value but you
	  /// customize how it's tokenized 
	  /// </summary>
	  protected internal TokenStream TokenStream_Renamed;

	  [NonSerialized]
	  private TokenStream InternalTokenStream;

	  /// <summary>
	  /// Field's boost </summary>
	  /// <seealso cref= #boost() </seealso>
	  protected internal float Boost_Renamed = 1.0f;

	  /// <summary>
	  /// Expert: creates a field with no initial value.
	  /// Intended only for custom Field subclasses. </summary>
	  /// <param name="name"> field name </param>
	  /// <param name="type"> field type </param>
	  /// <exception cref="IllegalArgumentException"> if either the name or type
	  ///         is null. </exception>
	  protected internal Field(string name, FieldType type)
	  {
		if (name == null)
		{
		  throw new System.ArgumentException("name cannot be null");
		}
		this.Name_Renamed = name;
		if (type == null)
		{
		  throw new System.ArgumentException("type cannot be null");
		}
		this.Type = type;
	  }

	  /// <summary>
	  /// Create field with Reader value. </summary>
	  /// <param name="name"> field name </param>
	  /// <param name="reader"> reader value </param>
	  /// <param name="type"> field type </param>
	  /// <exception cref="IllegalArgumentException"> if either the name or type
	  ///         is null, or if the field's type is stored(), or
	  ///         if tokenized() is false. </exception>
	  /// <exception cref="NullPointerException"> if the reader is null </exception>
	  public Field(string name, TextReader reader, FieldType type)
	  {
		if (name == null)
		{
		  throw new System.ArgumentException("name cannot be null");
		}
		if (type == null)
		{
		  throw new System.ArgumentException("type cannot be null");
		}
		if (reader == null)
		{
		  throw new System.NullReferenceException("reader cannot be null");
		}
		if (type.Stored)
		{
		  throw new System.ArgumentException("fields with a Reader value cannot be stored");
		}
		if (type.Indexed && !type.Tokenized)
		{
		  throw new System.ArgumentException("non-tokenized fields must use String values");
		}

		this.Name_Renamed = name;
		this.FieldsData = reader;
		this.Type = type;
	  }

	  /// <summary>
	  /// Create field with TokenStream value. </summary>
	  /// <param name="name"> field name </param>
	  /// <param name="tokenStream"> TokenStream value </param>
	  /// <param name="type"> field type </param>
	  /// <exception cref="IllegalArgumentException"> if either the name or type
	  ///         is null, or if the field's type is stored(), or
	  ///         if tokenized() is false, or if indexed() is false. </exception>
	  /// <exception cref="NullPointerException"> if the tokenStream is null </exception>
	  public Field(string name, TokenStream tokenStream, FieldType type)
	  {
		if (name == null)
		{
		  throw new System.ArgumentException("name cannot be null");
		}
		if (tokenStream == null)
		{
		  throw new System.NullReferenceException("tokenStream cannot be null");
		}
		if (!type.Indexed || !type.Tokenized)
		{
		  throw new System.ArgumentException("TokenStream fields must be indexed and tokenized");
		}
		if (type.Stored)
		{
		  throw new System.ArgumentException("TokenStream fields cannot be stored");
		}

		this.Name_Renamed = name;
		this.FieldsData = null;
		this.TokenStream_Renamed = tokenStream;
		this.Type = type;
	  }

	  /// <summary>
	  /// Create field with binary value.
	  /// 
	  /// <p>NOTE: the provided byte[] is not copied so be sure
	  /// not to change it until you're done with this field. </summary>
	  /// <param name="name"> field name </param>
	  /// <param name="value"> byte array pointing to binary content (not copied) </param>
	  /// <param name="type"> field type </param>
	  /// <exception cref="IllegalArgumentException"> if the field name is null,
	  ///         or the field's type is indexed() </exception>
	  /// <exception cref="NullPointerException"> if the type is null </exception>
	  public Field(string name, sbyte[] value, FieldType type) : this(name, value, 0, value.Length, type)
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
	  /// <exception cref="IllegalArgumentException"> if the field name is null,
	  ///         or the field's type is indexed() </exception>
	  /// <exception cref="NullPointerException"> if the type is null </exception>
	  public Field(string name, sbyte[] value, int offset, int length, FieldType type) : this(name, new BytesRef(value, offset, length), type)
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
	  /// <exception cref="IllegalArgumentException"> if the field name is null,
	  ///         or the field's type is indexed() </exception>
	  /// <exception cref="NullPointerException"> if the type is null </exception>
	  public Field(string name, BytesRef bytes, FieldType type)
	  {
		if (name == null)
		{
		  throw new System.ArgumentException("name cannot be null");
		}
		if (type.Indexed)
		{
		  throw new System.ArgumentException("Fields with BytesRef values cannot be indexed");
		}
		this.FieldsData = bytes;
		this.Type = type;
		this.Name_Renamed = name;
	  }

	  // TODO: allow direct construction of int, long, float, double value too..?

	  /// <summary>
	  /// Create field with String value. </summary>
	  /// <param name="name"> field name </param>
	  /// <param name="value"> string value </param>
	  /// <param name="type"> field type </param>
	  /// <exception cref="IllegalArgumentException"> if either the name or value
	  ///         is null, or if the field's type is neither indexed() nor stored(), 
	  ///         or if indexed() is false but storeTermVectors() is true. </exception>
	  /// <exception cref="NullPointerException"> if the type is null </exception>
	  public Field(string name, string value, FieldType type)
	  {
		if (name == null)
		{
		  throw new System.ArgumentException("name cannot be null");
		}
		if (value == null)
		{
		  throw new System.ArgumentException("value cannot be null");
		}
		if (!type.Stored && !type.Indexed)
		{
		  throw new System.ArgumentException("it doesn't make sense to have a field that " + "is neither indexed nor stored");
		}
		if (!type.Indexed && (type.StoreTermVectors))
		{
		  throw new System.ArgumentException("cannot store term vector information " + "for a field that is not indexed");
		}

		this.Type = type;
		this.Name_Renamed = name;
		this.FieldsData = value;
	  }

	  /// <summary>
	  /// The TokenStream for this field to be used when indexing, or null. If null,
	  /// the Reader value or String value is analyzed to produce the indexed tokens.
	  /// </summary>
	  public virtual TokenStream TokenStreamValue()
	  {
		return TokenStream_Renamed;
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
	  public string StringValue
	  {
		 get
            {
                if (FieldsData is string || FieldsData is Number)
                {
                    return FieldsData.ToString();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (!(FieldsData is String))
                {
                    throw new ArgumentException("cannot change value type from " + FieldsData.GetType().Name + " to String");
                }
                FieldsData = value;
            }
	  }

	  /// <summary>
	  /// Expert: change the value of this field. See 
	  /// <seealso cref="#setStringValue(String)"/>.
	  /// </summary>
	  public TextReader ReaderValue
	  {
		  get
            {
                return FieldsData is TextReader ? (TextReader)FieldsData : null;
            }

        set
        {
            if (!(FieldsData is TextReader))
            {
                throw new ArgumentException("cannot change value type from " + FieldsData.GetType().Name + " to Reader");
            }
            FieldsData = value;
        }
	  }

	  /// <summary>
	  /// Expert: change the value of this field. See 
	  /// <seealso cref="#setStringValue(String)"/>.
	  /// </summary>
	  public virtual sbyte[] BytesValue
	  {
		  set
		  {
			BytesRef temp = (new BytesRef(value));
            if (!(FieldsData is BytesRef))
            {
                throw new ArgumentException("cannot change value type from " + FieldsData.GetType().Name + " to BytesRef");
            }

            if (Type.Indexed)
            {
                throw new ArgumentException("cannot set a BytesRef value on an indexed field");
            }
            FieldsData = temp;
		  }
	  }

	  /// <summary>
	  /// Expert: change the value of this field. See 
	  /// <seealso cref="#setStringValue(String)"/>.
	  /// 
	  /// <p>NOTE: the provided BytesRef is not copied so be sure
	  /// not to change it until you're done with this field.
	  /// </summary>
	  public virtual BytesRef BytesValue
	  {
		  set
		  {
			if (!(FieldsData is BytesRef))
			{
			  throw new System.ArgumentException("cannot change value type from " + FieldsData.GetType().Name + " to BytesRef");
			}
			if (Type.Indexed)
			{
			  throw new System.ArgumentException("cannot set a BytesRef value on an indexed field");
			}
			FieldsData = value;
		  }
	  }

	  /// <summary>
	  /// Expert: change the value of this field. See 
	  /// <seealso cref="#setStringValue(String)"/>.
	  /// </summary>
	  public virtual sbyte ByteValue
	  {
		  set
		  {
			if (!(FieldsData is sbyte?))
			{
			  throw new System.ArgumentException("cannot change value type from " + FieldsData.GetType().Name + " to Byte");
			}
			FieldsData = Convert.ToByte(value);
		  }
	  }

	  /// <summary>
	  /// Expert: change the value of this field. See 
	  /// <seealso cref="#setStringValue(String)"/>.
	  /// </summary>
	  public virtual short ShortValue
	  {
		  set
		  {
			if (!(FieldsData is short?))
			{
			  throw new System.ArgumentException("cannot change value type from " + FieldsData.GetType().Name + " to Short");
			}
			FieldsData = Convert.ToInt16(value);
		  }
	  }

	  /// <summary>
	  /// Expert: change the value of this field. See 
	  /// <seealso cref="#setStringValue(String)"/>.
	  /// </summary>
	  public virtual int IntValue
	  {
		  set
		  {
			if (!(FieldsData is int?))
			{
			  throw new System.ArgumentException("cannot change value type from " + FieldsData.GetType().Name + " to Integer");
			}
			FieldsData = Convert.ToInt32(value);
		  }
	  }

	  /// <summary>
	  /// Expert: change the value of this field. See 
	  /// <seealso cref="#setStringValue(String)"/>.
	  /// </summary>
	  public virtual long LongValue
	  {
		  set
		  {
			if (!(FieldsData is long?))
			{
			  throw new System.ArgumentException("cannot change value type from " + FieldsData.GetType().Name + " to Long");
			}
			FieldsData = Convert.ToInt64(value);
		  }
	  }

	  /// <summary>
	  /// Expert: change the value of this field. See 
	  /// <seealso cref="#setStringValue(String)"/>.
	  /// </summary>
	  public virtual float FloatValue
	  {
		  set
		  {
			if (!(FieldsData is float?))
			{
			  throw new System.ArgumentException("cannot change value type from " + FieldsData.GetType().Name + " to Float");
			}
			FieldsData = Convert.ToSingle(value);
		  }
	  }

	  /// <summary>
	  /// Expert: change the value of this field. See 
	  /// <seealso cref="#setStringValue(String)"/>.
	  /// </summary>
	  public virtual double DoubleValue
	  {
		  set
		  {
			if (!(FieldsData is double?))
			{
			  throw new System.ArgumentException("cannot change value type from " + FieldsData.GetType().Name + " to Double");
			}
			FieldsData = Convert.ToDouble(value);
		  }
	  }

	  /// <summary>
	  /// Expert: sets the token stream to be used for indexing and causes
	  /// isIndexed() and isTokenized() to return true. May be combined with stored
	  /// values from stringValue() or getBinaryValue()
	  /// </summary>
	  public virtual TokenStream TokenStream
	  {
		  set
		  {
			if (!Type.Indexed || !Type.Tokenized)
			{
			  throw new System.ArgumentException("TokenStream fields must be indexed and tokenized");
			}
			if (Type.NumericTypeValue != null)
			{
			  throw new System.ArgumentException("cannot set private TokenStream on numeric fields");
			}
			this.TokenStream_Renamed = value;
		  }
	  }

	  public override string Name()
	  {
		return Name_Renamed;
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// The default value is <code>1.0f</code> (no boost). </summary>
	  /// <seealso cref= #setBoost(float) </seealso>
	  public override float Boost()
	  {
		return Boost_Renamed;
	  }

	  /// <summary>
	  /// Sets the boost factor on this field. </summary>
	  /// <exception cref="IllegalArgumentException"> if this field is not indexed, 
	  ///         or if it omits norms. </exception>
	  /// <seealso cref= #boost() </seealso>
	  public virtual float Boost
	  {
		  set
		  {
			if (value != 1.0f)
			{
			  if (Type.Indexed == false || Type.OmitNorms)
			  {
				throw new System.ArgumentException("You cannot set an index-time boost on an unindexed field, or one that omits norms");
			  }
			}
			this.Boost_Renamed = value;
		  }
	  }

	  public override Number NumericValue()
	  {
		if (FieldsData is Number)
		{
		  return (Number) FieldsData;
		}
		else
		{
		  return null;
		}
	  }

	  public override BytesRef BinaryValue()
	  {
		if (FieldsData is BytesRef)
		{
		  return (BytesRef) FieldsData;
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
		result.Append(Type.ToString());
		result.Append('<');
		result.Append(Name_Renamed);
		result.Append(':');

		if (FieldsData != null)
		{
		  result.Append(FieldsData);
		}

		result.Append('>');
		return result.ToString();
	  }

	  /// <summary>
	  /// Returns the <seealso cref="FieldType"/> for this field. </summary>
	  public override FieldType FieldType()
	  {
		return Type;
	  }

	  public override TokenStream TokenStream(Analyzer analyzer)
	  {
		if (!FieldType().Indexed)
		{
		  return null;
		}

		NumericType? numericType = FieldType().NumericTypeValue;
		if (numericType != null)
		{
		  if (!(InternalTokenStream is NumericTokenStream))
		  {
			// lazy init the TokenStream as it is heavy to instantiate
			// (attributes,...) if not needed (stored field loading)
			InternalTokenStream = new NumericTokenStream(Type.NumericPrecisionStep());
		  }
		  NumericTokenStream nts = (NumericTokenStream) InternalTokenStream;
		  // initialize value in TokenStream
		  Number val = (Number) FieldsData;
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
		  return InternalTokenStream;
		}

		if (!FieldType().Tokenized)
		{
		  if (StringValue == null)
		  {
			throw new System.ArgumentException("Non-Tokenized Fields must have a String value");
		  }
		  if (!(InternalTokenStream is StringTokenStream))
		  {
			// lazy init the TokenStream as it is heavy to instantiate
			// (attributes,...) if not needed (stored field loading)
			InternalTokenStream = new StringTokenStream();
		  }
		  ((StringTokenStream) InternalTokenStream).Value = StringValue;
		  return InternalTokenStream;
		}

		if (TokenStream_Renamed != null)
		{
		  return TokenStream_Renamed;
		}
		else if (ReaderValue != null)
		{
		  return analyzer.TokenStream(Name(), ReaderValue);
		}
		else if (StringValue != null)
		{
            using(TextReader sr = new StringReader(StringValue))
            {
                return analyzer.TokenStream(Name(), sr);
            }
		}

		throw new System.ArgumentException("Field must have either TokenStream, String, Reader or Number value; got " + this);
	  }

	  internal sealed class StringTokenStream : TokenStream
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal void InitializeInstanceFields()
		  {
			  TermAttribute = AddAttribute<CharTermAttribute>();
			  OffsetAttribute = AddAttribute<OffsetAttribute>();
		  }

		internal CharTermAttribute TermAttribute;
		internal OffsetAttribute OffsetAttribute;
		internal bool Used = false;
		internal string Value_Renamed = null;

		/// <summary>
		/// Creates a new TokenStream that returns a String as single token.
		/// <p>Warning: Does not initialize the value, you must call
		/// <seealso cref="#setValue(String)"/> afterwards!
		/// </summary>
		internal StringTokenStream()
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		}

		/// <summary>
		/// Sets the string value. </summary>
		internal string Value
		{
			set
			{
			  this.Value_Renamed = value;
			}
		}

		public override bool IncrementToken()
		{
		  if (Used)
		  {
			return false;
		  }
		  ClearAttributes();
		  TermAttribute.Append(Value_Renamed);
		  OffsetAttribute.SetOffset(0, Value_Renamed.Length);
		  Used = true;
		  return true;
		}

		public override void End()
		{
		  base.End();
		  int finalOffset = Value_Renamed.Length;
		  OffsetAttribute.SetOffset(finalOffset, finalOffset);
		}

		public override void Reset()
		{
		  Used = false;
		}

		public override void Close()
		{
		  Value_Renamed = null;
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
	  /// </summary>
	  ///  @deprecated this is here only to ease transition from
	  ///  the pre-4.0 APIs.  


	}

}