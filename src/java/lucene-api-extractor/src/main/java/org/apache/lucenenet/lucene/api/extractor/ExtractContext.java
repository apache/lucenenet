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

import java.util.stream.Stream;

public class ExtractContext {
    private final String downloadsDir;
    private final MavenCoordinates[] libraries;
    private final boolean force;
    private final String outputFile;
    private final MavenCoordinates[] dependencies;

    public ExtractContext(String downloadsDir,
                          String[] libraryNames,
                          boolean force,
                          String outputFile,
                          String[] dependencies) {
        this.downloadsDir = downloadsDir;
        this.libraries = Stream.of(libraryNames)
                .map(libraryName -> {
                    var parts = libraryName.split(":");
                    return new MavenCoordinates(parts[0], parts[1], parts[2]);
                })
                .toArray(MavenCoordinates[]::new);
        this.force = force;
        this.outputFile = outputFile;
        this.dependencies = Stream.of(dependencies)
                .map(dependency -> {
                    var parts = dependency.split(":");
                    return new MavenCoordinates(parts[0], parts[1], parts[2]);
                })
                .toArray(MavenCoordinates[]::new);
    }

    public String getDownloadsDir() {
        return downloadsDir;
    }

    public MavenCoordinates[] getLibraries() {
        return libraries;
    }

    public boolean isForce() {
        return force;
    }

    public String getOutputFile() {
        return outputFile;
    }

    public boolean isStandardOutput() {
        return outputFile == null;
    }

    public MavenCoordinates[] getDependencies() {
        return dependencies;
    }
}
