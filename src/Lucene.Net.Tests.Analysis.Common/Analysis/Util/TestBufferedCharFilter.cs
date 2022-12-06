// Lucene version compatibility level 4.8.1
// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Util
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

    public class TestBufferedCharFilter : LuceneTestCase
    {
        BufferedCharFilter br;

        readonly String testString = "Test_All_Tests\nTest_java_io_BufferedInputStream\nTest_java_io_BufferedOutputStream\nTest_java_io_ByteArrayInputStream\nTest_java_io_ByteArrayOutputStream\nTest_java_io_DataInputStream\nTest_java_io_File\nTest_java_io_FileDescriptor\nTest_java_io_FileInputStream\nTest_java_io_FileNotFoundException\nTest_java_io_FileOutputStream\nTest_java_io_FilterInputStream\nTest_java_io_FilterOutputStream\nTest_java_io_InputStream\nTest_java_io_IOException\nTest_java_io_OutputStream\nTest_java_io_PrintStream\nTest_java_io_RandomAccessFile\nTest_java_io_SyncFailedException\nTest_java_lang_AbstractMethodError\nTest_java_lang_ArithmeticException\nTest_java_lang_ArrayIndexOutOfBoundsException\nTest_java_lang_ArrayStoreException\nTest_java_lang_Boolean\nTest_java_lang_Byte\nTest_java_lang_Character\nTest_java_lang_Class\nTest_java_lang_ClassCastException\nTest_java_lang_ClassCircularityError\nTest_java_lang_ClassFormatError\nTest_java_lang_ClassLoader\nTest_java_lang_ClassNotFoundException\nTest_java_lang_CloneNotSupportedException\nTest_java_lang_Double\nTest_java_lang_Error\nTest_java_lang_Exception\nTest_java_lang_ExceptionInInitializerError\nTest_java_lang_Float\nTest_java_lang_IllegalAccessError\nTest_java_lang_IllegalAccessException\nTest_java_lang_IllegalArgumentException\nTest_java_lang_IllegalMonitorStateException\nTest_java_lang_IllegalThreadStateException\nTest_java_lang_IncompatibleClassChangeError\nTest_java_lang_IndexOutOfBoundsException\nTest_java_lang_InstantiationError\nTest_java_lang_InstantiationException\nTest_java_lang_Integer\nTest_java_lang_InternalError\nTest_java_lang_InterruptedException\nTest_java_lang_LinkageError\nTest_java_lang_Long\nTest_java_lang_Math\nTest_java_lang_NegativeArraySizeException\nTest_java_lang_NoClassDefFoundError\nTest_java_lang_NoSuchFieldError\nTest_java_lang_NoSuchMethodError\nTest_java_lang_NullPointerException\nTest_java_lang_Number\nTest_java_lang_NumberFormatException\nTest_java_lang_Object\nTest_java_lang_OutOfMemoryError\nTest_java_lang_RuntimeException\nTest_java_lang_SecurityManager\nTest_java_lang_Short\nTest_java_lang_StackOverflowError\nTest_java_lang_String\nTest_java_lang_StringBuffer\nTest_java_lang_StringIndexOutOfBoundsException\nTest_java_lang_System\nTest_java_lang_Thread\nTest_java_lang_ThreadDeath\nTest_java_lang_ThreadGroup\nTest_java_lang_Throwable\nTest_java_lang_UnknownError\nTest_java_lang_UnsatisfiedLinkError\nTest_java_lang_VerifyError\nTest_java_lang_VirtualMachineError\nTest_java_lang_vm_Image\nTest_java_lang_vm_MemorySegment\nTest_java_lang_vm_ROMStoreException\nTest_java_lang_vm_VM\nTest_java_lang_Void\nTest_java_net_BindException\nTest_java_net_ConnectException\nTest_java_net_DatagramPacket\nTest_java_net_DatagramSocket\nTest_java_net_DatagramSocketImpl\nTest_java_net_InetAddress\nTest_java_net_NoRouteToHostException\nTest_java_net_PlainDatagramSocketImpl\nTest_java_net_PlainSocketImpl\nTest_java_net_Socket\nTest_java_net_SocketException\nTest_java_net_SocketImpl\nTest_java_net_SocketInputStream\nTest_java_net_SocketOutputStream\nTest_java_net_UnknownHostException\nTest_java_util_ArrayEnumerator\nTest_java_util_Date\nTest_java_util_EventObject\nTest_java_util_HashEnumerator\nTest_java_util_Hashtable\nTest_java_util_Properties\nTest_java_util_ResourceBundle\nTest_java_util_tm\nTest_java_util_Vector\n";

        /**
         * The spec says that BufferedReader.readLine() considers only "\r", "\n"
         * and "\r\n" to be line separators. We must not permit additional separator
         * characters.
        */
        [Test, LuceneNetSpecific]
        public void Test_ReadLine_IgnoresEbcdic85Characters()
        {
            assertLines("A\u0085B", "A\u0085B");
        }

        [Test, LuceneNetSpecific]
        public void Test_ReadLine_Separators()
        {
            assertLines("A\nB\nC", "A", "B", "C");
            assertLines("A\rB\rC", "A", "B", "C");
            assertLines("A\r\nB\r\nC", "A", "B", "C");
            assertLines("A\n\rB\n\rC", "A", "", "B", "", "C");
            assertLines("A\n\nB\n\nC", "A", "", "B", "", "C");
            assertLines("A\r\rB\r\rC", "A", "", "B", "", "C");
            assertLines("A\n\n", "A", "");
            assertLines("A\n\r", "A", "");
            assertLines("A\r\r", "A", "");
            assertLines("A\r\n", "A");
            assertLines("A\r\n\r\n", "A", "");
        }

        private void assertLines(string @in, params string[] lines)
        {
            BufferedCharFilter bufferedReader
                = new BufferedCharFilter(new StringReader(@in));
            foreach (String line in lines)
            {
                assertEquals(line, bufferedReader.ReadLine());
            }
            assertNull(bufferedReader.ReadLine());
        }

        /**
         * @tests java.io.BufferedReader#BufferedReader(java.io.Reader)
         */
        [Test, LuceneNetSpecific]
        public void Test_ConstructorLjava_io_Reader()
        {
            // Test for method java.io.BufferedReader(java.io.Reader)
            assertTrue("Used in tests", true);
        }

        /**
         * @tests java.io.BufferedReader#BufferedReader(java.io.Reader, int)
         */
        [Test, LuceneNetSpecific]
        public void Test_ConstructorLjava_io_ReaderI()
        {
            // Test for method java.io.BufferedReader(java.io.Reader, int)
            assertTrue("Used in tests", true);
        }

        /**
         * @tests java.io.BufferedReader#close()
         */
        [Test, LuceneNetSpecific]
        public void Test_Close()
        {
            // Test for method void java.io.BufferedReader.close()
            try
            {
                br = new BufferedCharFilter(new StringReader(testString));
                br.Dispose();
                br.Read();
                fail("Read on closed stream");
            }
            catch (Exception x) when (x.IsIOException())
            {
                return;
            }
        }

        /**
         * @tests java.io.BufferedReader#mark(int)
         */
        [Test, LuceneNetSpecific]
        public void Test_MarkI()
        {
            // Test for method void java.io.BufferedReader.mark(int)
            char[] buf = null;
            br = new BufferedCharFilter(new StringReader(testString));
            br.Skip(500);
            br.Mark(1000);
            br.Skip(250);
            br.Reset();
            buf = new char[testString.Length];
            br.Read(buf, 0, 500);

            assertTrue("Failed to set mark properly", testString.Substring(500,
                    1000 - 500).Equals(new string(buf, 0, 500), StringComparison.Ordinal));

            try
            {
                br = new BufferedCharFilter(new StringReader(testString), 800);
                br.Skip(500);
                br.Mark(250);
                br.Read(buf, 0, 1000);
                br.Reset();

                fail("Failed to invalidate mark properly");
            }
            catch (Exception x) when (x.IsIOException())
            {
                // Expected
            }

            char[] chars = new char[256];
            for (int i = 0; i < 256; i++)
                chars[i] = (char)i;
            BufferedCharFilter @in = new BufferedCharFilter(new StringReader(new String(
                    chars)), 12);

            @in.Skip(6);
            @in.Mark(14);
            @in.Read(new char[14], 0, 14);
            @in.Reset();

            assertTrue("Wrong chars", @in.Read() == (char)6
                    && @in.Read() == (char)7);

            @in = new BufferedCharFilter(new StringReader(new String(chars)), 12);
            @in.Skip(6);
            @in.Mark(8);
            @in.Skip(7);
            @in.Reset();

            assertTrue("Wrong chars 2", @in.Read() == (char)6
                    && @in.Read() == (char)7);

            BufferedCharFilter br2 = new BufferedCharFilter(new StringReader("01234"), 2);
            br2.Mark(3);
            char[] carray = new char[3];
            int result = br2.read(carray);
            assertEquals(3, result);
            assertEquals("Assert 0:", '0', carray[0]);
            assertEquals("Assert 1:", '1', carray[1]);
            assertEquals("Assert 2:", '2', carray[2]);
            assertEquals("Assert 3:", '3', br2.Read());

            br2 = new BufferedCharFilter(new StringReader("01234"), 2);
            br2.Mark(3);
            carray = new char[4];
            result = br2.read(carray);
            assertEquals("Assert 4:", 4, result);
            assertEquals("Assert 5:", '0', carray[0]);
            assertEquals("Assert 6:", '1', carray[1]);
            assertEquals("Assert 7:", '2', carray[2]);
            assertEquals("Assert 8:", '3', carray[3]);
            assertEquals("Assert 9:", '4', br2.Read());
            assertEquals("Assert 10:", -1, br2.Read());

            BufferedCharFilter reader = new BufferedCharFilter(new StringReader("01234"));
            reader.Mark(int.MaxValue);
            reader.Read();
            reader.Dispose();
        }

        /**
         * @tests java.io.BufferedReader#markSupported()
         */
        [Test, LuceneNetSpecific]
        public void Test_IsMarkSupported()
        {
            // Test for method boolean java.io.BufferedReader.markSupported()
            br = new BufferedCharFilter(new StringReader(testString));
            assertTrue("markSupported returned false", br.IsMarkSupported);
        }

        /**
         * @tests java.io.BufferedReader#read()
         */
        [Test, LuceneNetSpecific]
        public void Test_Read()
        {
            // Test for method int java.io.BufferedReader.read()
            try
            {
                br = new BufferedCharFilter(new StringReader(testString));
                int r = br.Read();
                assertTrue("Char read improperly", testString[0] == r);
                br = new BufferedCharFilter(new StringReader(new String(
                        new char[] { '\u8765' })));
                assertTrue("Wrong double byte character", br.Read() == '\u8765');
            }
            catch (Exception e) when (e.IsIOException())
            {
                fail("Exception during read test");
            }

            char[] chars = new char[256];
            for (int i = 0; i < 256; i++)
                chars[i] = (char)i;
            BufferedCharFilter @in = new BufferedCharFilter(new StringReader(new String(
                chars)), 12);
            try
            {

                assertEquals("Wrong initial char", 0, @in.Read()); // Fill the
                                                                   // buffer
                char[] buf = new char[14];
                @in.Read(buf, 0, 14); // Read greater than the buffer

                assertTrue("Wrong block read data", new string(buf)
                        .Equals(new string(chars, 1, 14), StringComparison.Ordinal));

                assertEquals("Wrong chars", 15, @in.Read()); // Check next byte
            }
            catch (Exception e) when (e.IsIOException())
            {
                fail("Exception during read test 2:" + e);
            }

            // regression test for HARMONY-841
            assertTrue(new BufferedCharFilter(new StringReader(new string(new char[5], 1, 0)), 2).Read() == -1);
        }

        private sealed class ReaderAnonymousClass : CharFilter
        {
            private const int SIZE = 2;
            private int pos = 0;

            private readonly char[] contents = new char[SIZE];

            public ReaderAnonymousClass()
                : base(null)
            { }

            public override int Read()
            {
                if (pos >= SIZE)
                    throw new IOException("Read past end of data");
                return contents[pos++];
            }

            public override int Read(char[] buf, int off, int len)
            {
                if (pos >= SIZE)
                    throw new IOException("Read past end of data");
                int toRead = len;
                if (toRead > (SIZE - pos))
                    toRead = SIZE - pos;
                Arrays.Copy(contents, pos, buf, off, toRead);
                pos += toRead;
                return toRead;
            }

            public bool Ready()
            {
                return SIZE - pos > 0;
            }

//#if FEATURE_TEXTWRITER_CLOSE
//            public override void Close()
//            {
//            }
//#endif

            protected override void Dispose(bool disposing)
            {
            }

            protected override int Correct(int currentOff)
            {
                throw new NotImplementedException();
            }
        }

        /**
         * @tests java.io.BufferedReader#read(char[], int, int)
         */
        [Test, LuceneNetSpecific]
        public void Test_ReadCII()
        {
            char[] ca = new char[2];
            BufferedCharFilter toRet = new BufferedCharFilter(new StreamReader(
                new MemoryStream(new byte[0])));

            /* Null buffer should throw NPE even when len == 0 */
            try
            {
                toRet.Read(null, 1, 0);

                fail("null buffer reading zero bytes should throw NPE");
            }
#pragma warning disable 168
            catch (ArgumentNullException e) // LUCENENET specific - changed from NullPointerException to ArgumentNullException
#pragma warning restore 168
            {
                //expected
            }

            try
            {
                toRet.Dispose();
            }
            catch (Exception e) when (e.IsIOException())
            {

                fail("unexpected 1: " + e);
            }

            try
            {
                toRet.Read(null, 1, 0);

                fail("null buffer reading zero bytes on closed stream should throw IOException");
            }
            catch (Exception e) when (e.IsIOException())
            {
                //expected
            }

            /* Closed reader should throw IOException reading zero bytes */
            try
            {
                toRet.Read(ca, 0, 0);

                fail("Reading zero bytes on a closed reader should not work");
            }
            catch (Exception e) when (e.IsIOException())
            {
                // expected
            }

            /*
             * Closed reader should throw IOException in preference to index out of
             * bounds
             */
            try
            {
                // Read should throw IOException before
                // ArrayIndexOutOfBoundException
                toRet.Read(ca, 1, 5);

                fail("IOException should have been thrown");
            }
            catch (Exception e) when (e.IsIOException())
            {
                // expected
            }

            // Test to ensure that a drained stream returns 0 at EOF
            toRet = new BufferedCharFilter(new StreamReader(

                    new MemoryStream(new byte[2])));
            try
            {

                assertEquals("Emptying the reader should return two bytes", 2,
                        toRet.Read(ca, 0, 2));

                // LUCENENET specific: end of stream should be 0 in .NET
                assertEquals("EOF on a reader should be 0", 0, toRet.Read(ca, 0,
                        2));
                //assertEquals("EOF on a reader should be -1", -1, toRet.Read(ca, 0,
                //        2));

                assertEquals("Reading zero bytes at EOF should work", 0, toRet
                        .Read(ca, 0, 0));
            }
            catch (Exception ex) when (ex.IsIOException())
            {

                fail("Unexpected IOException : " + ex.ToString());
            }

            // Test for method int java.io.BufferedReader.read(char [], int, int)
            try
            {
                char[] buf = new char[testString.Length];
                br = new BufferedCharFilter(new StringReader(testString));
                br.Read(buf, 50, 500);

                assertTrue("Chars read improperly", new string(buf, 50, 500)
                        .Equals(testString.Substring(0, 500 - 0), StringComparison.Ordinal));
            }
            catch (Exception e) when (e.IsIOException())
            {

                fail("Exception during read test");
            }

            BufferedCharFilter bufin = new BufferedCharFilter(new ReaderAnonymousClass());

            //BufferedCharFilter bufin = new BufferedCharFilter(new Reader() {
            //            int size = 2, pos = 0;

            //char[] contents = new char[size];

            //public int read() 
            //{
            //				if (pos >= size)
            //					throw new IOException("Read past end of data");
            //				return contents[pos++];
            //			}

            //			public int read(char[] buf, int off, int len) throws IOException
            //{
            //				if (pos >= size)
            //					throw new IOException("Read past end of data");
            //int toRead = len;
            //				if (toRead > (size - pos))
            //					toRead = size - pos;
            //				System.arraycopy(contents, pos, buf, off, toRead);
            //				pos += toRead;
            //				return toRead;
            //			}

            //			public boolean ready() throws IOException
            //{
            //				return size - pos > 0;
            //}

            //public void close() 
            //{
            //}
            //		});
            try
            {
                bufin.Read();
                int result = bufin.Read(new char[2], 0, 2);

                assertTrue("Incorrect result: " + result, result == 1);
            }
            catch (Exception e) when (e.IsIOException())
            {

                fail("Unexpected: " + e);
            }

            //regression for HARMONY-831
            try
            {
                new BufferedCharFilter(new StringReader(""), 9).Read(new char[] { }, 7, 0);
                fail("should throw IndexOutOfBoundsException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException e)
#pragma warning restore 168
            {
            }

            // Regression for HARMONY-54
            char[] ch = { };
            BufferedCharFilter reader = new BufferedCharFilter(new StringReader(new string(ch)));
            try
            {
                // Check exception thrown when the reader is open.
                reader.Read(null, 1, 0);
                fail("Assert 0: NullPointerException expected");
            }
#pragma warning disable 168
            catch (ArgumentNullException e) // LUCENENET specific - changed from NullPointerException to ArgumentNullException
#pragma warning restore 168
            {
                // Expected
            }

            // Now check IOException is thrown in preference to
            // NullPointerexception when the reader is closed.
            reader.Dispose();
            try
            {
                reader.Read(null, 1, 0);
                fail("Assert 1: IOException expected");
            }
            catch (Exception e) when (e.IsIOException())
            {
                // Expected
            }

            try
            {
                // And check that the IOException is thrown before
                // ArrayIndexOutOfBoundException
                reader.Read(ch, 0, 42);
                fail("Assert 2: IOException expected");
            }
            catch (Exception e) when (e.IsIOException())
            {
                // expected
            }
        }

        /**
         * @tests java.io.BufferedReader#read(char[], int, int)
         */
        [Test, LuceneNetSpecific]
        public void Test_Read_CII_Exception()
        {
            br = new BufferedCharFilter(new StringReader(testString));
            char[] nullCharArray = null;
            char[] charArray = testString.toCharArray();

            try
            {
                br.Read(nullCharArray, -1, -1);

                fail("should throw IndexOutOfBoundsException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException e)
#pragma warning restore 168
            {
                // expected
            }

            try
            {
                br.Read(nullCharArray, -1, 0);

                fail("should throw IndexOutOfBoundsException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException e)
#pragma warning restore 168
            {
                // expected
            }

            try
            {
                br.Read(nullCharArray, 0, -1);

                fail("should throw NullPointerException");
            }
#pragma warning disable 168
            catch (ArgumentNullException e) // LUCENENET specific - changed from NullPointerException to ArgumentNullException
#pragma warning restore 168
            {
                // expected
            }

            try
            {
                br.Read(nullCharArray, 0, 0);

                fail("should throw NullPointerException");
            }
#pragma warning disable 168
            catch (ArgumentNullException e) // LUCENENET specific - changed from NullPointerException to ArgumentNullException
#pragma warning restore 168
            {
                // expected
            }

            try
            {
                br.Read(nullCharArray, 0, 1);

                fail("should throw NullPointerException");
            }
#pragma warning disable 168
            catch (ArgumentNullException e) // LUCENENET specific - changed from NullPointerException to ArgumentNullException
#pragma warning restore 168
            {
                // expected
            }

            try
            {
                br.Read(charArray, -1, -1);

                fail("should throw IndexOutOfBoundsException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException e)
#pragma warning restore 168
            {
                // expected
            }

            try
            {
                br.Read(charArray, -1, 0);

                fail("should throw IndexOutOfBoundsException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException e)
#pragma warning restore 168
            {
                // expected
            }

            br.Read(charArray, 0, 0);
            br.Read(charArray, 0, charArray.Length);
            br.Read(charArray, charArray.Length, 0);

            try
            {
                br.Read(charArray, charArray.Length + 1, 0);

                fail("should throw IndexOutOfBoundsException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException e)
#pragma warning restore 168
            {
                //expected
            }

            try
            {
                br.Read(charArray, charArray.Length + 1, 1);

                fail("should throw IndexOutOfBoundsException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException e)
#pragma warning restore 168
            {
                //expected
            }

            br.Dispose();

            try
            {
                br.Read(nullCharArray, -1, -1);

                fail("should throw IOException");
            }
            catch (Exception e) when (e.IsIOException())
            {
                // expected
            }

            try
            {
                br.Read(charArray, -1, 0);

                fail("should throw IOException");
            }
            catch (Exception e) when (e.IsIOException())
            {
                // expected
            }

            try
            {
                br.Read(charArray, 0, -1);

                fail("should throw IOException");
            }
            catch (Exception e) when (e.IsIOException())
            {
                // expected
            }
        }
        /**
         * @tests java.io.BufferedReader#readLine()
         */
        [Test, LuceneNetSpecific]
        public void Test_ReadLine()
        {
            // Test for method java.lang.String java.io.BufferedReader.readLine()
            try
            {
                br = new BufferedCharFilter(new StringReader(testString));
                String r = br.ReadLine();
                assertEquals("readLine returned incorrect string", "Test_All_Tests", r
                        );
            }
            catch (Exception e) when (e.IsIOException())
            {
                fail("Exception during readLine test");
            }
        }

        /**
         * @tests java.io.BufferedReader#ready()
         */
        public void Test_Ready()
        {
            // Test for method boolean java.io.BufferedReader.ready()
            try
            {
                br = new BufferedCharFilter(new StringReader(testString));
                assertTrue("IsReady returned false", br.IsReady);
            }
            catch (Exception e) when (e.IsIOException())
            {
                fail("Exception during ready test" + e.toString());
            }
        }

        /**
         * @tests java.io.BufferedReader#reset()
         */
        public void Test_Reset()
        {
            // Test for method void java.io.BufferedReader.reset()
            try
            {
                br = new BufferedCharFilter(new StringReader(testString));
                br.Skip(500);
                br.Mark(900);
                br.Skip(500);
                br.Reset();
                char[] buf = new char[testString.Length];
                br.Read(buf, 0, 500);
                assertTrue("Failed to reset properly", testString.Substring(500,
                        1000 - 500).Equals(new string(buf, 0, 500), StringComparison.Ordinal));
            }
            catch (Exception e) when (e.IsIOException())
            {
                fail("Exception during reset test");
            }
            try
            {
                br = new BufferedCharFilter(new StringReader(testString));
                br.Skip(500);
                br.Reset();
                fail("Reset succeeded on unmarked stream");
            }
            catch (Exception x) when (x.IsIOException())
            {
                return;

            }
        }

        [Test, LuceneNetSpecific]
        public void Test_Reset_IOException()
        {
            int[]
        expected = new int[] { '1', '2', '3', '4', '5', '6', '7', '8',
                '9', '0', -1 };
            br = new BufferedCharFilter(new StringReader("1234567890"), 9);
            br.Mark(9);
            for (int i = 0; i < 11; i++)
            {
                assertEquals(expected[i], br.Read());
            }
            try
            {
                br.Reset();
                fail("should throw IOException");
            }
            catch (Exception e) when (e.IsIOException())
            {
                // Expected
            }
            for (int i = 0; i < 11; i++)
            {
                assertEquals(-1, br.Read());
            }

            br = new BufferedCharFilter(new StringReader("1234567890"));
            br.Mark(10);
            for (int i = 0; i < 10; i++)
            {
                assertEquals(expected[i], br.Read());
            }
            br.Reset();
            for (int i = 0; i < 11; i++)
            {
                assertEquals(expected[i], br.Read());
            }
        }

        /**
         * @tests java.io.BufferedReader#skip(long)
         */
        [Test, LuceneNetSpecific]
        public void Test_SkipJ()
        {
            // Test for method long java.io.BufferedReader.skip(long)
            try
            {
                br = new BufferedCharFilter(new StringReader(testString));
                br.Skip(500);
                char[] buf = new char[testString.Length];
                br.Read(buf, 0, 500);
                assertTrue("Failed to set skip properly", testString.Substring(500,
                        1000 - 500).Equals(new string(buf, 0, 500), StringComparison.Ordinal));
            }
            catch (Exception e) when (e.IsIOException())
            {
                fail("Exception during skip test");
            }

        }

        /**
         * Sets up the fixture, for example, open a network connection. This method
         * is called before a test is executed.
         */
        public override void SetUp()
        {
        }

        /**
         * Tears down the fixture, for example, close a network connection. This
         * method is called after a test is executed.
         */
        public override void TearDown()
        {
            try
            {
                br.Dispose();
            }
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {
            }
        }
    }
}