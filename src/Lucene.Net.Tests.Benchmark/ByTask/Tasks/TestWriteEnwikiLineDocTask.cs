using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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

    public class TestWriteEnwikiLineDocTask : LuceneTestCase
    {
        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestCategoriesLineFileWithStringPath()
        {
            // Test with relative path
            string fileName = "data/document.txt";
            string result = WriteEnwikiLineDocTask.CategoriesLineFile(fileName);
            assertEquals(Path.Combine("data", "categories-document.txt"), result);

            // Test with no directory
            fileName = "document.txt";
            result = WriteEnwikiLineDocTask.CategoriesLineFile(fileName);
            assertEquals("categories-document.txt", result);

            // Test with complex path
            fileName = "../parent/subdir/document.txt";
            result = WriteEnwikiLineDocTask.CategoriesLineFile(fileName);
            assertEquals(Path.Combine("../parent/subdir", "categories-document.txt"), result);

            // Test with Windows-style absolute path
            if (Path.DirectorySeparatorChar == '\\')
            {
                fileName = @"C:\Users\test\document.txt";
                result = WriteEnwikiLineDocTask.CategoriesLineFile(fileName);
                assertEquals(@"C:\Users\test\categories-document.txt", result);
            }
            // Test with Unix-style absolute path
            else
            {
                fileName = "/home/test/document.txt";
                result = WriteEnwikiLineDocTask.CategoriesLineFile(fileName);
                assertEquals("/home/test/categories-document.txt", result);
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestCategoriesLineFileWithRelativePath()
        {
            // Create a temp directory structure
            var tempDir = CreateTempDir("writeEnwikiTest");

            try
            {
                // Use SystemEnvironment to safely change current directory
                SystemEnvironment.WithCurrentDirectory(tempDir.FullName, () =>
                {
                    // Test with relative path from current directory
                    string fileName = "subdir/document.txt";
                    string result = WriteEnwikiLineDocTask.CategoriesLineFile(fileName);
                    assertEquals(Path.Combine("subdir", "categories-document.txt"), result);

                    // Test with "./" prefix
                    fileName = "./document.txt";
                    result = WriteEnwikiLineDocTask.CategoriesLineFile(fileName);
                    assertEquals(Path.Combine(".", "categories-document.txt"), result);

                    // Test with parent directory reference
                    fileName = "../document.txt";
                    result = WriteEnwikiLineDocTask.CategoriesLineFile(fileName);
                    assertEquals(Path.Combine("..", "categories-document.txt"), result);
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
        public virtual void TestCategoriesLineFileComparisonWithFileInfo()
        {
            // Compare the string version with the FileInfo version
            string[] testPaths = {
                "document.txt",
                "data/document.txt",
                "relative/path/document.txt"
            };

            foreach (string path in testPaths)
            {
                // Get result from string version
                string stringResult = WriteEnwikiLineDocTask.CategoriesLineFile(path);

                // Get result from FileInfo version
                FileInfo fileInfo = new FileInfo(path);
                FileInfo fileInfoResult = WriteEnwikiLineDocTask.CategoriesLineFile(fileInfo);

                // The file names should match
                assertEquals(Path.GetFileName(stringResult), fileInfoResult.Name);

                // The directory paths should be equivalent
                string stringDir = Path.GetDirectoryName(stringResult);
                string fileInfoDir = fileInfoResult.DirectoryName;

                // Handle null directory case
                if (string.IsNullOrEmpty(stringDir))
                {
                    assertTrue(fileInfoDir == null || fileInfoDir == Environment.CurrentDirectory);
                }
                else
                {
                    // Normalize paths for comparison
                    string normalizedStringDir = Path.GetFullPath(stringDir);
                    string normalizedFileInfoDir = fileInfoDir ?? Environment.CurrentDirectory;
                    assertEquals(normalizedStringDir, normalizedFileInfoDir);
                }
            }
        }
    }
}
