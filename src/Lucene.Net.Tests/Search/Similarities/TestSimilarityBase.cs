using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using NUnit.Framework;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search.Similarities
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using FieldType = FieldType;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TextField = TextField;

    /// <summary>
    /// Tests the <see cref="SimilarityBase"/>-based Similarities. Contains unit tests and
    /// integration tests for all Similarities and correctness tests for a select
    /// few.
    /// <para>this class maintains a list of
    /// <see cref="SimilarityBase"/> subclasses. Each test case performs its test on all
    /// items in the list. If a test case fails, the name of the Similarity that
    /// caused the failure is returned as part of the assertion error message.</para>
    /// <para>Unit testing is performed by constructing statistics manually and calling
    /// the <see cref="SimilarityBase.Score(BasicStats, float, float)"/> method of the
    /// Similarities. The statistics represent corner cases of corpus distributions.
    /// </para>
    /// <para>For the integration tests, a small (8-document) collection is indexed. The
    /// tests verify that for a specific query, all relevant documents are returned
    /// in the correct order. The collection consists of two poems of English poet
    /// <a href="http://en.wikipedia.org/wiki/William_blake">William Blake</a>.</para>
    /// <para>Note: the list of Similarities is maintained by hand. If a new Similarity
    /// is added to the <see cref="Lucene.Net.Search.Similarities"/> package, the
    /// list should be updated accordingly.</para>
    /// <para>
    /// In the correctness tests, the score is verified against the result of manual
    /// computation. Since it would be impossible to test all Similarities
    /// (e.g. all possible DFR combinations, all parameter values for LM), only
    /// the best performing setups in the original papers are verified.
    /// </para>
    /// </summary>
    [TestFixture]
    public class TestSimilarityBase : LuceneTestCase
    {
        private static string FIELD_BODY = "body";
        private static string FIELD_ID = "id";

        /// <summary>
        /// The tolerance range for float equality. </summary>
        private static float FLOAT_EPSILON = 1e-5f;

        /// <summary>
        /// The DFR basic models to test. </summary>
        internal static BasicModel[] BASIC_MODELS = new BasicModel[] { new BasicModelBE(), new BasicModelD(), new BasicModelG(), new BasicModelIF(), new BasicModelIn(), new BasicModelIne(), new BasicModelP() };

        /// <summary>
        /// The DFR aftereffects to test. </summary>
        internal static AfterEffect[] AFTER_EFFECTS = new AfterEffect[] { new AfterEffectB(), new AfterEffectL(), new AfterEffect.NoAfterEffect() };

        /// <summary>
        /// The DFR normalizations to test. </summary>
        internal static Normalization[] NORMALIZATIONS = new Normalization[] { new NormalizationH1(), new NormalizationH2(), new NormalizationH3(), new NormalizationZ(), new Normalization.NoNormalization() };

        /// <summary>
        /// The distributions for IB. </summary>
        internal static Distribution[] DISTRIBUTIONS = new Distribution[] { new DistributionLL(), new DistributionSPL() };

        /// <summary>
        /// Lambdas for IB. </summary>
        internal static Lambda[] LAMBDAS = new Lambda[] { new LambdaDF(), new LambdaTTF() };

        private IndexSearcher searcher;
        private Directory dir;
        private IndexReader reader;

        /// <summary>
        /// The list of similarities to test. </summary>
        private IList<SimilarityBase> sims;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            for (int i = 0; i < docs.Length; i++)
            {
                Document d = new Document();
                FieldType ft = new FieldType(TextField.TYPE_STORED);
                ft.IsIndexed = false;
                d.Add(NewField(FIELD_ID, Convert.ToString(i), ft));
                d.Add(NewTextField(FIELD_BODY, docs[i], Field.Store.YES));
                writer.AddDocument(d);
            }

            reader = writer.GetReader();
            searcher = NewSearcher(reader);
            writer.Dispose();

            sims = new JCG.List<SimilarityBase>();
            foreach (BasicModel basicModel in BASIC_MODELS)
            {
                foreach (AfterEffect afterEffect in AFTER_EFFECTS)
                {
                    foreach (Normalization normalization in NORMALIZATIONS)
                    {
                        sims.Add(new DFRSimilarity(basicModel, afterEffect, normalization));
                    }
                }
            }
            foreach (Distribution distribution in DISTRIBUTIONS)
            {
                foreach (Lambda lambda in LAMBDAS)
                {
                    foreach (Normalization normalization in NORMALIZATIONS)
                    {
                        sims.Add(new IBSimilarity(distribution, lambda, normalization));
                    }
                }
            }
            sims.Add(new LMDirichletSimilarity());
            sims.Add(new LMJelinekMercerSimilarity(0.1f));
            sims.Add(new LMJelinekMercerSimilarity(0.7f));
        }

        // ------------------------------- Unit tests --------------------------------

        /// <summary>
        /// The default number of documents in the unit tests. </summary>
        private static int NUMBER_OF_DOCUMENTS = 100;

        /// <summary>
        /// The default total number of tokens in the field in the unit tests. </summary>
        private static long NUMBER_OF_FIELD_TOKENS = 5000;

        /// <summary>
        /// The default average field length in the unit tests. </summary>
        private static float AVG_FIELD_LENGTH = 50;

        /// <summary>
        /// The default document frequency in the unit tests. </summary>
        private static int DOC_FREQ = 10;

        /// <summary>
        /// The default total number of occurrences of this term across all documents
        /// in the unit tests.
        /// </summary>
        private static long TOTAL_TERM_FREQ = 70;

        /// <summary>
        /// The default tf in the unit tests. </summary>
        private static float FREQ = 7;

        /// <summary>
        /// The default document length in the unit tests. </summary>
        private static int DOC_LEN = 40;

        /// <summary>
        /// Creates the default statistics object that the specific tests modify. </summary>
        private BasicStats CreateStats()
        {
            BasicStats stats = new BasicStats("spoof", 1);
            stats.NumberOfDocuments = NUMBER_OF_DOCUMENTS;
            stats.NumberOfFieldTokens = NUMBER_OF_FIELD_TOKENS;
            stats.AvgFieldLength = AVG_FIELD_LENGTH;
            stats.DocFreq = DOC_FREQ;
            stats.TotalTermFreq = TOTAL_TERM_FREQ;
            return stats;
        }

        private CollectionStatistics ToCollectionStats(BasicStats stats)
        {
            return new CollectionStatistics(stats.Field, stats.NumberOfDocuments, -1, stats.NumberOfFieldTokens, -1);
        }

        private TermStatistics ToTermStats(BasicStats stats)
        {
            return new TermStatistics(new BytesRef("spoofyText"), stats.DocFreq, stats.TotalTermFreq);
        }

        /// <summary>
        /// The generic test core called by all unit test methods. It calls the
        /// <seealso cref="SimilarityBase#score(BasicStats, float, float)"/> method of all
        /// Similarities in <seealso cref="#sims"/> and checks if the score is valid; i.e. it
        /// is a finite positive real number.
        /// </summary>
        private void UnitTestCore(BasicStats stats, float freq, int docLen)
        {
            foreach (SimilarityBase sim in sims)
            {
                BasicStats realStats = (BasicStats)sim.ComputeWeight(stats.TotalBoost, ToCollectionStats(stats), ToTermStats(stats));
                float score = sim.Score(realStats, freq, docLen);
                float explScore = sim.Explain(realStats, 1, new Explanation(freq, "freq"), docLen).Value;
                Assert.IsFalse(float.IsInfinity(score), "Score infinite: " + sim.ToString());
                Assert.IsFalse(float.IsNaN(score), "Score NaN: " + sim.ToString());
                Assert.IsTrue(score >= 0, "Score negative: " + sim.ToString());
                Assert.AreEqual(score, explScore, FLOAT_EPSILON, "score() and explain() return different values: " + sim.ToString());
            }
        }

        /// <summary>
        /// Runs the unit test with the default statistics. </summary>
        [Test]
        public virtual void TestDefault()
        {
            UnitTestCore(CreateStats(), FREQ, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when
        /// <c>numberOfDocuments = numberOfFieldTokens</c>.
        /// </summary>
        [Test]
        public virtual void TestSparseDocuments()
        {
            BasicStats stats = CreateStats();
            stats.NumberOfFieldTokens = stats.NumberOfDocuments;
            stats.TotalTermFreq = stats.DocFreq;
            stats.AvgFieldLength = (float)stats.NumberOfFieldTokens / stats.NumberOfDocuments;
            UnitTestCore(stats, FREQ, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when
        /// <c>numberOfDocuments > numberOfFieldTokens</c>.
        /// </summary>
        [Test]
        public virtual void TestVerySparseDocuments()
        {
            BasicStats stats = CreateStats();
            stats.NumberOfFieldTokens = stats.NumberOfDocuments * 2 / 3;
            stats.TotalTermFreq = stats.DocFreq;
            stats.AvgFieldLength = (float)stats.NumberOfFieldTokens / stats.NumberOfDocuments;
            UnitTestCore(stats, FREQ, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when
        /// <c>NumberOfDocuments = 1</c>.
        /// </summary>
        [Test]
        public virtual void TestOneDocument()
        {
            BasicStats stats = CreateStats();
            stats.NumberOfDocuments = 1;
            stats.NumberOfFieldTokens = DOC_LEN;
            stats.AvgFieldLength = DOC_LEN;
            stats.DocFreq = 1;
            stats.TotalTermFreq = (int)FREQ;
            UnitTestCore(stats, FREQ, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when
        /// <c>docFreq = numberOfDocuments</c>.
        /// </summary>
        [Test]
        public virtual void TestAllDocumentsRelevant()
        {
            BasicStats stats = CreateStats();
            float mult = (0.0f + stats.NumberOfDocuments) / stats.DocFreq;
            stats.TotalTermFreq = (int)(stats.TotalTermFreq * mult);
            stats.DocFreq = stats.NumberOfDocuments;
            UnitTestCore(stats, FREQ, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when
        /// <c>docFreq > numberOfDocuments / 2</c>.
        /// </summary>
        [Test]
        public virtual void TestMostDocumentsRelevant()
        {
            BasicStats stats = CreateStats();
            float mult = (0.6f * stats.NumberOfDocuments) / stats.DocFreq;
            stats.TotalTermFreq = (int)(stats.TotalTermFreq * mult);
            stats.DocFreq = (int)(stats.NumberOfDocuments * 0.6);
            UnitTestCore(stats, FREQ, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when
        /// <c>docFreq = 1</c>.
        /// </summary>
        [Test]
        public virtual void TestOnlyOneRelevantDocument()
        {
            BasicStats stats = CreateStats();
            stats.DocFreq = 1;
            stats.TotalTermFreq = (int)FREQ + 3;
            UnitTestCore(stats, FREQ, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when
        /// <c>totalTermFreq = numberOfFieldTokens</c>.
        /// </summary>
        [Test]
        public virtual void TestAllTermsRelevant()
        {
            BasicStats stats = CreateStats();
            stats.TotalTermFreq = stats.NumberOfFieldTokens;
            UnitTestCore(stats, DOC_LEN, DOC_LEN);
            stats.AvgFieldLength = DOC_LEN + 10;
            UnitTestCore(stats, DOC_LEN, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when
        /// <c>totalTermFreq > numberOfDocuments</c>.
        /// </summary>
        [Test]
        public virtual void TestMoreTermsThanDocuments()
        {
            BasicStats stats = CreateStats();
            stats.TotalTermFreq = stats.TotalTermFreq + stats.NumberOfDocuments;
            UnitTestCore(stats, 2 * FREQ, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when
        /// <c>totalTermFreq = numberOfDocuments</c>.
        /// </summary>
        [Test]
        public virtual void TestNumberOfTermsAsDocuments()
        {
            BasicStats stats = CreateStats();
            stats.TotalTermFreq = stats.NumberOfDocuments;
            UnitTestCore(stats, FREQ, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when <c>totalTermFreq = 1</c>.
        /// </summary>
        [Test]
        public virtual void TestOneTerm()
        {
            BasicStats stats = CreateStats();
            stats.DocFreq = 1;
            stats.TotalTermFreq = 1;
            UnitTestCore(stats, 1, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when <c>totalTermFreq = freq</c>.
        /// </summary>
        [Test]
        public virtual void TestOneRelevantDocument()
        {
            BasicStats stats = CreateStats();
            stats.DocFreq = 1;
            stats.TotalTermFreq = (int)FREQ;
            UnitTestCore(stats, FREQ, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when <c>numberOfFieldTokens = freq</c>.
        /// </summary>
        [Test]
        public virtual void TestAllTermsRelevantOnlyOneDocument()
        {
            BasicStats stats = CreateStats();
            stats.NumberOfDocuments = 10;
            stats.NumberOfFieldTokens = 50;
            stats.AvgFieldLength = 5;
            stats.DocFreq = 1;
            stats.TotalTermFreq = 50;
            UnitTestCore(stats, 50, 50);
        }

        /// <summary>
        /// Tests correct behavior when there is only one document with a single term
        /// in the collection.
        /// </summary>
        [Test]
        public virtual void TestOnlyOneTermOneDocument()
        {
            BasicStats stats = CreateStats();
            stats.NumberOfDocuments = 1;
            stats.NumberOfFieldTokens = 1;
            stats.AvgFieldLength = 1;
            stats.DocFreq = 1;
            stats.TotalTermFreq = 1;
            UnitTestCore(stats, 1, 1);
        }

        /// <summary>
        /// Tests correct behavior when there is only one term in the field, but
        /// more than one documents.
        /// </summary>
        [Test]
        public virtual void TestOnlyOneTerm()
        {
            BasicStats stats = CreateStats();
            stats.NumberOfFieldTokens = 1;
            stats.AvgFieldLength = 1.0f / stats.NumberOfDocuments;
            stats.DocFreq = 1;
            stats.TotalTermFreq = 1;
            UnitTestCore(stats, 1, DOC_LEN);
        }

        /// <summary>
        /// Tests correct behavior when <c>avgFieldLength = docLen</c>.
        /// </summary>
        [Test]
        public virtual void TestDocumentLengthAverage()
        {
            BasicStats stats = CreateStats();
            UnitTestCore(stats, FREQ, (int)stats.AvgFieldLength);
        }

        // ---------------------------- Correctness tests ----------------------------

        /// <summary>
        /// Correctness test for the Dirichlet LM model. </summary>
        [Test]
        public virtual void TestLMDirichlet()
        {
            float p = (FREQ + 2000.0f * (TOTAL_TERM_FREQ + 1) / (NUMBER_OF_FIELD_TOKENS + 1.0f)) / (DOC_LEN + 2000.0f);
            float a = 2000.0f / (DOC_LEN + 2000.0f);
            float gold = (float)(Math.Log(p / (a * (TOTAL_TERM_FREQ + 1) / (NUMBER_OF_FIELD_TOKENS + 1.0f))) + Math.Log(a));
            CorrectnessTestCore(new LMDirichletSimilarity(), gold);
        }

        /// <summary>
        /// Correctness test for the Jelinek-Mercer LM model. </summary>
        [Test]
        public virtual void TestLMJelinekMercer()
        {
            float p = (1 - 0.1f) * FREQ / DOC_LEN + 0.1f * (TOTAL_TERM_FREQ + 1) / (NUMBER_OF_FIELD_TOKENS + 1.0f);
            float gold = (float)(Math.Log(p / (0.1f * (TOTAL_TERM_FREQ + 1) / (NUMBER_OF_FIELD_TOKENS + 1.0f))));
            CorrectnessTestCore(new LMJelinekMercerSimilarity(0.1f), gold);
        }

        /// <summary>
        /// Correctness test for the LL IB model with DF-based lambda and
        /// no normalization.
        /// </summary>
        [Test]
        public virtual void TestLLForIB()
        {
            SimilarityBase sim = new IBSimilarity(new DistributionLL(), new LambdaDF(), new Normalization.NoNormalization());
            CorrectnessTestCore(sim, 4.178574562072754f);
        }

        /// <summary>
        /// Correctness test for the SPL IB model with TTF-based lambda and
        /// no normalization.
        /// </summary>
        [Test]
        public virtual void TestSPLForIB()
        {
            SimilarityBase sim = new IBSimilarity(new DistributionSPL(), new LambdaTTF(), new Normalization.NoNormalization());
            CorrectnessTestCore(sim, 2.2387237548828125f);
        }

        /// <summary>
        /// Correctness test for the PL2 DFR model. </summary>
        [Test]
        public virtual void TestPL2()
        {
            SimilarityBase sim = new DFRSimilarity(new BasicModelP(), new AfterEffectL(), new NormalizationH2());
            float tfn = (float)(FREQ * SimilarityBase.Log2(1 + AVG_FIELD_LENGTH / DOC_LEN)); // 8.1894750101
            float l = 1.0f / (tfn + 1.0f); // 0.108820144666
            float lambda = (1.0f + TOTAL_TERM_FREQ) / (1f + NUMBER_OF_DOCUMENTS); // 0.7029703
            float p = (float)(tfn * SimilarityBase.Log2(tfn / lambda) + (lambda + 1 / (12 * tfn) - tfn) * SimilarityBase.Log2(Math.E) + 0.5 * SimilarityBase.Log2(2 * Math.PI * tfn)); // 21.065619
            float gold = l * p; // 2.2923636
            CorrectnessTestCore(sim, gold);
        }

        /// <summary>
        /// Correctness test for the IneB2 DFR model. </summary>
        [Test]
        public virtual void TestIneB2()
        {
            SimilarityBase sim = new DFRSimilarity(new BasicModelIne(), new AfterEffectB(), new NormalizationH2());
            CorrectnessTestCore(sim, 5.747603416442871f);
        }

        /// <summary>
        /// Correctness test for the GL1 DFR model. </summary>
        [Test]
        public virtual void TestGL1()
        {
            SimilarityBase sim = new DFRSimilarity(new BasicModelG(), new AfterEffectL(), new NormalizationH1());
            CorrectnessTestCore(sim, 1.6390540599822998f);
        }

        /// <summary>
        /// Correctness test for the BEB1 DFR model. </summary>
        [Test]
        public virtual void TestBEB1()
        {
            SimilarityBase sim = new DFRSimilarity(new BasicModelBE(), new AfterEffectB(), new NormalizationH1());
            float tfn = FREQ * AVG_FIELD_LENGTH / DOC_LEN; // 8.75
            float b = (TOTAL_TERM_FREQ + 1 + 1) / ((DOC_FREQ + 1) * (tfn + 1)); // 0.67132866
            double f = TOTAL_TERM_FREQ + 1 + tfn;
            double n = f + NUMBER_OF_DOCUMENTS;
            double n1 = n + f - 1; // 258.5
            double m1 = n + f - tfn - 2; // 248.75
            double n2 = f; // 79.75
            double m2 = f - tfn; // 71.0
            float be = (float)(-SimilarityBase.Log2(n - 1) - SimilarityBase.Log2(Math.E) + ((m1 + 0.5f) * SimilarityBase.Log2(n1 / m1) + (n1 - m1) * SimilarityBase.Log2(n1)) - ((m2 + 0.5f) * SimilarityBase.Log2(n2 / m2) + (n2 - m2) * SimilarityBase.Log2(n2))); // 67.26544321004599 -  91.9620374903885 -  -8.924494472554715
            // 15.7720995
            float gold = b * be; // 10.588263
            CorrectnessTestCore(sim, gold);
        }

        /// <summary>
        /// Correctness test for the D DFR model (basic model only). </summary>
        [Test]
        public virtual void TestD()
        {
            SimilarityBase sim = new DFRSimilarity(new BasicModelD(), new AfterEffect.NoAfterEffect(), new Normalization.NoNormalization());
            double totalTermFreqNorm = TOTAL_TERM_FREQ + FREQ + 1;
            double p = 1.0 / (NUMBER_OF_DOCUMENTS + 1); // 0.009900990099009901
            double phi = FREQ / totalTermFreqNorm; // 0.08974358974358974
            double D = phi * SimilarityBase.Log2(phi / p) + (1 - phi) * SimilarityBase.Log2((1 - phi) / (1 - p)); // 0.17498542370019005
            float gold = (float)(totalTermFreqNorm * D + 0.5 * SimilarityBase.Log2(1 + 2 * Math.PI * FREQ * (1 - phi))); // 16.328257
            CorrectnessTestCore(sim, gold);
        }

        /// <summary>
        /// Correctness test for the In2 DFR model with no aftereffect. </summary>
        [Test]
        public virtual void TestIn2()
        {
            SimilarityBase sim = new DFRSimilarity(new BasicModelIn(), new AfterEffect.NoAfterEffect(), new NormalizationH2());
            float tfn = (float)(FREQ * SimilarityBase.Log2(1 + AVG_FIELD_LENGTH / DOC_LEN)); // 8.1894750101
            float gold = (float)(tfn * SimilarityBase.Log2((NUMBER_OF_DOCUMENTS + 1) / (DOC_FREQ + 0.5))); // 26.7459577898
            CorrectnessTestCore(sim, gold);
        }

        /// <summary>
        /// Correctness test for the IFB DFR model with no normalization. </summary>
        [Test]
        public virtual void TestIFB()
        {
            SimilarityBase sim = new DFRSimilarity(new BasicModelIF(), new AfterEffectB(), new Normalization.NoNormalization());
            float B = (TOTAL_TERM_FREQ + 1 + 1) / ((DOC_FREQ + 1) * (FREQ + 1)); // 0.8875
            float IF = (float)(FREQ * SimilarityBase.Log2(1 + (NUMBER_OF_DOCUMENTS + 1) / (TOTAL_TERM_FREQ + 0.5))); // 8.97759389642
            float gold = B * IF; // 7.96761458307
            CorrectnessTestCore(sim, gold);
        }

        /// <summary>
        /// The generic test core called by all correctness test methods. It calls the
        /// <see cref="SimilarityBase.Score(BasicStats, float, float)"/> method of all
        /// Similarities in <see cref="sims"/> and compares the score against the manually
        /// computed <c>gold</c>.
        /// </summary>
        private void CorrectnessTestCore(SimilarityBase sim, float gold)
        {
            BasicStats stats = CreateStats();
            BasicStats realStats = (BasicStats)sim.ComputeWeight(stats.TotalBoost, ToCollectionStats(stats), ToTermStats(stats));
            float score = sim.Score(realStats, FREQ, DOC_LEN);
            Assert.AreEqual(gold, score, FLOAT_EPSILON, sim.ToString() + " score not correct.");
        }

        // ---------------------------- Integration tests ----------------------------

        /// <summary>
        /// The "collection" for the integration tests. </summary>
        internal string[] docs = new string[] { "Tiger, tiger burning bright   In the forest of the night   What immortal hand or eye   Could frame thy fearful symmetry ?", "In what distant depths or skies   Burnt the fire of thine eyes ?   On what wings dare he aspire ?   What the hands the seize the fire ?", "And what shoulder and what art   Could twist the sinews of thy heart ?   And when thy heart began to beat What dread hand ? And what dread feet ?", "What the hammer? What the chain ?   In what furnace was thy brain ?   What the anvil ? And what dread grasp   Dare its deadly terrors clasp ?", "And when the stars threw down their spears   And water'd heaven with their tear   Did he smile his work to see ?   Did he, who made the lamb, made thee ?", "Tiger, tiger burning bright   In the forest of the night   What immortal hand or eye   Dare frame thy fearful symmetry ?", "Cruelty has a human heart   And jealousy a human face   Terror the human form divine   And Secrecy the human dress .", "The human dress is forg'd iron   The human form a fiery forge   The human face a furnace seal'd   The human heart its fiery gorge ." };

        /// <summary>
        /// Tests whether all similarities return three documents for the query word
        /// "heart".
        /// </summary>
        [Test]
        public virtual void TestHeartList()
        {
            Query q = new TermQuery(new Term(FIELD_BODY, "heart"));

            foreach (SimilarityBase sim in sims)
            {
                searcher.Similarity = sim;
                TopDocs topDocs = searcher.Search(q, 1000);
                Assert.AreEqual(3, topDocs.TotalHits, "Failed: " + sim.ToString());
            }
        }

        /// <summary>
        /// Test whether all similarities return document 3 before documents 7 and 8. </summary>
        [Test]
        public virtual void TestHeartRanking()
        {
            AssumeFalse("PreFlex codec does not support the stats necessary for this test!", "Lucene3x".Equals(Codec.Default.Name, StringComparison.Ordinal));

            Query q = new TermQuery(new Term(FIELD_BODY, "heart"));

            foreach (SimilarityBase sim in sims)
            {
                searcher.Similarity = sim;
                TopDocs topDocs = searcher.Search(q, 1000);
                Assert.AreEqual("2", reader.Document(topDocs.ScoreDocs[0].Doc).Get(FIELD_ID), "Failed: " + sim.ToString());
            }
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        // LUCENE-5221
        [Test]
        public virtual void TestDiscountOverlapsBoost()
        {
            DefaultSimilarity expected = new DefaultSimilarity();
            SimilarityBase actual = new DFRSimilarity(new BasicModelIne(), new AfterEffectB(), new NormalizationH2());
            expected.DiscountOverlaps = false;
            actual.DiscountOverlaps = false;
            FieldInvertState state = new FieldInvertState("foo");
            state.Length = 5;
            state.NumOverlap = 2;
            state.Boost = 3;
            Assert.AreEqual(expected.ComputeNorm(state), actual.ComputeNorm(state));
            expected.DiscountOverlaps = true;
            actual.DiscountOverlaps = true;
            Assert.AreEqual(expected.ComputeNorm(state), actual.ComputeNorm(state));
        }
    }
}