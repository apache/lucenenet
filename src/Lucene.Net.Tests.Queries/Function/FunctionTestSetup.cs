using System;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries.Function
{
    /// <summary>
    /// Setup for function tests
    /// </summary>
    public abstract class FunctionTestSetup : LuceneTestCase
    {

        /// <summary>
        /// Actual score computation order is slightly different than assumptios
        /// this allows for a small amount of variation
        /// </summary>
        protected internal static float TEST_SCORE_TOLERANCE_DELTA = 0.001f;

        protected internal const int N_DOCS = 17; // select a primary number > 2

        protected internal const string ID_FIELD = "id";
        protected internal const string TEXT_FIELD = "text";
        protected internal const string INT_FIELD = "iii";
        protected internal const string FLOAT_FIELD = "fff";

        protected internal ValueSource BYTE_VALUESOURCE = new ByteFieldSource(INT_FIELD);
        protected internal ValueSource SHORT_VALUESOURCE = new ShortFieldSource(INT_FIELD);
        protected internal ValueSource INT_VALUESOURCE = new IntFieldSource(INT_FIELD);
        protected internal ValueSource INT_AS_FLOAT_VALUESOURCE = new FloatFieldSource(INT_FIELD);
        protected internal ValueSource FLOAT_VALUESOURCE = new FloatFieldSource(FLOAT_FIELD);

        private static readonly string[] DOC_TEXT_LINES =
        {
            @"Well, this is just some plain text we use for creating the ",
            "test documents. It used to be a text from an online collection ",
            "devoted to first aid, but if there was there an (online) lawyers ",
            "first aid collection with legal advices, \"it\" might have quite ",
            "probably advised one not to include \"it\"'s text or the text of ",
            "any other online collection in one's code, unless one has money ",
            "that one don't need and one is happy to donate for lawyers ",
            "charity. Anyhow at some point, rechecking the usage of this text, ",
            "it became uncertain that this text is free to use, because ",
            "the web site in the disclaimer of he eBook containing that text ",
            "was not responding anymore, and at the same time, in projGut, ",
            "searching for first aid no longer found that eBook as well. ",
            "So here we are, with a perhaps much less interesting ",
            "text for the test, but oh much much safer. "
        };

        protected internal static Directory dir;
        protected internal static Analyzer anlzr;

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            dir.Dispose();
            dir = null;
            anlzr = null;
        }

        /// <summary>
        /// LUCENENET specific
        /// Non-static because NewIndexWriterConfig is now non-static
        /// </summary>
        protected internal void CreateIndex(bool doMultiSegment)
        {
            if (VERBOSE)
            {
                Console.WriteLine("TEST: setUp");
            }
            // prepare a small index with just a few documents.
            dir = NewDirectory();
            anlzr = new MockAnalyzer(Random());
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, anlzr).SetMergePolicy(NewLogMergePolicy());
            if (doMultiSegment)
            {
                iwc.SetMaxBufferedDocs(TestUtil.NextInt(Random(), 2, 7));
            }
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
            // add docs not exactly in natural ID order, to verify we do check the order of docs by scores
            int remaining = N_DOCS;
            bool[] done = new bool[N_DOCS];
            int i = 0;
            while (remaining > 0)
            {
                if (done[i])
                {
                    throw new Exception("to set this test correctly N_DOCS=" + N_DOCS + " must be primary and greater than 2!");
                }
                AddDoc(iw, i);
                done[i] = true;
                i = (i + 4) % N_DOCS;
                remaining--;
            }
            if (!doMultiSegment)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: setUp full merge");
                }
                iw.ForceMerge(1);
            }
            iw.Dispose();
            if (VERBOSE)
            {
                Console.WriteLine("TEST: setUp done close");
            }
        }

        /// <summary>
        /// LUCENENET specific
        /// Non-static because NewField is now non-static
        /// </summary>
        private void AddDoc(RandomIndexWriter iw, int i)
        {
            Document d = new Document();
            Field f;
            int scoreAndID = i + 1;

            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.IsTokenized = false;
            customType.OmitNorms = true;

            f = NewField(ID_FIELD, Id2String(scoreAndID), customType); // for debug purposes
            d.Add(f);

            FieldType customType2 = new FieldType(TextField.TYPE_NOT_STORED);
            customType2.OmitNorms = true;
            f = NewField(TEXT_FIELD, "text of doc" + scoreAndID + TextLine(i), customType2); // for regular search
            d.Add(f);

            f = NewField(INT_FIELD, "" + scoreAndID, customType); // for function scoring
            d.Add(f);

            f = NewField(FLOAT_FIELD, scoreAndID + ".000", customType); // for function scoring
            d.Add(f);

            iw.AddDocument(d);
            Log("added: " + d);
        }

        // 17 --> ID00017
        protected internal static string Id2String(int scoreAndID)
        {
            string s = "000000000" + scoreAndID;
            int n = ("" + N_DOCS).Length + 3;
            int k = s.Length - n;
            return "ID" + s.Substring(k);
        }

        // some text line for regular search
        private static string TextLine(int docNum)
        {
            return DOC_TEXT_LINES[docNum % DOC_TEXT_LINES.Length];
        }

        // extract expected doc score from its ID Field: "ID7" --> 7.0
        protected internal static float ExpectedFieldScore(string docIDFieldVal)
        {
            return Convert.ToSingle(docIDFieldVal.Substring(2));
        }

        // debug messages (change DBG to true for anything to print)
        protected internal static void Log(object o)
        {
            if (VERBOSE)
            {
                Console.WriteLine(o.ToString());
            }
        }
    }
}