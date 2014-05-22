using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Index
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


	using FieldsConsumer = Lucene.Net.Codecs.FieldsConsumer;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using CollectionUtil = Lucene.Net.Util.CollectionUtil;
	using IOUtils = Lucene.Net.Util.IOUtils;

	internal sealed class FreqProxTermsWriter : TermsHashConsumer
	{

	  internal override void Abort()
	  // TODO: would be nice to factor out more of this, eg the
	  // FreqProxFieldMergeState, and code to visit all Fields
	  // under the same FieldInfo together, up into TermsHash*.
	  // Other writers would presumably share alot of this...
	  {
	  }
	  public override void Flush(IDictionary<string, TermsHashConsumerPerField> fieldsToFlush, SegmentWriteState state)
	  {

		// Gather all FieldData's that have postings, across all
		// ThreadStates
		IList<FreqProxTermsWriterPerField> allFields = new List<FreqProxTermsWriterPerField>();

		foreach (TermsHashConsumerPerField f in fieldsToFlush.Values)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FreqProxTermsWriterPerField perField = (FreqProxTermsWriterPerField) f;
		  FreqProxTermsWriterPerField perField = (FreqProxTermsWriterPerField) f;
		  if (perField.TermsHashPerField.bytesHash.size() > 0)
		  {
			allFields.Add(perField);
		  }
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numAllFields = allFields.size();
		int numAllFields = allFields.Count;

		// Sort by field name
		CollectionUtil.IntroSort(allFields);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Codecs.FieldsConsumer consumer = state.segmentInfo.getCodec().postingsFormat().fieldsConsumer(state);
		FieldsConsumer consumer = state.SegmentInfo.Codec.postingsFormat().fieldsConsumer(state);

		bool success = false;

		try
		{
		  TermsHash termsHash = null;

		  /*
		Current writer chain:
		  FieldsConsumer
		    -> IMPL: FormatPostingsTermsDictWriter
		      -> TermsConsumer
		        -> IMPL: FormatPostingsTermsDictWriter.TermsWriter
		          -> DocsConsumer
		            -> IMPL: FormatPostingsDocsWriter
		              -> PositionsConsumer
		                -> IMPL: FormatPostingsPositionsWriter
		   */

		  for (int fieldNumber = 0; fieldNumber < numAllFields; fieldNumber++)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FieldInfo fieldInfo = allFields.get(fieldNumber).fieldInfo;
			FieldInfo fieldInfo = allFields[fieldNumber].FieldInfo;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FreqProxTermsWriterPerField fieldWriter = allFields.get(fieldNumber);
			FreqProxTermsWriterPerField fieldWriter = allFields[fieldNumber];

			// If this field has postings then add them to the
			// segment
			fieldWriter.Flush(fieldInfo.Name, consumer, state);

			TermsHashPerField perField = fieldWriter.TermsHashPerField;
			Debug.Assert(termsHash == null || termsHash == perField.TermsHash);
			termsHash = perField.TermsHash;
			int numPostings = perField.BytesHash.size();
			perField.Reset();
			perField.ShrinkHash(numPostings);
			fieldWriter.Reset();
		  }

		  if (termsHash != null)
		  {
			termsHash.Reset();
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.Close(consumer);
		  }
		  else
		  {
			IOUtils.CloseWhileHandlingException(consumer);
		  }
		}
	  }

	  internal BytesRef Payload;

	  public override TermsHashConsumerPerField AddField(TermsHashPerField termsHashPerField, FieldInfo fieldInfo)
	  {
		return new FreqProxTermsWriterPerField(termsHashPerField, this, fieldInfo);
	  }

	  internal override void FinishDocument(TermsHash termsHash)
	  {
	  }

	  internal override void StartDocument()
	  {
	  }
	}

}