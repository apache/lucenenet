using Lucene.Net.Documents;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Index
{
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using StoredField = StoredField;
    using StringField = StringField;
    using TextField = TextField;

    public class DocHelper
    {
        public static readonly FieldType CustomType;
        public const string FIELD_1_TEXT = "field one text";
        public const string TEXT_FIELD_1_KEY = "textField1";
        public static Field TextField1;

        public static readonly FieldType CustomType2;
        public const string FIELD_2_TEXT = "field field field two text";

        //Fields will be lexicographically sorted.  So, the order is: field, text, two
        public static readonly int[] FIELD_2_FREQS = new int[] { 3, 1, 1 };

        public const string TEXT_FIELD_2_KEY = "textField2";
        public static Field TextField2;

        public static readonly FieldType CustomType3;
        public const string FIELD_3_TEXT = "aaaNoNorms aaaNoNorms bbbNoNorms";
        public const string TEXT_FIELD_3_KEY = "textField3";
        public static Field TextField3;

        public const string KEYWORD_TEXT = "Keyword";
        public const string KEYWORD_FIELD_KEY = "keyField";
        public static Field KeyField;

        public static readonly FieldType CustomType5;
        public const string NO_NORMS_TEXT = "omitNormsText";
        public const string NO_NORMS_KEY = "omitNorms";
        public static Field NoNormsField;

        public static readonly FieldType CustomType6;
        public const string NO_TF_TEXT = "analyzed with no tf and positions";
        public const string NO_TF_KEY = "omitTermFreqAndPositions";
        public static Field NoTFField;

        public static readonly FieldType CustomType7;
        public const string UNINDEXED_FIELD_TEXT = "unindexed field text";
        public const string UNINDEXED_FIELD_KEY = "unIndField";
        public static Field UnIndField;

        public const string UNSTORED_1_FIELD_TEXT = "unstored field text";
        public const string UNSTORED_FIELD_1_KEY = "unStoredField1";
        public static Field UnStoredField1;// = new TextField(UNSTORED_FIELD_1_KEY, UNSTORED_1_FIELD_TEXT, Field.Store.NO);

        public static readonly FieldType CustomType8;
        public const string UNSTORED_2_FIELD_TEXT = "unstored field text";
        public const string UNSTORED_FIELD_2_KEY = "unStoredField2";
        public static Field UnStoredField2;

        public const string LAZY_FIELD_BINARY_KEY = "lazyFieldBinary";
        public static byte[] LAZY_FIELD_BINARY_BYTES;
        public static Field LazyFieldBinary;

        public const string LAZY_FIELD_KEY = "lazyField";
        public const string LAZY_FIELD_TEXT = "These are some field bytes";
        public static Field LazyField;// = new Field(LAZY_FIELD_KEY, LAZY_FIELD_TEXT, CustomType);

        public const string LARGE_LAZY_FIELD_KEY = "largeLazyField";
        public static string LARGE_LAZY_FIELD_TEXT;
        public static Field LargeLazyField;

        //From Issue 509
        public const string FIELD_UTF1_TEXT = "field one \u4e00text";

        public const string TEXT_FIELD_UTF1_KEY = "textField1Utf8";
        public static Field TextUtfField1;// = new Field(TEXT_FIELD_UTF1_KEY, FIELD_UTF1_TEXT, CustomType);

        public const string FIELD_UTF2_TEXT = "field field field \u4e00two text";

        //Fields will be lexicographically sorted.  So, the order is: field, text, two
        public static readonly int[] FIELD_UTF2_FREQS = new int[] { 3, 1, 1 };

        public const string TEXT_FIELD_UTF2_KEY = "textField2Utf8";
        public static Field TextUtfField2;// = new Field(TEXT_FIELD_UTF2_KEY, FIELD_UTF2_TEXT, CustomType2);

        public static IDictionary<string, object> NameValues = null;

        // ordered list of all the fields...
        // could use LinkedHashMap for this purpose if Java1.4 is OK
        public static Field[] Fields = null;

        public static IDictionary<string, IIndexableField> All = new Dictionary<string, IIndexableField>();
        public static IDictionary<string, IIndexableField> Indexed = new Dictionary<string, IIndexableField>();
        public static IDictionary<string, IIndexableField> Stored = new Dictionary<string, IIndexableField>();
        public static IDictionary<string, IIndexableField> Unstored = new Dictionary<string, IIndexableField>();
        public static IDictionary<string, IIndexableField> Unindexed = new Dictionary<string, IIndexableField>();
        public static IDictionary<string, IIndexableField> Termvector = new Dictionary<string, IIndexableField>();
        public static IDictionary<string, IIndexableField> Notermvector = new Dictionary<string, IIndexableField>();
        public static IDictionary<string, IIndexableField> Lazy = new Dictionary<string, IIndexableField>();
        public static IDictionary<string, IIndexableField> NoNorms = new Dictionary<string, IIndexableField>();
        public static IDictionary<string, IIndexableField> NoTf = new Dictionary<string, IIndexableField>();

        private static void Add(IDictionary<string, IIndexableField> map, IIndexableField field)
        {
            map[field.Name] = field;
        }

        /// <summary>
        /// Adds the fields above to a document </summary>
        /// <param name="doc"> The document to write </param>
        public static void SetupDoc(Document doc)
        {
            for (int i = 0; i < Fields.Length; i++)
            {
                doc.Add(Fields[i]);
            }
        }

        /// <summary>
        /// Writes the document to the directory using a segment
        /// named "test"; returns the SegmentInfo describing the new
        /// segment
        /// </summary>
        public static SegmentCommitInfo WriteDoc(Random random, Directory dir, Document doc)
        {
            return WriteDoc(random, dir, new MockAnalyzer(random, MockTokenizer.WHITESPACE, false), null, doc);
        }

        /// <summary>
        /// Writes the document to the directory using the analyzer
        /// and the similarity score; returns the SegmentInfo
        /// describing the new segment
        /// </summary>
        public static SegmentCommitInfo WriteDoc(Random random, Directory dir, Analyzer analyzer, Similarity similarity, Document doc)
        {
            IndexWriter writer = new IndexWriter(dir, (new IndexWriterConfig(Util.LuceneTestCase.TEST_VERSION_CURRENT, analyzer)).SetSimilarity(similarity ?? IndexSearcher.DefaultSimilarity)); // LuceneTestCase.newIndexWriterConfig(random,
            //writer.SetNoCFSRatio(0.0);
            writer.AddDocument(doc);
            writer.Commit();
            SegmentCommitInfo info = writer.NewestSegment();
            writer.Dispose();
            return info;
        }

        public static int NumFields(Document doc)
        {
            return doc.Fields.Count;
        }

        public static Document CreateDocument(int n, string indexName, int numFields)
        {
            StringBuilder sb = new StringBuilder();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;

            FieldType customType1 = new FieldType(StringField.TYPE_STORED);
            customType1.StoreTermVectors = true;
            customType1.StoreTermVectorPositions = true;
            customType1.StoreTermVectorOffsets = true;

            Document doc = new Document();
            doc.Add(new Field("id", Convert.ToString(n), customType1));
            doc.Add(new Field("indexname", indexName, customType1));
            sb.Append("a");
            sb.Append(n);
            doc.Add(new Field("field1", sb.ToString(), customType));
            sb.Append(" b");
            sb.Append(n);
            for (int i = 1; i < numFields; i++)
            {
                doc.Add(new Field("field" + (i + 1), sb.ToString(), customType));
            }
            return doc;
        }

        static DocHelper()
        {
            CustomType = new FieldType(TextField.TYPE_STORED);
            TextField1 = new Field(TEXT_FIELD_1_KEY, FIELD_1_TEXT, CustomType);
            CustomType2 = new FieldType(TextField.TYPE_STORED);
            CustomType2.StoreTermVectors = true;
            CustomType2.StoreTermVectorPositions = true;
            CustomType2.StoreTermVectorOffsets = true;
            TextField2 = new Field(TEXT_FIELD_2_KEY, FIELD_2_TEXT, CustomType2);
            CustomType3 = new FieldType(TextField.TYPE_STORED);
            CustomType3.OmitNorms = true;
            TextField3 = new Field(TEXT_FIELD_3_KEY, FIELD_3_TEXT, CustomType3);
            KeyField = new StringField(KEYWORD_FIELD_KEY, KEYWORD_TEXT, Field.Store.YES);
            CustomType5 = new FieldType(TextField.TYPE_STORED);
            CustomType5.OmitNorms = true;
            CustomType5.IsTokenized = false;
            NoNormsField = new Field(NO_NORMS_KEY, NO_NORMS_TEXT, CustomType5);
            CustomType6 = new FieldType(TextField.TYPE_STORED);
            CustomType6.IndexOptions = IndexOptions.DOCS_ONLY;
            NoTFField = new Field(NO_TF_KEY, NO_TF_TEXT, CustomType6);
            CustomType7 = new FieldType();
            CustomType7.IsStored = true;
            UnIndField = new Field(UNINDEXED_FIELD_KEY, UNINDEXED_FIELD_TEXT, CustomType7);
            CustomType8 = new FieldType(TextField.TYPE_NOT_STORED);
            CustomType8.StoreTermVectors = true;
            UnStoredField2 = new Field(UNSTORED_FIELD_2_KEY, UNSTORED_2_FIELD_TEXT, CustomType8);

            UnStoredField1 = new TextField(UNSTORED_FIELD_1_KEY, UNSTORED_1_FIELD_TEXT, Field.Store.NO);
            LazyField = new Field(LAZY_FIELD_KEY, LAZY_FIELD_TEXT, CustomType);
            TextUtfField1 = new Field(TEXT_FIELD_UTF1_KEY, FIELD_UTF1_TEXT, CustomType);
            TextUtfField2 = new Field(TEXT_FIELD_UTF2_KEY, FIELD_UTF2_TEXT, CustomType2);

            Fields = new Field[] { TextField1, TextField2, TextField3, KeyField, NoNormsField, NoTFField, UnIndField, UnStoredField1, UnStoredField2, TextUtfField1, TextUtfField2, LazyField, LazyFieldBinary, LargeLazyField };

            //Initialize the large Lazy Field
            StringBuilder buffer = new StringBuilder();
            for (int i = 0; i < 10000; i++)
            {
                buffer.Append("Lazily loading lengths of language in lieu of laughing ");
            }

            try
            {
                LAZY_FIELD_BINARY_BYTES = "These are some binary field bytes".GetBytes(IOUtils.CHARSET_UTF_8);
            }
            catch (EncoderFallbackException e)
            {
            }
            LazyFieldBinary = new StoredField(LAZY_FIELD_BINARY_KEY, LAZY_FIELD_BINARY_BYTES);
            Fields[Fields.Length - 2] = LazyFieldBinary;
            LARGE_LAZY_FIELD_TEXT = buffer.ToString();
            LargeLazyField = new Field(LARGE_LAZY_FIELD_KEY, LARGE_LAZY_FIELD_TEXT, CustomType);
            Fields[Fields.Length - 1] = LargeLazyField;
            for (int i = 0; i < Fields.Length; i++)
            {
                IIndexableField f = Fields[i];
                Add(All, f);
                if (f.FieldType.IsIndexed)
                {
                    Add(Indexed, f);
                }
                else
                {
                    Add(Unindexed, f);
                }
                if (f.FieldType.StoreTermVectors)
                {
                    Add(Termvector, f);
                }
                if (f.FieldType.IsIndexed && !f.FieldType.StoreTermVectors)
                {
                    Add(Notermvector, f);
                }
                if (f.FieldType.IsStored)
                {
                    Add(Stored, f);
                }
                else
                {
                    Add(Unstored, f);
                }
                if (f.FieldType.IndexOptions == IndexOptions.DOCS_ONLY)
                {
                    Add(NoTf, f);
                }
                if (f.FieldType.OmitNorms)
                {
                    Add(NoNorms, f);
                }
                if (f.FieldType.IndexOptions == IndexOptions.DOCS_ONLY)
                {
                    Add(NoTf, f);
                }
                //if (f.isLazy()) add(lazy, f);
            }
            NameValues = new Dictionary<string, object>();
            NameValues[TEXT_FIELD_1_KEY] = FIELD_1_TEXT;
            NameValues[TEXT_FIELD_2_KEY] = FIELD_2_TEXT;
            NameValues[TEXT_FIELD_3_KEY] = FIELD_3_TEXT;
            NameValues[KEYWORD_FIELD_KEY] = KEYWORD_TEXT;
            NameValues[NO_NORMS_KEY] = NO_NORMS_TEXT;
            NameValues[NO_TF_KEY] = NO_TF_TEXT;
            NameValues[UNINDEXED_FIELD_KEY] = UNINDEXED_FIELD_TEXT;
            NameValues[UNSTORED_FIELD_1_KEY] = UNSTORED_1_FIELD_TEXT;
            NameValues[UNSTORED_FIELD_2_KEY] = UNSTORED_2_FIELD_TEXT;
            NameValues[LAZY_FIELD_KEY] = LAZY_FIELD_TEXT;
            NameValues[LAZY_FIELD_BINARY_KEY] = LAZY_FIELD_BINARY_BYTES;
            NameValues[LARGE_LAZY_FIELD_KEY] = LARGE_LAZY_FIELD_TEXT;
            NameValues[TEXT_FIELD_UTF1_KEY] = FIELD_UTF1_TEXT;
            NameValues[TEXT_FIELD_UTF2_KEY] = FIELD_UTF2_TEXT;
        }
    }
}