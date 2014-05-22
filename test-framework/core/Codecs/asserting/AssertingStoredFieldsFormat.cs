using System.Diagnostics;

namespace Lucene.Net.Codecs.asserting
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

	using Lucene41StoredFieldsFormat = Lucene.Net.Codecs.Lucene41.Lucene41StoredFieldsFormat;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using IndexableField = Lucene.Net.Index.IndexableField;
	using SegmentInfo = Lucene.Net.Index.SegmentInfo;
	using StoredFieldVisitor = Lucene.Net.Index.StoredFieldVisitor;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;

	/// <summary>
	/// Just like <seealso cref="Lucene41StoredFieldsFormat"/> but with additional asserts.
	/// </summary>
	public class AssertingStoredFieldsFormat : StoredFieldsFormat
	{
	  private readonly StoredFieldsFormat @in = new Lucene41StoredFieldsFormat();

	  public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo si, FieldInfos fn, IOContext context)
	  {
		return new AssertingStoredFieldsReader(@in.fieldsReader(directory, si, fn, context), si.DocCount);
	  }

	  public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
	  {
		return new AssertingStoredFieldsWriter(@in.fieldsWriter(directory, si, context));
	  }

	  internal class AssertingStoredFieldsReader : StoredFieldsReader
	  {
		internal readonly StoredFieldsReader @in;
		internal readonly int MaxDoc;

		internal AssertingStoredFieldsReader(StoredFieldsReader @in, int maxDoc)
		{
		  this.@in = @in;
		  this.MaxDoc = maxDoc;
		}

		public override void Close()
		{
		  @in.close();
		}

		public override void VisitDocument(int n, StoredFieldVisitor visitor)
		{
		  Debug.Assert(n >= 0 && n < MaxDoc);
		  @in.visitDocument(n, visitor);
		}

		public override StoredFieldsReader Clone()
		{
		  return new AssertingStoredFieldsReader(@in.clone(), MaxDoc);
		}

		public override long RamBytesUsed()
		{
		  return @in.ramBytesUsed();
		}

		public override void CheckIntegrity()
		{
		  @in.checkIntegrity();
		}
	  }

	  internal enum Status
	  {
		UNDEFINED,
		STARTED,
		FINISHED
	  }

	  internal class AssertingStoredFieldsWriter : StoredFieldsWriter
	  {
		internal readonly StoredFieldsWriter @in;
		internal int NumWritten;
		internal int FieldCount;
		internal Status DocStatus;

		internal AssertingStoredFieldsWriter(StoredFieldsWriter @in)
		{
		  this.@in = @in;
		  this.DocStatus = Status.UNDEFINED;
		}

		public override void StartDocument(int numStoredFields)
		{
		  Debug.Assert(DocStatus != Status.STARTED);
		  @in.startDocument(numStoredFields);
		  Debug.Assert(FieldCount == 0);
		  FieldCount = numStoredFields;
		  NumWritten++;
		  DocStatus = Status.STARTED;
		}

		public override void FinishDocument()
		{
		  Debug.Assert(DocStatus == Status.STARTED);
		  Debug.Assert(FieldCount == 0);
		  @in.finishDocument();
		  DocStatus = Status.FINISHED;
		}

		public override void WriteField(FieldInfo info, IndexableField field)
		{
		  Debug.Assert(DocStatus == Status.STARTED);
		  @in.writeField(info, field);
		  Debug.Assert(FieldCount > 0);
		  FieldCount--;
		}

		public override void Abort()
		{
		  @in.abort();
		}

		public override void Finish(FieldInfos fis, int numDocs)
		{
		  Debug.Assert(DocStatus == (numDocs > 0 ? Status.FINISHED, Status.UNDEFINED));
		  @in.finish(fis, numDocs);
		  Debug.Assert(FieldCount == 0);
		  Debug.Assert(numDocs == NumWritten);
		}

		public override void Close()
		{
		  @in.close();
		}
	  }
	}

}