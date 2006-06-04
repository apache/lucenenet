/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;

namespace Lucene.Net.Index
{
	
	/// <version>  $Id: TermVectorsReader.java 170226 2005-05-15 15:04:39Z bmesser $
	/// </version>
	public class TermVectorsReader : System.ICloneable
	{
		private FieldInfos fieldInfos;
		
		private IndexInput tvx;
		private IndexInput tvd;
		private IndexInput tvf;
		private int size;
		
		private int tvdFormat;
		private int tvfFormat;
		
		public /*internal*/ TermVectorsReader(Directory d, System.String segment, FieldInfos fieldInfos)
		{
			if (d.FileExists(segment + TermVectorsWriter.TVX_EXTENSION))
			{
				tvx = d.OpenInput(segment + TermVectorsWriter.TVX_EXTENSION);
				CheckValidFormat(tvx);
				tvd = d.OpenInput(segment + TermVectorsWriter.TVD_EXTENSION);
				tvdFormat = CheckValidFormat(tvd);
				tvf = d.OpenInput(segment + TermVectorsWriter.TVF_EXTENSION);
				tvfFormat = CheckValidFormat(tvf);
				size = (int) tvx.Length() / 8;
			}
			
			this.fieldInfos = fieldInfos;
		}
		
		private int CheckValidFormat(IndexInput in_Renamed)
		{
			int format = in_Renamed.ReadInt();
			if (format > TermVectorsWriter.FORMAT_VERSION)
			{
				throw new System.IO.IOException("Incompatible format version: " + format + " expected " + TermVectorsWriter.FORMAT_VERSION + " or less");
			}
			return format;
		}
		
		internal virtual void  Close()
		{
			// make all effort to close up. Keep the first exception
			// and throw it as a new one.
			System.IO.IOException keep = null;
			if (tvx != null)
				try
				{
					tvx.Close();
				}
				catch (System.IO.IOException e)
				{
					if (keep == null)
						keep = e;
				}
			if (tvd != null)
				try
				{
					tvd.Close();
				}
				catch (System.IO.IOException e)
				{
					if (keep == null)
						keep = e;
				}
			if (tvf != null)
				try
				{
					tvf.Close();
				}
				catch (System.IO.IOException e)
				{
					if (keep == null)
						keep = e;
				}
			if (keep != null)
			{
				throw new System.IO.IOException(keep.StackTrace);
			}
		}
		
		/// <summary> </summary>
		/// <returns> The number of documents in the reader
		/// </returns>
		internal virtual int Size()
		{
			return size;
		}
		
		/// <summary> Retrieve the term vector for the given document and field</summary>
		/// <param name="docNum">The document number to retrieve the vector for
		/// </param>
		/// <param name="field">The field within the document to retrieve
		/// </param>
		/// <returns> The TermFreqVector for the document and field or null if there is no termVector for this field.
		/// </returns>
		/// <throws>  IOException if there is an error reading the term vector files </throws>
		public /*internal*/ virtual TermFreqVector Get(int docNum, System.String field)
		{
			// Check if no term vectors are available for this segment at all
			int fieldNumber = fieldInfos.FieldNumber(field);
			TermFreqVector result = null;
			if (tvx != null)
			{
				//We need to account for the FORMAT_SIZE at when seeking in the tvx
				//We don't need to do this in other seeks because we already have the
				// file pointer
				//that was written in another file
				tvx.Seek((docNum * 8L) + TermVectorsWriter.FORMAT_SIZE);
				//System.out.println("TVX Pointer: " + tvx.getFilePointer());
				long position = tvx.ReadLong();
				
				tvd.Seek(position);
				int fieldCount = tvd.ReadVInt();
				//System.out.println("Num Fields: " + fieldCount);
				// There are only a few fields per document. We opt for a full scan
				// rather then requiring that they be ordered. We need to read through
				// all of the fields anyway to get to the tvf pointers.
				int number = 0;
				int found = - 1;
				for (int i = 0; i < fieldCount; i++)
				{
					if (tvdFormat == TermVectorsWriter.FORMAT_VERSION)
						number = tvd.ReadVInt();
					else
						number += tvd.ReadVInt();
					
					if (number == fieldNumber)
						found = i;
				}
				
				// This field, although valid in the segment, was not found in this
				// document
				if (found != - 1)
				{
					// Compute position in the tvf file
					position = 0;
					for (int i = 0; i <= found; i++)
						position += tvd.ReadVLong();
					
					result = ReadTermVector(field, position);
				}
				else
				{
					//System.out.println("Field not found");
				}
			}
			else
			{
				//System.out.println("No tvx file");
			}
			return result;
		}
		
		/// <summary> Return all term vectors stored for this document or null if the could not be read in.
		/// 
		/// </summary>
		/// <param name="docNum">The document number to retrieve the vector for
		/// </param>
		/// <returns> All term frequency vectors
		/// </returns>
		/// <throws>  IOException if there is an error reading the term vector files  </throws>
		public /*internal*/ virtual TermFreqVector[] Get(int docNum)
		{
			TermFreqVector[] result = null;
			// Check if no term vectors are available for this segment at all
			if (tvx != null)
			{
				//We need to offset by
				tvx.Seek((docNum * 8L) + TermVectorsWriter.FORMAT_SIZE);
				long position = tvx.ReadLong();
				
				tvd.Seek(position);
				int fieldCount = tvd.ReadVInt();
				
				// No fields are vectorized for this document
				if (fieldCount != 0)
				{
					int number = 0;
					System.String[] fields = new System.String[fieldCount];
					
					for (int i = 0; i < fieldCount; i++)
					{
						if (tvdFormat == TermVectorsWriter.FORMAT_VERSION)
							number = tvd.ReadVInt();
						else
							number += tvd.ReadVInt();
						
						fields[i] = fieldInfos.FieldName(number);
					}
					
					// Compute position in the tvf file
					position = 0;
					long[] tvfPointers = new long[fieldCount];
					for (int i = 0; i < fieldCount; i++)
					{
						position += tvd.ReadVLong();
						tvfPointers[i] = position;
					}
					
					result = ReadTermVectors(fields, tvfPointers);
				}
			}
			else
			{
				//System.out.println("No tvx file");
			}
			return result;
		}
		
		
		private SegmentTermVector[] ReadTermVectors(System.String[] fields, long[] tvfPointers)
		{
			SegmentTermVector[] res = new SegmentTermVector[fields.Length];
			for (int i = 0; i < fields.Length; i++)
			{
				res[i] = ReadTermVector(fields[i], tvfPointers[i]);
			}
			return res;
		}
		
		/// <summary> </summary>
		/// <param name="field">The field to read in
		/// </param>
		/// <param name="tvfPointer">The pointer within the tvf file where we should start reading
		/// </param>
		/// <returns> The TermVector located at that position
		/// </returns>
		/// <throws>  IOException </throws>
		private SegmentTermVector ReadTermVector(System.String field, long tvfPointer)
		{
			
			// Now read the data from specified position
			//We don't need to offset by the FORMAT here since the pointer already includes the offset
			tvf.Seek(tvfPointer);
			
			int numTerms = tvf.ReadVInt();
			//System.out.println("Num Terms: " + numTerms);
			// If no terms - return a constant empty termvector. However, this should never occur!
			if (numTerms == 0)
				return new SegmentTermVector(field, null, null);
			
			bool storePositions;
			bool storeOffsets;
			
			if (tvfFormat == TermVectorsWriter.FORMAT_VERSION)
			{
				byte bits = tvf.ReadByte();
				storePositions = (bits & TermVectorsWriter.STORE_POSITIONS_WITH_TERMVECTOR) != 0;
				storeOffsets = (bits & TermVectorsWriter.STORE_OFFSET_WITH_TERMVECTOR) != 0;
			}
			else
			{
				tvf.ReadVInt();
				storePositions = false;
				storeOffsets = false;
			}
			
			System.String[] terms = new System.String[numTerms];
			int[] termFreqs = new int[numTerms];
			
			//  we may not need these, but declare them
			int[][] positions = null;
			TermVectorOffsetInfo[][] offsets = null;
			if (storePositions)
				positions = new int[numTerms][];
			if (storeOffsets)
				offsets = new TermVectorOffsetInfo[numTerms][];
			
			int start = 0;
			int deltaLength = 0;
			int totalLength = 0;
			char[] buffer = new char[10]; // init the buffer with a length of 10 character
			char[] previousBuffer = new char[]{};
			
			for (int i = 0; i < numTerms; i++)
			{
				start = tvf.ReadVInt();
				deltaLength = tvf.ReadVInt();
				totalLength = start + deltaLength;
				if (buffer.Length < totalLength)
				{
					// increase buffer
					buffer = null; // give a hint to garbage collector
					buffer = new char[totalLength];
					
					if (start > 0)
				    	// just copy if necessary
						Array.Copy(previousBuffer, 0, buffer, 0, start);
				}
				
				tvf.ReadChars(buffer, start, deltaLength);
				terms[i] = new System.String(buffer, 0, totalLength);
				previousBuffer = buffer;
				int freq = tvf.ReadVInt();
				termFreqs[i] = freq;
				
				if (storePositions)
				{
					//read in the positions
					int[] pos = new int[freq];
					positions[i] = pos;
					int prevPosition = 0;
					for (int j = 0; j < freq; j++)
					{
						pos[j] = prevPosition + tvf.ReadVInt();
						prevPosition = pos[j];
					}
				}
				
				if (storeOffsets)
				{
					TermVectorOffsetInfo[] offs = new TermVectorOffsetInfo[freq];
					offsets[i] = offs;
					int prevOffset = 0;
					for (int j = 0; j < freq; j++)
					{
						int startOffset = prevOffset + tvf.ReadVInt();
						int endOffset = startOffset + tvf.ReadVInt();
						offs[j] = new TermVectorOffsetInfo(startOffset, endOffset);
						prevOffset = endOffset;
					}
				}
			}
			
			SegmentTermVector tv;
			if (storePositions || storeOffsets)
			{
				tv = new SegmentTermPositionVector(field, terms, termFreqs, positions, offsets);
			}
			else
			{
				tv = new SegmentTermVector(field, terms, termFreqs);
			}
			return tv;
		}
		
		public virtual System.Object Clone()
		{
			
			if (tvx == null || tvd == null || tvf == null)
				return null;
			
			TermVectorsReader clone = null;
			try
			{
				clone = (TermVectorsReader) base.MemberwiseClone();
			}
			catch (System.Exception)
			{
			}
			
			clone.tvx = (IndexInput) tvx.Clone();
			clone.tvd = (IndexInput) tvd.Clone();
			clone.tvf = (IndexInput) tvf.Clone();
			
			return clone;
		}
	}
}