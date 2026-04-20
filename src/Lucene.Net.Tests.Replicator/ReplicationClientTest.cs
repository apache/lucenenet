using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private sealed class StubReplicator : IAsyncReplicator
        {
            public Func<string?, SessionToken?> CheckForUpdateImpl { get; set; } = _ => null;
            public Func<string, string, string, Stream> ObtainFileImpl { get; set; } = (_, __, ___) => throw new NotImplementedException();
            public Action<string> ReleaseImpl { get; set; } = _ => { };
            public int ReleaseCount;
            public CancellationToken LastReleaseAsyncCancellationToken;

            public SessionToken? CheckForUpdate(string? currentVersion) => CheckForUpdateImpl(currentVersion);
            public Stream ObtainFile(string sessionId, string source, string fileName) => ObtainFileImpl(sessionId, source, fileName);
            public void Release(string sessionId) { Interlocked.Increment(ref ReleaseCount); ReleaseImpl(sessionId); }
            public void Publish(IRevision revision) => throw new NotSupportedException();

            public Task<SessionToken?> CheckForUpdateAsync(string? currentVersion, CancellationToken cancellationToken = default)
            {
                try
                {
                    return Task.FromResult(CheckForUpdateImpl(currentVersion));
                }
                catch (Exception e)
                {
                    return Task.FromException<SessionToken?>(e);
                }
            }

            public Task<Stream> ObtainFileAsync(string sessionId, string source, string fileName, CancellationToken cancellationToken = default)
            {
                try
                {
                    return Task.FromResult(ObtainFileImpl(sessionId, source, fileName));
                }
                catch (Exception e)
                {
                    return Task.FromException<Stream>(e);
                }
            }

            public Task ReleaseAsync(string sessionId, CancellationToken cancellationToken = default)
            {
                LastReleaseAsyncCancellationToken = cancellationToken;
                Interlocked.Increment(ref ReleaseCount);
                ReleaseImpl(sessionId);
                return Task.CompletedTask;
            }

            public Task PublishAsync(IRevision revision, CancellationToken cancellationToken = default)
                => Task.FromException(new NotSupportedException());

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

        private sealed class StubRevision : IRevision
        {
            public string Version { get; set; } = "v1";
            public IDictionary<string, IList<RevisionFile>> SourceFiles { get; set; } =
                new Dictionary<string, IList<RevisionFile>>();
            public int CompareTo(string version) => 0;
            public int CompareTo(IRevision other) => 0;
            public Stream Open(string source, string fileName) => throw new NotImplementedException();
            public void Release() { }
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

        [Test]
        public async Task UpdateNowAsync_WhenCheckForUpdateThrows_PropagatesOriginalException()
        {
            var expected = new IOException("simulated check failure");
            var replicator = new StubReplicator { CheckForUpdateImpl = _ => throw expected };
            using var client = new ReplicationClient(replicator, new NoopHandler(), new ThrowingFactory());

            try
            {
                await client.UpdateNowAsync();
                fail("expected exception");
            }
            catch (IOException actual)
            {
                assertSame(expected, actual);
            }
        }

        [Test]
        public async Task UpdateNowAsync_WhenCancelledMidCopy_StillReleasesSession()
        {
            // When the caller cancels mid-copy, we must still release the server-
            // side session. The release call should use CancellationToken.None so
            // the pre-cancelled token does not skip the release.
            using var cts = new CancellationTokenSource();
            var session = new SessionToken("session-id", new StubRevision());

            var replicator = new StubReplicator
            {
                CheckForUpdateImpl = _ =>
                {
                    // Cancel before we return the session, so DoUpdateAsync will
                    // proceed into the try-finally that must call ReleaseAsync.
                    cts.Cancel();
                    return session;
                },
            };
            using var client = new ReplicationClient(replicator, new NoopHandler(), new ThrowingFactory());

            try
            {
                await client.UpdateNowAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // expected
            }

            assertEquals("Release must be called exactly once", 1, replicator.ReleaseCount);
            assertFalse(
                "ReleaseAsync must be passed a non-cancelled token so the server session is actually released",
                replicator.LastReleaseAsyncCancellationToken.IsCancellationRequested);
        }

        [Test]
        public async Task StartUpdateThread_WhenAsyncLoopAlreadyRunning_Throws()
        {
            var replicator = new StubReplicator();
            using var client = new ReplicationClient(replicator, new NoopHandler(), new ThrowingFactory());
            client.StartAsyncUpdateLoop(intervalInMilliseconds: 60_000);
            try
            {
                try
                {
                    client.StartUpdateThread(intervalInMilliseconds: 60_000, threadName: null);
                    fail("expected IllegalStateException when starting sync thread while async loop is running");
                }
                catch (InvalidOperationException)
                {
                    // expected
                }
            }
            finally
            {
                await client.StopAsyncUpdateLoop();
            }
        }

        [Test]
        public async Task StopAsyncUpdateLoop_ReturnsCleanly_AndClearsInternalState()
        {
            var replicator = new StubReplicator();
            using var client = new ReplicationClient(replicator, new NoopHandler(), new ThrowingFactory());
            client.StartAsyncUpdateLoop(intervalInMilliseconds: 60_000);
            assertTrue(client.IsAsyncUpdateLoopAlive);

            await client.StopAsyncUpdateLoop();

            assertFalse("loop should report not alive after stop", client.IsAsyncUpdateLoopAlive);

            // Calling stop again should be a no-op.
            await client.StopAsyncUpdateLoop();
        }

        [Test]
        public void Dispose_WithAsyncLoopRunning_StopsLoopAndDoesNotHang()
        {
            var replicator = new StubReplicator();
            var client = new ReplicationClient(replicator, new NoopHandler(), new ThrowingFactory());
            client.StartAsyncUpdateLoop(intervalInMilliseconds: 60_000);
            assertTrue(client.IsAsyncUpdateLoopAlive);

            // Dispose should block until the async loop is fully cancelled.
            // If Dispose hangs, NUnit will time the test out.
            client.Dispose();
        }

        [Test]
        public void StartAsyncUpdateLoop_WhenSyncThreadAlreadyRunning_Throws()
        {
            var replicator = new StubReplicator();
            using var client = new ReplicationClient(replicator, new NoopHandler(), new ThrowingFactory());
            client.StartUpdateThread(intervalInMilliseconds: 60_000, threadName: null);
            try
            {
                try
                {
                    client.StartAsyncUpdateLoop(intervalInMilliseconds: 60_000);
                    fail("expected IllegalStateException when starting async loop while sync thread is running");
                }
                catch (InvalidOperationException)
                {
                    // expected
                }
            }
            finally
            {
                client.StopUpdateThread();
            }
        }
    }
}
