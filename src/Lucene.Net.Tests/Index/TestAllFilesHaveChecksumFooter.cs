using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;

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

    using CodecUtil = Lucene.Net.Codecs.CodecUtil;
    using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NumericDocValuesField = NumericDocValuesField;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Test that a plain default puts CRC32 footers in all files.
    /// </summary>
    [TestFixture]
    public class TestAllFilesHaveChecksumFooter : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetCodec(new Lucene46Codec());
            RandomIndexWriter riw = new RandomIndexWriter(Random, dir, conf);
            Document doc = new Document();
            // these fields should sometimes get term vectors, etc
            Field idField = NewStringField("id", "", Field.Store.NO);
            Field bodyField = NewTextField("body", "", Field.Store.NO);
            Field dvField = new NumericDocValuesField("dv", 5);
            doc.Add(idField);
            doc.Add(bodyField);
            doc.Add(dvField);
            for (int i = 0; i < 100; i++)
            {
                idField.SetStringValue(Convert.ToString(i));
                bodyField.SetStringValue(TestUtil.RandomUnicodeString(Random));
                riw.AddDocument(doc);
                if (Random.Next(7) == 0)
                {
                    riw.Commit();
                }
                if (Random.Next(20) == 0)
                {
                    riw.DeleteDocuments(new Term("id", Convert.ToString(i)));
                }
            }
            riw.Dispose();
            CheckHeaders(dir);
            dir.Dispose();
        }

        private void CheckHeaders(Directory dir)
        {
            foreach (string file in dir.ListAll())
            {
                if (file.Equals(IndexWriter.WRITE_LOCK_NAME, StringComparison.Ordinal))
                {
                    continue; // write.lock has no footer, thats ok
                }
                if (file.EndsWith(IndexFileNames.COMPOUND_FILE_EXTENSION, StringComparison.Ordinal))
                {
                    CompoundFileDirectory cfsDir = new CompoundFileDirectory(dir, file, NewIOContext(Random), false);
                    CheckHeaders(cfsDir); // recurse into cfs
                    cfsDir.Dispose();
                }
                IndexInput @in = null;
                bool success = false;
                try
                {
                    @in = dir.OpenInput(file, NewIOContext(Random));
                    CodecUtil.ChecksumEntireFile(@in);
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Dispose(@in);
                    }
                    else
                    {
                        IOUtils.DisposeWhileHandlingException(@in);
                    }
                }
            }
        }
    }
}