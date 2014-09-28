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
	using IndexOptions = org.apache.lucene.index.FieldInfo.IndexOptions;
	using SegmentReadState = org.apache.lucene.index.SegmentReadState;
	using SegmentWriteState = org.apache.lucene.index.SegmentWriteState;
	using IOUtils = org.apache.lucene.util.IOUtils;

	/// <summary>
	/// FST term dict + Lucene41PBF
	/// </summary>

	public sealed class FSTPostingsFormat : PostingsFormat
	{
	  public FSTPostingsFormat() : base("FST41")
	  {
	  }

	  public override string ToString()
	  {
		return Name;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.codecs.FieldsConsumer fieldsConsumer(org.apache.lucene.index.SegmentWriteState state) throws java.io.IOException
	  public override FieldsConsumer fieldsConsumer(SegmentWriteState state)
	  {
		PostingsWriterBase postingsWriter = new Lucene41PostingsWriter(state);

		bool success = false;
		try
		{
		  FieldsConsumer ret = new FSTTermsWriter(state, postingsWriter);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(postingsWriter);
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.codecs.FieldsProducer fieldsProducer(org.apache.lucene.index.SegmentReadState state) throws java.io.IOException
	  public override FieldsProducer fieldsProducer(SegmentReadState state)
	  {
		PostingsReaderBase postingsReader = new Lucene41PostingsReader(state.directory, state.fieldInfos, state.segmentInfo, state.context, state.segmentSuffix);
		bool success = false;
		try
		{
		  FieldsProducer ret = new FSTTermsReader(state, postingsReader);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(postingsReader);
		  }
		}
	  }
	}

}