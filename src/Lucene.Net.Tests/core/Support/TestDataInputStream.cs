using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Support
{
    public class TestDataInputStream : LuceneTestCase
    {
        [Test]
        public void TestReadFully()
        {
            const string READFULLY_TEST_FILE = "Lucene.Net.Tests.core.Support.ReadFully.txt";
            byte[] buffer = new byte[1367];

            Stream @in = GetType().Assembly.GetManifestResourceStream(READFULLY_TEST_FILE);
            DataInputStream dis;
            using (dis = new DataInputStream(@in))
            { 
                // Read once for real
                dis.ReadFully(buffer, 0, 1366);
            }

            // Read past the end of the stream
            @in = GetType().Assembly.GetManifestResourceStream(READFULLY_TEST_FILE);
            dis = new DataInputStream(@in);
            bool caughtException = false;
            try
            {
                dis.ReadFully(buffer, 0, buffer.Length);
            }
#pragma warning disable 168
            catch (EndOfStreamException ie)
#pragma warning restore 168
            {
                caughtException = true;
            }
            finally
            {
                dis.Dispose();
                if (!caughtException)
                    fail("Test failed");
            }

            // Ensure we get an IndexOutOfRangeException exception when length is negative
            @in = GetType().Assembly.GetManifestResourceStream(READFULLY_TEST_FILE);
            dis = new DataInputStream(@in);
            caughtException = false;
            try
            {
                dis.ReadFully(buffer, 0, -20);
            }
#pragma warning disable 168
            catch (IndexOutOfRangeException ie)
#pragma warning restore 168
            {
                caughtException = true;
            }
            finally
            {
                dis.Dispose();
                if (!caughtException)
                    fail("Test failed");
            }
        }

        [Test]
        public void TestReadLinePushback()
        {
            using (MemoryStream pis = new MemoryStream("\r".GetBytes(Encoding.UTF8)))
            {
                DataInputStream dis = new DataInputStream(pis);

#pragma warning disable 612, 618
                string line = dis.ReadLine();
#pragma warning restore 612, 618
                if (line == null)
                {
                    fail("Got null, should return empty line");
                }

                long count = pis.Length - (line.Length + 1 /*account for the newline*/);

                if (count != 0)
                {
                    fail("Test failed: available() returns "
                                         + count + " when the file is empty");
                }
            }
        }

        [Test]
        public void TestReadUTF()
        {
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                try
                {
                    WriteAndReadAString();
                }
                catch (FormatException utfdfe)
                {
                    if (utfdfe.Message == null)
                        fail("vague exception thrown");
                }
#pragma warning disable 168
                catch (EndOfStreamException eofe)
#pragma warning restore 168
                {
                    // These are rare and beyond the scope of the test
                }
            }
        }


        private static readonly int TEST_ITERATIONS = 1000;

        private static readonly int A_NUMBER_NEAR_65535 = 60000;

        private static readonly int MAX_CORRUPTIONS_PER_CYCLE = 3;

        private static void WriteAndReadAString()
        {
            // Write out a string whose UTF-8 encoding is quite possibly
            // longer than 65535 bytes
            int length = Random().nextInt(A_NUMBER_NEAR_65535) + 1;
            MemoryStream baos = new MemoryStream();
            StringBuilder testBuffer = new StringBuilder();
            for (int i = 0; i < length; i++)
                testBuffer.append((char)Random().Next());
            string testString = testBuffer.toString();
            DataOutputStream dos = new DataOutputStream(baos);
            dos.WriteUTF(testString);

            // Corrupt the data to produce malformed characters
            byte[] testBytes = baos.ToArray();
            int dataLength = testBytes.Length;
            int corruptions = Random().nextInt(MAX_CORRUPTIONS_PER_CYCLE);
            for (int i = 0; i < corruptions; i++)
            {
                int index = Random().nextInt(dataLength);
                testBytes[index] = (byte)Random().Next();
            }

            // Pay special attention to mangling the end to produce
            // partial characters at end
            testBytes[dataLength - 1] = (byte)Random().Next();
            testBytes[dataLength - 2] = (byte)Random().Next();

            // Attempt to decode the bytes back into a String
            MemoryStream bais = new MemoryStream(testBytes);
            DataInputStream dis = new DataInputStream(bais);
            dis.ReadUTF();
        }

        [Test]
        public void TestSkipBytes()
        {
            DataInputStream dis = new DataInputStream(new MyInputStream());
            dotest(dis, 0, 11, -1, 0);
            dotest(dis, 0, 11, 5, 5);
            Console.WriteLine("\n***CAUTION**** - may go into an infinite loop");
            dotest(dis, 5, 11, 20, 6);
        }


        private static void dotest(DataInputStream dis, int pos, int total,
                               int toskip, int expected)
        {

            try
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\n\nTotal bytes in the stream = " + total);
                    Console.WriteLine("Currently at position = " + pos);
                    Console.WriteLine("Bytes to skip = " + toskip);
                    Console.WriteLine("Expected result = " + expected);
                }
                int skipped = dis.SkipBytes(toskip);
                if (VERBOSE)
                {
                    Console.WriteLine("Actual skipped = " + skipped);
                }
                if (skipped != expected)
                {
                    fail("DataInputStream.skipBytes does not return expected value");
                }
            }
            catch (EndOfStreamException e)
            {
                fail("DataInputStream.skipBytes throws unexpected EOFException");
            }
            catch (IOException e)
            {
                Console.WriteLine("IOException is thrown - possible result");
            }
        }

        internal class MyInputStream : MemoryStream
        {

            private int readctr = 0;


            public override int ReadByte()
            {

                if (readctr > 10)
                {
                    return -1;
                }
                else
                {
                    readctr++;
                    return 0;
                }

            }

        }
    }
}
