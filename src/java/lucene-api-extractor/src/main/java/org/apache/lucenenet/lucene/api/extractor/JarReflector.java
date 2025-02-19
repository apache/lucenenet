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

import java.lang.reflect.Method;
import java.lang.reflect.Field;
import java.lang.reflect.Modifier;
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

        JarFile jarFile = new JarFile(library.getFullJarPath(context));
        Enumeration<JarEntry> entries = jarFile.entries();
        var types = new ArrayList<TypeMetadata>();

        while (entries.hasMoreElements()) {
            JarEntry entry = entries.nextElement();
            String entryName = entry.getName();

            // Process only class files
            if (entryName.endsWith(".class")) {
                // Convert "com/example/MyClass.class" to "com.example.MyClass"
                String packageName = entryName.substring(0, entryName.lastIndexOf("/")).replace("/", ".");
                String className = entryName.substring(packageName.length() + 1, entryName.length() - 6);
                String fullClassName = packageName + "." + className;

                try {
                    // Load the class
                    Class<?> clazz = Class.forName(fullClassName, true, classLoader);
                    var superClass = clazz.getSuperclass();
                    var modifiers = getModifiers(clazz.getModifiers());
                    modifiers.sort(String::compareTo);

                    var methodList = new ArrayList<MethodMetadata>();
                    for (Method method : clazz.getDeclaredMethods()) {
                        if (method.getDeclaringClass().equals(clazz) && !method.isBridge()) {
                            var methodModifiers = getModifiers(method.getModifiers());
                            methodModifiers.sort(String::compareTo);
                            var parameterTypes = Stream.of(method.getParameterTypes()).map(p -> new ParameterMetadata(p.getName(), p.getTypeName())).toList();
                            var returnType = method.getReturnType().getName();
                            var methodMetadata = new MethodMetadata(
                                    method.getName(),
                                    returnType,
                                    parameterTypes,
                                    methodModifiers,
                                    method.isVarArgs()
                            );
                            methodList.add(methodMetadata);
                        }
                    }
                    methodList.sort(MethodMetadata::compareTo);

                    var fieldList = new ArrayList<FieldMetadata>();
                    for (Field field : clazz.getDeclaredFields()) {
                        if (field.getDeclaringClass().equals(clazz)) {
                            var fieldModifiers = getModifiers(field.getModifiers());
                            fieldModifiers.sort(String::compareTo);
                            var fieldMetadata = new FieldMetadata(
                                    field.getName(),
                                    field.getType().getTypeName(),
                                    fieldModifiers
                            );
                            fieldList.add(fieldMetadata);
                        }
                    }
                    fieldList.sort(FieldMetadata::compareTo);

                    var type = new TypeMetadata(
                            packageName,
                            Modifier.isInterface(clazz.getModifiers()) ? "interface" : "class",
                            clazz.getSimpleName(),
                            clazz.getTypeName(),
                            superClass != null ? superClass.getTypeName() : null,
                            Stream.of(clazz.getInterfaces()).map(Class::getTypeName).sorted().toList(),
                            modifiers,
                            methodList,
                            fieldList
                    );

                    types.add(type);
                } catch (UnsatisfiedLinkError e) {
                    System.err.println("UnsatisfiedLinkError loading class: " + fullClassName + " - " + e.getMessage());
                } catch (ClassNotFoundException | NoClassDefFoundError e) {
                    throw new RuntimeException("Failed to load class: " + fullClassName, e);
                }
            }
        }

        jarFile.close();

        types.sort(TypeMetadata::compareTo);

        return types;
    }

    private static ArrayList<String> getModifiers(int value) {
        var modifiers = new ArrayList<String>();

        if (Modifier.isPublic(value)) {
            modifiers.add("public");
        }
        if (Modifier.isAbstract(value)) {
            modifiers.add("abstract");
        }
        if (Modifier.isFinal(value)) {
            modifiers.add("final");
        }
        if (Modifier.isPrivate(value)) {
            modifiers.add("private");
        }
        if (Modifier.isProtected(value)) {
            modifiers.add("protected");
        }
        if (Modifier.isStatic(value)) {
            modifiers.add("static");
        }
        if (Modifier.isSynchronized(value)) {
            modifiers.add("synchronized");
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

