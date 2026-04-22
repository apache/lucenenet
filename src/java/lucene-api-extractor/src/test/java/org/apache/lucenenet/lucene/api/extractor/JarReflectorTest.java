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

import org.apache.lucenenet.lucene.api.extractor.fixtures.PublicAnnotation;
import org.apache.lucenenet.lucene.api.extractor.fixtures.PublicClass;
import org.apache.lucenenet.lucene.api.extractor.fixtures.PublicEnum;
import org.apache.lucenenet.lucene.api.extractor.fixtures.PublicInterface;
import org.apache.lucenenet.lucene.api.extractor.fixtures.PublicRecord;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Test;

import java.lang.reflect.Modifier;

import static org.junit.jupiter.api.Assertions.*;

class JarReflectorTest {

    private static final String FIXTURES_PACKAGE = "org.apache.lucenenet.lucene.api.extractor.fixtures";

    @Nested
    class KindOf {
        @Test
        void reportsInterface() {
            assertEquals("interface", JarReflector.kindOf(PublicInterface.class));
        }

        @Test
        void reportsEnum() {
            assertEquals("enum", JarReflector.kindOf(PublicEnum.class));
        }

        @Test
        void reportsAnnotation() {
            assertEquals("annotation", JarReflector.kindOf(PublicAnnotation.class));
        }

        @Test
        void reportsRecord() {
            assertEquals("record", JarReflector.kindOf(PublicRecord.class));
        }

        @Test
        void reportsClass() {
            assertEquals("class", JarReflector.kindOf(PublicClass.class));
        }
    }

    @Nested
    class GetModifiers {
        @Test
        void returnsPublic() {
            var mods = JarReflector.getModifiers(Modifier.PUBLIC, null);
            assertTrue(mods.contains("public"));
        }

        @Test
        void returnsMultipleModifiers() {
            var mods = JarReflector.getModifiers(Modifier.PUBLIC | Modifier.STATIC | Modifier.FINAL, null);
            assertTrue(mods.contains("public"));
            assertTrue(mods.contains("static"));
            assertTrue(mods.contains("final"));
        }

        @Test
        void returnsNative() {
            var mods = JarReflector.getModifiers(Modifier.PUBLIC | Modifier.NATIVE, null);
            assertTrue(mods.contains("native"));
        }

        @Test
        void returnsStrictfp() {
            var mods = JarReflector.getModifiers(Modifier.PUBLIC | Modifier.STRICT, null);
            assertTrue(mods.contains("strictfp"));
        }

        @Test
        void returnsTransientAndVolatile() {
            var t = JarReflector.getModifiers(Modifier.TRANSIENT, null);
            var v = JarReflector.getModifiers(Modifier.VOLATILE, null);
            assertTrue(t.contains("transient"));
            assertTrue(v.contains("volatile"));
        }

        @Test
        void suppressesAbstractOnInterfaceType() {
            // When reflecting over an interface type, the JVM sets ABSTRACT as well — we should suppress it.
            var mods = JarReflector.getModifiers(PublicInterface.class.getModifiers(), PublicInterface.class);
            assertTrue(mods.contains("public"));
            assertFalse(mods.contains("abstract"));
        }

        @Test
        void keepsAbstractOnAbstractClassType() {
            int flags = Modifier.PUBLIC | Modifier.ABSTRACT;
            var mods = JarReflector.getModifiers(flags, PublicClass.class);
            assertTrue(mods.contains("abstract"));
        }

        @Test
        void keepsAbstractOnMembers_whenNoTypeContext() {
            var mods = JarReflector.getModifiers(Modifier.PUBLIC | Modifier.ABSTRACT, null);
            assertTrue(mods.contains("abstract"));
        }
    }

    @Nested
    class BuildTypeMetadata {
        @Test
        void publicClass_basicShape() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            assertEquals(FIXTURES_PACKAGE, md.packageName());
            assertEquals("class", md.kind());
            assertEquals("PublicClass", md.name());
            assertEquals(PublicClass.class.getTypeName(), md.fullName());
            assertTrue(md.modifiers().contains("public"));
        }

        @Test
        void capturesBaseTypeAndInterfaces() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            assertEquals(FIXTURES_PACKAGE + ".ParentClass", md.baseType());
            assertTrue(md.interfaces().contains(FIXTURES_PACKAGE + ".PublicInterface"));
        }

        @Test
        void capturesTypeParameters() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            // "T extends java.lang.Number"
            assertEquals(1, md.typeParameters().size());
            assertTrue(md.typeParameters().get(0).startsWith("T"));
            assertTrue(md.typeParameters().get(0).contains("java.lang.Number"));
        }

        @Test
        void capturesTypeAnnotations() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            assertTrue(md.annotations().stream()
                    .anyMatch(a -> a.type().equals("java.lang.Deprecated")));
        }

        @Test
        void filtersNonPublicConstructors() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            // PublicClass has: public(), public(String), protected(int,int), package-private(long), private(boolean).
            // Only the first three should survive.
            assertEquals(3, md.constructors().size());

            // Sorted by arity.
            assertEquals(0, md.constructors().get(0).parameters().size());
            assertEquals(1, md.constructors().get(1).parameters().size());
            assertEquals(2, md.constructors().get(2).parameters().size());
        }

        @Test
        void constructorsIncludeModifiers() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            var twoArg = md.constructors().get(2);
            assertTrue(twoArg.modifiers().contains("protected"));
            assertFalse(twoArg.modifiers().contains("public"));
        }

        @Test
        void filtersInheritedMethods() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            assertTrue(md.methods().stream().noneMatch(m -> m.name().equals("inheritedMethod")),
                    "Methods declared on the parent class must not be extracted");
        }

        @Test
        void filtersNonPublicMethods() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            assertTrue(md.methods().stream().noneMatch(m -> m.name().equals("packagePrivateMethod")));
            assertTrue(md.methods().stream().noneMatch(m -> m.name().equals("privateMethod")));
        }

        @Test
        void includesPublicAndProtectedMethods() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            assertTrue(md.methods().stream().anyMatch(m -> m.name().equals("publicMethod")));
            assertTrue(md.methods().stream().anyMatch(m -> m.name().equals("protectedMethod")));
            assertTrue(md.methods().stream().anyMatch(m -> m.name().equals("interfaceMethod")));
        }

        @Test
        void capturesMethodGenericReturnType() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            var m = md.methods().stream()
                    .filter(x -> x.name().equals("publicMethod"))
                    .findFirst().orElseThrow();

            assertEquals("java.util.List", m.returnType());
            assertEquals("java.util.List<T>", m.genericReturnType());
        }

        @Test
        void capturesMethodThrowsTypes() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            var m = md.methods().stream()
                    .filter(x -> x.name().equals("publicMethod"))
                    .findFirst().orElseThrow();

            assertTrue(m.throwsTypes().contains("java.io.IOException"));
        }

        @Test
        void capturesMethodTypeParameters() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            var m = md.methods().stream()
                    .filter(x -> x.name().equals("genericMethod"))
                    .findFirst().orElseThrow();

            assertEquals(1, m.typeParameters().size());
            assertEquals("U", m.typeParameters().get(0));
        }

        @Test
        void capturesVarArgsFlag() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            var m = md.methods().stream()
                    .filter(x -> x.name().equals("varArgsMethod"))
                    .findFirst().orElseThrow();

            assertTrue(m.isVarArgs());
        }

        @Test
        void capturesMethodAnnotations() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            var m = md.methods().stream()
                    .filter(x -> x.name().equals("interfaceMethod"))
                    .findFirst().orElseThrow();

            assertTrue(m.annotations().stream()
                    .anyMatch(a -> a.type().equals("java.lang.Deprecated")));
        }

        @Test
        void filtersNonPublicFields() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            assertTrue(md.fields().stream().noneMatch(f -> f.name().equals("privateField")));
            assertTrue(md.fields().stream().noneMatch(f -> f.name().equals("packagePrivateField")));
        }

        @Test
        void includesPublicAndProtectedFields() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            assertTrue(md.fields().stream().anyMatch(f -> f.name().equals("PUBLIC_CONST")));
            assertTrue(md.fields().stream().anyMatch(f -> f.name().equals("protectedField")));
        }

        @Test
        void capturesFieldStaticFlag() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            var f = md.fields().stream()
                    .filter(x -> x.name().equals("PUBLIC_CONST"))
                    .findFirst().orElseThrow();

            assertTrue(f.isStatic());
            assertTrue(f.modifiers().contains("static"));
        }

        @Test
        void enumKindIsReported() {
            var md = JarReflector.buildTypeMetadata(PublicEnum.class, FIXTURES_PACKAGE);
            assertEquals("enum", md.kind());
        }

        @Test
        void interfaceKindIsReportedAndAbstractIsSuppressed() {
            var md = JarReflector.buildTypeMetadata(PublicInterface.class, FIXTURES_PACKAGE);
            assertEquals("interface", md.kind());
            assertFalse(md.modifiers().contains("abstract"),
                    "abstract should be suppressed on interface types");
        }

        @Test
        void annotationKindIsReported() {
            var md = JarReflector.buildTypeMetadata(PublicAnnotation.class, FIXTURES_PACKAGE);
            assertEquals("annotation", md.kind());
        }

        @Test
        void recordKindIsReported() {
            var md = JarReflector.buildTypeMetadata(PublicRecord.class, FIXTURES_PACKAGE);
            assertEquals("record", md.kind());
        }

        @Test
        void nestedTypeReportsEnclosingType() {
            var md = JarReflector.buildTypeMetadata(PublicClass.NestedPublic.class, FIXTURES_PACKAGE);
            assertEquals(PublicClass.class.getTypeName(), md.enclosingType());
        }

        @Test
        void topLevelTypeHasNullEnclosingType() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);
            assertNull(md.enclosingType());
        }

        @Test
        void methodsAreSorted() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            for (int i = 1; i < md.methods().size(); i++) {
                assertTrue(md.methods().get(i - 1).compareTo(md.methods().get(i)) <= 0,
                        "methods must be in sorted order");
            }
        }

        @Test
        void fieldsAreSorted() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);

            for (int i = 1; i < md.fields().size(); i++) {
                assertTrue(md.fields().get(i - 1).compareTo(md.fields().get(i)) <= 0,
                        "fields must be in sorted order");
            }
        }
    }

    @Nested
    class ExtractHelpers {
        @Test
        void extractConstructorsMatchesBuildTypeMetadata() {
            var ctors = JarReflector.extractConstructors(PublicClass.class);
            var fromType = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE).constructors();
            assertEquals(fromType, ctors);
        }

        @Test
        void extractMethodsMatchesBuildTypeMetadata() {
            var methods = JarReflector.extractMethods(PublicClass.class);
            var fromType = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE).methods();
            assertEquals(fromType, methods);
        }

        @Test
        void extractFieldsMatchesBuildTypeMetadata() {
            var fields = JarReflector.extractFields(PublicClass.class);
            var fromType = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE).fields();
            assertEquals(fromType, fields);
        }

        @Test
        void extractConstructorsOnInterfaceReturnsEmpty() {
            assertTrue(JarReflector.extractConstructors(PublicInterface.class).isEmpty());
        }
    }

    @Nested
    class Immutability {
        @Test
        void typeMetadataListsAreUnmodifiable() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);
            assertThrows(UnsupportedOperationException.class, () -> md.methods().clear());
            assertThrows(UnsupportedOperationException.class, () -> md.fields().clear());
            assertThrows(UnsupportedOperationException.class, () -> md.constructors().clear());
            assertThrows(UnsupportedOperationException.class, () -> md.modifiers().clear());
            assertThrows(UnsupportedOperationException.class, () -> md.annotations().clear());
        }

        @Test
        void methodParametersAreUnmodifiable() {
            var md = JarReflector.buildTypeMetadata(PublicClass.class, FIXTURES_PACKAGE);
            var m = md.methods().stream()
                    .filter(x -> x.name().equals("publicMethod"))
                    .findFirst().orElseThrow();
            assertThrows(UnsupportedOperationException.class, () -> m.parameters().clear());
            assertThrows(UnsupportedOperationException.class, () -> m.throwsTypes().clear());
        }
    }
}
