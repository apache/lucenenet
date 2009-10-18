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
using StringIndex = Lucene.Net.Search.StringIndex; // required by GCJ

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
	/// <version>  $Id: FieldCacheImpl.java 331964 2005-11-09 06:44:10Z otis $
	/// </version>
	class FieldCacheImpl : FieldCache
	{
		public virtual void Close(IndexReader reader)
		{ 
			lock (this) 
			{ 
				System.Collections.Hashtable readerCache = (System.Collections.Hashtable) cache[reader]; 
				if (readerCache != null) 
				{ 
					readerCache.Clear(); 
					readerCache = null;
				}

				cache.Remove(reader); 
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
				return System.Single.Parse(value_Renamed);
			}
		}
		
		/// <summary>Expert: Every key in the internal cache is of this type. </summary>
		internal class Entry
		{
			internal System.String field; // which Field
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
					if (other.field == field && other.type == type)
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
		
		private static readonly IntParser INT_PARSER;
		
		private static readonly FloatParser FLOAT_PARSER;
		
		/// <summary>The internal cache. Maps Entry to array of interpreted term values. *</summary>
		internal System.Collections.IDictionary cache = new System.Collections.Hashtable();
		
		/// <summary>See if an object is in the cache. </summary>
		internal virtual System.Object Lookup(IndexReader reader, System.String field, int type, System.Globalization.CultureInfo locale)
		{
			Entry entry = new Entry(field, type, locale);
			lock (this)
			{
				System.Collections.Hashtable readerCache = (System.Collections.Hashtable) cache[reader];
				if (readerCache == null)
					return null;
				return readerCache[entry];
			}
		}
		
		/// <summary>See if a custom object is in the cache. </summary>
		internal virtual System.Object Lookup(IndexReader reader, System.String field, System.Object comparer)
		{
			Entry entry = new Entry(field, comparer);
			lock (this)
			{
				System.Collections.Hashtable readerCache = (System.Collections.Hashtable) cache[reader];
				if (readerCache == null)
					return null;
				return readerCache[entry];
			}
		}
		
		/// <summary>Put an object into the cache. </summary>
		internal virtual System.Object Store(IndexReader reader, System.String field, int type, System.Globalization.CultureInfo locale, System.Object value_Renamed)
		{
			Entry entry = new Entry(field, type, locale);
			lock (this)
			{
				System.Collections.Hashtable readerCache = (System.Collections.Hashtable) cache[reader];
				if (readerCache == null)
				{
					readerCache = new System.Collections.Hashtable();
					cache[reader] = readerCache;
				}
				System.Object tempObject;
				tempObject = readerCache[entry];
				readerCache[entry] = value_Renamed;
				return tempObject;
			}
		}
		
		/// <summary>Put a custom object into the cache. </summary>
		internal virtual System.Object Store(IndexReader reader, System.String field, System.Object comparer, System.Object value_Renamed)
		{
			Entry entry = new Entry(field, comparer);
			lock (this)
			{
				System.Collections.Hashtable readerCache = (System.Collections.Hashtable) cache[reader];
				if (readerCache == null)
				{
					readerCache = new System.Collections.Hashtable();
					cache[reader] = readerCache;
				}
				System.Object tempObject;
				tempObject = readerCache[entry];
				readerCache[entry] = value_Renamed;
				return tempObject;
			}
		}
		
		// inherit javadocs
		public virtual int[] GetInts(IndexReader reader, System.String field)
		{
			return GetInts(reader, field, INT_PARSER);
		}
		
		// inherit javadocs
		public virtual int[] GetInts(IndexReader reader, System.String field, IntParser parser)
		{
			field = String.Intern(field);
			System.Object ret = Lookup(reader, field, parser);
			if (ret == null)
			{
				int[] retArray = new int[reader.MaxDoc()];
				TermDocs termDocs = reader.TermDocs();
				TermEnum termEnum = reader.Terms(new Term(field, ""));
				try
				{
					do 
					{
						Term term = termEnum.Term();
						if (term == null || term.Field() != field)
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
				Store(reader, field, parser, retArray);
				return retArray;
			}
			return (int[]) ret;
		}
		
		// inherit javadocs
		public virtual float[] GetFloats(IndexReader reader, System.String field)
		{
			return GetFloats(reader, field, FLOAT_PARSER);
		}
		
		// inherit javadocs
		public virtual float[] GetFloats(IndexReader reader, System.String field, FloatParser parser)
		{
			field = String.Intern(field);
			System.Object ret = Lookup(reader, field, parser);
			if (ret == null)
			{
				float[] retArray = new float[reader.MaxDoc()];
				TermDocs termDocs = reader.TermDocs();
				TermEnum termEnum = reader.Terms(new Term(field, ""));
				try
				{
					do 
					{
						Term term = termEnum.Term();
						if (term == null || term.Field() != field)
							break;
						float termval;
						termval = SupportClass.Single.Parse(term.Text());
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
				Store(reader, field, parser, retArray);
				return retArray;
			}
			return (float[]) ret;
		}
		
		// inherit javadocs
		public virtual System.String[] GetStrings(IndexReader reader, System.String field)
		{
			field = String.Intern(field);
			System.Object ret = Lookup(reader, field, SortField.STRING, null);
			if (ret == null)
			{
				System.String[] retArray = new System.String[reader.MaxDoc()];
				TermDocs termDocs = reader.TermDocs();
				TermEnum termEnum = reader.Terms(new Term(field, ""));
				try
				{
					do 
					{
						Term term = termEnum.Term();
						if (term == null || term.Field() != field)
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
				Store(reader, field, SortField.STRING, null, retArray);
				return retArray;
			}
			return (System.String[]) ret;
		}
		
		// inherit javadocs
		public virtual StringIndex GetStringIndex(IndexReader reader, System.String field)
		{
			field = String.Intern(field);
			System.Object ret = Lookup(reader, field, Lucene.Net.Search.FieldCache_Fields.STRING_INDEX, null);
			if (ret == null)
			{
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
						if (term == null || term.Field() != field)
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
				Store(reader, field, Lucene.Net.Search.FieldCache_Fields.STRING_INDEX, null, value_Renamed);
				return value_Renamed;
			}
			return (StringIndex) ret;
		}
		
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
			field = String.Intern(field);
			System.Object ret = Lookup(reader, field, SortField.AUTO, null);
			if (ret == null)
			{
				TermEnum enumerator = reader.Terms(new Term(field, ""));
				try
				{
					Term term = enumerator.Term();
					if (term == null)
					{
						throw new System.SystemException("no terms in field " + field + " - cannot determine sort type");
					}
					if (term.Field() == field)
					{
						System.String termtext = term.Text().Trim();
						
                        /// <summary> Java 1.4 level code:
                        /// if (pIntegers.matcher(termtext).matches())
                        /// return IntegerSortedHitQueue.comparator (reader, enumerator, field);
                        /// else if (pFloats.matcher(termtext).matches())
                        /// return FloatSortedHitQueue.comparator (reader, enumerator, field);
                        /// </summary>
						
                        // Java 1.3 level code:
                        try
						{
							System.Int32.Parse(termtext);
							ret = GetInts(reader, field);
						}
						catch (System.FormatException nfe1)
						{
							try
							{
								System.Single.Parse(termtext);
								ret = GetFloats(reader, field);
							}
							catch (System.FormatException nfe2)
							{
								ret = GetStringIndex(reader, field);
							}
						}
						if (ret != null)
						{
							Store(reader, field, SortField.AUTO, null, ret);
						}
					}
					else
					{
						throw new System.SystemException("field \"" + field + "\" does not appear to be indexed");
					}
				}
				finally
				{
					enumerator.Close();
				}
			}
			return ret;
		}
		
		// inherit javadocs
		public virtual System.IComparable[] GetCustom(IndexReader reader, System.String field, SortComparator comparator)
		{
			field = String.Intern(field);
			System.Object ret = Lookup(reader, field, comparator);
			if (ret == null)
			{
				System.IComparable[] retArray = new System.IComparable[reader.MaxDoc()];
				TermDocs termDocs = reader.TermDocs();
				TermEnum termEnum = reader.Terms(new Term(field, ""));
				try
				{
					do 
					{
						Term term = termEnum.Term();
						if (term == null || term.Field() != field)
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
				Store(reader, field, comparator, retArray);
				return retArray;
			}
			return (System.IComparable[]) ret;
		}
		static FieldCacheImpl()
		{
			INT_PARSER = new AnonymousClassIntParser();
			FLOAT_PARSER = new AnonymousClassFloatParser();
		}
	}
}