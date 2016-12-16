using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Documents
{
    [TestFixture]
    public class TestLazyDocument : LuceneTestCase
    {
        public readonly int NUM_DOCS = AtLeast(10);
        public readonly string[] FIELDS = new string[]
            { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k" };
        public readonly int NUM_VALUES = AtLeast(100);

        public Directory dir = NewDirectory();

        [OneTimeTearDown]
        public void RemoveIndex()
        {
            if (null != dir)
            {
                try
                {
                    dir.Dispose();
                    dir = null;
                }
                catch (Exception /*e*/) { /* NOOP */ }
            }
        }

        [OneTimeSetUp]
        public void CreateIndex()
        {

            Analyzer analyzer = new MockAnalyzer(Random());
            IndexWriter writer = new IndexWriter
              (dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            try
            {
                for (int docid = 0; docid < NUM_DOCS; docid++)
                {
                    Document d = new Document();
                    d.Add(NewStringField("docid", "" + docid, Field.Store.YES));
                    d.Add(NewStringField("never_load", "fail", Field.Store.YES));
                    foreach (string f in FIELDS)
                    {
                        for (int val = 0; val < NUM_VALUES; val++)
                        {
                            d.Add(NewStringField(f, docid + "_" + f + "_" + val, Field.Store.YES));
                        }
                    }
                    d.Add(NewStringField("load_later", "yes", Field.Store.YES));
                    writer.AddDocument(d);
                }
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Test]
        public void TestLazy()
        {
            int id = Random().nextInt(NUM_DOCS);
            IndexReader reader = DirectoryReader.Open(dir);
            try
            {
                Query q = new TermQuery(new Term("docid", "" + id));
                IndexSearcher searcher = NewSearcher(reader);
                ScoreDoc[] hits = searcher.Search(q, 100).ScoreDocs;
                assertEquals("Too many docs", 1, hits.Length);
                LazyTestingStoredFieldVisitor visitor
                    = new LazyTestingStoredFieldVisitor(new LazyDocument(reader, hits[0].Doc),
                                                      FIELDS);
                reader.Document(hits[0].Doc, visitor);
                Document d = visitor.doc;

                int numFieldValues = 0;
                IDictionary<string, int> fieldValueCounts = new HashMap<string, int>();

                // at this point, all FIELDS should be Lazy and unrealized
                foreach (IndexableField f in d)
                {
                    numFieldValues++;
                    if (f.Name.equals("never_load"))
                    {
                        fail("never_load was loaded");
                    }
                    if (f.Name.equals("load_later"))
                    {
                        fail("load_later was loaded on first pass");
                    }
                    if (f.Name.equals("docid"))
                    {
                        assertFalse(f.Name, f is LazyDocument.LazyField);
                    }
                    else
                    {
                        int count = fieldValueCounts.ContainsKey(f.Name) ?
                          fieldValueCounts[f.Name] : 0;
                        count++;
                        fieldValueCounts.Put(f.Name, count);
                        assertTrue(f.Name + " is " + f.GetType(),
                                   f is LazyDocument.LazyField);
                        LazyDocument.LazyField lf = (LazyDocument.LazyField)f;
                        assertFalse(f.Name + " is loaded", lf.HasBeenLoaded);
                    }
                }
                Console.WriteLine("numFieldValues == " + numFieldValues);
                assertEquals("numFieldValues", 1 + (NUM_VALUES * FIELDS.Length), // LUCENENET TODO: Failing here 1 too small, but what field is the + 1 here supposed to represent?
                             numFieldValues);

                foreach (string field in fieldValueCounts.Keys)
                {
                    assertEquals("fieldName count: " + field,
                                 NUM_VALUES, fieldValueCounts[field]);
                }

                // pick a single field name to load a single value
                string fieldName = FIELDS[Random().nextInt(FIELDS.Length)];
                IndexableField[] fieldValues = d.GetFields(fieldName);
                assertEquals("#vals in field: " + fieldName,
                             NUM_VALUES, fieldValues.Length);
                int valNum = Random().nextInt(fieldValues.Length);
                assertEquals(id + "_" + fieldName + "_" + valNum,
                             fieldValues[valNum].StringValue);

                // now every value of fieldName should be loaded
                foreach (IndexableField f in d)
                {
                    if (f.Name.equals("never_load"))
                    {
                        fail("never_load was loaded");
                    }
                    if (f.Name.equals("load_later"))
                    {
                        fail("load_later was loaded too soon");
                    }
                    if (f.Name.equals("docid"))
                    {
                        assertFalse(f.Name, f is LazyDocument.LazyField);
                    }
                    else
                    {
                        assertTrue(f.Name + " is " + f.GetType(),
                                   f is LazyDocument.LazyField);
                        LazyDocument.LazyField lf = (LazyDocument.LazyField)f;
                        assertEquals(f.Name + " is loaded?",
                                     lf.Name.equals(fieldName), lf.HasBeenLoaded);
                    }
                }

                // use the same LazyDoc to ask for one more lazy field
                visitor = new LazyTestingStoredFieldVisitor(new LazyDocument(reader, hits[0].Doc),
                                                            "load_later");
                reader.Document(hits[0].Doc, visitor);
                d = visitor.doc;

                // ensure we have all the values we expect now, and that
                // adding one more lazy field didn't "unload" the existing LazyField's
                // we already loaded.
                foreach (IndexableField f in d)
                {
                    if (f.Name.equals("never_load"))
                    {
                        fail("never_load was loaded");
                    }
                    if (f.Name.equals("docid"))
                    {
                        assertFalse(f.Name, f is LazyDocument.LazyField);
                    }
                    else
                    {
                        assertTrue(f.Name + " is " + f.GetType(),
                                   f is LazyDocument.LazyField);
                        LazyDocument.LazyField lf = (LazyDocument.LazyField)f;
                        assertEquals(f.Name + " is loaded?",
                                     lf.Name.equals(fieldName), lf.HasBeenLoaded);
                    }
                }

                // even the underlying doc shouldn't have never_load
                assertNull("never_load was loaded in wrapped doc",
                           visitor.lazyDoc.Document.GetField("never_load"));

            }
            finally
            {
                reader.Dispose();
            }
        }

        internal class LazyTestingStoredFieldVisitor : StoredFieldVisitor
        {
            public readonly Document doc = new Document();
            public readonly LazyDocument lazyDoc;
            public readonly ISet<string> lazyFieldNames;

            internal LazyTestingStoredFieldVisitor(LazyDocument l, params string[] fields)
            {
                lazyDoc = l;
                lazyFieldNames = new HashSet<string>(Arrays.AsList(fields));
            }


            public override Status NeedsField(FieldInfo fieldInfo)
            {
                if (fieldInfo.Name.equals("docid"))
                {
                    return Status.YES;
                }
                else if (fieldInfo.Name.equals("never_load"))
                {
                    return Status.NO;
                }
                else
                {
                    if (lazyFieldNames.contains(fieldInfo.Name))
                    {
                        doc.Add(lazyDoc.GetField(fieldInfo));
                    }
                }
                return Status.NO;
            }

            public override void StringField(FieldInfo fieldInfo, string value)
            {
                FieldType ft = new FieldType(TextField.TYPE_STORED);
                ft.StoreTermVectors = fieldInfo.HasVectors();
                ft.Indexed = fieldInfo.Indexed;
                ft.OmitNorms = fieldInfo.OmitsNorms();
                ft.IndexOptions = fieldInfo.FieldIndexOptions;
                doc.Add(new Field(fieldInfo.Name, value, ft));
            }
        }
    }
}
