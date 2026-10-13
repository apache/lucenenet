using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Replicator
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

    // LUCENENET-specific: covers sessionId and source name validation in PerSessionDirectoryFactory
    // for the GetDirectory and CleanupSession entry points.
    [LuceneNetSpecific]
    public class PerSessionDirectoryFactoryTest : ReplicatorTestCase
    {
        private static readonly string[] InvalidPathComponents =
        {
            "../a",
            "..\\a",
            "/a",
            "C:\\folder",
            "subdir/segment",
            "subdir\\segment",
            "..",
            ".",
            "name\0extra",
            "",
        };

        [Test]
        [TestCaseSource(nameof(InvalidPathComponents))]
        public void TestGetDirectoryRejectsInvalidSessionId(string sessionId)
        {
            DirectoryInfo workDir = CreateTempDir("perSessionFactoryGetSession");
            PerSessionDirectoryFactory factory = new PerSessionDirectoryFactory(workDir.FullName);

            Assert.Throws<ArgumentException>(() =>
            {
                using Directory _ = factory.GetDirectory(sessionId, "src");
            }, $"GetDirectory should reject sessionId '{sessionId}'");
        }

        [Test]
        [TestCaseSource(nameof(InvalidPathComponents))]
        public void TestGetDirectoryRejectsInvalidSource(string source)
        {
            DirectoryInfo workDir = CreateTempDir("perSessionFactoryGetSource");
            PerSessionDirectoryFactory factory = new PerSessionDirectoryFactory(workDir.FullName);

            Assert.Throws<ArgumentException>(() =>
            {
                using Directory _ = factory.GetDirectory("session1", source);
            }, $"GetDirectory should reject source '{source}'");
        }

        [Test]
        [TestCaseSource(nameof(InvalidPathComponents))]
        public void TestCleanupSessionRejectsInvalidSessionId(string sessionId)
        {
            DirectoryInfo workDir = CreateTempDir("perSessionFactoryCleanup");
            PerSessionDirectoryFactory factory = new PerSessionDirectoryFactory(workDir.FullName);

            Assert.Throws<ArgumentException>(() =>
            {
                factory.CleanupSession(sessionId);
            }, $"CleanupSession should reject sessionId '{sessionId}'");
        }

        [Test]
        public void TestCleanupSessionDoesNotTouchSiblingDirectory()
        {
            // A sessionId that resolves to a sibling of the workingDirectory must be rejected by
            // validation. The sibling directory and its contents must remain untouched.
            DirectoryInfo workDir = CreateTempDir("perSessionFactorySiblingWork");
            DirectoryInfo sibling = CreateTempDir("perSessionFactorySibling");
            string siblingFile = Path.Combine(sibling.FullName, "marker.txt");
            File.WriteAllText(siblingFile, "untouched");

            PerSessionDirectoryFactory factory = new PerSessionDirectoryFactory(workDir.FullName);
            // Path.GetRelativePath is not available on .NET Framework; compute manually.
            // Both temp dirs share the same parent, so the relative path is "../<siblingName>".
            string relative = Path.Combine("..", sibling.Name);

            Assert.Throws<ArgumentException>(() => factory.CleanupSession(relative));
            assertTrue("sibling directory must still exist", System.IO.Directory.Exists(sibling.FullName));
            assertTrue("sibling file must still exist", File.Exists(siblingFile));
        }

        [Test]
        public void TestGetDirectoryAcceptsValidNames()
        {
            DirectoryInfo workDir = CreateTempDir("perSessionFactoryValid");
            PerSessionDirectoryFactory factory = new PerSessionDirectoryFactory(workDir.FullName);

            using Directory dir = factory.GetDirectory("session-abc", "src");
            assertNotNull(dir);

            string expected = Path.Combine(workDir.FullName, "session-abc", "src");
            assertTrue("expected session directory to exist on disk", System.IO.Directory.Exists(expected));
        }
    }
}
