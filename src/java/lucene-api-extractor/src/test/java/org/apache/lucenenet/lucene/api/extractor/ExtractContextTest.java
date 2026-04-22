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

import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

class ExtractContextTest {

    @Test
    void parsesLibrariesAsMavenCoordinates() {
        var context = new ExtractContext(
                "download",
                new String[]{"org.apache.lucene:lucene-core:4.8.1", "org.apache.lucene:lucene-analyzers-common:4.8.1"},
                false,
                null,
                new String[0]);

        var libs = context.getLibraries();
        assertEquals(2, libs.length);
        assertEquals(new MavenCoordinates("org.apache.lucene", "lucene-core", "4.8.1"), libs[0]);
        assertEquals(new MavenCoordinates("org.apache.lucene", "lucene-analyzers-common", "4.8.1"), libs[1]);
    }

    @Test
    void parsesDependenciesAsMavenCoordinates() {
        var context = new ExtractContext(
                "download",
                new String[]{"org.apache.lucene:lucene-core:4.8.1"},
                false,
                null,
                new String[]{"com.ibm.icu:icu4j:54.1"});

        var deps = context.getDependencies();
        assertEquals(1, deps.length);
        assertEquals(new MavenCoordinates("com.ibm.icu", "icu4j", "54.1"), deps[0]);
    }

    @Test
    void isStandardOutput_trueWhenOutputFileNull() {
        var context = new ExtractContext("download", new String[]{"g:a:v"}, false, null, new String[0]);
        assertTrue(context.isStandardOutput());
    }

    @Test
    void isStandardOutput_falseWhenOutputFileProvided() {
        var context = new ExtractContext("download", new String[]{"g:a:v"}, false, "out.json", new String[0]);
        assertFalse(context.isStandardOutput());
        assertEquals("out.json", context.getOutputFile());
    }

    @Test
    void emptyDependenciesArrayIsHandled() {
        var context = new ExtractContext("download", new String[]{"g:a:v"}, false, null, new String[0]);
        assertEquals(0, context.getDependencies().length);
    }

    @Test
    void downloadsDirAndForceAreExposed() {
        var context = new ExtractContext("my-dir", new String[]{"g:a:v"}, true, null, new String[0]);
        assertEquals("my-dir", context.getDownloadsDir());
        assertTrue(context.isForce());
    }
}
