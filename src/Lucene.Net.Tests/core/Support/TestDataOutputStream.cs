using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;

namespace Lucene.Net.Support
{
    public class TestDataOutputStream : LuceneTestCase
    {
        [Test]
        public void TestCounterOverflow()
        {
            var output = new MemoryStream();
            CounterOverflow dataOut = new CounterOverflow(output);

            dataOut.WriteByte(1);
            if (dataOut.Length < 0)
            {
                fail("Internal counter less than 0.");
            }
        }

        private class CounterOverflow : DataOutputStream
        {
            public CounterOverflow(Stream output)
                : base(output)
            {
                base.written = int.MaxValue;
            }
        }

        [Test]
        public void TestWriteUTF()
        {
            ByteArrayOutputStream baos = new ByteArrayOutputStream();
            DataOutputStream dos = new DataOutputStream(baos);
            dos.WriteUTF("Hello, World!");  // 15
            dos.Flush();
            if (baos.Length != dos.Length)
                fail("Miscounted bytes in DataOutputStream.");
        }

        [Test]
        public void TestBoundsCheck()
        {
            byte[] data = { 90, 91, 92, 93, 94, 95, 96, 97, 98, 99 };
            ByteArrayOutputStream bos = new ByteArrayOutputStream();
            DummyFilterStream dfs = new DummyFilterStream(bos);
            bool caughtException = false;

            // -ve length
            try
            {
                dfs.Write(data, 0, -5);
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException ie)
#pragma warning restore 168
            {
                caughtException = true;
            }
            finally
            {
                if (!caughtException)
                    fail("Test failed");
            }

            // -ve offset
            caughtException = false;
            try
            {
                dfs.Write(data, -2, 5);
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException ie)
#pragma warning restore 168
            {
                caughtException = true;
            }
            finally
            {
                if (!caughtException)
                    fail("Test failed");
            }

            // off + len > data.length
            caughtException = false;
            try
            {
                dfs.Write(data, 6, 5);
            }
#pragma warning disable 168
            catch (ArgumentException ie)
#pragma warning restore 168
            {
                caughtException = true;
            }
            finally
            {
                if (!caughtException)
                    fail("Test failed");
            }

            // null data
            caughtException = false;
            try
            {
                dfs.Write(null, 0, 5);
            }
#pragma warning disable 168
            catch (ArgumentNullException re)
#pragma warning restore 168
            {
                caughtException = true;
            }
            finally
            {
                if (!caughtException)
                    fail("Test failed");
            }
        }

        private class DummyFilterStream : DataOutputStream
        {

            public DummyFilterStream(Stream o)
                    : base(o)
            {
            }

            public override void Write(int val)
            {
                base.Write(val + 1);
            }
        }

        [Test]
        public void TestWrite()
        {
            IDataOutput f = new F(new Sink());
            f.Write(new byte[] { 1, 2, 3 }, 0, 3);
        }

        private class F : DataOutputStream
        {

            public F(Stream o)
                    : base(o)
            {
            }

            public override void Write(int b)
            {
                Debug.WriteLine("Ignoring write of " + b);
            }

        }

        private class Sink : MemoryStream
        {

            public override void WriteByte(byte b)
            {
                throw new Exception("DataOutputStream directly invoked"
                                           + " Write(byte) method of underlying"
                                           + " stream");
            }

        }
    }
}
