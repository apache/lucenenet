using J2N.IO;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    }
}