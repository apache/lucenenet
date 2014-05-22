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
	using FieldInfosReader = Lucene.Net.Codecs.FieldInfosReader;
	using FieldInfosWriter = Lucene.Net.Codecs.FieldInfosWriter;
	using Document = Lucene.Net.Document.Document;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	//import org.cnlp.utils.properties.ResourceBundleHelper;

	public class TestFieldInfos : LuceneTestCase
	{

	  private Document TestDoc = new Document();

	  public override void SetUp()
	  {
		base.setUp();
		DocHelper.setupDoc(TestDoc);
	  }

	  public virtual FieldInfos CreateAndWriteFieldInfos(Directory dir, string filename)
	  {
	  //Positive test of FieldInfos
		Assert.IsTrue(TestDoc != null);
		FieldInfos.Builder builder = new FieldInfos.Builder();
		foreach (IndexableField field in TestDoc)
		{
		  builder.addOrUpdate(field.name(), field.fieldType());
		}
		FieldInfos fieldInfos = builder.finish();
		//Since the complement is stored as well in the fields map
		Assert.IsTrue(fieldInfos.size() == DocHelper.all.size()); //this is all b/c we are using the no-arg constructor


		IndexOutput output = dir.createOutput(filename, newIOContext(random()));
		Assert.IsTrue(output != null);
		//Use a RAMOutputStream

		FieldInfosWriter writer = Codec.Default.fieldInfosFormat().FieldInfosWriter;
		writer.write(dir, filename, "", fieldInfos, IOContext.DEFAULT);
		output.close();
		return fieldInfos;
	  }

	  public virtual FieldInfos ReadFieldInfos(Directory dir, string filename)
	  {
		FieldInfosReader reader = Codec.Default.fieldInfosFormat().FieldInfosReader;
		return reader.read(dir, filename, "", IOContext.DEFAULT);
	  }

	  public virtual void Test()
	  {
		string name = "testFile";
		Directory dir = newDirectory();
		FieldInfos fieldInfos = CreateAndWriteFieldInfos(dir, name);

		FieldInfos readIn = ReadFieldInfos(dir, name);
		Assert.IsTrue(fieldInfos.size() == readIn.size());
		FieldInfo info = readIn.fieldInfo("textField1");
		Assert.IsTrue(info != null);
		Assert.IsTrue(info.hasVectors() == false);
		Assert.IsTrue(info.omitsNorms() == false);

		info = readIn.fieldInfo("textField2");
		Assert.IsTrue(info != null);
		Assert.IsTrue(info.omitsNorms() == false);

		info = readIn.fieldInfo("textField3");
		Assert.IsTrue(info != null);
		Assert.IsTrue(info.hasVectors() == false);
		Assert.IsTrue(info.omitsNorms() == true);

		info = readIn.fieldInfo("omitNorms");
		Assert.IsTrue(info != null);
		Assert.IsTrue(info.hasVectors() == false);
		Assert.IsTrue(info.omitsNorms() == true);

		dir.close();
	  }

	  public virtual void TestReadOnly()
	  {
		string name = "testFile";
		Directory dir = newDirectory();
		FieldInfos fieldInfos = CreateAndWriteFieldInfos(dir, name);
		FieldInfos readOnly = ReadFieldInfos(dir, name);
		AssertReadOnly(readOnly, fieldInfos);
		dir.close();
	  }

	  private void AssertReadOnly(FieldInfos readOnly, FieldInfos modifiable)
	  {
		Assert.AreEqual(modifiable.size(), readOnly.size());
		// assert we can iterate
		foreach (FieldInfo fi in readOnly)
		{
		  Assert.AreEqual(fi.name, modifiable.fieldInfo(fi.number).name);
		}
	  }
	}

}