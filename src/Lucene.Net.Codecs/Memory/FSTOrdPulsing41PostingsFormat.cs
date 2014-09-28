namespace org.apache.lucene.codecs.memory
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

	using Lucene41PostingsWriter = org.apache.lucene.codecs.lucene41.Lucene41PostingsWriter;
	using Lucene41PostingsReader = org.apache.lucene.codecs.lucene41.Lucene41PostingsReader;
	using Lucene41PostingsBaseFormat = org.apache.lucene.codecs.lucene41.Lucene41PostingsBaseFormat;
	using Lucene41PostingsFormat = org.apache.lucene.codecs.lucene41.Lucene41PostingsFormat;
	using PulsingPostingsWriter = org.apache.lucene.codecs.pulsing.PulsingPostingsWriter;
	using PulsingPostingsReader = org.apache.lucene.codecs.pulsing.PulsingPostingsReader;
	using SegmentReadState = org.apache.lucene.index.SegmentReadState;
	using SegmentWriteState = org.apache.lucene.index.SegmentWriteState;
	using IOUtils = org.apache.lucene.util.IOUtils;

	/// <summary>
	/// FSTOrd + Pulsing41
	///  @lucene.experimental 
	/// </summary>

	public class FSTOrdPulsing41PostingsFormat : PostingsFormat
	{
	  private readonly PostingsBaseFormat wrappedPostingsBaseFormat;
	  private readonly int freqCutoff;

	  public FSTOrdPulsing41PostingsFormat() : this(1)
	  {
	  }

	  public FSTOrdPulsing41PostingsFormat(int freqCutoff) : base("FSTOrdPulsing41")
	  {
		this.wrappedPostingsBaseFormat = new Lucene41PostingsBaseFormat();
		this.freqCutoff = freqCutoff;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.codecs.FieldsConsumer fieldsConsumer(org.apache.lucene.index.SegmentWriteState state) throws java.io.IOException
	  public override FieldsConsumer fieldsConsumer(SegmentWriteState state)
	  {
		PostingsWriterBase docsWriter = null;
		PostingsWriterBase pulsingWriter = null;

		bool success = false;
		try
		{
		  docsWriter = wrappedPostingsBaseFormat.postingsWriterBase(state);
		  pulsingWriter = new PulsingPostingsWriter(state, freqCutoff, docsWriter);
		  FieldsConsumer ret = new FSTOrdTermsWriter(state, pulsingWriter);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(docsWriter, pulsingWriter);
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.codecs.FieldsProducer fieldsProducer(org.apache.lucene.index.SegmentReadState state) throws java.io.IOException
	  public override FieldsProducer fieldsProducer(SegmentReadState state)
	  {
		PostingsReaderBase docsReader = null;
		PostingsReaderBase pulsingReader = null;
		bool success = false;
		try
		{
		  docsReader = wrappedPostingsBaseFormat.postingsReaderBase(state);
		  pulsingReader = new PulsingPostingsReader(state, docsReader);
		  FieldsProducer ret = new FSTOrdTermsReader(state, pulsingReader);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(docsReader, pulsingReader);
		  }
		}
	  }
	}

}