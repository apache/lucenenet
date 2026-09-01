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
package org.apache.lucenenet.compat;

import org.apache.lucene.codecs.Codec;
import org.apache.lucene.store.Directory;
import org.apache.lucene.store.SimpleFSDirectory;

import java.io.File;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Writes the deterministic compatibility index (both compound-file and
 * non-compound-file variants) with Apache Lucene, for Lucene.NET to read
 * back. This is the Java side of the "Java -&gt; .NET" direction of issue #270.
 *
 * <p>Usage (from the {@code src/java/index-compat} directory):
 * <pre>
 *   ./mvnw -q compile exec:java
 *   ./mvnw -q compile exec:java -Dexec.args="/path/to/output"
 *   ./mvnw -q compile exec:java -Dexec.args="/path/to/output" -Dcompat.codec.name=Lucene46
 * </pre>
 *
 * <p>The output is written under a temporary, gitignored {@code work/java}
 * folder by default (or the directory named by the {@code lucenenet.work.dir}
 * system property, or the first command-line argument). Two subdirectories are
 * created, named after the codec: {@code index.&lt;codec&gt;.cfs} and
 * {@code index.&lt;codec&gt;.nocfs}.
 *
 * <p>The codec name in those folder names is parameterized separately from the
 * work directory, via the {@code compat.codec.name} system property. It defaults
 * to {@link Codec#getDefault()}'s name, so nothing here has to name a specific
 * Lucene version. The .NET half of the harness reads a property of the same
 * name and defaults the same way, from {@code Codec.Default.Name}.
 */
public final class GenerateIndex {

    private GenerateIndex() {
    }

    public static void main(String[] args) throws Exception {
        String codecName = System.getProperty("compat.codec.name");
        if (codecName == null || codecName.trim().isEmpty()) {
            codecName = Codec.getDefault().getName();
        }

        Path baseDir;
        if (args.length > 0 && args[0] != null && !args[0].isEmpty()) {
            baseDir = Paths.get(args[0]);
        } else {
            String prop = System.getProperty("lucenenet.work.dir");
            baseDir = (prop != null && !prop.isEmpty())
                ? Paths.get(prop)
                : Paths.get("work", "java");
        }
        Files.createDirectories(baseDir);

        write(baseDir.resolve("index." + codecName + ".cfs"), true);
        write(baseDir.resolve("index." + codecName + ".nocfs"), false);

        System.out.println("Wrote Java " + codecName + " compatibility indexes under: " + baseDir.toAbsolutePath());
    }

    private static void write(Path indexPath, boolean useCompoundFile) throws Exception {
        File dirFile = indexPath.toFile();
        if (dirFile.exists()) {
            File[] files = dirFile.listFiles();
            if (files == null) {
                throw new IllegalStateException("Index path exists but is not a directory: " + dirFile.getAbsolutePath());
            }
            for (File f : files) {
                if (!f.delete()) {
                    throw new IllegalStateException("Failed to delete existing file: " + f.getAbsolutePath());
                }
            }
        } else {
            Files.createDirectories(indexPath);
        }
        try (Directory dir = new SimpleFSDirectory(dirFile)) {
            CompatDocs.writeIndex(dir, useCompoundFile);
            CompatDocs.checkIndex(dir, System.out);
        }
        System.out.println("  " + (useCompoundFile ? "cfs   " : "nocfs ") + "-> " + indexPath.toAbsolutePath());
    }
}
