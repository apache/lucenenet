/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Threading;
using NUnit.Framework;

using Analyzer = Lucene.Net.Analysis.Analyzer;
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Index
{
	
	[TestFixture]
	public class TestConcurrentMergeScheduler:LuceneTestCase
	{
		
		private static readonly Analyzer ANALYZER = new SimpleAnalyzer();
		
		private class FailOnlyOnFlush:MockRAMDirectory.Failure
		{
			//internal new bool doFail;
		    internal volatile bool hitExc;
			
			public override void SetDoFail()
			{
                this.doFail = true;
                hitExc = false;
			}
            public override void ClearDoFail()
			{
				this.doFail = false;
			}
			
			public override void  Eval(MockRAMDirectory dir)
			{
                if (doFail && !(Thread.CurrentThread.Name ?? "").Contains("Merge Thread"))
				{
					System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
					for (int i = 0; i < trace.FrameCount; i++)
					{
						System.Diagnostics.StackFrame sf = trace.GetFrame(i);
						if ("DoFlush".Equals(sf.GetMethod().Name))
						{
						    hitExc = true;
						    //Console.WriteLine(trace);
							throw new System.IO.IOException("now failing during flush");
						}
					}
				}
			}
		}
		
		// Make sure running BG merges still work fine even when
		// we are hitting exceptions during flushing.
        [Test]
        public virtual void TestFlushExceptions()
        {
            MockRAMDirectory directory = new MockRAMDirectory();
            FailOnlyOnFlush failure = new FailOnlyOnFlush();
            directory.FailOn(failure);

            IndexWriter writer = new IndexWriter(directory, ANALYZER, true, IndexWriter.MaxFieldLength.UNLIMITED);
            ConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
            writer.SetMergeScheduler(cms);
            writer.SetMaxBufferedDocs(2);
            Document doc = new Document();
            Field idField = new Field("id", "", Field.Store.YES, Field.Index.NOT_ANALYZED);
            doc.Add(idField);
            int extraCount = 0;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    idField.SetValue(System.Convert.ToString(i*20 + j));
                    writer.AddDocument(doc);
                }

                while (true)
                {
                    // must cycle here because sometimes the merge flushes
                    // the doc we just added and so there's nothing to
                    // flush, and we don't hit the exception
                    writer.AddDocument(doc);
                    failure.SetDoFail();
                    try
                    {
                        writer.Flush(true, false, true);
                        if (failure.hitExc)
                            Assert.Fail("failed to hit IOException");
                        extraCount++;
                    }
                    catch (System.IO.IOException ioe)
                    {
                        failure.ClearDoFail();
                        break;
                    }
                }
            }

            writer.Close();
            IndexReader reader = IndexReader.Open(directory, true);
            Assert.AreEqual(200 + extraCount, reader.NumDocs());
            reader.Close();
            directory.Close();
        }

	    // Test that deletes committed after a merge started and
		// before it finishes, are correctly merged back:
		[Test]
		public virtual void  TestDeleteMerging()
		{
			
			RAMDirectory directory = new MockRAMDirectory();

            IndexWriter writer = new IndexWriter(directory, ANALYZER, true, IndexWriter.MaxFieldLength.UNLIMITED);
			ConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
			writer.SetMergeScheduler(cms);
			
			LogDocMergePolicy mp = new LogDocMergePolicy(writer);
			writer.SetMergePolicy(mp);
			
			// Force degenerate merging so we can get a mix of
			// merging of segments with and without deletes at the
			// start:
			mp.MinMergeDocs = 1000;
			
			Document doc = new Document();
			Field idField = new Field("id", "", Field.Store.YES, Field.Index.NOT_ANALYZED);
			doc.Add(idField);
			for (int i = 0; i < 10; i++)
			{
				for (int j = 0; j < 100; j++)
				{
					idField.SetValue(System.Convert.ToString(i * 100 + j));
					writer.AddDocument(doc);
				}
				
				int delID = i;
				while (delID < 100 * (1 + i))
				{
					writer.DeleteDocuments(new Term("id", "" + delID));
					delID += 10;
				}
				
				writer.Commit();
			}
			
			writer.Close();
			IndexReader reader = IndexReader.Open(directory, true);
			// Verify that we did not lose any deletes...
			Assert.AreEqual(450, reader.NumDocs());
			reader.Close();
			directory.Close();
		}

        [Test]
        public virtual void TestNoExtraFiles()
        {
            RAMDirectory directory = new MockRAMDirectory();
            IndexWriter writer = new IndexWriter(directory, ANALYZER, true, IndexWriter.MaxFieldLength.UNLIMITED);

            for (int iter = 0; iter < 7; iter++)
            {
                ConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
                writer.SetMergeScheduler(cms);
                writer.SetMaxBufferedDocs(2);

                for (int j = 0; j < 21; j++)
                {
                    Document doc = new Document();
                    doc.Add(new Field("content", "a b c", Field.Store.NO, Field.Index.ANALYZED));
                    writer.AddDocument(doc);
                }

                writer.Close();
                TestIndexWriter.AssertNoUnreferencedFiles(directory, "testNoExtraFiles");
                // Reopen
                writer = new IndexWriter(directory, ANALYZER, false, IndexWriter.MaxFieldLength.UNLIMITED);
            }
            writer.Close();
            directory.Close();
        }

        [Test]
        public virtual void TestNoWaitClose()
        {
            RAMDirectory directory = new MockRAMDirectory();

            Document doc = new Document();
            Field idField = new Field("id", "", Field.Store.YES, Field.Index.NOT_ANALYZED);
            doc.Add(idField);

            IndexWriter writer = new IndexWriter(directory, ANALYZER, true, IndexWriter.MaxFieldLength.UNLIMITED);

            for (int iter = 0; iter < 10; iter++)
            {
                ConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
                writer.SetMergeScheduler(cms);
                writer.SetMaxBufferedDocs(2);
                writer.MergeFactor = 100;

                for (int j = 0; j < 201; j++)
                {
                    idField.SetValue(System.Convert.ToString(iter*201 + j));
                    writer.AddDocument(doc);
                }

                int delID = iter*201;
                for (int j = 0; j < 20; j++)
                {
                    writer.DeleteDocuments(new Term("id", delID.ToString()));
                    delID += 5;
                }

                // Force a bunch of merge threads to kick off so we
                // stress out aborting them on close:
                writer.MergeFactor = 3;
                writer.AddDocument(doc);
                writer.Commit();

                writer.Close(false);

                IndexReader reader = IndexReader.Open(directory, true);
                Assert.AreEqual((1 + iter)*182, reader.NumDocs());
                reader.Close();

                // Reopen
                writer = new IndexWriter(directory, ANALYZER, false, IndexWriter.MaxFieldLength.UNLIMITED);
            }
            writer.Close();

            directory.Close();
        }
	}
}