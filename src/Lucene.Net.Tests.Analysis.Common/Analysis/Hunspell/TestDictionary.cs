using System;
using System.Text;
using NUnit.Framework;
using Lucene.Net.Util;
using System.IO;
using Lucene.Net.Util.Fst;
using Lucene.Net.Support;

namespace Lucene.Net.Analysis.Hunspell
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

    public class TestDictionary : LuceneTestCase
    {

        [Test]
        public virtual void TestSimpleDictionary()
        {
            using (System.IO.Stream affixStream = this.GetType().getResourceAsStream("simple.aff"))
            {
                using (System.IO.Stream dictStream = this.GetType().getResourceAsStream("simple.dic"))
                {

                    Dictionary dictionary = new Dictionary(affixStream, dictStream);
                    assertEquals(3, dictionary.LookupSuffix(new char[] { 'e' }, 0, 1).Length);
                    assertEquals(1, dictionary.LookupPrefix(new char[] { 's' }, 0, 1).Length);
                    IntsRef ordList = dictionary.LookupWord(new char[] { 'o', 'l', 'r' }, 0, 3);
                    assertNotNull(ordList);
                    assertEquals(1, ordList.Length);

                    BytesRef @ref = new BytesRef();
                    dictionary.flagLookup.Get(ordList.Ints[0], @ref);
                    char[] flags = Dictionary.DecodeFlags(@ref);
                    assertEquals(1, flags.Length);

                    ordList = dictionary.LookupWord(new char[] { 'l', 'u', 'c', 'e', 'n' }, 0, 5);
                    assertNotNull(ordList);
                    assertEquals(1, ordList.Length);
                    dictionary.flagLookup.Get(ordList.Ints[0], @ref);
                    flags = Dictionary.DecodeFlags(@ref);
                    assertEquals(1, flags.Length);
                }
            }
        }

        [Test]
        public virtual void TestCompressedDictionary()
        {
            using (System.IO.Stream affixStream = this.GetType().getResourceAsStream("compressed.aff"))
            {
                using (System.IO.Stream dictStream = this.GetType().getResourceAsStream("compressed.dic"))
                {

                    Dictionary dictionary = new Dictionary(affixStream, dictStream);
                    assertEquals(3, dictionary.LookupSuffix(new char[] { 'e' }, 0, 1).Length);
                    assertEquals(1, dictionary.LookupPrefix(new char[] { 's' }, 0, 1).Length);
                    IntsRef ordList = dictionary.LookupWord(new char[] { 'o', 'l', 'r' }, 0, 3);
                    BytesRef @ref = new BytesRef();
                    dictionary.flagLookup.Get(ordList.Ints[0], @ref);
                    char[] flags = Dictionary.DecodeFlags(@ref);
                    assertEquals(1, flags.Length);
                }
            }
        }

        [Test]
        public virtual void TestCompressedBeforeSetDictionary()
        {
            using (System.IO.Stream affixStream = this.GetType().getResourceAsStream("compressed-before-set.aff"))
            {
                using (System.IO.Stream dictStream = this.GetType().getResourceAsStream("compressed.dic"))
                {

                    Dictionary dictionary = new Dictionary(affixStream, dictStream);
                    assertEquals(3, dictionary.LookupSuffix(new char[] { 'e' }, 0, 1).Length);
                    assertEquals(1, dictionary.LookupPrefix(new char[] { 's' }, 0, 1).Length);
                    IntsRef ordList = dictionary.LookupWord(new char[] { 'o', 'l', 'r' }, 0, 3);
                    BytesRef @ref = new BytesRef();
                    dictionary.flagLookup.Get(ordList.Ints[0], @ref);
                    char[] flags = Dictionary.DecodeFlags(@ref);
                    assertEquals(1, flags.Length);
                }
            }
        }

        [Test]
        public virtual void TestCompressedEmptyAliasDictionary()
        {
            using (System.IO.Stream affixStream = this.GetType().getResourceAsStream("compressed-empty-alias.aff"))
            {
                using (System.IO.Stream dictStream = this.GetType().getResourceAsStream("compressed.dic"))
                {
                    Dictionary dictionary = new Dictionary(affixStream, dictStream);
                    assertEquals(3, dictionary.LookupSuffix(new char[] { 'e' }, 0, 1).Length);
                    assertEquals(1, dictionary.LookupPrefix(new char[] { 's' }, 0, 1).Length);
                    IntsRef ordList = dictionary.LookupWord(new char[] { 'o', 'l', 'r' }, 0, 3);
                    BytesRef @ref = new BytesRef();
                    dictionary.flagLookup.Get(ordList.Ints[0], @ref);
                    char[] flags = Dictionary.DecodeFlags(@ref);
                    assertEquals(1, flags.Length);
                }
            }
        }

        // malformed rule causes ParseException
        [Test]
        public virtual void TestInvalidData()
        {
            using (System.IO.Stream affixStream = this.GetType().getResourceAsStream("broken.aff"))
            {
                using (System.IO.Stream dictStream = this.GetType().getResourceAsStream("simple.dic"))
                {

                    try
                    {
                        new Dictionary(affixStream, dictStream);
                        fail("didn't get expected exception");
                    }
                    catch (Exception expected)
                    {
                        assertTrue(expected.Message.StartsWith("The affix file contains a rule with less than four elements"));
                        //assertEquals(24, expected.ErrorOffset); // No parse exception in LUCENENET
                    }
                }
            }
        }

        // malformed flags causes ParseException
        [Test]
        public virtual void TestInvalidFlags()
        {
            using (System.IO.Stream affixStream = this.GetType().getResourceAsStream("broken-flags.aff"))
            {
                using (System.IO.Stream dictStream = this.GetType().getResourceAsStream("simple.dic"))
                {
                    try
                    {
                        new Dictionary(affixStream, dictStream);
                        fail("didn't get expected exception");
                    }
                    catch (Exception expected)
                    {
                        assertTrue(expected.Message.StartsWith("expected only one flag"));
                    }
                }
            }
        }

        private class CloseCheckInputStream : Stream, IDisposable
        {
            private readonly TestDictionary outerInstance;
            private readonly Stream @delegate;

            internal bool disposed = false;

            public override bool CanRead
            {
                get
                {
                    return @delegate.CanRead;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return @delegate.CanSeek;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return @delegate.CanWrite;
                }
            }

            public override long Length
            {
                get
                {
                    return @delegate.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return @delegate.Position;
                }

                set
                {
                    @delegate.Position = value;
                }
            }

            public CloseCheckInputStream(TestDictionary outerInstance, System.IO.Stream @delegate) 
            {
                this.@delegate = @delegate;
                this.outerInstance = outerInstance;
            }

            protected override void Dispose(bool disposing)
            {
                @delegate.Dispose();
            }


            new public void Dispose()
            {
                this.disposed = true;
                base.Dispose();
            }
            

            public virtual bool Disposed
            {
                get { return this.disposed; }
            }

            public override void Flush()
            {
                @delegate.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return @delegate.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                @delegate.SetLength(value);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return @delegate.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                @delegate.Write(buffer, offset, count);
            }
        }

        [Test]
        public virtual void TestResourceCleanup()
        {
            CloseCheckInputStream affixStream = new CloseCheckInputStream(this, this.GetType().getResourceAsStream("compressed.aff"));
            CloseCheckInputStream dictStream = new CloseCheckInputStream(this, this.GetType().getResourceAsStream("compressed.dic"));

            new Dictionary(affixStream, dictStream);

            assertFalse(affixStream.Disposed);
            assertFalse(dictStream.Disposed);

            affixStream.Dispose();
            dictStream.Dispose();

            assertTrue(affixStream.Disposed);
            assertTrue(dictStream.Disposed);
        }



        [Test]
        public virtual void TestReplacements()
        {
            Outputs<CharsRef> outputs = CharSequenceOutputs.Singleton;
            Builder<CharsRef> builder = new Builder<CharsRef>(FST.INPUT_TYPE.BYTE2, outputs);
            IntsRef scratchInts = new IntsRef();

            // a -> b
            Lucene.Net.Util.Fst.Util.ToUTF16("a", scratchInts);
            builder.Add(scratchInts, new CharsRef("b"));

            // ab -> c
            Lucene.Net.Util.Fst.Util.ToUTF16("ab", scratchInts);
            builder.Add(scratchInts, new CharsRef("c"));

            // c -> de
            Lucene.Net.Util.Fst.Util.ToUTF16("c", scratchInts);
            builder.Add(scratchInts, new CharsRef("de"));

            // def -> gh
            Lucene.Net.Util.Fst.Util.ToUTF16("def", scratchInts);
            builder.Add(scratchInts, new CharsRef("gh"));

            FST<CharsRef> fst = builder.Finish();

            StringBuilder sb = new StringBuilder("atestanother");
            Dictionary.ApplyMappings(fst, sb);
            assertEquals("btestbnother", sb.ToString());

            sb = new StringBuilder("abtestanother");
            Dictionary.ApplyMappings(fst, sb);
            assertEquals("ctestbnother", sb.ToString());

            sb = new StringBuilder("atestabnother");
            Dictionary.ApplyMappings(fst, sb);
            assertEquals("btestcnother", sb.ToString());

            sb = new StringBuilder("abtestabnother");
            Dictionary.ApplyMappings(fst, sb);
            assertEquals("ctestcnother", sb.ToString());

            sb = new StringBuilder("abtestabcnother");
            Dictionary.ApplyMappings(fst, sb);
            assertEquals("ctestcdenother", sb.ToString());

            sb = new StringBuilder("defdefdefc");
            Dictionary.ApplyMappings(fst, sb);
            assertEquals("ghghghde", sb.ToString());
        }

        [Test]
        public virtual void TestSetWithCrazyWhitespaceAndBOMs()
        {
            assertEquals("UTF-8", Dictionary.GetDictionaryEncoding(new MemoryStream("SET\tUTF-8\n".GetBytes(Encoding.UTF8))));
            assertEquals("UTF-8", Dictionary.GetDictionaryEncoding(new MemoryStream("SET\t UTF-8\n".GetBytes(Encoding.UTF8))));
            assertEquals("UTF-8", Dictionary.GetDictionaryEncoding(new MemoryStream("\uFEFFSET\tUTF-8\n".GetBytes(Encoding.UTF8))));
            assertEquals("UTF-8", Dictionary.GetDictionaryEncoding(new MemoryStream("\uFEFFSET\tUTF-8\r\n".GetBytes(Encoding.UTF8))));
        }

        [Test]
        public virtual void TestFlagWithCrazyWhitespace()
        {
            assertNotNull(Dictionary.GetFlagParsingStrategy("FLAG\tUTF-8"));
            assertNotNull(Dictionary.GetFlagParsingStrategy("FLAG    UTF-8"));
        }
    }
}