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

import java.util.List;

import static org.junit.jupiter.api.Assertions.*;

class JsonSerializerTest {

    @Test
    void serializesMavenCoordinates() {
        var json = JsonSerializer.serialize(new MavenCoordinates("org.foo", "bar", "1.0"));
        assertTrue(json.contains("\"groupId\":\"org.foo\""));
        assertTrue(json.contains("\"artifactId\":\"bar\""));
        assertTrue(json.contains("\"version\":\"1.0\""));
    }

    @Test
    void serializesLibraryResultWithTypesArray() {
        var result = new LibraryResult(
                new MavenCoordinates("g", "a", "v"),
                List.of());
        var json = JsonSerializer.serialize(result);

        assertTrue(json.contains("\"library\""));
        assertTrue(json.contains("\"types\":[]"));
    }

    @Test
    void serializesTypeMetadataFieldsAsCamelCase() {
        var type = new TypeMetadata(
                "pkg", "class", "Foo", "pkg.Foo", null, null, null,
                List.of(), List.of(), List.of("public"), List.of(), List.of(),
                List.of(), List.of(), List.of());
        var json = JsonSerializer.serialize(type);

        assertTrue(json.contains("\"packageName\":\"pkg\""));
        assertTrue(json.contains("\"fullName\":\"pkg.Foo\""));
        assertTrue(json.contains("\"enclosingType\":null"));
        assertTrue(json.contains("\"constructors\":[]"));
    }

    @Test
    void serializesAnnotationMetadata() {
        var ann = new AnnotationMetadata("java.lang.Deprecated");
        var json = JsonSerializer.serialize(ann);
        assertEquals("{\"type\":\"java.lang.Deprecated\"}", json);
    }

    @Test
    void isStableAcrossCalls() {
        var a = new MavenCoordinates("g", "a", "1");
        assertEquals(JsonSerializer.serialize(a), JsonSerializer.serialize(a));
    }
}
