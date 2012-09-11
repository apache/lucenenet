/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestOSClass
    {
        // LUCENENET-216
        [Test]
        public void TestFSDirectorySync()
        {
            System.IO.DirectoryInfo path = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppSettings.Get("tempDir", ""), "testsync"));
            Lucene.Net.Store.Directory directory = new Lucene.Net.Store.SimpleFSDirectory(path, null);
            try
            {
                Lucene.Net.Store.IndexOutput io = directory.CreateOutput("syncfile");
                io.Close();
                directory.Sync("syncfile");
            }
            finally
            {
                directory.Close();
                Lucene.Net.Util._TestUtil.RmDir(path);
            }
        }
    }
}
