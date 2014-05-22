namespace Lucene.Net.Codecs.mocksep
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

	using BlockTermsReader = Lucene.Net.Codecs.blockterms.BlockTermsReader;
	using BlockTermsWriter = Lucene.Net.Codecs.blockterms.BlockTermsWriter;
	using FixedGapTermsIndexReader = Lucene.Net.Codecs.blockterms.FixedGapTermsIndexReader;
	using FixedGapTermsIndexWriter = Lucene.Net.Codecs.blockterms.FixedGapTermsIndexWriter;
	using TermsIndexReaderBase = Lucene.Net.Codecs.blockterms.TermsIndexReaderBase;
	using TermsIndexWriterBase = Lucene.Net.Codecs.blockterms.TermsIndexWriterBase;
	using SepPostingsReader = Lucene.Net.Codecs.sep.SepPostingsReader;
	using SepPostingsWriter = Lucene.Net.Codecs.sep.SepPostingsWriter;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using SegmentReadState = Lucene.Net.Index.SegmentReadState;
	using BytesRef = Lucene.Net.Util.BytesRef;

	/// <summary>
	/// A silly codec that simply writes each file separately as
	/// single vInts.  Don't use this (performance will be poor)!
	/// this is here just to test the core sep codec
	/// classes.
	/// </summary>
	public sealed class MockSepPostingsFormat : PostingsFormat
	{

	  public MockSepPostingsFormat() : base("MockSep")
	  {
	  }

	  public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
	  {

		PostingsWriterBase postingsWriter = new SepPostingsWriter(state, new MockSingleIntFactory());

		bool success = false;
		TermsIndexWriterBase indexWriter;
		try
		{
		  indexWriter = new FixedGapTermsIndexWriter(state);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			postingsWriter.close();
		  }
		}

		success = false;
		try
		{
		  FieldsConsumer ret = new BlockTermsWriter(indexWriter, state, postingsWriter);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			try
			{
			  postingsWriter.close();
			}
			finally
			{
			  indexWriter.close();
			}
		  }
		}
	  }

	  public override FieldsProducer FieldsProducer(SegmentReadState state)
	  {

		PostingsReaderBase postingsReader = new SepPostingsReader(state.directory, state.fieldInfos, state.segmentInfo, state.context, new MockSingleIntFactory(), state.segmentSuffix);

		TermsIndexReaderBase indexReader;
		bool success = false;
		try
		{
		  indexReader = new FixedGapTermsIndexReader(state.directory, state.fieldInfos, state.segmentInfo.name, state.termsIndexDivisor, BytesRef.UTF8SortedAsUnicodeComparator, state.segmentSuffix, state.context);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			postingsReader.close();
		  }
		}

		success = false;
		try
		{
		  FieldsProducer ret = new BlockTermsReader(indexReader, state.directory, state.fieldInfos, state.segmentInfo, postingsReader, state.context, state.segmentSuffix);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			try
			{
			  postingsReader.close();
			}
			finally
			{
			  indexReader.close();
			}
		  }
		}
	  }
	}

}