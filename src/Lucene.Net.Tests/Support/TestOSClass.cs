using Lucene.Net.Attributes;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Support
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
    public class TestOSClass
    {
        // LUCENENET-216
        [Test, LuceneNetSpecific]
        public void TestFSDirectorySync()
        {
            DirectoryInfo path = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "testsync"));
            Lucene.Net.Store.Directory directory = new Lucene.Net.Store.SimpleFSDirectory(path, null);
            try
            {
                Lucene.Net.Store.IndexOutput io = directory.CreateOutput("syncfile", new Store.IOContext());
                io.Dispose();
                directory.Sync(new string[] { "syncfile" });
            }
            finally
            {
                directory.Dispose();
                Lucene.Net.Util.TestUtil.Rm(path);
            }
        }
    }
}
