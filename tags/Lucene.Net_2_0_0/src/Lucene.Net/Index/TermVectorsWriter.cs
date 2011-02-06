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
	
	/// <summary> Writer works by opening a document and then opening the fields within the document and then
	/// writing out the vectors for each field.
	/// 
	/// Rough usage:
	/// 
	/// <CODE>
	/// for each document
	/// {
	/// writer.openDocument();
	/// for each field on the document
	/// {
	/// writer.openField(field);
	/// for all of the terms
	/// {
	/// writer.addTerm(...)
	/// }
	/// writer.closeField
	/// }
	/// writer.closeDocument()    
	/// }
	/// </CODE>
	/// 
	/// </summary>
	/// <version>  $Id: TermVectorsWriter.java 150689 2004-11-29 21:42:02Z bmesser $
	/// 
	/// </version>
	public sealed class TermVectorsWriter
	{
		internal const byte STORE_POSITIONS_WITH_TERMVECTOR = (byte) (0x1);
		internal const byte STORE_OFFSET_WITH_TERMVECTOR = (byte) (0x2);
		
		internal const int FORMAT_VERSION = 2;
		//The size in bytes that the FORMAT_VERSION will take up at the beginning of each file 
		internal const int FORMAT_SIZE = 4;
		
		internal const System.String TVX_EXTENSION = ".tvx";
		internal const System.String TVD_EXTENSION = ".tvd";
		internal const System.String TVF_EXTENSION = ".tvf";
		
		private IndexOutput tvx = null, tvd = null, tvf = null;
		private System.Collections.ArrayList fields = null;
		private System.Collections.ArrayList terms = null;
		private FieldInfos fieldInfos;
		
		private TVField currentField = null;
		private long currentDocPointer = - 1;
		
        public static System.String TvxExtension
        {
            get {   return TVX_EXTENSION;   }
        }
        public static System.String TvdExtension
        {
            get {   return TVD_EXTENSION;   }
        }
        public static System.String TvfExtension
        {
            get {   return TVF_EXTENSION;   }
        }

		public TermVectorsWriter(Directory directory, System.String segment, FieldInfos fieldInfos)
		{
			// Open files for TermVector storage
			tvx = directory.CreateOutput(segment + TVX_EXTENSION);
			tvx.WriteInt(FORMAT_VERSION);
			tvd = directory.CreateOutput(segment + TVD_EXTENSION);
			tvd.WriteInt(FORMAT_VERSION);
			tvf = directory.CreateOutput(segment + TVF_EXTENSION);
			tvf.WriteInt(FORMAT_VERSION);
			
			this.fieldInfos = fieldInfos;
			fields = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(fieldInfos.Size()));
			terms = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
		}
		
		
		public void  OpenDocument()
		{
			CloseDocument();
			currentDocPointer = tvd.GetFilePointer();
		}
		
		
		public void  CloseDocument()
		{
			if (IsDocumentOpen())
			{
				CloseField();
				WriteDoc();
				fields.Clear();
				currentDocPointer = - 1;
			}
		}
		
		
		public bool IsDocumentOpen()
		{
			return currentDocPointer != - 1;
		}
		
		
		/// <summary>Start processing a field. This can be followed by a number of calls to
		/// addTerm, and a final call to closeField to indicate the end of
		/// processing of this field. If a field was previously open, it is
		/// closed automatically.
		/// </summary>
		public void  OpenField(System.String field)
		{
			FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
			OpenField(fieldInfo.number, fieldInfo.storePositionWithTermVector, fieldInfo.storeOffsetWithTermVector);
		}
		
		private void  OpenField(int fieldNumber, bool storePositionWithTermVector, bool storeOffsetWithTermVector)
		{
			if (!IsDocumentOpen())
				throw new System.SystemException("Cannot open field when no document is open.");
			CloseField();
			currentField = new TVField(fieldNumber, storePositionWithTermVector, storeOffsetWithTermVector);
		}
		
		/// <summary>Finished processing current field. This should be followed by a call to
		/// openField before future calls to addTerm.
		/// </summary>
		public void  CloseField()
		{
			if (IsFieldOpen())
			{
				/* DEBUG */
				//System.out.println("closeField()");
				/* DEBUG */
				
				// save field and terms
				WriteField();
				fields.Add(currentField);
				terms.Clear();
				currentField = null;
			}
		}
		
		/// <summary>Return true if a field is currently open. </summary>
		public bool IsFieldOpen()
		{
			return currentField != null;
		}
		
		/// <summary>Add term to the field's term vector. Field must already be open.
		/// Terms should be added in
		/// increasing order of terms, one call per unique termNum. ProxPointer
		/// is a pointer into the TermPosition file (prx). Freq is the number of
		/// times this term appears in this field, in this document.
		/// </summary>
		/// <throws>  IllegalStateException if document or field is not open </throws>
		public void  AddTerm(System.String termText, int freq)
		{
			AddTerm(termText, freq, null, null);
		}
		
		public void  AddTerm(System.String termText, int freq, int[] positions, TermVectorOffsetInfo[] offsets)
		{
			if (!IsDocumentOpen())
				throw new System.SystemException("Cannot add terms when document is not open");
			if (!IsFieldOpen())
				throw new System.SystemException("Cannot add terms when field is not open");
			
			AddTermInternal(termText, freq, positions, offsets);
		}
		
		private void  AddTermInternal(System.String termText, int freq, int[] positions, TermVectorOffsetInfo[] offsets)
		{
			TVTerm term = new TVTerm();
			term.termText = termText;
			term.freq = freq;
			term.positions = positions;
			term.offsets = offsets;
			terms.Add(term);
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
			OpenDocument();
			
			if (vectors != null)
			{
				for (int i = 0; i < vectors.Length; i++)
				{
					bool storePositionWithTermVector = false;
					bool storeOffsetWithTermVector = false;
					
					if (vectors[i] is TermPositionVector)
					{
						
						TermPositionVector tpVector = (TermPositionVector) vectors[i];
						
						if (tpVector.Size() > 0 && tpVector.GetTermPositions(0) != null)
							storePositionWithTermVector = true;
						if (tpVector.Size() > 0 && tpVector.GetOffsets(0) != null)
							storeOffsetWithTermVector = true;
						
						FieldInfo fieldInfo = fieldInfos.FieldInfo(tpVector.GetField());
						OpenField(fieldInfo.number, storePositionWithTermVector, storeOffsetWithTermVector);
						
						for (int j = 0; j < tpVector.Size(); j++)
							AddTermInternal(tpVector.GetTerms()[j], tpVector.GetTermFrequencies()[j], tpVector.GetTermPositions(j), tpVector.GetOffsets(j));
						
						CloseField();
					}
					else
					{
						
						TermFreqVector tfVector = vectors[i];
						
						FieldInfo fieldInfo = fieldInfos.FieldInfo(tfVector.GetField());
						OpenField(fieldInfo.number, storePositionWithTermVector, storeOffsetWithTermVector);
						
						for (int j = 0; j < tfVector.Size(); j++)
							AddTermInternal(tfVector.GetTerms()[j], tfVector.GetTermFrequencies()[j], null, null);
						
						CloseField();
					}
				}
			}
			
			CloseDocument();
		}
		
		/// <summary>Close all streams. </summary>
		public /*internal*/ void  Close()
		{
			try
			{
				CloseDocument();
			}
			finally
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
		
		
		
		private void  WriteField()
		{
			// remember where this field is written
			currentField.tvfPointer = tvf.GetFilePointer();
			//System.out.println("Field Pointer: " + currentField.tvfPointer);
			
			int size = terms.Count;
			tvf.WriteVInt(size);
			
			bool storePositions = currentField.storePositions;
			bool storeOffsets = currentField.storeOffsets;
			byte bits = (byte) (0x0);
			if (storePositions)
				bits |= STORE_POSITIONS_WITH_TERMVECTOR;
			if (storeOffsets)
				bits |= STORE_OFFSET_WITH_TERMVECTOR;
			tvf.WriteByte(bits);
			
			System.String lastTermText = "";
			for (int i = 0; i < size; i++)
			{
				TVTerm term = (TVTerm) terms[i];
				int start = StringHelper.StringDifference(lastTermText, term.termText);
				int length = term.termText.Length - start;
				tvf.WriteVInt(start); // write shared prefix length
				tvf.WriteVInt(length); // write delta length
				tvf.WriteChars(term.termText, start, length); // write delta chars
				tvf.WriteVInt(term.freq);
				lastTermText = term.termText;
				
				if (storePositions)
				{
					if (term.positions == null)
						throw new System.SystemException("Trying to write positions that are null!");
					
					// use delta encoding for positions
					int position = 0;
					for (int j = 0; j < term.freq; j++)
					{
						tvf.WriteVInt(term.positions[j] - position);
						position = term.positions[j];
					}
				}
				
				if (storeOffsets)
				{
					if (term.offsets == null)
						throw new System.SystemException("Trying to write offsets that are null!");
					
					// use delta encoding for offsets
					int position = 0;
					for (int j = 0; j < term.freq; j++)
					{
						tvf.WriteVInt(term.offsets[j].GetStartOffset() - position);
						tvf.WriteVInt(term.offsets[j].GetEndOffset() - term.offsets[j].GetStartOffset()); //Save the diff between the two.
						position = term.offsets[j].GetEndOffset();
					}
				}
			}
		}
		
		private void  WriteDoc()
		{
			if (IsFieldOpen())
				throw new System.SystemException("Field is still open while writing document");
			//System.out.println("Writing doc pointer: " + currentDocPointer);
			// write document index record
			tvx.WriteLong(currentDocPointer);
			
			// write document data record
			int size = fields.Count;
			
			// write the number of fields
			tvd.WriteVInt(size);
			
			// write field numbers
			for (int i = 0; i < size; i++)
			{
				TVField field = (TVField) fields[i];
				tvd.WriteVInt(field.number);
			}
			
			// write field pointers
			long lastFieldPointer = 0;
			for (int i = 0; i < size; i++)
			{
				TVField field = (TVField) fields[i];
				tvd.WriteVLong(field.tvfPointer - lastFieldPointer);
				lastFieldPointer = field.tvfPointer;
			}
			//System.out.println("After writing doc pointer: " + tvx.getFilePointer());
		}
		
		
		private class TVField
		{
			internal int number;
			internal long tvfPointer = 0;
			internal bool storePositions = false;
			internal bool storeOffsets = false;
			internal TVField(int number, bool storePos, bool storeOff)
			{
				this.number = number;
				storePositions = storePos;
				storeOffsets = storeOff;
			}
		}
		
		private class TVTerm
		{
			internal System.String termText;
			internal int freq = 0;
			internal int[] positions = null;
			internal TermVectorOffsetInfo[] offsets = null;
		}
	}
}