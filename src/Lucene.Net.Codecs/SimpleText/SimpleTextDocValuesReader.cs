using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.SimpleText
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

////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesWriter.END;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesWriter.FIELD;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesWriter.LENGTH;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesWriter.MAXLENGTH;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesWriter.MINVALUE;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesWriter.NUMVALUES;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesWriter.ORDPATTERN;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesWriter.PATTERN;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesWriter.TYPE;


	using BinaryDocValues = Index.BinaryDocValues;
	using CorruptIndexException = Index.CorruptIndexException;
	using DocValues = Index.DocValues;
	using FieldInfo = Index.FieldInfo;
	using DocValuesType = Index.FieldInfo.DocValuesType;
	using IndexFileNames = Index.IndexFileNames;
	using NumericDocValues = Index.NumericDocValues;
	using SegmentReadState = Index.SegmentReadState;
	using SortedDocValues = Index.SortedDocValues;
	using SortedSetDocValues = Index.SortedSetDocValues;
	using BufferedChecksumIndexInput = Store.BufferedChecksumIndexInput;
	using ChecksumIndexInput = Store.ChecksumIndexInput;
	using IndexInput = Store.IndexInput;
	using Bits = Util.Bits;
	using BytesRef = Util.BytesRef;
	using StringHelper = Util.StringHelper;

	public class SimpleTextDocValuesReader : DocValuesProducer
	{

	  internal class OneField
	  {
		internal long dataStartFilePointer;
		internal string pattern;
		internal string ordPattern;
		internal int maxLength;
		internal bool fixedLength;
		internal long minValue;
		internal long numValues;
	  }

	  internal readonly int maxDoc;
	  internal readonly IndexInput data;
	  internal readonly BytesRef scratch = new BytesRef();
	  internal readonly IDictionary<string, OneField> fields = new Dictionary<string, OneField>();

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public SimpleTextDocValuesReader(index.SegmentReadState state, String ext) throws java.io.IOException
	  public SimpleTextDocValuesReader(SegmentReadState state, string ext)
	  {
		// System.out.println("dir=" + state.directory + " seg=" + state.segmentInfo.name + " file=" + IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, ext));
		data = state.directory.openInput(IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, ext), state.context);
		maxDoc = state.segmentInfo.DocCount;
		while (true)
		{
		  readLine();
		  //System.out.println("READ field=" + scratch.utf8ToString());
		  if (scratch.Equals(END))
		  {
			break;
		  }
		  Debug.Assert(startsWith(FIELD), scratch.utf8ToString());
		  string fieldName = stripPrefix(FIELD);
		  //System.out.println("  field=" + fieldName);

		  OneField field = new OneField();
		  fields[fieldName] = field;

		  readLine();
		  Debug.Assert(startsWith(TYPE), scratch.utf8ToString());

		  FieldInfo.DocValuesType dvType = FieldInfo.DocValuesType.valueOf(stripPrefix(TYPE));
		  Debug.Assert(dvType != null);
		  if (dvType == FieldInfo.DocValuesType.NUMERIC)
		  {
			readLine();
			Debug.Assert(startsWith(MINVALUE), "got " + scratch.utf8ToString() + " field=" + fieldName + " ext=" + ext);
			field.minValue = Convert.ToInt64(stripPrefix(MINVALUE));
			readLine();
			Debug.Assert(startsWith(PATTERN));
			field.pattern = stripPrefix(PATTERN);
			field.dataStartFilePointer = data.FilePointer;
			data.seek(data.FilePointer + (1 + field.pattern.Length + 2) * maxDoc);
		  }
		  else if (dvType == FieldInfo.DocValuesType.BINARY)
		  {
			readLine();
			Debug.Assert(startsWith(MAXLENGTH));
			field.maxLength = Convert.ToInt32(stripPrefix(MAXLENGTH));
			readLine();
			Debug.Assert(startsWith(PATTERN));
			field.pattern = stripPrefix(PATTERN);
			field.dataStartFilePointer = data.FilePointer;
			data.seek(data.FilePointer + (9 + field.pattern.Length + field.maxLength + 2) * maxDoc);
		  }
		  else if (dvType == FieldInfo.DocValuesType.SORTED || dvType == FieldInfo.DocValuesType.SORTED_SET)
		  {
			readLine();
			Debug.Assert(startsWith(NUMVALUES));
			field.numValues = Convert.ToInt64(stripPrefix(NUMVALUES));
			readLine();
			Debug.Assert(startsWith(MAXLENGTH));
			field.maxLength = Convert.ToInt32(stripPrefix(MAXLENGTH));
			readLine();
			Debug.Assert(startsWith(PATTERN));
			field.pattern = stripPrefix(PATTERN);
			readLine();
			Debug.Assert(startsWith(ORDPATTERN));
			field.ordPattern = stripPrefix(ORDPATTERN);
			field.dataStartFilePointer = data.FilePointer;
			data.seek(data.FilePointer + (9 + field.pattern.Length + field.maxLength) * field.numValues + (1 + field.ordPattern.Length) * maxDoc);
		  }
		  else
		  {
			throw new AssertionError();
		  }
		}

		// We should only be called from above if at least one
		// field has DVs:
		Debug.Assert(fields.Count > 0);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public index.NumericDocValues getNumeric(index.FieldInfo fieldInfo) throws java.io.IOException
	  public override NumericDocValues getNumeric(FieldInfo fieldInfo)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final OneField field = fields.get(fieldInfo.name);
		OneField field = fields[fieldInfo.name];
		Debug.Assert(field != null);

		// SegmentCoreReaders already verifies this field is
		// valid:
		Debug.Assert(field != null, "field=" + fieldInfo.name + " fields=" + fields);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final store.IndexInput in = data.clone();
		IndexInput @in = data.clone();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final util.BytesRef scratch = new util.BytesRef();
		BytesRef scratch = new BytesRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.text.DecimalFormat decoder = new java.text.DecimalFormat(field.pattern, new java.text.DecimalFormatSymbols(java.util.Locale.ROOT));
		DecimalFormat decoder = new DecimalFormat(field.pattern, new DecimalFormatSymbols(Locale.ROOT));

		decoder.ParseBigDecimal = true;

		return new NumericDocValuesAnonymousInnerClassHelper(this, field, @in, scratch, decoder);
	  }

	  private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
	  {
		  private readonly SimpleTextDocValuesReader outerInstance;

		  private Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field;
		  private IndexInput @in;
		  private BytesRef scratch;
		  private DecimalFormat decoder;

		  public NumericDocValuesAnonymousInnerClassHelper(SimpleTextDocValuesReader outerInstance, Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field, IndexInput @in, BytesRef scratch, DecimalFormat decoder)
		  {
			  this.outerInstance = outerInstance;
			  this.field = field;
			  this.@in = @in;
			  this.scratch = scratch;
			  this.decoder = decoder;
		  }

		  public override long get(int docID)
		  {
			try
			{
			  //System.out.println(Thread.currentThread().getName() + ": get docID=" + docID + " in=" + in);
			  if (docID < 0 || docID >= outerInstance.maxDoc)
			  {
				throw new System.IndexOutOfRangeException("docID must be 0 .. " + (outerInstance.maxDoc - 1) + "; got " + docID);
			  }
			  @in.seek(field.dataStartFilePointer + (1 + field.pattern.Length + 2) * docID);
			  SimpleTextUtil.ReadLine(@in, scratch);
			  //System.out.println("parsing delta: " + scratch.utf8ToString());
			  decimal bd;
			  try
			  {
				bd = (decimal) decoder.parse(scratch.utf8ToString());
			  }
			  catch (ParseException pe)
			  {
				CorruptIndexException e = new CorruptIndexException("failed to parse BigDecimal value (resource=" + @in + ")");
				e.initCause(pe);
				throw e;
			  }
			  SimpleTextUtil.ReadLine(@in, scratch); // read the line telling us if its real or not
			  return System.Numerics.BigInteger.valueOf(field.minValue) + (long)bd.toBigIntegerExact();
			}
			catch (IOException ioe)
			{
			  throw new Exception(ioe);
			}
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private util.Bits getNumericDocsWithField(index.FieldInfo fieldInfo) throws java.io.IOException
	  private Bits getNumericDocsWithField(FieldInfo fieldInfo)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final OneField field = fields.get(fieldInfo.name);
		OneField field = fields[fieldInfo.name];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final store.IndexInput in = data.clone();
		IndexInput @in = data.clone();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final util.BytesRef scratch = new util.BytesRef();
		BytesRef scratch = new BytesRef();
		return new BitsAnonymousInnerClassHelper(this, field, @in, scratch);
	  }

	  private class BitsAnonymousInnerClassHelper : Bits
	  {
		  private readonly SimpleTextDocValuesReader outerInstance;

		  private Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field;
		  private IndexInput @in;
		  private BytesRef scratch;

		  public BitsAnonymousInnerClassHelper(SimpleTextDocValuesReader outerInstance, Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field, IndexInput @in, BytesRef scratch)
		  {
			  this.outerInstance = outerInstance;
			  this.field = field;
			  this.@in = @in;
			  this.scratch = scratch;
		  }

		  public override bool get(int index)
		  {
			try
			{
			  @in.seek(field.dataStartFilePointer + (1 + field.pattern.Length + 2) * index);
			  SimpleTextUtil.ReadLine(@in, scratch); // data
			  SimpleTextUtil.ReadLine(@in, scratch); // 'T' or 'F'
			  return scratch.bytes[scratch.offset] == (sbyte) 'T';
			}
			catch (IOException e)
			{
			  throw new Exception(e);
			}
		  }

		  public override int length()
		  {
			return outerInstance.maxDoc;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public index.BinaryDocValues getBinary(index.FieldInfo fieldInfo) throws java.io.IOException
	  public override BinaryDocValues getBinary(FieldInfo fieldInfo)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final OneField field = fields.get(fieldInfo.name);
		OneField field = fields[fieldInfo.name];

		// SegmentCoreReaders already verifies this field is
		// valid:
		Debug.Assert(field != null);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final store.IndexInput in = data.clone();
		IndexInput @in = data.clone();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final util.BytesRef scratch = new util.BytesRef();
		BytesRef scratch = new BytesRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.text.DecimalFormat decoder = new java.text.DecimalFormat(field.pattern, new java.text.DecimalFormatSymbols(java.util.Locale.ROOT));
		DecimalFormat decoder = new DecimalFormat(field.pattern, new DecimalFormatSymbols(Locale.ROOT));

		return new BinaryDocValuesAnonymousInnerClassHelper(this, field, @in, scratch, decoder);
	  }

	  private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
	  {
		  private readonly SimpleTextDocValuesReader outerInstance;

		  private Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field;
		  private IndexInput @in;
		  private BytesRef scratch;
		  private DecimalFormat decoder;

		  public BinaryDocValuesAnonymousInnerClassHelper(SimpleTextDocValuesReader outerInstance, Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field, IndexInput @in, BytesRef scratch, DecimalFormat decoder)
		  {
			  this.outerInstance = outerInstance;
			  this.field = field;
			  this.@in = @in;
			  this.scratch = scratch;
			  this.decoder = decoder;
		  }

		  public override void get(int docID, BytesRef result)
		  {
			try
			{
			  if (docID < 0 || docID >= outerInstance.maxDoc)
			  {
				throw new System.IndexOutOfRangeException("docID must be 0 .. " + (outerInstance.maxDoc - 1) + "; got " + docID);
			  }
			  @in.seek(field.dataStartFilePointer + (9 + field.pattern.Length + field.maxLength + 2) * docID);
			  SimpleTextUtil.ReadLine(@in, scratch);
			  Debug.Assert(StringHelper.StartsWith(scratch, LENGTH));
			  int len;
			  try
			  {
				len = (int)decoder.parse(new string(scratch.bytes, scratch.offset + LENGTH.length, scratch.length - LENGTH.length, StandardCharsets.UTF_8));
			  }
			  catch (ParseException pe)
			  {
				CorruptIndexException e = new CorruptIndexException("failed to parse int length (resource=" + @in + ")");
				e.initCause(pe);
				throw e;
			  }
			  result.bytes = new sbyte[len];
			  result.offset = 0;
			  result.length = len;
			  @in.readBytes(result.bytes, 0, len);
			}
			catch (IOException ioe)
			{
			  throw new Exception(ioe);
			}
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private util.Bits getBinaryDocsWithField(index.FieldInfo fieldInfo) throws java.io.IOException
	  private Bits getBinaryDocsWithField(FieldInfo fieldInfo)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final OneField field = fields.get(fieldInfo.name);
		OneField field = fields[fieldInfo.name];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final store.IndexInput in = data.clone();
		IndexInput @in = data.clone();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final util.BytesRef scratch = new util.BytesRef();
		BytesRef scratch = new BytesRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.text.DecimalFormat decoder = new java.text.DecimalFormat(field.pattern, new java.text.DecimalFormatSymbols(java.util.Locale.ROOT));
		DecimalFormat decoder = new DecimalFormat(field.pattern, new DecimalFormatSymbols(Locale.ROOT));

		return new BitsAnonymousInnerClassHelper2(this, field, @in, scratch, decoder);
	  }

	  private class BitsAnonymousInnerClassHelper2 : Bits
	  {
		  private readonly SimpleTextDocValuesReader outerInstance;

		  private Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field;
		  private IndexInput @in;
		  private BytesRef scratch;
		  private DecimalFormat decoder;

		  public BitsAnonymousInnerClassHelper2(SimpleTextDocValuesReader outerInstance, Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field, IndexInput @in, BytesRef scratch, DecimalFormat decoder)
		  {
			  this.outerInstance = outerInstance;
			  this.field = field;
			  this.@in = @in;
			  this.scratch = scratch;
			  this.decoder = decoder;
		  }

		  public override bool get(int index)
		  {
			try
			{
			  @in.seek(field.dataStartFilePointer + (9 + field.pattern.Length + field.maxLength + 2) * index);
			  SimpleTextUtil.ReadLine(@in, scratch);
			  Debug.Assert(StringHelper.StartsWith(scratch, LENGTH));
			  int len;
			  try
			  {
				len = (int)decoder.parse(new string(scratch.bytes, scratch.offset + LENGTH.length, scratch.length - LENGTH.length, StandardCharsets.UTF_8));
			  }
			  catch (ParseException pe)
			  {
				CorruptIndexException e = new CorruptIndexException("failed to parse int length (resource=" + @in + ")");
				e.initCause(pe);
				throw e;
			  }
			  // skip past bytes
			  sbyte[] bytes = new sbyte[len];
			  @in.readBytes(bytes, 0, len);
			  SimpleTextUtil.ReadLine(@in, scratch); // newline
			  SimpleTextUtil.ReadLine(@in, scratch); // 'T' or 'F'
			  return scratch.bytes[scratch.offset] == (sbyte) 'T';
			}
			catch (IOException ioe)
			{
			  throw new Exception(ioe);
			}
		  }

		  public override int length()
		  {
			return outerInstance.maxDoc;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public index.SortedDocValues getSorted(index.FieldInfo fieldInfo) throws java.io.IOException
	  public override SortedDocValues getSorted(FieldInfo fieldInfo)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final OneField field = fields.get(fieldInfo.name);
		OneField field = fields[fieldInfo.name];

		// SegmentCoreReaders already verifies this field is
		// valid:
		Debug.Assert(field != null);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final store.IndexInput in = data.clone();
		IndexInput @in = data.clone();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final util.BytesRef scratch = new util.BytesRef();
		BytesRef scratch = new BytesRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.text.DecimalFormat decoder = new java.text.DecimalFormat(field.pattern, new java.text.DecimalFormatSymbols(java.util.Locale.ROOT));
		DecimalFormat decoder = new DecimalFormat(field.pattern, new DecimalFormatSymbols(Locale.ROOT));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.text.DecimalFormat ordDecoder = new java.text.DecimalFormat(field.ordPattern, new java.text.DecimalFormatSymbols(java.util.Locale.ROOT));
		DecimalFormat ordDecoder = new DecimalFormat(field.ordPattern, new DecimalFormatSymbols(Locale.ROOT));

		return new SortedDocValuesAnonymousInnerClassHelper(this, field, @in, scratch, decoder, ordDecoder);
	  }

	  private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
	  {
		  private readonly SimpleTextDocValuesReader outerInstance;

		  private Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field;
		  private IndexInput @in;
		  private BytesRef scratch;
		  private DecimalFormat decoder;
		  private DecimalFormat ordDecoder;

		  public SortedDocValuesAnonymousInnerClassHelper(SimpleTextDocValuesReader outerInstance, Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field, IndexInput @in, BytesRef scratch, DecimalFormat decoder, DecimalFormat ordDecoder)
		  {
			  this.outerInstance = outerInstance;
			  this.field = field;
			  this.@in = @in;
			  this.scratch = scratch;
			  this.decoder = decoder;
			  this.ordDecoder = ordDecoder;
		  }

		  public override int getOrd(int docID)
		  {
			if (docID < 0 || docID >= outerInstance.maxDoc)
			{
			  throw new System.IndexOutOfRangeException("docID must be 0 .. " + (outerInstance.maxDoc - 1) + "; got " + docID);
			}
			try
			{
			  @in.seek(field.dataStartFilePointer + field.numValues * (9 + field.pattern.Length + field.maxLength) + docID * (1 + field.ordPattern.Length));
			  SimpleTextUtil.ReadLine(@in, scratch);
			  try
			  {
				return (long)(int) ordDecoder.parse(scratch.utf8ToString()) - 1;
			  }
			  catch (ParseException pe)
			  {
				CorruptIndexException e = new CorruptIndexException("failed to parse ord (resource=" + @in + ")");
				e.initCause(pe);
				throw e;
			  }
			}
			catch (IOException ioe)
			{
			  throw new Exception(ioe);
			}
		  }

		  public override void lookupOrd(int ord, BytesRef result)
		  {
			try
			{
			  if (ord < 0 || ord >= field.numValues)
			  {
				throw new System.IndexOutOfRangeException("ord must be 0 .. " + (field.numValues - 1) + "; got " + ord);
			  }
			  @in.seek(field.dataStartFilePointer + ord * (9 + field.pattern.Length + field.maxLength));
			  SimpleTextUtil.ReadLine(@in, scratch);
			  Debug.Assert(StringHelper.StartsWith(scratch, LENGTH), "got " + scratch.utf8ToString() + " in=" + @in);
			  int len;
			  try
			  {
				len = (int)decoder.parse(new string(scratch.bytes, scratch.offset + LENGTH.length, scratch.length - LENGTH.length, StandardCharsets.UTF_8));
			  }
			  catch (ParseException pe)
			  {
				CorruptIndexException e = new CorruptIndexException("failed to parse int length (resource=" + @in + ")");
				e.initCause(pe);
				throw e;
			  }
			  result.bytes = new sbyte[len];
			  result.offset = 0;
			  result.length = len;
			  @in.readBytes(result.bytes, 0, len);
			}
			catch (IOException ioe)
			{
			  throw new Exception(ioe);
			}
		  }

		  public override int ValueCount
		  {
			  get
			  {
				return (int)field.numValues;
			  }
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public index.SortedSetDocValues getSortedSet(index.FieldInfo fieldInfo) throws java.io.IOException
	  public override SortedSetDocValues getSortedSet(FieldInfo fieldInfo)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final OneField field = fields.get(fieldInfo.name);
		OneField field = fields[fieldInfo.name];

		// SegmentCoreReaders already verifies this field is
		// valid:
		Debug.Assert(field != null);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final store.IndexInput in = data.clone();
		IndexInput @in = data.clone();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final util.BytesRef scratch = new util.BytesRef();
		BytesRef scratch = new BytesRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.text.DecimalFormat decoder = new java.text.DecimalFormat(field.pattern, new java.text.DecimalFormatSymbols(java.util.Locale.ROOT));
		DecimalFormat decoder = new DecimalFormat(field.pattern, new DecimalFormatSymbols(Locale.ROOT));

		return new SortedSetDocValuesAnonymousInnerClassHelper(this, field, @in, scratch, decoder);
	  }

	  private class SortedSetDocValuesAnonymousInnerClassHelper : SortedSetDocValues
	  {
		  private readonly SimpleTextDocValuesReader outerInstance;

		  private Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field;
		  private IndexInput @in;
		  private BytesRef scratch;
		  private DecimalFormat decoder;

		  public SortedSetDocValuesAnonymousInnerClassHelper(SimpleTextDocValuesReader outerInstance, Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesReader.OneField field, IndexInput @in, BytesRef scratch, DecimalFormat decoder)
		  {
			  this.outerInstance = outerInstance;
			  this.field = field;
			  this.@in = @in;
			  this.scratch = scratch;
			  this.decoder = decoder;
			  currentOrds = new string[0];
			  currentIndex = 0;
		  }

		  internal string[] currentOrds;
		  internal int currentIndex;

		  public override long nextOrd()
		  {
			if (currentIndex == currentOrds.length)
			{
			  return NO_MORE_ORDS;
			}
			else
			{
			  return Convert.ToInt64(currentOrds[currentIndex++]);
			}
		  }

		  public override int Document
		  {
			  set
			  {
				if (value < 0 || value >= outerInstance.maxDoc)
				{
				  throw new System.IndexOutOfRangeException("docID must be 0 .. " + (outerInstance.maxDoc - 1) + "; got " + value);
				}
				try
				{
				  @in.seek(field.dataStartFilePointer + field.numValues * (9 + field.pattern.Length + field.maxLength) + value * (1 + field.ordPattern.Length));
				  SimpleTextUtil.ReadLine(@in, scratch);
				  string ordList = scratch.utf8ToString().Trim();
				  if (ordList.Length == 0)
				  {
					currentOrds = new string[0];
				  }
				  else
				  {
					currentOrds = ordList.Split(",", true);
				  }
				  currentIndex = 0;
				}
				catch (IOException ioe)
				{
				  throw new Exception(ioe);
				}
			  }
		  }

		  public override void lookupOrd(long ord, BytesRef result)
		  {
			try
			{
			  if (ord < 0 || ord >= field.numValues)
			  {
				throw new System.IndexOutOfRangeException("ord must be 0 .. " + (field.numValues - 1) + "; got " + ord);
			  }
			  @in.seek(field.dataStartFilePointer + ord * (9 + field.pattern.Length + field.maxLength));
			  SimpleTextUtil.ReadLine(@in, scratch);
			  Debug.Assert(StringHelper.StartsWith(scratch, LENGTH), "got " + scratch.utf8ToString() + " in=" + @in);
			  int len;
			  try
			  {
				len = (int)decoder.parse(new string(scratch.bytes, scratch.offset + LENGTH.length, scratch.length - LENGTH.length, StandardCharsets.UTF_8));
			  }
			  catch (ParseException pe)
			  {
				CorruptIndexException e = new CorruptIndexException("failed to parse int length (resource=" + @in + ")");
				e.initCause(pe);
				throw e;
			  }
			  result.bytes = new sbyte[len];
			  result.offset = 0;
			  result.length = len;
			  @in.readBytes(result.bytes, 0, len);
			}
			catch (IOException ioe)
			{
			  throw new Exception(ioe);
			}
		  }

		  public override long ValueCount
		  {
			  get
			  {
				return field.numValues;
			  }
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public util.Bits getDocsWithField(index.FieldInfo field) throws java.io.IOException
	  public override Bits getDocsWithField(FieldInfo field)
	  {
		switch (field.DocValuesType)
		{
		  case SORTED_SET:
			return DocValues.docsWithValue(getSortedSet(field), maxDoc);
		  case SORTED:
			return DocValues.docsWithValue(getSorted(field), maxDoc);
		  case BINARY:
			return getBinaryDocsWithField(field);
		  case NUMERIC:
			return getNumericDocsWithField(field);
		  default:
			throw new AssertionError();
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		data.close();
	  }

	  /// <summary>
	  /// Used only in ctor: </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void readLine() throws java.io.IOException
	  private void readLine()
	  {
		SimpleTextUtil.ReadLine(data, scratch);
		//System.out.println("line: " + scratch.utf8ToString());
	  }

	  /// <summary>
	  /// Used only in ctor: </summary>
	  private bool StartsWith(BytesRef prefix)
	  {
		return StringHelper.StartsWith(scratch, prefix);
	  }

	  /// <summary>
	  /// Used only in ctor: </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private String stripPrefix(util.BytesRef prefix) throws java.io.IOException
	  private string stripPrefix(BytesRef prefix)
	  {
		return new string(scratch.bytes, scratch.offset + prefix.length, scratch.length - prefix.length, StandardCharsets.UTF_8);
	  }

	  public override long ramBytesUsed()
	  {
		return 0;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void checkIntegrity() throws java.io.IOException
	  public override void checkIntegrity()
	  {
		BytesRef scratch = new BytesRef();
		IndexInput clone = data.clone();
		clone.seek(0);
		ChecksumIndexInput input = new BufferedChecksumIndexInput(clone);
		while (true)
		{
		  SimpleTextUtil.ReadLine(input, scratch);
		  if (scratch.Equals(END))
		  {
			SimpleTextUtil.CheckFooter(input);
			break;
		  }
		}
	  }
	}

}