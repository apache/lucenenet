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
	using Directory = store.Directory;
	using IOContext = store.IOContext;
	using IndexOutput = store.IndexOutput;
	using BytesRef = util.BytesRef;
	using IOUtils = util.IOUtils;

	/// <summary>
	/// writes plaintext field infos files
	/// <para>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// @lucene.experimental
	/// </para>
	/// </summary>
	public class SimpleTextFieldInfosWriter : FieldInfosWriter
	{

	  /// <summary>
	  /// Extension of field infos </summary>
	  internal const string FIELD_INFOS_EXTENSION = "inf";

	  internal static readonly BytesRef NUMFIELDS = new BytesRef("number of fields ");
	  internal static readonly BytesRef NAME = new BytesRef("  name ");
	  internal static readonly BytesRef NUMBER = new BytesRef("  number ");
	  internal static readonly BytesRef ISINDEXED = new BytesRef("  indexed ");
	  internal static readonly BytesRef STORETV = new BytesRef("  term vectors ");
	  internal static readonly BytesRef STORETVPOS = new BytesRef("  term vector positions ");
	  internal static readonly BytesRef STORETVOFF = new BytesRef("  term vector offsets ");
	  internal static readonly BytesRef PAYLOADS = new BytesRef("  payloads ");
	  internal static readonly BytesRef NORMS = new BytesRef("  norms ");
	  internal static readonly BytesRef NORMS_TYPE = new BytesRef("  norms type ");
	  internal static readonly BytesRef DOCVALUES = new BytesRef("  doc values ");
	  internal static readonly BytesRef DOCVALUES_GEN = new BytesRef("  doc values gen ");
	  internal static readonly BytesRef INDEXOPTIONS = new BytesRef("  index options ");
	  internal static readonly BytesRef NUM_ATTS = new BytesRef("  attributes ");
	  internal static readonly BytesRef ATT_KEY = new BytesRef("    key ");
	  internal static readonly BytesRef ATT_VALUE = new BytesRef("    value ");

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void write(store.Directory directory, String segmentName, String segmentSuffix, index.FieldInfos infos, store.IOContext context) throws java.io.IOException
	  public override void write(Directory directory, string segmentName, string segmentSuffix, FieldInfos infos, IOContext context)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String fileName = index.IndexFileNames.segmentFileName(segmentName, segmentSuffix, FIELD_INFOS_EXTENSION);
		string fileName = IndexFileNames.segmentFileName(segmentName, segmentSuffix, FIELD_INFOS_EXTENSION);
		IndexOutput @out = directory.createOutput(fileName, context);
		BytesRef scratch = new BytesRef();
		bool success = false;
		try
		{
		  SimpleTextUtil.write(@out, NUMFIELDS);
		  SimpleTextUtil.write(@out, Convert.ToString(infos.size()), scratch);
		  SimpleTextUtil.WriteNewline(@out);

		  foreach (FieldInfo fi in infos)
		  {
			SimpleTextUtil.write(@out, NAME);
			SimpleTextUtil.write(@out, fi.name, scratch);
			SimpleTextUtil.WriteNewline(@out);

			SimpleTextUtil.write(@out, NUMBER);
			SimpleTextUtil.write(@out, Convert.ToString(fi.number), scratch);
			SimpleTextUtil.WriteNewline(@out);

			SimpleTextUtil.write(@out, ISINDEXED);
			SimpleTextUtil.write(@out, Convert.ToString(fi.Indexed), scratch);
			SimpleTextUtil.WriteNewline(@out);

			if (fi.Indexed)
			{
			  Debug.Assert(fi.IndexOptions.compareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !fi.hasPayloads());
			  SimpleTextUtil.write(@out, INDEXOPTIONS);
			  SimpleTextUtil.write(@out, fi.IndexOptions.ToString(), scratch);
			  SimpleTextUtil.WriteNewline(@out);
			}

			SimpleTextUtil.write(@out, STORETV);
			SimpleTextUtil.write(@out, Convert.ToString(fi.hasVectors()), scratch);
			SimpleTextUtil.WriteNewline(@out);

			SimpleTextUtil.write(@out, PAYLOADS);
			SimpleTextUtil.write(@out, Convert.ToString(fi.hasPayloads()), scratch);
			SimpleTextUtil.WriteNewline(@out);

			SimpleTextUtil.write(@out, NORMS);
			SimpleTextUtil.write(@out, Convert.ToString(!fi.omitsNorms()), scratch);
			SimpleTextUtil.WriteNewline(@out);

			SimpleTextUtil.write(@out, NORMS_TYPE);
			SimpleTextUtil.write(@out, getDocValuesType(fi.NormType), scratch);
			SimpleTextUtil.WriteNewline(@out);

			SimpleTextUtil.write(@out, DOCVALUES);
			SimpleTextUtil.write(@out, getDocValuesType(fi.DocValuesType), scratch);
			SimpleTextUtil.WriteNewline(@out);

			SimpleTextUtil.write(@out, DOCVALUES_GEN);
			SimpleTextUtil.write(@out, Convert.ToString(fi.DocValuesGen), scratch);
			SimpleTextUtil.WriteNewline(@out);

			IDictionary<string, string> atts = fi.attributes();
			int numAtts = atts == null ? 0 : atts.Count;
			SimpleTextUtil.write(@out, NUM_ATTS);
			SimpleTextUtil.write(@out, Convert.ToString(numAtts), scratch);
			SimpleTextUtil.WriteNewline(@out);

			if (numAtts > 0)
			{
			  foreach (KeyValuePair<string, string> entry in atts.SetOfKeyValuePairs())
			  {
				SimpleTextUtil.write(@out, ATT_KEY);
				SimpleTextUtil.write(@out, entry.Key, scratch);
				SimpleTextUtil.WriteNewline(@out);

				SimpleTextUtil.write(@out, ATT_VALUE);
				SimpleTextUtil.write(@out, entry.Value, scratch);
				SimpleTextUtil.WriteNewline(@out);
			  }
			}
		  }
		  SimpleTextUtil.WriteChecksum(@out, scratch);
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			@out.close();
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(@out);
		  }
		}
	  }

	  private static string getDocValuesType(FieldInfo.DocValuesType type)
	  {
		return type == null ? "false" : type.ToString();
	  }
	}

}