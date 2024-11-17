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

import org.apache.http.client.methods.HttpGet;
import org.apache.http.impl.client.HttpClientBuilder;

import java.io.File;
import java.io.IOException;

public class JarDownloader {
    public static void downloadLuceneJar(ExtractContext context, Library library, boolean force) {
        // check if download directory exists
        var downloadDir = new File(context.getDownloadsDir());
        if (!downloadDir.exists()) {
            if (!downloadDir.mkdir()) {
                throw new RuntimeException("Failed to create download directory");
            }
        }

        var jarFile = new File(downloadDir, library.getJarName());
        if (jarFile.exists() && !force) {
            if (!context.isStandardOutput()) {
                System.out.printf("File %s already exists. Skipping download.%n", library.getJarName());
            }
            return;
        }

        var url = "https://repo1.maven.org/maven2/org/apache/lucene/lucene-%s/%s/%s".formatted(library.libraryName(), library.version(), library.getJarName());
        if (!context.isStandardOutput()) {
            System.out.printf("Downloading %s%n", url);
        }

        // Download the jar file
        try (var client = HttpClientBuilder.create().build()) {
            var request = new HttpGet(url);
            var response = client.execute(request);
            var entity = response.getEntity();
            if (entity != null) {
                var content = entity.getContent();

                // write the jar file to disk
                try (var fos = new java.io.FileOutputStream(jarFile)) {
                    content.transferTo(fos);
                }

                if (!context.isStandardOutput()) {
                    System.out.printf("Downloaded %s (%d bytes)%n", jarFile.getAbsolutePath(), jarFile.length());
                }
            } else {
                throw new RuntimeException("Failed to download jar file");
            }
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }
}
