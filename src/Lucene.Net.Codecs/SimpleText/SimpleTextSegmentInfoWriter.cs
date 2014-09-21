using System;
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


	using FieldInfos = index.FieldInfos;
	using IndexFileNames = index.IndexFileNames;
	using SegmentInfo = index.SegmentInfo;
	using Directory = store.Directory;
	using IOContext = store.IOContext;
	using IndexOutput = store.IndexOutput;
	using BytesRef = util.BytesRef;
	using IOUtils = util.IOUtils;

	/// <summary>
	/// writes plaintext segments files
	/// <para>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// @lucene.experimental
	/// </para>
	/// </summary>
	public class SimpleTextSegmentInfoWriter : SegmentInfoWriter
	{

	  internal static readonly BytesRef SI_VERSION = new BytesRef("    version ");
	  internal static readonly BytesRef SI_DOCCOUNT = new BytesRef("    number of documents ");
	  internal static readonly BytesRef SI_USECOMPOUND = new BytesRef("    uses compound file ");
	  internal static readonly BytesRef SI_NUM_DIAG = new BytesRef("    diagnostics ");
	  internal static readonly BytesRef SI_DIAG_KEY = new BytesRef("      key ");
	  internal static readonly BytesRef SI_DIAG_VALUE = new BytesRef("      value ");
	  internal static readonly BytesRef SI_NUM_FILES = new BytesRef("    files ");
	  internal static readonly BytesRef SI_FILE = new BytesRef("      file ");

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void write(store.Directory dir, index.SegmentInfo si, index.FieldInfos fis, store.IOContext ioContext) throws java.io.IOException
	  public override void write(Directory dir, SegmentInfo si, FieldInfos fis, IOContext ioContext)
	  {

		string segFileName = IndexFileNames.segmentFileName(si.name, "", SimpleTextSegmentInfoFormat.SI_EXTENSION);
		si.addFile(segFileName);

		bool success = false;
		IndexOutput output = dir.createOutput(segFileName, ioContext);

		try
		{
		  BytesRef scratch = new BytesRef();

		  SimpleTextUtil.write(output, SI_VERSION);
		  SimpleTextUtil.write(output, si.Version, scratch);
		  SimpleTextUtil.WriteNewline(output);

		  SimpleTextUtil.write(output, SI_DOCCOUNT);
		  SimpleTextUtil.write(output, Convert.ToString(si.DocCount), scratch);
		  SimpleTextUtil.WriteNewline(output);

		  SimpleTextUtil.write(output, SI_USECOMPOUND);
		  SimpleTextUtil.write(output, Convert.ToString(si.UseCompoundFile), scratch);
		  SimpleTextUtil.WriteNewline(output);

		  IDictionary<string, string> diagnostics = si.Diagnostics;
		  int numDiagnostics = diagnostics == null ? 0 : diagnostics.Count;
		  SimpleTextUtil.write(output, SI_NUM_DIAG);
		  SimpleTextUtil.write(output, Convert.ToString(numDiagnostics), scratch);
		  SimpleTextUtil.WriteNewline(output);

		  if (numDiagnostics > 0)
		  {
			foreach (KeyValuePair<string, string> diagEntry in diagnostics.SetOfKeyValuePairs())
			{
			  SimpleTextUtil.write(output, SI_DIAG_KEY);
			  SimpleTextUtil.write(output, diagEntry.Key, scratch);
			  SimpleTextUtil.WriteNewline(output);

			  SimpleTextUtil.write(output, SI_DIAG_VALUE);
			  SimpleTextUtil.write(output, diagEntry.Value, scratch);
			  SimpleTextUtil.WriteNewline(output);
			}
		  }

		  HashSet<string> files = si.files();
		  int numFiles = files == null ? 0 : files.Count;
		  SimpleTextUtil.write(output, SI_NUM_FILES);
		  SimpleTextUtil.write(output, Convert.ToString(numFiles), scratch);
		  SimpleTextUtil.WriteNewline(output);

		  if (numFiles > 0)
		  {
			foreach (string fileName in files)
			{
			  SimpleTextUtil.write(output, SI_FILE);
			  SimpleTextUtil.write(output, fileName, scratch);
			  SimpleTextUtil.WriteNewline(output);
			}
		  }

		  SimpleTextUtil.WriteChecksum(output, scratch);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(output);
			try
			{
			  dir.deleteFile(segFileName);
			}
			catch (Exception)
			{
			}
		  }
		  else
		  {
			output.close();
		  }
		}
	  }
	}

}