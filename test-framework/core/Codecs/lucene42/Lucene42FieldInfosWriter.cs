using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene42
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

	using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType_e;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// Lucene 4.2 FieldInfos writer.
	/// </summary>
	/// <seealso cref= Lucene42FieldInfosFormat
	/// @lucene.experimental </seealso>
	[Obsolete]
	public sealed class Lucene42FieldInfosWriter : FieldInfosWriter
	{

	  /// <summary>
	  /// Sole constructor. </summary>
	  public Lucene42FieldInfosWriter()
	  {
	  }

	  public override void Write(Directory directory, string segmentName, string segmentSuffix, FieldInfos infos, IOContext context)
	  {
		string fileName = IndexFileNames.segmentFileName(segmentName, "", Lucene42FieldInfosFormat.EXTENSION);
		IndexOutput output = directory.createOutput(fileName, context);
		bool success = false;
		try
		{
		  CodecUtil.writeHeader(output, Lucene42FieldInfosFormat.CODEC_NAME, Lucene42FieldInfosFormat.FORMAT_CURRENT);
		  output.writeVInt(infos.size());
		  foreach (FieldInfo fi in infos)
		  {
			IndexOptions_e indexOptions = fi.IndexOptions_e;
			sbyte bits = 0x0;
			if (fi.hasVectors())
			{
				bits |= Lucene42FieldInfosFormat.STORE_TERMVECTOR;
			}
			if (fi.omitsNorms())
			{
				bits |= Lucene42FieldInfosFormat.OMIT_NORMS;
			}
			if (fi.hasPayloads())
			{
				bits |= Lucene42FieldInfosFormat.STORE_PAYLOADS;
			}
			if (fi.Indexed)
			{
			  bits |= Lucene42FieldInfosFormat.IS_INDEXED;
			  Debug.Assert(indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !fi.hasPayloads());
			  if (indexOptions == IndexOptions.DOCS_ONLY)
			  {
				bits |= Lucene42FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS;
			  }
			  else if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
			  {
				bits |= Lucene42FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS;
			  }
			  else if (indexOptions == IndexOptions.DOCS_AND_FREQS)
			  {
				bits |= Lucene42FieldInfosFormat.OMIT_POSITIONS;
			  }
			}
			output.writeString(fi.name);
			output.writeVInt(fi.number);
			output.writeByte(bits);

			// pack the DV types in one byte
			sbyte dv = DocValuesByte(fi.DocValuesType_e);
			sbyte nrm = DocValuesByte(fi.NormType);
			assert(dv & (~0xF)) == 0 && (nrm & (~0x0F)) == 0;
			sbyte val = unchecked((sbyte)(0xff & ((nrm << 4) | dv)));
			output.writeByte(val);
			output.writeStringStringMap(fi.attributes());
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			output.close();
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(output);
		  }
		}
	  }

	  private static sbyte DocValuesByte(DocValuesType type)
	  {
		if (type == null)
		{
		  return 0;
		}
		else if (type == DocValuesType.NUMERIC)
		{
		  return 1;
		}
		else if (type == DocValuesType.BINARY)
		{
		  return 2;
		}
		else if (type == DocValuesType.SORTED)
		{
		  return 3;
		}
		else if (type == DocValuesType.SORTED_SET)
		{
		  return 4;
		}
		else
		{
		  throw new AssertionError();
		}
	  }
	}

}