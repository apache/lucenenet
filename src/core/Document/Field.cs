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
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Support;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using StringHelper = Lucene.Net.Util.StringHelper;

namespace Lucene.Net.Documents
{
    

    public class Field : IndexableField, StorableField 
    {

  /**
   * Field's type
   */
  protected readonly FieldType type;

  /**
   * Field's name
   */
  protected static string name;

  /** Field's value */
  protected Object fieldsData;

  /** Pre-analyzed tokenStream for indexed fields; this is
   * separate from fieldsData because you are allowed to
   * have both; eg maybe field has a String value but you
   * customize how it's tokenized */
  protected TokenStream tokenStream;
    
  [NonSerialized]
  private TokenStream internalTokenStream;
        [NonSerialized]
  private ReusableStringReader internalReader;

  /**
   * Field's boost
   * @see #boost()
   */
  protected float boost = 1.0f;

  /**
   * Expert: creates a field with no initial value.
   * Intended only for custom Field subclasses.
   * @param name field name
   * @param type field type
   * @throws IllegalArgumentException if either the name or type
   *         is null.
   */
  protected Field(String name, FieldType type) {
    if (name == null) {
      throw new ArgumentException( "name cannot be null");
    }
    this.name = name;
    if (type == null) {
      throw new ArgumentException( "type cannot be null");
    }
    this.type = type;
  }

  /**
   * Create field with Reader value.
   * @param name field name
   * @param reader reader value
   * @param type field type
   * @throws IllegalArgumentException if either the name or type
   *         is null, or if the field's type is stored(), or
   *         if tokenized() is false.
   * @throws NullPointerException if the reader is null
   */
  public Field(String name, PagedBytes.Reader reader, FieldType type) {
    if (name == null) {
      throw new ArgumentException( "name cannot be null");
    }
    if (type == null) {
      throw new ArgumentException( "type cannot be null");
    }
    if (reader == null) {
      throw new NullReferenceException( "reader cannot be null");
    }
    if (type.stored()) {
      throw new ArgumentException( "fields with a Reader value cannot be stored");
    }
    if (type.indexed() && !type.tokenized()) {
      throw new ArgumentException( "non-tokenized fields must use String values");
    }
    
    this.name = name;
    this.fieldsData = reader;
    this.type = type;
  }

  /**
   * Create field with TokenStream value.
   * @param name field name
   * @param tokenStream TokenStream value
   * @param type field type
   * @throws IllegalArgumentException if either the name or type
   *         is null, or if the field's type is stored(), or
   *         if tokenized() is false, or if indexed() is false.
   * @throws NullPointerException if the tokenStream is null
   */
  public Field(String name, TokenStream tokenStream, FieldType type) {
    if (name == null) {
      throw new ArgumentException( "name cannot be null");
    }
    if (tokenStream == null) {
      throw new ArgumentException( "tokenStream cannot be null");
    }
    if (!type.indexed() || !type.tokenized()) {
      throw new ArgumentException( "TokenStream fields must be indexed and tokenized");
    }
    if (type.stored()) {
      throw new ArgumentException( "TokenStream fields cannot be stored");
    }
    
    this.name = name;
    this.fieldsData = null;
    this.tokenStream = tokenStream;
    this.type = type;
  }
  
  /**
   * Create field with binary value.
   * 
   * <p>NOTE: the provided byte[] is not copied so be sure
   * not to change it until you're done with this field.
   * @param name field name
   * @param value byte array pointing to binary content (not copied)
   * @param type field type
   * @throws IllegalArgumentException if the field name is null,
   *         or the field's type is indexed()
   * @throws NullPointerException if the type is null
   */
  public Field(String name, byte[] value, FieldType type) {
    this(name, value, 0, value.Length, type);
  }

  /**
   * Create field with binary value.
   * 
   * <p>NOTE: the provided byte[] is not copied so be sure
   * not to change it until you're done with this field.
   * @param name field name
   * @param value byte array pointing to binary content (not copied)
   * @param offset starting position of the byte array
   * @param length valid length of the byte array
   * @param type field type
   * @throws IllegalArgumentException if the field name is null,
   *         or the field's type is indexed()
   * @throws NullPointerException if the type is null
   */
  public Field(String name, sbyte[] value, int offset, int length, FieldType type) {
    this(name, new BytesRef(value, offset, length), type);
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
  public Field(String name, BytesRef bytes, FieldType type) {
    if (name == null) {
      throw new ArgumentException( "name cannot be null");
    }
    if (type.indexed()) {
      throw new ArgumentException( "Fields with BytesRef values cannot be indexed");
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
  public Field(String name, String value, FieldType type) {
    if (name == null) {
      throw new ArgumentException( "name cannot be null");
    }
    if (value == null) {
      throw new ArgumentException( "value cannot be null");
    }
    if (!type.stored() && !type.indexed()) {
      throw new ArgumentException( "it doesn't make sense to have a field that "
        + "is neither indexed nor stored");
    }
    if (!type.indexed() && (type.storeTermVectors())) {
      throw new ArgumentException( "cannot store term vector information "
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
  
  override public String stringValue() {
    if (fieldsData is string || fieldsData is Number) {
      return fieldsData.ToString();
    } else {
      return null;
    }
  }
  
  /**
   * The value of the field as a Reader, or null. If null, the String value or
   * binary value is used. Exactly one of stringValue(), readerValue(), and
   * getBinaryValue() must be set.
   */
  
  override public PagedBytes.Reader readerValue() {
    return fieldsData is PagedBytes.Reader ? (PagedBytes.Reader) fieldsData : null;
  }
  
  /**
   * The TokenStream for this field to be used when indexing, or null. If null,
   * the Reader value or String value is analyzed to produce the indexed tokens.
   */
  public TokenStream tokenStreamValue() {
    return tokenStream;
  }
  
  /**
   * <p>
   * Expert: change the value of this field. This can be used during indexing to
   * re-use a single Field instance to improve indexing speed by avoiding GC
   * cost of new'ing and reclaiming Field instances. Typically a single
   * {@link Document} instance is re-used as well. This helps most on small
   * documents.
   * </p>
   * 
   * <p>
   * Each Field instance should only be used once within a single
   * {@link Document} instance. See <a
   * href="http://wiki.apache.org/lucene-java/ImproveIndexingSpeed"
   * >ImproveIndexingSpeed</a> for details.
   * </p>
   */
  public void SetStringValue(String value) {
    if (!(fieldsData is String)) {
      throw new ArgumentException( "cannot change value type from " + fieldsData.getClass().getSimpleName() + " to String");
    }
    fieldsData = value;
  }
  
  /**
   * Expert: change the value of this field. See 
   * {@link #setStringValue(String)}.
   */
  public void SetReaderValue(PagedBytes.Reader value) {
    if (!(fieldsData is PagedBytes.Reader)) {
      throw new ArgumentException( "cannot change value type from " + fieldsData.getClass().getSimpleName() + " to Reader");
    }
    fieldsData = value;
  }
  
  /**
   * Expert: change the value of this field. See 
   * {@link #setStringValue(String)}.
   */
  public void SetBytesValue(byte[] value) {
    SetBytesValue(new BytesRef(value));
  }

  /**
   * Expert: change the value of this field. See 
   * {@link #setStringValue(String)}.
   *
   * <p>NOTE: the provided BytesRef is not copied so be sure
   * not to change it until you're done with this field.
   */
  virtual public void SetBytesValue(BytesRef value) {
    if (!(fieldsData is BytesRef)) {
      throw new ArgumentException( "cannot change value type from " + fieldsData.getClass().getSimpleName() + " to BytesRef");
    }
    if (type.indexed()) {
      throw new ArgumentException( "cannot set a BytesRef value on an indexed field");
    }
    fieldsData = value;
  }

  /**
   * Expert: change the value of this field. See 
   * {@link #setStringValue(String)}.
   */
  virtual public void SetByteValue(byte value) {
    if (!(fieldsData is Byte)) {
      throw new ArgumentException( "cannot change value type from " + fieldsData.getClass().getSimpleName() + " to Byte");
    }
    fieldsData = Convert.ToByte(value);
  }

  /**
   * Expert: change the value of this field. See 
   * {@link #setStringValue(String)}.
   */
  public void SetShortValue(short value) {
    if (!(fieldsData is short)) {
      throw new ArgumentException( "cannot change value type from " + fieldsData.getClass().getSimpleName() + " to Short");
    }
    fieldsData = Convert.ToInt16(value);
  }

  /**
   * Expert: change the value of this field. See 
   * {@link #setStringValue(String)}.
   */
  public void SetIntValue(int value) {
    if (!(fieldsData is int)) {
      throw new ArgumentException( "cannot change value type from " + fieldsData.getClass().getSimpleName() + " to Integer");
    }
    fieldsData = Convert.ToInt32(value);
  }

  /**
   * Expert: change the value of this field. See 
   * {@link #setStringValue(String)}.
   */
  public void SetLongValue(long value) {
    if (!(fieldsData is long)) {
      throw new ArgumentException( "cannot change value type from " + fieldsData.getClass().getSimpleName() + " to Long");
    }
    fieldsData = Convert.ToInt64(value);
  }

  /**
   * Expert: change the value of this field. See 
   * {@link #setStringValue(String)}.
   */
  public void SetFloatValue(float value) {
    if (!(fieldsData is float)) {
      throw new ArgumentException( "cannot change value type from " + fieldsData.getClass().getSimpleName() + " to Float");
    }
    fieldsData = Convert.ToSingle(value);
  }

  /**
   * Expert: change the value of this field. See 
   * {@link #setStringValue(String)}.
   */
  public void SetDoubleValue(double value) {
    if (!(fieldsData is double)) {
      throw new ArgumentException("cannot change value type from " + fieldsData.getClass().getSimpleName() + " to Double");
    }
    fieldsData = Convert.ToDouble(value);
  }

  /**
   * Expert: sets the token stream to be used for indexing and causes
   * isIndexed() and isTokenized() to return true. May be combined with stored
   * values from stringValue() or getBinaryValue()
   */
  public void SetTokenStream(TokenStream tokenStream) {
    if (!type.indexed() || !type.tokenized()) {
      throw new ArgumentException("TokenStream fields must be indexed and tokenized");
    }
    if (type.numericType() != null) {
      throw new ArgumentException("cannot set private TokenStream on numeric fields");
    }
    this.tokenStream = tokenStream;
  }
  
  
  override public String name() {
    return name;
  }
  
  /** 
   * {@inheritDoc}
   * <p>
   * The default value is <code>1.0f</code> (no boost).
   * @see #setBoost(float)
   */
  
  override public float boost() {
    return boost;
  }

  /** 
   * Sets the boost factor on this field.
   * @throws IllegalArgumentException if this field is not indexed, 
   *         or if it omits norms. 
   * @see #boost()
   */
  public void SetBoost(float boost) {
    if (boost != 1.0f) {
      if (type.indexed() == false || type.omitNorms()) {
        throw new ArgumentException("You cannot set an index-time boost on an unindexed field, or one that omits norms");
      }
    }
    this.boost = boost;
  }

  
  override public Number numericValue() {
    if (fieldsData is Number ) {
      return (Number) fieldsData;
    } else {
      return null;
    }
  }

  
  override public BytesRef binaryValue() {
    if (fieldsData is BytesRef) {
      return (BytesRef) fieldsData;
    } else {
      return null;
    }
  }
  
  /** Prints a Field for human consumption. */
 
  override public String ToString() {
    StringBuilder result = new StringBuilder();
    result.Append(type.ToString());
    result.Append('<');
    result.Append(name);
    result.Append(':');

    if (fieldsData != null) {
      result.Append(fieldsData);
    }

    result.Append('>');
    return result.ToString();
  }
  
  /** Returns the {@link FieldType} for this field. */
  
  override public FieldType fieldType() {
    return type;
  }

  
  override public TokenStream TokenStream(Analyzer analyzer)  {
    if (!fieldType().indexed()) {
      return null;
    }

    NumericType numericType = fieldType().numericType();
    if (numericType != null) {
      if (!(internalTokenStream is NumericTokenStream)) {
        // lazy init the TokenStream as it is heavy to instantiate
        // (attributes,...) if not needed (stored field loading)
        internalTokenStream = new NumericTokenStream(type.numericPrecisionStep());
      }
      NumericTokenStream nts = (NumericTokenStream) internalTokenStream;
      // initialize value in TokenStream
      Number val = (Number) fieldsData;
      switch (numericType) {
      case INT:
        nts.SetIntValue(val.intValue());
        break;
      case LONG:
        nts.SetLongValue(val.longValue());
        break;
      case FLOAT:
        nts.SetFloatValue(val.floatValue());
        break;
      case DOUBLE:
        nts.SetDoubleValue(val.doubleValue());
        break;
      default:
        throw new Exception("Should never get here");
      }
      return internalTokenStream;
    }

    if (!fieldType().tokenized()) {
      if (stringValue() == null) {
        throw new ArgumentException("Non-Tokenized Fields must have a String value");
      }
      if (!(internalTokenStream is StringTokenStream)) {
        // lazy init the TokenStream as it is heavy to instantiate
        // (attributes,...) if not needed (stored field loading)
        internalTokenStream = new StringTokenStream();
      }
      ((StringTokenStream) internalTokenStream).SetValue(stringValue());
      return internalTokenStream;
    }

    if (tokenStream != null) {
      return tokenStream;
    } else if (readerValue() != null) {
      return analyzer.TokenStream(name(), readerValue());
    } else if (stringValue() != null) {
      if (internalReader == null) {
        internalReader = new ReusableStringReader();
      }
      internalReader.SetValue(stringValue());
      return analyzer.TokenStream(name(), internalReader);
    }

    throw new ArgumentException("Field must have either TokenStream, String, Reader or Number value");
  }
  
  static sealed class ReusableStringReader : PagedBytes.Reader {
    private int pos = 0, size = 0;
    private String s = null;
    
    void SetValue(String s) {
      this.s = s;
      this.size = s.Length();
      this.pos = 0;
    }
    
    
    override public int Read() {
      if (pos < size) {
        return s[pos++];
      } else {
        s = null;
        return -1;
      }
    }
    
    
    override public int Read(char[] c, int off, int len) {
      if (pos < size) {
        len = Math.Min(len, size-pos);
        s.ToCharArray(pos, pos+len, c, off);
        pos += len;
        return len;
      } else {
        s = null;
        return -1;
      }
    }
    
    
    override public void Close() {
      pos = size; // this prevents NPE when reading after close!
      s = null;
    }
  }
  
  static class StringTokenStream : TokenStream {
    private readonly CharTermAttribute termAttribute = AddAttribute(CharTermAttribute.class);
    private readonly OffsetAttribute offsetAttribute = AddAttribute(OffsetAttribute.class);
    private bool used = false;
    private String value = null;
    
    /** Creates a new TokenStream that returns a String as single token.
     * <p>Warning: Does not initialize the value, you must call
     * {@link #setValue(String)} afterwards!
     */

    
    /** Sets the string value. */
    void SetValue(String value) {
      this.value = value;
    }

    
    override public bool IncrementToken() {
      if (used) {
        return false;
      }
      ClearAttributes();
      termAttribute.Append(value);
      offsetAttribute.SetOffset(0, value.Length);
      used = true;
      return true;
    }

    
    override public void End() {
      int finalOffset = value.Length();
      offsetAttribute.SetOffset(finalOffset, finalOffset);
    }
    
    
    override public void Reset() {
      used = false;
    }

    
    override public void Close() {
      value = null;
    }
  }

  /** Specifies whether and how a field should be stored. */
  public enum Store {

    /** Store the original field value in the index. This is useful for short texts
     * like a document's title which should be displayed with the results. The
     * value is stored in its original form, i.e. no analyzer is used before it is
     * stored.
     */
    YES,

    /** Do not store the field value in the index. */
    NO
  }
    }

}