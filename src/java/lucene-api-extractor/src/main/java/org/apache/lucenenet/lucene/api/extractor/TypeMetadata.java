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

public record TypeMetadata(
        String packageName,
        String kind,
        String name,
        String baseType,
        List<String> interfaces,
        List<String> modifiers,
        List<MethodMetadata> methods,
        List<FieldMetadata> fields) {
    public int compareTo(TypeMetadata other) {
        var packageCompare = this.packageName.compareTo(other.packageName);
        var kindCompare = this.kind.compareTo(other.kind);
        var nameCompare = this.name.compareTo(other.name);

        if (packageCompare == 0) {
            if (kindCompare == 0) {
                if (nameCompare == 0) {
                    return this.baseType.compareTo(other.baseType);
                }
                return nameCompare;
            }
            return kindCompare;
        }

        return packageCompare;
    }
}
