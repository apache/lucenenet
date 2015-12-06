using System;
using System.Text;

namespace org.apache.lucene.analysis.hunspell
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


	using BytesRef = org.apache.lucene.util.BytesRef;
	using CharsRef = org.apache.lucene.util.CharsRef;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using IntsRef = org.apache.lucene.util.IntsRef;
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using Builder = org.apache.lucene.util.fst.Builder;
	using CharSequenceOutputs = org.apache.lucene.util.fst.CharSequenceOutputs;
	using FST = org.apache.lucene.util.fst.FST;
	using Outputs = org.apache.lucene.util.fst.Outputs;
	using Util = org.apache.lucene.util.fst.Util;

	public class TestDictionary : LuceneTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSimpleDictionary() throws Exception
	  public virtual void testSimpleDictionary()
	  {
		System.IO.Stream affixStream = this.GetType().getResourceAsStream("simple.aff");
		System.IO.Stream dictStream = this.GetType().getResourceAsStream("simple.dic");

		Dictionary dictionary = new Dictionary(affixStream, dictStream);
		assertEquals(3, dictionary.lookupSuffix(new char[]{'e'}, 0, 1).length);
		assertEquals(1, dictionary.lookupPrefix(new char[]{'s'}, 0, 1).length);
		IntsRef ordList = dictionary.lookupWord(new char[]{'o', 'l', 'r'}, 0, 3);
		assertNotNull(ordList);
		assertEquals(1, ordList.length);

		BytesRef @ref = new BytesRef();
		dictionary.flagLookup.get(ordList.ints[0], @ref);
		char[] flags = Dictionary.decodeFlags(@ref);
		assertEquals(1, flags.Length);

		ordList = dictionary.lookupWord(new char[]{'l', 'u', 'c', 'e', 'n'}, 0, 5);
		assertNotNull(ordList);
		assertEquals(1, ordList.length);
		dictionary.flagLookup.get(ordList.ints[0], @ref);
		flags = Dictionary.decodeFlags(@ref);
		assertEquals(1, flags.Length);

		affixStream.Close();
		dictStream.Close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCompressedDictionary() throws Exception
	  public virtual void testCompressedDictionary()
	  {
		System.IO.Stream affixStream = this.GetType().getResourceAsStream("compressed.aff");
		System.IO.Stream dictStream = this.GetType().getResourceAsStream("compressed.dic");

		Dictionary dictionary = new Dictionary(affixStream, dictStream);
		assertEquals(3, dictionary.lookupSuffix(new char[]{'e'}, 0, 1).length);
		assertEquals(1, dictionary.lookupPrefix(new char[]{'s'}, 0, 1).length);
		IntsRef ordList = dictionary.lookupWord(new char[]{'o', 'l', 'r'}, 0, 3);
		BytesRef @ref = new BytesRef();
		dictionary.flagLookup.get(ordList.ints[0], @ref);
		char[] flags = Dictionary.decodeFlags(@ref);
		assertEquals(1, flags.Length);

		affixStream.Close();
		dictStream.Close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCompressedBeforeSetDictionary() throws Exception
	  public virtual void testCompressedBeforeSetDictionary()
	  {
		System.IO.Stream affixStream = this.GetType().getResourceAsStream("compressed-before-set.aff");
		System.IO.Stream dictStream = this.GetType().getResourceAsStream("compressed.dic");

		Dictionary dictionary = new Dictionary(affixStream, dictStream);
		assertEquals(3, dictionary.lookupSuffix(new char[]{'e'}, 0, 1).length);
		assertEquals(1, dictionary.lookupPrefix(new char[]{'s'}, 0, 1).length);
		IntsRef ordList = dictionary.lookupWord(new char[]{'o', 'l', 'r'}, 0, 3);
		BytesRef @ref = new BytesRef();
		dictionary.flagLookup.get(ordList.ints[0], @ref);
		char[] flags = Dictionary.decodeFlags(@ref);
		assertEquals(1, flags.Length);

		affixStream.Close();
		dictStream.Close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCompressedEmptyAliasDictionary() throws Exception
	  public virtual void testCompressedEmptyAliasDictionary()
	  {
		System.IO.Stream affixStream = this.GetType().getResourceAsStream("compressed-empty-alias.aff");
		System.IO.Stream dictStream = this.GetType().getResourceAsStream("compressed.dic");

		Dictionary dictionary = new Dictionary(affixStream, dictStream);
		assertEquals(3, dictionary.lookupSuffix(new char[]{'e'}, 0, 1).length);
		assertEquals(1, dictionary.lookupPrefix(new char[]{'s'}, 0, 1).length);
		IntsRef ordList = dictionary.lookupWord(new char[]{'o', 'l', 'r'}, 0, 3);
		BytesRef @ref = new BytesRef();
		dictionary.flagLookup.get(ordList.ints[0], @ref);
		char[] flags = Dictionary.decodeFlags(@ref);
		assertEquals(1, flags.Length);

		affixStream.Close();
		dictStream.Close();
	  }

	  // malformed rule causes ParseException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInvalidData() throws Exception
	  public virtual void testInvalidData()
	  {
		System.IO.Stream affixStream = this.GetType().getResourceAsStream("broken.aff");
		System.IO.Stream dictStream = this.GetType().getResourceAsStream("simple.dic");

		try
		{
		  new Dictionary(affixStream, dictStream);
		  fail("didn't get expected exception");
		}
		catch (ParseException expected)
		{
		  assertTrue(expected.Message.startsWith("The affix file contains a rule with less than four elements"));
		  assertEquals(24, expected.ErrorOffset);
		}

		affixStream.Close();
		dictStream.Close();
	  }

	  // malformed flags causes ParseException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInvalidFlags() throws Exception
	  public virtual void testInvalidFlags()
	  {
		System.IO.Stream affixStream = this.GetType().getResourceAsStream("broken-flags.aff");
		System.IO.Stream dictStream = this.GetType().getResourceAsStream("simple.dic");

		try
		{
		  new Dictionary(affixStream, dictStream);
		  fail("didn't get expected exception");
		}
		catch (Exception expected)
		{
		  assertTrue(expected.Message.startsWith("expected only one flag"));
		}

		affixStream.Close();
		dictStream.Close();
	  }

	  private class CloseCheckInputStream : FilterInputStream
	  {
		  private readonly TestDictionary outerInstance;

		internal bool closed = false;

		public CloseCheckInputStream(TestDictionary outerInstance, System.IO.Stream @delegate) : base(@delegate)
		{
			this.outerInstance = outerInstance;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
		public override void close()
		{
		  this.closed = true;
		  base.close();
		}

		public virtual bool Closed
		{
			get
			{
			  return this.closed;
			}
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testResourceCleanup() throws Exception
	  public virtual void testResourceCleanup()
	  {
		CloseCheckInputStream affixStream = new CloseCheckInputStream(this, this.GetType().getResourceAsStream("compressed.aff"));
		CloseCheckInputStream dictStream = new CloseCheckInputStream(this, this.GetType().getResourceAsStream("compressed.dic"));

		new Dictionary(affixStream, dictStream);

		assertFalse(affixStream.Closed);
		assertFalse(dictStream.Closed);

		affixStream.close();
		dictStream.close();

		assertTrue(affixStream.Closed);
		assertTrue(dictStream.Closed);
	  }



//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReplacements() throws Exception
	  public virtual void testReplacements()
	  {
		Outputs<CharsRef> outputs = CharSequenceOutputs.Singleton;
		Builder<CharsRef> builder = new Builder<CharsRef>(FST.INPUT_TYPE.BYTE2, outputs);
		IntsRef scratchInts = new IntsRef();

		// a -> b
		Util.toUTF16("a", scratchInts);
		builder.add(scratchInts, new CharsRef("b"));

		// ab -> c
		Util.toUTF16("ab", scratchInts);
		builder.add(scratchInts, new CharsRef("c"));

		// c -> de
		Util.toUTF16("c", scratchInts);
		builder.add(scratchInts, new CharsRef("de"));

		// def -> gh
		Util.toUTF16("def", scratchInts);
		builder.add(scratchInts, new CharsRef("gh"));

		FST<CharsRef> fst = builder.finish();

		StringBuilder sb = new StringBuilder("atestanother");
		Dictionary.applyMappings(fst, sb);
		assertEquals("btestbnother", sb.ToString());

		sb = new StringBuilder("abtestanother");
		Dictionary.applyMappings(fst, sb);
		assertEquals("ctestbnother", sb.ToString());

		sb = new StringBuilder("atestabnother");
		Dictionary.applyMappings(fst, sb);
		assertEquals("btestcnother", sb.ToString());

		sb = new StringBuilder("abtestabnother");
		Dictionary.applyMappings(fst, sb);
		assertEquals("ctestcnother", sb.ToString());

		sb = new StringBuilder("abtestabcnother");
		Dictionary.applyMappings(fst, sb);
		assertEquals("ctestcdenother", sb.ToString());

		sb = new StringBuilder("defdefdefc");
		Dictionary.applyMappings(fst, sb);
		assertEquals("ghghghde", sb.ToString());
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSetWithCrazyWhitespaceAndBOMs() throws Exception
	  public virtual void testSetWithCrazyWhitespaceAndBOMs()
	  {
		assertEquals("UTF-8", Dictionary.getDictionaryEncoding(new ByteArrayInputStream("SET\tUTF-8\n".GetBytes(StandardCharsets.UTF_8))));
		assertEquals("UTF-8", Dictionary.getDictionaryEncoding(new ByteArrayInputStream("SET\t UTF-8\n".GetBytes(StandardCharsets.UTF_8))));
		assertEquals("UTF-8", Dictionary.getDictionaryEncoding(new ByteArrayInputStream("\uFEFFSET\tUTF-8\n".GetBytes(StandardCharsets.UTF_8))));
		assertEquals("UTF-8", Dictionary.getDictionaryEncoding(new ByteArrayInputStream("\uFEFFSET\tUTF-8\r\n".GetBytes(StandardCharsets.UTF_8))));
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFlagWithCrazyWhitespace() throws Exception
	  public virtual void testFlagWithCrazyWhitespace()
	  {
		assertNotNull(Dictionary.getFlagParsingStrategy("FLAG\tUTF-8"));
		assertNotNull(Dictionary.getFlagParsingStrategy("FLAG    UTF-8"));
	  }
	}

}