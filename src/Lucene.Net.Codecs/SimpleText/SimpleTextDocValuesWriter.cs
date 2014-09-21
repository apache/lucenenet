using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

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


	using FieldInfo = Index.FieldInfo;
	using IndexFileNames = Index.IndexFileNames;
	using SegmentWriteState = Index.SegmentWriteState;
	using DocValuesType = Index.FieldInfo.DocValuesType;
	using IndexOutput = Store.IndexOutput;
	using BytesRef = Util.BytesRef;
	using IOUtils = Util.IOUtils;

	public class SimpleTextDocValuesWriter : DocValuesConsumer
	{
	  internal static readonly BytesRef END = new BytesRef("END");
	  internal static readonly BytesRef FIELD = new BytesRef("field ");
	  internal static readonly BytesRef TYPE = new BytesRef("  type ");
	  // used for numerics
	  internal static readonly BytesRef MINVALUE = new BytesRef("  minvalue ");
	  internal static readonly BytesRef PATTERN = new BytesRef("  pattern ");
	  // used for bytes
	  internal static readonly BytesRef LENGTH = new BytesRef("length ");
	  internal static readonly BytesRef MAXLENGTH = new BytesRef("  maxlength ");
	  // used for sorted bytes
	  internal static readonly BytesRef NUMVALUES = new BytesRef("  numvalues ");
	  internal static readonly BytesRef ORDPATTERN = new BytesRef("  ordpattern ");

	  internal IndexOutput data;
	  internal readonly BytesRef scratch = new BytesRef();
	  internal readonly int numDocs;
	  private readonly HashSet<string> fieldsSeen = new HashSet<string>(); // for asserting

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public SimpleTextDocValuesWriter(index.SegmentWriteState state, String ext) throws java.io.IOException
	  public SimpleTextDocValuesWriter(SegmentWriteState state, string ext)
	  {
		// System.out.println("WRITE: " + IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, ext) + " " + state.segmentInfo.getDocCount() + " docs");
		data = state.directory.createOutput(IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, ext), state.context);
		numDocs = state.segmentInfo.DocCount;
	  }

	  // for asserting
	  private bool fieldSeen(string field)
	  {
		Debug.Assert(!fieldsSeen.Contains(field), "field \"" + field + "\" was added more than once during flush");
		fieldsSeen.Add(field);
		return true;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addNumericField(index.FieldInfo field, Iterable<Number> values) throws java.io.IOException
	  public override void addNumericField(FieldInfo field, IEnumerable<Number> values)
	  {
		Debug.Assert(fieldSeen(field.name));
		assert(field.DocValuesType == FieldInfo.DocValuesType.NUMERIC || field.NormType == FieldInfo.DocValuesType.NUMERIC);
		writeFieldEntry(field, FieldInfo.DocValuesType.NUMERIC);

		// first pass to find min/max
		long minValue = long.MaxValue;
		long maxValue = long.MinValue;
		foreach (Number n in values)
		{
		  long v = n == null ? 0 : (long)n;
		  minValue = Math.Min(minValue, v);
		  maxValue = Math.Max(maxValue, v);
		}

		// write our minimum value to the .dat, all entries are deltas from that
		SimpleTextUtil.write(data, MINVALUE);
		SimpleTextUtil.write(data, Convert.ToString(minValue), scratch);
		SimpleTextUtil.WriteNewline(data);

		// build up our fixed-width "simple text packed ints"
		// format
		System.Numerics.BigInteger maxBig = System.Numerics.BigInteger.valueOf(maxValue);
		System.Numerics.BigInteger minBig = System.Numerics.BigInteger.valueOf(minValue);
		System.Numerics.BigInteger diffBig = maxBig - minBig;
		int maxBytesPerValue = diffBig.ToString().Length;
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < maxBytesPerValue; i++)
		{
		  sb.Append('0');
		}

		// write our pattern to the .dat
		SimpleTextUtil.write(data, PATTERN);
		SimpleTextUtil.write(data, sb.ToString(), scratch);
		SimpleTextUtil.WriteNewline(data);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String patternString = sb.toString();
		string patternString = sb.ToString();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.text.DecimalFormat encoder = new java.text.DecimalFormat(patternString, new java.text.DecimalFormatSymbols(java.util.Locale.ROOT));
		DecimalFormat encoder = new DecimalFormat(patternString, new DecimalFormatSymbols(Locale.ROOT));

		int numDocsWritten = 0;

		// second pass to write the values
		foreach (Number n in values)
		{
		  long value = n == null ? 0 : (long)n;
		  Debug.Assert(value >= minValue);
		  Number delta = System.Numerics.BigInteger.valueOf(value) - System.Numerics.BigInteger.valueOf(minValue);
		  string s = encoder.format(delta);
		  Debug.Assert(s.Length == patternString.Length);
		  SimpleTextUtil.write(data, s, scratch);
		  SimpleTextUtil.WriteNewline(data);
		  if (n == null)
		  {
			SimpleTextUtil.write(data, "F", scratch);
		  }
		  else
		  {
			SimpleTextUtil.write(data, "T", scratch);
		  }
		  SimpleTextUtil.WriteNewline(data);
		  numDocsWritten++;
		  Debug.Assert(numDocsWritten <= numDocs);
		}

		Debug.Assert(numDocs == numDocsWritten, "numDocs=" + numDocs + " numDocsWritten=" + numDocsWritten);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addBinaryField(index.FieldInfo field, Iterable<util.BytesRef> values) throws java.io.IOException
	  public override void addBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		Debug.Assert(fieldSeen(field.name));
		Debug.Assert(field.DocValuesType == FieldInfo.DocValuesType.BINARY);
		int maxLength = 0;
		foreach (BytesRef value in values)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int length = value == null ? 0 : value.length;
		  int length = value == null ? 0 : value.length;
		  maxLength = Math.Max(maxLength, length);
		}
		writeFieldEntry(field, FieldInfo.DocValuesType.BINARY);

		// write maxLength
		SimpleTextUtil.write(data, MAXLENGTH);
		SimpleTextUtil.write(data, Convert.ToString(maxLength), scratch);
		SimpleTextUtil.WriteNewline(data);

		int maxBytesLength = Convert.ToString(maxLength).Length;
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < maxBytesLength; i++)
		{
		  sb.Append('0');
		}
		// write our pattern for encoding lengths
		SimpleTextUtil.write(data, PATTERN);
		SimpleTextUtil.write(data, sb.ToString(), scratch);
		SimpleTextUtil.WriteNewline(data);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.text.DecimalFormat encoder = new java.text.DecimalFormat(sb.toString(), new java.text.DecimalFormatSymbols(java.util.Locale.ROOT));
		DecimalFormat encoder = new DecimalFormat(sb.ToString(), new DecimalFormatSymbols(Locale.ROOT));

		int numDocsWritten = 0;
		foreach (BytesRef value in values)
		{
		  // write length
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int length = value == null ? 0 : value.length;
		  int length = value == null ? 0 : value.length;
		  SimpleTextUtil.write(data, LENGTH);
		  SimpleTextUtil.write(data, encoder.format(length), scratch);
		  SimpleTextUtil.WriteNewline(data);

		  // write bytes -- don't use SimpleText.write
		  // because it escapes:
		  if (value != null)
		  {
			data.writeBytes(value.bytes, value.offset, value.length);
		  }

		  // pad to fit
		  for (int i = length; i < maxLength; i++)
		  {
			data.writeByte((sbyte)' ');
		  }
		  SimpleTextUtil.WriteNewline(data);
		  if (value == null)
		  {
			SimpleTextUtil.write(data, "F", scratch);
		  }
		  else
		  {
			SimpleTextUtil.write(data, "T", scratch);
		  }
		  SimpleTextUtil.WriteNewline(data);
		  numDocsWritten++;
		}

		Debug.Assert(numDocs == numDocsWritten);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addSortedField(index.FieldInfo field, Iterable<util.BytesRef> values, Iterable<Number> docToOrd) throws java.io.IOException
	  public override void addSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
	  {
		Debug.Assert(fieldSeen(field.name));
		Debug.Assert(field.DocValuesType == FieldInfo.DocValuesType.SORTED);
		writeFieldEntry(field, FieldInfo.DocValuesType.SORTED);

		int valueCount = 0;
		int maxLength = -1;
		foreach (BytesRef value in values)
		{
		  maxLength = Math.Max(maxLength, value.length);
		  valueCount++;
		}

		// write numValues
		SimpleTextUtil.write(data, NUMVALUES);
		SimpleTextUtil.write(data, Convert.ToString(valueCount), scratch);
		SimpleTextUtil.WriteNewline(data);

		// write maxLength
		SimpleTextUtil.write(data, MAXLENGTH);
		SimpleTextUtil.write(data, Convert.ToString(maxLength), scratch);
		SimpleTextUtil.WriteNewline(data);

		int maxBytesLength = Convert.ToString(maxLength).Length;
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < maxBytesLength; i++)
		{
		  sb.Append('0');
		}

		// write our pattern for encoding lengths
		SimpleTextUtil.write(data, PATTERN);
		SimpleTextUtil.write(data, sb.ToString(), scratch);
		SimpleTextUtil.WriteNewline(data);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.text.DecimalFormat encoder = new java.text.DecimalFormat(sb.toString(), new java.text.DecimalFormatSymbols(java.util.Locale.ROOT));
		DecimalFormat encoder = new DecimalFormat(sb.ToString(), new DecimalFormatSymbols(Locale.ROOT));

		int maxOrdBytes = Convert.ToString(valueCount + 1L).Length;
		sb.Length = 0;
		for (int i = 0; i < maxOrdBytes; i++)
		{
		  sb.Append('0');
		}

		// write our pattern for ords
		SimpleTextUtil.write(data, ORDPATTERN);
		SimpleTextUtil.write(data, sb.ToString(), scratch);
		SimpleTextUtil.WriteNewline(data);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.text.DecimalFormat ordEncoder = new java.text.DecimalFormat(sb.toString(), new java.text.DecimalFormatSymbols(java.util.Locale.ROOT));
		DecimalFormat ordEncoder = new DecimalFormat(sb.ToString(), new DecimalFormatSymbols(Locale.ROOT));

		// for asserts:
		int valuesSeen = 0;

		foreach (BytesRef value in values)
		{
		  // write length
		  SimpleTextUtil.write(data, LENGTH);
		  SimpleTextUtil.write(data, encoder.format(value.length), scratch);
		  SimpleTextUtil.WriteNewline(data);

		  // write bytes -- don't use SimpleText.write
		  // because it escapes:
		  data.writeBytes(value.bytes, value.offset, value.length);

		  // pad to fit
		  for (int i = value.length; i < maxLength; i++)
		  {
			data.writeByte((sbyte)' ');
		  }
		  SimpleTextUtil.WriteNewline(data);
		  valuesSeen++;
		  Debug.Assert(valuesSeen <= valueCount);
		}

		Debug.Assert(valuesSeen == valueCount);

		foreach (Number ord in docToOrd)
		{
		  SimpleTextUtil.write(data, ordEncoder.format((long)ord + 1), scratch);
		  SimpleTextUtil.WriteNewline(data);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addSortedSetField(index.FieldInfo field, Iterable<util.BytesRef> values, Iterable<Number> docToOrdCount, Iterable<Number> ords) throws java.io.IOException
	  public override void addSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
	  {
		Debug.Assert(fieldSeen(field.name));
		Debug.Assert(field.DocValuesType == FieldInfo.DocValuesType.SORTED_SET);
		writeFieldEntry(field, FieldInfo.DocValuesType.SORTED_SET);

		long valueCount = 0;
		int maxLength = 0;
		foreach (BytesRef value in values)
		{
		  maxLength = Math.Max(maxLength, value.length);
		  valueCount++;
		}

		// write numValues
		SimpleTextUtil.write(data, NUMVALUES);
		SimpleTextUtil.write(data, Convert.ToString(valueCount), scratch);
		SimpleTextUtil.WriteNewline(data);

		// write maxLength
		SimpleTextUtil.write(data, MAXLENGTH);
		SimpleTextUtil.write(data, Convert.ToString(maxLength), scratch);
		SimpleTextUtil.WriteNewline(data);

		int maxBytesLength = Convert.ToString(maxLength).Length;
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < maxBytesLength; i++)
		{
		  sb.Append('0');
		}

		// write our pattern for encoding lengths
		SimpleTextUtil.write(data, PATTERN);
		SimpleTextUtil.write(data, sb.ToString(), scratch);
		SimpleTextUtil.WriteNewline(data);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.text.DecimalFormat encoder = new java.text.DecimalFormat(sb.toString(), new java.text.DecimalFormatSymbols(java.util.Locale.ROOT));
		DecimalFormat encoder = new DecimalFormat(sb.ToString(), new DecimalFormatSymbols(Locale.ROOT));

		// compute ord pattern: this is funny, we encode all values for all docs to find the maximum length
		int maxOrdListLength = 0;
		StringBuilder sb2 = new StringBuilder();
		IEnumerator<Number> ordStream = ords.GetEnumerator();
		foreach (Number n in docToOrdCount)
		{
		  sb2.Length = 0;
		  int count = (int)n;
		  for (int i = 0; i < count; i++)
		  {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			long ord = (long)ordStream.next();
			if (sb2.Length > 0)
			{
			  sb2.Append(",");
			}
			sb2.Append(Convert.ToString(ord));
		  }
		  maxOrdListLength = Math.Max(maxOrdListLength, sb2.Length);
		}

		sb2.Length = 0;
		for (int i = 0; i < maxOrdListLength; i++)
		{
		  sb2.Append('X');
		}

		// write our pattern for ord lists
		SimpleTextUtil.write(data, ORDPATTERN);
		SimpleTextUtil.write(data, sb2.ToString(), scratch);
		SimpleTextUtil.WriteNewline(data);

		// for asserts:
		long valuesSeen = 0;

		foreach (BytesRef value in values)
		{
		  // write length
		  SimpleTextUtil.write(data, LENGTH);
		  SimpleTextUtil.write(data, encoder.format(value.length), scratch);
		  SimpleTextUtil.WriteNewline(data);

		  // write bytes -- don't use SimpleText.write
		  // because it escapes:
		  data.writeBytes(value.bytes, value.offset, value.length);

		  // pad to fit
		  for (int i = value.length; i < maxLength; i++)
		  {
			data.writeByte((sbyte)' ');
		  }
		  SimpleTextUtil.WriteNewline(data);
		  valuesSeen++;
		  Debug.Assert(valuesSeen <= valueCount);
		}

		Debug.Assert(valuesSeen == valueCount);

		ordStream = ords.GetEnumerator();

		// write the ords for each doc comma-separated
		foreach (Number n in docToOrdCount)
		{
		  sb2.Length = 0;
		  int count = (int)n;
		  for (int i = 0; i < count; i++)
		  {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			long ord = (long)ordStream.next();
			if (sb2.Length > 0)
			{
			  sb2.Append(",");
			}
			sb2.Append(Convert.ToString(ord));
		  }
		  // now pad to fit: these are numbers so spaces work well. reader calls trim()
		  int numPadding = maxOrdListLength - sb2.Length;
		  for (int i = 0; i < numPadding; i++)
		  {
			sb2.Append(' ');
		  }
		  SimpleTextUtil.write(data, sb2.ToString(), scratch);
		  SimpleTextUtil.WriteNewline(data);
		}
	  }

	  /// <summary>
	  /// write the header for this field </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void writeFieldEntry(index.FieldInfo field, index.FieldInfo.DocValuesType type) throws java.io.IOException
	  private void writeFieldEntry(FieldInfo field, FieldInfo.DocValuesType type)
	  {
		SimpleTextUtil.write(data, FIELD);
		SimpleTextUtil.write(data, field.name, scratch);
		SimpleTextUtil.WriteNewline(data);

		SimpleTextUtil.write(data, TYPE);
		SimpleTextUtil.write(data, type.ToString(), scratch);
		SimpleTextUtil.WriteNewline(data);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		if (data != null)
		{
		  bool success = false;
		  try
		  {
			Debug.Assert(fieldsSeen.Count > 0);
			// TODO: sheisty to do this here?
			SimpleTextUtil.write(data, END);
			SimpleTextUtil.WriteNewline(data);
			SimpleTextUtil.WriteChecksum(data, scratch);
			success = true;
		  }
		  finally
		  {
			if (success)
			{
			  IOUtils.close(data);
			}
			else
			{
			  IOUtils.closeWhileHandlingException(data);
			}
			data = null;
		  }
		}
	  }
	}

}