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
using Term = Lucene.Net.Index.Term;
using TermDocs = Lucene.Net.Index.TermDocs;
using TermEnum = Lucene.Net.Index.TermEnum;

namespace Lucene.Net.Search
{
	
	/// <summary> Expert: The default cache implementation, storing all values in memory.
	/// A WeakHashMap is used for storage.
	/// 
	/// <p>Created: May 19, 2004 4:40:36 PM
	/// 
	/// </summary>
	/// <author>   Tim Jones (Nacimiento Software)
	/// </author>
	/// <since>   lucene 1.4
	/// </since>
	/// <version>  $Id: FieldCacheImpl.java 605225 2007-12-18 15:13:05Z gsingers $
	/// </version>
	public class FieldCacheImpl : FieldCache
	{
		public FieldCacheImpl()
		{
			InitBlock();
		}
		public class AnonymousClassByteParser : ByteParser
		{
			public virtual byte ParseByte(System.String value_Renamed)
			{
				return (byte) System.Byte.Parse(value_Renamed);
			}
		}
		public class AnonymousClassShortParser : ShortParser
		{
			public virtual short ParseShort(System.String value_Renamed)
			{
				return System.Int16.Parse(value_Renamed);
			}
		}
		public class AnonymousClassIntParser : IntParser
		{
			public virtual int ParseInt(System.String value_Renamed)
			{
				return System.Int32.Parse(value_Renamed);
			}
		}
		public class AnonymousClassFloatParser : FloatParser
		{
			public virtual float ParseFloat(System.String value_Renamed)
			{
				return SupportClass.Single.Parse(value_Renamed);
			}
		}
		
		internal class AnonymousClassCache : Cache
		{
			public AnonymousClassCache(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private Lucene.Net.Search.FieldCacheImpl enclosingInstance;
			public Lucene.Net.Search.FieldCacheImpl Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			protected internal override System.Object CreateValue(IndexReader reader, System.Object entryKey)
			{
				Entry entry = (Entry) entryKey;
				System.String field = entry.field;
				ByteParser parser = (ByteParser) entry.custom;
				byte[] retArray = new byte[reader.MaxDoc()];
				TermDocs termDocs = reader.TermDocs();
				TermEnum termEnum = reader.Terms(new Term(field, ""));
				try
				{
					do 
					{
						Term term = termEnum.Term();
						if (term == null || (System.Object) term.Field() != (System.Object) field)
							break;
						byte termval = parser.ParseByte(term.Text());
						termDocs.Seek(termEnum);
						while (termDocs.Next())
						{
							retArray[termDocs.Doc()] = termval;
						}
					}
					while (termEnum.Next());
				}
				finally
				{
					termDocs.Close();
					termEnum.Close();
				}
				return retArray;
			}
		}
		
		internal class AnonymousClassCache1 : Cache
		{
			public AnonymousClassCache1(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private Lucene.Net.Search.FieldCacheImpl enclosingInstance;
			public Lucene.Net.Search.FieldCacheImpl Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			protected internal override System.Object CreateValue(IndexReader reader, System.Object entryKey)
			{
				Entry entry = (Entry) entryKey;
				System.String field = entry.field;
				ShortParser parser = (ShortParser) entry.custom;
				short[] retArray = new short[reader.MaxDoc()];
				TermDocs termDocs = reader.TermDocs();
				TermEnum termEnum = reader.Terms(new Term(field, ""));
				try
				{
					do 
					{
						Term term = termEnum.Term();
						if (term == null || (System.Object) term.Field() != (System.Object) field)
							break;
						short termval = parser.ParseShort(term.Text());
						termDocs.Seek(termEnum);
						while (termDocs.Next())
						{
							retArray[termDocs.Doc()] = termval;
						}
					}
					while (termEnum.Next());
				}
				finally
				{
					termDocs.Close();
					termEnum.Close();
				}
				return retArray;
			}
		}
		
		internal class AnonymousClassCache2 : Cache
		{
			public AnonymousClassCache2(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private Lucene.Net.Search.FieldCacheImpl enclosingInstance;
			public Lucene.Net.Search.FieldCacheImpl Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			protected internal override System.Object CreateValue(IndexReader reader, System.Object entryKey)
			{
				Entry entry = (Entry) entryKey;
				System.String field = entry.field;
				IntParser parser = (IntParser) entry.custom;
				int[] retArray = new int[reader.MaxDoc()];
				TermDocs termDocs = reader.TermDocs();
				TermEnum termEnum = reader.Terms(new Term(field, ""));
				try
				{
					do 
					{
						Term term = termEnum.Term();
						if (term == null || (System.Object) term.Field() != (System.Object) field)
							break;
						int termval = parser.ParseInt(term.Text());
						termDocs.Seek(termEnum);
						while (termDocs.Next())
						{
							retArray[termDocs.Doc()] = termval;
						}
					}
					while (termEnum.Next());
				}
				finally
				{
					termDocs.Close();
					termEnum.Close();
				}
				return retArray;
			}
		}
		
		internal class AnonymousClassCache3 : Cache
		{
			public AnonymousClassCache3(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private Lucene.Net.Search.FieldCacheImpl enclosingInstance;
			public Lucene.Net.Search.FieldCacheImpl Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			protected internal override System.Object CreateValue(IndexReader reader, System.Object entryKey)
			{
				Entry entry = (Entry) entryKey;
				System.String field = entry.field;
				FloatParser parser = (FloatParser) entry.custom;
				float[] retArray = new float[reader.MaxDoc()];
				TermDocs termDocs = reader.TermDocs();
				TermEnum termEnum = reader.Terms(new Term(field, ""));
				try
				{
					do 
					{
						Term term = termEnum.Term();
						if (term == null || (System.Object) term.Field() != (System.Object) field)
							break;
						float termval = parser.ParseFloat(term.Text());
						termDocs.Seek(termEnum);
						while (termDocs.Next())
						{
							retArray[termDocs.Doc()] = termval;
						}
					}
					while (termEnum.Next());
				}
				finally
				{
					termDocs.Close();
					termEnum.Close();
				}
				return retArray;
			}
		}
		
		internal class AnonymousClassCache4 : Cache
		{
			public AnonymousClassCache4(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private Lucene.Net.Search.FieldCacheImpl enclosingInstance;
			public Lucene.Net.Search.FieldCacheImpl Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			protected internal override System.Object CreateValue(IndexReader reader, System.Object fieldKey)
			{
				System.String field = String.Intern(((System.String) fieldKey));
				System.String[] retArray = new System.String[reader.MaxDoc()];
				TermDocs termDocs = reader.TermDocs();
				TermEnum termEnum = reader.Terms(new Term(field, ""));
				try
				{
					do 
					{
						Term term = termEnum.Term();
						if (term == null || (System.Object) term.Field() != (System.Object) field)
							break;
						System.String termval = term.Text();
						termDocs.Seek(termEnum);
						while (termDocs.Next())
						{
							retArray[termDocs.Doc()] = termval;
						}
					}
					while (termEnum.Next());
				}
				finally
				{
					termDocs.Close();
					termEnum.Close();
				}
				return retArray;
			}
		}
		
		internal class AnonymousClassCache5 : Cache
		{
			public AnonymousClassCache5(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private Lucene.Net.Search.FieldCacheImpl enclosingInstance;
			public Lucene.Net.Search.FieldCacheImpl Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			protected internal override System.Object CreateValue(IndexReader reader, System.Object fieldKey)
			{
				System.String field = String.Intern(((System.String) fieldKey));
				int[] retArray = new int[reader.MaxDoc()];
				System.String[] mterms = new System.String[reader.MaxDoc() + 1];
				TermDocs termDocs = reader.TermDocs();
				TermEnum termEnum = reader.Terms(new Term(field, ""));
				int t = 0; // current term number
				
				// an entry for documents that have no terms in this field
				// should a document with no terms be at top or bottom?
				// this puts them at the top - if it is changed, FieldDocSortedHitQueue
				// needs to change as well.
				mterms[t++] = null;
				
				try
				{
					do 
					{
						Term term = termEnum.Term();
						if (term == null || (System.Object) term.Field() != (System.Object) field)
							break;
						
						// store term text
						// we expect that there is at most one term per document
						if (t >= mterms.Length)
							throw new System.SystemException("there are more terms than " + "documents in field \"" + field + "\", but it's impossible to sort on " + "tokenized fields");
						mterms[t] = term.Text();
						
						termDocs.Seek(termEnum);
						while (termDocs.Next())
						{
							retArray[termDocs.Doc()] = t;
						}
						
						t++;
					}
					while (termEnum.Next());
				}
				finally
				{
					termDocs.Close();
					termEnum.Close();
				}
				
				if (t == 0)
				{
					// if there are no terms, make the term array
					// have a single null entry
					mterms = new System.String[1];
				}
				else if (t < mterms.Length)
				{
					// if there are less terms than documents,
					// trim off the dead array space
					System.String[] terms = new System.String[t];
					Array.Copy(mterms, 0, terms, 0, t);
					mterms = terms;
				}
				
				StringIndex value_Renamed = new StringIndex(retArray, mterms);
				return value_Renamed;
			}
		}
		
		internal class AnonymousClassCache6 : Cache
		{
			public AnonymousClassCache6(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private Lucene.Net.Search.FieldCacheImpl enclosingInstance;
			public Lucene.Net.Search.FieldCacheImpl Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			protected internal override System.Object CreateValue(IndexReader reader, System.Object fieldKey)
			{
				System.String field = String.Intern(((System.String) fieldKey));
				TermEnum enumerator = reader.Terms(new Term(field, ""));
				try
				{
					Term term = enumerator.Term();
					if (term == null)
					{
						throw new System.SystemException("no terms in field " + field + " - cannot determine sort type");
					}
					System.Object ret = null;
					if ((System.Object) term.Field() == (System.Object) field)
					{
						System.String termtext = term.Text().Trim();
						
						/**
						* Java 1.4 level code:
						
						if (pIntegers.matcher(termtext).matches())
						return IntegerSortedHitQueue.comparator (reader, enumerator, field);
						
						else if (pFloats.matcher(termtext).matches())
						return FloatSortedHitQueue.comparator (reader, enumerator, field);
						*/
						
						// Java 1.3 level code:
						try
						{
							System.Int32.Parse(termtext);
							ret = Enclosing_Instance.GetInts(reader, field);
						}
						catch (System.FormatException nfe1)
						{
							try
							{
								SupportClass.Single.Parse(termtext);
								ret = Enclosing_Instance.GetFloats(reader, field);
							}
							catch (System.FormatException nfe3)
							{
								ret = Enclosing_Instance.GetStringIndex(reader, field);
							}
						}
					}
					else
					{
						throw new System.SystemException("field \"" + field + "\" does not appear to be indexed");
					}
					return ret;
				}
				finally
				{
					enumerator.Close();
				}
			}
		}
		
		internal class AnonymousClassCache7 : Cache
		{
			public AnonymousClassCache7(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private Lucene.Net.Search.FieldCacheImpl enclosingInstance;
			public Lucene.Net.Search.FieldCacheImpl Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			protected internal override System.Object CreateValue(IndexReader reader, System.Object entryKey)
			{
				Entry entry = (Entry) entryKey;
				System.String field = entry.field;
				SortComparator comparator = (SortComparator) entry.custom;
				System.IComparable[] retArray = new System.IComparable[reader.MaxDoc()];
				TermDocs termDocs = reader.TermDocs();
				TermEnum termEnum = reader.Terms(new Term(field, ""));
				try
				{
					do 
					{
						Term term = termEnum.Term();
						if (term == null || (System.Object) term.Field() != (System.Object) field)
							break;
						System.IComparable termval = comparator.GetComparable(term.Text());
						termDocs.Seek(termEnum);
						while (termDocs.Next())
						{
							retArray[termDocs.Doc()] = termval;
						}
					}
					while (termEnum.Next());
				}
				finally
				{
					termDocs.Close();
					termEnum.Close();
				}
				return retArray;
			}
		}
		private void  InitBlock()
		{
			bytesCache = new AnonymousClassCache(this);
			shortsCache = new AnonymousClassCache1(this);
			intsCache = new AnonymousClassCache2(this);
			floatsCache = new AnonymousClassCache3(this);
			stringsCache = new AnonymousClassCache4(this);
			stringsIndexCache = new AnonymousClassCache5(this);
			autoCache = new AnonymousClassCache6(this);
			customCache = new AnonymousClassCache7(this);
		}
		
		/// <summary>Expert: Internal cache. </summary>
		internal abstract class Cache
		{
			private System.Collections.IDictionary readerCache = new SupportClass.WeakHashTable();
			
			protected internal abstract System.Object CreateValue(IndexReader reader, System.Object key);
			
			public virtual System.Object Get(IndexReader reader, System.Object key)
			{
				System.Collections.IDictionary innerCache;
				System.Object value_Renamed;
				lock (readerCache.SyncRoot)
				{
					innerCache = (System.Collections.IDictionary) readerCache[reader];
					if (innerCache == null)
					{
						innerCache = new System.Collections.Hashtable();
						readerCache[reader] = innerCache;
						value_Renamed = null;
					}
					else
					{
						value_Renamed = innerCache[key];
					}
					if (value_Renamed == null)
					{
						value_Renamed = new CreationPlaceholder();
						innerCache[key] = value_Renamed;
					}
				}
				if (value_Renamed is CreationPlaceholder)
				{
					lock (value_Renamed)
					{
						CreationPlaceholder progress = (CreationPlaceholder) value_Renamed;
						if (progress.value_Renamed == null)
						{
							progress.value_Renamed = CreateValue(reader, key);
							lock (readerCache.SyncRoot)
							{
								innerCache[key] = progress.value_Renamed;
							}
						}
						return progress.value_Renamed;
					}
				}
				return value_Renamed;
			}
		}
		
		internal sealed class CreationPlaceholder
		{
			internal System.Object value_Renamed;
		}
		
		/// <summary>Expert: Every composite-key in the internal cache is of this type. </summary>
		internal class Entry
		{
			internal System.String field; // which Fieldable
			internal int type; // which SortField type
			internal System.Object custom; // which custom comparator
			internal System.Globalization.CultureInfo locale; // the locale we're sorting (if string)
			
			/// <summary>Creates one of these objects. </summary>
			internal Entry(System.String field, int type, System.Globalization.CultureInfo locale)
			{
				this.field = String.Intern(field);
				this.type = type;
				this.custom = null;
				this.locale = locale;
			}
			
			/// <summary>Creates one of these objects for a custom comparator. </summary>
			internal Entry(System.String field, System.Object custom)
			{
				this.field = String.Intern(field);
				this.type = SortField.CUSTOM;
				this.custom = custom;
				this.locale = null;
			}
			
			/// <summary>Two of these are equal iff they reference the same field and type. </summary>
			public  override bool Equals(System.Object o)
			{
				if (o is Entry)
				{
					Entry other = (Entry) o;
					if ((System.Object) other.field == (System.Object) field && other.type == type)
					{
						if (other.locale == null ? locale == null : other.locale.Equals(locale))
						{
							if (other.custom == null)
							{
								if (custom == null)
									return true;
							}
							else if (other.custom.Equals(custom))
							{
								return true;
							}
						}
					}
				}
				return false;
			}
			
			/// <summary>Composes a hashcode based on the field and type. </summary>
			public override int GetHashCode()
			{
				return field.GetHashCode() ^ type ^ (custom == null ? 0 : custom.GetHashCode()) ^ (locale == null ? 0 : locale.GetHashCode());
			}
		}
		
		private static readonly ByteParser BYTE_PARSER;
		
		private static readonly ShortParser SHORT_PARSER;
		
		private static readonly IntParser INT_PARSER;
		
		
		private static readonly FloatParser FLOAT_PARSER;
		
		// inherit javadocs
		public virtual byte[] GetBytes(IndexReader reader, System.String field)
		{
			return GetBytes(reader, field, BYTE_PARSER);
		}
		
		// inherit javadocs
		public virtual byte[] GetBytes(IndexReader reader, System.String field, ByteParser parser)
		{
			return (byte[]) bytesCache.Get(reader, new Entry(field, parser));
		}
		
		internal Cache bytesCache;
		
		// inherit javadocs
		public virtual short[] GetShorts(IndexReader reader, System.String field)
		{
			return GetShorts(reader, field, SHORT_PARSER);
		}
		
		// inherit javadocs
		public virtual short[] GetShorts(IndexReader reader, System.String field, ShortParser parser)
		{
			return (short[]) shortsCache.Get(reader, new Entry(field, parser));
		}
		
		internal Cache shortsCache;
		
		// inherit javadocs
		public virtual int[] GetInts(IndexReader reader, System.String field)
		{
			return GetInts(reader, field, INT_PARSER);
		}
		
		// inherit javadocs
		public virtual int[] GetInts(IndexReader reader, System.String field, IntParser parser)
		{
			return (int[]) intsCache.Get(reader, new Entry(field, parser));
		}
		
		internal Cache intsCache;
		
		
		// inherit javadocs
		public virtual float[] GetFloats(IndexReader reader, System.String field)
		{
			return GetFloats(reader, field, FLOAT_PARSER);
		}
		
		// inherit javadocs
		public virtual float[] GetFloats(IndexReader reader, System.String field, FloatParser parser)
		{
			return (float[]) floatsCache.Get(reader, new Entry(field, parser));
		}
		
		internal Cache floatsCache;
		
		// inherit javadocs
		public virtual System.String[] GetStrings(IndexReader reader, System.String field)
		{
			return (System.String[]) stringsCache.Get(reader, field);
		}
		
		internal Cache stringsCache;
		
		// inherit javadocs
		public virtual StringIndex GetStringIndex(IndexReader reader, System.String field)
		{
			return (StringIndex) stringsIndexCache.Get(reader, field);
		}
		
		internal Cache stringsIndexCache;
		
		/// <summary>The pattern used to detect integer values in a field </summary>
		/// <summary>removed for java 1.3 compatibility
		/// protected static final Pattern pIntegers = Pattern.compile ("[0-9\\-]+");
		/// 
		/// </summary>
		
		/// <summary>The pattern used to detect float values in a field </summary>
		/// <summary> removed for java 1.3 compatibility
		/// protected static final Object pFloats = Pattern.compile ("[0-9+\\-\\.eEfFdD]+");
		/// </summary>
		
		// inherit javadocs
		public virtual System.Object GetAuto(IndexReader reader, System.String field)
		{
			return autoCache.Get(reader, field);
		}
		
		internal Cache autoCache;
		
		// inherit javadocs
		public virtual System.IComparable[] GetCustom(IndexReader reader, System.String field, SortComparator comparator)
		{
			return (System.IComparable[]) customCache.Get(reader, new Entry(field, comparator));
		}
		
		internal Cache customCache;
		static FieldCacheImpl()
		{
			BYTE_PARSER = new AnonymousClassByteParser();
			SHORT_PARSER = new AnonymousClassShortParser();
			INT_PARSER = new AnonymousClassIntParser();
			FLOAT_PARSER = new AnonymousClassFloatParser();
		}
	}
}