// Lucene version compatibility level 4.8.1
using J2N;
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
                TextWriter os = new StreamWriter(new FileStream(System.IO.Path.Combine(@base.FullName, "template.txt"), FileMode.Create, FileAccess.Write), Encoding.UTF8);
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
            IResourceLoader rl = new FilesystemResourceLoader(null, new StringMockResourceLoader("foobar\n"));
            assertEquals("foobar", WordlistLoader.GetLines(rl.OpenResource("template.txt"), Encoding.UTF8).First());
        }
    }
}