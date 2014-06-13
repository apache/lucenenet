namespace Lucene.Net.Codecs.Lucene41ords
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
	using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat; // javadocs
	using Lucene41PostingsReader = Lucene.Net.Codecs.Lucene41.Lucene41PostingsReader;
	using Lucene41PostingsWriter = Lucene.Net.Codecs.Lucene41.Lucene41PostingsWriter;
	using SegmentReadState = Lucene.Net.Index.SegmentReadState;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using BytesRef = Lucene.Net.Util.BytesRef;

	// TODO: we could make separate base class that can wrapp
	// any PostingsBaseFormat and make it ord-able...

	/// <summary>
	/// Customized version of <seealso cref="Lucene41PostingsFormat"/> that uses
	/// <seealso cref="FixedGapTermsIndexWriter"/>.
	/// </summary>
	public sealed class Lucene41WithOrds : PostingsFormat
	{

	  public Lucene41WithOrds() : base("Lucene41WithOrds")
	  {
	  }

	  public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
	  {
		PostingsWriterBase docs = new Lucene41PostingsWriter(state);

		// TODO: should we make the terms index more easily
		// pluggable?  Ie so that this codec would record which
		// index impl was used, and switch on loading?
		// Or... you must make a new Codec for this?
		TermsIndexWriterBase indexWriter;
		bool success = false;
		try
		{
		  indexWriter = new FixedGapTermsIndexWriter(state);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			docs.close();
		  }
		}

		success = false;
		try
		{
		  // Must use BlockTermsWriter (not BlockTree) because
		  // BlockTree doens't support ords (yet)...
		  FieldsConsumer ret = new BlockTermsWriter(indexWriter, state, docs);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			try
			{
			  docs.close();
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
		PostingsReaderBase postings = new Lucene41PostingsReader(state.Directory, state.fieldInfos, state.SegmentInfo, state.Context, state.SegmentSuffix);
		TermsIndexReaderBase indexReader;

		bool success = false;
		try
		{
		  indexReader = new FixedGapTermsIndexReader(state.Directory, state.fieldInfos, state.SegmentInfo.Name, state.termsIndexDivisor, BytesRef.UTF8SortedAsUnicodeComparator, state.SegmentSuffix, state.Context);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			postings.close();
		  }
		}

		success = false;
		try
		{
		  FieldsProducer ret = new BlockTermsReader(indexReader, state.Directory, state.fieldInfos, state.SegmentInfo, postings, state.Context, state.SegmentSuffix);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			try
			{
			  postings.close();
			}
			finally
			{
			  indexReader.close();
			}
		  }
		}
	  }

	  /// <summary>
	  /// Extension of freq postings file </summary>
	  internal const string FREQ_EXTENSION = "frq";

	  /// <summary>
	  /// Extension of prox postings file </summary>
	  internal const string PROX_EXTENSION = "prx";
	}

}