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

using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Classification.Utils
{
    /**
     * Utility class for creating training / test / cross validation indexes from the original index.
     */
    public class DatasetSplitter
    {

        private readonly double _crossValidationRatio;
        private readonly double _testRatio;

        /**
         * Create a {@link DatasetSplitter} by giving test and cross validation IDXs sizes
         *
         * @param testRatio            the ratio of the original index to be used for the test IDX as a <code>double</code> between 0.0 and 1.0
         * @param crossValidationRatio the ratio of the original index to be used for the c.v. IDX as a <code>double</code> between 0.0 and 1.0
         */
        public DatasetSplitter(double testRatio, double crossValidationRatio)
        {
            this._crossValidationRatio = crossValidationRatio;
            this._testRatio = testRatio;
        }

        /**
         * Split a given index into 3 indexes for training, test and cross validation tasks respectively
         *
         * @param originalIndex        an {@link AtomicReader} on the source index
         * @param trainingIndex        a {@link Directory} used to write the training index
         * @param testIndex            a {@link Directory} used to write the test index
         * @param crossValidationIndex a {@link Directory} used to write the cross validation index
         * @param analyzer             {@link Analyzer} used to create the new docs
         * @param fieldNames           names of fields that need to be put in the new indexes or <code>null</code> if all should be used
         * @throws IOException if any writing operation fails on any of the indexes
         */
        public void Split(AtomicReader originalIndex, Directory trainingIndex, Directory testIndex, Directory crossValidationIndex, Analyzer analyzer, params string[] fieldNames)
        {
            // create IWs for train / test / cv IDXs
            IndexWriter testWriter = new IndexWriter(testIndex, new IndexWriterConfig(Util.LuceneVersion.LUCENE_CURRENT, analyzer));
            IndexWriter cvWriter = new IndexWriter(crossValidationIndex, new IndexWriterConfig(Util.LuceneVersion.LUCENE_CURRENT, analyzer));
            IndexWriter trainingWriter = new IndexWriter(trainingIndex, new IndexWriterConfig(Util.LuceneVersion.LUCENE_CURRENT, analyzer));

            try
            {
                int size = originalIndex.MaxDoc;

                IndexSearcher indexSearcher = new IndexSearcher(originalIndex);
                TopDocs topDocs = indexSearcher.Search(new MatchAllDocsQuery(), Int32.MaxValue);

                // set the type to be indexed, stored, with term vectors
                FieldType ft = new FieldType(TextField.TYPE_STORED);
                ft.StoreTermVectors = true;
                ft.StoreTermVectorOffsets = true;
                ft.StoreTermVectorPositions = true;

                int b = 0;

                // iterate over existing documents
                foreach (ScoreDoc scoreDoc in topDocs.ScoreDocs)
                {
                    // create a new document for indexing
                    Document doc = new Document();
                    if (fieldNames != null && fieldNames.Length > 0)
                    {
                        foreach (String fieldName in fieldNames)
                        {
                            doc.Add(new Field(fieldName, originalIndex.Document(scoreDoc.Doc).GetField(fieldName).ToString(), ft));
                        }
                    }
                    else
                    {
                        foreach (IndexableField storableField in originalIndex.Document(scoreDoc.Doc).Fields)
                        {
                            if (storableField.GetReaderValue() != null)
                            {
                                doc.Add(new Field(storableField.Name, storableField.GetReaderValue(), ft));
                            }
                            else if (storableField.GetBinaryValue() != null)
                            {
                                doc.Add(new Field(storableField.Name, storableField.GetBinaryValue(), ft));
                            }
                            else if (storableField.GetStringValue() != null)
                            {
                                doc.Add(new Field(storableField.Name, storableField.GetStringValue(), ft));
                            }
                            else if (storableField.GetNumericValue() != null)
                            {
                                doc.Add(new Field(storableField.Name, storableField.GetNumericValue().ToString(), ft));
                            }
                        }
                    }

                    // add it to one of the IDXs
                    if (b % 2 == 0 && testWriter.MaxDoc < size * _testRatio)
                    {
                        testWriter.AddDocument(doc);
                    }
                    else if (cvWriter.MaxDoc < size * _crossValidationRatio)
                    {
                        cvWriter.AddDocument(doc);
                    }
                    else
                    {
                        trainingWriter.AddDocument(doc);
                    }
                    b++;
                }
            }
            catch (Exception e)
            {
                throw new IOException("Exceptio in DatasetSplitter", e);
            }
            finally
            {
                testWriter.Commit();
                cvWriter.Commit();
                trainingWriter.Commit();
                // close IWs
                testWriter.Dispose();
                cvWriter.Dispose();
                trainingWriter.Dispose();
            }
        }

    }
}