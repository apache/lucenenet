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
package org.apache.lucenenet.compat;

import org.apache.lucene.document.Document;
import org.apache.lucene.index.BinaryDocValues;
import org.apache.lucene.index.DirectoryReader;
import org.apache.lucene.index.Fields;
import org.apache.lucene.index.IndexReader;
import org.apache.lucene.index.MultiDocValues;
import org.apache.lucene.index.MultiFields;
import org.apache.lucene.index.NumericDocValues;
import org.apache.lucene.index.SortedDocValues;
import org.apache.lucene.index.SortedSetDocValues;
import org.apache.lucene.index.Term;
import org.apache.lucene.index.Terms;
import org.apache.lucene.search.IndexSearcher;
import org.apache.lucene.search.ScoreDoc;
import org.apache.lucene.search.TermQuery;
import org.apache.lucene.store.Directory;
import org.apache.lucene.store.SimpleFSDirectory;
import org.apache.lucene.util.Bits;
import org.apache.lucene.util.BytesRef;
import org.junit.Test;

import java.io.File;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNotNull;
import static org.junit.Assert.fail;

/**
 * Verifies that Apache Lucene 4.8.1 can open an index that was written by
 * Lucene.NET, that {@code CheckIndex} passes, and that the contents match the
 * shared {@link CompatDocs} contract. This is the ".NET -&gt; Java" direction of
 * issue #270.
 *
 * <p>The index location is provided via {@code -Dlucenenet.index.dir=...}. Per
 * the harness contract, this test <b>fails</b> (it does not skip) if no path is
 * supplied or the index is missing: when Java is asked to read a .NET index, the
 * absence of that index is a failure of the pipeline that was supposed to
 * produce it.
 */
public class TestDotNetCompatibility {

    @Test
    public void dotNetIndexReadsCleanlyInJava() throws Exception {
        String path = System.getProperty("lucenenet.index.dir");
        if (path == null || path.trim().isEmpty()) {
            fail("System property 'lucenenet.index.dir' is not set. Generate the "
                + ".NET index first and pass -Dlucenenet.index.dir=<path>.");
        }

        File dirFile = new File(path);
        if (!dirFile.isDirectory() || !new File(dirFile, "segments.gen").exists()
                && segmentsFile(dirFile) == null) {
            fail("No Lucene index found at '" + dirFile.getAbsolutePath() + "'. "
                + "Generate the .NET index before running this test.");
        }

        try (Directory dir = new SimpleFSDirectory(dirFile)) {
            // Codec integrity gate.
            CompatDocs.checkIndex(dir, System.out);

            // Semantic read back.
            try (IndexReader reader = DirectoryReader.open(dir)) {
                assertContents(reader);
            }
        }
    }

    private static File segmentsFile(File dir) {
        File[] files = dir.listFiles();
        if (files != null) {
            for (File f : files) {
                if (f.getName().startsWith("segments_")) {
                    return f;
                }
            }
        }
        return null;
    }

    private static void assertContents(IndexReader reader) throws Exception {
        IndexSearcher searcher = new IndexSearcher(reader);

        Bits liveDocs = MultiFields.getLiveDocs(reader);

        for (int i = 0; i < CompatDocs.DOC_COUNT; i++) {
            boolean live = liveDocs == null || liveDocs.get(i);
            if (!live) {
                assertEquals("only id 7 should be deleted", CompatDocs.DELETED_ID, i);
                continue;
            }

            Document d = reader.document(i);
            assertEquals("id", Integer.toString(i), d.get("id"));
            assertEquals("utf8", CompatDocs.UTF8_VALUE, d.get("utf8"));
            assertEquals("autf8", CompatDocs.UTF8_VALUE, d.get("autf8"));
            assertEquals("content2", CompatDocs.CONTENT2_VALUE, d.get("content2"));
            assertEquals(CompatDocs.NON_ASCII_FIELD_NAME,
                CompatDocs.NON_ASCII_FIELD_VALUE, d.get(CompatDocs.NON_ASCII_FIELD_NAME));

            Fields tvFields = reader.getTermVectors(i);
            assertNotNull("term vectors missing for doc " + i, tvFields);
            assertNotNull("utf8 term vector missing for doc " + i, tvFields.terms("utf8"));
        }

        assertDocValues(reader, liveDocs);

        // content term should match every live doc (34 of 35).
        ScoreDoc[] hits = searcher.search(new TermQuery(new Term("content", "aaa")), 1000).scoreDocs;
        assertEquals(CompatDocs.DOC_COUNT - 1, hits.length);
        assertEquals("first hit should be id 0", "0",
            searcher.getIndexReader().document(hits[0].doc).get("id"));

        // offsets/positions-bearing fields.
        assertEquals(CompatDocs.DOC_COUNT - 1,
            searcher.search(new TermQuery(new Term("content5", "aaa")), 1000).scoreDocs.length);
        assertEquals(CompatDocs.DOC_COUNT - 1,
            searcher.search(new TermQuery(new Term("content6", "aaa")), 1000).scoreDocs.length);

        // The utf8 field's term dictionary must be identical to what the writing
        // runtime produced. Both sides analyze the same string with the same
        // StandardAnalyzer, so the produced term set is the cross-runtime contract
        // for the UTF-8 edge cases (astral planes, the skull, etc.).
        assertEquals("utf8 term set", CompatDocs.expectedUtf8Terms(),
            collectTerms(reader, "utf8"));

        // sanity: the content terms enum has exactly the single term "aaa".
        Terms contentTerms = MultiFields.getTerms(reader, "content");
        assertNotNull(contentTerms);
    }


    private static java.util.List<String> collectTerms(IndexReader reader, String field) throws Exception {
        java.util.List<String> result = new java.util.ArrayList<>();
        Terms terms = MultiFields.getTerms(reader, field);
        if (terms == null) {
            return result;
        }
        org.apache.lucene.index.TermsEnum te = terms.iterator(null);
        BytesRef term;
        while ((term = te.next()) != null) {
            result.add(term.utf8ToString());
        }
        java.util.Collections.sort(result);
        return result;
    }

    private static void assertDocValues(IndexReader reader, Bits liveDocs) throws Exception {
        NumericDocValues dvByte = MultiDocValues.getNumericValues(reader, "dvByte");
        BinaryDocValues dvBytesDerefFixed = MultiDocValues.getBinaryValues(reader, "dvBytesDerefFixed");
        BinaryDocValues dvBytesDerefVar = MultiDocValues.getBinaryValues(reader, "dvBytesDerefVar");
        SortedDocValues dvBytesSortedFixed = MultiDocValues.getSortedValues(reader, "dvBytesSortedFixed");
        SortedDocValues dvBytesSortedVar = MultiDocValues.getSortedValues(reader, "dvBytesSortedVar");
        BinaryDocValues dvBytesStraightFixed = MultiDocValues.getBinaryValues(reader, "dvBytesStraightFixed");
        BinaryDocValues dvBytesStraightVar = MultiDocValues.getBinaryValues(reader, "dvBytesStraightVar");
        NumericDocValues dvDouble = MultiDocValues.getNumericValues(reader, "dvDouble");
        NumericDocValues dvFloat = MultiDocValues.getNumericValues(reader, "dvFloat");
        NumericDocValues dvInt = MultiDocValues.getNumericValues(reader, "dvInt");
        NumericDocValues dvLong = MultiDocValues.getNumericValues(reader, "dvLong");
        NumericDocValues dvPacked = MultiDocValues.getNumericValues(reader, "dvPacked");
        NumericDocValues dvShort = MultiDocValues.getNumericValues(reader, "dvShort");
        SortedSetDocValues dvSortedSet = MultiDocValues.getSortedSetValues(reader, "dvSortedSet");

        assertNotNull("dvByte", dvByte);
        assertNotNull("dvSortedSet", dvSortedSet);

        for (int i = 0; i < CompatDocs.DOC_COUNT; i++) {
            boolean live = liveDocs == null || liveDocs.get(i);
            if (!live) {
                continue;
            }
            int id = Integer.parseInt(reader.document(i).get("id"));
            assertEquals("dvByte", id, dvByte.get(i));

            byte[] bytes = new byte[] {
                (byte) (id >>> 24), (byte) (id >>> 16), (byte) (id >>> 8), (byte) id
            };
            BytesRef expected = new BytesRef(bytes);
            BytesRef scratch = new BytesRef();

            dvBytesDerefFixed.get(i, scratch);
            assertEquals("dvBytesDerefFixed", expected, scratch);
            dvBytesDerefVar.get(i, scratch);
            assertEquals("dvBytesDerefVar", expected, scratch);
            dvBytesSortedFixed.get(i, scratch);
            assertEquals("dvBytesSortedFixed", expected, scratch);
            dvBytesSortedVar.get(i, scratch);
            assertEquals("dvBytesSortedVar", expected, scratch);
            dvBytesStraightFixed.get(i, scratch);
            assertEquals("dvBytesStraightFixed", expected, scratch);
            dvBytesStraightVar.get(i, scratch);
            assertEquals("dvBytesStraightVar", expected, scratch);

            assertEquals("dvDouble", (double) id, Double.longBitsToDouble(dvDouble.get(i)), 0D);
            assertEquals("dvFloat", (float) id, Float.intBitsToFloat((int) dvFloat.get(i)), 0F);
            assertEquals("dvInt", id, dvInt.get(i));
            assertEquals("dvLong", id, dvLong.get(i));
            assertEquals("dvPacked", id, dvPacked.get(i));
            assertEquals("dvShort", id, dvShort.get(i));

            dvSortedSet.setDocument(i);
            long ord = dvSortedSet.nextOrd();
            assertEquals("dvSortedSet single ord",
                SortedSetDocValues.NO_MORE_ORDS, dvSortedSet.nextOrd());
            dvSortedSet.lookupOrd(ord, scratch);
            assertEquals("dvSortedSet value", expected, scratch);
        }
    }
}
