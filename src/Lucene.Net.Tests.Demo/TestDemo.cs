using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Demo
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

    public class TestDemo : LuceneTestCase
    {
        private void TestOneSearch(DirectoryInfo indexPath, string query, int expectedHitCount)
        {
            TextWriter outSave = Console.Out;
            try
            {
                MemoryStream bytes = new MemoryStream();
                // .NET NOTE: GetEncoding(0) returns the current system's default encoding
                var fakeSystemOut = new StreamWriter(bytes, Encoding.GetEncoding(0));
                Console.SetOut(fakeSystemOut);
                // LUCENENET specific: changed the arguments to act more like the dotnet.exe commands.
                // * only optional arguments start with - 
                // * options have a long form that starts with --
                // * required arguments must be supplied without - or -- and in a specific order
                // Since the demo is meant to be seen by end users, these changes were necessary to make
                // it consistent with the lucene-cli utility.
                SearchFiles.Main(new string[] { indexPath.FullName, "--query", query });
                fakeSystemOut.Flush();
                // .NET NOTE: GetEncoding(0) returns the current system's default encoding
                string output = Encoding.GetEncoding(0).GetString(bytes.ToArray()); // intentionally use default encoding
                assertTrue("output=" + output, output.Contains(expectedHitCount + " total matching documents"));
            }
            finally
            {
                Console.SetOut(outSave);
            }
        }

        [Test]
        public void TestIndexSearch()
        {
            DirectoryInfo filesDir = CreateTempDir("DemoTestFiles");

            // LUCENENET specific - rather than relying on a flakey
            // file location, we first extract the files to the disk
            // from embedded resources. Then we know the exact location
            // where they are and it can't fail.
            var thisAssembly = this.GetType().Assembly;
            string embeddedDocsLocation = "Test_Files.Docs.";
            foreach (var file in thisAssembly.GetManifestResourceNames())
            {
                if (file.Contains(embeddedDocsLocation))
                {
                    string fileName = Regex.Replace(file, ".*" + embeddedDocsLocation.Replace(".", @"\."), "");
                    string destinationPath = Path.Combine(filesDir.FullName, fileName);

                    using Stream input = thisAssembly.GetManifestResourceStream(file);
                    using Stream output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
                    input.CopyTo(output);
                }
            }

            DirectoryInfo indexDir = CreateTempDir("DemoTest");
            // LUCENENET specific: changed the arguments to act more like the dotnet.exe commands.
            // * only optional arguments start with - 
            // * options have a long form that starts with --
            // * required arguments must be supplied without - or -- and in a specific order
            // Since the demo is meant to be seen by end users, these changes were necessary to make
            // it consistent with the lucene-cli utility.
            // NOTE: There is no -create in lucene, but it has the same effect as if --update were left out
            IndexFiles.Main(new string[] { indexDir.FullName, filesDir.FullName }); 
            //IndexFiles.Main(new string[] { "-create", "-docs", filesDir.FullName, "-index", indexDir.FullName });
            TestOneSearch(indexDir, "apache", 3);
            TestOneSearch(indexDir, "patent", 8);
            TestOneSearch(indexDir, "lucene", 0);
            TestOneSearch(indexDir, "gnu", 6);
            TestOneSearch(indexDir, "derivative", 8);
            TestOneSearch(indexDir, "license", 13);
        }
    }
}
