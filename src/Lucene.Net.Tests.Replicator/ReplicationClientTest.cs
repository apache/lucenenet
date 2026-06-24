using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Directory = Lucene.Net.Store.Directory;

#nullable enable

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

    /// <summary>
    /// Unit tests for <see cref="ReplicationClient"/> that use stub replicators,
    /// handlers, and factories to exercise edge cases not easily reached through
    /// the full <see cref="Http.HttpReplicatorTest"/> integration tests.
    /// </summary>
    [LuceneNetSpecific]
    public class ReplicationClientTest : LuceneTestCase
    {
        private sealed class StubReplicator : IReplicator
        {
            public Func<string?, SessionToken?> CheckForUpdateImpl { get; set; } = _ => null;
            public Func<string, string, string, Stream> ObtainFileImpl { get; set; } = (_, __, ___) => throw new NotImplementedException();
            public Action<string> ReleaseImpl { get; set; } = _ => { };
            public int ReleaseCount;

            public SessionToken? CheckForUpdate(string? currentVersion) => CheckForUpdateImpl(currentVersion);
            public Stream ObtainFile(string sessionId, string source, string fileName) => ObtainFileImpl(sessionId, source, fileName);
            public void Release(string sessionId) { Interlocked.Increment(ref ReleaseCount); ReleaseImpl(sessionId); }
            public void Publish(IRevision revision) => throw new NotSupportedException();

            public void Dispose() { }
        }

        private sealed class NoopHandler : IReplicationHandler
        {
            public string? CurrentVersion => null;
            public IDictionary<string, IList<RevisionFile>>? CurrentRevisionFiles => null;
            public void RevisionReady(string version,
                IDictionary<string, IList<RevisionFile>> revisionFiles,
                IDictionary<string, IList<string>> copiedFiles,
                IDictionary<string, Directory> sourceDirectory)
            { }
        }

        private sealed class ThrowingFactory : ISourceDirectoryFactory
        {
            public Directory GetDirectory(string? sessionId, string source) => throw new NotImplementedException();
            public void CleanupSession(string? sessionId) { }
        }

        [Test]
        public void UpdateNow_WhenCheckForUpdateThrows_PropagatesOriginalException()
        {
            // Before the fix, the finally block would NRE on session.Id because
            // `session` was never assigned when CheckForUpdate threw. This test
            // pins the contract that the original exception reaches the caller
            // unmasked.
            var expected = new IOException("simulated check failure");
            var replicator = new StubReplicator { CheckForUpdateImpl = _ => throw expected };
            using var client = new ReplicationClient(replicator, new NoopHandler(), new ThrowingFactory());

            try
            {
                client.UpdateNow();
                fail("expected exception");
            }
            catch (IOException actual)
            {
                assertSame(expected, actual);
            }
        }
    }
}
