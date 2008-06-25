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

using Directory = Lucene.Net.Store.Directory;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using StringHelper = Lucene.Net.Util.StringHelper;

namespace Lucene.Net.Index
{
	
	public sealed class TermVectorsWriter
	{
		
		private IndexOutput tvx = null, tvd = null, tvf = null;
		private FieldInfos fieldInfos;
		
		public TermVectorsWriter(Directory directory, System.String segment, FieldInfos fieldInfos)
		{
			// Open files for TermVector storage
			tvx = directory.CreateOutput(segment + "." + IndexFileNames.VECTORS_INDEX_EXTENSION);
			tvx.WriteInt(TermVectorsReader.FORMAT_VERSION);
			tvd = directory.CreateOutput(segment + "." + IndexFileNames.VECTORS_DOCUMENTS_EXTENSION);
			tvd.WriteInt(TermVectorsReader.FORMAT_VERSION);
			tvf = directory.CreateOutput(segment + "." + IndexFileNames.VECTORS_FIELDS_EXTENSION);
			tvf.WriteInt(TermVectorsReader.FORMAT_VERSION);
			
			this.fieldInfos = fieldInfos;
		}
		
		/// <summary> Add a complete document specified by all its term vectors. If document has no
		/// term vectors, add value for tvx.
		/// 
		/// </summary>
		/// <param name="vectors">
		/// </param>
		/// <throws>  IOException </throws>
		public void  AddAllDocVectors(TermFreqVector[] vectors)
		{
			
			tvx.WriteLong(tvd.GetFilePointer());
			
			if (vectors != null)
			{
				int numFields = vectors.Length;
				tvd.WriteVInt(numFields);
				
				long[] fieldPointers = new long[numFields];
				
				for (int i = 0; i < numFields; i++)
				{
					fieldPointers[i] = tvf.GetFilePointer();
					
					int fieldNumber = fieldInfos.FieldNumber(vectors[i].GetField());
					
					// 1st pass: write field numbers to tvd
					tvd.WriteVInt(fieldNumber);
					
					int numTerms = vectors[i].Size();
					tvf.WriteVInt(numTerms);
					
					TermPositionVector tpVector;
					
					byte bits;
					bool storePositions;
					bool storeOffsets;
					
					if (vectors[i] is TermPositionVector)
					{
						// May have positions & offsets
						tpVector = (TermPositionVector) vectors[i];
						storePositions = tpVector.Size() > 0 && tpVector.GetTermPositions(0) != null;
						storeOffsets = tpVector.Size() > 0 && tpVector.GetOffsets(0) != null;
						bits = (byte) ((storePositions ? TermVectorsReader.STORE_POSITIONS_WITH_TERMVECTOR : (byte) 0) + (storeOffsets ? TermVectorsReader.STORE_OFFSET_WITH_TERMVECTOR : (byte) 0));
					}
					else
					{
						tpVector = null;
						bits = 0;
						storePositions = false;
						storeOffsets = false;
					}
					
					tvf.WriteVInt(bits);
					
					System.String[] terms = vectors[i].GetTerms();
					int[] freqs = vectors[i].GetTermFrequencies();
					
					System.String lastTermText = "";
					for (int j = 0; j < numTerms; j++)
					{
						System.String termText = terms[j];
						int start = StringHelper.StringDifference(lastTermText, termText);
						int length = termText.Length - start;
						tvf.WriteVInt(start); // write shared prefix length
						tvf.WriteVInt(length); // write delta length
						tvf.WriteChars(termText, start, length); // write delta chars
						lastTermText = termText;
						
						int termFreq = freqs[j];
						
						tvf.WriteVInt(termFreq);
						
						if (storePositions)
						{
							int[] positions = tpVector.GetTermPositions(j);
							if (positions == null)
								throw new System.SystemException("Trying to write positions that are null!");
							System.Diagnostics.Debug.Assert(positions.Length == termFreq);
							
							// use delta encoding for positions
							int lastPosition = 0;
							for (int k = 0; k < positions.Length; k++)
							{
								int position = positions[k];
								tvf.WriteVInt(position - lastPosition);
								lastPosition = position;
							}
						}
						
						if (storeOffsets)
						{
							TermVectorOffsetInfo[] offsets = tpVector.GetOffsets(j);
							if (offsets == null)
								throw new System.SystemException("Trying to write offsets that are null!");
							System.Diagnostics.Debug.Assert(offsets.Length == termFreq);
							
							// use delta encoding for offsets
							int lastEndOffset = 0;
							for (int k = 0; k < offsets.Length; k++)
							{
								int startOffset = offsets[k].GetStartOffset();
								int endOffset = offsets[k].GetEndOffset();
								tvf.WriteVInt(startOffset - lastEndOffset);
								tvf.WriteVInt(endOffset - startOffset);
								lastEndOffset = endOffset;
							}
						}
					}
				}
				
				// 2nd pass: write field pointers to tvd
				long lastFieldPointer = 0;
				for (int i = 0; i < numFields; i++)
				{
					long fieldPointer = fieldPointers[i];
					tvd.WriteVLong(fieldPointer - lastFieldPointer);
					lastFieldPointer = fieldPointer;
				}
			}
			else
				tvd.WriteVInt(0);
		}
		
		/// <summary>Close all streams. </summary>
		internal void  Close()
		{
			// make an effort to close all streams we can but remember and re-throw
			// the first exception encountered in this process
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
	}
}