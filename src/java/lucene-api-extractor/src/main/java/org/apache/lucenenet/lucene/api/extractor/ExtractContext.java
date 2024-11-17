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
    private final String luceneVersion;
    private final Library[] libraries;
    private final boolean force;
    private final String outputFile;

    public ExtractContext(String downloadsDir, String luceneVersion, String[] libraryNames, boolean force, String outputFile) {
        this.downloadsDir = downloadsDir;
        this.luceneVersion = luceneVersion;
        this.libraries = Stream.of(libraryNames)
                .map(libraryName -> new Library(libraryName, luceneVersion))
                .toArray(Library[]::new);
        this.force = force;
        this.outputFile = outputFile;
    }

    public String getDownloadsDir() {
        return downloadsDir;
    }

    public String getLuceneVersion() {
        return luceneVersion;
    }

    public Library[] getLibraries() {
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
}
