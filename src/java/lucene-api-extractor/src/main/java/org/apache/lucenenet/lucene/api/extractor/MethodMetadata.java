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

import java.util.List;

public record MethodMetadata(String name,
                             String returnType,
                             String genericReturnType,
                             List<ParameterMetadata> parameters,
                             List<String> modifiers,
                             List<String> typeParameters,
                             List<String> throwsTypes,
                             List<AnnotationMetadata> annotations,
                             boolean isVarArgs) implements Comparable<MethodMetadata> {
    public MethodMetadata {
        parameters = List.copyOf(parameters);
        modifiers = List.copyOf(modifiers);
        typeParameters = List.copyOf(typeParameters);
        throwsTypes = List.copyOf(throwsTypes);
        annotations = List.copyOf(annotations);
    }

    @Override
    public int compareTo(MethodMetadata other) {
        var nameComparison = this.name.compareTo(other.name);
        if (nameComparison != 0) {
            return nameComparison;
        }

        if (this.parameters.size() != other.parameters.size()) {
            return this.parameters.size() - other.parameters.size();
        }

        for (int i = 0; i < this.parameters.size(); i++) {
            var paramComparison = this.parameters.get(i).compareTo(other.parameters.get(i));
            if (paramComparison != 0) {
                return paramComparison;
            }
        }

        return this.returnType.compareTo(other.returnType);
    }
}
