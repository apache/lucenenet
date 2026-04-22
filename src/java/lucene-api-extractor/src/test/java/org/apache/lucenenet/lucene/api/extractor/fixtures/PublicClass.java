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

package org.apache.lucenenet.lucene.api.extractor.fixtures;

import java.io.IOException;
import java.util.List;

/**
 * A hand-crafted public class exercising the full API surface the extractor should capture.
 */
@Deprecated
public class PublicClass<T extends Number> extends ParentClass implements PublicInterface {

    public static final String PUBLIC_CONST = "hello";
    protected int protectedField;
    private int privateField; // must be excluded
    static final int packagePrivateField = 42; // must be excluded

    public PublicClass() {
    }

    public PublicClass(String name) {
    }

    protected PublicClass(int x, int y) {
    }

    PublicClass(long ignored) {
        // package-private: must be excluded
    }

    private PublicClass(boolean ignored) {
        // private: must be excluded
    }

    public List<T> publicMethod(String arg) throws IOException {
        return null;
    }

    public <U> U genericMethod(U input) {
        return input;
    }

    public void varArgsMethod(String... args) {
    }

    protected void protectedMethod() {
    }

    void packagePrivateMethod() {
        // must be excluded
    }

    private void privateMethod() {
        // must be excluded
    }

    @Override
    @Deprecated
    public void interfaceMethod() {
    }

    public static class NestedPublic {
        public NestedPublic() {}
    }

    private static class NestedPrivate {
        // must be excluded at the type level
    }
}
