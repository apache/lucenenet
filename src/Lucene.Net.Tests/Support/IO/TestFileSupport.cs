using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Lucene.Net.Support.IO
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

    public class TestFileSupport : LuceneTestCase
    {
        private static String platformId = RuntimeInformation.FrameworkDescription.Replace('.', '-');

        /** Location to store tests in */
        private DirectoryInfo tempDirectory;

        public override void SetUp()
        {
            base.SetUp();
            tempDirectory = CreateTempDir(this.GetType().Name);
        }

        public override void TearDown()
        {
            if (tempDirectory != null)
            {
                Directory.Delete(tempDirectory.FullName, true);
                tempDirectory = null;
            }
            base.TearDown();
        }

        [Test, LuceneNetSpecific]
        public void TestCreateRandomFile()
        {
            var dir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "testrandomfile"));

            var file1 = FileSupport.CreateTempFile("foo", "bar", dir);
            var file2 = FileSupport.CreateTempFile("foo", "bar", dir);

            Assert.AreNotEqual(file1.FullName, file2.FullName);
        }

        [Test, LuceneNetSpecific]
        public void TestCreateRandomFileAsStream()
        {
            var dir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "testrandomfile"));

            using (var file1 = FileSupport.CreateTempFileAsStream("foo", "bar", dir, new FileStreamOptions { Access = FileAccess.Write }))
            using (var file2 = FileSupport.CreateTempFileAsStream("foo", "bar", dir, new FileStreamOptions { Access = FileAccess.Write }))
            {
                Assert.AreNotEqual(file1.Name, file2.Name);
            } 
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPath()
        {
            // Should work for Unix/Windows.
            String dots = "..";
            String @base = tempDirectory.GetCanonicalPath();
            @base = addTrailingSlash(@base);
            FileInfo f = new FileInfo(Path.Combine(@base, "temp.tst"));

            assertEquals("Test 1: Incorrect Path Returned.", @base + "temp.tst", f
                    .GetCanonicalPath());
            f = new FileInfo(@base + "Temp" + Path.DirectorySeparatorChar + dots + Path.DirectorySeparatorChar + "temp.tst");
            assertEquals("Test 2: Incorrect Path Returned.", @base + "temp.tst", f
                    .GetCanonicalPath());


            // Finding a non-existent directory for tests 3 and 4
            // This is necessary because getCanonicalPath is case sensitive and
            // could cause a failure in the test if the directory exists but with
            // different case letters (e.g "Temp" and "temp")
            int dirNumber = 1;
            bool dirExists = true;
            DirectoryInfo dir1 = new DirectoryInfo(Path.Combine(@base, dirNumber.ToString(CultureInfo.InvariantCulture)));
            while (dirExists)
            {
                if (dir1.Exists)
                {
                    dirNumber++;
                    dir1 = new DirectoryInfo(Path.Combine(@base, dirNumber.ToString(CultureInfo.InvariantCulture)));
                }
                else
                {
                    dirExists = false;
                }
            }
            f = new FileInfo(@base + dirNumber + Path.DirectorySeparatorChar + dots + Path.DirectorySeparatorChar + dirNumber
                    + Path.DirectorySeparatorChar + "temp.tst");
            assertEquals("Test 3: Incorrect Path Returned.", @base + dirNumber
                    + Path.DirectorySeparatorChar + "temp.tst", f.GetCanonicalPath());
            f = new FileInfo(@base + dirNumber + Path.DirectorySeparatorChar + "Temp" + Path.DirectorySeparatorChar + dots + Path.DirectorySeparatorChar
                    + "Test" + Path.DirectorySeparatorChar + "temp.tst");
            assertEquals("Test 4: Incorrect Path Returned.", @base + dirNumber
                    + Path.DirectorySeparatorChar + "Test" + Path.DirectorySeparatorChar + "temp.tst", f.GetCanonicalPath());

            f = new FileInfo(@base + "1234.567");
            assertEquals("Test 5: Incorrect Path Returned.", @base + "1234.567", f
                    .GetCanonicalPath());

            // Test for long file names on Windows
            bool onWindows = (Path.DirectorySeparatorChar == '\\');
            if (onWindows)
            {
                DirectoryInfo testdir = new DirectoryInfo(Path.Combine(@base, "long-" + platformId));
                testdir.Create();
                FileInfo f1 = new FileInfo(Path.Combine(testdir.FullName, "longfilename" + platformId + ".tst"));
                using (FileStream fos = new FileStream(f1.FullName, FileMode.CreateNew, FileAccess.Write))
                { }
                FileInfo f2 = null, f3 = null;
                DirectoryInfo dir2 = null;
                try
                {
                    String dirName1 = f1.GetCanonicalPath();
                    FileInfo f4 = new FileInfo(Path.Combine(testdir.FullName, "longfi~1.tst"));
                    /*
                     * If the "short file name" doesn't exist, then assume that the
                     * 8.3 file name compatibility is disabled.
                     */
                    if (f4.Exists)
                    {
                        String dirName2 = f4.GetCanonicalPath();
                        assertEquals("Test 6: Incorrect Path Returned.", dirName1,
                                dirName2);
                        dir2 = new DirectoryInfo(Path.Combine(testdir.FullName, "longdirectory" + platformId));
                        if (!dir2.Exists)
                        {
                            try
                            {
                                dir2.Create();
                            }
                            catch
                            {
                            }
                            finally
                            {
                                if (!Directory.Exists(dir2.FullName))
                                    fail("Could not create dir: " + dir2);
                            }

                        }
                        f2 = new FileInfo(testdir.FullName + Path.DirectorySeparatorChar + "longdirectory"
                                + platformId + Path.DirectorySeparatorChar + "Test" + Path.DirectorySeparatorChar + dots
                                + Path.DirectorySeparatorChar + "longfilename.tst");
                        using (FileStream fos2 = new FileStream(f2.FullName, FileMode.CreateNew, FileAccess.Write))
                        { }
                        dirName1 = f2.GetCanonicalPath();
                        f3 = new FileInfo(testdir.FullName + Path.DirectorySeparatorChar + "longdi~1"
                                + Path.DirectorySeparatorChar + "Test" + Path.DirectorySeparatorChar + dots + Path.DirectorySeparatorChar
                                + "longfi~1.tst");
                        dirName2 = f3.GetCanonicalPath();
                        assertEquals("Test 7: Incorrect Path Returned.", dirName1,
                                dirName2);
                    }
                }
                finally
                {
                    f1.Delete();
                    if (f2 != null)
                    {
                        f2.Delete();
                    }
                    if (dir2 != null)
                    {
                        dir2.Delete();
                    }
                    testdir.Delete();
                }
            }
        }

        private static String addTrailingSlash(String path)
        {
            if (Path.DirectorySeparatorChar == path[path.Length - 1])
            {
                return path;
            }
            return path + Path.DirectorySeparatorChar;
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathDriveLetterNormalization()
        {
            bool onWindows = (Path.DirectorySeparatorChar == '\\');
            if (onWindows)
            {
                var path = @"f:\testing\on\Windows";
                var expected = @"F:\testing\on\Windows";

                var dir = new DirectoryInfo(path);

                assertEquals(expected, dir.GetCanonicalPath());
            }
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathDriveLetter()
        {
            bool onWindows = (Path.DirectorySeparatorChar == '\\');
            if (onWindows)
            {
                var path = new FileInfo(@"c:\").GetCanonicalPath();
                if (path.Length > 3)
                    fail("Drive letter incorrectly represented");
            }
        }
    }
}
