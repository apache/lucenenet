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

import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;

/**
 * Integration tests — require network access to Maven Central.
 * Run with: {@code mvn -Pintegration-tests test}
 */
@Tag("integration")
class ExtractRunnerTest {

    @Test
    void testHashIsStable() throws Exception {
        var context1 = new ExtractContext("download", new String[] { "org.apache.lucene:lucene-core:4.8.1", "org.apache.lucene:lucene-analyzers-common:4.8.1" }, false, null, new String[0]);
        var hash1 = ExtractRunner.getHash(context1);
        var context2 = new ExtractContext("download", new String[] { "org.apache.lucene:lucene-core:4.8.1", "org.apache.lucene:lucene-analyzers-common:4.8.1" }, false, null, new String[0]);
        var hash2 = ExtractRunner.getHash(context2);
        assertEquals(hash1, hash2);
    }
}
