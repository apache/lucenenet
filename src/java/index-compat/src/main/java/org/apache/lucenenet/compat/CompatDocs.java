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

import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.standard.StandardAnalyzer;
import org.apache.lucene.document.BinaryDocValuesField;
import org.apache.lucene.document.Document;
import org.apache.lucene.document.DoubleDocValuesField;
import org.apache.lucene.document.Field;
import org.apache.lucene.document.FieldType;
import org.apache.lucene.document.FloatDocValuesField;
import org.apache.lucene.document.IntField;
import org.apache.lucene.document.LongField;
import org.apache.lucene.document.NumericDocValuesField;
import org.apache.lucene.document.SortedDocValuesField;
import org.apache.lucene.document.SortedSetDocValuesField;
import org.apache.lucene.document.StringField;
import org.apache.lucene.document.TextField;
import org.apache.lucene.index.CheckIndex;
import org.apache.lucene.index.FieldInfo.IndexOptions;
import org.apache.lucene.index.IndexWriter;
import org.apache.lucene.index.IndexWriterConfig;
import org.apache.lucene.index.LogByteSizeMergePolicy;
import org.apache.lucene.store.Directory;
import org.apache.lucene.util.BytesRef;
import org.apache.lucene.util.Version;

import java.io.PrintStream;

/**
 * The single source of truth for the deterministic document set shared between
 * Java (Apache Lucene 4.8.1) and Lucene.NET. This intentionally mirrors, field
 * for field, the document schema produced by
 * {@code TestBackwardsCompatibility.AddDoc} / {@code AddNoProxDoc} and the read
 * back verification in {@code TestBackwardsCompatibility.SearchIndex} on the
 * .NET side, so that an index written by either runtime can be validated by the
 * other. See issue #270.
 *
 * <p>Nothing here may use randomness: both runtimes must produce semantically
 * identical indexes from the same inputs.
 */
public final class CompatDocs {

    private CompatDocs() {
    }

    /** Number of "normal" (prox) documents indexed by {@link #writeIndex}. */
    public static final int DOC_COUNT = 35;

    /** The id that is deleted, matching the .NET harness. */
    public static final int DELETED_ID = 7;

    // Exactly matches the .NET literal: Lu + U+1D11E + ce + U+1D160 + ne + NUL + skull + astral + cd.
    public static final String UTF8_VALUE = "Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\uD917\uDC17cd";
    public static final String CONTENT2_VALUE = "here is more content with aaa aaa aaa";
    public static final String NON_ASCII_FIELD_NAME = "fie\u2C77ld";
    public static final String NON_ASCII_FIELD_VALUE = "field with non-ascii name";

    /**
     * Writes the deterministic compatibility index into {@code dir}. The result
     * has 35 documents, with id 7 deleted, term vectors, offsets, norms, and the
     * full DocValues matrix, in either compound-file or non-compound-file form.
     */
    public static void writeIndex(Directory dir, boolean useCompoundFile) throws Exception {
        Analyzer analyzer = new StandardAnalyzer(Version.LUCENE_48);

        LogByteSizeMergePolicy mp = new LogByteSizeMergePolicy();
        mp.setNoCFSRatio(useCompoundFile ? 1.0 : 0.0);
        mp.setMaxCFSSegmentSizeMB(Double.POSITIVE_INFINITY);

        IndexWriterConfig conf = new IndexWriterConfig(Version.LUCENE_48, analyzer)
            .setUseCompoundFile(useCompoundFile)
            .setMaxBufferedDocs(10)
            .setMergePolicy(mp);
        IndexWriter writer = new IndexWriter(dir, conf);
        for (int i = 0; i < DOC_COUNT; i++) {
            addDoc(writer, i);
        }
        writer.close();

        // Delete id 7 in a fresh writer so the layout matches the .NET harness.
        conf = new IndexWriterConfig(Version.LUCENE_48, analyzer)
            .setUseCompoundFile(useCompoundFile)
            .setMaxBufferedDocs(10)
            .setOpenMode(IndexWriterConfig.OpenMode.APPEND);
        writer = new IndexWriter(dir, conf);
        writer.deleteDocuments(new org.apache.lucene.index.Term("id", Integer.toString(DELETED_ID)));
        writer.close();
    }

    private static void addDoc(IndexWriter writer, int id) throws Exception {
        Document doc = new Document();
        doc.add(new TextField("content", "aaa", Field.Store.NO));
        doc.add(new StringField("id", Integer.toString(id), Field.Store.YES));

        FieldType customType2 = new FieldType(TextField.TYPE_STORED);
        customType2.setStoreTermVectors(true);
        customType2.setStoreTermVectorPositions(true);
        customType2.setStoreTermVectorOffsets(true);
        doc.add(new Field("autf8", UTF8_VALUE, customType2));
        doc.add(new Field("utf8", UTF8_VALUE, customType2));
        doc.add(new Field("content2", CONTENT2_VALUE, customType2));
        doc.add(new Field(NON_ASCII_FIELD_NAME, NON_ASCII_FIELD_VALUE, customType2));

        // numeric fields
        doc.add(new IntField("trieInt", id, Field.Store.NO));
        doc.add(new LongField("trieLong", (long) id, Field.Store.NO));

        // docvalues fields
        doc.add(new NumericDocValuesField("dvByte", (byte) id));
        byte[] bytes = new byte[] {
            (byte) (id >>> 24), (byte) (id >>> 16), (byte) (id >>> 8), (byte) id
        };
        BytesRef ref = new BytesRef(bytes);
        doc.add(new BinaryDocValuesField("dvBytesDerefFixed", ref));
        doc.add(new BinaryDocValuesField("dvBytesDerefVar", ref));
        doc.add(new SortedDocValuesField("dvBytesSortedFixed", ref));
        doc.add(new SortedDocValuesField("dvBytesSortedVar", ref));
        doc.add(new BinaryDocValuesField("dvBytesStraightFixed", ref));
        doc.add(new BinaryDocValuesField("dvBytesStraightVar", ref));
        doc.add(new DoubleDocValuesField("dvDouble", (double) id));
        doc.add(new FloatDocValuesField("dvFloat", (float) id));
        doc.add(new NumericDocValuesField("dvInt", id));
        doc.add(new NumericDocValuesField("dvLong", id));
        doc.add(new NumericDocValuesField("dvPacked", id));
        doc.add(new NumericDocValuesField("dvShort", (short) id));
        doc.add(new SortedSetDocValuesField("dvSortedSet", ref));

        // a field with both offsets and term vectors for a cross-check
        FieldType customType3 = new FieldType(TextField.TYPE_STORED);
        customType3.setStoreTermVectors(true);
        customType3.setStoreTermVectorPositions(true);
        customType3.setStoreTermVectorOffsets(true);
        customType3.setIndexOptions(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
        doc.add(new Field("content5", CONTENT2_VALUE, customType3));

        // a field that omits only positions
        FieldType customType4 = new FieldType(TextField.TYPE_STORED);
        customType4.setStoreTermVectors(true);
        customType4.setStoreTermVectorPositions(false);
        customType4.setStoreTermVectorOffsets(true);
        customType4.setIndexOptions(IndexOptions.DOCS_AND_FREQS);
        doc.add(new Field("content6", CONTENT2_VALUE, customType4));

        writer.addDocument(doc);
    }

    /**
     * Runs the Lucene {@link CheckIndex} tool against {@code dir} and throws if
     * the index reports any problem. This is the codec integrity gate; it
     * validates the stored per file checksums written by the codec.
     */
    public static void checkIndex(Directory dir, PrintStream infoStream) throws Exception {
        CheckIndex checker = new CheckIndex(dir);
        checker.setCrossCheckTermVectors(true);
        if (infoStream != null) {
            checker.setInfoStream(infoStream);
        }
        CheckIndex.Status status = checker.checkIndex();
        if (!status.clean) {
            throw new IllegalStateException("CheckIndex reported the index at " + dir + " is not clean");
        }
    }

    /**
     * The sorted, unique set of terms that {@code StandardAnalyzer(LUCENE_48)}
     * produces for {@link #UTF8_VALUE}. An index written by either runtime must
     * contain exactly this term set in the {@code utf8} field, which is the
     * cross-runtime contract for the UTF-8 edge cases. Computed by re-running the
     * analyzer so it stays correct if the constant changes.
     */
    public static java.util.List<String> expectedUtf8Terms() throws Exception {
        java.util.TreeSet<String> terms = new java.util.TreeSet<>();
        Analyzer analyzer = new StandardAnalyzer(Version.LUCENE_48);
        org.apache.lucene.analysis.TokenStream ts = analyzer.tokenStream("utf8",
            new java.io.StringReader(UTF8_VALUE));
        org.apache.lucene.analysis.tokenattributes.CharTermAttribute termAttr =
            ts.addAttribute(org.apache.lucene.analysis.tokenattributes.CharTermAttribute.class);
        ts.reset();
        while (ts.incrementToken()) {
            terms.add(termAttr.toString());
        }
        ts.end();
        ts.close();
        return new java.util.ArrayList<>(terms);
    }
}
