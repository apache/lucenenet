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
import java.util.LinkedHashMap;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class ExtractRunner {
    public static void extract(ExtractContext context) throws Exception {
        var libraries = reflect(context);
        var json = JsonSerializer.serialize(libraries);

        if (context.getOutputFile() != null) {
            Files.writeString(Path.of(context.getOutputFile()), json);
            System.out.println(MessageFormat.format("API extracted to: {0}", context.getOutputFile()));
        } else {
            System.out.println(json);
        }
    }

    public static void printHash(ExtractContext context) throws Exception {
        System.out.println(getHash(context));
    }

    /**
     * Produces a SHA-256 of the API surface, keyed by artifactId rather than the full
     * Maven coordinates. The group and version are deliberately excluded so that upgrading
     * from e.g. 4.8.1 → 4.8.2 with identical APIs yields the same hash.
     */
    public static String getHash(ExtractContext context) throws Exception {
        var libraries = reflect(context);

        // Hash payload: artifactId -> types. Version and groupId are excluded so the hash
        // reflects only the API surface.
        var payload = new LinkedHashMap<String, Object>();
        for (var lib : libraries) {
            payload.put(lib.library().artifactId(), lib.types());
        }
        var json = JsonSerializer.serialize(payload);

        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        byte[] hash = digest.digest(json.getBytes());

        StringBuilder hexString = new StringBuilder();
        for (byte b : hash) {
            String hex = Integer.toHexString(0xff & b);
            if (hex.length() == 1) {
                hexString.append('0');
            }
            hexString.append(hex);
        }

        return hexString.toString();
    }

    private static java.util.List<LibraryResult> reflect(ExtractContext context) throws Exception {
        if (context.getOutputFile() != null) {
            System.out.println("Extracting API");
            System.out.println(MessageFormat.format("Libraries: {0}",
                    Stream.of(context.getLibraries()).map(MavenCoordinates::artifactId).collect(Collectors.joining(", "))));
        }

        for (var library : context.getLibraries()) {
            JarDownloader.downloadMavenDependency(context, library, context.isForce());
        }

        for (var dependency : context.getDependencies()) {
            JarDownloader.downloadMavenDependency(context, dependency, context.isForce());
        }

        return JarReflector.reflectOverJars(context);
    }
}
