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
using IndexReader = Lucene.Net.Index.IndexReader;
using Hits = Lucene.Net.Search.Hits;
using Searcher = Lucene.Net.Search.Searcher;

namespace Lucene.Net.Documents
{
	
	/// <summary>Documents are the unit of indexing and search.
	/// 
	/// A Document is a set of fields.  Each field has a name and a textual value.
	/// A field may be {@link Field#IsStored() stored} with the document, in which
	/// case it is returned with search hits on the document.  Thus each document
	/// should typically contain one or more stored fields which uniquely identify
	/// it.
	/// 
	/// <p>Note that fields which are <i>not</i> {@link Field#IsStored() stored} are
	/// <i>not</i> available in documents retrieved from the index, e.g. with {@link
	/// Hits#Doc(int)}, {@link Searcher#Doc(int)} or {@link
	/// IndexReader#Document(int)}.
	/// </summary>
	
	[Serializable]
	public sealed class Document
	{
		internal System.Collections.IList fields = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
		private float boost = 1.0f;
		
		/// <summary>Constructs a new document with no fields. </summary>
		public Document()
		{
		}
		
        /// <summary>Returns the number of fields in this document</summary>
        /// Added as a helper for Lucene.Net
        public int GetFieldsCount()
        {
            return fields.Count;
        }
		
		/// <summary>Sets a boost factor for hits on any field of this document.  This value
		/// will be multiplied into the score of all hits on this document.
		/// 
		/// <p>Values are multiplied into the value of {@link Field#GetBoost()} of
		/// each field in this document.  Thus, this method in effect sets a default
		/// boost for the fields of this document.
		/// 
		/// </summary>
		/// <seealso cref="Field.SetBoost(float)">
		/// </seealso>
		public void  SetBoost(float boost)
		{
			this.boost = boost;
		}
		
		/// <summary>Returns the boost factor for hits on any field of this document.
		/// 
		/// <p>The default value is 1.0.
		/// 
		/// <p>Note: This value is not stored directly with the document in the index.
		/// Documents returned from {@link IndexReader#Document(int)} and
		/// {@link Hits#Doc(int)} may thus not have the same value present as when
		/// this document was indexed.
		/// 
		/// </summary>
		/// <seealso cref="SetBoost(float)">
		/// </seealso>
		public float GetBoost()
		{
			return boost;
		}
		
		/// <summary> <p>Adds a field to a document.  Several fields may be added with
		/// the same name.  In this case, if the fields are indexed, their text is
		/// treated as though appended for the purposes of search.</p>
		/// <p> Note that add like the removeField(s) methods only makes sense 
		/// prior to adding a document to an index. These methods cannot
		/// be used to change the content of an existing index! In order to achieve this,
		/// a document has to be deleted from an index and a new changed version of that
		/// document has to be added.</p>
		/// </summary>
		public void  Add(Field field)
		{
			fields.Add(field);
		}
		
		/// <summary> <p>Removes field with the specified name from the document.
		/// If multiple fields exist with this name, this method removes the first field that has been added.
		/// If there is no field with the specified name, the document remains unchanged.</p>
		/// <p> Note that the removeField(s) methods like the add method only make sense 
		/// prior to adding a document to an index. These methods cannot
		/// be used to change the content of an existing index! In order to achieve this,
		/// a document has to be deleted from an index and a new changed version of that
		/// document has to be added.</p>
		/// </summary>
		public void  RemoveField(System.String name)
		{
			System.Collections.IEnumerator it = fields.GetEnumerator();
			while (it.MoveNext())
			{
				Field field = (Field) it.Current;
				if (field.Name().Equals(name))
				{
					fields.Remove(field);
					return ;
				}
			}
		}
		
		/// <summary> <p>Removes all fields with the given name from the document.
		/// If there is no field with the specified name, the document remains unchanged.</p>
		/// <p> Note that the removeField(s) methods like the add method only make sense 
		/// prior to adding a document to an index. These methods cannot
		/// be used to change the content of an existing index! In order to achieve this,
		/// a document has to be deleted from an index and a new changed version of that
		/// document has to be added.</p>
		/// </summary>
		public void  RemoveFields(System.String name)
		{
            for (int i = fields.Count - 1; i >= 0; i--)
            {
                Field field = (Field) fields[i];
                if (field.Name().Equals(name))
                {
                    fields.RemoveAt(i);
                }
            }
		}
		
		/// <summary>Returns a field with the given name if any exist in this document, or
		/// null.  If multiple fields exists with this name, this method returns the
		/// first value added.
		/// </summary>
		public Field GetField(System.String name)
		{
			for (int i = 0; i < fields.Count; i++)
			{
				Field field = (Field) fields[i];
				if (field.Name().Equals(name))
					return field;
			}
			return null;
		}
		
		/// <summary>Returns the string value of the field with the given name if any exist in
		/// this document, or null.  If multiple fields exist with this name, this
		/// method returns the first value added. If only binary fields with this name
		/// exist, returns null.
		/// </summary>
		public System.String Get(System.String name)
		{
			for (int i = 0; i < fields.Count; i++)
			{
				Field field = (Field) fields[i];
				if (field.Name().Equals(name) && (!field.IsBinary()))
					return field.StringValue();
			}
			return null;
		}
		
		/// <summary>Returns an Enumeration of all the fields in a document. </summary>
		public System.Collections.IEnumerable Fields()
		{
            return fields;
		}
		
		/// <summary> Returns an array of {@link Field}s with the given name.
		/// This method can return <code>null</code>.
		/// 
		/// </summary>
		/// <param name="name">the name of the field
		/// </param>
		/// <returns> a <code>Field[]</code> array
		/// </returns>
		public Field[] GetFields(System.String name)
		{
			System.Collections.ArrayList result = new System.Collections.ArrayList();
			for (int i = 0; i < fields.Count; i++)
			{
				Field field = (Field) fields[i];
				if (field.Name().Equals(name))
				{
					result.Add(field);
				}
			}
			
			if (result.Count == 0)
				return null;
			
			return (Field[]) result.ToArray(typeof(Field));
		}
		
		/// <summary> Returns an array of values of the field specified as the method parameter.
		/// This method can return <code>null</code>.
		/// 
		/// </summary>
		/// <param name="name">the name of the field
		/// </param>
		/// <returns> a <code>String[]</code> of field values
		/// </returns>
		public System.String[] GetValues(System.String name)
		{
			System.Collections.ArrayList result = new System.Collections.ArrayList();
			for (int i = 0; i < fields.Count; i++)
			{
				Field field = (Field) fields[i];
				if (field.Name().Equals(name) && (!field.IsBinary()))
					result.Add(field.StringValue());
			}
			
			if (result.Count == 0)
				return null;
			
			return (System.String[]) (result.ToArray(typeof(System.String)));
		}
		
		/// <summary> Returns an array of byte arrays for of the fields that have the name specified
		/// as the method parameter. This method will return <code>null</code> if no
		/// binary fields with the specified name are available.
		/// 
		/// </summary>
		/// <param name="name">the name of the field
		/// </param>
		/// <returns> a  <code>byte[][]</code> of binary field values.
		/// </returns>
		public byte[][] GetBinaryValues(System.String name)
		{
			System.Collections.IList result = new System.Collections.ArrayList();
			for (int i = 0; i < fields.Count; i++)
			{
				Field field = (Field) fields[i];
				if (field.Name().Equals(name) && (field.IsBinary()))
                {
                    byte[] byteArray = field.BinaryValue();
                    byte[] resultByteArray = new byte[byteArray.Length];
                    for (int index = 0; index < byteArray.Length; index++)
                        resultByteArray[index] = (byte) byteArray[index];

                    result.Add(resultByteArray);
                }
            }
			
			if (result.Count == 0)
				return null;
			
            System.Collections.ICollection c = result;
            System.Object[] objects = new byte[result.Count][];

            System.Type type = objects.GetType().GetElementType();
            System.Object[] objs = (System.Object[]) Array.CreateInstance(type, c.Count );

            System.Collections.IEnumerator e = c.GetEnumerator();
            int ii = 0;

            while (e.MoveNext())
                objs[ii++] = e.Current;

            // If objects is smaller than c then do not return the new array in the parameter
            if (objects.Length >= c.Count)
                objs.CopyTo(objects, 0);

            return (byte[][]) objs;
        }
		
		/// <summary> Returns an array of bytes for the first (or only) field that has the name
		/// specified as the method parameter. This method will return <code>null</code>
		/// if no binary fields with the specified name are available.
		/// There may be non-binary fields with the same name.
		/// 
		/// </summary>
		/// <param name="name">the name of the field.
		/// </param>
		/// <returns> a <code>byte[]</code> containing the binary field value.
		/// </returns>
		public byte[] GetBinaryValue(System.String name)
		{
			for (int i = 0; i < fields.Count; i++)
			{
				Field field = (Field) fields[i];
				if (field.Name().Equals(name) && (field.IsBinary()))
					return field.BinaryValue();
			}
			return null;
		}
		
		/// <summary>Prints the fields of a document for human consumption. </summary>
		public override System.String ToString()
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			buffer.Append("Document<");
			for (int i = 0; i < fields.Count; i++)
			{
				Field field = (Field) fields[i];
				buffer.Append(field.ToString());
				if (i != fields.Count - 1)
					buffer.Append(" ");
			}
			buffer.Append(">");
			return buffer.ToString();
		}
	}
}