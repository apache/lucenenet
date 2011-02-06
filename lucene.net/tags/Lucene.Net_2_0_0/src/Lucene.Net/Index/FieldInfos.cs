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
using Field = Lucene.Net.Documents.Field;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;

namespace Lucene.Net.Index
{
	
	/// <summary>Access to the Field Info file that describes document fields and whether or
	/// not they are indexed. Each segment has a separate Field Info file. Objects
	/// of this class are thread-safe for multiple readers, but only one thread can
	/// be adding documents at a time, with no other reader or writer threads
	/// accessing this object.
	/// </summary>
	public sealed class FieldInfos
	{
		
		internal const byte IS_INDEXED = (byte) (0x1);
		internal const byte STORE_TERMVECTOR = (byte) (0x2);
		internal const byte STORE_POSITIONS_WITH_TERMVECTOR = (byte) (0x4);
		internal const byte STORE_OFFSET_WITH_TERMVECTOR = (byte) (0x8);
		internal const byte OMIT_NORMS = (byte) (0x10);
		
		private System.Collections.ArrayList byNumber = new System.Collections.ArrayList();
		private System.Collections.Hashtable byName = new System.Collections.Hashtable();
		
		public /*internal*/ FieldInfos()
		{
		}
		
		/// <summary> Construct a FieldInfos object using the directory and the name of the file
		/// IndexInput
		/// </summary>
		/// <param name="d">The directory to open the IndexInput from
		/// </param>
		/// <param name="name">The name of the file to open the IndexInput from in the Directory
		/// </param>
		/// <throws>  IOException </throws>
		public /*internal*/ FieldInfos(Directory d, System.String name)
		{
			IndexInput input = d.OpenInput(name);
			try
			{
				Read(input);
			}
			finally
			{
				input.Close();
			}
		}
		
		/// <summary>Adds field info for a Document. </summary>
		public void  Add(Document doc)
		{
            foreach (Field field in doc.Fields())
            {
				Add(field.Name(), field.IsIndexed(), field.IsTermVectorStored(), field.IsStorePositionWithTermVector(), field.IsStoreOffsetWithTermVector(), field.GetOmitNorms());
			}
		}
		
		/// <summary> Add fields that are indexed. Whether they have termvectors has to be specified.
		/// 
		/// </summary>
		/// <param name="names">The names of the fields
		/// </param>
		/// <param name="storeTermVectors">Whether the fields store term vectors or not
		/// </param>
		/// <param name="storePositionWithTermVector">treu if positions should be stored.
		/// </param>
		/// <param name="storeOffsetWithTermVector">true if offsets should be stored
		/// </param>
		public void  AddIndexed(System.Collections.ICollection names, bool storeTermVectors, bool storePositionWithTermVector, bool storeOffsetWithTermVector)
		{
			System.Collections.IEnumerator i = names.GetEnumerator();
			while (i.MoveNext())
			{
				System.Collections.DictionaryEntry t = (System.Collections.DictionaryEntry) i.Current;
				Add((System.String) t.Key, true, storeTermVectors, storePositionWithTermVector, storeOffsetWithTermVector);
			}
		}
		
		/// <summary> Assumes the fields are not storing term vectors.
		/// 
		/// </summary>
		/// <param name="names">The names of the fields
		/// </param>
		/// <param name="isIndexed">Whether the fields are indexed or not
		/// 
		/// </param>
		/// <seealso cref="Add(String, boolean)">
		/// </seealso>
		public void  Add(System.Collections.ICollection names, bool isIndexed)
		{
			System.Collections.IEnumerator i = names.GetEnumerator();
			while (i.MoveNext())
			{
				System.Collections.DictionaryEntry t = (System.Collections.DictionaryEntry) i.Current;
				Add((System.String) t.Key, isIndexed);
			}
		}
		
		/// <summary> Calls 5 parameter add with false for all TermVector parameters.
		/// 
		/// </summary>
		/// <param name="name">The name of the Field
		/// </param>
		/// <param name="isIndexed">true if the field is indexed
		/// </param>
		/// <seealso cref="Add(String, boolean, boolean, boolean, boolean)">
		/// </seealso>
		public void  Add(System.String name, bool isIndexed)
		{
			Add(name, isIndexed, false, false, false, false);
		}
		
		/// <summary> Calls 5 parameter add with false for term vector positions and offsets.
		/// 
		/// </summary>
		/// <param name="name">The name of the field
		/// </param>
		/// <param name="isIndexed"> true if the field is indexed
		/// </param>
		/// <param name="storeTermVector">true if the term vector should be stored
		/// </param>
		public void  Add(System.String name, bool isIndexed, bool storeTermVector)
		{
			Add(name, isIndexed, storeTermVector, false, false, false);
		}
		
		/// <summary>If the field is not yet known, adds it. If it is known, checks to make
		/// sure that the isIndexed flag is the same as was given previously for this
		/// field. If not - marks it as being indexed.  Same goes for the TermVector
		/// parameters.
		/// 
		/// </summary>
		/// <param name="name">The name of the field
		/// </param>
		/// <param name="isIndexed">true if the field is indexed
		/// </param>
		/// <param name="storeTermVector">true if the term vector should be stored
		/// </param>
		/// <param name="storePositionWithTermVector">true if the term vector with positions should be stored
		/// </param>
		/// <param name="storeOffsetWithTermVector">true if the term vector with offsets should be stored
		/// </param>
		public void  Add(System.String name, bool isIndexed, bool storeTermVector, bool storePositionWithTermVector, bool storeOffsetWithTermVector)
		{
			
			Add(name, isIndexed, storeTermVector, storePositionWithTermVector, storeOffsetWithTermVector, false);
		}
		
		/// <summary>If the field is not yet known, adds it. If it is known, checks to make
		/// sure that the isIndexed flag is the same as was given previously for this
		/// field. If not - marks it as being indexed.  Same goes for the TermVector
		/// parameters.
		/// 
		/// </summary>
		/// <param name="name">The name of the field
		/// </param>
		/// <param name="isIndexed">true if the field is indexed
		/// </param>
		/// <param name="storeTermVector">true if the term vector should be stored
		/// </param>
		/// <param name="storePositionWithTermVector">true if the term vector with positions should be stored
		/// </param>
		/// <param name="storeOffsetWithTermVector">true if the term vector with offsets should be stored
		/// </param>
		/// <param name="omitNorms">true if the norms for the indexed field should be omitted
		/// </param>
		public void  Add(System.String name, bool isIndexed, bool storeTermVector, bool storePositionWithTermVector, bool storeOffsetWithTermVector, bool omitNorms)
		{
			FieldInfo fi = FieldInfo(name);
			if (fi == null)
			{
				AddInternal(name, isIndexed, storeTermVector, storePositionWithTermVector, storeOffsetWithTermVector, omitNorms);
			}
			else
			{
				if (fi.isIndexed != isIndexed)
				{
					fi.isIndexed = true; // once indexed, always index
				}
				if (fi.storeTermVector != storeTermVector)
				{
					fi.storeTermVector = true; // once vector, always vector
				}
				if (fi.storePositionWithTermVector != storePositionWithTermVector)
				{
					fi.storePositionWithTermVector = true; // once vector, always vector
				}
				if (fi.storeOffsetWithTermVector != storeOffsetWithTermVector)
				{
					fi.storeOffsetWithTermVector = true; // once vector, always vector
				}
				if (fi.omitNorms != omitNorms)
				{
					fi.omitNorms = false; // once norms are stored, always store
				}
			}
		}
		
		
		private void  AddInternal(System.String name, bool isIndexed, bool storeTermVector, bool storePositionWithTermVector, bool storeOffsetWithTermVector, bool omitNorms)
		{
			FieldInfo fi = new FieldInfo(name, isIndexed, byNumber.Count, storeTermVector, storePositionWithTermVector, storeOffsetWithTermVector, omitNorms);
			byNumber.Add(fi);
			byName[name] = fi;
		}
		
		public int FieldNumber(System.String fieldName)
		{
			try
			{
				FieldInfo fi = FieldInfo(fieldName);
				if (fi != null)
					return fi.number;
			}
			catch (System.IndexOutOfRangeException ioobe)
			{
				return - 1;
			}
			return - 1;
		}
		
		public FieldInfo FieldInfo(System.String fieldName)
		{
			return (FieldInfo) byName[fieldName];
		}
		
		/// <summary> Return the fieldName identified by its number.
		/// 
		/// </summary>
		/// <param name="fieldNumber">
		/// </param>
		/// <returns> the fieldName or an empty string when the field
		/// with the given number doesn't exist.
		/// </returns>
		public System.String FieldName(int fieldNumber)
		{
			FieldInfo fi = FieldInfo(fieldNumber);
			if (fi != null)
				return fi.name;
			return "";

			/*
			try
			{
				return FieldInfo(fieldNumber).name;
			}
			catch (System.NullReferenceException)
			{
				return "";
			}
			*/
		}
		
		/// <summary> Return the fieldinfo object referenced by the fieldNumber.</summary>
		/// <param name="fieldNumber">
		/// </param>
		/// <returns> the FieldInfo object or null when the given fieldNumber
		/// doesn't exist.
		/// </returns>
		public FieldInfo FieldInfo(int fieldNumber)
		{
			if (fieldNumber > -1 && fieldNumber < byNumber.Count)
				return (FieldInfo) byNumber[fieldNumber];
			return null;

			/*
            try
			{
				return (FieldInfo) byNumber[fieldNumber];
			}
			catch (System.ArgumentOutOfRangeException) // (System.IndexOutOfRangeException)
			{
				return null;
			}
			*/
		}
		
		public int Size()
		{
			return byNumber.Count;
		}
		
		public bool HasVectors()
		{
			bool hasVectors = false;
			for (int i = 0; i < Size(); i++)
			{
				if (FieldInfo(i).storeTermVector)
				{
					hasVectors = true;
					break;
				}
			}
			return hasVectors;
		}
		
		public void  Write(Directory d, System.String name)
		{
			IndexOutput output = d.CreateOutput(name);
			try
			{
				Write(output);
			}
			finally
			{
				output.Close();
			}
		}
		
		public void  Write(IndexOutput output)
		{
			output.WriteVInt(Size());
			for (int i = 0; i < Size(); i++)
			{
				FieldInfo fi = FieldInfo(i);
				byte bits = (byte) (0x0);
				if (fi.isIndexed)
					bits |= IS_INDEXED;
				if (fi.storeTermVector)
					bits |= STORE_TERMVECTOR;
				if (fi.storePositionWithTermVector)
					bits |= STORE_POSITIONS_WITH_TERMVECTOR;
				if (fi.storeOffsetWithTermVector)
					bits |= STORE_OFFSET_WITH_TERMVECTOR;
				if (fi.omitNorms)
					bits |= OMIT_NORMS;
				output.WriteString(fi.name);
				output.WriteByte(bits);
			}
		}
		
		private void  Read(IndexInput input)
		{
			int size = input.ReadVInt(); //read in the size
			for (int i = 0; i < size; i++)
			{
				System.String name = String.Intern(input.ReadString());
				byte bits = input.ReadByte();
				bool isIndexed = (bits & IS_INDEXED) != 0;
				bool storeTermVector = (bits & STORE_TERMVECTOR) != 0;
				bool storePositionsWithTermVector = (bits & STORE_POSITIONS_WITH_TERMVECTOR) != 0;
				bool storeOffsetWithTermVector = (bits & STORE_OFFSET_WITH_TERMVECTOR) != 0;
				bool omitNorms = (bits & OMIT_NORMS) != 0;
				
				AddInternal(name, isIndexed, storeTermVector, storePositionsWithTermVector, storeOffsetWithTermVector, omitNorms);
			}
		}
	}
}