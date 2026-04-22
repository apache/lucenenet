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

package org.apache.lucenenet.lucene.api.extractor;

import com.sun.net.httpserver.HttpServer;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

import java.net.InetSocketAddress;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.MessageDigest;
import java.util.concurrent.atomic.AtomicInteger;

import static org.junit.jupiter.api.Assertions.*;

/**
 * Unit tests for JarDownloader that stand up an in-process HTTP server pretending to be
 * Maven Central. Swaps JarDownloader.MAVEN_CENTRAL for the duration of each test.
 */
class JarDownloaderTest {

    private HttpServer server;
    private String originalMavenCentral;

    @BeforeEach
    void startServer() throws Exception {
        server = HttpServer.create(new InetSocketAddress("127.0.0.1", 0), 0);
        server.start();
        originalMavenCentral = JarDownloader.MAVEN_CENTRAL;
        JarDownloader.MAVEN_CENTRAL = "http://127.0.0.1:" + server.getAddress().getPort();
    }

    @AfterEach
    void stopServer() {
        if (server != null) {
            server.stop(0);
        }
        JarDownloader.MAVEN_CENTRAL = originalMavenCentral;
    }

    @Test
    void downloadsJarSuccessfully(@TempDir Path tmp) throws Exception {
        byte[] jarBytes = "fake-jar-content".getBytes();
        serveBytes("/org/foo/bar/1.0/bar-1.0.jar", jarBytes);
        serveBytes("/org/foo/bar/1.0/bar-1.0.jar.sha1", sha1Hex(jarBytes).getBytes());

        var context = contextFor(tmp, true);
        JarDownloader.downloadMavenDependency(context, new MavenCoordinates("org.foo", "bar", "1.0"), false);

        var downloaded = tmp.resolve("bar-1.0.jar");
        assertTrue(Files.exists(downloaded));
        assertArrayEquals(jarBytes, Files.readAllBytes(downloaded));
    }

    @Test
    void skipsDownloadWhenFileExistsAndNotForced(@TempDir Path tmp) throws Exception {
        var existing = tmp.resolve("bar-1.0.jar");
        Files.writeString(existing, "already-here");
        // Server has no route registered — a request would 404 and fail.

        var context = contextFor(tmp, false); // verification off so it won't hit the network
        JarDownloader.downloadMavenDependency(context, new MavenCoordinates("org.foo", "bar", "1.0"), false);

        assertEquals("already-here", Files.readString(existing));
    }

    @Test
    void forceRedownloadsEvenIfFileExists(@TempDir Path tmp) throws Exception {
        var existing = tmp.resolve("bar-1.0.jar");
        Files.writeString(existing, "stale");

        byte[] fresh = "fresh-content".getBytes();
        serveBytes("/org/foo/bar/1.0/bar-1.0.jar", fresh);

        var context = contextFor(tmp, false);
        JarDownloader.downloadMavenDependency(context, new MavenCoordinates("org.foo", "bar", "1.0"), true);

        assertArrayEquals(fresh, Files.readAllBytes(existing));
    }

    @Test
    void verifyChecksumFailsOnMismatch(@TempDir Path tmp) {
        byte[] jarBytes = "payload".getBytes();
        serveBytes("/org/foo/bar/1.0/bar-1.0.jar", jarBytes);
        serveBytes("/org/foo/bar/1.0/bar-1.0.jar.sha1", "deadbeef".getBytes());

        var context = contextFor(tmp, true);
        var ex = assertThrows(RuntimeException.class,
                () -> JarDownloader.downloadMavenDependency(context, new MavenCoordinates("org.foo", "bar", "1.0"), false));
        assertTrue(ex.getMessage().contains("SHA-1 mismatch"));
    }

    @Test
    void verifyChecksumSkippedWhenSidecarMissing(@TempDir Path tmp) throws Exception {
        byte[] jarBytes = "payload".getBytes();
        serveBytes("/org/foo/bar/1.0/bar-1.0.jar", jarBytes);
        // HttpServer uses longest-prefix matching, so we must register an explicit
        // handler for the .sha1 path; otherwise it falls through to the jar handler.
        server.createContext("/org/foo/bar/1.0/bar-1.0.jar.sha1", exchange -> {
            exchange.sendResponseHeaders(404, -1);
            exchange.close();
        });

        var context = contextFor(tmp, true);
        assertDoesNotThrow(() -> JarDownloader.downloadMavenDependency(context, new MavenCoordinates("org.foo", "bar", "1.0"), false));
        assertTrue(Files.exists(tmp.resolve("bar-1.0.jar")));
    }

    @Test
    void verifyChecksumCanBeDisabled(@TempDir Path tmp) throws Exception {
        byte[] jarBytes = "payload".getBytes();
        serveBytes("/org/foo/bar/1.0/bar-1.0.jar", jarBytes);
        serveBytes("/org/foo/bar/1.0/bar-1.0.jar.sha1", "deadbeef".getBytes());

        var context = contextFor(tmp, false);
        // Checksum disabled, so the mismatch should not matter.
        assertDoesNotThrow(() -> JarDownloader.downloadMavenDependency(context, new MavenCoordinates("org.foo", "bar", "1.0"), false));
        assertTrue(Files.exists(tmp.resolve("bar-1.0.jar")));
    }

    @Test
    void retriesOnTransientFailure(@TempDir Path tmp) throws Exception {
        byte[] jarBytes = "flaky".getBytes();
        var attempt = new AtomicInteger(0);
        server.createContext("/org/foo/bar/1.0/bar-1.0.jar", exchange -> {
            int n = attempt.incrementAndGet();
            if (n < 2) {
                exchange.sendResponseHeaders(500, -1);
            } else {
                exchange.sendResponseHeaders(200, jarBytes.length);
                try (var os = exchange.getResponseBody()) {
                    os.write(jarBytes);
                }
            }
            exchange.close();
        });
        serveBytes("/org/foo/bar/1.0/bar-1.0.jar.sha1", sha1Hex(jarBytes).getBytes());

        var context = contextFor(tmp, true);
        JarDownloader.downloadMavenDependency(context, new MavenCoordinates("org.foo", "bar", "1.0"), false);

        assertEquals(2, attempt.get(), "should have retried after the initial 500");
        assertTrue(Files.exists(tmp.resolve("bar-1.0.jar")));
    }

    @Test
    void failsAfterExhaustingRetries(@TempDir Path tmp) {
        server.createContext("/org/foo/bar/1.0/bar-1.0.jar", exchange -> {
            exchange.sendResponseHeaders(500, -1);
            exchange.close();
        });

        var context = contextFor(tmp, false);
        var ex = assertThrows(RuntimeException.class,
                () -> JarDownloader.downloadMavenDependency(context, new MavenCoordinates("org.foo", "bar", "1.0"), false));
        assertTrue(ex.getMessage().contains("Failed to download"));
    }

    @Test
    void createsDownloadDirectoryIfMissing(@TempDir Path tmp) throws Exception {
        var nested = tmp.resolve("a/b/c");
        assertFalse(Files.exists(nested));

        byte[] jarBytes = "nested-dir".getBytes();
        serveBytes("/org/foo/bar/1.0/bar-1.0.jar", jarBytes);

        var context = contextFor(nested, false);
        JarDownloader.downloadMavenDependency(context, new MavenCoordinates("org.foo", "bar", "1.0"), false);

        assertTrue(Files.exists(nested.resolve("bar-1.0.jar")));
    }

    private ExtractContext contextFor(Path downloadDir, boolean verifyChecksum) {
        return new ExtractContext(
                downloadDir.toString(),
                new String[]{"org.foo:bar:1.0"},
                false,
                null,
                new String[0],
                false,
                verifyChecksum);
    }

    private void serveBytes(String path, byte[] body) {
        server.createContext(path, exchange -> {
            exchange.sendResponseHeaders(200, body.length);
            try (var os = exchange.getResponseBody()) {
                os.write(body);
            }
            exchange.close();
        });
    }

    private static String sha1Hex(byte[] bytes) throws Exception {
        var digest = MessageDigest.getInstance("SHA-1").digest(bytes);
        var sb = new StringBuilder(digest.length * 2);
        for (byte b : digest) {
            sb.append(String.format("%02x", b & 0xff));
        }
        return sb.toString();
    }
}
