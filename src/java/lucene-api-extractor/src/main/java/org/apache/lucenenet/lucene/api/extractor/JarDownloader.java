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

import java.io.File;
import java.io.IOException;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.file.Files;
import java.security.MessageDigest;
import java.time.Duration;

public class JarDownloader {
    // Package-private and non-final so tests can rebind it to a local HTTP fixture.
    static String MAVEN_CENTRAL = "https://repo1.maven.org/maven2";
    private static final int MAX_ATTEMPTS = 3;
    private static final Duration CONNECT_TIMEOUT = Duration.ofSeconds(15);
    private static final Duration REQUEST_TIMEOUT = Duration.ofMinutes(2);

    private static volatile HttpClient httpClient;

    public static void downloadMavenDependency(ExtractContext context, MavenCoordinates dependency, boolean force) {
        var downloadDir = new File(context.getDownloadsDir());
        if (!downloadDir.exists() && !downloadDir.mkdirs()) {
            throw new RuntimeException("Failed to create download directory: " + downloadDir.getAbsolutePath());
        }

        var jarName = dependency.getJarName();
        var jarFile = new File(downloadDir, jarName);
        if (jarFile.exists() && !force) {
            System.err.printf("File %s already exists. Skipping download.%n", jarName);
            return;
        }

        var jarUrl = mavenUrl(dependency, jarName);
        System.err.printf("Downloading %s%n", jarUrl);

        downloadWithRetry(jarUrl, jarFile);

        if (context.isVerifyChecksum()) {
            verifyChecksum(dependency, jarFile);
        }

        System.err.printf("Downloaded %s (%d bytes)%n", jarFile.getAbsolutePath(), jarFile.length());
    }

    private static String mavenUrl(MavenCoordinates coords, String fileName) {
        var groupPath = coords.groupId().replace(".", "/");
        return "%s/%s/%s/%s/%s".formatted(MAVEN_CENTRAL, groupPath, coords.artifactId(), coords.version(), fileName);
    }

    private static void downloadWithRetry(String url, File destination) {
        IOException lastFailure = null;
        for (int attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {
            try {
                var request = HttpRequest.newBuilder(URI.create(url))
                        .timeout(REQUEST_TIMEOUT)
                        .GET()
                        .build();
                var response = client().send(request, HttpResponse.BodyHandlers.ofFile(destination.toPath()));
                if (response.statusCode() == 200) {
                    return;
                }
                throw new IOException("HTTP " + response.statusCode() + " for " + url);
            } catch (IOException e) {
                lastFailure = e;
                if (attempt < MAX_ATTEMPTS) {
                    System.err.printf("Attempt %d/%d failed (%s); retrying…%n", attempt, MAX_ATTEMPTS, e.getMessage());
                    sleep(500L * attempt);
                }
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                throw new RuntimeException("Interrupted while downloading " + url, e);
            }
        }
        throw new RuntimeException("Failed to download " + url + " after " + MAX_ATTEMPTS + " attempts", lastFailure);
    }

    private static void verifyChecksum(MavenCoordinates coords, File jarFile) {
        var sha1Url = mavenUrl(coords, coords.getJarName() + ".sha1");
        try {
            var request = HttpRequest.newBuilder(URI.create(sha1Url))
                    .timeout(REQUEST_TIMEOUT)
                    .GET()
                    .build();
            var response = client().send(request, HttpResponse.BodyHandlers.ofString());
            if (response.statusCode() != 200) {
                // Checksum sidecar is missing; warn but don't fail — not every artifact publishes one.
                System.err.printf("No .sha1 sidecar available for %s (HTTP %d); skipping verification.%n",
                        coords.getJarName(), response.statusCode());
                return;
            }
            var expected = response.body().trim().split("\\s+")[0].toLowerCase();
            var actual = sha1Hex(jarFile);
            if (!expected.equalsIgnoreCase(actual)) {
                throw new RuntimeException("SHA-1 mismatch for " + coords.getJarName()
                        + ": expected " + expected + ", got " + actual);
            }
        } catch (IOException e) {
            throw new RuntimeException("Failed to fetch checksum for " + coords.getJarName(), e);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            throw new RuntimeException("Interrupted while fetching checksum for " + coords.getJarName(), e);
        }
    }

    private static String sha1Hex(File file) throws IOException {
        try {
            var digest = MessageDigest.getInstance("SHA-1");
            try (var in = Files.newInputStream(file.toPath())) {
                var buffer = new byte[8192];
                int read;
                while ((read = in.read(buffer)) != -1) {
                    digest.update(buffer, 0, read);
                }
            }
            var hash = digest.digest();
            var sb = new StringBuilder(hash.length * 2);
            for (byte b : hash) {
                sb.append(String.format("%02x", b & 0xff));
            }
            return sb.toString();
        } catch (java.security.NoSuchAlgorithmException e) {
            throw new IOException("SHA-1 not available on this JVM", e);
        }
    }

    private static HttpClient client() {
        var local = httpClient;
        if (local == null) {
            synchronized (JarDownloader.class) {
                local = httpClient;
                if (local == null) {
                    local = HttpClient.newBuilder()
                            .connectTimeout(CONNECT_TIMEOUT)
                            .followRedirects(HttpClient.Redirect.NORMAL)
                            .build();
                    httpClient = local;
                }
            }
        }
        return local;
    }

    private static void sleep(long millis) {
        try {
            Thread.sleep(millis);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }
    }
}
