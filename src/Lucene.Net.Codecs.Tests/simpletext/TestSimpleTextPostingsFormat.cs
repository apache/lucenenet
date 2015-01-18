namespace org.apache.lucene.codecs.simpletext
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

	using BasePostingsFormatTestCase = org.apache.lucene.index.BasePostingsFormatTestCase;
	using Nightly = org.apache.lucene.util.LuceneTestCase.Nightly;

	/// <summary>
	/// Tests SimpleText's postings
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Nightly public class TestSimpleTextPostingsFormat extends org.apache.lucene.index.BasePostingsFormatTestCase
	public class TestSimpleTextPostingsFormat : BasePostingsFormatTestCase // please figure out why I am so horrendously slow!
	{
	  private readonly Codec codec = new SimpleTextCodec();

	  protected internal override Codec Codec
	  {
		  get
		  {
			return codec;
		  }
	  }
	}

}