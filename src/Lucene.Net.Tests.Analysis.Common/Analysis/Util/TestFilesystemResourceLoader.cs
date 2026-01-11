// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;

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

    public class TestFilesystemResourceLoader : LuceneTestCase
    {
        private void assertNotFound(IResourceLoader rl)
        {
            try
            {
                IOUtils.DisposeWhileHandlingException(rl.OpenResource("/this-directory-really-really-really-should-not-exist/foo/bar.txt"));
                fail("The resource does not exist, should fail!");
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                // pass
            }
            try
            {
                rl.NewInstance<TokenFilterFactory>("org.apache.lucene.analysis.FooBarFilterFactory"); // LUCENENET TODO: This test is invalid because this type name doesn't work in .NET
                fail("The class does not exist, should fail!");
            }
            catch (Exception iae) when (iae.IsRuntimeException())
            {
                // pass
            }
        }

        private void assertClasspathDelegation(IResourceLoader rl)
        {
            //var englishStopText = System.IO.Path.Combine(analysisCommonFolder.FullName, @"Analysis\Snowball\english_stop.txt");
            // LUCENENET specific - rather than being completely dependent on the location of the file
            // in the file system, we use the embedded resource to write the file to a known location
            // before passing it to our resource loader.
            string englishStopFile = "english_stop.txt";
            var file = CreateTempFile(System.IO.Path.GetFileNameWithoutExtension(englishStopFile), System.IO.Path.GetExtension(englishStopFile));
            using (var stream = typeof(Snowball.SnowballFilter).FindAndGetManifestResourceStream(englishStopFile))
            using (var outputStream = new FileStream(file.FullName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                stream.CopyTo(outputStream);
            }
            // try a stopwords file from classpath
            CharArraySet set = WordlistLoader.GetSnowballWordSet(new StreamReader(rl.OpenResource(file.FullName), Encoding.UTF8), TEST_VERSION_CURRENT);
            assertTrue(set.contains("you"));
            // try to load a class; we use string comparison because classloader may be different...
            assertEquals("Lucene.Net.Analysis.Util.RollingCharBuffer", rl.NewInstance<object>("Lucene.Net.Analysis.Util.RollingCharBuffer").ToString());
            // theoretically classes should also be loadable:
            //IOUtils.CloseWhileHandlingException(rl.OpenResource("java/lang/String.class")); // LUCENENET TODO: Not sure what the equivalent to this is (or if there is one).
        }

        [Test]
        public virtual void TestBaseDir()
        {
            DirectoryInfo @base = CreateTempDir("fsResourceLoaderBase");
            try
            {
                TextWriter os = new StreamWriter(new FileStream(System.IO.Path.Combine(@base.FullName, "template.txt"), FileMode.Create, FileAccess.Write), StandardCharsets.UTF_8);
                try
                {
                    os.Write("foobar\n");
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(os);
                }

                IResourceLoader rl = new FilesystemResourceLoader(@base);
                assertEquals("foobar", WordlistLoader.GetLines(rl.OpenResource("template.txt"), Encoding.UTF8).First());
                // Same with full path name:
                string fullPath = (new FileInfo(System.IO.Path.Combine(@base.FullName, "template.txt"))).ToString();
                assertEquals("foobar", WordlistLoader.GetLines(rl.OpenResource(fullPath), Encoding.UTF8).First());
                assertClasspathDelegation(rl);
                assertNotFound(rl);

                // now use RL without base dir:
                rl = new FilesystemResourceLoader();
                assertEquals("foobar", WordlistLoader.GetLines(rl.OpenResource(new FileInfo(System.IO.Path.Combine(@base.FullName, "template.txt")).FullName), Encoding.UTF8).First());
                assertClasspathDelegation(rl);
                assertNotFound(rl);
            }
            finally
            {
                // clean up
                foreach (var file in @base.EnumerateFiles())
                {
                    file.Delete();
                }
                @base.Delete();
            }
        }

        [Test]
        public virtual void TestDelegation()
        {
            IResourceLoader rl = new FilesystemResourceLoader((string)null, new StringMockResourceLoader("foobar\n"));
            assertEquals("foobar", WordlistLoader.GetLines(rl.OpenResource("template.txt"), Encoding.UTF8).First());
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestBaseDirWithString()
        {
            DirectoryInfo @base = CreateTempDir("fsResourceLoaderBaseString");
            try
            {
                TextWriter os = new StreamWriter(new FileStream(System.IO.Path.Combine(@base.FullName, "template.txt"), FileMode.Create, FileAccess.Write), StandardCharsets.UTF_8);
                try
                {
                    os.Write("foobar\n");
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(os);
                }

                // Test with string path instead of DirectoryInfo
                IResourceLoader rl = new FilesystemResourceLoader(@base.FullName);
                assertEquals("foobar", WordlistLoader.GetLines(rl.OpenResource("template.txt"), Encoding.UTF8).First());
                // Same with full path name:
                string fullPath = (new FileInfo(System.IO.Path.Combine(@base.FullName, "template.txt"))).ToString();
                assertEquals("foobar", WordlistLoader.GetLines(rl.OpenResource(fullPath), Encoding.UTF8).First());
                assertClasspathDelegation(rl);
                assertNotFound(rl);

                // now use RL without base dir:
                rl = new FilesystemResourceLoader((string)null);
                assertEquals("foobar", WordlistLoader.GetLines(rl.OpenResource(new FileInfo(System.IO.Path.Combine(@base.FullName, "template.txt")).FullName), Encoding.UTF8).First());
                assertClasspathDelegation(rl);
                assertNotFound(rl);
            }
            finally
            {
                // clean up
                foreach (var file in @base.EnumerateFiles())
                {
                    file.Delete();
                }
                @base.Delete();
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestDelegationWithString()
        {
            IResourceLoader rl = new FilesystemResourceLoader((string)null, new StringMockResourceLoader("foobar\n"));
            assertEquals("foobar", WordlistLoader.GetLines(rl.OpenResource("template.txt"), Encoding.UTF8).First());

            // Test with string base directory and delegation
            DirectoryInfo @base = CreateTempDir("fsResourceLoaderDelegationString");
            try
            {
                TextWriter os = new StreamWriter(new FileStream(System.IO.Path.Combine(@base.FullName, "template2.txt"), FileMode.Create, FileAccess.Write), StandardCharsets.UTF_8);
                try
                {
                    os.Write("baz\n");
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(os);
                }

                rl = new FilesystemResourceLoader(@base.FullName, new StringMockResourceLoader("fallback\n"));
                assertEquals("baz", WordlistLoader.GetLines(rl.OpenResource("template2.txt"), Encoding.UTF8).First());
                // Test delegation when file doesn't exist in base dir
                assertEquals("fallback", WordlistLoader.GetLines(rl.OpenResource("nonexistent.txt"), Encoding.UTF8).First());
            }
            finally
            {
                // clean up
                foreach (var file in @base.EnumerateFiles())
                {
                    file.Delete();
                }
                @base.Delete();
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestRelativePathsWithString()
        {
            // Create a directory structure: base/subdir/file.txt
            DirectoryInfo @base = CreateTempDir("fsResourceLoaderRelativePath");
            DirectoryInfo subdir = new DirectoryInfo(System.IO.Path.Combine(@base.FullName, "subdir"));
            subdir.Create();

            try
            {
                // Create a file in the subdirectory
                TextWriter os = new StreamWriter(new FileStream(System.IO.Path.Combine(subdir.FullName, "file.txt"), FileMode.Create, FileAccess.Write), StandardCharsets.UTF_8);
                try
                {
                    os.Write("content\n");
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(os);
                }

                // Test with base directory using absolute path
                IResourceLoader rl = new FilesystemResourceLoader(@base.FullName);
                assertEquals("content", WordlistLoader.GetLines(rl.OpenResource("subdir/file.txt"), Encoding.UTF8).First());

                // Test with relative path from current directory
                SystemEnvironment.WithCurrentDirectory(@base.FullName, () =>
                {
                    // Test with relative path "." as base
                    IResourceLoader rl = new FilesystemResourceLoader(".");
                    assertEquals("content", WordlistLoader.GetLines(rl.OpenResource("subdir/file.txt"), Encoding.UTF8).First());

                    // Test with relative path "subdir" as base
                    rl = new FilesystemResourceLoader("subdir");
                    assertEquals("content", WordlistLoader.GetLines(rl.OpenResource("file.txt"), Encoding.UTF8).First());
                });

                // Test from parent directory
                SystemEnvironment.WithCurrentDirectory(@base.Parent?.FullName ?? throw new InvalidOperationException("Base directory has no parent"), () =>
                {
                    IResourceLoader rl = new FilesystemResourceLoader(@base.Name);
                    assertEquals("content", WordlistLoader.GetLines(rl.OpenResource("subdir/file.txt"), Encoding.UTF8).First());
                });

                // Test with relative path containing ".."
                SystemEnvironment.WithCurrentDirectory(subdir.FullName, () =>
                {
                    IResourceLoader rl = new FilesystemResourceLoader("..");
                    assertEquals("content", WordlistLoader.GetLines(rl.OpenResource("subdir/file.txt"), Encoding.UTF8).First());
                });
            }
            finally
            {
                // clean up
                foreach (var file in subdir.EnumerateFiles())
                {
                    file.Delete();
                }
                subdir.Delete();
                @base.Delete();
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestNestedRelativePathsWithString()
        {
            // Create a nested directory structure: base/level1/level2/file.txt
            DirectoryInfo @base = CreateTempDir("fsResourceLoaderNestedRelative");
            DirectoryInfo level1 = new DirectoryInfo(System.IO.Path.Combine(@base.FullName, "level1"));
            DirectoryInfo level2 = new DirectoryInfo(System.IO.Path.Combine(level1.FullName, "level2"));
            level1.Create();
            level2.Create();

            try
            {
                // Create files at different levels
                using (var fs = new FileStream(System.IO.Path.Combine(@base.FullName, "root.txt"), FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(fs, StandardCharsets.UTF_8))
                {
                    writer.Write("root content\n");
                }

                using (var fs = new FileStream(System.IO.Path.Combine(level1.FullName, "level1.txt"), FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(fs, StandardCharsets.UTF_8))
                {
                    writer.Write("level1 content\n");
                }

                using (var fs = new FileStream(System.IO.Path.Combine(level2.FullName, "level2.txt"), FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(fs, StandardCharsets.UTF_8))
                {
                    writer.Write("level2 content\n");
                }

                // Test from base directory with nested relative paths
                SystemEnvironment.WithCurrentDirectory(@base.FullName, () =>
                {
                    IResourceLoader rl = new FilesystemResourceLoader((string)null);
                    assertEquals("root content", WordlistLoader.GetLines(rl.OpenResource("root.txt"), Encoding.UTF8).First());
                    assertEquals("level1 content", WordlistLoader.GetLines(rl.OpenResource("level1/level1.txt"), Encoding.UTF8).First());
                    assertEquals("level2 content", WordlistLoader.GetLines(rl.OpenResource("level1/level2/level2.txt"), Encoding.UTF8).First());

                    // Test with relative base path "level1"
                    rl = new FilesystemResourceLoader("level1");
                    assertEquals("level1 content", WordlistLoader.GetLines(rl.OpenResource("level1.txt"), Encoding.UTF8).First());
                    assertEquals("level2 content", WordlistLoader.GetLines(rl.OpenResource("level2/level2.txt"), Encoding.UTF8).First());

                    // Test with nested relative base path
                    rl = new FilesystemResourceLoader("level1/level2");
                    assertEquals("level2 content", WordlistLoader.GetLines(rl.OpenResource("level2.txt"), Encoding.UTF8).First());
                });

                // Test with ".." in resource path from level2 directory
                SystemEnvironment.WithCurrentDirectory(level2.FullName, () =>
                {
                    IResourceLoader rl = new FilesystemResourceLoader((string)null);
                    assertEquals("level1 content", WordlistLoader.GetLines(rl.OpenResource("../level1.txt"), Encoding.UTF8).First());
                    assertEquals("root content", WordlistLoader.GetLines(rl.OpenResource("../../root.txt"), Encoding.UTF8).First());
                });
            }
            finally
            {
                // clean up
                foreach (var file in level2.EnumerateFiles())
                {
                    file.Delete();
                }
                level2.Delete();
                foreach (var file in level1.EnumerateFiles())
                {
                    file.Delete();
                }
                level1.Delete();
                foreach (var file in @base.EnumerateFiles())
                {
                    file.Delete();
                }
                @base.Delete();
            }
        }

        [Test]
        [LuceneNetSpecific] // Issue #832
        public virtual void TestRelativePathsWithStringAndDelegation()
        {
            // Create a directory structure
            DirectoryInfo @base = CreateTempDir("fsResourceLoaderRelativePathDelegation");
            DirectoryInfo subdir = new DirectoryInfo(System.IO.Path.Combine(@base.FullName, "subdir"));
            subdir.Create();

            try
            {
                // Create a file in the subdirectory
                TextWriter os = new StreamWriter(new FileStream(System.IO.Path.Combine(subdir.FullName, "existing.txt"), FileMode.Create, FileAccess.Write), StandardCharsets.UTF_8);
                try
                {
                    os.Write("found\n");
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(os);
                }

                SystemEnvironment.WithCurrentDirectory(@base.FullName, () =>
                {
                    // Test with relative path and delegation
                    IResourceLoader rl = new FilesystemResourceLoader("subdir", new StringMockResourceLoader("delegated\n"));

                    // File exists in relative path
                    assertEquals("found", WordlistLoader.GetLines(rl.OpenResource("existing.txt"), Encoding.UTF8).First());

                    // File doesn't exist, should delegate
                    assertEquals("delegated", WordlistLoader.GetLines(rl.OpenResource("missing.txt"), Encoding.UTF8).First());
                });

                // Test with ".." in relative base path
                SystemEnvironment.WithCurrentDirectory(subdir.FullName, () =>
                {
                    IResourceLoader rl = new FilesystemResourceLoader("..", new StringMockResourceLoader("parent-delegated\n"));
                    assertEquals("found", WordlistLoader.GetLines(rl.OpenResource("subdir/existing.txt"), Encoding.UTF8).First());
                    assertEquals("parent-delegated", WordlistLoader.GetLines(rl.OpenResource("nonexistent.txt"), Encoding.UTF8).First());
                });
            }
            finally
            {
                // clean up
                foreach (var file in subdir.EnumerateFiles())
                {
                    file.Delete();
                }
                subdir.Delete();
                @base.Delete();
            }
        }
    }
}
