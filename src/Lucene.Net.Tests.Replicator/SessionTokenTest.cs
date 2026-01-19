using J2N.IO;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JCG = J2N.Collections.Generic;
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

    public class SessionTokenTest : ReplicatorTestCase
    {
        [Test]
        public void TestSerialization()
        {
            Directory directory = NewDirectory();
            IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            config.IndexDeletionPolicy = new SnapshotDeletionPolicy(config.IndexDeletionPolicy);

            IndexWriter writer = new IndexWriter(directory, config);
            writer.AddDocument(new Document());
            writer.Commit();
            IRevision revision = new IndexRevision(writer);

            SessionToken session1 = new SessionToken("17", revision);
            MemoryStream baos = new MemoryStream();
            session1.Serialize(new DataOutputStream(baos));
            byte[] b = baos.ToArray();

            SessionToken session2 = new SessionToken(new DataInputStream(new MemoryStream(b)));
            assertEquals(session1.Id, session2.Id);
            assertEquals(session1.Version, session2.Version);
            assertEquals(1, session2.SourceFiles.Count);
            assertEquals(session1.SourceFiles.Count, session2.SourceFiles.Count);

            // LUCENENET: Collections don't compare automatically in .NET and J2N has no structural equality
            // checking on Keys, so using CollectionAssert here. This is set
            // equality (where order doesn't matter) because in Java the keys and values collections are sets.
            CollectionAssert.AreEquivalent(session1.SourceFiles.Keys, session2.SourceFiles.Keys);
            IList<RevisionFile> files1 = session1.SourceFiles.Values.First();
            IList<RevisionFile> files2 = session2.SourceFiles.Values.First();
            assertEquals(files1, files2, aggressive: false);

            IOUtils.Dispose(writer, directory);
        }

        [Test, LuceneNetSpecific]
        public void TestToString()
        {
            // Create a mock SessionToken with known data
            Dictionary<string, IList<RevisionFile>> sourceFiles = new Dictionary<string, IList<RevisionFile>>();
            IList<RevisionFile> files1 = new JCG.List<RevisionFile>
            {
                new RevisionFile("file1.txt", 100),
                new RevisionFile("file2.txt", 200)
            };
            IList<RevisionFile> files2 = new JCG.List<RevisionFile>
            {
                new RevisionFile("file3.txt", 300)
            };
            sourceFiles.Add("source1", files1);
            sourceFiles.Add("source2", files2);

            MockRevision revision = new MockRevision("v1.0", sourceFiles);
            SessionToken session = new SessionToken("session123", revision);

            string result = session.ToString();

            // Verify the output contains all expected components
            Assert.IsTrue(result.Contains("id=session123"), "Should contain session id");
            Assert.IsTrue(result.Contains("version=v1.0"), "Should contain version");
            Assert.IsTrue(result.Contains("source1"), "Should contain source1");
            Assert.IsTrue(result.Contains("source2"), "Should contain source2");
            Assert.IsTrue(result.Contains("fileName=file1.txt length=100"), "Should contain file1 details");
            Assert.IsTrue(result.Contains("fileName=file2.txt length=200"), "Should contain file2 details");
            Assert.IsTrue(result.Contains("fileName=file3.txt length=300"), "Should contain file3 details");

            // Verify it's not using the generic dictionary ToString()
            Assert.IsFalse(result.Contains("System.Collections.Generic.Dictionary"), "Should not contain generic Dictionary type");
        }

        // Mock implementation for testing
        private class MockRevision : IRevision
        {
            private readonly string version;
            private readonly IDictionary<string, IList<RevisionFile>> sourceFiles;

            public MockRevision(string version, IDictionary<string, IList<RevisionFile>> sourceFiles)
            {
                this.version = version;
                this.sourceFiles = sourceFiles;
            }

            public string Version => version;
            public IDictionary<string, IList<RevisionFile>> SourceFiles => sourceFiles;
            public int CompareTo(string other) => version.CompareTo(other);
            public int CompareTo(IRevision other) => version.CompareTo(other?.Version);
            public Stream Open(string source, string fileName) => throw new NotImplementedException();
            public void Release() { }
        }

    }
}
