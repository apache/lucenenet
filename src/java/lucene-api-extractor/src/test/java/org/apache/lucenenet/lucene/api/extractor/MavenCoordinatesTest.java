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

import java.io.File;

import static org.junit.jupiter.api.Assertions.*;

class MavenCoordinatesTest {

    @Test
    void getJarName_buildsArtifactDashVersionDotJar() {
        var coords = new MavenCoordinates("org.apache.lucene", "lucene-core", "4.8.1");
        assertEquals("lucene-core-4.8.1.jar", coords.getJarName());
    }

    @Test
    void getFullJarPath_combinesDownloadDirAndJarName() {
        var coords = new MavenCoordinates("org.apache.lucene", "lucene-core", "4.8.1");
        var context = new ExtractContext("some/dir", new String[0], false, null, new String[0]);

        File path = coords.getFullJarPath(context);

        assertEquals(new File("some/dir", "lucene-core-4.8.1.jar"), path);
    }

    @Test
    void compareTo_groupIdTakesPrecedence() {
        var a = new MavenCoordinates("aaa", "zzz", "9.9");
        var b = new MavenCoordinates("bbb", "aaa", "1.0");
        assertTrue(a.compareTo(b) < 0);
        assertTrue(b.compareTo(a) > 0);
    }

    @Test
    void compareTo_artifactIdTieBreaksWhenGroupEqual() {
        var a = new MavenCoordinates("org", "aaa", "9.9");
        var b = new MavenCoordinates("org", "bbb", "1.0");
        assertTrue(a.compareTo(b) < 0);
    }

    @Test
    void compareTo_versionTieBreaksWhenGroupAndArtifactEqual() {
        var a = new MavenCoordinates("org", "lib", "1.0");
        var b = new MavenCoordinates("org", "lib", "2.0");
        assertTrue(a.compareTo(b) < 0);
    }

    @Test
    void compareTo_equalCoordinatesReturnZero() {
        var a = new MavenCoordinates("org", "lib", "1.0");
        var b = new MavenCoordinates("org", "lib", "1.0");
        assertEquals(0, a.compareTo(b));
    }

    @Test
    void implementsComparable() {
        assertTrue(Comparable.class.isAssignableFrom(MavenCoordinates.class));
    }
}
