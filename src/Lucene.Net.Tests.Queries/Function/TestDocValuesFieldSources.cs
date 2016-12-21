using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using NUnit.Framework;
using System;
using System.Globalization;

namespace Lucene.Net.Tests.Queries.Function
{
    // [Util.LuceneTestCase.SuppressCodecs("Lucene3x")]
    public class TestDocValuesFieldSources : LuceneTestCase
    {
        private void DoTest(DocValuesType type)
        {
            Directory d = NewDirectory();
            IndexWriterConfig iwConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            int nDocs = AtLeast(50);
            Field id = new NumericDocValuesField("id", 0);
            Field f;
            switch (type)
            {
                case DocValuesType.BINARY:
                    f = new BinaryDocValuesField("dv", new BytesRef());
                    break;
                case DocValuesType.SORTED:
                    f = new SortedDocValuesField("dv", new BytesRef());
                    break;
                case DocValuesType.NUMERIC:
                    f = new NumericDocValuesField("dv", 0);
                    break;
                default:
                    throw new InvalidOperationException();
            }
            Document document = new Document();
            document.Add(id);
            document.Add(f);

            object[] vals = new object[nDocs];

            RandomIndexWriter iw = new RandomIndexWriter(Random(), d, iwConfig);
            for (int i = 0; i < nDocs; ++i)
            {
                id.SetInt64Value(i);
                switch (type)
                {
                    case DocValuesType.SORTED:
                    case DocValuesType.BINARY:
                        do
                        {
                            vals[i] = TestUtil.RandomSimpleString(Random(), 20);
                        } while (((string)vals[i]).Length == 0);
                        f.SetBytesValue(new BytesRef((string)vals[i]));
                        break;
                    case DocValuesType.NUMERIC:
                        int bitsPerValue = Random().NextIntBetween(1, 31); // keep it an int
                        vals[i] = (long)Random().Next((int)PackedInts.MaxValue(bitsPerValue));
                        f.SetInt64Value((long) vals[i]);
                        break;
                }
                iw.AddDocument(document);
                if (Random().NextBoolean() && i % 10 == 9)
                {
                    iw.Commit();
                }
            }
            iw.Dispose();

            DirectoryReader rd = DirectoryReader.Open(d);
            foreach (AtomicReaderContext leave in rd.Leaves)
            {
                FunctionValues ids = (new LongFieldSource("id")).GetValues(null, leave);
                ValueSource vs;
                switch (type)
                {
                    case DocValuesType.BINARY:
                    case DocValuesType.SORTED:
                        vs = new BytesRefFieldSource("dv");
                        break;
                    case DocValuesType.NUMERIC:
                        vs = new LongFieldSource("dv");
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                FunctionValues values = vs.GetValues(null, leave);
                BytesRef bytes = new BytesRef();
                for (int i = 0; i < leave.AtomicReader.MaxDoc; ++i)
                {
                    assertTrue(values.Exists(i));
                    if (vs is BytesRefFieldSource)
                    {
                        assertTrue(values.ObjectVal(i) is string);
                    }
                    else if (vs is LongFieldSource)
                    {
                        assertTrue(values.ObjectVal(i) is long?);
                        assertTrue(values.BytesVal(i, bytes));
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    object expected = vals[ids.IntVal(i)];
                    switch (type)
                    {
                        case DocValuesType.SORTED:
                            values.OrdVal(i); // no exception
                            assertTrue(values.NumOrd() >= 1);
                            goto case DocValuesType.BINARY;
                        case DocValuesType.BINARY:
                            assertEquals(expected, values.ObjectVal(i));
                            assertEquals(expected, values.StrVal(i));
                            assertEquals(expected, values.ObjectVal(i));
                            assertEquals(expected, values.StrVal(i));
                            assertTrue(values.BytesVal(i, bytes));
                            assertEquals(new BytesRef((string)expected), bytes);
                            break;
                        case DocValuesType.NUMERIC:
                            assertEquals(Convert.ToInt64(expected, CultureInfo.InvariantCulture), values.LongVal(i));
                            break;
                    }
                }
            }
            rd.Dispose();
            d.Dispose();
        }
        
        [Test]
        public void Test()
        {
            var values = Enum.GetValues(typeof(DocValuesType));
            foreach (DocValuesType type in values)
            {
                if (type != DocValuesType.SORTED_SET)
                {
                    DoTest(type);
                }
            }
        }

    }
}