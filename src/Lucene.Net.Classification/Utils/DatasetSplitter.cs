using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Globalization;
using System.IO;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Classification.Utils
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

    /// <summary>
    /// Utility class for creating training / test / cross validation indexes from the original index.
    /// </summary>
    public class DatasetSplitter
    {

        private readonly double crossValidationRatio;
        private readonly double testRatio;

        /// <summary>
        /// Create a <see cref="DatasetSplitter"/> by giving test and cross validation IDXs sizes
        /// </summary>
        /// <param name="testRatio">the ratio of the original index to be used for the test IDX as a <see cref="double"/> between 0.0 and 1.0</param>
        /// <param name="crossValidationRatio">the ratio of the original index to be used for the c.v. IDX as a <see cref="double"/> between 0.0 and 1.0</param>
        public DatasetSplitter(double testRatio, double crossValidationRatio)
        {
            this.crossValidationRatio = crossValidationRatio;
            this.testRatio = testRatio;
        }

        /// <summary>
        /// Split a given index into 3 indexes for training, test and cross validation tasks respectively
        /// </summary>
        /// <param name="originalIndex">an <see cref="AtomicReader"/> on the source index</param>
        /// <param name="trainingIndex">a <see cref="Directory"/> used to write the training index</param>
        /// <param name="testIndex">a <see cref="Directory"/> used to write the test index</param>
        /// <param name="crossValidationIndex">a <see cref="Directory"/> used to write the cross validation index</param>
        /// <param name="analyzer"><see cref="Analyzer"/> used to create the new docs</param>
        /// <param name="fieldNames">names of fields that need to be put in the new indexes or <c>null</c> if all should be used</param>
        /// <exception cref="IOException">if any writing operation fails on any of the indexes</exception>
        public virtual void Split(AtomicReader originalIndex, Directory trainingIndex, Directory testIndex, Directory crossValidationIndex, Analyzer analyzer, params string[] fieldNames)
        {
#pragma warning disable 612, 618
            // create IWs for train / test / cv IDXs
            IndexWriter testWriter = new IndexWriter(testIndex, new IndexWriterConfig(LuceneVersion.LUCENE_CURRENT, analyzer));
            IndexWriter cvWriter = new IndexWriter(crossValidationIndex, new IndexWriterConfig(LuceneVersion.LUCENE_CURRENT, analyzer));
            IndexWriter trainingWriter = new IndexWriter(trainingIndex, new IndexWriterConfig(LuceneVersion.LUCENE_CURRENT, analyzer));
#pragma warning restore 612, 618

            try
            {
                int size = originalIndex.MaxDoc;

                IndexSearcher indexSearcher = new IndexSearcher(originalIndex);
                TopDocs topDocs = indexSearcher.Search(new MatchAllDocsQuery(), int.MaxValue);

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
                        foreach (string fieldName in fieldNames)
                        {
                            doc.Add(new Field(fieldName, originalIndex.Document(scoreDoc.Doc).GetField(fieldName).ToString(), ft));
                        }
                    }
                    else
                    {
                        // LUCENENET specific: Cache the value locally rather than fetching it twice
                        TextReader readerValue;
                        BytesRef binaryValue;
                        string stringValue;
                        foreach (IIndexableField storableField in originalIndex.Document(scoreDoc.Doc).Fields)
                        {
                            if ((readerValue = storableField.GetReaderValue()) != null)
                            {
                                doc.Add(new Field(storableField.Name, readerValue, ft));
                            }
                            else if ((binaryValue = storableField.GetBinaryValue()) != null)
                            {
                                doc.Add(new Field(storableField.Name, binaryValue, ft));
                            }
                            else if ((stringValue = storableField.GetStringValue(CultureInfo.InvariantCulture)) != null)
                            {
                                doc.Add(new Field(storableField.Name, stringValue, ft));
                            }
                        }
                    }

                    // add it to one of the IDXs
                    if (b % 2 == 0 && testWriter.MaxDoc < size * testRatio)
                    {
                        testWriter.AddDocument(doc);
                    }
                    else if (cvWriter.MaxDoc < size * crossValidationRatio)
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
            catch (Exception e) when (e.IsException())
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