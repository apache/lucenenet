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

	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using OffsetAttribute = Lucene.Net.Analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = Lucene.Net.Analysis.tokenattributes.PositionIncrementAttribute;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// Holds state for inverting all occurrences of a single
	/// field in the document.  this class doesn't do anything
	/// itself; instead, it forwards the tokens produced by
	/// analysis to its own consumer
	/// (InvertedDocConsumerPerField).  It also interacts with an
	/// endConsumer (InvertedDocEndConsumerPerField).
	/// </summary>

	internal sealed class DocInverterPerField : DocFieldConsumerPerField
	{

	  internal readonly FieldInfo FieldInfo_Renamed;
	  internal readonly InvertedDocConsumerPerField Consumer;
	  internal readonly InvertedDocEndConsumerPerField EndConsumer;
	  internal readonly DocumentsWriterPerThread.DocState DocState;
	  internal readonly FieldInvertState FieldState;

	  public DocInverterPerField(DocInverter parent, FieldInfo fieldInfo)
	  {
		this.FieldInfo_Renamed = fieldInfo;
		DocState = parent.DocState;
		FieldState = new FieldInvertState(fieldInfo.Name);
		this.Consumer = parent.Consumer.addField(this, fieldInfo);
		this.EndConsumer = parent.EndConsumer.addField(this, fieldInfo);
	  }

	  internal override void Abort()
	  {
		try
		{
		  Consumer.Abort();
		}
		finally
		{
		  EndConsumer.Abort();
		}
	  }

	  public override void ProcessFields(IndexableField[] fields, int count)
	  {

		FieldState.Reset();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean doInvert = consumer.start(fields, count);
		bool doInvert = Consumer.Start(fields, count);

		for (int i = 0;i < count;i++)
		{

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final IndexableField field = fields[i];
		  IndexableField field = fields[i];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final IndexableFieldType fieldType = field.fieldType();
		  IndexableFieldType fieldType = field.FieldType();

		  // TODO FI: this should be "genericized" to querying
		  // consumer if it wants to see this particular field
		  // tokenized.
		  if (fieldType.Indexed() && doInvert)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean analyzed = fieldType.tokenized() && docState.analyzer != null;
			bool analyzed = fieldType.Tokenized() && DocState.Analyzer != null;

			// if the field omits norms, the boost cannot be indexed.
			if (fieldType.OmitNorms() && field.Boost() != 1.0f)
			{
			  throw new System.NotSupportedException("You cannot set an index-time boost: norms are omitted for field '" + field.Name() + "'");
			}

			// only bother checking offsets if something will consume them.
			// TODO: after we fix analyzers, also check if termVectorOffsets will be indexed.
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean checkOffsets = fieldType.indexOptions() == Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
			bool checkOffsets = fieldType.IndexOptions() == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
			int lastStartOffset = 0;

			if (i > 0)
			{
			  FieldState.Position_Renamed += analyzed ? DocState.Analyzer.getPositionIncrementGap(FieldInfo_Renamed.Name) : 0;
			}

			 /*
			* To assist people in tracking down problems in analysis components, we wish to write the field name to the infostream
			* when we fail. We expect some caller to eventually deal with the real exception, so we don't want any 'catch' clauses,
			* but rather a finally that takes note of the problem.
			*/

			bool succeededInProcessingField = false;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Analysis.TokenStream stream = field.tokenStream(docState.analyzer);
			TokenStream stream = field.TokenStream(DocState.Analyzer);
			// reset the TokenStream to the first token
			stream.Reset();

			try
			{
			  bool hasMoreTokens = stream.IncrementToken();

			  FieldState.AttributeSource_Renamed = stream;

			  OffsetAttribute offsetAttribute = FieldState.AttributeSource_Renamed.addAttribute(typeof(OffsetAttribute));
			  PositionIncrementAttribute posIncrAttribute = FieldState.AttributeSource_Renamed.addAttribute(typeof(PositionIncrementAttribute));

			  if (hasMoreTokens)
			  {
				Consumer.Start(field);

				do
				{
				  // If we hit an exception in stream.next below
				  // (which is fairly common, eg if analyzer
				  // chokes on a given document), then it's
				  // non-aborting and (above) this one document
				  // will be marked as deleted, but still
				  // consume a docID

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int posIncr = posIncrAttribute.getPositionIncrement();
				  int posIncr = posIncrAttribute.PositionIncrement;
				  if (posIncr < 0)
				  {
					throw new System.ArgumentException("position increment must be >=0 (got " + posIncr + ") for field '" + field.Name() + "'");
				  }
				  if (FieldState.Position_Renamed == 0 && posIncr == 0)
				  {
					throw new System.ArgumentException("first position increment must be > 0 (got 0) for field '" + field.Name() + "'");
				  }
				  int position = FieldState.Position_Renamed + posIncr;
				  if (position > 0)
				  {
					// NOTE: confusing: this "mirrors" the
					// position++ we do below
					position--;
				  }
				  else if (position < 0)
				  {
					throw new System.ArgumentException("position overflow for field '" + field.Name() + "'");
				  }

				  // position is legal, we can safely place it in fieldState now.
				  // not sure if anything will use fieldState after non-aborting exc...
				  FieldState.Position_Renamed = position;

				  if (posIncr == 0)
				  {
					FieldState.NumOverlap_Renamed++;
				  }

				  if (checkOffsets)
				  {
					int startOffset = FieldState.Offset_Renamed + offsetAttribute.StartOffset();
					int endOffset = FieldState.Offset_Renamed + offsetAttribute.EndOffset();
					if (startOffset < 0 || endOffset < startOffset)
					{
					  throw new System.ArgumentException("startOffset must be non-negative, and endOffset must be >= startOffset, " + "startOffset=" + startOffset + ",endOffset=" + endOffset + " for field '" + field.Name() + "'");
					}
					if (startOffset < lastStartOffset)
					{
					  throw new System.ArgumentException("offsets must not go backwards startOffset=" + startOffset + " is < lastStartOffset=" + lastStartOffset + " for field '" + field.Name() + "'");
					}
					lastStartOffset = startOffset;
				  }

				  bool success = false;
				  try
				  {
					// If we hit an exception in here, we abort
					// all buffered documents since the last
					// flush, on the likelihood that the
					// internal state of the consumer is now
					// corrupt and should not be flushed to a
					// new segment:
					Consumer.Add();
					success = true;

				  }
				  finally
				  {
					if (!success)
					{
					  DocState.DocWriter.setAborting();
					}
				  }
				  FieldState.Length_Renamed++;
				  FieldState.Position_Renamed++;

				} while (stream.IncrementToken());
			  }
			  // trigger streams to perform end-of-stream operations
			  stream.End();
			  // TODO: maybe add some safety? then again, its already checked 
			  // when we come back around to the field...
			  FieldState.Position_Renamed += posIncrAttribute.PositionIncrement;
			  FieldState.Offset_Renamed += offsetAttribute.EndOffset();


			  if (DocState.MaxTermPrefix != null)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String msg = "Document contains at least one immense term in field=\"" + fieldInfo.name + "\" (whose UTF8 encoding is longer than the max length " + DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8 + "), all of which were skipped.  Please correct the analyzer to not produce such terms.  The prefix of the first immense term is: '" + docState.maxTermPrefix + "...'";
				string msg = "Document contains at least one immense term in field=\"" + FieldInfo_Renamed.Name + "\" (whose UTF8 encoding is longer than the max length " + DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8 + "), all of which were skipped.  Please correct the analyzer to not produce such terms.  The prefix of the first immense term is: '" + DocState.MaxTermPrefix + "...'";
				if (DocState.InfoStream.isEnabled("IW"))
				{
				  DocState.InfoStream.message("IW", "ERROR: " + msg);
				}
				DocState.MaxTermPrefix = null;
				throw new System.ArgumentException(msg);
			  }

			  /* if success was false above there is an exception coming through and we won't get here.*/
			  succeededInProcessingField = true;
			}
			finally
			{
			  if (!succeededInProcessingField)
			  {
				IOUtils.CloseWhileHandlingException(stream);
			  }
			  else
			  {
				stream.Close();
			  }
			  if (!succeededInProcessingField && DocState.InfoStream.isEnabled("DW"))
			  {
				DocState.InfoStream.message("DW", "An exception was thrown while processing field " + FieldInfo_Renamed.Name);
			  }
			}

			FieldState.Offset_Renamed += analyzed ? DocState.Analyzer.getOffsetGap(FieldInfo_Renamed.Name) : 0;
			FieldState.Boost_Renamed *= field.Boost();
		  }

		  // LUCENE-2387: don't hang onto the field, so GC can
		  // reclaim
		  fields[i] = null;
		}

		Consumer.Finish();
		EndConsumer.Finish();
	  }

	  internal override FieldInfo FieldInfo
	  {
		  get
		  {
			return FieldInfo_Renamed;
		  }
	  }
	}

}