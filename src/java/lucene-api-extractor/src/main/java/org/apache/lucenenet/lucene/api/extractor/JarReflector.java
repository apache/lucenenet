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

import java.lang.annotation.Annotation;
import java.lang.reflect.Constructor;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.lang.reflect.Type;
import java.lang.reflect.TypeVariable;
import java.util.ArrayList;
import java.util.Enumeration;
import java.util.List;
import java.util.jar.JarEntry;
import java.util.jar.JarFile;
import java.util.stream.Stream;

public class JarReflector {

    public static List<LibraryResult> reflectOverJars(ExtractContext context) throws Exception {
        var classLoader = JarLoader.loadJars(context);
        var libraries = context.getLibraries();
        var results = new ArrayList<LibraryResult>();

        for (MavenCoordinates library : libraries) {
            var types = reflectOverJar(context, classLoader, library);
            results.add(new LibraryResult(library, types));
        }

        results.sort(LibraryResult::compareTo);
        return results;
    }

    public static List<TypeMetadata> reflectOverJar(ExtractContext context, ClassLoader classLoader, MavenCoordinates library) throws Exception {
        if (!context.isStandardOutput()) {
            System.out.println("Reflecting over jar: " + library.getJarName());
        }

        var types = new ArrayList<TypeMetadata>();

        try (JarFile jarFile = new JarFile(library.getFullJarPath(context))) {
            Enumeration<JarEntry> entries = jarFile.entries();

            while (entries.hasMoreElements()) {
                JarEntry entry = entries.nextElement();
                String entryName = entry.getName();

                if (!entryName.endsWith(".class")) {
                    continue;
                }

                int lastSlash = entryName.lastIndexOf("/");
                String packageName = lastSlash < 0 ? "" : entryName.substring(0, lastSlash).replace("/", ".");
                String fullClassName = entryName.substring(0, entryName.length() - 6).replace("/", ".");

                try {
                    Class<?> clazz = Class.forName(fullClassName, false, classLoader);

                    // Filter to API-visible types only: public or protected.
                    int typeMods = clazz.getModifiers();
                    if (!Modifier.isPublic(typeMods) && !Modifier.isProtected(typeMods)) {
                        continue;
                    }

                    types.add(buildTypeMetadata(clazz, packageName));
                } catch (UnsatisfiedLinkError e) {
                    System.err.println("UnsatisfiedLinkError loading class: " + fullClassName + " - " + e.getMessage());
                } catch (ClassNotFoundException | NoClassDefFoundError e) {
                    System.err.println("Failed to load class: " + fullClassName + " - " + e.getMessage());
                }
            }
        }

        types.sort(TypeMetadata::compareTo);

        return types;
    }

    static TypeMetadata buildTypeMetadata(Class<?> clazz, String packageName) {
        int typeMods = clazz.getModifiers();
        var modifiers = getModifiers(typeMods, clazz);
        modifiers.sort(String::compareTo);

        var constructors = new ArrayList<ConstructorMetadata>();
        for (Constructor<?> ctor : clazz.getDeclaredConstructors()) {
            int cMods = ctor.getModifiers();
            if (!Modifier.isPublic(cMods) && !Modifier.isProtected(cMods)) {
                continue;
            }
            if (ctor.isSynthetic()) {
                continue;
            }
            var ctorModifiers = getModifiers(cMods, null);
            ctorModifiers.sort(String::compareTo);
            constructors.add(new ConstructorMetadata(
                    buildParameters(ctor.getParameters(), ctor.getGenericParameterTypes()),
                    ctorModifiers,
                    getTypeNames(ctor.getGenericExceptionTypes()),
                    getAnnotations(ctor.getDeclaredAnnotations()),
                    ctor.isVarArgs()
            ));
        }
        constructors.sort(ConstructorMetadata::compareTo);

        var methodList = new ArrayList<MethodMetadata>();
        for (Method method : clazz.getDeclaredMethods()) {
            if (!method.getDeclaringClass().equals(clazz)) {
                continue;
            }
            if (method.isBridge() || method.isSynthetic()) {
                continue;
            }
            int mMods = method.getModifiers();
            if (!Modifier.isPublic(mMods) && !Modifier.isProtected(mMods)) {
                continue;
            }
            var methodModifiers = getModifiers(mMods, null);
            methodModifiers.sort(String::compareTo);
            methodList.add(new MethodMetadata(
                    method.getName(),
                    method.getReturnType().getTypeName(),
                    method.getGenericReturnType().getTypeName(),
                    buildParameters(method.getParameters(), method.getGenericParameterTypes()),
                    methodModifiers,
                    getTypeParameterNames(method.getTypeParameters()),
                    getTypeNames(method.getGenericExceptionTypes()),
                    getAnnotations(method.getDeclaredAnnotations()),
                    method.isVarArgs()
            ));
        }
        methodList.sort(MethodMetadata::compareTo);

        var fieldList = new ArrayList<FieldMetadata>();
        for (Field field : clazz.getDeclaredFields()) {
            if (!field.getDeclaringClass().equals(clazz)) {
                continue;
            }
            if (field.isSynthetic()) {
                continue;
            }
            int fMods = field.getModifiers();
            if (!Modifier.isPublic(fMods) && !Modifier.isProtected(fMods)) {
                continue;
            }
            var fieldModifiers = getModifiers(fMods, null);
            fieldModifiers.sort(String::compareTo);
            fieldList.add(new FieldMetadata(
                    field.getName(),
                    field.getType().getTypeName(),
                    field.getGenericType().getTypeName(),
                    fieldModifiers,
                    getAnnotations(field.getDeclaredAnnotations()),
                    Modifier.isStatic(fMods)
            ));
        }
        fieldList.sort(FieldMetadata::compareTo);

        var superClass = clazz.getSuperclass();
        var enclosing = clazz.getEnclosingClass();

        return new TypeMetadata(
                packageName,
                kindOf(clazz),
                clazz.getSimpleName(),
                clazz.getTypeName(),
                enclosing != null ? enclosing.getTypeName() : null,
                superClass != null ? superClass.getTypeName() : null,
                clazz.getGenericSuperclass() != null ? clazz.getGenericSuperclass().getTypeName() : null,
                Stream.of(clazz.getInterfaces()).map(Class::getTypeName).sorted().toList(),
                Stream.of(clazz.getGenericInterfaces()).map(Type::getTypeName).sorted().toList(),
                modifiers,
                getTypeParameterNames(clazz.getTypeParameters()),
                getAnnotations(clazz.getDeclaredAnnotations()),
                constructors,
                methodList,
                fieldList
        );
    }

    static String kindOf(Class<?> clazz) {
        if (clazz.isAnnotation()) {
            return "annotation";
        }
        if (clazz.isEnum()) {
            return "enum";
        }
        if (clazz.isRecord()) {
            return "record";
        }
        if (clazz.isInterface()) {
            return "interface";
        }
        return "class";
    }

    private static List<ParameterMetadata> buildParameters(java.lang.reflect.Parameter[] params, Type[] genericTypes) {
        var result = new ArrayList<ParameterMetadata>(params.length);
        for (int i = 0; i < params.length; i++) {
            var p = params[i];
            var genericType = i < genericTypes.length ? genericTypes[i].getTypeName() : p.getType().getTypeName();
            result.add(new ParameterMetadata(
                    p.getName(),
                    p.getType().getTypeName(),
                    genericType,
                    getAnnotations(p.getDeclaredAnnotations())
            ));
        }
        return result;
    }

    private static List<String> getTypeNames(Type[] types) {
        var result = new ArrayList<String>(types.length);
        for (var t : types) {
            result.add(t.getTypeName());
        }
        result.sort(String::compareTo);
        return result;
    }

    private static List<String> getTypeParameterNames(TypeVariable<?>[] typeParameters) {
        var result = new ArrayList<String>(typeParameters.length);
        for (var tp : typeParameters) {
            var bounds = tp.getBounds();
            if (bounds.length == 0 || (bounds.length == 1 && bounds[0].getTypeName().equals("java.lang.Object"))) {
                result.add(tp.getName());
            } else {
                var sb = new StringBuilder(tp.getName()).append(" extends ");
                for (int i = 0; i < bounds.length; i++) {
                    if (i > 0) {
                        sb.append(" & ");
                    }
                    sb.append(bounds[i].getTypeName());
                }
                result.add(sb.toString());
            }
        }
        return result;
    }

    private static List<AnnotationMetadata> getAnnotations(Annotation[] annotations) {
        var result = new ArrayList<AnnotationMetadata>(annotations.length);
        for (var a : annotations) {
            result.add(new AnnotationMetadata(a.annotationType().getTypeName()));
        }
        result.sort(AnnotationMetadata::compareTo);
        return result;
    }

    static ArrayList<String> getModifiers(int value, Class<?> typeContext) {
        var modifiers = new ArrayList<String>();

        if (Modifier.isPublic(value)) {
            modifiers.add("public");
        }
        if (Modifier.isProtected(value)) {
            modifiers.add("protected");
        }
        if (Modifier.isPrivate(value)) {
            modifiers.add("private");
        }
        // For types, suppress redundant "abstract" on interfaces/annotations.
        // For members, "abstract" is always meaningful.
        boolean suppressAbstract = typeContext != null
                && (Modifier.isInterface(value) || typeContext.isAnnotation());
        if (Modifier.isAbstract(value) && !suppressAbstract) {
            modifiers.add("abstract");
        }
        if (Modifier.isFinal(value)) {
            modifiers.add("final");
        }
        if (Modifier.isStatic(value)) {
            modifiers.add("static");
        }
        if (Modifier.isSynchronized(value)) {
            modifiers.add("synchronized");
        }
        if (Modifier.isNative(value)) {
            modifiers.add("native");
        }
        if (Modifier.isStrict(value)) {
            modifiers.add("strictfp");
        }
        if (Modifier.isTransient(value)) {
            modifiers.add("transient");
        }
        if (Modifier.isVolatile(value)) {
            modifiers.add("volatile");
        }
        return modifiers;
    }
}
