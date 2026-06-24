using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#nullable enable

namespace Lucene.Net.Replicator.Http
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
    /// Unit tests for the async methods on <see cref="HttpClientBase"/> that
    /// use a mocked <see cref="HttpMessageHandler"/>, so we can assert properties
    /// of the HTTP interaction (streaming behavior, response lifetime, cancellation)
    /// that are difficult to verify via the full integration tests.
    /// </summary>
    [LuceneNetSpecific]
    public class HttpClientBaseTest : LuceneTestCase
    {
        private sealed class TestableHttpClientBase : HttpClientBase
        {
            public TestableHttpClientBase(string url, HttpClient client)
                : base(url, client)
            {
            }

            public new Task<HttpResponseMessage> ExecuteGetAsync(string request, string[]? parameters, CancellationToken cancellationToken)
                => base.ExecuteGetAsync(request, parameters, cancellationToken);

            public new Task<HttpResponseMessage> ExecutePostAsync(string request, HttpContent content, CancellationToken cancellationToken, params string[]? parameters)
                => base.ExecutePostAsync(request, content, cancellationToken, parameters);
        }

        private sealed class MockHttpMessageHandler : HttpMessageHandler
        {
            public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Handler { get; init; } =
                (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Handler(request, cancellationToken);
            }
        }

        /// <summary>
        /// A stream that blocks on read until the test releases it, so we can
        /// prove the caller receives the response message before the body has
        /// been consumed (i.e. the client is using ResponseHeadersRead, not
        /// the default ResponseContentRead which buffers the entire body).
        /// </summary>
        private sealed class BlockingStream : Stream
        {
            private readonly ManualResetEventSlim gate;

            public BlockingStream(ManualResetEventSlim gate)
            {
                this.gate = gate;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => 0; set => throw new NotSupportedException(); }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                gate.Wait();
                return 0;
            }
        }

        [Test]
        public async Task ExecuteGetAsync_ServerReturns500_ThrowsBeforeReturning()
        {
            var handler = new MockHttpMessageHandler
            {
                Handler = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "boom",
                    Content = new StringContent("server failed"),
                }),
            };
            using var client = new TestableHttpClientBase("http://example/", new HttpClient(handler));

            try
            {
                using var response = await client.ExecuteGetAsync("action", parameters: null, CancellationToken.None);
                fail("expected exception");
            }
            catch (Exception e) when (e.IsThrowable())
            {
                // expected
            }
        }

        [Test]
        public async Task ExecuteGetAsync_LargeBody_ReturnsBeforeBodyConsumed()
        {
            // Give the handler a response whose body would block forever if the
            // client tried to buffer it. The async GET should return immediately
            // after the headers arrive (HttpCompletionOption.ResponseHeadersRead).
            using var gate = new ManualResetEventSlim(false);
            var handler = new MockHttpMessageHandler
            {
                Handler = (_, _) =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var content = new StreamContent(new BlockingStream(gate));
                    var resp = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
                    return Task.FromResult(resp);
                },
            };
            using var client = new TestableHttpClientBase("http://example/", new HttpClient(handler));

            var task = client.ExecuteGetAsync("action", parameters: null, CancellationToken.None);
            var winner = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));
            try
            {
                assertSame("ExecuteGetAsync should return before the response body is consumed", task, winner);
                using var response = await task;
                assertTrue(response.IsSuccessStatusCode);
            }
            finally
            {
                gate.Set();
            }
        }

        [Test]
        public async Task ExecuteGetAsync_CancellationToken_IsPropagated()
        {
            using var cts = new CancellationTokenSource();
            var handler = new MockHttpMessageHandler
            {
                Handler = async (_, ct) =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                },
            };
            using var client = new TestableHttpClientBase("http://example/", new HttpClient(handler));

            var task = client.ExecuteGetAsync("action", parameters: null, cts.Token);
            await cts.CancelAsync();

            try
            {
                using var response = await task;
                fail("expected cancellation");
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        [Test]
        public async Task ExecutePostAsync_ServerReturns500_ThrowsBeforeReturning()
        {
            var handler = new MockHttpMessageHandler
            {
                Handler = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    ReasonPhrase = "bad gateway",
                    Content = new StringContent(string.Empty),
                }),
            };
            using var client = new TestableHttpClientBase("http://example/", new HttpClient(handler));

            try
            {
                using var response = await client.ExecutePostAsync(
                    "action",
                    new StringContent("body", Encoding.UTF8),
                    CancellationToken.None);
                fail("expected exception");
            }
            catch (Exception e) when (e.IsThrowable())
            {
                // expected
            }
        }

        /// <summary>
        /// HttpResponseMessage subclass that counts Dispose calls, so we can
        /// verify response lifetime invariants without a real server.
        /// </summary>
        private sealed class DisposeTrackingResponse : HttpResponseMessage
        {
            private int disposeCount;
            public int DisposeCount => disposeCount;

            public DisposeTrackingResponse(HttpStatusCode status) : base(status) { }

            protected override void Dispose(bool disposing)
            {
                Interlocked.Increment(ref disposeCount);
                base.Dispose(disposing);
            }
        }

        [Test]
        public async Task ObtainFileAsync_WhenBodyReadFails_DisposesResponse()
        {
            // Regression test for response-leak bug: ObtainFileAsync hands response
            // ownership to the returned stream only on success; on failure the
            // response must be disposed of.
            DisposeTrackingResponse? capturedResponse = null;
            var handler = new MockHttpMessageHandler
            {
                Handler = (_, _) =>
                {
                    capturedResponse = new DisposeTrackingResponse(HttpStatusCode.OK)
                    {
                        Content = new ThrowingContent(),
                    };
                    return Task.FromResult<HttpResponseMessage>(capturedResponse);
                },
            };
            using var replicator = new HttpReplicator("http://example/", new HttpClient(handler));

            try
            {
                await using var stream = await replicator.ObtainFileAsync("s", "src", "file.dat");
                fail("expected exception");
            }
            catch (Exception e) when (e.IsThrowable())
            {
                // expected
            }

            assertNotNull(capturedResponse);
            assertTrue("response must be disposed when obtaining the file fails", capturedResponse!.DisposeCount > 0);
        }

        /// <summary>
        /// <see cref="HttpContent"/> whose stream creation throws — used to simulate the
        /// post-headers failure path in ObtainFileAsync.
        /// </summary>
        private sealed class ThrowingContent : HttpContent
        {
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
                => throw new IOException("simulated body failure");

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }

#if FEATURE_HTTPCONTENT_READASSTREAM
            protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
                => throw new IOException("simulated body failure");
#endif

            protected override Task<Stream> CreateContentReadStreamAsync()
                => Task.FromException<Stream>(new IOException("simulated body failure"));
        }

        [Test]
        public void PublishAsync_ReturnsFaultedTask_DoesNotThrowSynchronously()
        {
            // The method must return a Task that completes with the exception,
            // not throw synchronously — otherwise callers using Task.WhenAll or
            // fire-and-forget patterns would see the exception in the wrong place.
            using var replicator = new HttpReplicator("http://example/", new HttpClient(new MockHttpMessageHandler()));

            Task publishTask = replicator.PublishAsync(revision: null!);

            assertNotNull(publishTask);
            assertTrue("PublishAsync should return a faulted Task", publishTask.IsFaulted);
            assertNotNull(publishTask.Exception);
        }

        [Test]
        public async Task ExecutePostAsync_CancellationToken_IsPropagated()
        {
            using var cts = new CancellationTokenSource();
            var handler = new MockHttpMessageHandler
            {
                Handler = async (_, ct) =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                },
            };
            using var client = new TestableHttpClientBase("http://example/", new HttpClient(handler));

            var task = client.ExecutePostAsync("action", new StringContent("x"), cts.Token);
            await cts.CancelAsync();

            try
            {
                using var response = await task;
                fail("expected cancellation");
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }
    }
}
