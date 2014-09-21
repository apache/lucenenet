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


	using FieldInfo = index.FieldInfo;
	using DocValuesType = index.FieldInfo.DocValuesType;
	using FieldInfos = index.FieldInfos;
	using IndexFileNames = index.IndexFileNames;
	using IndexOptions = index.FieldInfo.IndexOptions;
	using ChecksumIndexInput = store.ChecksumIndexInput;
	using Directory = store.Directory;
	using IOContext = store.IOContext;
	using BytesRef = util.BytesRef;
	using IOUtils = util.IOUtils;
	using StringHelper = util.StringHelper;

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.SimpleText.SimpleTextFieldInfosWriter.*;

	/// <summary>
	/// reads plaintext field infos files
	/// <para>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// @lucene.experimental
	/// </para>
	/// </summary>
	public class SimpleTextFieldInfosReader : FieldInfosReader
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public index.FieldInfos read(store.Directory directory, String segmentName, String segmentSuffix, store.IOContext iocontext) throws java.io.IOException
	  public override FieldInfos read(Directory directory, string segmentName, string segmentSuffix, IOContext iocontext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String fileName = index.IndexFileNames.segmentFileName(segmentName, segmentSuffix, FIELD_INFOS_EXTENSION);
		string fileName = IndexFileNames.segmentFileName(segmentName, segmentSuffix, FIELD_INFOS_EXTENSION);
		ChecksumIndexInput input = directory.openChecksumInput(fileName, iocontext);
		BytesRef scratch = new BytesRef();

		bool success = false;
		try
		{

		  SimpleTextUtil.ReadLine(input, scratch);
		  Debug.Assert(StringHelper.StartsWith(scratch, NUMFIELDS));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = Integer.parseInt(readString(NUMFIELDS.length, scratch));
		  int size = Convert.ToInt32(readString(NUMFIELDS.length, scratch));
		  FieldInfo[] infos = new FieldInfo[size];

		  for (int i = 0; i < size; i++)
		  {
			SimpleTextUtil.ReadLine(input, scratch);
			Debug.Assert(StringHelper.StartsWith(scratch, NAME));
			string name = readString(NAME.length, scratch);

			SimpleTextUtil.ReadLine(input, scratch);
			Debug.Assert(StringHelper.StartsWith(scratch, NUMBER));
			int fieldNumber = Convert.ToInt32(readString(NUMBER.length, scratch));

			SimpleTextUtil.ReadLine(input, scratch);
			Debug.Assert(StringHelper.StartsWith(scratch, ISINDEXED));
			bool isIndexed = Convert.ToBoolean(readString(ISINDEXED.length, scratch));

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final index.FieldInfo.IndexOptions indexOptions;
			FieldInfo.IndexOptions indexOptions;
			if (isIndexed)
			{
			  SimpleTextUtil.ReadLine(input, scratch);
			  Debug.Assert(StringHelper.StartsWith(scratch, INDEXOPTIONS));
			  indexOptions = FieldInfo.IndexOptions.valueOf(readString(INDEXOPTIONS.length, scratch));
			}
			else
			{
			  indexOptions = null;
			}

			SimpleTextUtil.ReadLine(input, scratch);
			Debug.Assert(StringHelper.StartsWith(scratch, STORETV));
			bool storeTermVector = Convert.ToBoolean(readString(STORETV.length, scratch));

			SimpleTextUtil.ReadLine(input, scratch);
			Debug.Assert(StringHelper.StartsWith(scratch, PAYLOADS));
			bool storePayloads = Convert.ToBoolean(readString(PAYLOADS.length, scratch));

			SimpleTextUtil.ReadLine(input, scratch);
			Debug.Assert(StringHelper.StartsWith(scratch, NORMS));
			bool omitNorms = !Convert.ToBoolean(readString(NORMS.length, scratch));

			SimpleTextUtil.ReadLine(input, scratch);
			Debug.Assert(StringHelper.StartsWith(scratch, NORMS_TYPE));
			string nrmType = readString(NORMS_TYPE.length, scratch);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final index.FieldInfo.DocValuesType normsType = docValuesType(nrmType);
			FieldInfo.DocValuesType normsType = docValuesType(nrmType);

			SimpleTextUtil.ReadLine(input, scratch);
			Debug.Assert(StringHelper.StartsWith(scratch, DOCVALUES));
			string dvType = readString(DOCVALUES.length, scratch);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final index.FieldInfo.DocValuesType docValuesType = docValuesType(dvType);
			FieldInfo.DocValuesType docValuesType = docValuesType(dvType);

			SimpleTextUtil.ReadLine(input, scratch);
			Debug.Assert(StringHelper.StartsWith(scratch, DOCVALUES_GEN));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long dvGen = Long.parseLong(readString(DOCVALUES_GEN.length, scratch));
			long dvGen = Convert.ToInt64(readString(DOCVALUES_GEN.length, scratch));

			SimpleTextUtil.ReadLine(input, scratch);
			Debug.Assert(StringHelper.StartsWith(scratch, NUM_ATTS));
			int numAtts = Convert.ToInt32(readString(NUM_ATTS.length, scratch));
			IDictionary<string, string> atts = new Dictionary<string, string>();

			for (int j = 0; j < numAtts; j++)
			{
			  SimpleTextUtil.ReadLine(input, scratch);
			  Debug.Assert(StringHelper.StartsWith(scratch, ATT_KEY));
			  string key = readString(ATT_KEY.length, scratch);

			  SimpleTextUtil.ReadLine(input, scratch);
			  Debug.Assert(StringHelper.StartsWith(scratch, ATT_VALUE));
			  string value = readString(ATT_VALUE.length, scratch);
			  atts[key] = value;
			}

			infos[i] = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms, storePayloads, indexOptions, docValuesType, normsType, Collections.unmodifiableMap(atts));
			infos[i].DocValuesGen = dvGen;
		  }

		  SimpleTextUtil.CheckFooter(input);

		  FieldInfos fieldInfos = new FieldInfos(infos);
		  success = true;
		  return fieldInfos;
		}
		finally
		{
		  if (success)
		  {
			input.close();
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(input);
		  }
		}
	  }

	  public virtual FieldInfo.DocValuesType docValuesType(string dvType)
	  {
		if ("false".Equals(dvType))
		{
		  return null;
		}
		else
		{
		  return FieldInfo.DocValuesType.valueOf(dvType);
		}
	  }

	  private string readString(int offset, BytesRef scratch)
	  {
		return new string(scratch.bytes, scratch.offset + offset, scratch.length - offset, StandardCharsets.UTF_8);
	  }
	}

}