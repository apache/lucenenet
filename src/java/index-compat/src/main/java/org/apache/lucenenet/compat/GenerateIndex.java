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

import org.apache.lucene.store.Directory;
import org.apache.lucene.store.SimpleFSDirectory;

import java.io.File;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Writes the deterministic compatibility index (both compound-file and
 * non-compound-file variants) with Apache Lucene 4.8.1, for Lucene.NET to read
 * back. This is the Java side of the "Java -&gt; .NET" direction of issue #270.
 *
 * <p>Usage (from the {@code src/java/index-compat} directory):
 * <pre>
 *   ./mvnw -q compile exec:java
 *   ./mvnw -q compile exec:java -Dexec.args="/path/to/output"
 * </pre>
 *
 * <p>The output is written under a temporary, gitignored {@code work/java}
 * folder by default (or the directory named by the {@code lucenenet.work.dir}
 * system property, or the first command-line argument). Two subdirectories are
 * created: {@code index.481.cfs} and {@code index.481.nocfs}.
 */
public final class GenerateIndex {

    private GenerateIndex() {
    }

    public static void main(String[] args) throws Exception {
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

        write(baseDir.resolve("index.481.cfs"), true);
        write(baseDir.resolve("index.481.nocfs"), false);

        System.out.println("Wrote Java 4.8.1 compatibility indexes under: " + baseDir.toAbsolutePath());
    }

    private static void write(Path indexPath, boolean useCompoundFile) throws Exception {
        File dirFile = indexPath.toFile();
        if (dirFile.exists()) {
            for (File f : dirFile.listFiles()) {
                f.delete();
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
