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
using IndexReader = Lucene.Net.Index.IndexReader;
using PriorityQueue = Lucene.Net.Util.PriorityQueue;
namespace Lucene.Net.Search
{
	
	/// <summary> Expert: A hit queue for sorting by hits by terms in more than one Field.
	/// Uses <code>FieldCache.DEFAULT</code> for maintaining internal term lookup tables.
	/// 
	/// <p>Created: Dec 8, 2003 12:56:03 PM
	/// 
	/// </summary>
	/// <author>   Tim Jones (Nacimiento Software)
	/// </author>
	/// <since>   lucene 1.4
	/// </since>
	/// <version>  $Id: FieldSortedHitQueue.java,v 1.11.2.2 2004/09/30 18:46:27 dnaber Exp $
	/// </version>
	/// <seealso cref="Searchable#Search(Query,Filter,int,Sort)">
	/// </seealso>
	/// <seealso cref="FieldCache">
	/// </seealso>
	class FieldSortedHitQueue : PriorityQueue
	{
		private class AnonymousClassScoreDocComparator : ScoreDocComparator
		{
			public AnonymousClassScoreDocComparator(int[] fieldOrder)
			{
				InitBlock(fieldOrder);
			}
			private void  InitBlock(int[] fieldOrder)
			{
				this.fieldOrder = fieldOrder;
			}
			
			private int[] fieldOrder;
			
			public int Compare(ScoreDoc i, ScoreDoc j)
			{
				int fi = fieldOrder[i.doc];
				int fj = fieldOrder[j.doc];
				if (fi < fj)
					return - 1;
				if (fi > fj)
					return 1;
				return 0;
			}
			
			public virtual System.IComparable SortValue(ScoreDoc i)
			{
				return (System.Int32) fieldOrder[i.doc];
			}
			
			public virtual int SortType()
			{
				return SortField.INT;
			}
		}
		private class AnonymousClassScoreDocComparator1 : ScoreDocComparator
		{
			public AnonymousClassScoreDocComparator1(float[] fieldOrder)
			{
				InitBlock(fieldOrder);
			}
			private void  InitBlock(float[] fieldOrder)
			{
				this.fieldOrder = fieldOrder;
			}
			private float[] fieldOrder;
			
			public int Compare(ScoreDoc i, ScoreDoc j)
			{
				float fi = fieldOrder[i.doc];
				float fj = fieldOrder[j.doc];
				if (fi < fj)
					return - 1;
				if (fi > fj)
					return 1;
				return 0;
			}
			
			public virtual System.IComparable SortValue(ScoreDoc i)
			{
				return (float) fieldOrder[i.doc];
			}
			
			public virtual int SortType()
			{
				return SortField.FLOAT;
			}
		}
		private class AnonymousClassScoreDocComparator2 : ScoreDocComparator
		{
			public AnonymousClassScoreDocComparator2(Lucene.Net.Search.StringIndex index)
			{
				InitBlock(index);
			}
			private void  InitBlock(Lucene.Net.Search.StringIndex index)
			{
				this.index = index;
			}
			private Lucene.Net.Search.StringIndex index;
			
			public int Compare(ScoreDoc i, ScoreDoc j)
			{
				int fi = index.order[i.doc];
				int fj = index.order[j.doc];
				if (fi < fj)
					return - 1;
				if (fi > fj)
					return 1;
				return 0;
			}
			
			public virtual System.IComparable SortValue(ScoreDoc i)
			{
				return index.lookup[index.order[i.doc]];
			}
			
			public virtual int SortType()
			{
				return SortField.STRING;
			}
		}
		private class AnonymousClassScoreDocComparator3 : ScoreDocComparator
		{
			public AnonymousClassScoreDocComparator3(System.Globalization.CompareInfo collator, System.String[] index)
			{
				InitBlock(collator, index);
			}
			private void  InitBlock(System.Globalization.CompareInfo collator, System.String[] index)
			{
                this.collator = collator;
                this.index = index;
            }
			private System.Globalization.CompareInfo collator;
			private System.String[] index;
			
			public int Compare(ScoreDoc i, ScoreDoc j)
			{
				return collator.Compare(index[i.doc].ToString(), index[j.doc].ToString());
			}
			
			public virtual System.IComparable SortValue(ScoreDoc i)
			{
				return index[i.doc];
			}
			
			public virtual int SortType()
			{
				return SortField.STRING;
			}
		}
		
		/// <summary> Creates a hit queue sorted by the given list of fields.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fields">Field names, in priority order (highest priority first).  Cannot be <code>null</code> or empty.
		/// </param>
		/// <param name="size"> The number of hits to retain.  Must be greater than zero.
		/// </param>
		/// <throws>  IOException </throws>
		internal FieldSortedHitQueue(IndexReader reader, SortField[] fields, int size)
		{
			int n = fields.Length;
			comparators = new ScoreDocComparator[n];
			this.fields = new SortField[n];
			for (int i = 0; i < n; ++i)
			{
				System.String fieldname = fields[i].GetField();
				comparators[i] = GetCachedComparator(reader, fieldname, fields[i].GetType(), fields[i].GetLocale(), fields[i].GetFactory());
				this.fields[i] = new SortField(fieldname, comparators[i].SortType(), fields[i].GetReverse());
			}
			Initialize(size);
		}
		
		
		/// <summary>Stores a comparator corresponding to each Field being sorted by </summary>
		protected internal ScoreDocComparator[] comparators;
		
		/// <summary>Stores the sort criteria being used. </summary>
		protected internal SortField[] fields;
		
		/// <summary>Stores the maximum score value encountered, for normalizing.
		/// we only care about scores greater than 1.0 - if all the scores
		/// are less than 1.0, we don't have to normalize. 
		/// </summary>
		protected internal float maxscore = 1.0f;
		
		
		/// <summary> Returns whether <code>a</code> is less relevant than <code>b</code>.</summary>
		/// <param name="a">ScoreDoc
		/// </param>
		/// <param name="b">ScoreDoc
		/// </param>
		/// <returns> <code>true</code> if document <code>a</code> should be sorted after document <code>b</code>.
		/// </returns>
		public override bool LessThan(System.Object a, System.Object b)
		{
			ScoreDoc docA = (ScoreDoc) a;
			ScoreDoc docB = (ScoreDoc) b;
			
			// keep track of maximum score
			if (docA.score > maxscore)
				maxscore = docA.score;
			if (docB.score > maxscore)
				maxscore = docB.score;
			
			// run comparators
			int n = comparators.Length;
			int c = 0;
			for (int i = 0; i < n && c == 0; ++i)
			{
				c = (fields[i].reverse) ? comparators[i].Compare(docB, docA) : comparators[i].Compare(docA, docB);
			}
            // avoid random sort order that could lead to duplicates (bug #31241):
            if (c == 0)
                return docA.doc > docB.doc;
            return c > 0;
        }
		
		
		/// <summary> Given a FieldDoc object, stores the values used
		/// to sort the given document.  These values are not the raw
		/// values out of the index, but the internal representation
		/// of them.  This is so the given search hit can be collated
		/// by a MultiSearcher with other search hits.
		/// </summary>
		/// <param name="doc"> The FieldDoc to store sort values into.
		/// </param>
		/// <returns>  The same FieldDoc passed in.
		/// </returns>
		/// <seealso cref="Searchable#Search(Query,Filter,int,Sort)">
		/// </seealso>
		internal virtual FieldDoc FillFields(FieldDoc doc)
		{
			int n = comparators.Length;
			System.IComparable[] fields = new System.IComparable[n];
			for (int i = 0; i < n; ++i)
				fields[i] = comparators[i].SortValue(doc);
			doc.fields = fields;
			if (maxscore > 1.0f)
				doc.score /= maxscore; // normalize scores
			return doc;
		}
		
		
		/// <summary>Returns the SortFields being used by this hit queue. </summary>
		internal virtual SortField[] GetFields()
		{
			return fields;
		}
		
		/// <summary>Internal cache of comparators. Similar to FieldCache, only
		/// caches comparators instead of term values. 
		/// </summary>
		internal static readonly System.Collections.IDictionary Comparators = new System.Collections.Hashtable();
		
		/// <summary>Returns a comparator if it is in the cache. </summary>
		internal static ScoreDocComparator Lookup(IndexReader reader, System.String field, int type, System.Object factory)
		{
			FieldCacheImpl.Entry entry = (factory != null) ? new FieldCacheImpl.Entry(field, factory) : new FieldCacheImpl.Entry(field, type);
			lock (Comparators.SyncRoot)
			{
                System.Collections.Hashtable readerCache = (System.Collections.Hashtable) Comparators[reader];
                if (readerCache == null)
                    return null;
                return (ScoreDocComparator) readerCache[entry];
			}
		}
		
		/// <summary>Stores a comparator into the cache. </summary>
		internal static System.Object Store(IndexReader reader, System.String field, int type, System.Object factory, System.Object value_Renamed)
		{
			FieldCacheImpl.Entry entry = (factory != null) ? new FieldCacheImpl.Entry(field, factory) : new FieldCacheImpl.Entry(field, type);
			lock (Comparators.SyncRoot)
			{
                System.Collections.Hashtable readerCache = (System.Collections.Hashtable) Comparators[reader];
                if (readerCache == null)
                {
                    readerCache = new System.Collections.Hashtable();
                    Comparators[reader] = readerCache;
                }
                System.Object tempObject;
                tempObject = readerCache[entry];
                readerCache[entry] = value_Renamed;
                return tempObject;
            }
		}
		
		internal static ScoreDocComparator GetCachedComparator(IndexReader reader, System.String fieldname, int type, System.Globalization.CultureInfo locale, SortComparatorSource factory)
		{
			if (type == SortField.DOC)
				return Lucene.Net.Search.ScoreDocComparator_Fields.INDEXORDER;
			if (type == SortField.SCORE)
				return Lucene.Net.Search.ScoreDocComparator_Fields.RELEVANCE;
			ScoreDocComparator comparator = Lookup(reader, fieldname, type, factory);
			if (comparator == null)
			{
				switch (type)
				{
					
					case SortField.AUTO: 
						comparator = ComparatorAuto(reader, fieldname);
						break;
					
					case SortField.INT: 
						comparator = ComparatorInt(reader, fieldname);
						break;
					
					case SortField.FLOAT: 
						comparator = ComparatorFloat(reader, fieldname);
						break;
					
					case SortField.STRING: 
						if (locale != null)
							comparator = ComparatorStringLocale(reader, fieldname, locale);
						else
							comparator = ComparatorString(reader, fieldname);
						break;
					
					case SortField.CUSTOM: 
						comparator = factory.NewComparator(reader, fieldname);
						break;
					
					default: 
						throw new System.SystemException("unknown Field type: " + type);
					
				}
				Store(reader, fieldname, type, factory, comparator);
			}
			return comparator;
		}
		
		/// <summary> Returns a comparator for sorting hits according to a Field containing integers.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Field containg integer values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorInt(IndexReader reader, System.String fieldname)
		{
			System.String field = String.Intern(fieldname);
            int[] fieldOrder = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetInts(reader, field);
            return new AnonymousClassScoreDocComparator(fieldOrder);
        }
		
		/// <summary> Returns a comparator for sorting hits according to a Field containing floats.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Field containg float values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorFloat(IndexReader reader, System.String fieldname)
		{
			System.String field = String.Intern(fieldname);
            float[] fieldOrder = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetFloats(reader, field);
            return new AnonymousClassScoreDocComparator1(fieldOrder);
        }
		
		/// <summary> Returns a comparator for sorting hits according to a Field containing strings.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Field containg string values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorString(IndexReader reader, System.String fieldname)
		{
			System.String field = String.Intern(fieldname);
            Lucene.Net.Search.StringIndex index = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetStringIndex(reader, field);
            return new AnonymousClassScoreDocComparator2(index);
        }
		
		/// <summary> Returns a comparator for sorting hits according to a Field containing strings.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Field containg string values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorStringLocale(IndexReader reader, System.String fieldname, System.Globalization.CultureInfo locale)
		{
			System.Globalization.CompareInfo collator = locale.CompareInfo;
			System.String field = String.Intern(fieldname);
            System.String[] index = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetStrings(reader, field);
            return new AnonymousClassScoreDocComparator3(collator, index);
        }
		
		/// <summary> Returns a comparator for sorting hits according to values in the given Field.
		/// The terms in the Field are looked at to determine whether they contain integers,
		/// floats or strings.  Once the type is determined, one of the other static methods
		/// in this class is called to get the comparator.
		/// </summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Field containg values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorAuto(IndexReader reader, System.String fieldname)
		{
			System.String field = String.Intern(fieldname);
			System.Object lookupArray = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetAuto(reader, field);
			if (lookupArray is Lucene.Net.Search.StringIndex)
			{
				return ComparatorString(reader, field);
			}
			else if (lookupArray is int[])
			{
				return ComparatorInt(reader, field);
			}
			else if (lookupArray is float[])
			{
				return ComparatorFloat(reader, field);
			}
			else if (lookupArray is System.String[])
			{
				return ComparatorString(reader, field);
			}
			else
			{
				throw new System.SystemException("unknown data type in Field '" + field + "'");
			}
		}
	}
}