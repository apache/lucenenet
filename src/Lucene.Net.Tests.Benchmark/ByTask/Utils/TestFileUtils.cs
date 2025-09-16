using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

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

    public class TestFileUtils : LuceneTestCase
    {
        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestFullyDeleteWithStringPath()
        {
            // Create a temp directory with files
            DirectoryInfo tempDir = CreateTempDir("testFullyDelete");
            string tempDirPath = tempDir.FullName;

            // Create some files in the directory
            File.WriteAllText(Path.Combine(tempDirPath, "file1.txt"), "content1");
            File.WriteAllText(Path.Combine(tempDirPath, "file2.txt"), "content2");

            // Create a subdirectory with a file
            string subDirPath = Path.Combine(tempDirPath, "subdir");
            Directory.CreateDirectory(subDirPath);
            File.WriteAllText(Path.Combine(subDirPath, "file3.txt"), "content3");

            // Verify directory exists with files
            assertTrue(Directory.Exists(tempDirPath));
            assertTrue(File.Exists(Path.Combine(tempDirPath, "file1.txt")));
            assertTrue(File.Exists(Path.Combine(tempDirPath, "file2.txt")));
            assertTrue(Directory.Exists(subDirPath));
            assertTrue(File.Exists(Path.Combine(subDirPath, "file3.txt")));

            // Test FullyDelete with string path
            bool result = FileUtils.FullyDelete(tempDirPath);

            // Verify deletion was successful
            assertTrue(result);
            assertFalse(Directory.Exists(tempDirPath));
            assertFalse(File.Exists(Path.Combine(tempDirPath, "file1.txt")));
            assertFalse(File.Exists(Path.Combine(tempDirPath, "file2.txt")));
            assertFalse(Directory.Exists(subDirPath));
            assertFalse(File.Exists(Path.Combine(subDirPath, "file3.txt")));
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestFullyDeleteNonExistentStringPath()
        {
            string nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + System.Guid.NewGuid().ToString());

            // Verify directory doesn't exist
            assertFalse(Directory.Exists(nonExistentPath));

            // Test FullyDelete with non-existent path
            bool result = FileUtils.FullyDelete(nonExistentPath);

            // Should return true since the directory doesn't exist (nothing to delete)
            assertTrue(result);
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestFullyDeleteWithDirectoryInfo()
        {
            // Create a temp directory with files
            DirectoryInfo tempDir = CreateTempDir("testFullyDeleteDirInfo");

            // Create some files in the directory
            File.WriteAllText(Path.Combine(tempDir.FullName, "file1.txt"), "content1");
            File.WriteAllText(Path.Combine(tempDir.FullName, "file2.txt"), "content2");

            // Verify directory exists with files
            assertTrue(tempDir.Exists);
            assertTrue(File.Exists(Path.Combine(tempDir.FullName, "file1.txt")));
            assertTrue(File.Exists(Path.Combine(tempDir.FullName, "file2.txt")));

            // Test FullyDelete with DirectoryInfo (original overload)
            bool result = FileUtils.FullyDelete(tempDir);

            // Verify deletion was successful
            assertTrue(result);
            assertFalse(Directory.Exists(tempDir.FullName));
            assertFalse(File.Exists(Path.Combine(tempDir.FullName, "file1.txt")));
            assertFalse(File.Exists(Path.Combine(tempDir.FullName, "file2.txt")));
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestFullyDeleteWithRelativeStringPath()
        {
            // Create a temp base directory
            DirectoryInfo tempBase = CreateTempDir("testFullyDeleteRelative");

            try
            {
                // Create subdirectory structure
                string subDir1 = Path.Combine(tempBase.FullName, "subdir1");
                string subDir2 = Path.Combine(subDir1, "subdir2");
                Directory.CreateDirectory(subDir2);
                File.WriteAllText(Path.Combine(subDir2, "file.txt"), "content");

                // Use SystemEnvironment to safely change current directory
                SystemEnvironment.WithCurrentDirectory(tempBase.FullName, () =>
                {
                    // Verify structure exists
                    assertTrue(Directory.Exists("subdir1"));
                    assertTrue(Directory.Exists("subdir1/subdir2"));
                    assertTrue(File.Exists("subdir1/subdir2/file.txt"));

                    // Test FullyDelete with relative path
                    bool result = FileUtils.FullyDelete("subdir1");

                    // Verify deletion was successful
                    assertTrue(result);
                    assertFalse(Directory.Exists("subdir1"));
                    assertFalse(Directory.Exists(subDir1));
                    assertFalse(Directory.Exists(subDir2));

                    // Test with "./" prefix
                    string subDir3 = Path.Combine(tempBase.FullName, "subdir3");
                    Directory.CreateDirectory(subDir3);
                    File.WriteAllText(Path.Combine(subDir3, "file2.txt"), "content2");

                    assertTrue(Directory.Exists("./subdir3"));
                    result = FileUtils.FullyDelete("./subdir3");
                    assertTrue(result);
                    assertFalse(Directory.Exists("./subdir3"));
                    assertFalse(Directory.Exists(subDir3));
                });
            }
            finally
            {
                // Clean up base directory
                if (Directory.Exists(tempBase.FullName))
                {
                    Directory.Delete(tempBase.FullName, true);
                }
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestFullyDeleteEmptyDirectoryWithStringPath()
        {
            // Create an empty temp directory
            DirectoryInfo tempDir = CreateTempDir("testFullyDeleteEmpty");
            string tempDirPath = tempDir.FullName;

            // Verify directory exists and is empty
            assertTrue(Directory.Exists(tempDirPath));
            assertEquals(0, Directory.GetFiles(tempDirPath).Length);
            assertEquals(0, Directory.GetDirectories(tempDirPath).Length);

            // Test FullyDelete with string path
            bool result = FileUtils.FullyDelete(tempDirPath);

            // Verify deletion was successful
            assertTrue(result);
            assertFalse(Directory.Exists(tempDirPath));
        }
    }
}
