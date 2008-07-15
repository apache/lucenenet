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
using FieldCache = Lucene.Net.Search.FieldCache;

namespace Lucene.Net.Search.Function
{
	
	/// <summary> Expert: obtains the ordinal of the field value from the default Lucene 
	/// {@link Lucene.Net.Search.FieldCache Fieldcache} using getStringIndex().
	/// <p>
	/// The native lucene index order is used to assign an ordinal value for each field value.
	/// <p
	/// Field values (terms) are lexicographically ordered by unicode value, and numbered starting at 1.
	/// <p>
	/// Example:
	/// <br>If there were only three field values: "apple","banana","pear"
	/// <br>then ord("apple")=1, ord("banana")=2, ord("pear")=3
	/// <p>
	/// WARNING: 
	/// ord() depends on the position in an index and can thus change 
	/// when other documents are inserted or deleted,
	/// or if a MultiSearcher is used. 
	/// 
	/// <p><font color="#FF0000">
	/// WARNING: The status of the <b>search.function</b> package is experimental. 
	/// The APIs introduced here might change in the future and will not be 
	/// supported anymore in such a case.</font>
	/// 
	/// </summary>
	/// <author>  yonik
	/// </author>
	
	[Serializable]
	public class OrdFieldSource : ValueSource
	{
		private class AnonymousClassDocValues : DocValues
		{
			public AnonymousClassDocValues(int[] arr, OrdFieldSource enclosingInstance)
			{
				InitBlock(arr, enclosingInstance);
			}
			private void  InitBlock(int[] arr, OrdFieldSource enclosingInstance)
			{
				this.arr = arr;
				this.enclosingInstance = enclosingInstance;
			}
			private int[] arr;
			private OrdFieldSource enclosingInstance;
			public OrdFieldSource Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			/*(non-Javadoc) @see Lucene.Net.Search.Function.DocValues#floatVal(int) */
			public override float FloatVal(int doc)
			{
				return (float) arr[doc];
			}
			/*(non-Javadoc) @see Lucene.Net.Search.Function.DocValues#strVal(int) */
			public override System.String StrVal(int doc)
			{
				// the string value of the ordinal, not the string itself
				return System.Convert.ToString(arr[doc]);
			}
			/*(non-Javadoc) @see Lucene.Net.Search.Function.DocValues#toString(int) */
			public override System.String ToString(int doc)
			{
				return Enclosing_Instance.Description() + '=' + IntVal(doc);
			}
			/*(non-Javadoc) @see Lucene.Net.Search.Function.DocValues#getInnerArray() */
			public /*internal*/ override System.Object GetInnerArray()
			{
				return arr;
			}
		}
		protected internal System.String field;
		
		/// <summary> Contructor for a certain field.</summary>
		/// <param name="field">field whose values order is used.  
		/// </param>
		public OrdFieldSource(System.String field)
		{
			this.field = field;
		}
		
		/*(non-Javadoc) @see Lucene.Net.Search.Function.ValueSource#description() */
		public override System.String Description()
		{
			return "ord(" + field + ')';
		}
		
		/*(non-Javadoc) @see Lucene.Net.Search.Function.ValueSource#getValues(Lucene.Net.Index.IndexReader) */
		public override DocValues GetValues(IndexReader reader)
		{
			int[] arr = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetStringIndex(reader, field).order;
			return new AnonymousClassDocValues(arr, this);
		}
		
		/*(non-Javadoc) @see java.lang.Object#equals(java.lang.Object) */
		public  override bool Equals(System.Object o)
		{
			if (o.GetType() != typeof(OrdFieldSource))
				return false;
			OrdFieldSource other = (OrdFieldSource) o;
			return this.field.Equals(other.field);
		}
		
		private static readonly int hcode;
		
		/*(non-Javadoc) @see java.lang.Object#hashCode() */
		public override int GetHashCode()
		{
			return hcode + field.GetHashCode();
		}
		static OrdFieldSource()
		{
			hcode = typeof(OrdFieldSource).GetHashCode();
		}
	}
}