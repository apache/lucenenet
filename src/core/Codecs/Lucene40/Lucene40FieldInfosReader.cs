using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene40
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


	using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// Lucene 4.0 FieldInfos reader.
	/// 
	/// @lucene.experimental </summary>
	/// <seealso cref= Lucene40FieldInfosFormat </seealso>
	/// @deprecated Only for reading old 4.0 and 4.1 segments 
	[Obsolete("Only for reading old 4.0 and 4.1 segments")]
	internal class Lucene40FieldInfosReader : FieldInfosReader
	{

	  /// <summary>
	  /// Sole constructor. </summary>
	  public Lucene40FieldInfosReader()
	  {
	  }

	  public override FieldInfos Read(Directory directory, string segmentName, string segmentSuffix, IOContext iocontext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String fileName = Lucene.Net.Index.IndexFileNames.segmentFileName(segmentName, "", Lucene40FieldInfosFormat.FIELD_INFOS_EXTENSION);
		string fileName = IndexFileNames.SegmentFileName(segmentName, "", Lucene40FieldInfosFormat.FIELD_INFOS_EXTENSION);
		IndexInput input = directory.OpenInput(fileName, iocontext);

		bool success = false;
		try
		{
		  CodecUtil.CheckHeader(input, Lucene40FieldInfosFormat.CODEC_NAME, Lucene40FieldInfosFormat.FORMAT_START, Lucene40FieldInfosFormat.FORMAT_CURRENT);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = input.readVInt();
		  int size = input.ReadVInt(); //read in the size
		  FieldInfo[] infos = new FieldInfo[size];

		  for (int i = 0; i < size; i++)
		  {
			string name = input.ReadString();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int fieldNumber = input.readVInt();
			int fieldNumber = input.ReadVInt();
			sbyte bits = input.ReadByte();
			bool isIndexed = (bits & Lucene40FieldInfosFormat.IS_INDEXED) != 0;
			bool storeTermVector = (bits & Lucene40FieldInfosFormat.STORE_TERMVECTOR) != 0;
			bool omitNorms = (bits & Lucene40FieldInfosFormat.OMIT_NORMS) != 0;
			bool storePayloads = (bits & Lucene40FieldInfosFormat.STORE_PAYLOADS) != 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.FieldInfo.IndexOptions indexOptions;
			FieldInfo.IndexOptions indexOptions;
			if (!isIndexed)
			{
			  indexOptions = null;
			}
			else if ((bits & Lucene40FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS) != 0)
			{
			  indexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
			}
			else if ((bits & Lucene40FieldInfosFormat.OMIT_POSITIONS) != 0)
			{
			  indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS;
			}
			else if ((bits & Lucene40FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS) != 0)
			{
			  indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
			}
			else
			{
			  indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
			}

			// LUCENE-3027: past indices were able to write
			// storePayloads=true when omitTFAP is also true,
			// which is invalid.  We correct that, here:
			if (isIndexed && indexOptions.compareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
			{
			  storePayloads = false;
			}
			// DV Types are packed in one byte
			sbyte val = input.ReadByte();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final LegacyDocValuesType oldValuesType = getDocValuesType((byte)(val & 0x0F));
			LegacyDocValuesType oldValuesType = GetDocValuesType((sbyte)(val & 0x0F));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final LegacyDocValuesType oldNormsType = getDocValuesType((byte)((val >>> 4) & 0x0F));
			LegacyDocValuesType oldNormsType = GetDocValuesType((sbyte)(((int)((uint)val >> 4)) & 0x0F));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String,String> attributes = input.readStringStringMap();
			IDictionary<string, string> attributes = input.ReadStringStringMap();
			if (oldValuesType.mapping != null)
			{
			  attributes[LEGACY_DV_TYPE_KEY] = oldValuesType.name();
			}
			if (oldNormsType.mapping != null)
			{
			  if (oldNormsType.mapping != FieldInfo.DocValuesType.NUMERIC)
			  {
				throw new CorruptIndexException("invalid norm type: " + oldNormsType + " (resource=" + input + ")");
			  }
			  attributes[LEGACY_NORM_TYPE_KEY] = oldNormsType.name();
			}
			infos[i] = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms, storePayloads, indexOptions, oldValuesType.mapping, oldNormsType.mapping, Collections.unmodifiableMap(attributes));
		  }

		  CodecUtil.CheckEOF(input);
		  FieldInfos fieldInfos = new FieldInfos(infos);
		  success = true;
		  return fieldInfos;
		}
		finally
		{
		  if (success)
		  {
			input.Close();
		  }
		  else
		  {
			IOUtils.CloseWhileHandlingException(input);
		  }
		}
	  }

	  internal static readonly string LEGACY_DV_TYPE_KEY = typeof(Lucene40FieldInfosReader).SimpleName + ".dvtype";
	  internal static readonly string LEGACY_NORM_TYPE_KEY = typeof(Lucene40FieldInfosReader).SimpleName + ".normtype";

	  // mapping of 4.0 types -> 4.2 types
	  internal enum LegacyDocValuesType
	  {
		NONE = null,
		VAR_INTS = Lucene.Net.Index.FieldInfo.DocValuesType.NUMERIC,
		FLOAT_32 = Lucene.Net.Index.FieldInfo.DocValuesType.NUMERIC,
		FLOAT_64 = Lucene.Net.Index.FieldInfo.DocValuesType.NUMERIC,
		BYTES_FIXED_STRAIGHT = Lucene.Net.Index.FieldInfo.DocValuesType.BINARY,
		BYTES_FIXED_DEREF = Lucene.Net.Index.FieldInfo.DocValuesType.BINARY,
		BYTES_VAR_STRAIGHT = Lucene.Net.Index.FieldInfo.DocValuesType.BINARY,
		BYTES_VAR_DEREF = Lucene.Net.Index.FieldInfo.DocValuesType.BINARY,
		FIXED_INTS_16 = Lucene.Net.Index.FieldInfo.DocValuesType.NUMERIC,
		FIXED_INTS_32 = Lucene.Net.Index.FieldInfo.DocValuesType.NUMERIC,
		FIXED_INTS_64 = Lucene.Net.Index.FieldInfo.DocValuesType.NUMERIC,
		FIXED_INTS_8 = Lucene.Net.Index.FieldInfo.DocValuesType.NUMERIC,
		BYTES_FIXED_SORTED = Lucene.Net.Index.FieldInfo.DocValuesType.SORTED,
		BYTES_VAR_SORTED = Lucene.Net.Index.FieldInfo.DocValuesType.SORTED

//JAVA TO C# CONVERTER TODO TASK: Enums cannot contain fields in .NET:
//		final Lucene.Net.Index.FieldInfo.DocValuesType mapping;
//JAVA TO C# CONVERTER TODO TASK: Enums cannot contain methods in .NET:
//		LegacyDocValuesType(Lucene.Net.Index.FieldInfo.DocValuesType mapping)
	//	{
	//	  this.mapping = mapping;
	//	}
	  }

	  // decodes a 4.0 type
	  private static LegacyDocValuesType GetDocValuesType(sbyte b)
	  {
		return Enum.GetValues(typeof(LegacyDocValuesType))[b];
	  }
	}

}