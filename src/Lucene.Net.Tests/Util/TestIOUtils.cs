using Lucene.Net.Attributes;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util
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

    [TestFixture]
    public class TestIOUtils : LuceneTestCase
    {
        internal sealed class BrokenIDisposable : IDisposable
        {
            internal readonly int i;

            public BrokenIDisposable(int i)
            {
                this.i = i;
            }

            public void Dispose()
            {
                throw new IOException("TEST-IO-EXCEPTION-" + i);
            }
        }

        internal sealed class TestException : Exception
        {
            public TestException()
                : base("BASE-EXCEPTION")
            {
            }
        }

        [Test]
        public virtual void TestSuppressedExceptions()
        {
            // test with prior exception
            try
            {
                TestException t = new TestException();
                IOUtils.DisposeWhileHandlingException(t, new BrokenIDisposable(1), new BrokenIDisposable(2));
            }
            catch (TestException e1)
            {
                assertEquals("BASE-EXCEPTION", e1.Message);
                assertEquals(2, e1.GetSuppressed().Length);
                assertEquals("TEST-IO-EXCEPTION-1", e1.GetSuppressed()[0].Message);
                assertEquals("TEST-IO-EXCEPTION-2", e1.GetSuppressed()[1].Message);
            }
            catch (Exception e2) when (e2.IsIOException())
            {
                Assert.Fail("IOException should not be thrown here");
            }

            // test without prior exception
            try
            {
                IOUtils.DisposeWhileHandlingException((TestException)null, new BrokenIDisposable(1), new BrokenIDisposable(2));
            }
            catch (TestException)
            {
                fail("TestException should not be thrown here");
            }
            catch (Exception e2) when (e2.IsIOException())
            {
                assertEquals("TEST-IO-EXCEPTION-1", e2.Message);
                assertEquals(1, e2.GetSuppressed().Length);
                assertEquals("TEST-IO-EXCEPTION-2", e2.GetSuppressed()[0].Message);
            }
            catch (Exception)
            {
                fail("Exception should not be thrown here");
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestGetDecodingReaderWithStringPath()
        {
            // Test with UTF-8 encoding
            var tempFile = CreateTempFile("ioutilsreader", ".txt");
            string content = "Test content with special chars: ñ, ü, 中文";
            File.WriteAllText(tempFile.FullName, content, Encoding.UTF8);

            try
            {
                // Test GetDecodingReader with string path
                using (var reader = IOUtils.GetDecodingReader(tempFile.FullName, StandardCharsets.UTF_8))
                {
                    string readContent = reader.ReadToEnd();
                    assertEquals(content, readContent);
                }
            }
            finally
            {
                // Clean up
                if (File.Exists(tempFile.FullName))
                {
                    File.Delete(tempFile.FullName);
                }
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestGetDecodingReaderWithRelativePath()
        {
            // Create temp file
            var tempDir = CreateTempDir("ioutilsreader_relative");
            string fileName = "test.txt";
            string filePath = System.IO.Path.Combine(tempDir.FullName, fileName);
            string content = "Test content with relative path";
            File.WriteAllText(filePath, content, Encoding.UTF8);

            // Use SystemEnvironment to safely change current directory
            SystemEnvironment.WithCurrentDirectory(tempDir.FullName, () =>
            {
                // Test with just the file name
                using (var reader = IOUtils.GetDecodingReader(fileName, StandardCharsets.UTF_8))
                {
                    string readContent = reader.ReadToEnd();
                    assertEquals(content, readContent);
                }

                // Test with "./" prefix
                using (var reader = IOUtils.GetDecodingReader("./" + fileName, StandardCharsets.UTF_8))
                {
                    string readContent = reader.ReadToEnd();
                    assertEquals(content, readContent);
                }
            });

            // Clean up
            if (Directory.Exists(tempDir.FullName))
            {
                Directory.Delete(tempDir.FullName, true);
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestGetDecodingReaderWithDifferentEncodings()
        {
            var tempDir = CreateTempDir("ioutilsreader_encodings");

            try
            {
                // Test with UTF-8 and Unicode encodings
                string testString = "Text with accents: à, é, í, ó, ú";

                // Test UTF-8
                string filePathUtf8 = System.IO.Path.Combine(tempDir.FullName, "test_utf8.txt");
                File.WriteAllText(filePathUtf8, testString, Encoding.UTF8);

                using (var reader = IOUtils.GetDecodingReader(filePathUtf8, StandardCharsets.UTF_8))
                {
                    string readContent = reader.ReadToEnd();
                    assertEquals(testString, readContent);
                }

                // Test with ASCII (simple content)
                string asciiString = "Simple ASCII text";
                string filePathAscii = System.IO.Path.Combine(tempDir.FullName, "test_ascii.txt");
                File.WriteAllText(filePathAscii, asciiString, Encoding.ASCII);

                using (var reader = IOUtils.GetDecodingReader(filePathAscii, Encoding.ASCII))
                {
                    string readContent = reader.ReadToEnd();
                    assertEquals(asciiString, readContent);
                }
            }
            finally
            {
                // Clean up
                if (Directory.Exists(tempDir.FullName))
                {
                    Directory.Delete(tempDir.FullName, true);
                }
            }
        }
    }
}
