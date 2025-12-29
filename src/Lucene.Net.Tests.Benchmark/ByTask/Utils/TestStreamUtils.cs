using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask.Utils
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

    public class TestStreamUtils : LuceneTestCase
    {
        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestGetInputStreamWithStringPath()
        {
            // Create a temp file with content
            var tempFile = CreateTempFile("streamutils", ".txt");
            const string content = "Test content for GetInputStream";
            File.WriteAllText(tempFile.FullName, content);

            try
            {
                // Test GetInputStream with string path
                using (var stream = StreamUtils.GetInputStream(tempFile.FullName))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string readContent = reader.ReadToEnd();
                    assertEquals(content, readContent);
                }

                // Test with relative path
                SystemEnvironment.WithCurrentDirectory(tempFile.DirectoryName, () =>
                {
                    string relativePath = tempFile.Name;

                    using var stream = StreamUtils.GetInputStream(relativePath);
                    using var reader = new StreamReader(stream, Encoding.UTF8);

                    string readContent = reader.ReadToEnd();
                    assertEquals(content, readContent);
                });
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
        public virtual void TestGetOutputStreamWithStringPath()
        {
            // Create a temp file path
            var tempDir = CreateTempDir("streamutils_output");
            string tempFilePath = Path.Combine(tempDir.FullName, "output.txt");
            string content = "Test content for GetOutputStream";

            try
            {
                // Test GetOutputStream with string path
                using (var stream = StreamUtils.GetOutputStream(tempFilePath))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.Write(content);
                }

                // Verify file was written correctly
                assertTrue(File.Exists(tempFilePath));
                string readContent = File.ReadAllText(tempFilePath);
                assertEquals(content, readContent);

                // Test with relative path
                SystemEnvironment.WithCurrentDirectory(tempDir.FullName, () =>
                {
                    const string relativePath = "output2.txt";
                    string absolutePath = Path.Combine(tempDir.FullName, relativePath);

                    using (var stream = StreamUtils.GetOutputStream(relativePath))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.Write(content);
                    }

                    // Verify file was written correctly
                    assertTrue(File.Exists(absolutePath));
                    string readContent = File.ReadAllText(absolutePath);
                    assertEquals(content, readContent);
                });
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

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestGetInputOutputStreamWithCompression()
        {
            // Create temp files with different extensions
            var tempDir = CreateTempDir("streamutils_compression");
            string[] fileExtensions = { ".txt", ".gz", ".bz2" };

            try
            {
                foreach (string ext in fileExtensions)
                {
                    string tempFilePath = Path.Combine(tempDir.FullName, "test" + ext);
                    string content = "Test content for compression " + ext;

                    // Write using GetOutputStream (handles compression based on extension)
                    using (var stream = StreamUtils.GetOutputStream(tempFilePath))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.Write(content);
                    }

                    // Verify file exists
                    assertTrue(File.Exists(tempFilePath));

                    // Read using GetInputStream (handles decompression based on extension)
                    using (var stream = StreamUtils.GetInputStream(tempFilePath))
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string readContent = reader.ReadToEnd();
                        assertEquals("Content mismatch for extension: " + ext, content, readContent);
                    }
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
