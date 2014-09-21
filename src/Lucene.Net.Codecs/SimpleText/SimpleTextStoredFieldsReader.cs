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

namespace Lucene.Net.Codecs.SimpleText
{

    using System;
    using System.Diagnostics;

	using FieldInfo = Index.FieldInfo;
	using FieldInfos = Index.FieldInfos;
	using IndexFileNames = Index.IndexFileNames;
	using SegmentInfo = Index.SegmentInfo;
	using StoredFieldVisitor = Index.StoredFieldVisitor;
	using AlreadyClosedException = Store.AlreadyClosedException;
	using BufferedChecksumIndexInput = Store.BufferedChecksumIndexInput;
	using ChecksumIndexInput = Store.ChecksumIndexInput;
	using Directory = Store.Directory;
	using IOContext = Store.IOContext;
	using IndexInput = Store.IndexInput;
	using ArrayUtil = Util.ArrayUtil;
	using BytesRef = Util.BytesRef;
	using CharsRef = Util.CharsRef;
	using IOUtils = Util.IOUtils;
	using StringHelper = Util.StringHelper;
	using UnicodeUtil = Util.UnicodeUtil;

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.SimpleText.SimpleTextStoredFieldsWriter.*;

	/// <summary>
	/// reads plaintext stored fields
	/// <para>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// @lucene.experimental
	/// </para>
	/// </summary>
	public class SimpleTextStoredFieldsReader : StoredFieldsReader
	{
	  private long[] offsets; // docid -> offset in .fld file
	  private IndexInput @in;
	  private BytesRef scratch = new BytesRef();
	  private CharsRef scratchUTF16 = new CharsRef();
	  private readonly FieldInfos fieldInfos;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public SimpleTextStoredFieldsReader(store.Directory directory, index.SegmentInfo si, index.FieldInfos fn, store.IOContext context) throws java.io.IOException
	  public SimpleTextStoredFieldsReader(Directory directory, SegmentInfo si, FieldInfos fn, IOContext context)
	  {
		this.fieldInfos = fn;
		bool success = false;
		try
		{
		  @in = directory.openInput(IndexFileNames.segmentFileName(si.name, "", SimpleTextStoredFieldsWriter.FIELDS_EXTENSION), context);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			try
			{
			  close();
			} // ensure we throw our original exception
			catch (Exception)
			{
			}
		  }
		}
		readIndex(si.DocCount);
	  }

	  // used by clone
	  internal SimpleTextStoredFieldsReader(long[] offsets, IndexInput @in, FieldInfos fieldInfos)
	  {
		this.offsets = offsets;
		this.@in = @in;
		this.fieldInfos = fieldInfos;
	  }

	  // we don't actually write a .fdx-like index, instead we read the 
	  // stored fields file in entirety up-front and save the offsets 
	  // so we can seek to the documents later.
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void readIndex(int size) throws java.io.IOException
	  private void readIndex(int size)
	  {
		ChecksumIndexInput input = new BufferedChecksumIndexInput(@in);
		offsets = new long[size];
		int upto = 0;
		while (!scratch.Equals(END))
		{
		  SimpleTextUtil.ReadLine(input, scratch);
		  if (StringHelper.StartsWith(scratch, DOC))
		  {
			offsets[upto] = input.FilePointer;
			upto++;
		  }
		}
		SimpleTextUtil.CheckFooter(input);
		Debug.Assert(upto == offsets.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void visitDocument(int n, index.StoredFieldVisitor visitor) throws java.io.IOException
	  public override void visitDocument(int n, StoredFieldVisitor visitor)
	  {
		@in.seek(offsets[n]);
		readLine();
		Debug.Assert(StringHelper.StartsWith(scratch, NUM));
		int numFields = parseIntAt(NUM.length);

		for (int i = 0; i < numFields; i++)
		{
		  readLine();
		  Debug.Assert(StringHelper.StartsWith(scratch, FIELD));
		  int fieldNumber = parseIntAt(FIELD.length);
		  FieldInfo fieldInfo = fieldInfos.fieldInfo(fieldNumber);
		  readLine();
		  Debug.Assert(StringHelper.StartsWith(scratch, NAME));
		  readLine();
		  Debug.Assert(StringHelper.StartsWith(scratch, TYPE));

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final util.BytesRef type;
		  BytesRef type;
		  if (equalsAt(TYPE_STRING, scratch, TYPE.length))
		  {
			type = TYPE_STRING;
		  }
		  else if (equalsAt(TYPE_BINARY, scratch, TYPE.length))
		  {
			type = TYPE_BINARY;
		  }
		  else if (equalsAt(TYPE_INT, scratch, TYPE.length))
		  {
			type = TYPE_INT;
		  }
		  else if (equalsAt(TYPE_LONG, scratch, TYPE.length))
		  {
			type = TYPE_LONG;
		  }
		  else if (equalsAt(TYPE_FLOAT, scratch, TYPE.length))
		  {
			type = TYPE_FLOAT;
		  }
		  else if (equalsAt(TYPE_DOUBLE, scratch, TYPE.length))
		  {
			type = TYPE_DOUBLE;
		  }
		  else
		  {
			throw new Exception("unknown field type");
		  }

		  switch (visitor.needsField(fieldInfo))
		  {
			case YES:
			  readField(type, fieldInfo, visitor);
			  break;
			case NO:
			  readLine();
			  Debug.Assert(StringHelper.StartsWith(scratch, VALUE));
			  break;
			case STOP:
				return;
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void readField(util.BytesRef type, index.FieldInfo fieldInfo, index.StoredFieldVisitor visitor) throws java.io.IOException
	  private void readField(BytesRef type, FieldInfo fieldInfo, StoredFieldVisitor visitor)
	  {
		readLine();
		Debug.Assert(StringHelper.StartsWith(scratch, VALUE));
		if (type == TYPE_STRING)
		{
		  visitor.stringField(fieldInfo, new string(scratch.bytes, scratch.offset + VALUE.length, scratch.length - VALUE.length, StandardCharsets.UTF_8));
		}
		else if (type == TYPE_BINARY)
		{
		  sbyte[] copy = new sbyte[scratch.length - VALUE.length];
		  Array.Copy(scratch.bytes, scratch.offset + VALUE.length, copy, 0, copy.Length);
		  visitor.binaryField(fieldInfo, copy);
		}
		else if (type == TYPE_INT)
		{
		  UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + VALUE.length, scratch.length - VALUE.length, scratchUTF16);
		  visitor.intField(fieldInfo, Convert.ToInt32(scratchUTF16.ToString()));
		}
		else if (type == TYPE_LONG)
		{
		  UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + VALUE.length, scratch.length - VALUE.length, scratchUTF16);
		  visitor.longField(fieldInfo, Convert.ToInt64(scratchUTF16.ToString()));
		}
		else if (type == TYPE_FLOAT)
		{
		  UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + VALUE.length, scratch.length - VALUE.length, scratchUTF16);
		  visitor.floatField(fieldInfo, Convert.ToSingle(scratchUTF16.ToString()));
		}
		else if (type == TYPE_DOUBLE)
		{
		  UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + VALUE.length, scratch.length - VALUE.length, scratchUTF16);
		  visitor.doubleField(fieldInfo, Convert.ToDouble(scratchUTF16.ToString()));
		}
	  }

	  public override StoredFieldsReader clone()
	  {
		if (@in == null)
		{
		  throw new AlreadyClosedException("this FieldsReader is closed");
		}
		return new SimpleTextStoredFieldsReader(offsets, @in.clone(), fieldInfos);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		try
		{
		  IOUtils.close(@in);
		}
		finally
		{
		  @in = null;
		  offsets = null;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void readLine() throws java.io.IOException
	  private void readLine()
	  {
		SimpleTextUtil.ReadLine(@in, scratch);
	  }

	  private int parseIntAt(int offset)
	  {
		UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + offset, scratch.length - offset, scratchUTF16);
		return ArrayUtil.parseInt(scratchUTF16.chars, 0, scratchUTF16.length);
	  }

	  private bool equalsAt(BytesRef a, BytesRef b, int bOffset)
	  {
		return a.length == b.length - bOffset && ArrayUtil.Equals(a.bytes, a.offset, b.bytes, b.offset + bOffset, b.length - bOffset);
	  }

	  public override long ramBytesUsed()
	  {
		return 0;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void checkIntegrity() throws java.io.IOException
	  public override void checkIntegrity()
	  {
	  }
	}

}