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

import java.nio.file.Files;
import java.nio.file.Path;
import java.security.MessageDigest;
import java.text.MessageFormat;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class ExtractRunner {
    public static void extract(ExtractContext context) throws Exception {
        var json = extractJson(context);

        if (context.getOutputFile() != null) {
            Files.writeString(Path.of(context.getOutputFile()), json);
            System.out.println(MessageFormat.format("API extracted to: {0}", context.getOutputFile()));
        } else {
            System.out.println(json);
        }
    }

    public static void printHash(ExtractContext context) throws Exception {
        var hashString = getHash(context);

        System.out.println(hashString);
    }

    public static String getHash(ExtractContext context) throws Exception {
        var json = extractJson(context);

        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        byte[] hash = digest.digest(json.getBytes());

        StringBuilder hexString = new StringBuilder();
        for (byte b : hash) {
            String hex = Integer.toHexString(0xff & b);
            if (hex.length() == 1) {
                hexString.append('0'); // Add leading zero for single digit hex values
            }
            hexString.append(hex);
        }

        return hexString.toString();
    }

    private static String extractJson(ExtractContext context) throws Exception {
        if (context.getOutputFile() != null) {
            System.out.println(MessageFormat.format("Extracting API for Lucene version: {0}", context.getLuceneVersion()));
            System.out.println(MessageFormat.format("Libraries: {0}",
                    Stream.of(context.getLibraries()).map(Library::libraryName).collect(Collectors.joining(", "))));
        }

        for (var library : context.getLibraries()) {
            JarDownloader.downloadLuceneJar(context, library, context.isForce());
        }

        for (var dependency : context.getDependencies()) {
            JarDownloader.downloadMavenDependency(context, dependency, context.isForce());
        }

        var loadedLibraries = JarReflector.reflectOverJars(context);

        return JsonSerializer.serialize(loadedLibraries);
    }
}
