using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

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
        private readonly static string platformId = RuntimeInformation.FrameworkDescription.Replace('.', '-');

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
            const string dots = "..";
            string @base = tempDirectory.GetCanonicalPath();
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
                using (FileStream _ = new FileStream(f1.FullName, FileMode.CreateNew, FileAccess.Write))
                { }
                FileInfo f2 = null, f3 = null;
                DirectoryInfo dir2 = null;
                try
                {
                    string dirName1 = f1.GetCanonicalPath();
                    FileInfo f4 = new FileInfo(Path.Combine(testdir.FullName, "longfi~1.tst"));
                    /*
                     * If the "short file name" doesn't exist, then assume that the
                     * 8.3 file name compatibility is disabled.
                     */
                    if (f4.Exists)
                    {
                        string dirName2 = f4.GetCanonicalPath();
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
                        using (FileStream _ = new FileStream(f2.FullName, FileMode.CreateNew, FileAccess.Write))
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
                    f2?.Delete();
                    dir2?.Delete();
                    testdir.Delete();
                }
            }
        }

#if FEATURE_FILE_CREATESYMBOLICLINK
        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathResolvesSymbolicLinks()
        {
            AssumeTrue("Symbolic links are only resolved on Unix", Path.DirectorySeparatorChar == '/');

            DirectoryInfo real = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "real"));
            DirectoryInfo sub = Directory.CreateDirectory(Path.Combine(real.FullName, "sub"));
            string realFile = Path.Combine(sub.FullName, "file.txt");
            File.WriteAllText(realFile, "x");

            // Absolute and relative link targets
            string absLink = Path.Combine(tempDirectory.FullName, "abs-link");
            Directory.CreateSymbolicLink(absLink, real.FullName);
            string relLink = Path.Combine(tempDirectory.FullName, "rel-link");
            Directory.CreateSymbolicLink(relLink, "real");

            string expectedDir = real.GetCanonicalPath();
            string expectedFile = new FileInfo(realFile).GetCanonicalPath();

            foreach (string link in new[] { absLink, relLink })
            {
                assertEquals(expectedDir, new DirectoryInfo(link).GetCanonicalPath());
                assertEquals(expectedFile, new FileInfo(Path.Combine(link, "sub", "file.txt")).GetCanonicalPath());
            }

            // A link to a file
            string fileLink = Path.Combine(tempDirectory.FullName, "file-link.txt");
            File.CreateSymbolicLink(fileLink, realFile);
            assertEquals(expectedFile, new FileInfo(fileLink).GetCanonicalPath());

            // A chain of links, where the last one is reached through a linked directory
            string chainLink = Path.Combine(sub.FullName, "chain-link.txt");
            File.CreateSymbolicLink(chainLink, fileLink);
            assertEquals(expectedFile, new FileInfo(Path.Combine(absLink, "sub", "chain-link.txt")).GetCanonicalPath());
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathSymbolicLinkToNonExistentPath()
        {
            AssumeTrue("Symbolic links are only resolved on Unix", Path.DirectorySeparatorChar == '/');

            DirectoryInfo real = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "real"));
            string link = Path.Combine(tempDirectory.FullName, "link");
            Directory.CreateSymbolicLink(link, real.FullName);

            // Segments below the link that don't exist yet are kept as-is
            assertEquals(
                Path.Combine(real.GetCanonicalPath(), "not", "created", "yet.txt"),
                new FileInfo(Path.Combine(link, "not", "created", "yet.txt")).GetCanonicalPath());

            // A dangling link resolves to its target
            string dangling = Path.Combine(tempDirectory.FullName, "dangling");
            Directory.CreateSymbolicLink(dangling, Path.Combine(real.FullName, "missing"));
            assertEquals(
                Path.Combine(real.GetCanonicalPath(), "missing"),
                new DirectoryInfo(dangling).GetCanonicalPath());
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathSymbolicLinkWithParentReference()
        {
            AssumeTrue("Symbolic links are only resolved on Unix", Path.DirectorySeparatorChar == '/');

            // A relative link target that climbs out of the link's directory (sub/link -> ../real)
            DirectoryInfo real = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "real"));
            DirectoryInfo sub = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "sub"));
            string link = Path.Combine(sub.FullName, "link");
            Directory.CreateSymbolicLink(link, Path.Combine("..", "real"));

            assertEquals(real.GetCanonicalPath(), new DirectoryInfo(link).GetCanonicalPath());
            assertEquals(
                Path.Combine(real.GetCanonicalPath(), "file.txt"),
                new FileInfo(Path.Combine(link, "file.txt")).GetCanonicalPath());
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathSymbolicLinkCycle()
        {
            AssumeTrue("Symbolic links are only resolved on Unix", Path.DirectorySeparatorChar == '/');

            // a -> a/b never terminates. Java's getCanonicalPath() throws IOException
            // ("Too many levels of symbolic links") here, and so do we.
            string cycle = Path.Combine(tempDirectory.FullName, "a");
            Directory.CreateSymbolicLink(cycle, Path.Combine(cycle, "b"));
            string path = Path.Combine(cycle, "file.txt");

            Assert.Throws<IOException>(() => new FileInfo(path).GetCanonicalPath());
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathSymbolicLinkNotCached()
        {
            AssumeTrue("Symbolic links are only resolved on Unix", Path.DirectorySeparatorChar == '/');

            // The symlink-swap deployment pattern: current -> v1, build v2, repoint current.
            // The result must follow the link as it is now, not as it was the first time we looked.
            DirectoryInfo v1 = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "v1"));
            DirectoryInfo v2 = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "v2"));
            string current = Path.Combine(tempDirectory.FullName, "current");
            Directory.CreateSymbolicLink(current, v1.FullName);
            assertEquals(v1.GetCanonicalPath(), new DirectoryInfo(current).GetCanonicalPath());

            Directory.Delete(current);
            Directory.CreateSymbolicLink(current, v2.FullName);
            assertEquals(v2.GetCanonicalPath(), new DirectoryInfo(current).GetCanonicalPath());
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathLongSymbolicLinkTarget()
        {
            AssumeTrue("Symbolic links are only resolved on Unix", Path.DirectorySeparatorChar == '/');

            // The resolved path is several times longer than the path we started with,
            // so it cannot fit in a buffer sized from the input alone.
            string segment = new string('d', 100);
            DirectoryInfo target = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "real", segment, segment, segment));
            string link = Path.Combine(tempDirectory.FullName, "l");
            Directory.CreateSymbolicLink(link, target.FullName);

            assertEquals(target.GetCanonicalPath(), new DirectoryInfo(link).GetCanonicalPath());
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathSymbolicLinkCycleOfTwo()
        {
            AssumeTrue("Symbolic links are only resolved on Unix", Path.DirectorySeparatorChar == '/');

            string a = Path.Combine(tempDirectory.FullName, "a");
            string b = Path.Combine(tempDirectory.FullName, "b");
            Directory.CreateSymbolicLink(a, "b");
            Directory.CreateSymbolicLink(b, "a");

            Assert.Throws<IOException>(() => new FileInfo(Path.Combine(a, "file.txt")).GetCanonicalPath());
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathSymbolicLinkTargetWithCurrentDirectory()
        {
            AssumeTrue("Symbolic links are only resolved on Unix", Path.DirectorySeparatorChar == '/');

            DirectoryInfo real = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "real"));
            string link = Path.Combine(tempDirectory.FullName, "link");
            Directory.CreateSymbolicLink(link, "." + Path.DirectorySeparatorChar + "real" + Path.DirectorySeparatorChar);

            assertEquals(real.GetCanonicalPath(), new DirectoryInfo(link).GetCanonicalPath());
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathSymbolicLinkToRoot()
        {
            AssumeTrue("Symbolic links are only resolved on Unix", Path.DirectorySeparatorChar == '/');

            string rootLink = Path.Combine(tempDirectory.FullName, "root");
            Directory.CreateSymbolicLink(rootLink, Path.DirectorySeparatorChar.ToString());
            string expected = tempDirectory.GetCanonicalPath();

            assertEquals(expected, new DirectoryInfo(rootLink + expected).GetCanonicalPath());
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathParentReferenceBeforeSymbolicLink()
        {
            AssumeTrue("Symbolic links are only resolved on Unix", Path.DirectorySeparatorChar == '/');

            // Pins the documented deviation from Java: FullName removes ".." lexically before we see the path,
            // so through the FileSystemInfo overloads "link/../x" is a sibling of the link, not of its target.
            // The span overloads get the unnormalized path and resolve it like Java does.
            DirectoryInfo real = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "other", "real"));
            string link = Path.Combine(tempDirectory.FullName, "link");
            Directory.CreateSymbolicLink(link, real.FullName);
            string @base = tempDirectory.GetCanonicalPath();
            char sep = Path.DirectorySeparatorChar;

            assertEquals(@base + sep + "x", new FileInfo(Path.Combine(link, "..", "x")).GetCanonicalPath());
            assertEquals(@base + sep + "other" + sep + "x", Canonicalize(@base + sep + "link" + sep + ".." + sep + "x"));
        }

        [Test, LuceneNetSpecific]
        public void TestEqualsCanonicalPathSymbolicLink()
        {
            AssumeTrue("Symbolic links are only resolved on Unix", Path.DirectorySeparatorChar == '/');

            DirectoryInfo real = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "real"));
            DirectoryInfo other = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "other"));
            var link = new DirectoryInfo(Path.Combine(tempDirectory.FullName, "link"));
            Directory.CreateSymbolicLink(link.FullName, real.FullName);

            Assert.IsTrue(link.EqualsCanonicalPath(real));
            Assert.IsTrue(real.EqualsCanonicalPath(link));
            Assert.IsFalse(link.EqualsCanonicalPath(other));
        }
#endif

        [Test, LuceneNetSpecific]
        public void TestEqualsCanonicalPath()
        {
            string @base = tempDirectory.GetCanonicalPath();
            char sep = Path.DirectorySeparatorChar;
            var viaParent = new FileInfo(@base + sep + "Temp" + sep + ".." + sep + "temp.tst");
            var direct = new FileInfo(@base + sep + "temp.tst");
            var other = new FileInfo(@base + sep + "other.tst");

            Assert.IsTrue(viaParent.EqualsCanonicalPath(direct));
            Assert.IsTrue(direct.EqualsCanonicalPath(viaParent));
            Assert.IsTrue(direct.EqualsCanonicalPath(direct));
            Assert.IsFalse(viaParent.EqualsCanonicalPath(other));

            // The span overload takes absolute paths and removes "." and ".." itself
            Assert.IsTrue(FileSupport.EqualsCanonicalPath(
                (@base + sep + "Temp" + sep + ".." + sep + "temp.tst").AsSpan(),
                (@base + sep + "." + sep + "temp.tst").AsSpan()));
            Assert.IsFalse(FileSupport.EqualsCanonicalPath(direct.FullName.AsSpan(), other.FullName.AsSpan()));

            if (sep == '\\')
            {
                // Drive letters are normalized to upper case on Windows
                Assert.IsTrue(FileSupport.EqualsCanonicalPath(@"c:\testing\on\Windows".AsSpan(), @"C:\testing\on\Windows".AsSpan()));
            }
        }

        [Test, LuceneNetSpecific]
        public void TestEqualsCanonicalPathNullPath()
        {
            FileSystemInfo path = null;
            var other = new FileInfo(Path.Combine(tempDirectory.FullName, "temp.tst"));

            Assert.Throws<ArgumentNullException>(() => path.EqualsCanonicalPath(other));
            Assert.Throws<ArgumentNullException>(() => other.EqualsCanonicalPath(path));
        }

        [Test, LuceneNetSpecific]
        public void TestTryGetCanonicalPathBytes()
        {
            char sep = Path.DirectorySeparatorChar;
            string absPath = tempDirectory.GetCanonicalPath() + sep + "Temp" + sep + ".." + sep + "temp.tst";
            string expected = new FileInfo(absPath).GetCanonicalPath();

            byte[] destination = new byte[FileSupport.GetMaxCanonicalPathByteCount(absPath.Length)];
            Assert.IsTrue(FileSupport.TryGetCanonicalPathBytes(absPath.AsSpan(), destination, out int bytesWritten));
            Assert.AreEqual(expected, Encoding.UTF8.GetString(destination, 0, bytesWritten));

            // Returns false (rather than throwing) when the destination is too small
            Assert.IsFalse(FileSupport.TryGetCanonicalPathBytes(absPath.AsSpan(), new byte[1], out bytesWritten));
            Assert.AreEqual(0, bytesWritten);
        }

        [Test, LuceneNetSpecific]
        public void TestTryGetCanonicalPathBytesDotSegments()
        {
            // Only a segment that is exactly "." or ".." is special. Harmony counted dots anywhere in a
            // segment, so "a." lost its dot and the separator after it, "a.." went up a level and "..." went up two.
            // Windows strips trailing dots from names itself, which is why FullName never showed this.
            string @base = tempDirectory.GetCanonicalPath();
            char sep = Path.DirectorySeparatorChar;

            assertEquals(@base + sep + "a." + sep + "y", Canonicalize(@base + sep + "a." + sep + "y"));
            assertEquals(@base + sep + "a.." + sep + "y", Canonicalize(@base + sep + "a.." + sep + "y"));
            assertEquals(@base + sep + "..." + sep + "y", Canonicalize(@base + sep + "..." + sep + "y"));
            assertEquals(@base + sep + ".a" + sep + "y", Canonicalize(@base + sep + ".a" + sep + "y"));
            assertEquals(@base + sep + "a.b", Canonicalize(@base + sep + "a.b" + sep));
            assertEquals(@base + sep + "y", Canonicalize(@base + sep + "." + sep + "y"));
            assertEquals(@base + sep + "y", Canonicalize(@base + sep + "x" + sep + ".." + sep + "y"));
            assertEquals(@base, Canonicalize(@base + sep + "x" + sep + ".."));
            assertEquals(@base, Canonicalize(@base + sep + "."));

            // ".." never goes above the root
            string root = Path.GetPathRoot(@base);
            assertEquals(root.TrimEnd(sep) + sep + "y", Canonicalize(root + ".." + sep + ".." + sep + "y"));
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathFileUsedAsDirectory()
        {
            // Nothing below a regular file can exist (ENOTDIR on Unix), so the rest of the path is kept as it is
            string file = Path.Combine(tempDirectory.FullName, "file.txt");
            File.WriteAllText(file, "x");
            char sep = Path.DirectorySeparatorChar;

            assertEquals(
                tempDirectory.GetCanonicalPath() + sep + "file.txt" + sep + "sub" + sep + "x.txt",
                new FileInfo(Path.Combine(file, "sub", "x.txt")).GetCanonicalPath());
        }

        [Test, LuceneNetSpecific]
        public void TestTryGetCanonicalPathBytesRequiresAbsolutePath()
        {
            Assert.Throws<ArgumentException>(() => FileSupport.TryGetCanonicalPathBytes(ReadOnlySpan<char>.Empty, new byte[16], out _));
            Assert.Throws<ArgumentException>(() => FileSupport.TryGetCanonicalPathBytes("relative".AsSpan(), new byte[64], out _));
            if (Path.DirectorySeparatorChar == '\\')
            {
                Assert.Throws<ArgumentException>(() => FileSupport.TryGetCanonicalPathBytes(@"c:drive-relative".AsSpan(), new byte[64], out _));
            }
            Assert.Throws<ArgumentException>(() => FileSupport.EqualsCanonicalPath("relative".AsSpan(), tempDirectory.FullName.AsSpan()));
            Assert.Throws<ArgumentOutOfRangeException>(() => FileSupport.GetMaxCanonicalPathByteCount(-1));
        }

        private static string Canonicalize(string absPath)
        {
            byte[] destination = new byte[FileSupport.GetMaxCanonicalPathByteCount(absPath.Length)];
            Assert.IsTrue(FileSupport.TryGetCanonicalPathBytes(absPath.AsSpan(), destination, out int bytesWritten));
            return Encoding.UTF8.GetString(destination, 0, bytesWritten);
        }

        [Test, LuceneNetSpecific]
        public void TestGetMaxCanonicalPathByteCount()
        {
            // Must cover the worst case UTF-8 expansion of the input
            Assert.IsTrue(FileSupport.GetMaxCanonicalPathByteCount(0) >= Encoding.UTF8.GetMaxByteCount(0));
            Assert.IsTrue(FileSupport.GetMaxCanonicalPathByteCount(260) >= Encoding.UTF8.GetMaxByteCount(260));
        }

        [Test, LuceneNetSpecific]
        public void TestGetCanonicalPathNullPath()
        {
            FileSystemInfo path = null;

            Assert.Throws<ArgumentNullException>(() => path.GetCanonicalPath());
        }

        private static string addTrailingSlash(string path)
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
                const string path = @"f:\testing\on\Windows";
                const string expected = @"F:\testing\on\Windows";

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
