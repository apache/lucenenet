using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
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



	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using CharsRef = Lucene.Net.Util.CharsRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;


	/// <summary>
	/// this stores a monotonically increasing set of <Term, TermInfo> pairs in a
	///  Directory.  A TermInfos can be written once, in order.  
	/// </summary>

	internal sealed class TermInfosWriter : IDisposable
	{
	  /// <summary>
	  /// The file format version, a negative number. </summary>
	  public const int FORMAT = -3;

	  // Changed strings to true utf8 with length-in-bytes not
	  // length-in-chars
	  public const int FORMAT_VERSION_UTF8_LENGTH_IN_BYTES = -4;

	  // NOTE: always change this if you switch to a new format!
	  public const int FORMAT_CURRENT = FORMAT_VERSION_UTF8_LENGTH_IN_BYTES;

	  private FieldInfos FieldInfos;
	  private IndexOutput Output;
	  private TermInfo LastTi = new TermInfo();
	  private long Size;

	  // TODO: the default values for these two parameters should be settable from
	  // IndexWriter.  However, once that's done, folks will start setting them to
	  // ridiculous values and complaining that things don't work well, as with
	  // mergeFactor.  So, let's wait until a number of folks find that alternate
	  // values work better.  Note that both of these values are stored in the
	  // segment, so that it's safe to change these w/o rebuilding all indexes.

	  /// <summary>
	  /// Expert: The fraction of terms in the "dictionary" which should be stored
	  /// in RAM.  Smaller values use more memory, but make searching slightly
	  /// faster, while larger values use less memory and make searching slightly
	  /// slower.  Searching is typically not dominated by dictionary lookup, so
	  /// tweaking this is rarely useful.
	  /// </summary>
	  internal int IndexInterval = 128;

	  /// <summary>
	  /// Expert: The fraction of term entries stored in skip tables,
	  /// used to accelerate skipping.  Larger values result in
	  /// smaller indexes, greater acceleration, but fewer accelerable cases, while
	  /// smaller values result in bigger indexes, less acceleration and more
	  /// accelerable cases. More detailed experiments would be useful here. 
	  /// </summary>
	  internal int SkipInterval = 16;

	  /// <summary>
	  /// Expert: The maximum number of skip levels. Smaller values result in 
	  /// slightly smaller indexes, but slower skipping in big posting lists.
	  /// </summary>
	  internal int MaxSkipLevels = 10;

	  private long LastIndexPointer;
	  private bool IsIndex;
	  private readonly BytesRef LastTerm = new BytesRef();
	  private int LastFieldNumber = -1;

	  private TermInfosWriter Other;

	  internal TermInfosWriter(Directory directory, string segment, FieldInfos fis, int interval)
	  {
		Initialize(directory, segment, fis, interval, false);
		bool success = false;
		try
		{
		  Other = new TermInfosWriter(directory, segment, fis, interval, true);
		  Other.Other = this;
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(Output);

			try
			{
			  directory.deleteFile(IndexFileNames.segmentFileName(segment, "", (IsIndex ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat.TERMS_EXTENSION)));
			}
			catch (IOException ignored)
			{
			}
		  }
		}
	  }

	  private TermInfosWriter(Directory directory, string segment, FieldInfos fis, int interval, bool isIndex)
	  {
		Initialize(directory, segment, fis, interval, isIndex);
	  }

	  private void Initialize(Directory directory, string segment, FieldInfos fis, int interval, bool isi)
	  {
		IndexInterval = interval;
		FieldInfos = fis;
		IsIndex = isi;
		Output = directory.createOutput(IndexFileNames.segmentFileName(segment, "", (IsIndex ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat.TERMS_EXTENSION)), IOContext.DEFAULT);
		bool success = false;
		try
		{
		  Output.writeInt(FORMAT_CURRENT); // write format
		  Output.writeLong(0); // leave space for size
		  Output.writeInt(IndexInterval); // write indexInterval
		  Output.writeInt(SkipInterval); // write skipInterval
		  Output.writeInt(MaxSkipLevels); // write maxSkipLevels
		  Debug.Assert(InitUTF16Results());
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(Output);

			try
			{
			  directory.deleteFile(IndexFileNames.segmentFileName(segment, "", (IsIndex ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat.TERMS_EXTENSION)));
			}
			catch (IOException ignored)
			{
			}
		  }
		}
	  }

	  // Currently used only by assert statements
	  internal CharsRef Utf16Result1;
	  internal CharsRef Utf16Result2;
	  private readonly BytesRef ScratchBytes = new BytesRef();

	  // Currently used only by assert statements
	  private bool InitUTF16Results()
	  {
		Utf16Result1 = new CharsRef(10);
		Utf16Result2 = new CharsRef(10);
		return true;
	  }

	  /// <summary>
	  /// note: -1 is the empty field: "" !!!! </summary>
	  internal static string FieldName(FieldInfos infos, int fieldNumber)
	  {
		if (fieldNumber == -1)
		{
		  return "";
		}
		else
		{
		  return infos.fieldInfo(fieldNumber).name;
		}
	  }

	  // Currently used only by assert statement
	  private int CompareToLastTerm(int fieldNumber, BytesRef term)
	  {

		if (LastFieldNumber != fieldNumber)
		{
		  int cmp = FieldName(FieldInfos, LastFieldNumber).CompareTo(FieldName(FieldInfos, fieldNumber));
		  // If there is a field named "" (empty string) then we
		  // will get 0 on this comparison, yet, it's "OK".  But
		  // it's not OK if two different field numbers map to
		  // the same name.
		  if (cmp != 0 || LastFieldNumber != -1)
		  {
			return cmp;
		  }
		}

		ScratchBytes.copyBytes(term);
		Debug.Assert(LastTerm.offset == 0);
		UnicodeUtil.UTF8toUTF16(LastTerm.bytes, 0, LastTerm.length, Utf16Result1);

		Debug.Assert(ScratchBytes.offset == 0);
		UnicodeUtil.UTF8toUTF16(ScratchBytes.bytes, 0, ScratchBytes.length, Utf16Result2);

		int len;
		if (Utf16Result1.length < Utf16Result2.length)
		{
		  len = Utf16Result1.length;
		}
		else
		{
		  len = Utf16Result2.length;
		}

		for (int i = 0;i < len;i++)
		{
		  char ch1 = Utf16Result1.chars[i];
		  char ch2 = Utf16Result2.chars[i];
		  if (ch1 != ch2)
		  {
			return ch1 - ch2;
		  }
		}
		if (Utf16Result1.length == 0 && LastFieldNumber == -1)
		{
		  // If there is a field named "" (empty string) with a term text of "" (empty string) then we
		  // will get 0 on this comparison, yet, it's "OK". 
		  return -1;
		}
		return Utf16Result1.length - Utf16Result2.length;
	  }

	  /// <summary>
	  /// Adds a new <<fieldNumber, termBytes>, TermInfo> pair to the set.
	  ///  Term must be lexicographically greater than all previous Terms added.
	  ///  TermInfo pointers must be positive and greater than all previous.
	  /// </summary>
	  public void Add(int fieldNumber, BytesRef term, TermInfo ti)
	  {

		Debug.Assert(CompareToLastTerm(fieldNumber, term) < 0 || (IsIndex && term.length == 0 && LastTerm.length == 0), "Terms are out of order: field=" + FieldName(FieldInfos, fieldNumber) + " (number " + fieldNumber + ")" + " lastField=" + FieldName(FieldInfos, LastFieldNumber) + " (number " + LastFieldNumber + ")" + " text=" + term.utf8ToString() + " lastText=" + LastTerm.utf8ToString());

		Debug.Assert(ti.freqPointer >= LastTi.freqPointer, "freqPointer out of order (" + ti.freqPointer + " < " + LastTi.freqPointer + ")");
		Debug.Assert(ti.proxPointer >= LastTi.proxPointer, "proxPointer out of order (" + ti.proxPointer + " < " + LastTi.proxPointer + ")");

		if (!IsIndex && Size % IndexInterval == 0)
		{
		  Other.Add(LastFieldNumber, LastTerm, LastTi); // add an index term
		}
		WriteTerm(fieldNumber, term); // write term

		Output.writeVInt(ti.docFreq); // write doc freq
		Output.writeVLong(ti.freqPointer - LastTi.freqPointer); // write pointers
		Output.writeVLong(ti.proxPointer - LastTi.proxPointer);

		if (ti.docFreq >= SkipInterval)
		{
		  Output.writeVInt(ti.skipOffset);
		}

		if (IsIndex)
		{
		  Output.writeVLong(Other.Output.FilePointer - LastIndexPointer);
		  LastIndexPointer = Other.Output.FilePointer; // write pointer
		}

		LastFieldNumber = fieldNumber;
		LastTi.set(ti);
		Size++;
	  }

	  private void WriteTerm(int fieldNumber, BytesRef term)
	  {

		//System.out.println("  tiw.write field=" + fieldNumber + " term=" + term.utf8ToString());

		// TODO: UTF16toUTF8 could tell us this prefix
		// Compute prefix in common with last term:
		int start = 0;
		int limit = term.length < LastTerm.length ? term.length : LastTerm.length;
		while (start < limit)
		{
		  if (term.bytes[start + term.offset] != LastTerm.bytes[start + LastTerm.offset])
		  {
			break;
		  }
		  start++;
		}

		int length = term.length - start;
		Output.writeVInt(start); // write shared prefix length
		Output.writeVInt(length); // write delta length
		Output.writeBytes(term.bytes, start + term.offset, length); // write delta bytes
		Output.writeVInt(fieldNumber); // write field num
		LastTerm.copyBytes(term);
	  }

	  /// <summary>
	  /// Called to complete TermInfos creation. </summary>
	  public void Close()
	  {
		try
		{
		  Output.seek(4); // write size after format
		  Output.writeLong(Size);
		}
		finally
		{
		  try
		  {
			Output.close();
		  }
		  finally
		  {
			if (!IsIndex)
			{
			  Other.Close();
			}
		  }
		}
	  }
	}

}