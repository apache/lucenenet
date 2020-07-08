using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using FieldInfosReader = Lucene.Net.Codecs.FieldInfosReader;
    using FieldInfosWriter = Lucene.Net.Codecs.FieldInfosWriter;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    public class TestFieldInfos : LuceneTestCase
    {
        private Document testDoc = new Document();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            DocHelper.SetupDoc(testDoc);
        }

        public virtual FieldInfos CreateAndWriteFieldInfos(Directory dir, string filename)
        {
            //Positive test of FieldInfos
            Assert.IsTrue(testDoc != null);
            FieldInfos.Builder builder = new FieldInfos.Builder();
            foreach (IIndexableField field in testDoc)
            {
                builder.AddOrUpdate(field.Name, field.IndexableFieldType);
            }
            FieldInfos fieldInfos = builder.Finish();
            //Since the complement is stored as well in the fields map
            Assert.IsTrue(fieldInfos.Count == DocHelper.All.Count); //this is all b/c we are using the no-arg constructor

            IndexOutput output = dir.CreateOutput(filename, NewIOContext(Random));
            Assert.IsTrue(output != null);
            //Use a RAMOutputStream

            FieldInfosWriter writer = Codec.Default.FieldInfosFormat.FieldInfosWriter;
            writer.Write(dir, filename, "", fieldInfos, IOContext.DEFAULT);
            output.Dispose();
            return fieldInfos;
        }

        public virtual FieldInfos ReadFieldInfos(Directory dir, string filename)
        {
            FieldInfosReader reader = Codec.Default.FieldInfosFormat.FieldInfosReader;
            return reader.Read(dir, filename, "", IOContext.DEFAULT);
        }

        [Test]
        public virtual void Test()
        {
            string name = "testFile";
            Directory dir = NewDirectory();
            FieldInfos fieldInfos = CreateAndWriteFieldInfos(dir, name);

            FieldInfos readIn = ReadFieldInfos(dir, name);
            Assert.IsTrue(fieldInfos.Count == readIn.Count);
            FieldInfo info = readIn.FieldInfo("textField1");
            Assert.IsTrue(info != null);
            Assert.IsTrue(info.HasVectors == false);
            Assert.IsTrue(info.OmitsNorms == false);

            info = readIn.FieldInfo("textField2");
            Assert.IsTrue(info != null);
            Assert.IsTrue(info.OmitsNorms == false);

            info = readIn.FieldInfo("textField3");
            Assert.IsTrue(info != null);
            Assert.IsTrue(info.HasVectors == false);
            Assert.IsTrue(info.OmitsNorms == true);

            info = readIn.FieldInfo("omitNorms");
            Assert.IsTrue(info != null);
            Assert.IsTrue(info.HasVectors == false);
            Assert.IsTrue(info.OmitsNorms == true);

            dir.Dispose();
        }

        [Test]
        public virtual void TestReadOnly()
        {
            string name = "testFile";
            Directory dir = NewDirectory();
            FieldInfos fieldInfos = CreateAndWriteFieldInfos(dir, name);
            FieldInfos readOnly = ReadFieldInfos(dir, name);
            AssertReadOnly(readOnly, fieldInfos);
            dir.Dispose();
        }

        private void AssertReadOnly(FieldInfos readOnly, FieldInfos modifiable)
        {
            Assert.AreEqual(modifiable.Count, readOnly.Count);
            // assert we can iterate
            foreach (FieldInfo fi in readOnly)
            {
                Assert.AreEqual(fi.Name, modifiable.FieldInfo(fi.Number).Name);
            }
        }
    }
}