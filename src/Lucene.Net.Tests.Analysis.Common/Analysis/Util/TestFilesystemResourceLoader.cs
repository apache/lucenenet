using System;
using System.Text;
using NUnit.Framework;
using Lucene.Net.Util;
using System.IO;
using System.Linq;
using System.Reflection;

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
                IOUtils.CloseWhileHandlingException(rl.OpenResource("/this-directory-really-really-really-should-not-exist/foo/bar.txt"));
                fail("The resource does not exist, should fail!");
            }
            catch (IOException)
            {
                // pass
            }
            try
            {
                rl.NewInstance<TokenFilterFactory>("org.apache.lucene.analysis.FooBarFilterFactory");
                fail("The class does not exist, should fail!");
            }
            catch (Exception)
            {
                // pass
            }
        }

        private void assertClasspathDelegation(IResourceLoader rl)
        {
            const string LuceneNetAnalysisCommon = "Lucene.Net.Analysis.Common";
            var assemblyDirectory = System.IO.Path.GetDirectoryName(typeof(TestFilesystemResourceLoader).GetTypeInfo().Assembly.Location);
            var current = new DirectoryInfo(assemblyDirectory);

            DirectoryInfo analysisCommonFolder = null;

            // LUCENENET: Searching upwards for the parent rather than using a
            // relative path because in .NET Core, the base directory is not
            // always under bin\Debug\. The program may be running from
            // bin\Debug\netcoreapp1.0\win7-x64\.
            while (current != null)
            {
                var matching = current.GetDirectories(LuceneNetAnalysisCommon, SearchOption.TopDirectoryOnly);

                if (matching == null || matching.Length == 0)
                {
                    current = Directory.GetParent(current.FullName);
                }
                else
                {
                    analysisCommonFolder = matching.First();
                    break;
                }
            }

            if (analysisCommonFolder == null)
            {
                throw new InvalidOperationException("Should have been able to find " + LuceneNetAnalysisCommon + " as a parent of " + typeof(TestFilesystemResourceLoader).GetTypeInfo().Assembly.Location);
            }

            var englishStopText = System.IO.Path.Combine(analysisCommonFolder.FullName, @"Analysis\Snowball\english_stop.txt");
            // try a stopwords file from classpath
            CharArraySet set = WordlistLoader.GetSnowballWordSet(new System.IO.StreamReader(rl.OpenResource(englishStopText), Encoding.UTF8), TEST_VERSION_CURRENT);
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
                TextWriter os = new System.IO.StreamWriter(new System.IO.FileStream(System.IO.Path.Combine(@base.FullName, "template.txt"), System.IO.FileMode.Create, System.IO.FileAccess.Write), Encoding.UTF8);
                try
                {
                    os.Write("foobar\n");
                }
                finally
                {
                    IOUtils.CloseWhileHandlingException(os);
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