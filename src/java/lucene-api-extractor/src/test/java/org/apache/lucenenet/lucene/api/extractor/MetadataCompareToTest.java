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

import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.*;

class MetadataCompareToTest {

    private static ParameterMetadata param(String name, String type) {
        return new ParameterMetadata(name, type, type, List.of());
    }

    private static MethodMetadata method(String name, String returnType, List<ParameterMetadata> params) {
        return new MethodMetadata(name, returnType, returnType, params, List.of(), List.of(), List.of(), List.of(), false);
    }

    private static FieldMetadata field(String name) {
        return new FieldMetadata(name, "java.lang.String", "java.lang.String", List.of(), List.of(), false);
    }

    private static TypeMetadata type(String pkg, String kind, String fullName) {
        return new TypeMetadata(pkg, kind, fullName.substring(fullName.lastIndexOf('.') + 1), fullName,
                null, null, null, List.of(), List.of(), List.of(), List.of(), List.of(), List.of(), List.of(), List.of());
    }

    @Nested
    class Parameter {
        @Test
        void sortsByTypeFirst() {
            var a = param("z", "java.lang.Integer");
            var b = param("a", "java.lang.String");
            assertTrue(a.compareTo(b) < 0);
        }

        @Test
        void tieBreaksByName_whenTypesEqual() {
            var a = param("aaa", "java.lang.String");
            var b = param("zzz", "java.lang.String");
            assertTrue(a.compareTo(b) < 0);
        }

        @Test
        void equalParametersReturnZero() {
            var a = param("x", "int");
            var b = param("x", "int");
            assertEquals(0, a.compareTo(b));
        }
    }

    @Nested
    class Method {
        @Test
        void sortsByNameFirst() {
            var a = method("aaa", "void", List.of());
            var b = method("zzz", "void", List.of());
            assertTrue(a.compareTo(b) < 0);
        }

        @Test
        void tieBreaksByArity_whenNameEqual() {
            var a = method("m", "void", List.of());
            var b = method("m", "void", List.of(param("x", "int")));
            assertTrue(a.compareTo(b) < 0);
        }

        @Test
        void tieBreaksByParameterTypes_whenArityEqual() {
            var a = method("m", "void", List.of(param("x", "java.lang.Integer")));
            var b = method("m", "void", List.of(param("y", "java.lang.String")));
            assertTrue(a.compareTo(b) < 0);
        }

        @Test
        void tieBreaksByReturnType_whenNameAndParamsEqual() {
            var a = method("m", "int", List.of());
            var b = method("m", "java.lang.String", List.of());
            assertTrue(a.compareTo(b) < 0);
        }

        @Test
        void equalMethodsReturnZero() {
            var a = method("m", "int", List.of(param("x", "int")));
            var b = method("m", "int", List.of(param("x", "int")));
            assertEquals(0, a.compareTo(b));
        }
    }

    @Nested
    class Field {
        @Test
        void sortsByName() {
            assertTrue(field("aaa").compareTo(field("zzz")) < 0);
            assertTrue(field("zzz").compareTo(field("aaa")) > 0);
            assertEquals(0, field("x").compareTo(field("x")));
        }
    }

    @Nested
    class Type {
        @Test
        void sortsByPackageFirst() {
            var a = type("a.pkg", "class", "a.pkg.Zeta");
            var b = type("b.pkg", "class", "b.pkg.Alpha");
            assertTrue(a.compareTo(b) < 0);
        }

        @Test
        void tieBreaksByFullName_whenPackageEqual() {
            var a = type("org.x", "class", "org.x.A");
            var b = type("org.x", "class", "org.x.B");
            assertTrue(a.compareTo(b) < 0);
        }

        @Test
        void nestedTypesSortNearTheirEnclosing() {
            // "org.x.A$Inner" > "org.x.A" but < "org.x.B"
            var outer = type("org.x", "class", "org.x.A");
            var inner = type("org.x", "class", "org.x.A$Inner");
            var sibling = type("org.x", "class", "org.x.B");
            assertTrue(outer.compareTo(inner) < 0);
            assertTrue(inner.compareTo(sibling) < 0);
        }

        @Test
        void tieBreaksByKind_whenFullNameEqual() {
            var c = type("org.x", "class", "org.x.X");
            var i = type("org.x", "interface", "org.x.X");
            assertTrue(c.compareTo(i) < 0);
        }
    }

    @Nested
    class Constructor {
        @Test
        void sortsByArity() {
            var a = new ConstructorMetadata(List.of(), List.of(), List.of(), List.of(), false);
            var b = new ConstructorMetadata(List.of(param("x", "int")), List.of(), List.of(), List.of(), false);
            assertTrue(a.compareTo(b) < 0);
        }

        @Test
        void tieBreaksByParameterType() {
            var a = new ConstructorMetadata(List.of(param("x", "java.lang.Integer")), List.of(), List.of(), List.of(), false);
            var b = new ConstructorMetadata(List.of(param("y", "java.lang.String")), List.of(), List.of(), List.of(), false);
            assertTrue(a.compareTo(b) < 0);
        }
    }

    @Nested
    class Annotation {
        @Test
        void sortsByTypeName() {
            var a = new AnnotationMetadata("java.lang.Deprecated");
            var b = new AnnotationMetadata("java.lang.Override");
            assertTrue(a.compareTo(b) < 0);
        }
    }
}
