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


	using Lucene.Net.Analysis;
	using PayloadAttribute = Lucene.Net.Analysis.Tokenattributes.PayloadAttribute;
	using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using Before = org.junit.Before;

	/// <summary>
	/// this testcase tests whether multi-level skipping is being used
	/// to reduce I/O while skipping through posting lists.
	/// 
	/// Skipping in general is already covered by several other
	/// testcases.
	/// 
	/// </summary>
	public class TestMultiLevelSkipList : LuceneTestCase
	{

	  internal class CountingRAMDirectory : MockDirectoryWrapper
	  {
		  private readonly TestMultiLevelSkipList OuterInstance;

		public CountingRAMDirectory(TestMultiLevelSkipList outerInstance, Directory @delegate) : base(random(), @delegate)
		{
			this.OuterInstance = outerInstance;
		}

		public override IndexInput OpenInput(string fileName, IOContext context)
		{
		  IndexInput @in = base.openInput(fileName, context);
		  if (fileName.EndsWith(".frq"))
		  {
			@in = new CountingStream(OuterInstance, @in);
		  }
		  return @in;
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Override @Before public void setUp() throws Exception
	  public override void SetUp()
	  {
		base.setUp();
		Counter = 0;
	  }

	  public virtual void TestSimpleSkip()
	  {
		Directory dir = new CountingRAMDirectory(this, new RAMDirectory());
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new PayloadAnalyzer()).setCodec(TestUtil.alwaysPostingsFormat(new Lucene41PostingsFormat())).setMergePolicy(newLogMergePolicy()));
		Term term = new Term("test", "a");
		for (int i = 0; i < 5000; i++)
		{
		  Document d1 = new Document();
		  d1.add(newTextField(term.field(), term.text(), Field.Store.NO));
		  writer.addDocument(d1);
		}
		writer.commit();
		writer.forceMerge(1);
		writer.close();

		AtomicReader reader = getOnlySegmentReader(DirectoryReader.open(dir));

		for (int i = 0; i < 2; i++)
		{
		  Counter = 0;
		  DocsAndPositionsEnum tp = reader.termPositionsEnum(term);
		  CheckSkipTo(tp, 14, 185); // no skips
		  CheckSkipTo(tp, 17, 190); // one skip on level 0
		  CheckSkipTo(tp, 287, 200); // one skip on level 1, two on level 0

		  // this test would fail if we had only one skip level,
		  // because than more bytes would be read from the freqStream
		  CheckSkipTo(tp, 4800, 250); // one skip on level 2
		}
	  }

	  public virtual void CheckSkipTo(DocsAndPositionsEnum tp, int target, int maxCounter)
	  {
		tp.advance(target);
		if (maxCounter < Counter)
		{
		  Assert.Fail("Too many bytes read: " + Counter + " vs " + maxCounter);
		}

		Assert.AreEqual("Wrong document " + tp.docID() + " after skipTo target " + target, target, tp.docID());
		Assert.AreEqual("Frequency is not 1: " + tp.freq(), 1,tp.freq());
		tp.nextPosition();
		BytesRef b = tp.Payload;
		Assert.AreEqual(1, b.length);
		Assert.AreEqual("Wrong payload for the target " + target + ": " + b.bytes[b.offset], (sbyte) target, b.bytes[b.offset]);
	  }

	  private class PayloadAnalyzer : Analyzer
	  {
		internal readonly AtomicInteger PayloadCount = new AtomicInteger(-1);
		public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		{
		  Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
		  return new TokenStreamComponents(tokenizer, new PayloadFilter(PayloadCount, tokenizer));
		}

	  }

	  private class PayloadFilter : TokenFilter
	  {

		internal PayloadAttribute PayloadAtt;
		internal AtomicInteger PayloadCount;

		protected internal PayloadFilter(AtomicInteger payloadCount, TokenStream input) : base(input)
		{
		  this.PayloadCount = payloadCount;
		  PayloadAtt = addAttribute(typeof(PayloadAttribute));
		}

		public override bool IncrementToken()
		{
		  bool hasNext = input.IncrementToken();
		  if (hasNext)
		  {
			PayloadAtt.Payload = new BytesRef(new sbyte[] {(sbyte) PayloadCount.incrementAndGet()});
		  }
		  return hasNext;
		}

	  }

	  private int Counter = 0;

	  // Simply extends IndexInput in a way that we are able to count the number
	  // of bytes read
	  internal class CountingStream : IndexInput
	  {
		  private readonly TestMultiLevelSkipList OuterInstance;

		internal IndexInput Input;

		internal CountingStream(TestMultiLevelSkipList outerInstance, IndexInput input) : base("CountingStream(" + input + ")")
		{
			this.OuterInstance = outerInstance;
		  this.Input = input;
		}

		public override sbyte ReadByte()
		{
		  OuterInstance.Counter++;
		  return this.Input.readByte();
		}

		public override void ReadBytes(sbyte[] b, int offset, int len)
		{
		  OuterInstance.Counter += len;
		  this.Input.readBytes(b, offset, len);
		}

		public override void Close()
		{
		  this.Input.close();
		}

		public override long FilePointer
		{
			get
			{
			  return this.Input.FilePointer;
			}
		}

		public override void Seek(long pos)
		{
		  this.Input.seek(pos);
		}

		public override long Length()
		{
		  return this.Input.length();
		}

		public override CountingStream Clone()
		{
		  return new CountingStream(OuterInstance, this.Input.clone());
		}

	  }
	}

}