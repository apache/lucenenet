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

using Document = Lucene.Net.Documents.Document;
using Fieldable = Lucene.Net.Documents.Fieldable;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;

namespace Lucene.Net.Index
{
	
	sealed class FieldsWriter
	{
		internal const byte FIELD_IS_TOKENIZED = (byte) (0x1);
		internal const byte FIELD_IS_BINARY = (byte) (0x2);
		internal const byte FIELD_IS_COMPRESSED = (byte) (0x4);
		
		private FieldInfos fieldInfos;
		
		private IndexOutput fieldsStream;
		
		private IndexOutput indexStream;
		
		private bool doClose;
		
		internal FieldsWriter(Directory d, System.String segment, FieldInfos fn)
		{
			fieldInfos = fn;
			fieldsStream = d.CreateOutput(segment + ".fdt");
			indexStream = d.CreateOutput(segment + ".fdx");
			doClose = true;
		}
		
		internal FieldsWriter(IndexOutput fdx, IndexOutput fdt, FieldInfos fn)
		{
			fieldInfos = fn;
			fieldsStream = fdt;
			indexStream = fdx;
			doClose = false;
		}
		
		// Writes the contents of buffer into the fields stream
		// and adds a new entry for this document into the index
		// stream.  This assumes the buffer was already written
		// in the correct fields format.
		internal void  FlushDocument(int numStoredFields, RAMOutputStream buffer)
		{
			indexStream.WriteLong(fieldsStream.GetFilePointer());
			fieldsStream.WriteVInt(numStoredFields);
			buffer.WriteTo(fieldsStream);
		}
		
		internal void  Flush()
		{
			indexStream.Flush();
			fieldsStream.Flush();
		}
		
		internal void  Close()
		{
			if (doClose)
			{
				fieldsStream.Close();
				indexStream.Close();
			}
		}
		
		internal void  WriteField(FieldInfo fi, Fieldable field)
		{
			// if the field as an instanceof FieldsReader.FieldForMerge, we're in merge mode
			// and field.binaryValue() already returns the compressed value for a field
			// with isCompressed()==true, so we disable compression in that case
			bool disableCompression = (field is FieldsReader.FieldForMerge);
			fieldsStream.WriteVInt(fi.number);
			byte bits = 0;
			if (field.IsTokenized())
				bits |= FieldsWriter.FIELD_IS_TOKENIZED;
			if (field.IsBinary())
				bits |= FieldsWriter.FIELD_IS_BINARY;
			if (field.IsCompressed())
				bits |= FieldsWriter.FIELD_IS_COMPRESSED;
			
			fieldsStream.WriteByte(bits);
			
			if (field.IsCompressed())
			{
				// compression is enabled for the current field
				byte[] data = null;
				
				if (disableCompression)
				{
					// optimized case for merging, the data
					// is already compressed
					data = field.BinaryValue();
				}
				else
				{
					// check if it is a binary field
					if (field.IsBinary())
					{
						data = Compress(field.BinaryValue());
					}
					else
					{
						data = Compress(System.Text.Encoding.GetEncoding("UTF-8").GetBytes(field.StringValue()));
					}
				}
				int len = data.Length;
				fieldsStream.WriteVInt(len);
				fieldsStream.WriteBytes(data, len);
			}
			else
			{
				// compression is disabled for the current field
				if (field.IsBinary())
				{
					byte[] data = field.BinaryValue();
					int len = data.Length;
					fieldsStream.WriteVInt(len);
					fieldsStream.WriteBytes(data, len);
				}
				else
				{
					fieldsStream.WriteString(field.StringValue());
				}
			}
		}
		
		/// <summary>Bulk write a contiguous series of documents.  The
		/// lengths array is the length (in bytes) of each raw
		/// document.  The stream IndexInput is the
		/// fieldsStream from which we should bulk-copy all
		/// bytes. 
		/// </summary>
		internal void  AddRawDocuments(IndexInput stream, int[] lengths, int numDocs)
		{
			long position = fieldsStream.GetFilePointer();
			long start = position;
			for (int i = 0; i < numDocs; i++)
			{
				indexStream.WriteLong(position);
				position += lengths[i];
			}
			fieldsStream.CopyBytes(stream, position - start);
			System.Diagnostics.Debug.Assert(fieldsStream.GetFilePointer() == position);
		}
		
		internal void  AddDocument(Document doc)
		{
			indexStream.WriteLong(fieldsStream.GetFilePointer());
			
			int storedCount = 0;
			System.Collections.IEnumerator fieldIterator = doc.GetFields().GetEnumerator();
			while (fieldIterator.MoveNext())
			{
				Fieldable field = (Fieldable) fieldIterator.Current;
				if (field.IsStored())
					storedCount++;
			}
			fieldsStream.WriteVInt(storedCount);
			
			fieldIterator = doc.GetFields().GetEnumerator();
			while (fieldIterator.MoveNext())
			{
				Fieldable field = (Fieldable) fieldIterator.Current;
				if (field.IsStored())
					WriteField(fieldInfos.FieldInfo(field.Name()), field);
			}
		}
		
		private byte[] Compress(byte[] input)
		{
			return SupportClass.CompressionSupport.Compress(input);
        }
	}
}