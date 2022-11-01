using System;
using System.Diagnostics;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Classification
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

    /**
     * Base class for testing <see cref="IClassifier{T}"/>s
     */
    public abstract class ClassificationTestBase<T> : Util.LuceneTestCase
    {
        public readonly static String POLITICS_INPUT = "Here are some interesting questions and answers about Mitt Romney.. " +
            "If you don't know the answer to the question about Mitt Romney, then simply click on the answer below the question section.";
        public static readonly BytesRef POLITICS_RESULT = new BytesRef("politics");

        public static readonly String TECHNOLOGY_INPUT = "Much is made of what the likes of Facebook, Google and Apple know about users." +
            " Truth is, Amazon may know more.";
        public static readonly BytesRef TECHNOLOGY_RESULT = new BytesRef("technology");

        private RandomIndexWriter indexWriter;
        private Directory dir;
        private FieldType ft;

        protected String textFieldName;
        protected String categoryFieldName;

        String booleanFieldName;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            indexWriter = new RandomIndexWriter(Random, dir);
            textFieldName = "text";
            categoryFieldName = "cat";
            booleanFieldName = "bool";
            ft = new FieldType(TextField.TYPE_STORED);
            ft.StoreTermVectors = true;
            ft.StoreTermVectorOffsets = true;
            ft.StoreTermVectorPositions = true;
        }

        [TearDown]
        public override void TearDown()
        {
            indexWriter.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        protected void CheckCorrectClassification(IClassifier<T> classifier, String inputDoc, T expectedResult, Analyzer analyzer, String textFieldName, String classFieldName)
        {
            CheckCorrectClassification(classifier, inputDoc, expectedResult, analyzer, textFieldName, classFieldName, null);
        }

        protected void CheckCorrectClassification(IClassifier<T> classifier, String inputDoc, T expectedResult, Analyzer analyzer, String textFieldName, String classFieldName, Query query)
        {
            AtomicReader atomicReader = null;
            try
            {
                PopulateSampleIndex(analyzer);
                atomicReader = SlowCompositeReaderWrapper.Wrap(indexWriter.GetReader());
                classifier.Train(atomicReader, textFieldName, classFieldName, analyzer, query);
                ClassificationResult<T> classificationResult = classifier.AssignClass(inputDoc);
                Assert.NotNull(classificationResult.AssignedClass);
                Assert.AreEqual(expectedResult, classificationResult.AssignedClass, "got an assigned class of " + classificationResult.AssignedClass);
                Assert.IsTrue(classificationResult.Score > 0, "got a not positive score " + classificationResult.Score);
            }
            finally
            {
                if (atomicReader != null)
                    atomicReader.Dispose();
            }
        }
        protected void CheckOnlineClassification(IClassifier<T> classifier, String inputDoc, T expectedResult, Analyzer analyzer, String textFieldName, String classFieldName)
        {
            CheckOnlineClassification(classifier, inputDoc, expectedResult, analyzer, textFieldName, classFieldName, null);
        }

        protected void CheckOnlineClassification(IClassifier<T> classifier, String inputDoc, T expectedResult, Analyzer analyzer, String textFieldName, String classFieldName, Query query)
        {
            AtomicReader atomicReader = null;
            try
            {
                PopulateSampleIndex(analyzer);
                atomicReader = SlowCompositeReaderWrapper.Wrap(indexWriter.GetReader());
                classifier.Train(atomicReader, textFieldName, classFieldName, analyzer, query);
                ClassificationResult<T> classificationResult = classifier.AssignClass(inputDoc);
                Assert.NotNull(classificationResult.AssignedClass);
                Assert.AreEqual(expectedResult, classificationResult.AssignedClass, "got an assigned class of " + classificationResult.AssignedClass);
                Assert.IsTrue(classificationResult.Score > 0, "got a not positive score " + classificationResult.Score);
                UpdateSampleIndex(analyzer);
                ClassificationResult<T> secondClassificationResult = classifier.AssignClass(inputDoc);
                Equals(classificationResult.AssignedClass, secondClassificationResult.AssignedClass);
                Equals(classificationResult.Score, secondClassificationResult.Score);

            }
            finally
            {
                if (atomicReader != null)
                    atomicReader.Dispose();
            }
        }

        private void PopulateSampleIndex(Analyzer analyzer)
        {
            indexWriter.DeleteAll();
            indexWriter.Commit();

            String text;

            Document doc = new Document();
            text = "The traveling press secretary for Mitt Romney lost his cool and cursed at reporters " +
                "who attempted to ask questions of the Republican presidential candidate in a public plaza near the Tomb of " +
                "the Unknown Soldier in Warsaw Tuesday.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "politics", ft));
            doc.Add(new Field(booleanFieldName, "true", ft));

            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "Mitt Romney seeks to assure Israel and Iran, as well as Jewish voters in the United" +
                " States, that he will be tougher against Iran's nuclear ambitions than President Barack Obama.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "politics", ft));
            doc.Add(new Field(booleanFieldName, "true", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "And there's a threshold question that he has to answer for the American people and " +
                "that's whether he is prepared to be commander-in-chief,\" she continued. \"As we look to the past events, we " +
                "know that this raises some questions about his preparedness and we'll see how the rest of his trip goes.\"";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "politics", ft));
            doc.Add(new Field(booleanFieldName, "true", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "Still, when it comes to gun policy, many congressional Democrats have \"decided to " +
                "keep quiet and not go there,\" said Alan Lizotte, dean and professor at the State University of New York at " +
                "Albany's School of Criminal Justice.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "politics", ft));
            doc.Add(new Field(booleanFieldName, "true", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "Standing amongst the thousands of people at the state Capitol, Jorstad, director of " +
                "technology at the University of Wisconsin-La Crosse, documented the historic moment and shared it with the " +
                "world through the Internet.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "technology", ft));
            doc.Add(new Field(booleanFieldName, "false", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "So, about all those experts and analysts who've spent the past year or so saying " +
                "Facebook was going to make a phone. A new expert has stepped forward to say it's not going to happen.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "technology", ft));
            doc.Add(new Field(booleanFieldName, "false", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "More than 400 million people trust Google with their e-mail, and 50 million store files" +
                " in the cloud using the Dropbox service. People manage their bank accounts, pay bills, trade stocks and " +
                "generally transfer or store huge volumes of personal data online.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "technology", ft));
            doc.Add(new Field(booleanFieldName, "false", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "unlabeled doc";
            doc.Add(new Field(textFieldName, text, ft));
            indexWriter.AddDocument(doc, analyzer);

            indexWriter.Commit();
        }

        protected void CheckPerformance(IClassifier<T> classifier, Analyzer analyzer, String classFieldName)
        {
            AtomicReader atomicReader = null;
            long trainStart = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            try
            {
                PopulatePerformanceIndex(analyzer);
                atomicReader = SlowCompositeReaderWrapper.Wrap(indexWriter.GetReader());
                classifier.Train(atomicReader, textFieldName, classFieldName, analyzer);
                long trainEnd = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                long trainTime = trainEnd - trainStart;
                // LUCENENET: This test is running slow on .NET Framework in CI, so we are giving it a little more time to complete.
#if NETFRAMEWORK
                Assert.IsTrue(trainTime < 150000, "training took more than 2.5 mins : " + trainTime / 1000 + "s");
#else
                Assert.IsTrue(trainTime < 120000, "training took more than 2 mins : " + trainTime / 1000 + "s");
#endif
            }
            finally
            {
                if (atomicReader != null)
                    atomicReader.Dispose();
            }
        }

        private void PopulatePerformanceIndex(Analyzer analyzer)
        {
            indexWriter.DeleteAll();
            indexWriter.Commit();

            FieldType ft = new FieldType(TextField.TYPE_STORED);
            ft.StoreTermVectors = true;
            ft.StoreTermVectorOffsets = true;
            ft.StoreTermVectorPositions = true;
            int docs = 1000;
            Random random = Random;
            for (int i = 0; i < docs; i++)
            {
                Boolean b = random.NextBoolean();
                Document doc = new Document();
                doc.Add(new Field(textFieldName, createRandomString(random), ft));
                doc.Add(new Field(categoryFieldName, b ? "technology" : "politics", ft));
                doc.Add(new Field(booleanFieldName, b.ToString(), ft));
                indexWriter.AddDocument(doc, analyzer);
            }
            indexWriter.Commit();
        }

        private String createRandomString(Random random)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 20; i++)
            {
                builder.Append(TestUtil.RandomSimpleString(random, 5));
                builder.Append(' ');
            }
            return builder.ToString();
        }

        private void UpdateSampleIndex(Analyzer analyzer)
        {
            String text;

            Document doc = new Document();
            text = "Warren Bennis says John F. Kennedy grasped a key lesson about the presidency that few have followed.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "politics", ft));
            doc.Add(new Field(booleanFieldName, "true", ft));

            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "Julian Zelizer says Bill Clinton is still trying to shape his party, years after the White House, while George W. Bush opts for a much more passive role.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "politics", ft));
            doc.Add(new Field(booleanFieldName, "true", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "Crossfire: Sen. Tim Scott passes on Sen. Lindsey Graham endorsement";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "politics", ft));
            doc.Add(new Field(booleanFieldName, "true", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "Illinois becomes 16th state to allow same-sex marriage.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "politics", ft));
            doc.Add(new Field(booleanFieldName, "true", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "Apple is developing iPhones with curved-glass screens and enhanced sensors that detect different levels of pressure, according to a new report.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "technology", ft));
            doc.Add(new Field(booleanFieldName, "false", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "The Xbox One is Microsoft's first new gaming console in eight years. It's a quality piece of hardware but it's also noteworthy because Microsoft is using it to make a statement.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "technology", ft));
            doc.Add(new Field(booleanFieldName, "false", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "Google says it will replace a Google Maps image after a California father complained it shows the body of his teen-age son, who was shot to death in 2009.";
            doc.Add(new Field(textFieldName, text, ft));
            doc.Add(new Field(categoryFieldName, "technology", ft));
            doc.Add(new Field(booleanFieldName, "false", ft));
            indexWriter.AddDocument(doc, analyzer);

            doc = new Document();
            text = "second unlabeled doc";
            doc.Add(new Field(textFieldName, text, ft));
            indexWriter.AddDocument(doc, analyzer);

            indexWriter.Commit();
        }
    }
}