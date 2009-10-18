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
using IndexOutput = Lucene.Net.Store.IndexOutput;

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
		
		internal FieldsWriter(Directory d, System.String segment, FieldInfos fn)
		{
			fieldInfos = fn;
			fieldsStream = d.CreateOutput(segment + ".fdt");
			indexStream = d.CreateOutput(segment + ".fdx");
		}
		
		internal void  Close()
		{
			fieldsStream.Close();
			indexStream.Close();
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
				// if the field as an instanceof FieldsReader.FieldForMerge, we're in merge mode
				// and field.binaryValue() already returns the compressed value for a field
				// with isCompressed()==true, so we disable compression in that case
				bool disableCompression = (field is FieldsReader.FieldForMerge);
				if (field.IsStored())
				{
					fieldsStream.WriteVInt(fieldInfos.FieldNumber(field.Name()));
					
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
			}
		}
		
		private byte[] Compress(byte[] input)
		{
			return SupportClass.CompressionSupport.Compress(input);
        }
	}
}