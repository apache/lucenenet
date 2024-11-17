package org.apache.lucenenet.lucene.api.extractor;

import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;

class ExtractRunnerTest {

    @Test
    void testHashIsStable() throws Exception {
        var context1 = new ExtractContext("download", "4.8.1", new String[] { "core", "analyzers-common" }, false, null);
        var hash1 = ExtractRunner.getHash(context1);
        var context2 = new ExtractContext("download", "4.8.1", new String[] { "core", "analyzers-common" }, false, null);
        var hash2 = ExtractRunner.getHash(context2);
        System.out.println(hash1);
        System.out.println(hash2);
        assertEquals(hash1, hash2);
    }
}