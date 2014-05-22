using System;
using System.Diagnostics;

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

	using LegacyDocValuesType = Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosReader.LegacyDocValuesType;
	using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType_e;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// Lucene 4.0 FieldInfos writer.
	/// </summary>
	/// <seealso cref= Lucene40FieldInfosFormat
	/// @lucene.experimental </seealso>
	[Obsolete]
	public class Lucene40FieldInfosWriter : FieldInfosWriter
	{

	  /// <summary>
	  /// Sole constructor. </summary>
	  public Lucene40FieldInfosWriter()
	  {
	  }

	  public override void Write(Directory directory, string segmentName, string segmentSuffix, FieldInfos infos, IOContext context)
	  {
		string fileName = IndexFileNames.segmentFileName(segmentName, "", Lucene40FieldInfosFormat.FIELD_INFOS_EXTENSION);
		IndexOutput output = directory.createOutput(fileName, context);
		bool success = false;
		try
		{
		  CodecUtil.writeHeader(output, Lucene40FieldInfosFormat.CODEC_NAME, Lucene40FieldInfosFormat.FORMAT_CURRENT);
		  output.writeVInt(infos.size());
		  foreach (FieldInfo fi in infos)
		  {
			IndexOptions indexOptions = fi.IndexOptions;
			sbyte bits = 0x0;
			if (fi.hasVectors())
			{
				bits |= Lucene40FieldInfosFormat.STORE_TERMVECTOR;
			}
			if (fi.omitsNorms())
			{
				bits |= Lucene40FieldInfosFormat.OMIT_NORMS;
			}
			if (fi.hasPayloads())
			{
				bits |= Lucene40FieldInfosFormat.STORE_PAYLOADS;
			}
			if (fi.Indexed)
			{
			  bits |= Lucene40FieldInfosFormat.IS_INDEXED;
			  Debug.Assert(indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !fi.hasPayloads());
			  if (indexOptions == IndexOptions.DOCS_ONLY)
			  {
				bits |= Lucene40FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS;
			  }
			  else if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
			  {
				bits |= Lucene40FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS;
			  }
			  else if (indexOptions == IndexOptions.DOCS_AND_FREQS)
			  {
				bits |= Lucene40FieldInfosFormat.OMIT_POSITIONS;
			  }
			}
			output.writeString(fi.name);
			output.writeVInt(fi.number);
			output.writeByte(bits);

			// pack the DV types in one byte
			sbyte dv = DocValuesByte(fi.DocValuesType_e, fi.getAttribute(Lucene40FieldInfosReader.LEGACY_DV_TYPE_KEY));
			sbyte nrm = DocValuesByte(fi.NormType, fi.getAttribute(Lucene40FieldInfosReader.LEGACY_NORM_TYPE_KEY));
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

	  /// <summary>
	  /// 4.0-style docvalues byte </summary>
	  public virtual sbyte DocValuesByte(DocValuesType type, string legacyTypeAtt)
	  {
		if (type == null)
		{
		  Debug.Assert(legacyTypeAtt == null);
		  return 0;
		}
		else
		{
		  Debug.Assert(legacyTypeAtt != null);
		  return (sbyte) LegacyDocValuesType.valueOf(legacyTypeAtt).ordinal();
		}
	  }
	}

}