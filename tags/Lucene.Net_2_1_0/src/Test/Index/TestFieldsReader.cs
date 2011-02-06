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

using NUnit.Framework;

using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Lucene.Net.Documents;
using Similarity = Lucene.Net.Search.Similarity;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using _TestUtil = Lucene.Net.Util._TestUtil;

namespace Lucene.Net.Index
{
	
	[TestFixture]
	public class TestFieldsReader
	{
        private class AnonymousClassFieldSelector : FieldSelector
        {
            public AnonymousClassFieldSelector(TestFieldsReader enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }
            private void  InitBlock(TestFieldsReader enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private TestFieldsReader enclosingInstance;
            public TestFieldsReader Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
				
            }
            public virtual FieldSelectorResult Accept(System.String fieldName)
            {
                if (fieldName.Equals(DocHelper.TEXT_FIELD_1_KEY) || fieldName.Equals(DocHelper.COMPRESSED_TEXT_FIELD_2_KEY) || fieldName.Equals(DocHelper.LAZY_FIELD_BINARY_KEY))
                    return FieldSelectorResult.SIZE;
                else if (fieldName.Equals(DocHelper.TEXT_FIELD_3_KEY))
                    return FieldSelectorResult.LOAD;
                else
                    return FieldSelectorResult.NO_LOAD;
            }
        }
        private RAMDirectory dir = new RAMDirectory();
		private Lucene.Net.Documents.Document testDoc = new Lucene.Net.Documents.Document();
		private FieldInfos fieldInfos = null;
		
        // public TestFieldsReader(System.String s)
        // {
        // }
		
        [SetUp]
        public virtual void  SetUp()
		{
			fieldInfos = new FieldInfos();
			DocHelper.SetupDoc(testDoc);
			fieldInfos.Add(testDoc);
			DocumentWriter writer = new DocumentWriter(dir, new WhitespaceAnalyzer(), Similarity.GetDefault(), 50);
			Assert.IsTrue(writer != null);
			writer.AddDocument("test", testDoc);
		}
		
		[Test]
        public virtual void  Test()
		{
			Assert.IsTrue(dir != null);
			Assert.IsTrue(fieldInfos != null);
			FieldsReader reader = new FieldsReader(dir, "test", fieldInfos);
			Assert.IsTrue(reader != null);
			Assert.IsTrue(reader.Size() == 1);
			Lucene.Net.Documents.Document doc = reader.Doc(0, null);
			Assert.IsTrue(doc != null);
			Assert.IsTrue(doc.GetField(DocHelper.TEXT_FIELD_1_KEY) != null);
			
			Field field = doc.GetField(DocHelper.TEXT_FIELD_2_KEY);
			Assert.IsTrue(field != null);
			Assert.IsTrue(field.IsTermVectorStored() == true);
			
			Assert.IsTrue(field.IsStoreOffsetWithTermVector() == true);
			Assert.IsTrue(field.IsStorePositionWithTermVector() == true);
			Assert.IsTrue(field.GetOmitNorms() == false);
			
			field = doc.GetField(DocHelper.TEXT_FIELD_3_KEY);
			Assert.IsTrue(field != null);
			Assert.IsTrue(field.IsTermVectorStored() == false);
			Assert.IsTrue(field.IsStoreOffsetWithTermVector() == false);
			Assert.IsTrue(field.IsStorePositionWithTermVector() == false);
			Assert.IsTrue(field.GetOmitNorms() == true);
			
			
			reader.Close();
		}
		
		[Test]
        public virtual void  TestLazyFields()
        {
            Assert.IsTrue(dir != null);
            Assert.IsTrue(fieldInfos != null);
            FieldsReader reader = new FieldsReader(dir, "test", fieldInfos);
            Assert.IsTrue(reader != null);
            Assert.IsTrue(reader.Size() == 1);
            System.Collections.Hashtable loadFieldNames = new System.Collections.Hashtable();
            loadFieldNames.Add(DocHelper.TEXT_FIELD_1_KEY, DocHelper.TEXT_FIELD_1_KEY);
            loadFieldNames.Add(DocHelper.TEXT_FIELD_UTF1_KEY, DocHelper.TEXT_FIELD_UTF1_KEY);
            System.Collections.Hashtable lazyFieldNames = new System.Collections.Hashtable();
            //new String[]{DocHelper.LARGE_LAZY_FIELD_KEY, DocHelper.LAZY_FIELD_KEY, DocHelper.LAZY_FIELD_BINARY_KEY};
            lazyFieldNames.Add(DocHelper.LARGE_LAZY_FIELD_KEY, DocHelper.LARGE_LAZY_FIELD_KEY);
            lazyFieldNames.Add(DocHelper.LAZY_FIELD_KEY, DocHelper.LAZY_FIELD_KEY);
            lazyFieldNames.Add(DocHelper.LAZY_FIELD_BINARY_KEY, DocHelper.LAZY_FIELD_BINARY_KEY);
            lazyFieldNames.Add(DocHelper.TEXT_FIELD_UTF2_KEY, DocHelper.TEXT_FIELD_UTF2_KEY);
            lazyFieldNames.Add(DocHelper.COMPRESSED_TEXT_FIELD_2_KEY, DocHelper.COMPRESSED_TEXT_FIELD_2_KEY);
            SetBasedFieldSelector fieldSelector = new SetBasedFieldSelector(loadFieldNames, lazyFieldNames);
            Lucene.Net.Documents.Document doc = reader.Doc(0, fieldSelector);
            Assert.IsTrue(doc != null, "doc is null and it shouldn't be");
            Fieldable field = doc.GetFieldable(DocHelper.LAZY_FIELD_KEY);
            Assert.IsTrue(field != null, "field is null and it shouldn't be");
            Assert.IsTrue(field.IsLazy(), "field is not lazy and it should be");
            System.String value_Renamed = field.StringValue();
            Assert.IsTrue(value_Renamed != null, "value is null and it shouldn't be");
            Assert.IsTrue(value_Renamed.Equals(DocHelper.LAZY_FIELD_TEXT) == true, value_Renamed + " is not equal to " + DocHelper.LAZY_FIELD_TEXT);
            field = doc.GetFieldable(DocHelper.COMPRESSED_TEXT_FIELD_2_KEY);
            Assert.IsTrue(field != null, "field is null and it shouldn't be");
            Assert.IsTrue(field.IsLazy(), "field is not lazy and it should be");
            value_Renamed = field.StringValue();
            Assert.IsTrue(value_Renamed != null, "value is null and it shouldn't be");
            Assert.IsTrue(value_Renamed.Equals(DocHelper.FIELD_2_COMPRESSED_TEXT) == true, value_Renamed + " is not equal to " + DocHelper.FIELD_2_COMPRESSED_TEXT);
            field = doc.GetFieldable(DocHelper.TEXT_FIELD_1_KEY);
            Assert.IsTrue(field != null, "field is null and it shouldn't be");
            Assert.IsTrue(field.IsLazy() == false, "Field is lazy and it should not be");
            field = doc.GetFieldable(DocHelper.TEXT_FIELD_UTF1_KEY);
            Assert.IsTrue(field != null, "field is null and it shouldn't be");
            Assert.IsTrue(field.IsLazy() == false, "Field is lazy and it should not be");
            Assert.IsTrue(field.StringValue().Equals(DocHelper.FIELD_UTF1_TEXT) == true, field.StringValue() + " is not equal to " + DocHelper.FIELD_UTF1_TEXT);
			
            field = doc.GetFieldable(DocHelper.TEXT_FIELD_UTF2_KEY);
            Assert.IsTrue(field != null, "field is null and it shouldn't be");
            Assert.IsTrue(field.IsLazy() == true, "Field is lazy and it should not be");
            Assert.IsTrue(field.StringValue().Equals(DocHelper.FIELD_UTF2_TEXT) == true, field.StringValue() + " is not equal to " + DocHelper.FIELD_UTF2_TEXT);
			
            field = doc.GetFieldable(DocHelper.LAZY_FIELD_BINARY_KEY);
            Assert.IsTrue(field != null, "field is null and it shouldn't be");
            byte[] bytes = field.BinaryValue();
            Assert.IsTrue(bytes != null, "bytes is null and it shouldn't be");
            Assert.IsTrue(DocHelper.LAZY_FIELD_BINARY_BYTES.Length == bytes.Length, "");
            for (int i = 0; i < bytes.Length; i++)
            {
                Assert.IsTrue(bytes[i] == DocHelper.LAZY_FIELD_BINARY_BYTES[i], "byte[" + i + "] is mismatched");
            }
        }
		
        [Test]
        public virtual void  TestLoadFirst()
        {
            Assert.IsTrue(dir != null);
            Assert.IsTrue(fieldInfos != null);
            FieldsReader reader = new FieldsReader(dir, "test", fieldInfos);
            Assert.IsTrue(reader != null);
            Assert.IsTrue(reader.Size() == 1);
            LoadFirstFieldSelector fieldSelector = new LoadFirstFieldSelector();
            Lucene.Net.Documents.Document doc = reader.Doc(0, fieldSelector);
            Assert.IsTrue(doc != null, "doc is null and it shouldn't be");
            int count = 0;
            System.Collections.IList l = doc.GetFields();
            for (System.Collections.IEnumerator iter = l.GetEnumerator(); iter.MoveNext(); )
            {
                Field field = (Field) iter.Current;
                Assert.IsTrue(field != null, "field is null and it shouldn't be");
                System.String sv = field.StringValue();
                Assert.IsTrue(sv != null, "sv is null and it shouldn't be");
                count++;
            }
            Assert.IsTrue(count == 1, count + " does not equal: " + 1);
        }
		
        /// <summary> Not really a test per se, but we should have some way of assessing whether this is worthwhile.
        /// <p/>
        /// Must test using a File based directory
        /// 
        /// </summary>
        /// <throws>  Exception </throws>
        [Test]
        public virtual void  TestLazyPerformance()
        {
            System.String tmpIODir = SupportClass.AppSettings.Get("tempDir", "");
            System.String userName = System.Environment.UserName;
            System.String path = tmpIODir + System.IO.Path.DirectorySeparatorChar.ToString() + "lazyDir" + userName;
            System.IO.FileInfo file = new System.IO.FileInfo(path);
            _TestUtil.RmDir(file);
            FSDirectory tmpDir = FSDirectory.GetDirectory(file);
            Assert.IsTrue(tmpDir != null);
            DocumentWriter writer = new DocumentWriter(tmpDir, new WhitespaceAnalyzer(), Similarity.GetDefault(), 50);
            Assert.IsTrue(writer != null);
            writer.AddDocument("test", testDoc);
            Assert.IsTrue(fieldInfos != null);
            FieldsReader reader;
            long lazyTime = 0;
            long regularTime = 0;
            int length = 50;
            System.Collections.Hashtable lazyFieldNames = new System.Collections.Hashtable();
            lazyFieldNames.Add(DocHelper.LARGE_LAZY_FIELD_KEY, DocHelper.LARGE_LAZY_FIELD_KEY);
            SetBasedFieldSelector fieldSelector = new SetBasedFieldSelector(new System.Collections.Hashtable(), lazyFieldNames);
			
            for (int i = 0; i < length; i++)
            {
                reader = new FieldsReader(tmpDir, "test", fieldInfos);
                Assert.IsTrue(reader != null);
                Assert.IsTrue(reader.Size() == 1);
				
                Lucene.Net.Documents.Document doc;
                doc = reader.Doc(0, null); //Load all of them
                Assert.IsTrue(doc != null, "doc is null and it shouldn't be");
                Fieldable field = doc.GetFieldable(DocHelper.LARGE_LAZY_FIELD_KEY);
                Assert.IsTrue(field.IsLazy() == false, "field is lazy");
                System.String value_Renamed;
                long start;
                long finish;
                start = System.DateTime.Now.Millisecond;
                //On my machine this was always 0ms.
                value_Renamed = field.StringValue();
                finish = System.DateTime.Now.Millisecond;
                Assert.IsTrue(value_Renamed != null, "value is null and it shouldn't be");
                Assert.IsTrue(field != null, "field is null and it shouldn't be");
                regularTime += (finish - start);
                reader.Close();
                reader = null;
                doc = null;
                //Hmmm, are we still in cache???
                System.GC.Collect();
                reader = new FieldsReader(tmpDir, "test", fieldInfos);
                doc = reader.Doc(0, fieldSelector);
                field = doc.GetFieldable(DocHelper.LARGE_LAZY_FIELD_KEY);
                Assert.IsTrue(field.IsLazy() == true, "field is not lazy");
                start = System.DateTime.Now.Millisecond;
                //On my machine this took around 50 - 70ms
                value_Renamed = field.StringValue();
                finish = System.DateTime.Now.Millisecond;
                Assert.IsTrue(value_Renamed != null, "value is null and it shouldn't be");
                lazyTime += (finish - start);
                reader.Close();
            }
            System.Console.Out.WriteLine("Average Non-lazy time (should be very close to zero): " + regularTime / length + " ms for " + length + " reads");
            System.Console.Out.WriteLine("Average Lazy Time (should be greater than zero): " + lazyTime / length + " ms for " + length + " reads");
        }
		
        [Test]
        public virtual void  TestLoadSize()
        {
            FieldsReader reader = new FieldsReader(dir, "test", fieldInfos);
            Lucene.Net.Documents.Document doc;
			
            doc = reader.Doc(0, new AnonymousClassFieldSelector(this));
            Fieldable f1 = doc.GetFieldable(DocHelper.TEXT_FIELD_1_KEY);
            Fieldable f3 = doc.GetFieldable(DocHelper.TEXT_FIELD_3_KEY);
            Fieldable fb = doc.GetFieldable(DocHelper.LAZY_FIELD_BINARY_KEY);
            Assert.IsTrue(f1.IsBinary());
            Assert.IsTrue(!f3.IsBinary());
            Assert.IsTrue(fb.IsBinary());
            AssertSizeEquals(2 * DocHelper.FIELD_1_TEXT.Length, f1.BinaryValue());
            Assert.AreEqual(DocHelper.FIELD_3_TEXT, f3.StringValue());
            AssertSizeEquals(DocHelper.LAZY_FIELD_BINARY_BYTES.Length, fb.BinaryValue());
			
            reader.Close();
        }
		
        private void  AssertSizeEquals(int size, byte[] sizebytes)
        {
            Assert.AreEqual((byte) (size >> 24), sizebytes[0]);
            Assert.AreEqual((byte) (size >> 16), sizebytes[1]);
            Assert.AreEqual((byte) (size >> 8), sizebytes[2]);
            Assert.AreEqual((byte) size, sizebytes[3]);
        }
    }
}